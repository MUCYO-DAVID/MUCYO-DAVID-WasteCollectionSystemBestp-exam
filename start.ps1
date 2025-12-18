# Navigate to script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Stop any running instance
Get-Process -Name "WasteCollectionSystem" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# Set environment and run
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project WasteCollectionSystem.csproj --urls "http://localhost:5290"

