$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
function FileHash([string]$Path) {
    $stream=[IO.File]::OpenRead($Path); $hasher=[Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($hasher.ComputeHash($stream)).Replace('-','') }
    finally { $stream.Dispose(); $hasher.Dispose() }
}
function NoLinks([string]$Path) {
    for($cursor=[IO.Path]::GetFullPath($Path); $cursor; $cursor=[IO.Path]::GetDirectoryName($cursor)) {
        if((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'XNA_CACHE_LINK'}
    }
}
$approved=[ordered]@{
    'Microsoft.Xna.Framework'='38E7093F52D7474BBC6256906519781A1210D7DA50A1C667B52716FCF49CA130'
    'Microsoft.Xna.Framework.Game'='B5DFFDD8125ABEF2A4507BA4E1D2F11062143F0A63D48FE4F298B95AD746A1F0'
    'Microsoft.Xna.Framework.Graphics'='560080FC39021C611CA9D076DCEBED312FAF6D7D1413C2DC523683EA635E9F55'
}
Push-Location $repo
try {
    & git -c "safe.directory=$repo" -c core.fsmonitor=false check-ignore local-cache/cdr-082-xna/probe | Out-Null
    if($LASTEXITCODE -ne 0){throw 'XNA_CACHE_NOT_IGNORED'}
    $work=Join-Path $repo ('local-cache/cdr-082-xna/'+[guid]::NewGuid().ToString('N'))
    NoLinks $work
    $runtime=Join-Path $work 'runtime'
    New-Item -ItemType Directory -Path $runtime -Force | Out-Null
    $rows=@()
    foreach($name in $approved.Keys) {
        $source=Join-Path $env:SystemRoot ('Microsoft.NET/assembly/GAC_32/'+$name+'/v4.0_4.0.0.0__842cf8be1de50553/'+$name+'.dll')
        NoLinks $source
        if((FileHash $source) -ne $approved[$name]){throw 'XNA_CACHE_SOURCE_CHANGED'}
        $destination=Join-Path $runtime ($name+'.dll')
        Copy-Item -LiteralPath $source -Destination $destination
        if((FileHash $source) -ne $approved[$name] -or (FileHash $destination) -ne $approved[$name]){throw 'XNA_CACHE_COPY_CHANGED'}
        $factPath=Join-Path $work ($name+'.metadata.json')
        & dotnet tools/CelesteDesktop.AssemblyInventory/bin/Release/net8.0/CelesteDesktop.AssemblyInventory.dll --reference-root $runtime --name $name --output $factPath
        if($LASTEXITCODE -ne 0){throw 'XNA_CACHE_METADATA_FAILED'}
        $fact=Get-Content $factPath -Raw | ConvertFrom-Json
        if($fact.identity.name -ne $name -or $fact.identity.version -ne '4.0.0.0' -or $fact.identity.publicKeyToken -ne '842cf8be1de50553'){throw 'XNA_CACHE_IDENTITY_CHANGED'}
        $rows += [ordered]@{name=$name;sha256=$approved[$name];identityMatched=$true}
    }
    $manifest=[ordered]@{taskId='CDR-082';stage='approved-xna-private-cache';copiedFiles=3;rows=$rows;assemblyExecuted=$false;runtimeSafetyEstablished=$false;gameLaunched=$false;guiOpened=$false;installationWrites=0;downloads=0;commercialDependenciesLocalOnly=$true}
    [IO.File]::WriteAllText((Join-Path $work 'summary.json'),($manifest | ConvertTo-Json -Depth 6))
    $artifact=Join-Path $repo 'artifacts/cdr-082-xna-cache'
    NoLinks $artifact
    New-Item -ItemType Directory -Path $artifact -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $artifact 'summary.json'),($manifest | ConvertTo-Json -Depth 6))
    Write-Output 'XNA_PRIVATE_CACHE_COMPLETED copiedFiles=3 identityMatched=true assemblyExecuted=false'
} finally {Pop-Location}
exit 0
