#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'autostart-common.ps1')

$exe = Get-WeatherAlertExe -BuildIfMissing
$shortcutPath = New-WeatherAlertStartupShortcut -Exe $exe
Write-Host "已启用 WeatherAlert 开机自启：$shortcutPath"
