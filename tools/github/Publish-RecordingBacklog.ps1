#requires -Version 7.0
param(
    [switch]$ValidateOnly,
    [string]$PlanDirectory = (Join-Path $PSScriptRoot '../../docs/planning/recording-2026-09-08')
)

# Publishes only the reviewed recording plan to its explicitly named repository.
# Authentication stays with gh; credentials are never read or stored by this script.
$ErrorActionPreference = 'Stop'
$OutputEncoding = New-Object System.Text.UTF8Encoding($false)
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$planPath = Join-Path $PlanDirectory 'backlog.json'
$overviewPath = Join-Path $PlanDirectory 'overview.md'
$plan = Get-Content -LiteralPath $planPath -Encoding UTF8 -Raw | ConvertFrom-Json
$overviewTemplate = Get-Content -LiteralPath $overviewPath -Encoding UTF8 -Raw
if ($plan.repository -ne 'BL-Developments/TheLostSoulOfFire') {
    throw 'Unexpected repository. Review the target before publishing.'
}
$repo = $plan.repository
$markerPrefix = 'recording-plan:' + $plan.recording_import_date
$milestoneIds = @($plan.milestones | ForEach-Object { $_.id })
$itemIds = @($plan.items | ForEach-Object { $_.id })
if (@($itemIds | Select-Object -Unique).Count -ne $itemIds.Count -or
    @($milestoneIds | Select-Object -Unique).Count -ne $milestoneIds.Count) {
    throw 'Duplicate plan IDs.'
}
foreach ($item in $plan.items) {
    if ($item.milestone -notin $milestoneIds) { throw "Unknown milestone: $($item.id)" }
    foreach ($dependency in $item.depends) {
        if ($dependency -notin $itemIds) { throw "Unknown dependency: $dependency" }
    }
    if (-not $item.title -or @($item.acceptance).Count -eq 0) {
        throw "Missing title or acceptance criteria: $($item.id)"
    }
}
if (-not $overviewTemplate.Contains('<!-- MILESTONE_LINKS -->') -or
    -not $overviewTemplate.Contains('<!-- PUBLISH_STATUS -->')) {
    throw 'Overview publication markers are missing.'
}
# Topological order makes every dependency a real issue link at creation time.
$ordered = @()
$pending = @($plan.items)
while ($pending.Count -gt 0) {
    $doneIds = @($ordered | ForEach-Object { $_.id })
    $ready = @($pending | Where-Object {
        @($_.depends | Where-Object { $_ -notin $doneIds }).Count -eq 0
    })
    if ($ready.Count -eq 0) { throw 'Dependency cycle in recording plan.' }
    $ordered += $ready
    $readyIds = @($ready | ForEach-Object { $_.id })
    $pending = @($pending | Where-Object { $_.id -notin $readyIds })
}
if ($ValidateOnly) {
    [pscustomobject]@{
        status = 'PASS'; repository = $repo
        milestones = $plan.milestones.Count; work_issues = $plan.items.Count
        overview_issues = 1; categories = @($plan.items.category | Select-Object -Unique).Count
        dependency_order = @($ordered.id); remote_writes = 0
    } | ConvertTo-Json -Depth 5
    return
}
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw 'GitHub CLI (gh) is required.' }
& gh auth status
if ($LASTEXITCODE -ne 0) { throw 'Run gh auth login, then rerun this script.' }

function Invoke-PlanningGitHub {
    param([string]$Endpoint, [string]$Method = 'GET', [object]$Body = $null)
    $requestArgs = @('api', $Endpoint, '--method', $Method)
    if ($null -ne $Body) {
        $requestArgs += @('--input', '-')
        $requestJson = $Body | ConvertTo-Json -Depth 20 -Compress
        $responseText = $requestJson | & gh @requestArgs
    } else {
        $responseText = & gh @requestArgs
    }
    if ($LASTEXITCODE -ne 0) {
        throw "GitHub request failed: $Method $Endpoint. Rerunning reuses already-created plan entries."
    }
    if ($responseText) { return ($responseText -join "`n" | ConvertFrom-Json) }
}
function Get-PlanningCollection {
    param([string]$Endpoint)
    $all = @()
    $page = 1
    do {
        $separator = if ($Endpoint.Contains('?')) { '&' } else { '?' }
        $batch = @(Invoke-PlanningGitHub ($Endpoint + $separator + "per_page=100&page=$page"))
        $all += $batch
        $page++
    } while ($batch.Count -eq 100)
    return $all
}
function Get-SinglePlanIssue {
    param([string]$Marker)
    $matches = @($script:existingIssues | Where-Object { $_.body -and $_.body.Contains($Marker) })
    if ($matches.Count -gt 1) { throw "Multiple issues with marker $Marker; review before publishing." }
    if ($matches.Count -eq 1) { return $matches[0] }
}
function Add-PlanIssue {
    param([hashtable]$Payload)
    $created = Invoke-PlanningGitHub "repos/$repo/issues" 'POST' $Payload
    $script:existingIssues += $created
    return $created
}

