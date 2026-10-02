[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$InstallRoot, [switch]$AllowToolRestore)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$cacheRoot = Join-Path $projectRoot 'local-cache'
$toolList = (& dotnet tool list --local 2>&1) -join "`n"
if ($LASTEXITCODE -ne 0 -or $toolList -notmatch '(?m)^ilspycmd\s') {
    if (-not $AllowToolRestore) { throw '固定版本 ILSpy 尚未恢复。请从根目录双击“建立本地行为参考.cmd”并明确同意首次下载。' }
    & dotnet tool restore --add-source 'https://api.nuget.org/v3/index.json'
    if ($LASTEXITCODE -ne 0) { throw "固定版本 ILSpy 恢复失败：$LASTEXITCODE。" }
}

& dotnet run --project (Join-Path $projectRoot 'tools\CelesteDesktop.LocalReference\CelesteDesktop.LocalReference.csproj') --configuration Release -- --root ([IO.Path]::GetFullPath($InstallRoot)) --cache-root ([IO.Path]::GetFullPath($cacheRoot))
if ($LASTEXITCODE -ne 0) { throw "CDR-070 本地行为参考建立失败：$LASTEXITCODE。" }
Write-Host ''
Write-Host '完成：反编译参考只保存在 local-cache，Git 会忽略它。'
Write-Host '它可用于人工核对行为逻辑，但不会自动改写产品代码，也不等于已经达到原版手感。'
