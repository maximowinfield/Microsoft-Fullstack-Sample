using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ CORS Policy: allow dev + deployed origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5000",
                "https://maximowinfield.github.io",
                "https://microsoft-fullstack-sample.onrender.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

// Password hashing (lightweight, no full Identity stack)
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

// JWT Auth (Option B: Parent login + Kid session token)
var jwtSecret =
    builder.Configuration["JWT_SECRET"]
    ?? "dev-only-secret-change-me-32chars-minimum!!"; // 32+ chars

var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ParentOnly", policy => policy.RequireRole("Parent"));
    options.AddPolicy("KidOnly", policy => policy.RequireRole("Kid"));
});

var app = builder.Build();
app.MapGet("/__version", () => Results.Text("CORS-GROUP-V1")).AllowAnonymous();

// -------------------- ✅ SPA Static Files (single-domain hosting) --------------------
app.UseDefaultFiles();
app.UseStaticFiles();
// -----------------------------------------------------------------------------


// Routing + Middleware order matters
app.UseRouting();

// CORS must be before auth so headers get added
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

// All /api routes go through this group
var api = app.MapGroup("/api")
    .RequireCors("Frontend");

// Preflight handler for anything under /api/*
api.MapMethods("/{*path}", new[] { "OPTIONS" }, () => Results.Ok())
   .AllowAnonymous();

// Helper: reliably read the authenticated user id (Parent or Kid)
static string? GetUserId(ClaimsPrincipal principal)
{
    return principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
}

// Helper: create JWT tokens
string CreateToken(string subjectId, string role, string? kidId = null, string? parentId = null)
{
    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, subjectId),
        new Claim(ClaimTypes.NameIdentifier, subjectId),
        new Claim(ClaimTypes.Role, role),
    };

    if (!string.IsNullOrWhiteSpace(kidId))
        claims.Add(new Claim("kidId", kidId));

    if (!string.IsNullOrWhiteSpace(parentId))
        claims.Add(new Claim("parentId", parentId));

    var creds = new SigningCredentials(jwtKey, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// -------------------- ✅ Database migrate + deterministic seed --------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

    // Ensure default parent exists
    var parent = db.Users.FirstOrDefault(u => u.Username == "parent1" && u.Role == "Parent");
    if (parent == null)
    {
        parent = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Username = "parent1",
            Role = "Parent"
        };
        parent.PasswordHash = hasher.HashPassword(parent, "ChangeMe123!");
        db.Users.Add(parent);
        db.SaveChanges();
    }

    var parentId = parent.Id;

    // Ensure default kids exist for that parent
    var hasKidsForParent = db.Kids.Any(k => k.ParentId == parentId);
    if (!hasKidsForParent)
    {
        db.Kids.AddRange(
            new KidProfile { Id = "kid-1", ParentId = parentId, DisplayName = "Kid 1" },
            new KidProfile { Id = "kid-2", ParentId = parentId, DisplayName = "Kid 2" }
        );
        db.SaveChanges();
    }
    // ✅ Seed default rewards if none exist
    if (!db.Rewards.Any())
    {
        db.Rewards.AddRange(
            new Reward { Name = "Ice Cream", Cost = 100 },
            new Reward { Name = "Extra Screen Time", Cost = 50 },
            new Reward { Name = "Movie Night", Cost = 150 }
        );
        db.SaveChanges();
    }

    // ✅ Seed default tasks if none exist
    // (Only seed tasks if the database has zero tasks total, to avoid duplicating)
    if (!db.Tasks.Any())
    {
        db.Tasks.AddRange(
            new KidTask
            {
                Title = "Brush Teeth",
                Points = 50,
                AssignedKidId = "kid-1",
                CreatedByParentId = parentId,
                IsComplete = false,
                CompletedAt = null
            },
            new KidTask
            {
                Title = "Go to School",
                Points = 50,
                AssignedKidId = "kid-1",
                CreatedByParentId = parentId,
                IsComplete = false,
                CompletedAt = null
            },
            new KidTask
            {
                Title = "Homework",
                Points = 50,
                AssignedKidId = "kid-2",
                CreatedByParentId = parentId,
                IsComplete = false,
                CompletedAt = null
            }
        );
        db.SaveChanges();
    }




}
// -------------------------------------------------------------------------------


// Health
api.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .AllowAnonymous();

// -------------------- Auth (Option B) --------------------

// Parent login: returns Parent token
api.MapPost("/parent/login", async (AppDbContext db, IPasswordHasher<AppUser> hasher, ParentLoginRequest req) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username && u.Role == "Parent");
    if (user is null) return Results.Unauthorized();

    var verified = hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
    if (verified == PasswordVerificationResult.Failed) return Results.Unauthorized();

    var token = CreateToken(subjectId: user.Id, role: "Parent");
    return Results.Ok(new { token, role = "Parent" });
})
.AllowAnonymous();

