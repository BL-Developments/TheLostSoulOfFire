# CODEX AUTOPILOT — CURRENT REPO ADDENDUM

> **Historical.** The repository is no longer a minimal starter or greenfield implementation. See [`../current/CURRENT-SLICE.md`](../current/CURRENT-SLICE.md) and the root `AGENTS.md` before making changes.

Apply this addendum together with `docs/mvp/CODEX_AUTOPILOT.md`.

## Current Main Baseline

At the time this addendum was written, the repository's actual game implementation was still a minimal MonoGame starter.

Do not treat the current branch as greenfield gameplay implementation.

Do not waste time trying to preserve nonexistent gameplay architecture.

## Preserve

Preserve:

- `.NET 9`
- MonoGame DesktopGL
- existing project/solution
- existing content pipeline
- repository root structure where practical

## Leave Unrelated Agent Tooling Alone

Do not reorganize or delete:

- `.codex/`
- `.claude/`
- `openspec/`

They are outside the MVP runtime.

## Implementation Home

Put runtime implementation primarily beneath:

`src/TheLostSoulOfFire/`

Create simple folders/classes as needed according to `17_TECHNICAL_ARCHITECTURE.md`.

## Human Review Gates

When running in interactive development mode, stop for human gameplay review after:

- Phase 04
- Phase 08
- Phase 12
- Phase 17

Do not ask for code review.

At each gate provide:

- how to run the game
- what gameplay is ready to test
- any known blocker
- build status

Do not commit automatically unless instructed.

## Verification

The standard run command is:

```bash
dotnet run --project src/TheLostSoulOfFire
```

Use:

```bash
dotnet restore
dotnet build
```

for baseline verification.

## Principle

The human evaluates the product through gameplay.

Codex evaluates implementation correctness through code/build/runtime verification.
