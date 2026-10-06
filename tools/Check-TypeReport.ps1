$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
Push-Location $repo
try{
    $fixture=Join-Path $repo ('artifacts/cdr-082-type-contracts/report-checks-'+[guid]::NewGuid().ToString('N'))
    for($p=$fixture;$p;$p=[IO.Path]::GetDirectoryName($p)){if((Test-Path -LiteralPath $p) -and ((Get-Item -LiteralPath $p -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'M2T_REPORT_LINK'}}
    New-Item -ItemType Directory -Path $fixture -Force|Out-Null
    $dll='samples/CelesteDesktop.ProgressDemo/bin/Release/net8.0/CelesteDesktop.ProgressDemo.dll';$actual='artifacts/cdr-082-type-contracts/summary.json'
    & dotnet $dll --output (Join-Path $fixture 'actual') --type-report $actual
    $html=[IO.File]::ReadAllText((Join-Path $fixture 'actual/index.html'))
    if($LASTEXITCODE -ne 0 -or $html -notmatch 'data-type-status="metadata-recorded"' -or ([regex]::Matches($html,'data-latest-update=')).Count -ne 1 -or $html -notmatch 'data-progress-overview="plain"' -or $html -notmatch '<details data-history="collapsed">'){throw 'M2T_REPORT_ACTUAL'}
    & dotnet $dll --output (Join-Path $fixture 'missing') --type-report (Join-Path $fixture 'missing.json')
    if($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText((Join-Path $fixture 'missing/index.html')) -notmatch 'data-type-status="not-inspected"'){throw 'M2T_REPORT_MISSING'}
    foreach($test in @('executed','compatibility','changed','assemblies','found','checks','oversized','outside','mixed','module','claim','public-bytes')){
        $report=Get-Content -LiteralPath $actual -Raw|ConvertFrom-Json
        switch($test){'executed'{$report.originalExecuted=$true};'compatibility'{$report.modernCompatibilityEstablished=$true};'changed'{$report.hashesStable=$false};'assemblies'{$report.inspectedAssemblies=4};'found'{$report.foundTypes=23};'checks'{$report.ownMetadataChecks=0};'mixed'{$report.mixedModeAssemblies=4};'module'{$report.moduleInitializerDeclarations=-1};'claim'{$report.metadataSurfaceEstablished=$false};'public-bytes'{$report.publicSourceBytes=1}}
        $content=$report|ConvertTo-Json -Depth 8;if($test -eq 'oversized'){$content+=(' '*20000)}
        $path=Join-Path $fixture ($test+'.json');[IO.File]::WriteAllText($path,$content)
        if($test -eq 'outside'){$path=Join-Path $repo 'docs/GOAL.md'}
        $saved=$ErrorActionPreference
        try{$ErrorActionPreference='Continue';$messages=@(& dotnet $dll --output (Join-Path $fixture $test) --type-report $path 2>&1);$code=$LASTEXITCODE}finally{$ErrorActionPreference=$saved}
        if($code -ne 2){throw ('M2T_REPORT_REJECTION_'+$test)}
    }
    Write-Output 'TYPE_REPORT_CHECKS passed=14 targetExecuted=false probeRun=false guiOpened=false'
}finally{Pop-Location}
