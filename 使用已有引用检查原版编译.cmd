@echo off
chcp 65001 >nul
cd /d "%~dp0"
type docs\zh-CN\CLOSURE-ENTRY.txt
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Invoke-OriginalClosureCompile.ps1
if errorlevel 3 goto failed
if errorlevel 2 goto blocked
echo COMPILE_EMIT_SUCCEEDED codeExecuted=false
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 0
:blocked
echo COMPILE_BLOCKED summary=artifacts\cdr-082-closure-real\summary.json
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 2
:failed
echo FAILED
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 3
