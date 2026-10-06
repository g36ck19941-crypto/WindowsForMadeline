@echo off
chcp 65001 >nul
cd /d "%~dp0"
type docs\zh-CN\METHOD-DESIGN-ENTRY.txt
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Inspect-MethodDesign.ps1
set "method_exit=%errorlevel%"
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b %method_exit%
