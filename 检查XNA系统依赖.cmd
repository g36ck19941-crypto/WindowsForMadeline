@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"
type "docs\zh-CN\SYSTEM-DEPENDENCY-ENTRY.txt"
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\Verify-XnaPreflight.ps1" -AuditSystemDependencies
set "CDR_EXIT=%ERRORLEVEL%"
if not "%CDR_EXIT%"=="0" echo FAILED exitCode=%CDR_EXIT%
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b %CDR_EXIT%
