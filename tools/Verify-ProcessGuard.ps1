param([switch]$Demo)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $repo 'artifacts/cdr-081-verification/dotnet-home'
$env:APPDATA=Join-Path $repo 'artifacts/cdr-081-verification/appdata'
$env:NUGET_PACKAGES=Join-Path $repo 'artifacts/cdr-081-verification/nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
Push-Location $repo
try {
    & dotnet restore tools/CelesteDesktop.ProcessGuard --configfile NuGet.Offline.Config
    if($LASTEXITCODE -ne 0){throw 'PROCESS_GUARD_RESTORE_FAILED'}
    & dotnet build tools/CelesteDesktop.ProcessGuard -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'PROCESS_GUARD_BUILD_FAILED'}
    $mode=if($Demo){'--demo'}else{'--verify'}
    $lines=@(& dotnet tools/CelesteDesktop.ProcessGuard/bin/Release/net8.0/CelesteDesktop.ProcessGuard.dll $mode)
    if($LASTEXITCODE -ne 0){throw 'PROCESS_GUARD_TEST_FAILED'}
    $events=@($lines | ForEach-Object {$_ | ConvertFrom-Json})
    $summary=@($events | Where-Object {$_.eventId -eq 'PROCESS_GUARD_VERIFIED'})
    if($summary.Count -ne 1 -or $summary[0].passed -ne 8 -or $summary[0].failed -ne 0 -or -not $summary[0].allOwnedChildrenExited -or $summary[0].originalCodeExecuted -or $summary[0].guiOpened -or $summary[0].processSandboxEstablished){throw 'PROCESS_GUARD_REPORT_INVALID'}
    $artifact=Join-Path $repo 'artifacts/cdr-082-process-guard'
    for($cursor=$artifact; $cursor; $cursor=[IO.Path]::GetDirectoryName($cursor)) {
        if((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'PROCESS_GUARD_OUTPUT_LINK'}
    }
    New-Item -ItemType Directory -Path $artifact -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $artifact 'summary.json'),($summary[0] | ConvertTo-Json))
    [IO.File]::WriteAllText((Join-Path $artifact 'cases.json'),($events | ConvertTo-Json -Depth 8))
    foreach($row in ($events | Where-Object {$_.eventId -eq 'PROCESS_GUARD_CASE'})) {
        Write-Output ('PROCESS_GUARD_CASE scenario='+$row.result.scenario+' outcome='+$row.result.outcome+' lastStage='+$row.result.lastStage+' exitCode='+$row.result.exitCode+' childExited='+$row.result.childExited)
    }
    Write-Output 'PROCESS_GUARD_VERIFIED passed=8 failed=0 allOwnedChildrenExited=true originalCodeExecuted=false processSandboxEstablished=false'
} finally {Pop-Location}
exit 0
