$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
Push-Location $repo
try{
    $fixture=Join-Path $repo ('artifacts/cdr-082-method-design/report-checks-'+[guid]::NewGuid().ToString('N'))
    for($p=$fixture;$p;$p=[IO.Path]::GetDirectoryName($p)){if((Test-Path -LiteralPath $p) -and ((Get-Item -LiteralPath $p -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'M2_REPORT_LINK'}}
    New-Item -ItemType Directory -Path $fixture -Force|Out-Null
    $dll='samples/CelesteDesktop.ProgressDemo/bin/Release/net8.0/CelesteDesktop.ProgressDemo.dll'
    $actual='artifacts/cdr-082-method-design/summary.json'
    & dotnet $dll --output (Join-Path $fixture 'actual') --method-report $actual
    if($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText((Join-Path $fixture 'actual/index.html')) -notmatch 'data-method-status="design-recorded"'){throw 'M2_REPORT_ACTUAL'}
    & dotnet $dll --output (Join-Path $fixture 'missing') --method-report (Join-Path $fixture 'missing.json')
    if($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText((Join-Path $fixture 'missing/index.html')) -notmatch 'data-method-status="not-inspected"'){throw 'M2_REPORT_MISSING'}
    foreach($test in @('executed','semantic','changed','files','checks','oversized','outside','deferred','digest')){
        $report=Get-Content -LiteralPath $actual -Raw|ConvertFrom-Json
        switch($test){'executed'{$report.originalExecuted=$true};'semantic'{$report.semanticBindingEstablished=$true};'changed'{$report.sourceHashesStable=$false};'files'{$report.selectedFiles=12};'checks'{$report.ownSyntaxChecks=0};'deferred'{$report.deferredCallSites=100001};'digest'{$report.sourceDigest='bad'}}
        $content=$report|ConvertTo-Json -Depth 8;if($test -eq 'oversized'){$content+=(' '*20000)}
        $path=Join-Path $fixture ($test+'.json');[IO.File]::WriteAllText($path,$content)
        if($test -eq 'outside'){$path=Join-Path $repo 'docs/GOAL.md'}
        $saved=$ErrorActionPreference
        try{$ErrorActionPreference='Continue';$messages=@(& dotnet $dll --output (Join-Path $fixture $test) --method-report $path 2>&1);$code=$LASTEXITCODE}finally{$ErrorActionPreference=$saved}
        if($code -ne 2){throw ('M2_REPORT_REJECTION_'+$test)}
    }
    Write-Output 'METHOD_REPORT_CHECKS passed=11 targetExecuted=false probeRun=false guiOpened=false'
}finally{Pop-Location}
