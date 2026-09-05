# The Lost Soul of Fire — Repository Agent Instructions

Scope: entire repository, with current work focused on `prototype/design-polish`.

## Mission

Build The Lost Soul of Fire into a highly polished 2D cooperative action roguelike using the existing C#/.NET 9/MonoGame DesktopGL project.

The owner defines intent, taste and final approval. Agents inspect, implement, generate bounded alternatives, verify the real game, record decisions and continue autonomously until the requested acceptance gate is met.

## Instruction priority

When sources conflict, follow this order:

1. latest explicit owner instruction;
2. `docs/current/DECISION-LOG.md`;
3. `docs/current/PRODUCT-TRUTH.md` and `docs/current/CANON-STATUS.md`;
4. `docs/current/CURRENT-SLICE.md`;
5. current runtime code and tests as implementation truth;
6. older files under `docs/vision/`, `docs/mvp/` and `docs/visual-max/`.

Older documents remain valuable historical context but are not authoritative when the current context supersedes them. A label such as `DESIGN STATUS: LOCKED` in an older document does not override a newer owner decision.

## Required reading

Before material work, read:

- `docs/current/PRODUCT-TRUTH.md`
- `docs/current/CANON-STATUS.md`
- `docs/current/CURRENT-SLICE.md`
- `docs/current/DECISION-LOG.md`
- `docs/current/HANDOFF.md`
- relevant source and current git status

For combat or graphics work, also read:

- `docs/current/VISUAL-ART-DIRECTION.md`
- `docs/current/COMBAT-VISION.md`
- `docs/agent-prompts/FIRST-PLAYABLE-ROADMAP.md`

Read older specifications only when relevant to the requested system.

## Creative invariants

- Life Flame gives a Soul the strength to remain.
- Death Flame gives a Soul the strength to let go.
- Lost Souls are tragic people, not an evil species.
- Destroying a manifestation and releasing its Soul are distinct events.
- The Player never consumes an intact Soul as ordinary fuel.
- Vaelor is First/Highest Warden, not a god.
- The transition layer is not the true afterlife.
- The Keeper remains unknown.
- Soulfire regions begin with human place + human event + emotional residue + Soul distortion.
- Death Flame is violet-white; Life Flame is rare and warm orange.
- Soulfire Gothic is the identity. References are translated, never copied.

## Current product direction

- Primary visual quality reference: Children of Morta for spatial pixel art, readable silhouettes, grounding, atmosphere and emotional character presence.
- Combat direction: readable 2D action plus timing-based reactions that unlock special attacks or team opportunities.
- Large regions may use Diablo-like exploration structure, without copying its loot or visual identity.
- The story and core mechanics must be cooperative by construction.
- The second player is the protagonist’s brother, already a Warden.
- Continue raising both combat and graphics toward the Children of Morta quality bar until the owner explicitly approves the direction; do not treat one polish pass as final merely because it builds.

## Engineering rules

- Preserve C#, .NET 9, MonoGame DesktopGL and the existing Content pipeline.
- Reuse working combat, enemies, Soul lifecycle, presentation, audio and visual capture systems.
- Prefer small concrete classes, data and enum state machines.
- Create abstractions only for immediate requirements.
- Do not introduce ECS, a generic ability framework, a generic event bus, a full quest engine or premature procedural generation.
- Keep gameplay state separate from visual presentation where practical.
- Design Player input, camera, enemy targeting, HUD and resources for one or two local players before multiplying content.
- Do not add online networking until local co-op is approved.
- Preserve unrelated user changes.

## AI autonomy

Bias toward action. Complete routine, reversible and in-scope work without asking the owner to choose implementation details.

When a choice materially changes the game’s identity:

1. complete all independent work;
2. create the smallest playable comparison when practical;
3. document the alternatives and recommendation;
4. ask the owner to approve a concrete result.

Do not stop at a plan when the prompt requests implementation. Do not declare success from build output alone.

## Visual work

- Capture the current matching scenario before changes.
- Inspect actual native-resolution output.
- Implement a coherent visual hypothesis.
- Capture the same scenario afterward.
- Evaluate player/threat readability, depth, grounding, palette, material consistency and Soulfire identity.
- Keep generated candidates outside runtime Content until selected.
- Never hide weak composition under particles, bloom or darkness.

## Gameplay work

- Verify feel in the running game when the environment permits.
- Preserve clear telegraphs and deterministic collision.
- Timing-based advantages must remain fair in solo and co-op.
- New items must change decisions or play patterns, not only add small percentage bonuses.
- Every enemy and item should connect to Flame, Soul, Anchor, human history or release.

## Verification

For relevant changes:

- restore/build the solution;
- run focused automated checks;
- run the relevant real visual scenarios;
- add or extend a deterministic scenario for new critical behavior;
- inspect screenshots, not only logs;
- report checks honestly as PASS, FAIL or NOT_RUN.

Do not broaden testing after relevant checks pass unless a failure or risk justifies it.

## Persistent handoff

Maintain `docs/current/HANDOFF.md` during long work. Record:

- current objective;
- completed work;
- changed files;
- assumptions;
- verification and evidence;
- remaining issues;
- exact next action.

Update `docs/current/DECISION-LOG.md` whenever the owner approves, rejects or revises a result. Never keep important product knowledge only in chat.

## Final response format

Lead with the playable outcome.

Include:

- implemented result;
- how to run it;
- what the owner should test;
- screenshots/evidence;
- relevant verification;
- known limitations;
- one recommended owner decision: APPROVE, REVISE or REJECT.
