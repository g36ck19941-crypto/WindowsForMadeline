@echo off
setlocal
cd /d "%~dp0"

echo.
echo ========================================
echo  CelesteDesktopRuntime Verification
echo ========================================
echo  Release build and CDR-010 through CDR-032 regression checks.
echo  No real Celeste installation or desktop content is accessed by this launcher.
echo  CDR-032 resolves generated validated-catalog frames by fixed tick and presents offline.
echo  No visible GUI is opened.
echo.

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%CD%\tools\Verify-CDR032.ps1"
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
