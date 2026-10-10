[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $PackageDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-NonEmptyFile([string] $RelativePath) {
    $path = Join-Path $PackageDirectory $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required package file is missing: $RelativePath"
    }
    if ((Get-Item -LiteralPath $path).Length -le 0) {
        throw "Required package file is empty: $RelativePath"
    }
}

if (-not (Test-Path -LiteralPath $PackageDirectory -PathType Container)) {
    throw "Package directory does not exist: $PackageDirectory"
}
$PackageDirectory = (Resolve-Path -LiteralPath $PackageDirectory).Path

@(
    'TheLostSoulOfFire.exe', 'TheLostSoulOfFire.dll', 'TheLostSoulOfFire.deps.json',
    'TheLostSoulOfFire.runtimeconfig.json', 'coreclr.dll', 'hostfxr.dll', 'hostpolicy.dll',
    'System.Private.CoreLib.dll', 'MonoGame.Framework.dll', 'MonoGame.Extended.dll',
    'Content/Visuals/registry.json', 'Content/Audio/Sfx/scythe_swing_1.xnb',
    'Content/Textures/Weapons/scythe_physical_256.xnb', 'Content/Effects/SceneGrade.xnb',
    'Content/Effects/SpriteLit.xnb', 'Content/Effects/Dissolve.xnb', 'Content/Effects/DeathFlame.xnb',
    'Content/Fonts/ui.xnb', 'build-info.json', 'TESTER-README.md'
) | ForEach-Object { Assert-NonEmptyFile $_ }

foreach ($name in @('SDL2.dll', 'openal.dll')) {
    $candidates = @(
        (Join-Path $PackageDirectory $name),
        (Join-Path (Join-Path $PackageDirectory 'runtimes/win-x64/native') $name)
    )
    $found = $false
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            if ((Get-Item -LiteralPath $candidate).Length -le 0) {
                throw "Required native library is empty: $name"
            }
            $found = $true
            break
        }
    }
    if (-not $found) {
        throw "Required win-x64 native library is missing: $name (package root or runtimes/win-x64/native)"
    }
}

try {
    $registry = Get-Content -LiteralPath (Join-Path $PackageDirectory 'Content/Visuals/registry.json') -Raw | ConvertFrom-Json -DateKind String
    $metadata = Get-Content -LiteralPath (Join-Path $PackageDirectory 'build-info.json') -Raw | ConvertFrom-Json -DateKind String
    $runtimeConfig = Get-Content -LiteralPath (Join-Path $PackageDirectory 'TheLostSoulOfFire.runtimeconfig.json') -Raw | ConvertFrom-Json -DateKind String
} catch {
    throw "Package JSON is invalid: $($_.Exception.Message)"
}

if ($null -eq $registry.visuals -or $registry.version -ne 1) {
    throw 'Content/Visuals/registry.json has an unexpected schema.'
}
if ($metadata.schemaVersion -ne 1 -or $metadata.version -notmatch '\A[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?\z' -or
    $metadata.sourceCommit -notmatch '\A[0-9a-f]{40}\z' -or $metadata.runtimeIdentifier -ne 'win-x64' -or
    $metadata.configuration -ne 'Release' -or $metadata.selfContained -ne $true) {
    throw 'build-info.json has invalid release metadata.'
}
try {
    $builtAt = [DateTimeOffset]::Parse($metadata.builtAtUtc, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind)
} catch {
    throw 'build-info.json builtAtUtc must be an ISO-8601 UTC timestamp.'
}
if ($builtAt.Offset -ne [TimeSpan]::Zero) {
    throw 'build-info.json builtAtUtc must be an ISO-8601 UTC timestamp.'
}
if ($runtimeConfig.runtimeOptions.tfm -ne 'net9.0') {
    throw 'Runtime configuration does not target net9.0.'
}
$runtimeOptionNames = @($runtimeConfig.runtimeOptions.PSObject.Properties.Name)
if ($runtimeOptionNames -contains 'framework' -or $runtimeOptionNames -contains 'frameworks') {
    throw 'Runtime configuration depends on a separately installed shared .NET framework.'
}
$includedFrameworks = @($runtimeConfig.runtimeOptions.includedFrameworks)
if (-not ($includedFrameworks | Where-Object { $_.name -eq 'Microsoft.NETCore.App' -and $_.version -match '\A9\.' })) {
    throw 'Self-contained runtime configuration must include Microsoft.NETCore.App 9.x.'
}

Write-Output "Package contents passed validation: $PackageDirectory"
