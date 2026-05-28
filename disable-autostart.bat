@echo off
setlocal
set "RUN_KEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Run"
set "VALUE_NAME=WeatherAlert"

reg delete "%RUN_KEY%" /v "%VALUE_NAME%" /f >nul
if %errorlevel% neq 0 (
  echo Startup entry not found or remove failed.
  exit /b 1
)

echo Startup disabled for current user.
endlocal
