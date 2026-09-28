@echo off
setlocal
cd /d "%~dp0"

echo.
echo ========================================
echo  CDR-016 Read-only Celeste Verification
echo ========================================
echo  This does not launch Celeste or a GUI.
echo  It writes evidence only under this project.
echo.

set "install_root=%~1"
if not defined install_root (
    set /p "install_root=Drag the Celeste installation folder here, or type its path: "
)
set "install_root=%install_root:"=%"
if not defined install_root (
    echo No installation folder was supplied.
    set "verify_exit=2"
    goto finished
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%CD%\tools\Verify-CDR016.ps1" -InstallRoot "%install_root%"
set "verify_exit=%ERRORLEVEL%"

:finished
echo.
if "%verify_exit%"=="0" (
    echo CDR-016 REAL CONFORMANCE PASSED
) else (
    echo CDR-016 REAL CONFORMANCE FAILED. Please send the error above to the development agent.
)

if /i "%CDR_NO_PAUSE%"=="1" exit /b %verify_exit%
pause
exit /b %verify_exit%
