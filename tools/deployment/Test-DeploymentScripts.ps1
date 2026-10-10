[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testRoot = Join-Path $repositoryRoot "artifacts/deployment-tests/$([guid]::NewGuid().ToString('N'))"
$testCount = 0

function Assert-True([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw "Test failed: $Message" }
    $script:testCount++
}

function Assert-ScriptFails([scriptblock] $Action, [string] $MessageFragment, [string] $Name) {
    $failedAsExpected = $false
    try { & $Action } catch { $failedAsExpected = $_.Exception.Message.Contains($MessageFragment) }
    Assert-True $failedAsExpected $Name
}

function New-ValidPackage([string] $Path) {
    $files = @(
        'TheLostSoulOfFire.exe', 'TheLostSoulOfFire.dll', 'TheLostSoulOfFire.deps.json',
        'TheLostSoulOfFire.runtimeconfig.json', 'coreclr.dll', 'hostfxr.dll', 'hostpolicy.dll',
        'System.Private.CoreLib.dll', 'MonoGame.Framework.dll', 'MonoGame.Extended.dll',
        'Content/Audio/Sfx/scythe_swing_1.xnb', 'Content/Textures/Weapons/scythe_physical_256.xnb',
        'Content/Effects/SceneGrade.xnb', 'Content/Effects/SpriteLit.xnb', 'Content/Effects/Dissolve.xnb',
        'Content/Effects/DeathFlame.xnb', 'Content/Fonts/ui.xnb', 'TESTER-README.md',
        'SDL2.dll', 'openal.dll'
    )
    foreach ($file in $files) {
        $filePath = Join-Path $Path $file
        New-Item -ItemType Directory -Force -Path (Split-Path $filePath) | Out-Null
        Set-Content -LiteralPath $filePath -Value 'fixture' -Encoding ascii
    }
    $registryPath = Join-Path $Path 'Content/Visuals/registry.json'
    New-Item -ItemType Directory -Force -Path (Split-Path $registryPath) | Out-Null
    Set-Content -LiteralPath $registryPath -Value '{"version":1,"visuals":[]}' -Encoding ascii
    Set-Content -LiteralPath (Join-Path $Path 'TheLostSoulOfFire.runtimeconfig.json') -Value '{"runtimeOptions":{"tfm":"net9.0","includedFrameworks":[{"name":"Microsoft.NETCore.App","version":"9.0.0"}]}}' -Encoding ascii
    $metadata = [ordered]@{
        schemaVersion = 1; version = '0.1.0-prototype.1'; sourceCommit = ('a' * 40)
        builtAtUtc = '2026-01-01T00:00:00.000Z'; runtimeIdentifier = 'win-x64'
        configuration = 'Release'; selfContained = $true
    }
    $metadata | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Path 'build-info.json') -Encoding utf8
}


function New-UploadArtifact([string] $Path, [string] $PackagePath, [bool] $IncludeTraversal = $false) {
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
    Copy-Item -LiteralPath (Join-Path $PackagePath 'build-info.json') -Destination (Join-Path $Path 'build-info.json')
    $zipName = 'TheLostSoulOfFire-0.1.0-prototype.1-win-x64.zip'
    $zipPath = Join-Path $Path $zipName
    if ($IncludeTraversal) {
        $fileStream = [System.IO.File]::Open($zipPath, [System.IO.FileMode]::CreateNew)
        $archive = [System.IO.Compression.ZipArchive]::new($fileStream, [System.IO.Compression.ZipArchiveMode]::Create)
        try {
            $entry = $archive.CreateEntry('../escape.txt')
            $writer = [System.IO.StreamWriter]::new($entry.Open())
            try { $writer.Write('must not extract') } finally { $writer.Dispose() }
        } finally { $archive.Dispose(); $fileStream.Dispose() }
    } else {
        [System.IO.Compression.ZipFile]::CreateFromDirectory($PackagePath, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)
    }
    $hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -LiteralPath "$zipPath.sha256" -Value "$hash  $zipName" -Encoding ascii
    return $hash
}
function New-RunFixture([string] $Path, [string] $Branch = 'main', [string] $Workflow = '.github/workflows/prototype-build.yml', [string] $Conclusion = 'success') {
    $fixture = [ordered]@{
        status = 'completed'; conclusion = $Conclusion; event = 'workflow_dispatch'; head_branch = $Branch
        path = $Workflow; head_repository = @{ full_name = 'test-owner/test-game' }
        repository = @{ full_name = 'test-owner/test-game' }; head_sha = ('a' * 40)
    }
    $fixture | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Path -Encoding utf8
}

