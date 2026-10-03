[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Push-Location $repo
try {
    $expectedHash='1A1E117ADD967C0F26AD470A49D4FF442435209265BF1FDDA623821D797E80B5'
    $candidates=@(Get-ChildItem -LiteralPath (Join-Path $repo 'local-cache/cdr-082') -Directory | Where-Object {
        $manifest=Join-Path $_.FullName 'summary.json'
        if (Test-Path -LiteralPath $manifest -PathType Leaf) {
            $data=Get-Content -Raw -LiteralPath $manifest | ConvertFrom-Json
            $data.assemblySha256 -eq $expectedHash -and $data.recoveredSourceFiles -eq 918 -and -not $data.recoveredCodeExecuted
        }
    })
    if($candidates.Count -ne 1) { throw 'CLOSURE_CACHE_MISSING_OR_AMBIGUOUS' }
    $source=Join-Path $candidates[0].FullName 'source'
    $ignored=& git -c "safe.directory=$repo" -c core.fsmonitor=false check-ignore local-cache/cdr-082-closure/probe
    if($LASTEXITCODE -ne 0) { throw 'CLOSURE_CACHE_NOT_IGNORED' }
    $work=Join-Path $repo ('local-cache/cdr-082-closure/'+[guid]::NewGuid().ToString('N'))
    for($cursor=$work; $cursor; $cursor=[IO.Path]::GetDirectoryName($cursor)) {
        if((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'CLOSURE_REPARSE_POINT' }
    }
    New-Item -ItemType Directory -Path $work -Force | Out-Null
    $search=Get-Content -Raw -LiteralPath (Join-Path $repo 'artifacts/cdr-082-reference-search/summary.json') | ConvertFrom-Json
    $refs=@()
    foreach($name in @('Microsoft.Xna.Framework','Microsoft.Xna.Framework.Game','Microsoft.Xna.Framework.Graphics')) {
        $row=@($search.referenceRows | Where-Object { $_.scope -eq 'gac32-xna' -and $_.name -eq $name -and $_.exactOriginalReferenceMatch })
        if($row.Count -ne 1) { throw 'CLOSURE_XNA_EVIDENCE_MISSING' }
        $path=Join-Path $env:SystemRoot ('Microsoft.NET/assembly/GAC_32/'+$name+'/v4.0_4.0.0.0__842cf8be1de50553/'+$name+'.dll')
        $refs += [ordered]@{Name=$name;Path=$path;Sha256=$row[0].sha256}
    }
    foreach($name in @('mscorlib','System','System.Core','System.Xml')) {
        $row=@($search.referenceRows | Where-Object { $_.scope -eq 'reference-framework-v4.7.2' -and $_.name -eq $name -and $_.exactOriginalReferenceMatch })
        if($row.Count -ne 1) { throw 'CLOSURE_FRAMEWORK_EVIDENCE_MISSING' }
        $path=Join-Path ${env:ProgramFiles(x86)} ('Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7.2/'+$name+'.dll')
        $refs += [ordered]@{Name=$name;Path=$path;Sha256=$row[0].sha256}
    }
    $refsFile=Join-Path $work 'references.json'
    [IO.File]::WriteAllText($refsFile, ($refs|ConvertTo-Json -Depth 5))
    $env:DOTNET_CLI_HOME=Join-Path $repo 'artifacts/cdr-081-verification/dotnet-home'
    $env:APPDATA=Join-Path $repo 'artifacts/cdr-081-verification/appdata'
    $env:NUGET_PACKAGES=Join-Path $repo 'artifacts/cdr-081-verification/nuget-packages'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
    & dotnet restore tools/CelesteDesktop.CompileClosure --configfile NuGet.Offline.Config
    if($LASTEXITCODE -ne 0) { throw 'CLOSURE_TOOL_RESTORE_FAILED' }
    & dotnet build tools/CelesteDesktop.CompileClosure -c Release --no-restore
    if($LASTEXITCODE -ne 0) { throw 'CLOSURE_TOOL_BUILD_FAILED' }
    & dotnet tools/CelesteDesktop.CompileClosure/bin/Release/net10.0/CelesteDesktop.CompileClosure.dll --source $source --references $refsFile --output (Join-Path $work 'emit')
    $probeExit=$LASTEXITCODE
    if($probeExit -notin @(0,2)) { throw 'CLOSURE_PROBE_FAILED' }
    $summary=Get-Content -Raw -LiteralPath (Join-Path $work 'emit/summary.json')
    $artifact=Join-Path $repo 'artifacts/cdr-082-closure-real'
    New-Item -ItemType Directory -Path $artifact -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $artifact 'summary.json'), $summary)
} catch {
    $code=if($_.Exception.Message -match '^CLOSURE_[A-Z_]+$') {$_.Exception.Message} else {'CLOSURE_WRAPPER_FAILED'}
    Write-Output "$code exceptionType=$($_.Exception.GetType().Name)"
    exit 3
} finally { Pop-Location }
exit $probeExit
