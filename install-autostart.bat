@echo off
chcp 65001 >nul
setlocal EnableExtensions
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\install-autostart.ps1"
if errorlevel 1 pause
exit /b %ERRORLEVEL%
