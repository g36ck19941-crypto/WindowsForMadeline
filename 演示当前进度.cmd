@echo off
chcp 65001 >nul
cd /d "%~dp0"
set "DOTNET_CLI_HOME=%CD%\artifacts\cdr-080-verification\dotnet-home"
set "APPDATA=%CD%\artifacts\cdr-080-verification\appdata"
set "NUGET_PACKAGES=%CD%\artifacts\cdr-080-verification\nuget-packages"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"
echo 当前进度：旧玩法已删除，正在准备原版逻辑的本地重组。
echo 本入口生成中文状态页面，说明已有工具、缓存和缺少的功能。
echo 它不是角色演示；不会启动游戏或读取安装目录，不会导出商业源码和图片。
echo 若失败，请提供窗口最后的错误内容。
dotnet restore samples\CelesteDesktop.ProgressDemo\CelesteDesktop.ProgressDemo.csproj --configfile NuGet.Offline.Config
if errorlevel 1 goto failed
dotnet run --project samples/CelesteDesktop.ProgressDemo -c Release --no-restore -- --output artifacts/current-progress-demo
if errorlevel 1 goto failed
if not "%CDR_DEMO_NO_OPEN%"=="1" start "" "artifacts\current-progress-demo\index.html"
echo 成功：已生成状态页面。成功不代表角色已经可以运行。
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 0
:failed
echo 失败：请提供最后的错误信息。
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 1
