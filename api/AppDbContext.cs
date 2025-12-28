using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<KidProfile> Kids => Set<KidProfile>();
    public DbSet<KidTask> Tasks => Set<KidTask>();
    public DbSet<Reward> Rewards => Set<Reward>();
    public DbSet<Redemption> Redemptions => Set<Redemption>();
    public DbSet<TodoItem> Todos => Set<TodoItem>();
    public DbSet<AppUser> Users => Set<AppUser>();
}