// Parent enters Kid Mode: returns Kid token scoped to a kid profile
api.MapPost("/kid-session", async (ClaimsPrincipal principal, AppDbContext db, KidSessionRequest req) =>
{
    var parentId = GetUserId(principal);
    if (string.IsNullOrWhiteSpace(parentId)) return Results.Unauthorized();

    var kid = await db.Kids.FirstOrDefaultAsync(k => k.Id == req.KidId && k.ParentId == parentId);
    if (kid is null) return Results.NotFound("Kid not found for this parent.");

    var kidToken = CreateToken(subjectId: kid.Id, role: "Kid", kidId: kid.Id, parentId: parentId);
    return Results.Ok(new { token = kidToken, role = "Kid", kidId = kid.Id, displayName = kid.DisplayName });
})
.RequireAuthorization("ParentOnly");

// -------------------- Kids --------------------

// Parent sees only their kids
api.MapGet("/kids", async (ClaimsPrincipal principal, AppDbContext db) =>
{
    var parentId = GetUserId(principal);
    if (string.IsNullOrWhiteSpace(parentId)) return Results.Unauthorized();

    var kids = await db.Kids.Where(k => k.ParentId == parentId).ToListAsync();
    return Results.Ok(kids);
})
.RequireAuthorization("ParentOnly");

// Parent adds a kid
api.MapPost("/kids", async (ClaimsPrincipal principal, AppDbContext db, CreateKidRequest req) =>
{
    var parentId = GetUserId(principal);
    if (string.IsNullOrWhiteSpace(parentId)) return Results.Unauthorized();

    var name = (req.DisplayName ?? "").Trim();
    if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest("DisplayName is required.");

    var kid = new KidProfile
    {
        Id = Guid.NewGuid().ToString(),
        ParentId = parentId,
        DisplayName = name
    };

    db.Kids.Add(kid);
    await db.SaveChangesAsync();

    return Results.Created($"/api/kids/{kid.Id}", kid);
})
.RequireAuthorization("ParentOnly");

// Parent renames a kid
api.MapPut("/kids/{kidId}", async (ClaimsPrincipal principal, AppDbContext db, string kidId, UpdateKidRequest req) =>
{
    var parentId = GetUserId(principal);
    if (string.IsNullOrWhiteSpace(parentId)) return Results.Unauthorized();

    var name = (req.DisplayName ?? "").Trim();
    if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest("DisplayName is required.");

    var kid = await db.Kids.FirstOrDefaultAsync(k => k.Id == kidId && k.ParentId == parentId);
    if (kid is null) return Results.NotFound("Kid not found for this parent.");

    kid.DisplayName = name;
    await db.SaveChangesAsync();

    return Results.Ok(kid);
})
.RequireAuthorization("ParentOnly");

// -------------------- Tasks --------------------

// Parent can view tasks (optional kidId filter); Kid can view only their tasks
api.MapGet("/tasks", async (ClaimsPrincipal principal, AppDbContext db, string? kidId) =>
{
    var role = principal.FindFirstValue(ClaimTypes.Role);

    if (role == "Kid")
    {
        var kidClaim = principal.FindFirstValue("kidId") ?? GetUserId(principal);
        if (string.IsNullOrWhiteSpace(kidClaim)) return Results.Unauthorized();

        var tasks = await db.Tasks.Where(t => t.AssignedKidId == kidClaim).ToListAsync();
        return Results.Ok(tasks);
    }

    var parentId = GetUserId(principal);
    if (string.IsNullOrWhiteSpace(parentId)) return Results.Unauthorized();

    var q = db.Tasks.AsQueryable();

    if (!string.IsNullOrWhiteSpace(kidId))
    {
        var kidOwned = await db.Kids.AnyAsync(k => k.Id == kidId && k.ParentId == parentId);
        if (!kidOwned) return Results.BadRequest("Unknown kidId for this parent.");
        q = q.Where(t => t.AssignedKidId == kidId);
    }
    else
    {
        q = q.Where(t => t.CreatedByParentId == parentId);
    }

    return Results.Ok(await q.ToListAsync());
})
.RequireAuthorization();

// Parent creates tasks
api.MapPost("/tasks", async (ClaimsPrincipal principal, AppDbContext db, CreateTaskRequest req) =>
{
    var parentId = GetUserId(principal);
    if (string.IsNullOrWhiteSpace(parentId)) return Results.Unauthorized();

    var kidExists = await db.Kids.AnyAsync(k => k.Id == req.AssignedKidId && k.ParentId == parentId);
    if (!kidExists) return Results.BadRequest("Unknown kidId for this parent.");

    var task = new KidTask
    {
        Title = req.Title,
        Points = req.Points,
        AssignedKidId = req.AssignedKidId,
        CreatedByParentId = parentId,
        IsComplete = false,
        CompletedAt = null
    };

    db.Tasks.Add(task);
    await db.SaveChangesAsync();

    return Results.Created($"/api/tasks/{task.Id}", task);
})
.RequireAuthorization("ParentOnly");

// Kid completes tasks
api.MapPut("/tasks/{id:int}/complete", async (ClaimsPrincipal principal, AppDbContext db, int id) =>
{
    var kidId = principal.FindFirstValue("kidId") ?? GetUserId(principal);
    if (string.IsNullOrWhiteSpace(kidId)) return Results.Unauthorized();

    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.AssignedKidId == kidId);
    if (task is null) return Results.NotFound();

    if (task.IsComplete) return Results.Ok(task);

    task.IsComplete = true;
    task.CompletedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(task);
})
.RequireAuthorization("KidOnly");

