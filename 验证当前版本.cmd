@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"

echo.
echo ================================================================
echo  CelesteDesktopRuntime 当前版本验证
echo ================================================================
echo.
echo  这个入口是用来回答：现有代码有没有按任务要求稳定工作？
echo.
echo  它会做这些事：
echo  1. 使用 Release 配置重新构建整个项目。
echo  2. 运行 CDR-010 到 CDR-040 的全部 402 项自动测试。
echo  3. 检查素材解析、确定性移动、动画呈现和 Theo 交互是否仍然正确。
echo  4. 核对累计演示的清单，避免只有测试通过却没有可观察结果。
echo.
echo  验证成功时，你应该看到：
echo  - 构建为 0 个警告、0 个错误。
echo  - 每组测试全部通过，总计 402/402。
echo  - 最后显示 CDR-040 OFFLINE VERIFICATION PASSED。
echo.
echo  成功表示代码和离线演示通过检查，但不表示：
echo  - 原版角色已经显示在真实桌面；
echo  - 已经完成真实 GUI、实时输入或人眼可见性验收；
echo  - 已经读取、复制或保存 Celeste 商业素材。
echo.
echo  本入口不会读取真实游戏安装或桌面内容，也不会打开可见 GUI。
echo.

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%CD%\tools\Verify-CDR040.ps1"
set "verify_exit=%ERRORLEVEL%"

echo.
if not "%verify_exit%"=="0" goto verify_failed

echo 验证通过。
echo 这说明当前离线代码、测试和累计演示彼此一致。
echo 下一步仍需按照任务书进行人工验收，不能把本结果当成桌面可见性证明。
goto verify_done

:verify_failed
echo 验证失败。
echo 请把窗口中第一个 FAILED 或 error 以及它前后的阶段名称发给开发代理。
echo 后面的失败有时只是前一个问题引起的，请优先保留第一个错误。

:verify_done

if /i "%CDR_NO_PAUSE%"=="1" exit /b %verify_exit%
pause
exit /b %verify_exit%
