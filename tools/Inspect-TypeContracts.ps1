$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
function NoLinks([string]$p){for($c=[IO.Path]::GetFullPath($p);$c;$c=[IO.Path]::GetDirectoryName($c)){if((Test-Path -LiteralPath $c) -and ((Get-Item -LiteralPath $c -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'M2T_LINK'}}}
Push-Location $repo
try{
    $cache=Join-Path $repo 'local-cache/cdr-082-xna';NoLinks $cache
    $candidates=@(Get-ChildItem -LiteralPath $cache -Directory | Where-Object {NoLinks $_.FullName;$p=Join-Path $_.FullName 'summary.json';NoLinks $p
        if(Test-Path -LiteralPath $p){if((Get-Item -LiteralPath $p).Length -gt 65536){throw 'M2T_MANIFEST_BUDGET'};$d=Get-Content -LiteralPath $p -Raw|ConvertFrom-Json
            $d.stage -eq 'approved-xna-private-cache' -and $d.copiedFiles -eq 3 -and -not $d.assemblyExecuted}})
    if($candidates.Count -ne 1){throw 'M2T_CACHE_AMBIGUOUS'}
    $gitRepo=$repo.Replace('\','/');& git -c "safe.directory=$gitRepo" -c core.fsmonitor=false check-ignore local-cache/cdr-082-type-contracts/check
    if($LASTEXITCODE -ne 0){throw 'M2T_PRIVATE_NOT_IGNORED'}
    $output=Join-Path $repo ('local-cache/cdr-082-type-contracts/'+[guid]::NewGuid().ToString('N'));NoLinks $output
    $env:DOTNET_CLI_HOME=Join-Path $repo 'artifacts/cdr-081-verification/dotnet-home';$env:APPDATA=Join-Path $repo 'artifacts/cdr-081-verification/appdata'
    $env:NUGET_PACKAGES=Join-Path $repo 'artifacts/cdr-081-verification/nuget-packages';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_NOLOGO='1';$env:DOTNET_EnableDiagnostics='0'
    foreach($hook in @('DOTNET_STARTUP_HOOKS','DOTNET_ADDITIONAL_DEPS','DOTNET_SHARED_STORE','CORECLR_PROFILER','CORECLR_PROFILER_PATH','COR_PROFILER','COR_PROFILER_PATH')){[Environment]::SetEnvironmentVariable($hook,$null,'Process')}
    $env:CORECLR_ENABLE_PROFILING='0';$env:COR_ENABLE_PROFILING='0'
    & dotnet restore tools/CelesteDesktop.XnaPreflight --configfile NuGet.Offline.Config
    if($LASTEXITCODE -ne 0){throw 'M2T_OWN_RESTORE'}
    & dotnet build tools/CelesteDesktop.XnaPreflight -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'M2T_OWN_BUILD'}
    & dotnet tools/CelesteDesktop.XnaPreflight/bin/Release/net8.0/CelesteDesktop.XnaPreflight.dll --type-contracts (Join-Path $candidates[0].FullName 'runtime') --output $output
    if($LASTEXITCODE -ne 0){throw 'M2T_ANALYSIS'}
    $artifact=Join-Path $repo 'artifacts/cdr-082-type-contracts';NoLinks $artifact;New-Item -ItemType Directory -Path $artifact -Force|Out-Null
    $summary=Join-Path $artifact 'summary.json';NoLinks $summary
    [IO.File]::WriteAllText($summary,[IO.File]::ReadAllText((Join-Path $output 'summary.json')))
    Write-Output ('M2T_PRIVATE_REPORT local-cache/cdr-082-type-contracts/'+[IO.Path]::GetFileName($output))
}catch{$code=if($_.Exception.Message -match '^M2T_[A-Z_]+$'){$_.Exception.Message}else{'M2T_WRAPPER_FAILED'};Write-Output ('TYPE_CONTRACTS_FAILED code='+$code+' exceptionType='+$_.Exception.GetType().Name);exit 3}finally{Pop-Location}
