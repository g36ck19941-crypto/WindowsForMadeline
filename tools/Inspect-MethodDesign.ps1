$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
function NoLinks([string]$value){for($p=[IO.Path]::GetFullPath($value);$p;$p=[IO.Path]::GetDirectoryName($p)){if((Test-Path -LiteralPath $p) -and ((Get-Item -LiteralPath $p -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'M2_LINK'}}}
Push-Location $repo
try{
    $cache=Join-Path $repo 'local-cache/cdr-082';NoLinks $cache
    $candidates=@(Get-ChildItem -LiteralPath $cache -Directory|Where-Object{
        NoLinks $_.FullName;$manifest=Join-Path $_.FullName 'summary.json';NoLinks $manifest
        if(Test-Path -LiteralPath $manifest){if((Get-Item -LiteralPath $manifest).Length -gt 65536){throw 'M2_MANIFEST_BUDGET'}
            $data=Get-Content -LiteralPath $manifest -Raw|ConvertFrom-Json
            $data.assemblySha256 -eq '1A1E117ADD967C0F26AD470A49D4FF442435209265BF1FDDA623821D797E80B5' -and $data.recoveredSourceFiles -eq 918 -and -not $data.recoveredCodeExecuted}})
    if($candidates.Count -ne 1){throw 'M2_CACHE_AMBIGUOUS'}
    $gitRepo=$repo.Replace('\','/')
    & git -c "safe.directory=$gitRepo" -c core.fsmonitor=false check-ignore local-cache/cdr-082-method-design/check
    if($LASTEXITCODE -ne 0){throw 'M2_PRIVATE_NOT_IGNORED'}
    $output=Join-Path $repo ('local-cache/cdr-082-method-design/'+[guid]::NewGuid().ToString('N'));NoLinks $output
    $env:DOTNET_CLI_HOME=Join-Path $repo 'artifacts/cdr-081-verification/dotnet-home'
    $env:APPDATA=Join-Path $repo 'artifacts/cdr-081-verification/appdata';$env:NUGET_PACKAGES=Join-Path $repo 'artifacts/cdr-081-verification/nuget-packages'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_NOLOGO='1';$env:DOTNET_EnableDiagnostics='0'
    foreach($hook in @('DOTNET_STARTUP_HOOKS','DOTNET_ADDITIONAL_DEPS','DOTNET_SHARED_STORE','CORECLR_PROFILER','CORECLR_PROFILER_PATH','COR_PROFILER','COR_PROFILER_PATH')){[Environment]::SetEnvironmentVariable($hook,$null,'Process')}
    $env:CORECLR_ENABLE_PROFILING='0';$env:COR_ENABLE_PROFILING='0'
    & dotnet restore tools/CelesteDesktop.CompileClosure --configfile NuGet.Offline.Config
    if($LASTEXITCODE -ne 0){throw 'M2_OWN_RESTORE'}
    & dotnet build tools/CelesteDesktop.CompileClosure -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'M2_OWN_BUILD'}
    & dotnet tools/CelesteDesktop.CompileClosure/bin/Release/net10.0/CelesteDesktop.CompileClosure.dll --method-design (Join-Path $candidates[0].FullName 'source') --output $output
    if($LASTEXITCODE -ne 0){throw 'M2_ANALYSIS'}
    $artifact=Join-Path $repo 'artifacts/cdr-082-method-design';NoLinks $artifact;New-Item -ItemType Directory -Path $artifact -Force|Out-Null
    $summary=Join-Path $artifact 'summary.json';NoLinks $summary
    [IO.File]::WriteAllText($summary,[IO.File]::ReadAllText((Join-Path $output 'summary.json')))
    Write-Output ('M2_PRIVATE_REPORT local-cache/cdr-082-method-design/'+[IO.Path]::GetFileName($output))
}catch{$code=if($_.Exception.Message -match '^M2_[A-Z_]+$'){$_.Exception.Message}else{'M2_WRAPPER_FAILED'};Write-Output ('METHOD_DESIGN_FAILED code='+$code+' exceptionType='+$_.Exception.GetType().Name);exit 3}finally{Pop-Location}
