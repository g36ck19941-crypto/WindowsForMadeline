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
 echo 12. 运行 Seeker 的巡逻、发现、追逐、蓄力、冲刺、命中、撞墙眩晕和恢复轨迹。
 echo 13. 把以上模块交给 App 统一启动和逐 tick 调度，并演示暂停、恢复、停止和呈现故障隔离。
 echo 14. 用无界面宿主按固定 60 Hz 持续驱动 App；模拟电脑卡顿时限制补算并明确丢弃过期时间片。
 echo 15. 用程序生成提供器演示未来素材与地图接口，并验证读取/房间预算保护。
 echo 16. 在进度目录中说明 CDR-070 本地行为参考工具与产品运行时相互隔离。
 echo 17. 演示 CDR-071 从高速右移并上升的平台起跳，实际应用有界平台速度。
 echo 18. 演示 CDR-072 高速撞墙后在 4 tick 内保留并恢复横向速度。
 echo 19. 演示 CDR-073 向上擦到平台边角时，在 4 像素内精确横移并继续上升。
 echo 20. 演示 CDR-074 从下方穿过单向平台、从上方落地，以及明确向下穿透后在下一层重新落地。
 echo 21. 生成一份本地 HTML 报告，方便直接查看结果。
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
 echo  - CDR-047 的 30 个时间点，以及 Seeker 三次发现、两次冲刺、一次目标命中、一次撞墙、两次眩晕恢复和一次目标丢失。
 echo  - CDR-050 的 4 个 App 时间点：World 也恰好推进 4 次；效果被路由，暂停后可恢复，呈现故障后模拟仍继续并正常停止。
 echo  - CDR-051 正常节拍运行 6 tick、丢弃 0；积压保护只追赶 4 tick，并明确丢弃 4 个过期时间片，最终正常停止。
 echo  - CDR-060 的 4 字节程序素材、1 个世界/房间、2 个 Solid、1 个出生点和 3 个实体；两种超预算请求都被拒绝。
 echo  - CDR-071 的平台起跳继承速度为 (250,-130)，并出现一次 LiftVelocityApplied 诊断。
 echo  - CDR-072 先保留 90 横向速度和 4 tick 窗口，墙移开后恢复到 90，并各出现一次保留/恢复诊断。
 echo  - CDR-073 从 (0,4) 向上撞到边角后右移 1 像素到 (1,2)，出现一次 UpwardCornerCorrected，且上升速度没有被清零。
 echo  - CDR-074 能从下方穿过单向平台，从 y=0 向下穿透上层后在 y=19 落到下层，开始/完成/落地事件各 1 次，并能再次下穿。
echo  - 重复运行结果一致，并标明商业素材字节为 0。
echo.
echo  请注意：
echo  - 报告里的图片全部由程序生成，不是 Celeste 原版角色素材。
echo  - 这能证明离线模块已由 App 按顺序统一运行，并能被固定节拍宿主安全驱动；不能证明角色已显示在真实桌面。
echo  - CDR-060 只证明未来接口可安全交换有限数据，不代表已经能读取原版地图或 Mod。
echo  - CDR-070 只提供本机行为证据，不会自动把反编译内容变成产品代码，也不证明手感完全一致。
echo  - 这个入口不会启动 Celeste、Everest，也不会读取或写入游戏安装目录。
echo.

set "DOTNET_CLI_HOME=%CD%\artifacts\current-progress-runtime\dotnet-home"
set "APPDATA=%CD%\artifacts\current-progress-runtime\appdata"
set "NUGET_PACKAGES=%CD%\artifacts\current-progress-runtime\nuget-packages"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
set "DOTNET_NOLOGO=1"
set "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0"

dotnet restore "samples\CelesteDesktop.ProgressDemo\CelesteDesktop.ProgressDemo.csproj" --configfile "%CD%\NuGet.Offline.Config"
if errorlevel 1 goto demo_failed

dotnet run --project "samples\CelesteDesktop.ProgressDemo\CelesteDesktop.ProgressDemo.csproj" --configuration Release --no-restore -- --output "artifacts\current-progress-demo"
if errorlevel 1 (
    goto demo_failed
)

echo.
echo 演示报告已经生成。
if /i "%CDR_DEMO_NO_OPEN%"=="1" goto report_ready
start "" "%CD%\artifacts\current-progress-demo\index.html"
if errorlevel 1 (
    echo 无法自动打开报告，请手动打开下面的文件：
    echo %CD%\artifacts\current-progress-demo\index.html
)

:report_ready
echo.
echo 报告位置：%CD%\artifacts\current-progress-demo\index.html
echo 验收 CDR-074 时请查看 Player 说明中的单向平台结果；CDR-060 接口和 CDR-070 本地证据工具仍作为累计阶段保留，反编译内容本身不会进入报告。
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
