@echo off
setlocal
cd /d "%~dp0"

echo.
echo ========================================
echo  CelesteDesktopRuntime Verification
echo ========================================
echo  Release build and CDR-010 through CDR-030 regression checks.
echo  No real Celeste installation is accessed by this launcher.
echo  CDR-030 uses a hidden native DirectComposition smoke test; no visible GUI is opened.
echo.

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%CD%\tools\Verify-CDR030.ps1"
set "verify_exit=%ERRORLEVEL%"

echo.
if "%verify_exit%"=="0" (
    echo VERIFICATION PASSED
) else (
    echo VERIFICATION FAILED. Please send the error above to the development agent.
)

if /i "%CDR_NO_PAUSE%"=="1" exit /b %verify_exit%
pause
exit /b %verify_exit%
