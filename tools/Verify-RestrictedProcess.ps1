$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$artifact = Join-Path $repo 'artifacts/cdr-082-restricted-process'
function Reject-Links([string]$value) {
    for($cursor = [IO.Path]::GetFullPath($value); $cursor; $cursor = [IO.Path]::GetDirectoryName($cursor)) {
        if((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'RESTRICTED_OUTPUT_LINK' }
    }
}
Reject-Links $artifact
$env:DOTNET_CLI_HOME = Join-Path $repo 'artifacts/cdr-081-verification/dotnet-home'
$env:APPDATA = Join-Path $repo 'artifacts/cdr-081-verification/appdata'
$env:NUGET_PACKAGES = Join-Path $repo 'artifacts/cdr-081-verification/nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
foreach($hook in @('DOTNET_STARTUP_HOOKS','DOTNET_ADDITIONAL_DEPS','DOTNET_SHARED_STORE','CORECLR_PROFILER','CORECLR_PROFILER_PATH','COR_PROFILER','COR_PROFILER_PATH')) {
    [Environment]::SetEnvironmentVariable($hook,$null,'Process')
}
$env:CORECLR_ENABLE_PROFILING = '0'; $env:COR_ENABLE_PROFILING = '0'; $env:DOTNET_EnableDiagnostics = '0'
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Preview\VC\Tools\MSVC\14.39.33218\bin\Hostx64\x64\cl.exe'
$setup = 'C:\Program Files\Microsoft Visual Studio\2022\Preview\VC\Auxiliary\Build\vcvars64.bat'
$dumpbin = 'C:\Program Files\Microsoft Visual Studio\2022\Preview\VC\Tools\MSVC\14.39.33218\bin\Hostx64\x64\dumpbin.exe'
foreach($tool in @($compiler,$setup,$dumpbin)) { if(-not (Test-Path -LiteralPath $tool -PathType Leaf)){throw 'EXISTING_MSVC_TOOL_MISSING_NO_DOWNLOAD'} }
Push-Location $repo
try {
    New-Item -ItemType Directory -Path $artifact -Force | Out-Null
    foreach($outputName in @('native-child.exe','native-child.obj','native-child.exe.sha256','summary.json','cases.json','managed-startup.json','imports.txt')) { Reject-Links (Join-Path $artifact $outputName) }
    $buildCommand = '"' + $setup + '" -vcvars_ver=14.39 >nul && "' + $compiler + '" /nologo /W4 /WX /GS- /Zl /Od /Foartifacts\cdr-082-restricted-process\native-child.obj tools\CelesteDesktop.RestrictedProcessProbe\NativeChild.c /link /nodefaultlib /entry:entry /subsystem:console /out:artifacts\cdr-082-restricted-process\native-child.exe kernel32.lib'
    & cmd.exe /d /c $buildCommand
    if($LASTEXITCODE -ne 0){throw 'OWN_NATIVE_BUILD_FAILED'}
    $nativeFile = Join-Path $artifact 'native-child.exe'
    $imports = @(& $dumpbin /imports $nativeFile)
    if($LASTEXITCODE -ne 0){throw 'OWN_NATIVE_IMPORT_AUDIT_FAILED'}
    [IO.File]::WriteAllText((Join-Path $artifact 'imports.txt'),($imports -join "`r`n"))
    $modules = @($imports | ForEach-Object { if($_ -match '^\s+([A-Za-z0-9_.-]+\.dll)\s*$') { $matches[1] } })
    if($modules.Count -ne 1 -or $modules[0] -ine 'KERNEL32.dll'){throw 'OWN_NATIVE_UNAPPROVED_IMPORT_MODULE'}
    $permitted = @('GetCurrentProcess','GetProcessMitigationPolicy','IsProcessInJob','GetCommandLineW','GetCurrentDirectoryW',
        'CreateFileW','WriteFile','CloseHandle','MoveFileExW','Sleep','ExitProcess')
    $importNames = @($imports | ForEach-Object { if($_ -match '^\s+[0-9A-Fa-f]+\s+([A-Za-z][A-Za-z0-9_]+)\s*$') { $matches[1] } })
    if($importNames.Count -ne $permitted.Count -or @($importNames | Where-Object {$_ -notin $permitted}).Count -gt 0){throw 'OWN_NATIVE_UNAPPROVED_API'}
    $hashStream = [IO.File]::OpenRead($nativeFile); $hasher = [Security.Cryptography.SHA256]::Create()
    try { $nativeHash = [BitConverter]::ToString($hasher.ComputeHash($hashStream)).Replace('-','') }
    finally { $hasher.Dispose(); $hashStream.Dispose() }
    [IO.File]::WriteAllText($nativeFile+'.sha256',$nativeHash)
    & dotnet restore tools/CelesteDesktop.RestrictedProcessProbe --configfile NuGet.Offline.Config
    if($LASTEXITCODE -ne 0){throw 'OWN_PARENT_OFFLINE_RESTORE_FAILED'}
    & dotnet build tools/CelesteDesktop.RestrictedProcessProbe -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'OWN_PARENT_BUILD_FAILED'}
    $dll = 'tools/CelesteDesktop.RestrictedProcessProbe/bin/Release/net8.0/CelesteDesktop.RestrictedProcessProbe.dll'
    $nativeLines = @(& dotnet $dll --verify-native-controls)
    if($LASTEXITCODE -ne 0){throw 'NATIVE_CONTROL_CASES_FAILED'}
    $nativeEvents = @($nativeLines | ForEach-Object { $_ | ConvertFrom-Json })
    $verified = @($nativeEvents | Where-Object {$_.eventId -eq 'RESTRICTED_PROCESS_VERIFIED'})
    $cases = @($nativeEvents | Where-Object {$_.eventId -eq 'RESTRICTED_PROCESS_CASE'})
    if($verified.Count -ne 1 -or $verified[0].passed -ne 6 -or $cases.Count -ne 6 -or -not $verified[0].allOwnedChildrenExited -or $verified[0].fullSandboxEstablished -or $verified[0].childKind -ne 'own-kernel32-native'){throw 'NATIVE_CONTROL_SUMMARY_INVALID'}
    foreach($case in $cases) { if(-not $case.result.preResumeGuiDenied -or -not $case.result.jobAssignedBeforeResume -or -not $case.result.exited){throw 'NATIVE_CONTROL_CASE_INVALID'} }
    $savedErrorPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue' # Expected nonzero startup outcome is evidence, not a pass.
        $managedLines = @(& dotnet $dll --verify 2>&1 | ForEach-Object { $_.ToString() })
        $managedExit = $LASTEXITCODE
    } finally { $ErrorActionPreference = $savedErrorPreference }
    $managedEvents = @($managedLines | ForEach-Object { $_ | ConvertFrom-Json })
    $managedPassed = @($managedEvents | Where-Object {$_.eventId -eq 'RESTRICTED_PROCESS_VERIFIED'})
    $managedFailure = @($managedEvents | Where-Object {$_.eventId -eq 'RESTRICTED_PROCESS_FAILED'})
    $startup = 'unknown'; $failedCode = $null; $failedSubcode = $null
    if($managedExit -eq 0 -and $managedPassed.Count -eq 1 -and $managedPassed[0].passed -eq 6) { $startup = 'own-net8-verified' }
    elseif($managedExit -eq 2 -and $managedFailure.Count -eq 1 -and $managedFailure[0].result.scenario -eq 'Complete' -and
        $managedFailure[0].result.exited -and -not $managedFailure[0].result.childReportObserved) {
        $startup = 'own-net8-startup-blocked'; $failedCode = $managedFailure[0].result.nativeExceptionCode; $failedSubcode = $managedFailure[0].result.nativeExceptionParameter0
    } else { throw 'MANAGED_PROBE_UNEXPECTED_OUTCOME' }
    $summary = [ordered]@{schemaVersion=1;taskId='CDR-082';stage='own-restricted-process-prototype';inspectedUtc=[DateTime]::UtcNow.ToString('o')
        status=if($startup -eq 'own-net8-verified'){'own-controls-verified'}else{'partial-managed-startup-blocked'}
        nativeControlPassed=6;managedStartup=$startup;managedProbeExitCode=$managedExit;nativeExceptionCode=$failedCode;nativeExceptionParameter0=$failedSubcode
        nativeExceptionImage=if($managedFailure.Count -eq 1){$managedFailure[0].result.exceptionImage}else{$null}
        nativeGuardTargetImage=if($managedFailure.Count -eq 1){$managedFailure[0].result.guardTargetImage}else{$null}
        nativeProbeHash=$nativeHash;nativeImportModules=1;nativeImportMethods=$importNames.Count;allObservedOwnedChildrenExited=$true
        guiPolicyQueriedBeforeResume=$true;commitMemoryLimitBytes=536870912;cpuHardCapPercent=20;activeProcessLimit=1;observationTimeoutMs=3000
        memoryCpuStressTested=$false;guiCreationProbeAttempted=$false;fullSandboxEstablished=$false;appContainerEstablished=$false
        originalLoaded=$false;xnaLoaded=$false;steamLoaded=$false;realInputUsed=$false;audioUsed=$false;networkUsed=$false;guiOpened=$false
        aclChanged=$false;systemConfigurationChanged=$false;gameDirectoryAccessed=$false;originalCompatibilityEstablished=$false}
    [IO.File]::WriteAllText((Join-Path $artifact 'summary.json'),($summary | ConvertTo-Json -Depth 6))
    [IO.File]::WriteAllText((Join-Path $artifact 'cases.json'),($nativeEvents | ConvertTo-Json -Depth 8))
    [IO.File]::WriteAllText((Join-Path $artifact 'managed-startup.json'),($managedEvents | ConvertTo-Json -Depth 8))
    foreach($case in $cases) { Write-Output ('RESTRICTED_PROCESS_CASE scenario='+$case.result.scenario+' lastStage='+$case.result.lastStage+' exitCode='+$case.result.exitCode+' cleanup='+$case.result.cleanupReason+' exited='+$case.result.exited) }
    Write-Output ('RESTRICTED_PROCESS_SUMMARY nativeControlsPassed=6 managedStartup='+$startup+' nativeException='+$failedCode+' parameter0='+$failedSubcode+' fullSandboxEstablished=false')
} finally { Pop-Location }
if($startup -ne 'own-net8-verified'){exit 2}
exit 0
