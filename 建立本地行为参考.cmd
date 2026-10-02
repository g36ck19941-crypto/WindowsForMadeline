@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Start-LocalBehaviorReference.ps1" -InstallRoot "%~1"
exit /b %ERRORLEVEL%
