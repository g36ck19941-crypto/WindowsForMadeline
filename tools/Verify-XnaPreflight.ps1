param([switch]$AuditCache,[switch]$AuditSystemDependencies)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $repo 'artifacts/cdr-081-verification/dotnet-home'
$env:APPDATA=Join-Path $repo 'artifacts/cdr-081-verification/appdata'
$env:NUGET_PACKAGES=Join-Path $repo 'artifacts/cdr-081-verification/nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
Push-Location $repo
try {
    & dotnet restore tools/CelesteDesktop.XnaPreflight --configfile NuGet.Offline.Config
    if($LASTEXITCODE -ne 0){throw 'XNA_PREFLIGHT_RESTORE_FAILED'}
    & dotnet build tools/CelesteDesktop.XnaPreflight -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'XNA_PREFLIGHT_BUILD_FAILED'}
    & dotnet tools/CelesteDesktop.XnaPreflight/bin/Release/net8.0/CelesteDesktop.XnaPreflight.dll --self-test
    if($LASTEXITCODE -ne 0){throw 'XNA_PREFLIGHT_TEST_FAILED'}
    if($AuditCache) {
        $cacheRoot=Join-Path $repo 'local-cache/cdr-082-xna'
        if(((Get-Item -LiteralPath $cacheRoot -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'XNA_PREFLIGHT_CACHE_LINK'}
        $candidate=Get-ChildItem -LiteralPath $cacheRoot -Directory | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
        if(-not $candidate){throw 'XNA_PREFLIGHT_CACHE_MISSING'}
        $privateOutput=Join-Path $candidate.FullName ('initializer-boundaries-'+[guid]::NewGuid().ToString('N')+'.json')
        $lines=@(& dotnet tools/CelesteDesktop.XnaPreflight/bin/Release/net8.0/CelesteDesktop.XnaPreflight.dll --cached-root (Join-Path $candidate.FullName 'runtime') --private-boundaries $privateOutput)
        if($LASTEXITCODE -ne 0 -or $lines.Count -ne 1){throw 'XNA_PREFLIGHT_AUDIT_FAILED'}
        $report=$lines[0] | ConvertFrom-Json
        if($report.assemblyExecuted -or $report.runtimeSafetyEstablished -or $report.originalTestExecuted){throw 'XNA_PREFLIGHT_RUNTIME_CLAIM'}
        $artifact=Join-Path $repo 'artifacts/cdr-082-xna-cache'
        for($cursor=$artifact; $cursor; $cursor=[IO.Path]::GetDirectoryName($cursor)) {
            if((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'XNA_PREFLIGHT_OUTPUT_LINK'}
        }
        New-Item -ItemType Directory -Path $artifact -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $artifact 'audit.json'), $lines[0])
        foreach($row in $report.rows){
            Write-Output ("XNA_METADATA name="+$row.name+" ilOnly="+$row.ilOnly+" moduleInitializer="+$row.moduleInitializerPresent+" nativeMethods="+$row.nativeMethodCount+" pinvokeMethods="+$row.pinvokeMethods)
            Write-Output ("XNA_INITIALIZER_GRAPH name="+$row.name+" managedMethods="+$row.initialization.visitedManagedMethods+" pinvokeBoundaries="+$row.initialization.pinvokeBoundaries+" memberBoundaries="+$row.initialization.memberReferenceBoundaries+" indirectCalls="+$row.initialization.indirectCalls+" actualInvocationProven=false")
            Write-Output ("XNA_BOUNDARY_CLASSIFICATION name="+$row.name+" pinvokeWithNativeRva="+$row.initialization.pinvokeWithNativeRva+" emptyModuleNames="+$row.initialization.pinvokeEmptyModuleNames+" memberScopes="+($row.initialization.memberReferenceScopes | ConvertTo-Json -Compress)+" runtimeSafetyEstablished=false")
        }
        Write-Output 'XNA_STATIC_AUDIT_COMPLETED runtimeSafetyEstablished=false originalTestExecuted=false'
    }
    if($AuditSystemDependencies) {
        $lines=@(& dotnet tools/CelesteDesktop.XnaPreflight/bin/Release/net8.0/CelesteDesktop.XnaPreflight.dll --system-dependencies)
        if($LASTEXITCODE -ne 0 -or $lines.Count -ne 1){throw 'SYSTEM_DEPENDENCY_AUDIT_FAILED'}
        $report=$lines[0] | ConvertFrom-Json
        if($report.targetExecuted -or $report.dependenciesCopied -or $report.recursivelyResolved -or $report.runtimeSafetyEstablished -or $report.gameDirectoryAccessed){throw 'SYSTEM_DEPENDENCY_BOUNDARY_FAILED'}
        $artifact=Join-Path $repo 'artifacts/cdr-082-system-dependencies'
        for($cursor=$artifact; $cursor; $cursor=[IO.Path]::GetDirectoryName($cursor)) {
            if((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'SYSTEM_DEPENDENCY_OUTPUT_LINK'}
        }
        New-Item -ItemType Directory -Path $artifact -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $artifact 'summary.json'),$lines[0])
        foreach($row in $report.rows){Write-Output ("SYSTEM_DEPENDENCY name="+$row.name+" status="+$row.status+" machine="+$row.fact.machine+" moduleInitializer="+$row.fact.moduleInitializer+" pinvokeMethods="+$row.fact.pinvokeMethods)}
        Write-Output 'SYSTEM_DEPENDENCY_AUDIT_COMPLETED targetExecuted=false runtimeSafetyEstablished=false'
    }
} finally {Pop-Location}
exit 0
