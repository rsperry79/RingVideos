# Setup script for code coverage tools

Write-Host "Setting up code coverage tools..." -ForegroundColor Cyan

# Install ReportGenerator globally
Write-Host "`nInstalling ReportGenerator..." -ForegroundColor Yellow
dotnet tool install -g dotnet-reportgenerator-globaltool

if ($LASTEXITCODE -eq 0) {
    Write-Host "ReportGenerator installed successfully!" -ForegroundColor Green
}
else {
    Write-Host "ReportGenerator already installed or installation failed" -ForegroundColor Yellow
}

# Restore packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

Write-Host "`nSetup complete! You can now run coverage with:" -ForegroundColor Green
Write-Host "  API: .\external\RingApi\coverage.ps1" -ForegroundColor Cyan
Write-Host "  App: .\coverage.ps1" -ForegroundColor Cyan
