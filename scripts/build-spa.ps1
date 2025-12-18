$ErrorActionPreference = "Stop"

Write-Host "Building web..." -ForegroundColor Cyan
Push-Location "web"
npm install
npm run build
Pop-Location

Write-Host "Copying web/dist -> api/wwwroot..." -ForegroundColor Cyan

# Ensure wwwroot exists
New-Item -ItemType Directory -Force -Path "api\wwwroot" | Out-Null

# Clear old files (avoid stale assets)
if (Test-Path "api\wwwroot") {
  Get-ChildItem "api\wwwroot" -Force | Remove-Item -Recurse -Force
}

# Copy dist contents
Copy-Item -Path "web\dist\*" -Destination "api\wwwroot" -Recurse -Force

Write-Host "Done. SPA is now in api/wwwroot." -ForegroundColor Green
