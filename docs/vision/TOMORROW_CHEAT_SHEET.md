# Tomorrow Cheat Sheet

## 1. Start

```bash
git checkout main
git pull
git checkout -b prototype/soulfire-mvp
dotnet restore
dotnet build
```

## 2. Codex

Paste:

```text
Read docs/mvp/CODEX_AUTOPILOT.md, docs/mvp/CODEX_EXECUTION_PLAN.md and docs/vision/NEANDERTHAL_MODE_V2.md.

Implement the MVP on the current branch.
Leave .codex, .claude and openspec alone unless strictly necessary.
Use placeholders when assets are missing.
Build continuously.
Execute through Phase 04, then stop for gameplay review.
Do not commit unless I ask.
```

## 3. Play

```bash
dotnet run --project src/TheLostSoulOfFire
```

## 4. Decision

Feels bad:

```text
This feels bad: <normal human description>.
Compare against the specs and fix the tunable implementation/game-feel issues.
Do not invent new mechanics.
```

Feels good:

```text
Checkpoint approved.
Build, verify and commit.
Continue to the next review checkpoint.
```

## 5. Review Checkpoints

- Phase 04 — movement + Dash + Scythe
- Phase 08 — Hollow + Souls + Sense + Cannon
- Phase 12 — Burning + Devourer + Resonance + waves
- Phase 17 — finished prototype

## 6. VFX

While Codex works:

`docs/vision/VFX_RAPID_PRODUCTION.md`

Generate → drop into `Content/Textures/Effects` → tell Codex to integrate.

## 7. Final

```text
Final checkpoint approved.
Run the complete acceptance criteria, restore/build, fix required failures, and commit:
Complete Soulfire MVP prototype
```
