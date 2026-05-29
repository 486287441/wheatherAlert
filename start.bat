@echo off
chcp 65001 >nul
setlocal EnableExtensions

rem 切换到本脚本所在目录（仓库根目录）
cd /d "%~dp0"

set "APP_PROJ=src\WeatherAlert.TrayPopup.App\WeatherAlert.TrayPopup.App.csproj"
set "APP_EXE=src\WeatherAlert.TrayPopup.App\bin\Release\net10.0-windows10.0.19041.0\WeatherAlert.TrayPopup.App.exe"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [错误] 未找到 dotnet 命令。
    echo 请先安装 .NET 10 SDK: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

if not exist "%APP_PROJ%" (
    echo [错误] 找不到项目文件: %APP_PROJ%
    pause
    exit /b 1
)

if not exist "src\WeatherAlert.TrayPopup.App\appsettings.Local.json" (
    echo [提示] 未找到 appsettings.Local.json
    echo        请复制 appsettings.Local.json.example 并填入和风 API 密钥，详见 README.md
    echo.
)

if not exist "%APP_EXE%" (
    echo 首次运行，正在编译 Release 版本...
    dotnet build "%APP_PROJ%" -c Release -v minimal
    if errorlevel 1 (
        echo [错误] 编译失败。
        pause
        exit /b 1
    )
    echo.
)

if not exist "%APP_EXE%" (
    echo [错误] 找不到可执行文件: %APP_EXE%
    pause
    exit /b 1
)

for %%I in ("%APP_EXE%") do set "APP_DIR=%%~dpI"

echo 正在启动 WeatherAlert（托盘降雨提醒）...
rem 工作目录必须为 exe 所在目录，否则找不到 appsettings.json / data / logs
start "" /D "%APP_DIR%" "%APP_EXE%" %*
exit /b 0
