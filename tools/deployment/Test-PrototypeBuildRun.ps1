[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $RunJsonPath,
    [Parameter(Mandatory = $true)] [string] $ExpectedRepository
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$run = Get-Content -LiteralPath $RunJsonPath -Raw | ConvertFrom-Json -DateKind String
if ($run.status -ne 'completed' -or $run.conclusion -ne 'success' -or $run.event -ne 'workflow_dispatch' -or
    $run.head_branch -ne 'main' -or $run.path -ne '.github/workflows/prototype-build.yml' -or
    $run.head_repository.full_name -ne $ExpectedRepository -or $run.repository.full_name -ne $ExpectedRepository -or
    $run.head_sha -notmatch '\A[0-9a-f]{40}\z') {
    throw 'The selected run is not a successful manual prototype build from main in this repository.'
}

Write-Output $run.head_sha
