# Prompt 03 — First Playable Death-Layer Prologue

Continue only after the Golden Combat Slice and local brother co-op proof have an owner decision.

You are the owning gameplay, world and narrative implementation agent. Build the first 8–12 playable minutes of The Lost Soul of Fire.

Do not stop after producing a design document. Deliver a runnable prologue.

## Outcome

Implement:

> Death → emergence in the uncivilized Death Layer → exploration and first Lost Souls → Warden search/bergung → meeting the already-Warden brother → dangerous route/transport → arrival at the Warden homebase exterior.

The homebase itself may remain a single arrival scene. The prologue must have a clean beginning, middle and ending.

## Required reading

Read:

- `AGENTS.md`;
- all `docs/current/` sources;
- current Story Opening, World Grammar, Visual Art Direction and Combat Vision;
- latest owner decisions for the Golden Slice and co-op;
- the actual current code and git status.

Treat current code as implementation truth and the newest current documents as design truth.

## World structure

Build three authored connected sectors using the approved Soulfire grammar:

### Sector A — Emergence

- protagonist wakes alone in a raw, uncivilized Death-Layer space;
- player learns movement and sees unstable Death Flame;
- environment shows fragments of human places without explaining everything;
- one Soul Trace or Soul Sense reveal establishes that the landscape is emotional residue.

### Sector B — Search and recovery

- first Hollow and Burning encounters teach the approved combat;
- Warden markers or signals show that someone is searching;
- the brother enters as Player 2 in co-op and in the approved solo representation;
- the meeting is brief, human and playable; avoid a large cutscene system.

### Sector C — Escape / transit

- a Devourer threatens an exposed Soul;
- both brothers reach a Warden extraction route;
- implement the strongest feasible version of the Death-Flame vehicle with Soul Cannons;
- end on arrival at the exterior or threshold of the Warden homebase.

## Map approach

Use larger connected spaces with Diablo-like exploration rhythm, translated into Soulfire.

- Compose each sector from authored zones, landmarks and encounter spaces.
- Reuse the current arena as a contained encounter or ruin where appropriate.
- Add only a minimal `SceneDirector`/`WorldZone`/segment transition layer.
- Use data for bounds, connections, spawn groups and story triggers.
- Do not build a general-purpose editor or full procedural generator.
- Make the start region revisit-able through a simple post-prologue route or debug selection.

The first authored route is the reference implementation future generation must learn from.

## Narrative delivery

Use:

- environment composition;
- human objects and repeated motifs;
- enemy behavior;
- short in-world lines;
- Soul Sense traces;
- Warden route markers;
- brief brother exchanges.

Do not implement a branching dialogue engine. A small data-driven story-beat structure is enough.

Do not reveal:

- the nature of The Keeper;
- the true afterlife;
- the full Life-Reactor conflict;
- a confirmed remaining Life Flame in the protagonist.

## Vehicle setpiece

Prototype one short 45–75 second sequence.

- Death-Flame-powered Warden vehicle;
- pursuers from rear/flanks;
- Soul Cannon interaction using the established Cannon visual/combat language;
- useful roles for one or two Players;
- no disposable intact Souls as ammunition;
- readable solo fallback;
- same combat semantics as on-foot play.

Keep it reusable enough for later transit, but do not build a vehicle framework.

## Art production

Use the approved Golden Slice as the only internal visual benchmark.

Create only assets needed by these sectors:

- a small modular Death-Layer environment kit;
- Warden search markers;
- one major landmark per sector;
- vehicle and Cannon presentation;
- homebase exterior silhouette;
- a few human-residue props.

AI may create candidates. Promote only assets that pass in-game readability and Soulfire identity review. Preserve source prompts/manifests and rejected candidate reasons.

## Checkpoint and restart

Implement the smallest complete flow:

- start prologue;
- restart current sector after full party defeat;
- retain only story state needed to continue the current run;
- return to the start region after completion for replay or debug review.

Do not build full save slots yet unless strictly necessary.

## Acceptance gate

- Prologue is playable from launch to homebase arrival.
- Expected first run lasts roughly 8–12 minutes.
- All three sectors have distinct spatial identity.
- At least one Soul Sense reveal carries narrative meaning.
- The brother meeting works in solo and co-op.
- Existing Scythe, Dash, Cannon, Soul Sense, Soul Release and reaction mechanic appear naturally.
- The vehicle sequence works in solo and co-op or is honestly marked as the only incomplete acceptance item.
- The start area can be revisited.
- Build and focused automated checks pass.
- Critical story/combat frames have visual captures.
- The result looks like the approved Golden Slice expanded into a world, not a second prototype style.
- Current documentation and handoff are updated continuously.

## Non-goals

- complete homebase interior;
- online networking;
- procedural map generation;
- full item system;
- boss;
- quest log;
- voice acting;
- final cinematics;
- multiple new enemy families;
- full campaign.

## Final handoff

Provide:

- PLAYABLE RESULT
- HOW TO START SOLO AND COOP
- STORY FLOW
- MAP / SECTOR STRUCTURE
- ASSETS REUSED
- ASSETS CREATED
- VEHICLE SETPIECE
- CAPTURES
- VERIFICATION
- KNOWN LIMITATIONS
- OWNER DECISION: APPROVE / REVISE / REJECT
- NEXT RECOMMENDED SESSION
