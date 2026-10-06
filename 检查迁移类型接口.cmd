@echo off
chcp 65001 >nul
cd /d "%~dp0"
type docs\zh-CN\TYPE-CONTRACTS-ENTRY.txt
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Inspect-TypeContracts.ps1
set "type_exit=%errorlevel%"
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b %type_exit%
