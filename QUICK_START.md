# Quick Start Guide

## 🚀 Start the Project

### Option 1: Using Helper Script (Easiest)
```powershell
.\start.ps1
```

### Option 2: Manual Command
```powershell
# Make sure you're in the project root directory
cd "C:\Users\HP 840\Desktop\Learning\Best Programming Practices\WasteCollectionSystem(1)\WasteCollectionSystem\WasteCollectionSystem\WasteCollectionSystem"

# Stop any running instance
Get-Process -Name "WasteCollectionSystem" -ErrorAction SilentlyContinue | Stop-Process -Force

# Start the application
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project WasteCollectionSystem.csproj --urls "http://localhost:5290"
```

## 🐳 Start with Docker

```bash
# Build and run
docker-compose up --build

# Access at: http://localhost:8080
```

## ✅ Verify Everything Works

1. **Application starts**: Check terminal for "Application started"
2. **Access homepage**: Open http://localhost:5290 in browser
3. **Docker works**: Run `docker-compose up` and access http://localhost:8080
4. **Tests pass**: Run `dotnet test tests/WasteCollectionSystem.Tests/`

## 📋 For Examiner - Show These

1. **Code**: Open any file in `Services/` or `Controllers/`
2. **Design Pattern**: Show `DESIGN_PATTERN.md` and `Services/IMomoPaymentService.cs`
3. **Docker**: Run `docker-compose up` and show container running
4. **Tests**: Run `dotnet test` and show results
5. **Version Control**: Show GitHub repository (after setup)
6. **Diagrams**: Open `diagrams.drawio` in draw.io

