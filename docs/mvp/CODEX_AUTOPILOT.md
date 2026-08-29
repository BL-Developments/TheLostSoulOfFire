# Codex Autopilot — The Lost Soul of Fire MVP

You are implementing the complete MVP prototype for **The Lost Soul of Fire**.

Your job is to execute the existing design, not redesign it.

## Authoritative Sources

Before modifying code:

1. Read every Markdown file in `docs/mvp/`.
2. Treat those documents as authoritative.
3. Pay special attention to:
   - `00_MVP_VISION.md`
   - `17_TECHNICAL_ARCHITECTURE.md`
   - `18_MVP_ACCEPTANCE_CRITERIA.md`
   - `CODEX_EXECUTION_PLAN.md`

## Design Authority

All decisions marked:

`DESIGN STATUS: LOCKED`

are fixed.

Do not:

- replace them
- reinterpret them into a different mechanic
- remove them because they are inconvenient
- expand them into larger systems

Values explicitly marked:

`TUNABLE`

may be adjusted when required for balance, responsiveness, readability, or fun.

## Core Engineering Rule

> Implement the simplest solution that completely satisfies the specification.

And:

> Simplify implementation, not design.

Prefer:

- readable C#
- concrete classes
- small state machines
- primitive collision
- centralized balance values
- direct solutions

Avoid:

- speculative abstractions
- ECS
- behavior trees
- generic ability frameworks
- dependency-injection infrastructure
- generic factories/strategies for tiny fixed sets
- architecture for hypothetical future requirements

## Asset Rule

Final art is not required.

If an asset does not exist:

1. create/use a simple placeholder
2. preserve the specified silhouette/color/readability
3. continue implementation

Never block a required mechanic because final art, audio, or shaders are missing.

Use simple shapes, textures, particles, trails, overlays, and placeholder SFX when necessary.

## Scope Rule

Do not implement features outside the specs.

Specifically do not add:

- procedural generation
- hub
- meta progression
- skill tree
- inventory
- crafting
- quests
- dialogue system
- save system
- multiple weapons
- boss
- multiplayer
- achievements
- localization
- complex settings
- generic future-proof frameworks

Missing out-of-scope features are not defects.

## Execution

Execute `CODEX_EXECUTION_PLAN.md` sequentially from Phase 01 through Phase 17.

For every phase:

1. Read the phase.
2. Read all specs relevant to it.
3. Inspect the existing implementation.
4. Implement only the required scope.
5. Build.
6. Verify the phase behavior as far as the environment allows.
7. Fix failures/regressions.
8. Only then proceed.

If an earlier mechanic breaks:

> STOP → FIX → VERIFY → CONTINUE

Do not knowingly accumulate broken phases.

## Build Requirements

Use the repository's existing project structure and commands.

At minimum, repeatedly verify:

```bash
dotnet restore
dotnet build
```

Do not leave compilation errors for later phases.

## Gameplay Priorities

When tradeoffs are necessary, prioritize:

1. Complete required gameplay behavior
2. Combat readability
3. Responsive game feel
4. Soul Release identity
5. Death-Flame visual identity
6. Soul Sense readability
7. Resonance payoff
8. Enemy silhouette/readability
9. Environment detail

Do not spend hours polishing scenery while required gameplay remains incomplete.

## Critical Identity

The final prototype must communicate:

- fast Scythe combat
- heavy physical Soul Cannon
- responsive Ignition Dash
- Soul Sense revealing hidden Soul anatomy
- manifestations being defeated while actual Souls are released
- Devourer threatening exposed Souls
- successful releases building Resonance
- Resonance making the Player noticeably faster, more mobile, and stronger
- violet-white Flame of Death
- rare orange Flame of Life
- dark Gothic-industrial atmosphere

## Required Enemy Roles

Do not blur these roles:

### Hollow
Slow basic enemy that teaches the chest Soul Core.

### Burning
Fast unstable enemy with a Charge that can be detonated by the Soul Cannon to damage nearby enemies.

### Devourer
Slow heavy enemy that prioritizes exposed Souls, consumes them, and can have trapped Souls extracted with a fully charged Soul Cannon.

## Soul Rule

The Player never consumes actual Souls.

Correct:

`Soul released → Soul leaves world → Residue remains → Residue increases Resonance`

Incorrect:

`Player absorbs/eats Soul`

Preserve this distinction in visuals, naming, and code behavior.

## Resonance Rule

Resonance adds no new ability.

It enhances:

- movement
- mobility
- Scythe
- Dash
- Soul Cannon
- Soul Sense

The mobility improvement must be noticeable but not absurd.

## Completion Rule

Do not declare completion merely because the project builds.

Before finishing:

1. Read `18_MVP_ACCEPTANCE_CRITERIA.md` again.
2. Check every criterion.
3. Fix all required failures that can be reproduced or identified.
4. Run final restore/build.
5. Ensure restart and full wave progression are not obviously broken.

A required Acceptance Criterion may not be moved into "Known Limitations" simply to finish faster.

## Final Response

When all phases are complete, provide:

```text
IMPLEMENTED
- ...

VERIFIED
- ...

TUNED VALUES
- ...

KNOWN LIMITATIONS
- ...

OUT OF SCOPE
- ...

FILES CHANGED
- ...
```

Keep the report factual.

Do not propose a second MVP.

Do not add new features.

The task is complete when the locked MVP is implemented, playable, stable enough for prototype use, and satisfies the Acceptance Criteria.
