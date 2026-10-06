$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
Push-Location $repo
try{
    $fixture=Join-Path $repo ('artifacts/cdr-082-migration-inventory/report-checks-'+[guid]::NewGuid().ToString('N'))
    for($p=$fixture;$p;$p=[IO.Path]::GetDirectoryName($p)){if((Test-Path -LiteralPath $p) -and ((Get-Item -LiteralPath $p -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'M1_REPORT_LINK'}}
    New-Item -ItemType Directory -Path $fixture -Force|Out-Null
    $dll='samples/CelesteDesktop.ProgressDemo/bin/Release/net8.0/CelesteDesktop.ProgressDemo.dll'
    $actual='artifacts/cdr-082-migration-inventory/summary.json'
    & dotnet $dll --output (Join-Path $fixture 'actual') --migration-report $actual
    # M1 remains validated on ingestion; the newest visible card now belongs to M2.
    if($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText((Join-Path $fixture 'actual/index.html')) -notmatch 'data-latest-update="type-contracts-v1"'){throw 'M1_REPORT_ACTUAL'}
    & dotnet $dll --output (Join-Path $fixture 'missing') --migration-report (Join-Path $fixture 'missing.json')
    if($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText((Join-Path $fixture 'missing/index.html')) -notmatch 'data-latest-update="type-contracts-v1"'){throw 'M1_REPORT_MISSING'}
    foreach($test in @('original-executed','semantic-claim','source-changed','bad-count','bad-checks','oversized','outside','invalid-xna')){
        $report=Get-Content -LiteralPath $actual -Raw|ConvertFrom-Json
        switch($test){'original-executed'{$report.originalExecuted=$true};'semantic-claim'{$report.semanticBindingEstablished=$true};'source-changed'{$report.sourceHashesStable=$false};'bad-count'{$report.coreCandidateFiles=919};'bad-checks'{$report.ownSyntaxChecks=0};'invalid-xna'{$report.xnaUsingFiles=919}}
        $content=$report|ConvertTo-Json -Depth 8;if($test -eq 'oversized'){$content+=(' '*20000)}
        $path=Join-Path $fixture ($test+'.json');[IO.File]::WriteAllText($path,$content)
        if($test -eq 'outside'){$path=Join-Path $repo 'docs/GOAL.md'}
        $saved=$ErrorActionPreference
        try{$ErrorActionPreference='Continue';$messages=@(& dotnet $dll --output (Join-Path $fixture $test) --migration-report $path 2>&1);$code=$LASTEXITCODE}finally{$ErrorActionPreference=$saved}
        if($code -ne 2){throw ('M1_REPORT_REJECTION_'+$test)}
    }
    Write-Output 'MIGRATION_REPORT_CHECKS passed=10 targetExecuted=false probeRun=false guiOpened=false'
}finally{Pop-Location}
