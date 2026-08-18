#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw '未找到 npm，请先安装 Node.js 20 或更高版本。'
}

if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'node_modules'))) {
    npm install
    if ($LASTEXITCODE -ne 0) { throw "npm install 失败：$LASTEXITCODE" }
}

npm run tauri:dev
exit $LASTEXITCODE
