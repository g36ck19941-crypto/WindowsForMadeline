@echo off
setlocal
cd /d "%~dp0"

echo.
echo ========================================
echo  CelesteDesktopRuntime Verification
echo ========================================
echo  Release build and CDR-010 through CDR-016 offline regression checks.
echo  No real Celeste installation is accessed by this launcher.
echo.

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%CD%\tools\Verify-CDR016.ps1" -OfflineOnly
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
