[CmdletBinding()]
param([string]$InstallRoot, [string]$IlSpyDll = $env:CDR_ILSPY_DLL, [switch]$SelfTest)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$expectedHash = '1A1E117ADD967C0F26AD470A49D4FF442435209265BF1FDDA623821D797E80B5'

function Test-AbsoluteWindowsPath([string]$Path) {
    # Path.IsPathFullyQualified is unavailable in Windows PowerShell 5/.NET Framework.
    return $Path -match '^[A-Za-z]:[\\/]' -or $Path -match '^\\\\[^\\]+\\[^\\]+(\\|$)'
}

function Assert-NoLink([string]$Path) {
    $cursor = [IO.Path]::GetFullPath($Path)
    while ($cursor) {
        if (Test-Path -LiteralPath $cursor) {
            if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw 'COMPILE_PROBE_REPARSE_POINT'
            }
        }
        $parent = [IO.Directory]::GetParent($cursor)
        if ($null -eq $parent) { break }
        $cursor = $parent.FullName
    }
}
function Assert-Contained([string]$Root, [string]$Path) {
    $prefix = [IO.Path]::GetFullPath($Root).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not [IO.Path]::GetFullPath($Path).StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'COMPILE_PROBE_PATH_ESCAPE'
    }
    Assert-NoLink $Path
}
function Invoke-Captured([string[]]$Arguments, [string]$WorkingDirectory, [int]$TimeoutSeconds = 300) {
    # Windows PowerShell 5: explicit quoting, no shell and no command-string execution.
    $quoted = foreach ($argument in $Arguments) {
        if ($argument.Contains('"') -or $argument.Contains("`r") -or $argument.Contains("`n")) { throw 'COMPILE_PROBE_ARGUMENT_INVALID' }
        '"' + $argument.TrimEnd('\') + '"'
    }
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = 'dotnet'
    $start.Arguments = $quoted -join ' '
    $start.WorkingDirectory = $WorkingDirectory
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) { throw 'COMPILE_PROBE_TOOL_START_FAILED' }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            $process.Kill()
            $process.WaitForExit()
            throw 'COMPILE_PROBE_TIMEOUT'
        }
        # Raw text stays in memory only; never print compiler excerpts or private paths.
        [pscustomobject]@{ exitCode = $process.ExitCode; text = $stdout.Result + "`n" + $stderr.Result }
    } finally { $process.Dispose() }
}
function Get-DiagnosticCounts([string]$Text) {
    $unique = @{}
    foreach ($line in ($Text -split "`n")) {
        if ($line -match '^(.+?)\((\d+),(\d+)\):\s+(error|warning)\s+(CS\d+):') {
            $key = $matches[1] + ':' + $matches[2] + ':' + $matches[3] + ':' + $matches[5]
            $unique[$key] = $matches[5]
        }
    }
    $counts = [ordered]@{}
    foreach ($group in @($unique.Values | Group-Object | Sort-Object Name)) { $counts[$group.Name] = $group.Count }
    return $counts
}
function New-ProbeProject([string[]]$Sources) {
    if ($Sources.Count -eq 0) { throw 'COMPILE_PROBE_EMPTY_SOURCES' }
    $items = foreach ($source in $Sources) {
        if ([IO.Path]::GetExtension($source) -ne '.cs') { throw 'COMPILE_PROBE_SOURCE_INVALID' }
        '<Compile Include="' + [Security.SecurityElement]::Escape($source) + '" />'
    }
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework><OutputType>Library</OutputType>
    <EnableDefaultItems>false</EnableDefaultItems><GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <RunAnalyzers>false</RunAnalyzers><RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
    <EnableNETAnalyzers>false</EnableNETAnalyzers><UseSharedCompilation>false</UseSharedCompilation>
  </PropertyGroup>
  <ItemGroup>$($items -join "`n")</ItemGroup>
  <Target Name="RemoveAnalyzers" BeforeTargets="CoreCompile"><ItemGroup><Analyzer Remove="@(Analyzer)" /></ItemGroup></Target>
</Project>
"@
}

