[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $ArtifactDirectory,
    [Parameter(Mandatory = $true)] [string] $ExpectedSourceCommit,
    [Parameter(Mandatory = $true)] [string] $ExpectedZipSha256,
    [Parameter(Mandatory = $true)] [string] $ItchProject,
    [Parameter(Mandatory = $true)] [string] $ButlerPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($ExpectedSourceCommit -notmatch '\A[0-9a-f]{40}\z') { throw 'Expected source commit must be a full lowercase Git SHA.' }
if ($ExpectedZipSha256 -notmatch '\A[0-9a-fA-F]{64}\z') { throw 'Expected ZIP SHA-256 must contain exactly 64 hexadecimal characters.' }
if ($ItchProject -cnotmatch '\A[a-z0-9][a-z0-9-]*/[a-z0-9][a-z0-9-]*\z') { throw 'ITCH_PROJECT must use lowercase owner/slug format.' }
if (-not $env:BUTLER_API_KEY) { throw 'BUTLER_API_KEY environment variable is required.' }
if (-not (Test-Path -LiteralPath $ButlerPath -PathType Leaf)) { throw "butler executable does not exist: $ButlerPath" }
if (-not (Test-Path -LiteralPath $ArtifactDirectory -PathType Container)) { throw "Artifact directory does not exist: $ArtifactDirectory" }

$zipFiles = @(Get-ChildItem -LiteralPath $ArtifactDirectory -Filter '*.zip' -File)
if ($zipFiles.Count -ne 1) { throw "Expected exactly one ZIP artifact; found $($zipFiles.Count)." }
$zipFile = $zipFiles[0]
$checksumPath = "$($zipFile.FullName).sha256"
$externalMetadataPath = Join-Path $ArtifactDirectory 'build-info.json'
if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) { throw 'ZIP checksum file is missing.' }
if (-not (Test-Path -LiteralPath $externalMetadataPath -PathType Leaf)) { throw 'External build-info.json is missing.' }

$checksumLine = (Get-Content -LiteralPath $checksumPath -Raw).TrimEnd("`r", "`n")
if ($checksumLine -notmatch '\A([0-9a-f]{64})  ([^\\/:]+\.zip)\z') { throw 'Checksum file must contain lowercase SHA-256, two spaces, and the ZIP filename.' }
if ($Matches[2] -cne $zipFile.Name) { throw 'Checksum filename does not match the selected ZIP.' }
$checksumFileHash = $Matches[1]
$actualHash = (Get-FileHash -LiteralPath $zipFile.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
if ($checksumFileHash -cne $actualHash -or $ExpectedZipSha256.ToLowerInvariant() -cne $actualHash) {
    throw 'The downloaded ZIP does not match its stored and operator-confirmed SHA-256.'
}

try {
    $outerMetadata = Get-Content -LiteralPath $externalMetadataPath -Raw | ConvertFrom-Json -DateKind String
} catch { throw "External build-info.json is invalid: $($_.Exception.Message)" }
if ($outerMetadata.version -notmatch '\A[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?\z' -or
    $outerMetadata.sourceCommit -cne $ExpectedSourceCommit) {
    throw 'External build metadata does not match the selected successful build run.'
}

$extractRoot = Join-Path (Split-Path $ArtifactDirectory -Parent) "prototype-upload/$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipFile.FullName)
try {
    $rootPrefix = [System.IO.Path]::GetFullPath($extractRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    foreach ($entry in $archive.Entries) {
        if ($entry.FullName.StartsWith('/') -or $entry.FullName.StartsWith('\') -or $entry.FullName.Contains(':')) {
            throw "ZIP contains an absolute or device path: $($entry.FullName)"
        }
        $segments = $entry.FullName -split '[/\\]'
        if ($segments -contains '..') { throw "ZIP contains a path traversal entry: $($entry.FullName)" }
        $targetPath = [System.IO.Path]::GetFullPath((Join-Path $extractRoot ($entry.FullName.Replace('/', [System.IO.Path]::DirectorySeparatorChar))))
        if (-not $targetPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "ZIP entry resolves outside extraction directory: $($entry.FullName)"
        }
    }
} finally { $archive.Dispose() }
[System.IO.Compression.ZipFile]::ExtractToDirectory($zipFile.FullName, $extractRoot)
& (Join-Path $PSScriptRoot 'Test-PrototypePackage.ps1') -PackageDirectory $extractRoot

try {
    $innerMetadata = Get-Content -LiteralPath (Join-Path $extractRoot 'build-info.json') -Raw | ConvertFrom-Json -DateKind String
} catch { throw "Packaged build-info.json is invalid: $($_.Exception.Message)" }
foreach ($field in @('schemaVersion', 'version', 'sourceCommit', 'builtAtUtc', 'runtimeIdentifier', 'configuration', 'selfContained')) {
    if ([string]$innerMetadata.$field -cne [string]$outerMetadata.$field) {
        throw "The metadata inside the ZIP differs from the verified external file: $field"
    }
}
if ($innerMetadata.sourceCommit -cne $ExpectedSourceCommit) { throw 'Packaged source commit does not match the selected build run.' }

$butlerArguments = @(
    'push', $extractRoot, "${ItchProject}:windows-prototype", '--userversion', [string]$innerMetadata.version
)
& $ButlerPath @butlerArguments
if ($LASTEXITCODE -ne 0) { throw "butler push failed with exit code $LASTEXITCODE." }
Write-Output "Uploaded version $($innerMetadata.version) from commit $ExpectedSourceCommit to ${ItchProject}:windows-prototype (SHA-256 $actualHash)."
