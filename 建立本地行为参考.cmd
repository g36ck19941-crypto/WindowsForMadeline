@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"
echo ============================================================
echo  CDR-070 本地行为参考建立器
echo ============================================================
echo.
echo 它会只读分析你明确指定的正版 Celeste 安装中的 Celeste.dll
echo （没有 DLL 时才使用 Celeste.exe），把可读参考代码放进本项目的
echo local-cache。这个目录已被 Git 忽略，不会上传到 GitHub。
echo.
echo 它不会启动 Celeste/Everest，不会打开游戏 GUI，不会写安装目录，
echo 也不会把反编译源码接入产品编译。它只帮助我们核对状态顺序、
echo 碰撞框、常量和交互逻辑；不能单独证明桌面版已经完全还原。
echo.
set "INSTALL_ROOT=%~1"
if not defined INSTALL_ROOT set /p "INSTALL_ROOT=请输入或拖入正版 Celeste 安装文件夹："
set "INSTALL_ROOT=%INSTALL_ROOT:"=%"
if not defined INSTALL_ROOT goto :invalid
dotnet tool list --local 2>nul | findstr /i /c:"ilspycmd" >nul
if errorlevel 1 (
  echo.
  echo 首次使用需要按仓库清单下载固定版本 ILSpy 11.1.0.9782。
  echo ILSpy 使用 MIT 许可证；只从官方 NuGet 源下载到本机缓存，不进入 Git。
  set /p "ALLOW_RESTORE=是否允许本次下载？输入 Y 继续："
  if /i not "%ALLOW_RESTORE%"=="Y" goto :cancelled
  set "RESTORE_ARG=-AllowToolRestore"
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Build-LocalBehaviorReference.ps1" -InstallRoot "%INSTALL_ROOT%" %RESTORE_ARG%
if errorlevel 1 goto :failed
echo.
echo 成功：本机参考已建立，且不会被 Git 跟踪。
if /i "%CDR_NO_PAUSE%"=="1" exit /b 0
pause
exit /b 0
:invalid
echo 未提供安装文件夹，未执行任何操作。
if /i "%CDR_NO_PAUSE%"=="1" exit /b 2
pause
exit /b 2
:cancelled
echo 已取消；未下载工具，也未读取安装。
if /i "%CDR_NO_PAUSE%"=="1" exit /b 2
pause
exit /b 2
:failed
echo.
echo 失败：请保留上方第一个 LOCAL_REFERENCE_ 错误码和错误文字。
if /i "%CDR_NO_PAUSE%"=="1" exit /b 1
pause
exit /b 1
