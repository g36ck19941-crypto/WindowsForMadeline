@echo off
chcp 65001 >nul
cd /d "%~dp0"
type docs\zh-CN\ISOLATION-FRAMEWORK-ENTRY.txt
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Compile-IsolationFramework.ps1
if errorlevel 1 goto failed
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 0
:failed
echo FAILED
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 1
