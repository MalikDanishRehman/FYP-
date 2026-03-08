@echo off
REM Run from repo root. Starts backend in a new window, then runs Blazor app.
set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"

echo [1/2] Starting HydroAI Backend in new window...
start "HydroAI Backend" cmd /k "cd /d "%ROOT%" && Start_Backend.bat"

echo Waiting for backend to start...
timeout /t 4 /nobreak >nul

echo [2/2] Starting Blazor app in this window...
echo When you see "Now listening on: http://localhost:5054", the app is ready.
echo Browser will open in ~15 seconds, or open http://localhost:5054 yourself.
echo.
start "OpenBrowser" cmd /c "timeout /t 15 /nobreak >nul && start http://localhost:5054"
cd /d "%ROOT%\AI_Driven_Water_Supply.Presentation"
dotnet run --launch-profile http
