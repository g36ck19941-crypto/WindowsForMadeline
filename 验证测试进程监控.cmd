@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"
type "docs\zh-CN\PROCESS-GUARD-ENTRY.txt"
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\Verify-ProcessGuard.ps1" -Demo
set "CDR_EXIT=%ERRORLEVEL%"
if not "%CDR_EXIT%"=="0" echo FAILED exitCode=%CDR_EXIT%
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b %CDR_EXIT%
