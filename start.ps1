# 一键启动（PowerShell）
# 用法: .\start.ps1
#       .\start.ps1 --check-now
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
& (Join-Path $PSScriptRoot 'start.bat') @args
