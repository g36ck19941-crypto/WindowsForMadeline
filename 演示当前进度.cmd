@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"

echo.
echo ================================================================
echo  CelesteDesktopRuntime 当前项目进度演示
echo ================================================================
echo.
echo  这个入口是用来回答：项目目前实际上已经能做什么？
echo.
echo  它会做这些事：
echo  1. 生成安全的测试图片和精灵定义，不读取 Celeste 游戏文件。
echo  2. 让项目自己的解析器读取这些测试数据并建立素材目录。
echo  3. 运行角色移动、冲刺、攀爬和桌面几何等累计离线演示。
 echo  4. 按固定时间切换两张测试动画帧，再交给离线渲染流程。
 echo  5. 运行 Theo 的拿起、携带、投掷、碰撞反弹和落地轨迹。
 echo  6. 运行 Glider 的拿起、缓降请求、Player 实际应用、投掷、滑落、反弹和落地轨迹。
 echo  7. 运行 Spring 的激活、压缩、冷却、复位，以及 Player、Theo、Glider 实际受力轨迹。
 echo  8. 运行 Refill 的收集、Player 冲刺/体力恢复、冷却和重生轨迹。
 echo  9. 运行 Water 的进入、浸没、阻力、浮力、方向游动、限速、离开和 Player 实际应用轨迹。
 echo 10. 运行 Bumper 的圆形接触、径向弹飞、冷却、重新武装、中心回退和 Player 实际应用轨迹。
 echo 11. 运行 Puffer 的左右游动、接近预警、爆炸弹射、冷却重生、中心回退和 Player 实际应用轨迹。
 echo 12. 生成一份本地 HTML 报告，方便直接查看结果。
echo.
echo  你应该看到：
echo  - 各阶段完成了什么，以及每个阶段的输入和输出。
echo  - CDR-032 的 8 个时间点、8 次呈现和 3 次像素变化。
 echo  - CDR-040 的 36 个时间点，以及 Theo 的拿起、投掷、反弹和落地事件。
 echo  - CDR-041 的 48 个时间点，以及 Glider 的缓降请求、Player 应用、投掷、展开、反弹和落地事件。
 echo  - CDR-042 的 30 个时间点，以及 Spring 三次激活、三次复位和三个目标各一次实际速度变化。
 echo  - CDR-043 的 16 个时间点，以及 Refill 两次收集、两次 Player 资源恢复和两次重生。
 echo  - CDR-044 的 12 个时间点，以及 Water 进入、持续浸没、多目标、离开、再次进入和十次 Player 实际速度应用。
 echo  - CDR-045 的 15 个时间点，以及 Bumper 四次弹飞、三次冷却完成、中心回退、范围外忽略和四次 Player 实际应用。
 echo  - CDR-046 的 20 个时间点，以及 Puffer 游动/转向、三次预警/爆炸/弹射、两次重生、中心回退和三次 Player 实际应用。
echo  - 重复运行结果一致，并标明商业素材字节为 0。
echo.
echo  请注意：
echo  - 报告里的图片全部由程序生成，不是 Celeste 原版角色素材。
echo  - 这能证明离线动画管线已连接，不能证明角色已显示在真实桌面。
echo  - 这个入口不会启动 Celeste、Everest，也不会读取或写入游戏安装目录。
echo.

set "DOTNET_CLI_HOME=%CD%\artifacts\cdr-046-demo-runtime\dotnet-home"
set "APPDATA=%CD%\artifacts\cdr-046-demo-runtime\appdata"
set "NUGET_PACKAGES=%CD%\artifacts\cdr-046-demo-runtime\nuget-packages"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
set "DOTNET_NOLOGO=1"
set "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0"

dotnet restore "samples\CelesteDesktop.ProgressDemo\CelesteDesktop.ProgressDemo.csproj" --configfile "%CD%\NuGet.Config"
if errorlevel 1 goto demo_failed

dotnet run --project "samples\CelesteDesktop.ProgressDemo\CelesteDesktop.ProgressDemo.csproj" --configuration Release --no-restore -- --output "artifacts\cdr-046-demo"
if errorlevel 1 (
    goto demo_failed
)

echo.
echo 演示报告已经生成。
if /i "%CDR_DEMO_NO_OPEN%"=="1" goto report_ready
start "" "%CD%\artifacts\cdr-046-demo\index.html"
if errorlevel 1 (
    echo 无法自动打开报告，请手动打开下面的文件：
    echo %CD%\artifacts\cdr-046-demo\index.html
)

:report_ready
echo.
echo 报告位置：%CD%\artifacts\cdr-046-demo\index.html
echo 验收时请重点查看 CDR-046 Puffer 的游动/转向、预警、爆炸弹射、冷却重生、中心回退、Player 实际应用和页面底部限制说明。
echo.
if /i "%CDR_NO_PAUSE%"=="1" exit /b 0
pause
exit /b 0

:demo_failed
echo.
echo 演示生成失败。
echo 请把窗口中从第一个 FAILED 或 error 开始的内容发给开发代理，
echo 并保留它上方显示的阶段名称，方便判断问题出在解析、模拟还是呈现。
if /i "%CDR_NO_PAUSE%"=="1" exit /b 1
pause
exit /b 1
