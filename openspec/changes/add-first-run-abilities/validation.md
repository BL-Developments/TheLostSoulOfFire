# Validation — add-first-run-abilities

04.10.2026. Code is local in E:/Projects/TheLostSoulOfFire; no commit or PR was published by this task.

- **PASS** Release build and all 155 tests (including 15 new ability tests).
- **PASS** Native scripted solo run: `ABILITY_VISUAL_TEST_PASS sixCasts=true menu=true profile=isolated`.
- **PASS** Native 1920×1080 screenshot inspection: selection panel, Sog and Vorlage; six effect captures saved under artifacts/screenshots. Ability slots moved above the existing resonance bar.
- **PASS** `git diff --check` and `openspec validate add-first-run-abilities --strict`.
- **NOT_RUN** Human hands-on play and controller validation.
- **NOT_RUN** Local coop: no player-two implementation in this checkout; Vorlage uses solo follow-up.
- **NOT_RUN** Final balance; costs and effects deliberately use provisional values.

Reproduce: `dotnet test tests/TheLostSoulOfFire.Tests/TheLostSoulOfFire.Tests.csproj -c Release` and `dotnet run --project src/TheLostSoulOfFire -- --ability-visual-test`. Test harness isolates the profile and does not secure rewards into the real save.

Direct play: `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena`. C opens ability selection during intro/intermission; left/right chooses a slot, 1–6 equips, Enter closes. Z/X casts in combat.

Captures inspected:
- ../../../artifacts/screenshots/20261004_193236_402_ability_selection.png
- ../../../artifacts/screenshots/20261004_193245_105_ability_sog.png
- ../../../artifacts/screenshots/20261004_193249_130_ability_vorlage.png

The existing nullable warnings in audio/rendering/enemy files remain. Random draft, weapon selection, coop, persistent skilltrees and Ultimate are separate work.

