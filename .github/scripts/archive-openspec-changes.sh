#!/usr/bin/env bash
# Archiviert OpenSpec-Changes und öffnet pro Change einen Folge-PR.
#
# Erwartete Umgebung:
#   CHANGES       Change-Namen, getrennt durch Leerzeichen oder Zeilenumbrüche
#   BASE_BRANCH   Zielbranch der Folge-PRs (z. B. main)
#   SOURCE_PR     Nummer des gemergten PRs (optional, nur für Texte)
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

failed=0
for name in $CHANGES; do
  if [[ ! "$name" =~ ^[a-z0-9][a-z0-9-]*$ ]]; then
    summary "\`$name\`: ungültiger Change-Name, übersprungen."
    continue
  fi

  git checkout --quiet --detach "origin/$BASE_BRANCH"

  if [ ! -d "openspec/changes/$name" ]; then
    summary "\`$name\`: liegt nicht mehr unter openspec/changes (schon archiviert oder entfernt), übersprungen."
    continue
  fi

  branch="chore/archive-$name"
  open_pr="$(gh pr list --head "$branch" --base "$BASE_BRANCH" --state open --json url --jq '.[0].url // empty')"
  if [ -n "$open_pr" ]; then
    summary "\`$name\`: Archiv-PR ist bereits offen ($open_pr), übersprungen."
    continue
  fi

  git checkout --quiet -B "$branch" "origin/$BASE_BRANCH"

  log="$(mktemp)"
  if ! $OPENSPEC archive "$name" --yes < /dev/null 2>&1 | tee "$log"; then
    summary "\`$name\`: \`openspec archive\` ist fehlgeschlagen, siehe Job-Log."
    failed=1
    git reset --quiet --hard
    git clean --quiet -fd
    continue
  fi

  git add -A openspec
  if git diff --cached --quiet; then
    summary "\`$name\`: OpenSpec hat nichts verändert, übersprungen."
    continue
  fi

  git commit --quiet -m "chore: archive OpenSpec change $name" \
    -m "Archiviert openspec/changes/$name und übernimmt die Spec-Deltas nach openspec/specs."
  git push --quiet --force origin "HEAD:refs/heads/$branch"

  source_line=""
  if [ -n "${SOURCE_PR:-}" ]; then
    source_line="Folgt auf #$SOURCE_PR."
  fi

  body="$(mktemp)"
  {
    echo "Vorher: Der OpenSpec-Change \`$name\` liegt nach dem Merge noch unter \`openspec/changes/\`, die Haupt-Specs kennen seine Anforderungen nicht."
    echo
    echo "Nachher: Der Change liegt unter \`openspec/changes/archive/\`, seine Spec-Deltas sind in \`openspec/specs/\` übernommen."
    echo
    echo "$source_line Automatisch erstellt vom Workflow *OpenSpec archive* mit \`openspec archive $name --yes\`."
    echo
    echo "<details><summary>Ausgabe von openspec archive</summary>"
    echo
    echo '```'
    sed '/collects anonymous usage stats/d' "$log"
    echo '```'
    echo
    echo "</details>"
  } > "$body"

  url="$(gh pr create --base "$BASE_BRANCH" --head "$branch" \
    --title "chore: archive OpenSpec change $name" --body-file "$body")"
  summary "\`$name\`: Archiv-PR geöffnet: $url"
done

exit "$failed"
