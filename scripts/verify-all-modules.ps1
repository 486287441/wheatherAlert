#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet build --nologo -v q
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet test --nologo -v q --no-build
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
