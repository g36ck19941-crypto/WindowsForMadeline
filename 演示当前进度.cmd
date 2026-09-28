@echo off
setlocal
cd /d "%~dp0"

echo.
echo ========================================
echo  CelesteDesktopRuntime Progress Demo
echo ========================================
echo  Generated diagnostic data only. No game files are read.
echo.

set "DOTNET_CLI_HOME=%CD%\artifacts\cdr-021-demo-runtime\dotnet-home"
set "APPDATA=%CD%\artifacts\cdr-021-demo-runtime\appdata"
set "NUGET_PACKAGES=%CD%\artifacts\cdr-021-demo-runtime\nuget-packages"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
set "DOTNET_NOLOGO=1"

dotnet restore "samples\CelesteDesktop.ProgressDemo\CelesteDesktop.ProgressDemo.csproj" --configfile "%CD%\NuGet.Config"
if errorlevel 1 goto demo_failed

dotnet run --project "samples\CelesteDesktop.ProgressDemo\CelesteDesktop.ProgressDemo.csproj" --configuration Release --no-restore -- --output "artifacts\cdr-021-demo"
if errorlevel 1 (
    goto demo_failed
)

echo.
echo Demo report generated.
if /i "%CDR_DEMO_NO_OPEN%"=="1" goto report_ready
start "" "%CD%\artifacts\cdr-021-demo\index.html"
if errorlevel 1 (
    echo Automatic open failed. Open this file manually:
    echo %CD%\artifacts\cdr-021-demo\index.html
)

:report_ready
echo Report: %CD%\artifacts\cdr-021-demo\index.html
echo.
if /i "%CDR_NO_PAUSE%"=="1" exit /b 0
pause
exit /b 0

:demo_failed
echo.
echo Demo generation failed. Please send the error above to the development agent.
if /i "%CDR_NO_PAUSE%"=="1" exit /b 1
pause
exit /b 1
