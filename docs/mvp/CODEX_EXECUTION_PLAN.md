# Codex Execution Plan

**STATUS: LOCKED**

Implement the MVP sequentially.

Every phase must be built and verified before moving to the next phase.

## Global Rules

The Markdown documents in `docs/mvp/` are authoritative.

### Locked vs Tunable

- `DESIGN STATUS: LOCKED` decisions may not be redesigned.
- Values explicitly marked `TUNABLE` may be adjusted for balance and feel.

### Mandatory Behavior

- Read all relevant specs before modifying code.
- Do not invent new gameplay systems.
- Do not expand MVP scope.
- Do not replace a specified mechanic because another mechanic is easier.
- Choose the simplest implementation compatible with the specification.
- Simplify implementation, not design.
- Prefer simple readable C# over speculative abstractions.
- Do not introduce architecture for hypothetical future requirements.
- Placeholder assets are allowed.
- Missing final art must never block implementation.
- Build and verify each phase before proceeding.
- If an earlier phase breaks: stop, fix it, verify again, then continue.

---

# Phase 01 — Foundation

## Goal

Turn the MonoGame skeleton into a minimal game structure.

## Implement

- GameWorld
- GameBalance
- InputState
- Camera2D
- basic Arena
- debug-system foundation
- placeholder rendering helpers if needed

Do not implement combat yet.

## Verification

```bash
dotnet restore
dotnet build
```

Game launches.

Arena is visible.

No exceptions.

---

# Phase 02 — Player Movement

## Implement

- Player
- WASD
- mouse-facing
- Camera follow
- HP
- Player placeholder
- visible/faint Death Flame Core
- simple idle particles

## Verification

- Player moves cleanly.
- Movement feels direct and fast.
- Facing works independently of movement.

---

# Phase 03 — Ignition Dash

## Implement

- Space input
- directional Dash
- facing fallback
- cooldown
- short i-frames
- enemy pass-through behavior
- Death-Flame trail
- 2–3 afterimages
- small camera kick

## Verification

- Dash is clearly faster than normal movement.
- Cooldown prevents uncontrolled spam.
- Dash deals no damage.

---

# Phase 04 — Scythe Combat

## Implement

- visible Scythe
- mouse aim
- three-hit combo
- combo reset
- attack movement
- hit detection
- damage
- knockback
- trails
- hitstop
- Soul Cleave impact

A dummy target may be used temporarily.

## Verification

The combo clearly reads:

> SWISH → SWISH → BOOM

If all three hits feel identical, continue polishing before moving on.

---

# Phase 05 — Combat Foundation + Hollow

## Implement

- Enemy base
- DamageInfo or equivalent
- Hollow
- pursuit
- Swipe Telegraph
- Swipe
- Player damage
- Hollow damage/death
- basic ScreenEffects

## Verification

- Multiple Hollows can fight Player.
- Player can die.
- Hollows can die.
- Hollow attack is readable.

---

# Phase 06 — Soul System + Soul Release

## Implement

`Enemy dies → Soul → Exposed → Release → Residue → Player`

Include:

- Soul states
- Release delay
- Death-Flame connection
- peaceful release VFX
- Resonance gain hook

## Verification

- Each normal Hollow death creates exactly one Soul.
- Soul Release is visually distinct from enemy death.
- Release completes without Devourer present.

---

# Phase 07 — Soul Sense

## Implement

- Q Hold
- dark/desaturated world treatment
- Player eye glow
- Hollow Core
- Core hit detection
- bonus damage
- bonus Resonance contribution
- movement penalty
- Dash compatibility

Use overlay fallback instead of blocking on shaders.

## Verification

Without Soul Sense:

- Hollow Core is not visible.

With Soul Sense:

- Core is immediately readable.

---

# Phase 08 — Physical Soul Cannon

## Implement

- Cannon visible on back
- draw transition
- RMB Hold
- continuous charge
- three charge stages
- full-charge cue
- fire on release
- projectile/beam
- recoil
- movement penalty
- return to back
- Hollow Core full-shot stagger

## Verification

Cannon must feel fundamentally different from Scythe:

- Scythe = fast
- Cannon = heavy

---

# Phase 09 — Burning

## Implement

- Burning visuals
- AI states
- Charge Telegraph
- Charge
- miss Recovery
- Soul Fractures
- Cannon-during-Charge detection
- detonation
- enemy-only AoE damage
- Soul remains

