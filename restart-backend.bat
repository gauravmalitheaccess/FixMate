@echo off
echo Stopping existing dotnet processes...
taskkill /f /im dotnet.exe 2>nul
timeout /t 2 /nobreak >nul

echo Starting backend on port 5160...
cd ErrorLogPrioritization.Api
start "Backend" dotnet run

echo Backend should be starting on http://localhost:5160
pause