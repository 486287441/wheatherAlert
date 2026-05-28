@echo off
setlocal

powershell -NoProfile -Command "Get-Process WeatherAlert.App -ErrorAction SilentlyContinue | Stop-Process -Force"
powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"name = 'dotnet.exe'\" | Where-Object { $_.CommandLine -like '*WeatherAlert.App.csproj*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }"

echo WeatherAlert stopped (if it was running).
endlocal
