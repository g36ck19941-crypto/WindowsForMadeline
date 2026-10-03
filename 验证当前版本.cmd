@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo 本入口验证保留下来的资源、反编译安全、动画、渲染和桌面几何工具。
echo 不运行旧玩法，不启动游戏或可见 GUI，不访问安装目录。
echo 成功只代表工具测试通过，不代表原版角色已经重组完成。
echo 若失败，请提供最后的错误信息。
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Verify-CDR081.ps1
if errorlevel 1 goto failed
echo 成功：保留工具验证完成。后续仍需原版依赖和本地编译验证。
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 0
:failed
echo 失败：请提供最后的错误信息。
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 1
