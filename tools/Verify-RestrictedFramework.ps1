$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$artifact=Join-Path $repo 'artifacts/cdr-082-restricted-process'
function NoLinks([string]$value){for($cursor=[IO.Path]::GetFullPath($value);$cursor;$cursor=[IO.Path]::GetDirectoryName($cursor)){
    if((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'OWN_FRAMEWORK_LINK'}}}
function Hash([string]$value){$stream=[IO.File]::OpenRead($value);$hash=[Security.Cryptography.SHA256]::Create();try{return [BitConverter]::ToString($hash.ComputeHash($stream)).Replace('-','')}finally{$stream.Dispose();$hash.Dispose()}}
$env:DOTNET_CLI_HOME=Join-Path $repo 'artifacts/cdr-081-verification/dotnet-home'
$env:APPDATA=Join-Path $repo 'artifacts/cdr-081-verification/appdata'
$env:NUGET_PACKAGES=Join-Path $repo 'artifacts/cdr-081-verification/nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_NOLOGO='1';$env:DOTNET_EnableDiagnostics='0'
foreach($hook in @('DOTNET_STARTUP_HOOKS','DOTNET_ADDITIONAL_DEPS','DOTNET_SHARED_STORE','CORECLR_PROFILER','CORECLR_PROFILER_PATH','COR_PROFILER','COR_PROFILER_PATH')){[Environment]::SetEnvironmentVariable($hook,$null,'Process')}
$env:CORECLR_ENABLE_PROFILING='0';$env:COR_ENABLE_PROFILING='0'
Push-Location $repo
try{
    NoLinks $artifact;New-Item -ItemType Directory -Path $artifact -Force|Out-Null
    $search=Get-Content artifacts/cdr-082-reference-search/summary.json -Raw|ConvertFrom-Json
    $row=@($search.referenceRows|Where-Object{$_.scope -eq 'reference-framework-v4.7.2' -and $_.name -eq 'mscorlib' -and $_.exactOriginalReferenceMatch})
    if($row.Count -ne 1){throw 'OWN_FRAMEWORK_REFERENCE_EVIDENCE'}
    $reference=Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7.2/mscorlib.dll'
    NoLinks $reference;if((Hash $reference) -ne $row[0].sha256){throw 'OWN_FRAMEWORK_REFERENCE_CHANGED'}
    $source='tools/CelesteDesktop.RestrictedProcessProbe/FrameworkChild.cs';NoLinks $source;$sourceHash=Hash $source
    $sdk=(& dotnet --version).Trim();if($LASTEXITCODE -ne 0 -or $sdk -notmatch '^\d+\.\d+\.\d+$'){throw 'OWN_COMPILER_VERSION'}
    $root=Split-Path -Parent (Get-Command dotnet -CommandType Application).Source
    $compiler=Join-Path $root ('sdk/'+$sdk+'/Roslyn/bincore/csc.dll');NoLinks $compiler
    $output=Join-Path $artifact 'framework-child.exe'
    foreach($file in @($output,$output+'.sha256',(Join-Path $artifact 'framework-summary.json'),(Join-Path $artifact 'framework-cases.json'))){NoLinks $file}
    & dotnet $compiler /nologo /target:exe /platform:x86 /nostdlib+ /deterministic+ /optimize+ /warnaserror+ /nullable:disable ('/reference:'+$reference) ('/out:'+$output) $source
    if($LASTEXITCODE -ne 0){throw 'OWN_FRAMEWORK_COMPILE_FAILED'}
    if((Hash $reference) -ne $row[0].sha256 -or (Hash $source) -ne $sourceHash){throw 'OWN_FRAMEWORK_INPUT_CHANGED'}
    $outputHash=Hash $output;[IO.File]::WriteAllText($output+'.sha256',$outputHash)
    & dotnet restore tools/CelesteDesktop.RestrictedProcessProbe --configfile NuGet.Offline.Config
    if($LASTEXITCODE -ne 0){throw 'OWN_PARENT_RESTORE_FAILED'}
    & dotnet build tools/CelesteDesktop.RestrictedProcessProbe -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'OWN_PARENT_BUILD_FAILED'}
    $dll='tools/CelesteDesktop.RestrictedProcessProbe/bin/Release/net8.0/CelesteDesktop.RestrictedProcessProbe.dll'
    $audit=@(& dotnet $dll --audit-framework-child);if($LASTEXITCODE -ne 0){throw 'OWN_FRAMEWORK_METADATA_FAILED'}
    $metadata=$audit[0]|ConvertFrom-Json
    if($metadata.eventId -ne 'OWN_FRAMEWORK_METADATA_VERIFIED' -or $metadata.executed -or $metadata.references -ne 1 -or $metadata.imports -ne 3){throw 'OWN_FRAMEWORK_METADATA_PROTOCOL'}
    $saved=$ErrorActionPreference
    try{$ErrorActionPreference='Continue';$lines=@(& dotnet $dll --verify-framework-controls 2>&1|ForEach-Object{$_.ToString()});$exitCode=$LASTEXITCODE}finally{$ErrorActionPreference=$saved}
    $events=@($lines|ForEach-Object{$_|ConvertFrom-Json});$cases=@($events|Where-Object{$_.eventId -eq 'RESTRICTED_PROCESS_CASE'})
    $verified=@($events|Where-Object{$_.eventId -eq 'RESTRICTED_PROCESS_VERIFIED'});$failed=@($events|Where-Object{$_.eventId -eq 'RESTRICTED_PROCESS_FAILED'})
    $status='unknown';$failedResult=$null
    if($exitCode -eq 0 -and $verified.Count -eq 1 -and $verified[0].passed -eq 6 -and $verified[0].childKind -eq 'own-net472-x86'){$status='own-net472-controls-verified'}
    elseif($exitCode -eq 2 -and $failed.Count -eq 1 -and $failed[0].result.exited -and $failed[0].result.preResumeGuiDenied -and $failed[0].result.jobAssignedBeforeResume){$status='partial-own-net472-blocked';$failedResult=$failed[0].result}
    else{throw 'OWN_FRAMEWORK_UNEXPECTED_OUTCOME'}
    foreach($case in $cases){if(-not $case.result.exited -or -not $case.result.preResumeGuiDenied -or -not $case.result.jobAssignedBeforeResume){throw 'OWN_FRAMEWORK_CASE_PROTOCOL'}}
    if((Hash $output) -ne $outputHash){throw 'OWN_FRAMEWORK_OUTPUT_CHANGED'}
    $summary=[ordered]@{schemaVersion=1;taskId='CDR-082';stage='own-restricted-framework-prototype';inspectedUtc=[DateTime]::UtcNow.ToString('o');status=$status
        framework='net472';architecture='x86';passed=$cases.Count;probeExitCode=$exitCode;metadataReferences=1;metadataImports=3;outputHash=$outputHash
        failedScenario=if($failedResult){$failedResult.scenario}else{$null};lastOwnPhase=if($failedResult){$failedResult.lastOwnPhase}else{'job-query-verified'}
        nativeExceptionCode=if($failedResult){$failedResult.nativeExceptionCode}else{$null};nativeExceptionParameter0=if($failedResult){$failedResult.nativeExceptionParameter0}else{$null}
        childExitCode=if($failedResult){('{0:X8}' -f [uint32]$failedResult.exitCode)}else{$null};cleanupReason=if($failedResult){$failedResult.cleanupReason}else{$null}
        allObservedOwnedChildrenExited=$true;guiPolicyQueriedBeforeResume=$true;commitMemoryLimitBytes=536870912;cpuHardCapPercent=20;activeProcessLimit=1
        fullSandboxEstablished=$false;originalCompatibilityEstablished=$false;originalLoaded=$false;xnaLoaded=$false;steamLoaded=$false;guiOpened=$false
        realInputUsed=$false;audioUsed=$false;networkUsed=$false;deviceUsed=$false;gameDirectoryAccessed=$false;systemConfigurationChanged=$false;aclChanged=$false;net8FailureReplaced=$false}
    [IO.File]::WriteAllText((Join-Path $artifact 'framework-summary.json'),($summary|ConvertTo-Json -Depth 8))
    [IO.File]::WriteAllText((Join-Path $artifact 'framework-cases.json'),($events|ConvertTo-Json -Depth 12))
    foreach($case in $cases){Write-Output ('OWN_FRAMEWORK_CASE scenario='+$case.result.scenario+' stage='+$case.result.lastStage+' exited='+$case.result.exited)}
    Write-Output ('OWN_FRAMEWORK_SUMMARY status='+$status+' passed='+$cases.Count+' failedScenario='+$summary.failedScenario+' nativeException='+$summary.nativeExceptionCode+' lastOwnPhase='+$summary.lastOwnPhase+' originalCompatibilityEstablished=false')
}finally{Pop-Location}
exit $exitCode