// Parent deletes tasks they created
api.MapDelete("/tasks/{id:int}", async (ClaimsPrincipal principal, AppDbContext db, int id) =>
{
    var parentId = GetUserId(principal);
    if (string.IsNullOrWhiteSpace(parentId)) return Results.Unauthorized();

    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.CreatedByParentId == parentId);
    if (task is null) return Results.NotFound();

    db.Tasks.Remove(task);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization("ParentOnly");

// -------------------- Points --------------------

api.MapGet("/points", async (ClaimsPrincipal principal, AppDbContext db, string? kidId) =>
{
    var role = principal.FindFirstValue(ClaimTypes.Role);

    string effectiveKidId;
    if (role == "Kid")
    {
        effectiveKidId = principal.FindFirstValue("kidId") ?? GetUserId(principal) ?? "";
        if (string.IsNullOrWhiteSpace(effectiveKidId)) return Results.Unauthorized();
    }
    else
    {
        var parentId = GetUserId(principal);
        if (string.IsNullOrWhiteSpace(parentId)) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(kidId)) return Results.BadRequest("kidId is required for parent.");
        var kidOwned = await db.Kids.AnyAsync(k => k.Id == kidId && k.ParentId == parentId);
        if (!kidOwned) return Results.BadRequest("Unknown kidId for this parent.");

        effectiveKidId = kidId;
    }

    var earned = await db.Tasks
        .Where(t => t.AssignedKidId == effectiveKidId && t.IsComplete)
        .SumAsync(t => (int?)t.Points) ?? 0;

    var spent = await (from red in db.Redemptions
                       join rw in db.Rewards on red.RewardId equals rw.Id
                       where red.KidId == effectiveKidId
                       select (int?)rw.Cost).SumAsync() ?? 0;

    return Results.Ok(new { kidId = effectiveKidId, points = earned - spent });
})
.RequireAuthorization();

// -------------------- Rewards + Redemptions --------------------

// Everyone can view rewards
api.MapGet("/rewards", async (AppDbContext db) =>
    Results.Ok(await db.Rewards.ToListAsync())
)
.RequireAuthorization();

// Parent creates rewards
api.MapPost("/rewards", async (AppDbContext db, CreateRewardRequest req) =>
{
    var reward = new Reward { Name = req.Name, Cost = req.Cost };
    db.Rewards.Add(reward);
    await db.SaveChangesAsync();

    return Results.Created($"/api/rewards/{reward.Id}", reward);
})
.RequireAuthorization("ParentOnly");

// Kid redeems a reward
api.MapPost("/rewards/{rewardId:int}/redeem", async (ClaimsPrincipal principal, AppDbContext db, int rewardId) =>
{
    var kidId = principal.FindFirstValue("kidId") ?? GetUserId(principal);
    if (string.IsNullOrWhiteSpace(kidId)) return Results.Unauthorized();

    var reward = await db.Rewards.FirstOrDefaultAsync(r => r.Id == rewardId);
    if (reward is null) return Results.NotFound("Reward not found.");

    var earned = await db.Tasks
        .Where(t => t.AssignedKidId == kidId && t.IsComplete)
        .SumAsync(t => (int?)t.Points) ?? 0;

    var spent = await (from red in db.Redemptions
                       join rw in db.Rewards on red.RewardId equals rw.Id
                       where red.KidId == kidId
                       select (int?)rw.Cost).SumAsync() ?? 0;

    var currentPoints = earned - spent;
    if (currentPoints < reward.Cost) return Results.BadRequest("Not enough points.");

    var redemption = new Redemption
    {
        KidId = kidId,
        RewardId = rewardId,
        RedeemedAt = DateTime.UtcNow
    };

    db.Redemptions.Add(redemption);
    await db.SaveChangesAsync();

    return Results.Ok(new { kidId, newPoints = currentPoints - reward.Cost, redemption });
})
.RequireAuthorization("KidOnly");

// -------------------- Todos --------------------

api.MapGet("/todos", async (AppDbContext db) =>
    Results.Ok(await db.Todos.OrderBy(t => t.Id).ToListAsync())
)
.RequireAuthorization();

api.MapPost("/todos", async (AppDbContext db, TodoItem todo) =>
{
    todo.Id = 0;
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/api/todos/{todo.Id}", todo);
});

api.MapPut("/todos/{id:int}", async (AppDbContext db, int id, TodoItem updated) =>
{
    var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id);
    if (todo is null) return Results.NotFound();

    todo.Title = updated.Title;
    todo.IsDone = updated.IsDone;

    await db.SaveChangesAsync();
    return Results.Ok(todo);
});

api.MapDelete("/todos/{id:int}", async (AppDbContext db, int id) =>
{
    var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id);
    if (todo is null) return Results.NotFound();

    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// -------------------- ✅ SPA fallback (deep links) --------------------
app.MapFallbackToFile("index.html");

app.Run();
