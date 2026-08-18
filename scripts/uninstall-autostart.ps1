#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'autostart-common.ps1')

$shortcutPath = Get-WeatherAlertStartupShortcut
if (Test-Path -LiteralPath $shortcutPath) {
    Remove-Item -LiteralPath $shortcutPath -Force
    Write-Host "已关闭 WeatherAlert 开机自启：$shortcutPath"
} else {
    Write-Host 'WeatherAlert 启动快捷方式不存在，无需移除。'
}
