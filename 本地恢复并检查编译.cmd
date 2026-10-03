@echo off
chcp 65001 >nul
cd /d "%~dp0"
type docs\zh-CN\RECOVERY-ENTRY.txt
set "CDR_EXIT_CODE=0"
set "CDR_SELECTED_ROOT="
set /p "CDR_SELECTED_ROOT=> "
if not defined CDR_SELECTED_ROOT goto cancelled
set "CDR_SELECTED_ROOT=%CDR_SELECTED_ROOT:"=%"
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Invoke-OriginalCompileProbe.ps1 -InstallRoot "%CDR_SELECTED_ROOT%"
if errorlevel 3 goto failed
if errorlevel 2 goto blocked
echo COMPILE_CHECK_PASSED codeExecuted=false
goto done
:blocked
set "CDR_EXIT_CODE=2"
echo COMPILE_BLOCKED summary=artifacts\cdr-082-real\summary.json
goto done
:failed
set "CDR_EXIT_CODE=3"
echo FAILED
goto done
:cancelled
echo CANCELLED
:done
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b %CDR_EXIT_CODE%