if ($SelfTest) {
    $passed = 0
    function Check([bool]$Condition) { if (-not $Condition) { throw 'COMPILE_PROBE_TEST_FAILED' }; $script:passed++ }
    Check (Test-AbsoluteWindowsPath 'C:\generated')
    Check (Test-AbsoluteWindowsPath '\\synthetic\share\generated')
    Check (-not (Test-AbsoluteWindowsPath 'C:relative'))
    Check (-not (Test-AbsoluteWindowsPath '\relative'))
    $xmlText = New-ProbeProject @('C:\generated\Player.cs', 'C:\generated\A&B.cs')
    [xml]$xml = $xmlText
    Check ($xml.Project.PropertyGroup.OutputType -eq 'Library')
    Check ($xml.Project.PropertyGroup.EnableDefaultItems -eq 'false')
    Check ($xml.Project.PropertyGroup.RunAnalyzers -eq 'false')
    Check ($xml.Project.PropertyGroup.EnableNETAnalyzers -eq 'false')
    Check ($xml.Project.PropertyGroup.UseSharedCompilation -eq 'false')
    Check ($xml.Project.ItemGroup.Compile.Count -eq 2)
    Check ($xml.Project.ItemGroup.Compile[1].Include -eq 'C:\generated\A&B.cs')
    Check ($xmlText -notmatch 'PackageReference|ProjectReference|<Import|<Exec|EmbeddedResource')
    Check ($xml.Project.Target.ItemGroup.Analyzer.Remove -eq '@(Analyzer)')
    foreach ($inputValue in @(@(), @('generated.dll'))) {
        $rejected = $false
        try { New-ProbeProject $inputValue | Out-Null } catch { $rejected = $true }
        Check $rejected
    }
    $counts = Get-DiagnosticCounts "private/Player.cs(10,2): error CS0246: PRIVATE COMMERCIAL TEXT`nprivate/Player.cs(10,2): error CS0246: duplicate`nprivate/Actor.cs(4,3): error CS0234: PRIVATE"
    Check ($counts.CS0246 -eq 1)
    Check ($counts.CS0234 -eq 1)
    Check (($counts | ConvertTo-Json) -notmatch 'PRIVATE|Player|private/')
    Check ((Get-DiagnosticCounts 'unexpected text').Count -eq 0)
    $rejected = $false
    try { Assert-Contained (Join-Path $projectRoot 'local-cache') (Join-Path $projectRoot 'local-cache-other/a') } catch { $rejected = $true }
    Check $rejected
    $fixture = Join-Path $projectRoot ('artifacts/cdr-082-verification/' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $fixture -Force | Out-Null
    $link = Join-Path $fixture 'link'
    New-Item -ItemType Junction -Path $link -Target $fixture | Out-Null
    try {
        $rejected = $false
        try { Assert-NoLink (Join-Path $link 'not-created') } catch { $rejected = $true }
        Check $rejected
    } finally { [IO.Directory]::Delete($link) }
    $env:DOTNET_CLI_HOME = Join-Path $fixture 'dotnet-home'
    $env:APPDATA = Join-Path $fixture 'appdata'
    $env:NUGET_PACKAGES = Join-Path $fixture 'nuget-packages'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_NOLOGO = '1'
    $generated = Join-Path $fixture 'Generated.cs'
    $testProject = Join-Path $fixture 'Synthetic.csproj'
    [IO.File]::WriteAllText($generated, 'public sealed class Generated { public int Value => 1; }')
    [IO.File]::WriteAllText($testProject, (New-ProbeProject @($generated)))
    $safeProperties = @('-p:ImportDirectoryBuildProps=false','-p:ImportDirectoryBuildTargets=false','-p:ImportDirectoryPackagesProps=false','-p:NuGetAudit=false')
    $result = Invoke-Captured (@('restore',$testProject,'--configfile',(Join-Path $projectRoot 'NuGet.Offline.Config')) + $safeProperties) $fixture
    Check ($result.exitCode -eq 0)
    $result = Invoke-Captured (@('build',$testProject,'--no-restore','--disable-build-servers') + $safeProperties) $fixture
    Check ($result.exitCode -eq 0)
    Check ((Get-DiagnosticCounts $result.text).Count -eq 0)
    [IO.File]::WriteAllText($generated, 'public sealed class Generated { public MissingType Value; }')
    $result = Invoke-Captured (@('build',$testProject,'--no-restore','--disable-build-servers') + $safeProperties) $fixture
    Check ($result.exitCode -ne 0)
    Check ((Get-DiagnosticCounts $result.text).CS0246 -eq 1)
    Write-Output "COMPILE_PROBE_TESTS passed=$passed failed=0"
    exit 0
}

try {
    if (-not $InstallRoot -or -not (Test-AbsoluteWindowsPath $InstallRoot)) { throw 'COMPILE_PROBE_ROOT_REQUIRED' }
    $assembly = Join-Path $InstallRoot 'orig/Celeste.exe'
    Assert-NoLink $assembly
    if (-not (Test-Path -LiteralPath $assembly -PathType Leaf)) { throw 'COMPILE_PROBE_CANDIDATE_MISSING' }
    if ((Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash -ne $expectedHash) { throw 'COMPILE_PROBE_IDENTITY_CHANGED' }
    $cache = Join-Path $projectRoot 'local-cache'
    Assert-NoLink $cache
    $prefix = [IO.Path]::GetFullPath($InstallRoot).TrimEnd('\','/') + '\'
    if ([IO.Path]::GetFullPath($cache).StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'COMPILE_PROBE_INSTALL_OVERLAP' }
    $ignored = & git -c "safe.directory=$projectRoot" -c core.fsmonitor=false -C $projectRoot check-ignore local-cache/cdr-082/probe
    if ($LASTEXITCODE -ne 0) { throw 'COMPILE_PROBE_CACHE_NOT_IGNORED' }
    # Only an already-installed, fixed version. No tool restore or dependency download.
    $tool = if ($IlSpyDll) { [IO.Path]::GetFullPath($IlSpyDll) } else {
        Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages/ilspycmd/11.1.0.9782/tools/net10.0/any/ilspycmd.dll'
    }
    if ($tool.Replace('\','/') -notmatch '/ilspycmd/11\.1\.0\.9782/tools/net10\.0/any/ilspycmd\.dll$') { throw 'COMPILE_PROBE_TOOL_LOCATION_INVALID' }
    Assert-NoLink $tool
    if (-not (Test-Path -LiteralPath $tool -PathType Leaf)) { throw 'COMPILE_PROBE_PINNED_TOOL_MISSING' }
    $work = Join-Path $cache ('cdr-082/' + [guid]::NewGuid().ToString('N'))
    Assert-Contained $cache $work
    New-Item -ItemType Directory -Path $work -Force | Out-Null
    $env:DOTNET_CLI_HOME = Join-Path $work 'dotnet-home'
    $env:APPDATA = Join-Path $work 'appdata'
    $env:NUGET_PACKAGES = Join-Path $work 'nuget-packages'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_NOLOGO = '1'
    $version = Invoke-Captured @($tool, '--disable-updatecheck', '--version') $projectRoot
    if ($version.exitCode -ne 0 -or $version.text -notmatch 'ilspycmd: 11\.1\.0\.9782') { throw 'COMPILE_PROBE_TOOL_VERSION_CHANGED' }
    $source = Join-Path $work 'source'
    New-Item -ItemType Directory -Path $source | Out-Null
    Write-Output 'CDR082_RECOVERY_STARTED candidate=orig/Celeste.exe networkRestore=false'
    $decompile = Invoke-Captured @($tool, '--disable-updatecheck', '-p', '-o', $source, '-r', (Join-Path $InstallRoot 'orig'), $assembly) $projectRoot
    if ($decompile.exitCode -ne 0) { throw 'COMPILE_PROBE_DECOMPILER_FAILED' }
    if ((Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash -ne $expectedHash) { throw 'COMPILE_PROBE_IDENTITY_CHANGED' }
    $allSources = @(Get-ChildItem -LiteralPath $source -Recurse -File -Filter '*.cs')
    if ($allSources.Count -eq 0 -or $allSources.Count -gt 10000) { throw 'COMPILE_PROBE_SOURCE_COUNT_INVALID' }
    foreach ($file in $allSources) { Assert-Contained $cache $file.FullName }
    # Explicit original core seed, not a guessed implementation or generated project execution.
    $seeds = @('Celeste/Player.cs','Celeste/Actor.cs','Celeste/Solid.cs','Monocle/Entity.cs','Monocle/Scene.cs','Monocle/Component.cs','Monocle/Collider.cs','Monocle/Hitbox.cs','Monocle/Grid.cs','Monocle/StateMachine.cs','Monocle/Coroutine.cs','Monocle/Tracker.cs','Monocle/Sprite.cs','Monocle/ComponentList.cs','Monocle/EntityList.cs')
    $selected = @()
    foreach ($seed in $seeds) {
        $file = Join-Path $source $seed
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw 'COMPILE_PROBE_CORE_SEED_MISSING' }
        Assert-Contained $cache $file
        $selected += $file
    }
    $compileRoot = Join-Path $work 'compile'
    New-Item -ItemType Directory -Path $compileRoot | Out-Null
    $project = Join-Path $compileRoot 'Probe.csproj'
    [IO.File]::WriteAllText($project, (New-ProbeProject $selected), (New-Object Text.UTF8Encoding($false)))
    $properties = @('-p:ImportDirectoryBuildProps=false','-p:ImportDirectoryBuildTargets=false','-p:ImportDirectoryPackagesProps=false','-p:NuGetAudit=false')
    $restore = Invoke-Captured (@('restore',$project,'--configfile',(Join-Path $projectRoot 'NuGet.Offline.Config')) + $properties) $compileRoot
    $build = $null
    if ($restore.exitCode -eq 0) {
        $build = Invoke-Captured (@('build',$project,'-c','Release','--no-restore','--disable-build-servers') + $properties) $compileRoot
    }
    $counts = if ($null -ne $build) { Get-DiagnosticCounts $build.text } else { [ordered]@{} }
    $compiled = $null -ne $build -and $build.exitCode -eq 0
    $report = [ordered]@{
        schemaVersion=1; taskId='CDR-082'; candidate='orig/Celeste.exe'; assemblySha256=$expectedHash
        decompilerVersion='11.1.0.9782'; recoveredSourceFiles=$allSources.Count; selectedCoreFiles=$selected.Count
        targetFramework='net8.0'; originalFramework='net45'; frameworkRetargetingExperimental=$true
        restoreExitCode=$restore.exitCode; compileAttempted=($null -ne $build); compileEstablished=$compiled
        compileExitCode=$(if ($null -ne $build) { $build.exitCode } else { $null }); diagnosticCounts=$counts
        dependencyClosureEstablished=$false; xnaReferencesSupplied=$false; fnaSubstituted=$false
        recoveredCodeExecuted=$false; gameLaunched=$false; guiOpened=$false; installationWrites=0; dependencyDownloads=0
        commercialMaterialLocalOnly=$true; generatedProjectExecuted=$false; runtimeIntegrated=$false
        outcome=$(if ($compiled) { 'compiled-not-executed' } else { 'recovered-compile-blocked' })
    }
    $json = $report | ConvertTo-Json -Depth 8
    [IO.File]::WriteAllText((Join-Path $work 'summary.json'), $json)
    $evidence = Join-Path $projectRoot 'artifacts/cdr-082-real'
    Assert-NoLink $evidence
    New-Item -ItemType Directory -Path $evidence -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $evidence 'summary.json'), $json)
    Write-Output "CDR082_PROBE_COMPLETED recovered=$($allSources.Count) selected=$($selected.Count) compiled=$compiled outcome=$($report.outcome)"
    foreach ($entry in $counts.GetEnumerator()) { Write-Output "COMPILER_DIAGNOSTIC code=$($entry.Key) uniqueLocations=$($entry.Value)" }
    # A completed check is not successful compilation. Distinct nonzero outcome for blocked compilation.
    if (-not $compiled) { exit 2 }
    exit 0
} catch {
    # Never echo exception messages containing private paths or decompiler/compiler content.
    $code = if ($_.Exception.Message -match '^COMPILE_PROBE_[A-Z_]+$') { $_.Exception.Message } else { 'COMPILE_PROBE_UNEXPECTED' }
    Write-Output "$code exceptionType=$($_.Exception.GetType().Name) hresult=$($_.Exception.HResult)"
    exit 3
}
