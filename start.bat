@echo off
chcp 65001 >nul
setlocal EnableExtensions
cd /d "%~dp0"

where npm >nul 2>&1
if errorlevel 1 (
  echo [错误] 未找到 npm，请先安装 Node.js 20 或更高版本。
  pause
  exit /b 1
)

if not exist "node_modules" (
  echo 首次运行，正在安装前端依赖...
  call npm install
  if errorlevel 1 exit /b %ERRORLEVEL%
)

call npm run tauri:dev
exit /b %ERRORLEVEL%
