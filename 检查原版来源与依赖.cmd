@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo 本入口只读检查你指定安装中的程序集身份、原版候选和所需依赖。
echo 它不反编译源码，不运行程序集，不启动 Celeste 或 Everest，不写安装目录。
echo 成功后可在“演示当前进度”里看摘要；成功不代表角色能运行或百分百原版。
echo 报告只记录版本、哈希、计数和依赖，不保存商业源码或素材。
echo 失败请提供最后的阶段、错误码和脱敏异常详情。
set "CDR_INVENTORY_ROOT="
set /p "CDR_INVENTORY_ROOT=请输入或拖入正版安装文件夹："
if not defined CDR_INVENTORY_ROOT goto cancelled
set "CDR_INVENTORY_ROOT=%CDR_INVENTORY_ROOT:"=%"
if not defined CDR_INVENTORY_ROOT goto cancelled
powershell -NoProfile -ExecutionPolicy Bypass -Command "& '.\tools\Invoke-AssemblyInventory.ps1' -InstallRoot $env:CDR_INVENTORY_ROOT"
if errorlevel 1 goto failed
echo 成功：只读检查报告已生成。现在可双击“演示当前进度”查看说明。
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 0
:cancelled
echo 已取消，没有读取安装。
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 2
:failed
echo 检查失败，请提供上方最后的阶段、错误码和脱敏异常详情。
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b 1
