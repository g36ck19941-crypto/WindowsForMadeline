$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
function FileHash([string]$Path) {
    $stream=[IO.File]::OpenRead($Path); $hasher=[Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($hasher.ComputeHash($stream)).Replace('-','') }
    finally { $stream.Dispose(); $hasher.Dispose() }
}
function NoLinks([string]$Path) {
    for($cursor=[IO.Path]::GetFullPath($Path); $cursor; $cursor=[IO.Path]::GetDirectoryName($cursor)) {
        if((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'ISOLATION_FRAMEWORK_LINK' }
    }
}
Push-Location $repo
try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-RuntimeIsolation.ps1
    if($LASTEXITCODE -ne 0){throw 'ISOLATION_GATE_FAILED'}
    $search=Get-Content artifacts/cdr-082-reference-search/summary.json -Raw | ConvertFrom-Json
    $references=@(); $expectedHashes=@{}
    foreach($name in @('mscorlib','System','System.Core','System.Xml')) {
        $row=@($search.referenceRows | Where-Object { $_.scope -eq 'reference-framework-v4.7.2' -and $_.name -eq $name -and $_.exactOriginalReferenceMatch })
        if($row.Count -ne 1){throw 'ISOLATION_FRAMEWORK_REFERENCE_EVIDENCE'}
        $path=Join-Path ${env:ProgramFiles(x86)} ('Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7.2/'+$name+'.dll')
        NoLinks $path
        if((FileHash $path) -ne $row[0].sha256){throw 'ISOLATION_FRAMEWORK_REFERENCE_CHANGED'}
        $references+=('/reference:'+ $path); $expectedHashes[$path]=$row[0].sha256
    }
    $sdk=(& dotnet --version).Trim()
    if($LASTEXITCODE -ne 0 -or $sdk -notmatch '^\d+\.\d+\.\d+$'){throw 'ISOLATION_COMPILER_VERSION'}
    $dotnetRoot=Split-Path -Parent (Get-Command dotnet -CommandType Application).Source
    $compiler=Join-Path $dotnetRoot ('sdk/'+$sdk+'/Roslyn/bincore/csc.dll')
    NoLinks $compiler
    if(-not (Test-Path -LiteralPath $compiler -PathType Leaf)){throw 'ISOLATION_COMPILER_MISSING'}
    $artifact=Join-Path $repo 'artifacts/cdr-082-isolation-framework'
    $work=Join-Path $artifact ([guid]::NewGuid().ToString('N'))
    NoLinks $work
    New-Item -ItemType Directory -Path $work -Force | Out-Null
    $output=Join-Path $work 'CelesteDesktop.RuntimeIsolation.Net472.dll'
    $sources=@('src/CelesteDesktop.RuntimeIsolation/IsolationSession.cs','src/CelesteDesktop.RuntimeIsolation/FrameworkCompatibility.cs')
    $sourceHashes=@{}; foreach($source in $sources){NoLinks $source; $sourceHashes[$source]=FileHash $source}
    & dotnet $compiler /nologo /target:library /nostdlib+ /deterministic+ /optimize+ /warnaserror+ /nullable:enable /langversion:latest /define:NETFRAMEWORK ('/out:'+$output) @references @sources
    if($LASTEXITCODE -ne 0){throw 'ISOLATION_FRAMEWORK_COMPILE_FAILED'}
    foreach($path in $expectedHashes.Keys){if((FileHash $path) -ne $expectedHashes[$path]){throw 'ISOLATION_FRAMEWORK_REFERENCE_CHANGED'}}
    foreach($source in $sources){if((FileHash $source) -ne $sourceHashes[$source]){throw 'ISOLATION_FRAMEWORK_SOURCE_CHANGED'}}
    $audit=@(& dotnet tests/CelesteDesktop.RuntimeIsolation.Tests/bin/Release/net8.0/CelesteDesktop.RuntimeIsolation.Tests.dll --audit-net472 $output)
    if($LASTEXITCODE -ne 0 -or $audit.Count -ne 1){throw 'ISOLATION_FRAMEWORK_AUDIT_FAILED'}
    $data=$audit[0] | ConvertFrom-Json
    if(-not $data.compileSucceeded -or -not $data.metadataAuditPassed -or $data.net472OutputExecuted){throw 'ISOLATION_FRAMEWORK_AUDIT_CONTRADICTION'}
    [IO.File]::WriteAllText((Join-Path $artifact 'summary.json'), $audit[0])
    Write-Output 'ISOLATION_NET472_COMPILED originalBound=false outputExecuted=false'
} finally { Pop-Location }
exit 0