# Read existing state before mutation; never delete or close existing material.
$existingMilestones = @(Get-PlanningCollection "repos/$repo/milestones?state=all")
$existingLabels = @(Get-PlanningCollection "repos/$repo/labels")
$script:existingIssues = @(Get-PlanningCollection "repos/$repo/issues?state=all" |
    Where-Object { -not $_.pull_request })
$remoteRepo = Invoke-PlanningGitHub "repos/$repo"
if ($remoteRepo.full_name -ne $repo -or $remoteRepo.archived) { throw 'Repository identity or archive state changed.' }
$milestoneMap = @{}
foreach ($milestone in $plan.milestones) {
    $matches = @($existingMilestones | Where-Object { $_.title -ceq $milestone.title })
    if ($matches.Count -gt 1) { throw "Duplicate milestone title: $($milestone.title)" }
    if ($matches.Count -eq 1) {
        $remoteMilestone = $matches[0]
    } else {
        $remoteMilestone = Invoke-PlanningGitHub "repos/$repo/milestones" 'POST' @{
            title = $milestone.title; description = $milestone.description
        }
    }
    $milestoneMap[$milestone.id] = $remoteMilestone
    Write-Host "Milestone $($milestone.id): $($remoteMilestone.html_url)"
}
$categoryLabels = @{}
foreach ($category in ($plan.items.category | Select-Object -Unique)) {
    $labelName = 'Bereich: ' + $category
    if ($labelName -notin $existingLabels.name) {
        $null = Invoke-PlanningGitHub "repos/$repo/labels" 'POST' @{
            name = $labelName; color = '7057ff'; description = "Konzeptaufnahme: $category"
        }
    }
    $categoryLabels[$category] = $labelName
}
$overviewMarker = "<!-- $markerPrefix overview -->"
$overview = Get-SinglePlanIssue $overviewMarker
if (-not $overview) {
    $initialBody = $overviewTemplate.Replace('<!-- MILESTONE_LINKS -->', 'Die Arbeits-Issues werden in diesem Importlauf verknüpft.').Replace('<!-- PUBLISH_STATUS -->', 'Übertragung läuft; vorhandene Einträge werden bei Wiederholung wiederverwendet.')
    $overview = Add-PlanIssue @{
        title = '[Planung] Aufnahme: Zusammenfassung, Lore-Abgleich und Entwicklungsfahrplan'
        body = $initialBody; labels = @('documentation')
    }
}
$issueMap = @{}
foreach ($item in $ordered) {
    $marker = "<!-- $markerPrefix $($item.id) -->"
    $typeLabel = if ($item.type -eq 'design') { 'documentation' } else { 'enhancement' }
    $requiredLabels = @($typeLabel, $categoryLabels[$item.category])
    $remoteIssue = Get-SinglePlanIssue $marker
    if (-not $remoteIssue) {
        $milestone = $milestoneMap[$item.milestone]
        $criteria = ($item.acceptance | ForEach-Object { '- [ ] ' + $_ }) -join "`n"
        $dependencies = if (@($item.depends).Count -gt 0) {
            ($item.depends | ForEach-Object { '#' + $issueMap[$_].number }) -join ', '
        } else { 'Keine vorgelagerte Arbeitsaufgabe.' }
        $body = "## Ziel und Grundlage`n`n$($item.context)`n`n"
        $body += "Kategorie: **$($item.category)** · Typ: **$($item.type)**`n`n"
        $body += "Meilenstein: [$($milestone.title)]($($milestone.html_url))`n`n"
        $body += "Quelle und Lore-Abgleich: #$($overview.number). Aufnahme hat Vorrang; Beispiele sind keine festen Balancewerte.`n`n"
        $body += "## Abnahmekriterien`n`n$criteria`n`n## Abhängigkeiten`n`n$dependencies`n"
        if ($item.open) { $body += "`n## Offene Ausgestaltung`n`n$($item.open)`n" }
        $body += "`n## Umsetzungshinweis`n`nVorhandenes C#/.NET-9-/MonoGame-Spiel und Content-Pipeline weiterverwenden. Dies ist geplante Arbeit, keine Behauptung einer vorhandenen Implementierung. Bei Spieländerungen relevante deterministische Checks, echte Solo-/Koop-Prüfung und bei visuellen Änderungen native Captures liefern; Ergebnisse als PASS/FAIL/NOT_RUN dokumentieren.`n`n$marker"
        $remoteIssue = Add-PlanIssue @{
            title = "[$($item.category)] $($item.title)"; body = $body
            labels = $requiredLabels; milestone = $milestone.number
        }
    }
    if ($remoteIssue.milestone.number -ne $milestoneMap[$item.milestone].number) {
        $remoteIssue = Invoke-PlanningGitHub "repos/$repo/issues/$($remoteIssue.number)" 'PATCH' @{
            milestone = $milestoneMap[$item.milestone].number
        }
    }
    $missingLabels = @($requiredLabels | Where-Object { $_ -notin $remoteIssue.labels.name })
    if ($missingLabels.Count -gt 0) {
        $null = Invoke-PlanningGitHub "repos/$repo/issues/$($remoteIssue.number)/labels" 'POST' @{
            labels = $missingLabels
        }
    }
    $issueMap[$item.id] = $remoteIssue
    Write-Host "Issue $($item.id): $($remoteIssue.html_url)"
}
$sections = foreach ($milestone in $plan.milestones) {
    $remote = $milestoneMap[$milestone.id]
    $section = "### [$($milestone.title)]($($remote.html_url))`n`n$($milestone.description)`n"
    foreach ($item in ($plan.items | Where-Object { $_.milestone -eq $milestone.id })) {
        $remoteIssue = $issueMap[$item.id]
        $tick = if ($remoteIssue.state -eq 'closed') { 'x' } else { ' ' }
        $section += "`n- [$tick] #$($remoteIssue.number) — $($item.title)"
    }
    $section
}
$finalBody = $overviewTemplate.Replace('<!-- MILESTONE_LINKS -->', ($sections -join "`n`n")).Replace('<!-- PUBLISH_STATUS -->', '**GitHub-Status:** 6 Meilensteine und 24 zugeordnete Arbeits-Issues angelegt bzw. wiederverwendet, zusätzlich dieses Übersichts-Issue.')
$overview = Invoke-PlanningGitHub "repos/$repo/issues/$($overview.number)" 'PATCH' @{ body = $finalBody }

# Verify exact returned IDs and assignments; never infer success from exit status alone.
foreach ($item in $plan.items) {
    $verified = Invoke-PlanningGitHub "repos/$repo/issues/$($issueMap[$item.id].number)"
    $expectedMarker = "<!-- $markerPrefix $($item.id) -->"
    if (-not $verified.body.Contains($expectedMarker) -or
        $verified.milestone.number -ne $milestoneMap[$item.milestone].number -or
        $categoryLabels[$item.category] -notin $verified.labels.name) {
        throw "Verification failed for $($item.id)."
    }
}
$result = [ordered]@{
    status = 'PASS'; repository = $repo; overview_url = $overview.html_url
    milestones = @($plan.milestones | ForEach-Object {
        @{ id = $_.id; number = $milestoneMap[$_.id].number; url = $milestoneMap[$_.id].html_url }
    })
    issues = @($plan.items | ForEach-Object {
        @{ id = $_.id; number = $issueMap[$_.id].number; url = $issueMap[$_.id].html_url }
    })
}
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PlanDirectory 'published.json') -Encoding UTF8
$result | ConvertTo-Json -Depth 8
