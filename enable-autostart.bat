@echo off
setlocal
set "ROOT=%~dp0"
set "RUN_KEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Run"
set "VALUE_NAME=WeatherAlert"

reg add "%RUN_KEY%" /v "%VALUE_NAME%" /t REG_SZ /d "\"%ROOT%start-weatheralert.bat\"" /f >nul
if %errorlevel% neq 0 (
  echo Failed to enable startup.
  exit /b 1
)

echo Startup enabled for current user.
endlocal
