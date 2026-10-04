param([switch]$SelfTest)
$ErrorActionPreference = 'Stop'

function Get-FrameworkFloor([long]$Release) {
    if ($Release -ge 533320) { return '4.8.1-or-later' }
    if ($Release -ge 528040) { return '4.8-or-later' }
    if ($Release -ge 461808) { return '4.7.2-or-later' }
    return 'below-4.7.2-or-unknown'
}
function Assert-NoLinks([string]$Candidate) {
    for ($current = [IO.Path]::GetFullPath($Candidate); $current; $current = [IO.Path]::GetDirectoryName($current)) {
        if ((Test-Path -LiteralPath $current) -and ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'Linked inventory/output path rejected.'
        }
    }
}
if ($SelfTest) {
    $cases = @(@(0,'below-4.7.2-or-unknown'),@(461807,'below-4.7.2-or-unknown'),
        @(461808,'4.7.2-or-later'),@(528039,'4.7.2-or-later'),@(528040,'4.8-or-later'),
        @(533319,'4.8-or-later'),@(533320,'4.8.1-or-later'),@(999999,'4.8.1-or-later'))
    foreach ($case in $cases) { if ((Get-FrameworkFloor $case[0]) -ne $case[1]) { throw 'Framework floor check failed.' } }
    Write-Output 'ENVIRONMENT_SELF_CHECKS_OK checks=8 systemReads=0'
    exit 0
}
$projectRoot = Split-Path -Parent $PSScriptRoot
Assert-NoLinks $projectRoot
$reportedArchitecture = $env:PROCESSOR_ARCHITEW6432
if (-not $reportedArchitecture) { $reportedArchitecture = $env:PROCESSOR_ARCHITECTURE }
if ($reportedArchitecture -notin @('AMD64','ARM64','x86')) { $reportedArchitecture = 'unknown' }
$windowsRoot = [Environment]::GetFolderPath([Environment+SpecialFolder]::Windows)
$registry = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,[Microsoft.Win32.RegistryView]::Registry64)
try {
    $osKey = $registry.OpenSubKey('SOFTWARE\Microsoft\Windows NT\CurrentVersion', $false)
    if (-not $osKey) { throw 'OS version registry key unavailable.' }
    try {
        $build = [int]$osKey.GetValue('CurrentBuildNumber',0)
        $os = [ordered]@{ major = $osKey.GetValue('CurrentMajorVersionNumber',0); minor = $osKey.GetValue('CurrentMinorVersionNumber',0)
            build = $build; revision = $osKey.GetValue('UBR',0); displayVersion = $osKey.GetValue('DisplayVersion','unknown')
            edition = $osKey.GetValue('EditionID','unknown'); is64Bit = [Environment]::Is64BitOperatingSystem
            process64Bit = [Environment]::Is64BitProcess; reportedArchitecture = $reportedArchitecture
            versionEligibleForWindows8Apis = ($build -ge 9200) }
    } finally { $osKey.Dispose() }
} finally { $registry.Dispose() }
$framework = @()
foreach ($view in @([Microsoft.Win32.RegistryView]::Registry32,[Microsoft.Win32.RegistryView]::Registry64)) {
    $baseKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,$view)
    try {
        $key = $baseKey.OpenSubKey('SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', $false)
        $release = 0
        if ($key) { try { $release = [long]$key.GetValue('Release',0) } finally { $key.Dispose() } }
        $framework += [ordered]@{ view = $view.ToString(); release = $release; minimumVersion = Get-FrameworkFloor $release }
    } finally { $baseKey.Dispose() }
}
$slots = @('System32\kernel32.dll','System32\userenv.dll','SysWOW64\kernel32.dll','SysWOW64\userenv.dll',
    'Microsoft.NET\Framework\v4.0.30319\clr.dll','Microsoft.NET\Framework\v4.0.30319\mscorlib.dll',
    'Microsoft.NET\Framework64\v4.0.30319\clr.dll','Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll')
$files = @()
foreach ($slot in $slots) {
    $filePath = Join-Path $windowsRoot $slot
    Assert-NoLinks $filePath
    $present = Test-Path -LiteralPath $filePath -PathType Leaf
    $version = $null
    if ($present) { $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($filePath).FileVersion }
    $files += [ordered]@{ slot = $slot; present = $present; fileVersion = $version }
}
$summary = [ordered]@{ schemaVersion = 1; taskId = 'CDR-082'; stage = 'restricted-environment-readonly-inventory'
    inspectedUtc = [DateTime]::UtcNow.ToString('o'); windows = $os; framework = $framework; fixedFiles = $files
    registryKeysInspected = 3; fileSlotsInspected = 8; targetLoaded = $false; xnaLoaded = $false
    gameDirectoryAccessed = $false; guiOpened = $false; systemConfigurationChanged = $false; downloads = 0
    isolationEstablished = $false; originalRuntimeCompatibilityEstablished = $false
    capabilityStatus = @('filesystem:unknown','network:unknown','gui:unknown','live-input:unknown','audio:unknown',
        'steam:unknown','child-process-containment:unknown','x86-mixed-mode:unknown') }
$output = Join-Path $projectRoot 'artifacts/cdr-082-environment'
Assert-NoLinks $output
New-Item -ItemType Directory -Path $output -Force | Out-Null
$runFile = Join-Path $output ('inventory-' + [guid]::NewGuid().ToString('N') + '.json')
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $runFile -Encoding UTF8
$latestFile = Join-Path $output 'summary.json'
Assert-NoLinks $latestFile
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $latestFile -Encoding UTF8
Write-Output ('ENVIRONMENT_INVENTORY_COMPLETED build=' + $build + ' release32=' + $framework[0].release + ' fileSlots=8 isolationEstablished=false')
Write-Output ('REPORT_PATH=' + $runFile)
