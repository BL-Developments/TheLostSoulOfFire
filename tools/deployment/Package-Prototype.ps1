[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Version
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($Version.Length -gt 64 -or $Version -cnotmatch '\A[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?\z') {
    throw 'Version must match N.N.N or N.N.N-suffix (for example 0.1.0-prototype.1), with no v prefix.'
}
if ($env:OS -ne 'Windows_NT') {
    throw 'Prototype packages must be built on Windows.'
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$projectPath = Join-Path $repositoryRoot 'src/TheLostSoulOfFire/TheLostSoulOfFire.csproj'
$testerReadmePath = Join-Path $repositoryRoot 'docs/deployment/TESTER-README.md'
$outputRoot = Join-Path $repositoryRoot "artifacts/prototype/$Version"
$stagingPath = Join-Path $outputRoot 'staging'
$releasePath = Join-Path $outputRoot 'release'
$zipName = "TheLostSoulOfFire-$Version-win-x64.zip"

if (Test-Path -LiteralPath $outputRoot) {
    throw "Output already exists and will not be overwritten: $outputRoot"
}
if (-not (Test-Path -LiteralPath $testerReadmePath -PathType Leaf)) {
    throw "Tester instructions are missing: $testerReadmePath"
}

$sourceCommit = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceCommit -notmatch '\A[0-9a-f]{40}\z') {
    throw 'Could not resolve the full source commit SHA.'
}
$gitStatus = @(& git -C $repositoryRoot status --porcelain --untracked-files=normal)
if ($LASTEXITCODE -ne 0) {
    throw 'Could not inspect the Git working tree.'
}
if ($gitStatus.Count -gt 0) {
    throw 'Release packaging requires a clean Git working tree. Commit or remove all changes before retrying.'
}

New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null
New-Item -ItemType Directory -Path $releasePath -Force | Out-Null
$publishArguments = @(
    'publish', $projectPath, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
    '-p:PublishSingleFile=false', '-p:PublishTrimmed=false', '-p:PublishAot=false',
    '-p:PublishReadyToRun=false', '-p:TieredCompilation=false', '-o', $stagingPath
)
& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE. Partial output remains at $outputRoot."
}

$builtAtUtc = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
$metadata = [ordered]@{
    schemaVersion = 1
    version = $Version
    sourceCommit = $sourceCommit
    builtAtUtc = $builtAtUtc
    runtimeIdentifier = 'win-x64'
    configuration = 'Release'
    selfContained = $true
}
$metadataPath = Join-Path $stagingPath 'build-info.json'
$metadata | ConvertTo-Json | Set-Content -LiteralPath $metadataPath -Encoding utf8
Copy-Item -LiteralPath $testerReadmePath -Destination (Join-Path $stagingPath 'TESTER-README.md')
& (Join-Path $PSScriptRoot 'Test-PrototypePackage.ps1') -PackageDirectory $stagingPath


$dotnetInfoPath = Join-Path $releasePath 'dotnet-info.txt'
$dotnetInfo = & dotnet --info
$dotnetExitCode = $LASTEXITCODE
if ($dotnetExitCode -ne 0) { throw "dotnet --info failed with exit code $dotnetExitCode." }
$dotnetInfo | Set-Content -LiteralPath $dotnetInfoPath -Encoding utf8

$packagesPath = Join-Path $releasePath 'packages.txt'
$packageListing = & dotnet list $projectPath package --include-transitive
$packageExitCode = $LASTEXITCODE
if ($packageExitCode -ne 0) { throw "dotnet list package failed with exit code $packageExitCode." }
$packageListing | Set-Content -LiteralPath $packagesPath -Encoding utf8
Copy-Item -LiteralPath $metadataPath -Destination (Join-Path $releasePath 'build-info.json')

$zipPath = Join-Path $releasePath $zipName
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stagingPath, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$zipPath.sha256" -Value "$hash  $zipName" -Encoding ascii

Write-Output "Release candidate created: $zipPath"
Write-Output "SHA-256: $hash"
Write-Output "Source commit: $sourceCommit"
