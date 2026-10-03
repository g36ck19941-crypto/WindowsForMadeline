@echo off
chcp 65001 >nul
cd /d "%~dp0"
type docs\zh-CN\PROGRESS-ENTRY.txt
set "DOTNET_CLI_HOME=%CD%\artifacts\cdr-080-verification\dotnet-home"
set "APPDATA=%CD%\artifacts\cdr-080-verification\appdata"
set "NUGET_PACKAGES=%CD%\artifacts\cdr-080-verification\nuget-packages"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"
dotnet restore samples\CelesteDesktop.ProgressDemo\CelesteDesktop.ProgressDemo.csproj --configfile NuGet.Offline.Config
if errorlevel 1 goto failed
dotnet run --project samples/CelesteDesktop.ProgressDemo -c Release --no-restore -- --output artifacts/current-progress-demo
if errorlevel 1 goto failed
if not "%CDR_DEMO_NO_OPEN%"=="1" start "" "artifacts\current-progress-demo\index.html"
echo REPORT_GENERATED runtimeIntegrated=false
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 0
:failed
echo FAILED
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 1
