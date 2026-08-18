#Requires -Version 5.1

function Get-WeatherAlertRoot {
    Split-Path -Parent $PSScriptRoot
}

function Get-WeatherAlertExe {
    param([switch]$BuildIfMissing)

    $root = Get-WeatherAlertRoot
    $exePath = Join-Path $root 'src-tauri\target\release\weather-alert.exe'
    if (-not (Test-Path -LiteralPath $exePath) -and $BuildIfMissing) {
        Write-Host '未找到 Release 程序，正在构建 Tauri 安装包...'
        Push-Location $root
        try {
            npm run tauri:build
            if ($LASTEXITCODE -ne 0) { throw "构建失败：$LASTEXITCODE" }
        }
        finally { Pop-Location }
    }
    if (-not (Test-Path -LiteralPath $exePath)) { throw "找不到程序：$exePath" }
    Get-Item -LiteralPath $exePath
}

function Get-WeatherAlertStartupShortcut {
    Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Startup\WeatherAlert.lnk'
}

function New-WeatherAlertStartupShortcut {
    param([Parameter(Mandatory)][System.IO.FileInfo]$Exe)
    $shortcutPath = Get-WeatherAlertStartupShortcut
    $shell = New-Object -ComObject WScript.Shell
    $link = $shell.CreateShortcut($shortcutPath)
    $link.TargetPath = $Exe.FullName
    $link.Arguments = '--autostart'
    $link.WorkingDirectory = $Exe.DirectoryName
    $link.Description = 'WeatherAlert 降雨提醒'
    $link.IconLocation = $Exe.FullName
    $link.Save()
    $shortcutPath
}