New-Item -ItemType Directory -Force -Path $testRoot | Out-Null
try {
    $validPackage = Join-Path $testRoot 'valid'
    New-ValidPackage $validPackage
    & (Join-Path $PSScriptRoot 'Test-PrototypePackage.ps1') -PackageDirectory $validPackage
    Assert-True $true 'a complete synthetic package is accepted'

    $missingRuntime = Join-Path $testRoot 'missing-runtime'
    Copy-Item -LiteralPath $validPackage -Destination $missingRuntime -Recurse
    Remove-Item -LiteralPath (Join-Path $missingRuntime 'coreclr.dll')
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Test-PrototypePackage.ps1') -PackageDirectory $missingRuntime } 'coreclr.dll' 'missing runtime is rejected by file name'

    $missingShader = Join-Path $testRoot 'missing-shader'
    Copy-Item -LiteralPath $validPackage -Destination $missingShader -Recurse
    Remove-Item -LiteralPath (Join-Path $missingShader 'Content/Effects/SceneGrade.xnb')
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Test-PrototypePackage.ps1') -PackageDirectory $missingShader } 'SceneGrade.xnb' 'missing shader is rejected by file name'

    $badJson = Join-Path $testRoot 'bad-json'
    Copy-Item -LiteralPath $validPackage -Destination $badJson -Recurse
    Set-Content -LiteralPath (Join-Path $badJson 'Content/Visuals/registry.json') -Value '{invalid' -Encoding ascii
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Test-PrototypePackage.ps1') -PackageDirectory $badJson } 'Package JSON is invalid' 'invalid registry JSON is rejected'

    $wrongArchitecture = Join-Path $testRoot 'wrong-architecture'
    Copy-Item -LiteralPath $validPackage -Destination $wrongArchitecture -Recurse
    Remove-Item -LiteralPath (Join-Path $wrongArchitecture 'SDL2.dll'), (Join-Path $wrongArchitecture 'openal.dll')
    $armDirectory = Join-Path $wrongArchitecture 'runtimes/win-arm64/native'
    New-Item -ItemType Directory -Force -Path $armDirectory | Out-Null
    Set-Content -LiteralPath (Join-Path $armDirectory 'SDL2.dll') -Value 'fixture'
    Set-Content -LiteralPath (Join-Path $armDirectory 'openal.dll') -Value 'fixture'
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Test-PrototypePackage.ps1') -PackageDirectory $wrongArchitecture } 'win-x64 native library' 'wrong RID native libraries are rejected'

    $frameworkDependent = Join-Path $testRoot 'framework-dependent'
    Copy-Item -LiteralPath $validPackage -Destination $frameworkDependent -Recurse
    Set-Content -LiteralPath (Join-Path $frameworkDependent 'TheLostSoulOfFire.runtimeconfig.json') -Value '{"runtimeOptions":{"tfm":"net9.0","framework":{"name":"Microsoft.NETCore.App","version":"9.0.0"}}}' -Encoding ascii
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Test-PrototypePackage.ps1') -PackageDirectory $frameworkDependent } 'separately installed shared .NET framework' 'framework-dependent runtime configuration is rejected'
    $badMetadata = Join-Path $testRoot 'bad-metadata'
    Copy-Item -LiteralPath $validPackage -Destination $badMetadata -Recurse
    $metadata = Get-Content -LiteralPath (Join-Path $badMetadata 'build-info.json') -Raw | ConvertFrom-Json -DateKind String
    $metadata.selfContained = $false
    $metadata | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $badMetadata 'build-info.json') -Encoding utf8
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Test-PrototypePackage.ps1') -PackageDirectory $badMetadata } 'invalid release metadata' 'non-self-contained metadata is rejected'

    $badVersionOutput = (& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Package-Prototype.ps1') -Version '../bad' 2>&1 | Out-String)
    Assert-True ($LASTEXITCODE -ne 0 -and $badVersionOutput.Contains('Version must match')) 'invalid package version fails before operating system or filesystem changes'

    $existingVersion = "0.0.0-deployment-script-test.$([guid]::NewGuid().ToString('N'))"
    $existingOutput = Join-Path $repositoryRoot "artifacts/prototype/$existingVersion"
    New-Item -ItemType Directory -Path $existingOutput | Out-Null
    try {
        $existingOutputResult = (& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Package-Prototype.ps1') -Version $existingVersion 2>&1 | Out-String)
        Assert-True ($LASTEXITCODE -ne 0 -and $existingOutputResult.Contains('Output already exists')) 'existing version directory is rejected before packaging'
        Assert-True (Test-Path -LiteralPath $existingOutput) 'existing output was not deleted or overwritten'
    } finally { Remove-Item -LiteralPath $existingOutput -Recurse -Force }

    $runJson = Join-Path $testRoot 'run.json'
    New-RunFixture $runJson
    $runCommit = & (Join-Path $PSScriptRoot 'Test-PrototypeBuildRun.ps1') -RunJsonPath $runJson -ExpectedRepository 'test-owner/test-game'
    Assert-True ($runCommit -ceq ('a' * 40)) 'successful main prototype run returns its source commit'
    New-RunFixture $runJson -Branch 'feature/deploy'
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Test-PrototypeBuildRun.ps1') -RunJsonPath $runJson -ExpectedRepository 'test-owner/test-game' } 'successful manual prototype build' 'non-main run is rejected'
    New-RunFixture $runJson -Workflow '.github/workflows/ci.yml'
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Test-PrototypeBuildRun.ps1') -RunJsonPath $runJson -ExpectedRepository 'test-owner/test-game' } 'successful manual prototype build' 'wrong workflow is rejected'
    New-RunFixture $runJson -Conclusion 'failure'
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Test-PrototypeBuildRun.ps1') -RunJsonPath $runJson -ExpectedRepository 'test-owner/test-game' } 'successful manual prototype build' 'failed workflow run is rejected'
    New-RunFixture $runJson
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Test-PrototypeBuildRun.ps1') -RunJsonPath $runJson -ExpectedRepository 'another-owner/repo' } 'successful manual prototype build' 'foreign repository run is rejected'


    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $stubPath = Join-Path $testRoot 'butler-stub.cmd'
    $stubLog = Join-Path $testRoot 'butler-arguments.txt'
    [System.IO.File]::WriteAllLines($stubPath, @('@echo off', '> "%MOCK_BUTLER_LOG%" echo %*', 'exit /b %MOCK_BUTLER_EXIT%'))
    $env:BUTLER_API_KEY = 'test-only-placeholder'
    $env:MOCK_BUTLER_LOG = $stubLog
    $env:MOCK_BUTLER_EXIT = '0'

    $uploadDirectory = Join-Path $testRoot 'upload-valid'
    $validHash = New-UploadArtifact $uploadDirectory $validPackage
    & (Join-Path $PSScriptRoot 'Upload-Prototype.ps1') -ArtifactDirectory $uploadDirectory -ExpectedSourceCommit ('a' * 40) -ExpectedZipSha256 $validHash -ItchProject 'test-owner/test-game' -ButlerPath $stubPath
    Assert-True ($LASTEXITCODE -eq 0 -and (Test-Path -LiteralPath $stubLog)) 'verified package reaches butler exactly once'
    $butlerArguments = Get-Content -LiteralPath $stubLog -Raw
    Assert-True ($butlerArguments.Contains('test-owner/test-game:windows-prototype') -and $butlerArguments.Contains('0.1.0-prototype.1')) 'butler receives the selected project channel and package version'


    $ambiguousDirectory = Join-Path $testRoot 'upload-ambiguous'
    $ambiguousHash = New-UploadArtifact $ambiguousDirectory $validPackage
    Copy-Item -LiteralPath (Get-ChildItem -LiteralPath $ambiguousDirectory -Filter '*.zip' -File | Select-Object -First 1).FullName -Destination (Join-Path $ambiguousDirectory 'second.zip')
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Upload-Prototype.ps1') -ArtifactDirectory $ambiguousDirectory -ExpectedSourceCommit ('a' * 40) -ExpectedZipSha256 $ambiguousHash -ItchProject 'test-owner/test-game' -ButlerPath $stubPath } 'exactly one ZIP' 'ambiguous artifact is rejected'
    $tamperedDirectory = Join-Path $testRoot 'upload-tampered'
    $tamperedHash = New-UploadArtifact $tamperedDirectory $validPackage
    $tamperedZip = Get-ChildItem -LiteralPath $tamperedDirectory -Filter '*.zip' -File | Select-Object -First 1
    Add-Content -LiteralPath $tamperedZip.FullName -Value 'modified'
    Remove-Item -LiteralPath $stubLog -Force
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Upload-Prototype.ps1') -ArtifactDirectory $tamperedDirectory -ExpectedSourceCommit ('a' * 40) -ExpectedZipSha256 $tamperedHash -ItchProject 'test-owner/test-game' -ButlerPath $stubPath } 'SHA-256' 'tampered ZIP is rejected before butler'
    Assert-True (-not (Test-Path -LiteralPath $stubLog)) 'tampered ZIP never invokes butler'

    $wrongCommitDirectory = Join-Path $testRoot 'upload-wrong-commit'
    $wrongCommitHash = New-UploadArtifact $wrongCommitDirectory $validPackage
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Upload-Prototype.ps1') -ArtifactDirectory $wrongCommitDirectory -ExpectedSourceCommit ('b' * 40) -ExpectedZipSha256 $wrongCommitHash -ItchProject 'test-owner/test-game' -ButlerPath $stubPath } 'metadata does not match' 'different selected source commit is rejected'
    Assert-True (-not (Test-Path -LiteralPath $stubLog)) 'wrong commit never invokes butler'

    $metadataPackage = Join-Path $testRoot 'different-inner-metadata'
    Copy-Item -LiteralPath $validPackage -Destination $metadataPackage -Recurse
    $innerBuildInfo = Get-Content -LiteralPath (Join-Path $metadataPackage 'build-info.json') -Raw | ConvertFrom-Json -DateKind String
    $innerBuildInfo.version = '0.1.0-prototype.2'
    $innerBuildInfo | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $metadataPackage 'build-info.json') -Encoding utf8
    $metadataMismatchDirectory = Join-Path $testRoot 'upload-metadata-mismatch'
    $metadataMismatchHash = New-UploadArtifact $metadataMismatchDirectory $metadataPackage
    Copy-Item -LiteralPath (Join-Path $validPackage 'build-info.json') -Destination (Join-Path $metadataMismatchDirectory 'build-info.json') -Force
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Upload-Prototype.ps1') -ArtifactDirectory $metadataMismatchDirectory -ExpectedSourceCommit ('a' * 40) -ExpectedZipSha256 $metadataMismatchHash -ItchProject 'test-owner/test-game' -ButlerPath $stubPath } 'differs from the verified external file' 'inner and outer build metadata must agree'
    Assert-True (-not (Test-Path -LiteralPath $stubLog)) 'metadata mismatch never invokes butler'
    $traversalDirectory = Join-Path $testRoot 'upload-traversal'
    $traversalHash = New-UploadArtifact $traversalDirectory $validPackage -IncludeTraversal $true
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Upload-Prototype.ps1') -ArtifactDirectory $traversalDirectory -ExpectedSourceCommit ('a' * 40) -ExpectedZipSha256 $traversalHash -ItchProject 'test-owner/test-game' -ButlerPath $stubPath } 'path traversal' 'ZIP traversal entry is rejected'
    Assert-True (-not (Test-Path -LiteralPath $stubLog)) 'traversal ZIP never invokes butler'

    $env:MOCK_BUTLER_EXIT = '17'
    $failedButlerDirectory = Join-Path $testRoot 'upload-butler-failure'
    $failedButlerHash = New-UploadArtifact $failedButlerDirectory $validPackage
    Assert-ScriptFails { & (Join-Path $PSScriptRoot 'Upload-Prototype.ps1') -ArtifactDirectory $failedButlerDirectory -ExpectedSourceCommit ('a' * 40) -ExpectedZipSha256 $failedButlerHash -ItchProject 'test-owner/test-game' -ButlerPath $stubPath } 'butler push failed' 'butler failure propagates as upload failure'
    Write-Output "Deployment script checks passed: $testCount assertions."
    $global:LASTEXITCODE = 0
    # GitHub's built-in pwsh shell returns the last native command's code.
} finally {
    Remove-Item -LiteralPath $testRoot -Recurse -Force -ErrorAction SilentlyContinue
}
