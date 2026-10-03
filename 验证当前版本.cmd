@echo off
chcp 65001 >nul
cd /d "%~dp0"
type docs\zh-CN\VERIFICATION-ENTRY.txt
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Verify-CDR082.ps1
if errorlevel 1 goto failed
echo OFFLINE_VERIFIED windowProbesExecuted=0 originalRuntimePassNotClaimed=true
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 0
:failed
echo FAILED
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 1
