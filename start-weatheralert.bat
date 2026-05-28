@echo off
setlocal
set "ROOT=%~dp0"
cd /d "%ROOT%"

tasklist /FI "IMAGENAME eq WeatherAlert.App.exe" | find /I "WeatherAlert.App.exe" >nul
if %errorlevel%==0 (
  echo WeatherAlert is already running.
  exit /b 0
)

start "WeatherAlert" /min cmd /c dotnet run --project "%ROOT%src\WeatherAlert.App\WeatherAlert.App.csproj"
echo WeatherAlert started.
endlocal
