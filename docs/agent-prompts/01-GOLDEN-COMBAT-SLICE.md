# Prompt 01 — Vision Alignment and Golden Combat Slice

You are the owning gameplay and visual implementation agent for **The Lost Soul of Fire**.

Work directly in the repository branch provided by the owner. This is an implementation session. Do not stop after auditing or writing a plan.

## Outcome

Create one polished 60–90 second playable encounter in the existing arena that establishes the visual and combat grammar the rest of the game should follow.

The owner must be able to play it and answer:

> “Yes, this is the style and combat direction we should continue.”

## Context

Read the entire repository and the current in-repository context, especially:

- `docs/current/AGENT-CONTEXT-COMPACT.md`
- `docs/current/CANON-STATUS.md`
- `docs/current/STORY-OPENING.md`
- `docs/current/VISUAL-ART-DIRECTION.md`
- `docs/current/COMBAT-VISION.md`

The current context supersedes conflicting older MVP or Full Vision statements.

Inspect at minimum:

- actual current `GameWorld`, `Player`, combat, enemy, Soul, renderer and input code;
- current Content and art source files;
- current twelve-scenario capture runner;
- `docs/mvp/`, `docs/vision/` and `docs/visual-max/` for useful history and conflicts;
- git status and current build.

## Phase A — Create the durable project control plane

Before broad code edits, verify and reconcile:

- root `AGENTS.md`;
- `docs/current/PRODUCT-TRUTH.md`;
- `docs/current/CANON-STATUS.md`;
- `docs/current/CURRENT-SLICE.md`;
- `docs/current/DECISION-LOG.md`;
- `docs/current/HANDOFF.md`.

Keep the existing lore/art/combat decisions concise and consistent. Do not duplicate the entire history. Mark older contradictory documents as historical or superseded through clear headers/links; do not delete useful history.

Repair documentation links that claim missing Astra handoff or capture scripts exist. Documentation must describe the branch that actually exists.

Keep this phase bounded. The product is the playable encounter.

## Phase B — Capture the real baseline

Build Release and run the existing visual scenarios.

Inspect at least:

- arena idle;
- Scythe combo;
- Hollow swipe;
- Burning charge;
- Devourer slam;
- Cannon + Soul Sense;
- busy Resonance;
- Soul Release.

Record the three largest visible problems. Expected hypotheses include an almost-black empty floor, weak grounding, limited spatial depth, small actor presence and insufficient contrast hierarchy. Verify rather than assume.

## Phase C — Build the golden encounter

Use the current arena and existing enemies. Preserve the strong systems already present.

Create a short authored encounter with:

1. a readable calm entrance;
2. one Hollow teaching the Soul Core;
3. one Burning teaching the Cannon detonation opportunity;
4. one Devourer threatening an exposed Soul;
5. one meaningful Soul Release and clear conclusion.

The encounter should not be four anonymous waves. Stage enemies and pauses so each role is learned through play.

## Phase D — Add one reaction-based combat thesis

Implement one coherent timing mechanic, preferably:

> A well-timed dodge through or immediately before a clearly telegraphed attack creates a short **Severance Window**. During that window, the next Scythe strike becomes a distinctive counter with positional or Anchor-related value.

Requirements:

- use current inputs where practical;
- do not add a QTE prompt;
- keep the timing tunable in `GameBalance`;
- make success readable through pose, local VFX and audio;
- make failure understandable;
- do not trivialize every enemy;
- keep the implementation compatible with later two-player use;
- do not build a generic counter framework.

If another reaction thesis is measurably better in the current code, implement it and document the reason.

## Phase E — Raise the visual level

Translate Children of Morta’s useful principles into Soulfire Gothic without copying it.

Improve the current arena through a small number of high-impact changes:

- readable floor material and value variation;
- stronger actor and prop grounding;
- visible wall/prop height;
- one asymmetric landmark;
- foreground/background layering;
- restrained local environment light;
- better scale hierarchy;
- a quiet, readable combat center;
- stronger but not noisy reaction/counter feedback.

Reuse existing art where it works. Create derivative or AI-assisted assets only when they materially improve the in-game frame. Keep candidates outside runtime Content until chosen. Do not solve the frame with extra bloom or particles.

The Player should remain supernatural but become more emotionally legible as a newly dead person. Prefer scale, pose, grounding, framing and selective art adjustment over replacing the entire character without evidence.

## Phase F — Extend evidence

Add deterministic visual scenarios for:

- golden encounter overview;
- Severance timing opportunity;
- successful counter impact;
- final Soul Release.

Capture matched before/after evidence at native resolution. Include baseline and reduced-effects views where relevant.

## Non-goals

Do not implement yet:

- co-op;
- online networking;
- large maps;
- procedural generation;
- items or inventory;
- homebase;
- dialogue engine;
- save system;
- additional enemy families;
- a general level framework;
- a full asset replacement.

## Acceptance gate

The session succeeds only when:

- the Release build passes;
- the existing automated lifecycle still passes or is intentionally replaced by an equivalent focused check;
- the encounter is playable from launch to conclusion;
- all three existing enemy identities remain intact;
- the reaction mechanic is functional, readable and tunable;
- the arena visibly gains depth, grounding and authored identity in screenshots;
- combat remains readable with full and reduced effects;
- Soul Release remains peaceful and distinct from destruction;
- current lore documents and runtime no longer contradict each other silently;
- `docs/current/HANDOFF.md` contains exact next steps and evidence.

## Working style

Progress autonomously. Make routine reversible decisions yourself. If an aesthetic choice is uncertain, implement the strongest bounded hypothesis and present it for owner review instead of stopping early.

Do not claim visual success from code or build results. Inspect the rendered game.

## Final handoff

Provide:

- IMPLEMENTED
- HOW TO PLAY
- WHAT TO FEEL FOR
- BEFORE / AFTER CAPTURES
- REACTION MECHANIC
- REUSED SYSTEMS AND ASSETS
- DOCUMENTATION RECONCILED
- VERIFICATION
- KNOWN LIMITATIONS
- OWNER DECISION: APPROVE / REVISE / REJECT
- NEXT RECOMMENDED SESSION