## Verification

Required interaction:

`Burning charges → Cannon hit → Detonation → nearby Hollow takes damage`

Player takes no damage from this detonation.

---

# Phase 10 — Devourer

## Implement

- heavy silhouette
- Heavy Slam
- Player targeting
- Soul targeting
- Soul priority
- Devour
- heal/buff
- trapped Souls in Soul Sense
- full-Cannon Soul extraction
- death releases remaining Souls

## Verification A

`Hollow dies → Soul exposed → Devourer retargets → approaches Soul → attempts Devour`

## Verification B

`Soul consumed → Soul Sense reveals it → Full Cannon hits Devourer → Soul expelled → Soul can release`

Do not proceed until both work.

---

# Phase 11 — Resonance

## Implement

- meter
- successful-release gain
- Full state
- Ready cue
- R activation
- transformation
- duration
- movement/mobility buff
- Scythe buff
- Dash buff
- Cannon buff
- automatic Soul Sense
- removal of Soul Sense movement penalty
- return to normal

## Verification

The Player must feel noticeably faster and more mobile.

The effect must be meaningful but not absurdly overpowered.

---

# Phase 12 — Waves + Full Arena Loop

## Implement

### Wave 1
- 3 Hollow

### Wave 2
- 2 Hollow
- 2 Burning

### Wave 3
- 2 Hollow
- 2 Burning
- 1 Devourer

### Final Wave
- mixed heavy encounter

Also:

- Wave transitions
- arena start/gates
- restart
- progression
- difficulty tuning

## Verification

A complete run from start through the final combat encounter works without debug intervention.

---

# Phase 13 — Arena Art Pass

## Implement

Improve placeholder arena with:

- furnaces
- pipes
- Gothic arches
- chains
- broken machinery
- asymmetry
- foreground/background shapes
- subtle ambient Soul/Death-Flame effects

Do not change gameplay.

## Verification

A screenshot should communicate:

> Dark Gothic Industrial

not:

> MonoGame collision test.

---

# Phase 14 — VFX + Audio Polish

## Review and Improve

- Scythe trails
- hit particles
- hitstop
- camera shake
- Cannon charge
- Cannon blast
- Dash
- Burning explosion
- Soul Sense
- Soul Release
- Resonance transformation
- enemy telegraphs
- placeholder SFX
- ambience/music

Priority:

`Combat readability > Game Feel > Soul identity > Environment decoration`

## Verification

Important actions are understandable without Debug UI.

---

# Phase 15 — Ending

## Implement

After final Soul Release:

`Silence → purple energy calms → orange Flame of Life → THE LOST SOUL OF FIRE → Prototype Complete`

Restart must work.

Do not build a cutscene framework.

---

# Phase 16 — Debug + Hardening

## Implement / Complete

- F1 Debug overlay
- F2 Spawn Hollow
- F3 Spawn Burning
- F4 Spawn Devourer
- F5 Fill Resonance
- F6 Kill enemies
- F7 Toggle/force Soul Sense
- F8 Reset Arena

Overlay:

- FPS
- HP
- Resonance
- Wave
- Enemy Count
- Soul Count
- Player State

Optional if cheap:

- hitboxes
- AI state
- target

Then run through `18_MVP_ACCEPTANCE_CRITERIA.md`.

Fix failures.

---

# Phase 17 — Final Verification

Do not develop new features.

## Steps

1. Re-read all `docs/mvp/` specs.
2. Review every Acceptance Criterion.
3. Run restore/build.
4. Fix errors.
5. Fix gameplay regressions.
6. Fix obviously broken placeholder presentation.
7. Perform final playable-loop verification.

Minimum:

```bash
dotnet restore
dotnet build
```

## Final Report

Return:

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

A required Acceptance Criterion may not be dismissed as a "Known Limitation."

---

# Recommended Session Grouping

If execution quality remains high, multiple phases may be completed in one Codex session.

Suggested groups:

- Session 1: Phases 01–04
- Session 2: Phases 05–08
- Session 3: Phases 09–12
- Session 4: Phases 13–17

Example:

```text
Execute Phases 01-04 from docs/mvp/CODEX_EXECUTION_PLAN.md.

Read all authoritative docs/mvp specifications before modifying code.
Complete each phase sequentially.
Build and verify each phase before proceeding.
Fix regressions before continuing.
Do not expand scope.
```
