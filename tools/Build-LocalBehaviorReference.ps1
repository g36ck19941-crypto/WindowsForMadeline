[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$InstallRoot, [switch]$AllowToolRestore)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$cacheRoot = Join-Path $projectRoot 'local-cache'
$toolList = (& dotnet tool list --local 2>&1) -join "`n"
if ($LASTEXITCODE -ne 0 -or $toolList -notmatch '(?m)^ilspycmd\s') {
    if (-not $AllowToolRestore) { throw 'Pinned ILSpy is unavailable. Use the repository CMD launcher and explicitly approve the first restore.' }
    & dotnet tool restore --add-source 'https://api.nuget.org/v3/index.json'
    if ($LASTEXITCODE -ne 0) { throw "Pinned ILSpy restore failed with exit code $LASTEXITCODE." }
}

& dotnet run --project (Join-Path $projectRoot 'tools\CelesteDesktop.LocalReference\CelesteDesktop.LocalReference.csproj') --configuration Release -- --root ([IO.Path]::GetFullPath($InstallRoot)) --cache-root ([IO.Path]::GetFullPath($cacheRoot))
if ($LASTEXITCODE -ne 0) { throw "CDR-070 local reference build failed with exit code $LASTEXITCODE." }
Write-Host ''
Write-Host 'Completed: the decompiled reference remains only in Git-ignored local-cache.'
Write-Host 'It supports behavior review but does not rewrite product code or establish exact feel parity.'
