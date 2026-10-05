#!/usr/bin/env bash
# Archiviert OpenSpec-Changes nacheinander und öffnet dafür einen Folge-PR.
#
# Alle Changes eines Laufs landen auf einem Branch, in der angegebenen
# Reihenfolge. So bauen spätere Changes auf den Specs der früheren auf und die
# Folge-PRs geraten nicht in Konflikt, wenn mehrere Changes dieselbe Spec ändern.
#
# Erwartete Umgebung:
#   CHANGES       Change-Namen, getrennt durch Leerzeichen oder Zeilenumbrüche
#   BASE_BRANCH   Zielbranch der Folge-PRs (z. B. main)
#   SOURCE_PR     Nummer des gemergten PRs (optional, für Branch und Texte)
#   OPENSPEC      Aufruf der OpenSpec-CLI (z. B. "npx --yes @fission-ai/openspec@1.5.0")
#   GH_TOKEN      Token für gh und git push
set -euo pipefail

: "${BASE_BRANCH:?BASE_BRANCH fehlt}"
: "${OPENSPEC:?OPENSPEC fehlt}"
SUMMARY_FILE="${GITHUB_STEP_SUMMARY:-/dev/null}"

summary() {
  echo "$1"
  echo "- $1" >> "$SUMMARY_FILE"
}

echo "## OpenSpec-Archivierung" >> "$SUMMARY_FILE"

if [ -z "${CHANGES//[[:space:]]/}" ]; then
  summary "Keine Zeile \`OpenSpec-Archive:\` im PR-Text, nichts zu archivieren."
  exit 0
fi

if [ -n "${SOURCE_PR:-}" ]; then
  branch="chore/archive-pr-$SOURCE_PR"
else
  branch="chore/archive-run-${GITHUB_RUN_ID:-$(date +%s)}"
fi

open_pr="$(gh pr list --head "$branch" --base "$BASE_BRANCH" --state open --json url --jq '.[0].url // empty')"
if [ -n "$open_pr" ]; then
  summary "Archiv-PR ist bereits offen ($open_pr), nichts zu tun."
  exit 0
fi

git checkout --quiet -B "$branch" "origin/$BASE_BRANCH"

failed=0
archived=()
log="$(mktemp)"
for name in $CHANGES; do
  if [[ ! "$name" =~ ^[a-z0-9][a-z0-9-]*$ ]]; then
    summary "\`$name\`: ungültiger Change-Name, übersprungen."
    failed=1
    continue
  fi

  if [ ! -d "openspec/changes/$name" ]; then
    summary "\`$name\`: liegt nicht unter openspec/changes (schon archiviert oder entfernt), übersprungen."
    continue
  fi

  out="$(mktemp)"
  status=0
  $OPENSPEC archive "$name" --yes < /dev/null > "$out" 2>&1 || status=$?
  cat "$out"
  { echo "### $name"; sed '/collects anonymous usage stats/d' "$out"; echo; } >> "$log"

  # openspec beendet sich auch bei "Aborted." mit Status 0; der Ordner bleibt dann liegen.
  if [ "$status" -ne 0 ] || [ -d "openspec/changes/$name" ]; then
    summary "\`$name\`: \`openspec archive\` ist fehlgeschlagen, siehe Job-Log. Spätere Changes, die darauf aufbauen, schlagen ebenfalls fehl."
    failed=1
    git checkout --quiet -- openspec
    git clean --quiet -fd openspec
    continue
  fi

  git add -A openspec
  git commit --quiet -m "chore: archive OpenSpec change $name" \
    -m "Archiviert openspec/changes/$name und übernimmt die Spec-Deltas nach openspec/specs."
  archived+=("$name")
  summary "\`$name\`: archiviert."
done

if [ "${#archived[@]}" -eq 0 ]; then
  summary "Kein Change archiviert, kein Archiv-PR."
  exit "$failed"
fi

git push --quiet --force origin "HEAD:refs/heads/$branch"

if [ "${#archived[@]}" -eq 1 ]; then
  title="chore: archive OpenSpec change ${archived[0]}"
else
  title="chore: archive OpenSpec changes ${archived[*]}"
fi
list="$(printf '`%s`, ' "${archived[@]}")"
list="${list%, }"

source_line=""
if [ -n "${SOURCE_PR:-}" ]; then
  source_line="Folgt auf #$SOURCE_PR."
fi

body="$(mktemp)"
{
  echo "Vorher: Die OpenSpec-Changes $list liegen nach dem Merge noch unter \`openspec/changes/\`, die Haupt-Specs kennen ihre Anforderungen nicht."
  echo
  echo "Nachher: Die Changes liegen unter \`openspec/changes/archive/\`, ihre Spec-Deltas sind in dieser Reihenfolge in \`openspec/specs/\` übernommen."
  echo
  echo "$source_line Automatisch erstellt vom Workflow *OpenSpec archive* mit \`openspec archive <name> --yes\`, ein Commit pro Change."
  echo
  echo "<details><summary>Ausgabe von openspec archive</summary>"
  echo
  echo '```'
  cat "$log"
  echo '```'
  echo
  echo "</details>"
} > "$body"

if ! url="$(gh pr create --base "$BASE_BRANCH" --head "$branch" --title "$title" --body-file "$body" 2>&1)"; then
  echo "$url"
  summary "Branch \`$branch\` ist gepusht, aber der PR ließ sich nicht öffnen: $url"
  if [[ "$url" == *"not permitted to create or approve pull requests"* ]]; then
    summary "Abhilfe: Settings → Actions → General → Workflow permissions → \"Allow GitHub Actions to create and approve pull requests\" aktivieren oder das Secret OPENSPEC_ARCHIVE_TOKEN setzen. Bis dahin den PR für \`$branch\` von Hand öffnen."
  fi
  exit 1
fi
summary "Archiv-PR geöffnet: $url"

exit "$failed"
