#Requires -Version 5.1

function Get-WeatherAlertRoot {
    Split-Path -Parent $PSScriptRoot
}

function Get-WeatherAlertExe {
    param([switch]$BuildIfMissing)

    $root = Get-WeatherAlertRoot
    $proj = Join-Path $root 'src\WeatherAlert.TrayPopup.App\WeatherAlert.TrayPopup.App.csproj'
    $releaseDir = Join-Path $root 'src\WeatherAlert.TrayPopup.App\bin\Release'

    $findExe = {
        Get-ChildItem -Path $releaseDir -Recurse -Filter 'WeatherAlert.TrayPopup.App.exe' -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
    }

    $exe = & $findExe

    if (-not $exe -and $BuildIfMissing) {
        Write-Host '??? Release ??????????...'
        Push-Location $root
        try {
            dotnet build $proj -c Release -v minimal
            if ($LASTEXITCODE -ne 0) {
                throw "???????? $LASTEXITCODE"
            }
        }
        finally {
            Pop-Location
        }
        $exe = & $findExe
    }

    if (-not $exe) {
        throw '??? WeatherAlert.TrayPopup.App.exe????? start.bat???????????? install-autostart.ps1????????'
    }

    $exe
}

function Get-WeatherAlertStartupShortcut {
    Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Startup\WeatherAlert.lnk'
}

function New-WeatherAlertStartupShortcut {
    param(
        [Parameter(Mandatory)]
        [System.IO.FileInfo]$Exe
    )

    $shortcutPath = Get-WeatherAlertStartupShortcut
    $shell = New-Object -ComObject WScript.Shell
    $link = $shell.CreateShortcut($shortcutPath)
    $link.TargetPath = $Exe.FullName
    $link.WorkingDirectory = $Exe.DirectoryName
    $link.Description = 'WeatherAlert ??????'
    $link.Save()

    $shortcutPath
}
