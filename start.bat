@echo off
echo Stopping any running instances...
taskkill /F /IM WasteCollectionSystem.exe 2>nul
timeout /t 1 /nobreak >nul
echo Starting application...
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --project WasteCollectionSystem.csproj --urls "http://localhost:5290"
pause

