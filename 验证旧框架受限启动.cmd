@echo off
chcp 65001 >nul
cd /d "%~dp0"
type docs\zh-CN\RESTRICTED-FRAMEWORK-ENTRY.txt
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Verify-RestrictedFramework.ps1
set "probe_exit=%errorlevel%"
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b %probe_exit%
