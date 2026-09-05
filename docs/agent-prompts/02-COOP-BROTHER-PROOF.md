# Prompt 02 — Cooperative Brother Combat Proof

Continue only after the owner approves or explicitly revises the Golden Combat Slice.

You are the owning gameplay agent. Implement a real local shared-screen two-player proof in the approved golden encounter. Do not stop after designing co-op architecture.

## Outcome

The same encounter must work with:

- one player;
- two local players;
- Player 2 represented by the protagonist’s brother, already a Warden.

The owner must be able to decide whether co-op feels native to the game rather than added afterward.

## Required reading

Read:

- root `AGENTS.md`;
- everything in `docs/current/`;
- the latest handoff and owner decision;
- current Player, InputState, enemy, Soul, camera, HUD and renderer code;
- the approved golden-slice captures.

## Implementation strategy

Add only the seams required for one or two local players.

Recommended minimal structure:

- `PlayerSlot` containing identity, Player and input source;
- a small per-frame player command/input value;
- keyboard/mouse source for Player 1;
- gamepad source for Player 2;
- group camera target and bounded dynamic zoom;
- enemy target selection across living Players;
- Soul/Residue ownership rules;
- two-player HUD identity.

Do not add dependency injection, a generic entity framework or network abstractions.

## Brother identity

Create a visually distinct but production-efficient brother prototype.

- Reuse the existing Warden asset structure and animations where practical.
- Use an approved derivative, palette/material variation and distinct silhouette accents.
- Preserve violet-white Death Flame meaning.
- Ensure both Players remain distinguishable in full combat, grayscale and reduced effects.
- Give the brother a small gameplay identity through tuning or one complementary interaction, not an entirely separate class kit.

Document all temporary art assumptions.

## Combat rules

- Both Players can move, dash, use Scythe, Soul Cannon and Soul Sense.
- Both can use the approved reaction mechanic.
- Enemies choose targets predictably and can switch without jitter.
- Telegraphs remain fair when targets change.
- Devourers still prioritize exposed Souls appropriately.
- Burning detonation remains enemy-only unless intentionally changed and documented.
- Avoid body blocking that makes close combat frustrating.
- Keep the gold encounter fun in solo.

Use a team Resonance pool as the first prototype unless current evidence supports a better low-risk option. Make ownership explicit and tunable. Do not create a competitive pickup race by accident.

## Failure and recovery

Implement the smallest complete two-player rule:

- one downed Warden can be stabilized by the other during a clear danger window;
- the encounter fails when both are down;
- solo death/retry remains functional;
- stabilization must not trivialize damage or create endless invulnerability.

Connect the presentation to Warden stabilization rather than generic arcade revival.

## Camera and readability

- Keep both Players visible within a useful combat frame.
- Use bounded zoom and soft constraints.
- Handle temporary separation without teleporting immediately.
- If separation exceeds the safe encounter area, use a clear soft tether or regroup rule.
- Do not build split-screen in this slice.
- Ensure aim mapping remains correct while camera zoom changes.

## Verification

Add deterministic scenarios for:

- two-player idle/readability;
- both Players attacking different enemies;
- shared counter opportunity;
- one Player down and stabilization;
- group camera at maximum intended separation;
- two-player Soul Release and Resonance gain;
- solo regression.

Run Release build, current lifecycle tests, visual captures and a real local controller playthrough if hardware is available. Report hardware checks honestly.

## Non-goals

- online networking;
- matchmaking;
- split-screen;
- second full weapon kit;
- full brother backstory or cutscenes;
- large map traversal;
- items;
- persistence.

## Acceptance gate

- One- and two-player launch paths work.
- The golden encounter completes in both modes.
- Existing combat remains recognizable.
- Both brothers are visually distinguishable.
- Target switching and telegraphs are fair.
- Camera and aim remain correct.
- Soul Release and Resonance have explicit two-player ownership.
- Down/stabilize/failure rules work.
- New scenarios capture the critical states.
- `docs/current/HANDOFF.md` and `DECISION-LOG.md` are updated.

## Final handoff

Provide:

- IMPLEMENTED
- PLAYER 1 / PLAYER 2 CONTROLS
- SOLO BEHAVIOR
- COOP BEHAVIOR
- BROTHER IDENTITY
- CAMERA / TARGETING RULES
- RESONANCE / SOUL OWNERSHIP
- VERIFICATION AND CAPTURES
- KNOWN LIMITATIONS
- OWNER DECISION: APPROVE / REVISE / REJECT
- NEXT RECOMMENDED SESSION
