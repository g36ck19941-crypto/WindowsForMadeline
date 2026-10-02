[CmdletBinding()]
param([string]$InstallRoot = '')

$ErrorActionPreference = 'Stop'
$noPause = $env:CDR_NO_PAUSE -eq '1'
$exitCode = 0

try {
    Write-Host '============================================================'
    Write-Host ' CDR-070 本地行为参考建立器'
    Write-Host '============================================================'
    Write-Host ''
    Write-Host '它会只读分析你明确指定的正版 Celeste 安装中的 Celeste.dll'
    Write-Host '（没有 DLL 时才使用 Celeste.exe），把可读参考代码放进本项目的'
    Write-Host 'local-cache。这个目录已被 Git 忽略，不会上传到 GitHub。'
    Write-Host ''
    Write-Host '它不会启动 Celeste/Everest，不会打开游戏 GUI，不会写安装目录，'
    Write-Host '也不会把反编译源码接入产品编译。它只帮助我们核对状态顺序、'
    Write-Host '碰撞框、常量和交互逻辑；不能单独证明桌面版已经完全还原。'
    Write-Host ''

    if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
        $InstallRoot = Read-Host '请输入或拖入正版 Celeste 安装文件夹'
    }
    $InstallRoot = $InstallRoot.Trim().Trim('"')
    if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
        throw '未提供安装文件夹，未执行任何操作。'
    }

    $toolList = (& dotnet tool list --local 2>&1) -join "`n"
    $restoreAllowed = $false
    if ($LASTEXITCODE -ne 0 -or $toolList -notmatch '(?m)^ilspycmd\s') {
        Write-Host ''
        Write-Host '首次使用需要从官方 NuGet 源下载固定版本 ILSpy 11.1.0.9782。'
        Write-Host 'ILSpy 使用 MIT 许可证；工具包只进入本机缓存，不进入 Git。'
        $answer = Read-Host '是否允许本次下载？输入 Y 继续'
        if ($answer -notmatch '^[Yy]$') {
            throw '已取消；未下载工具，也未读取安装。'
        }
        $restoreAllowed = $true
    }

    $arguments = @{ InstallRoot = $InstallRoot }
    if ($restoreAllowed) { $arguments.AllowToolRestore = $true }
    & (Join-Path $PSScriptRoot 'Build-LocalBehaviorReference.ps1') @arguments
    if ($LASTEXITCODE -ne 0) { throw "CDR-070 本地行为参考建立失败：$LASTEXITCODE。" }

    Write-Host ''
    Write-Host '成功：本机参考已建立，且不会被 Git 跟踪。'
}
catch {
    $exitCode = 1
    Write-Host ''
    Write-Host '失败：' -ForegroundColor Red -NoNewline
    Write-Host $_.Exception.Message
    Write-Host '请保留上方第一个 LOCAL_REFERENCE_ 错误码和错误文字。'
}

if (-not $noPause) {
    Write-Host ''
    Write-Host '按任意键关闭窗口...'
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
}
exit $exitCode
