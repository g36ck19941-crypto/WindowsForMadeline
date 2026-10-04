@echo off
chcp 65001 >nul
cd /d "%~dp0"
type docs\zh-CN\RESTRICTED-PROCESS-ENTRY.txt
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Verify-RestrictedProcess.ps1
if errorlevel 3 goto failed
if errorlevel 2 goto partial
if errorlevel 1 goto failed
echo OWN_PROTOTYPE_VERIFIED fullSandboxEstablished=false
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 0
:partial
echo PARTIAL managedStartupBlocked=true originalCompatibilityEstablished=false
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 2
:failed
echo FAILED
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 1
