# Fastest Path to the First Playable Story Slice

## Target

Produce an 8–12 minute playable prologue that starts with the protagonist’s emergence in the wild Death Layer and ends at the Warden homebase arrival.

The slice should be good enough to answer four questions:

1. Is this the visual style to continue?
2. Is the reaction-based combat worth expanding?
3. Does the brother make co-op feel native to the story?
4. Does the Death Layer feel like a place worth exploring?

## Recommended sequence

### Gate 1 — Gold-standard encounter

Duration target for the player: 60–90 seconds.

Use the existing arena and content. Improve depth, grounding, combat readability and one reaction mechanic. Do not build a map system yet.

Owner decision:

- **APPROVE:** preserve its visual/combat grammar.
- **REVISE:** run a bounded polish pass on the same encounter.
- **REJECT:** change the hypothesis before creating more content.

Why first: copying an unapproved style across several maps creates expensive cleanup.

### Gate 2 — Brother co-op proof

Use the same gold-standard encounter. Add local shared-screen Player 2 using the brother. Prove camera, target selection, effects, Soul ownership and failure/revive behavior.

Do not add online networking. Local co-op exposes most design problems at a fraction of the cost.

Owner decision:

- Is two-player play readable?
- Does the brother have a distinct role?
- Does combat remain good with one player?

### Gate 3 — Death-Layer prologue route

Build three authored connected sectors from the approved grammar:

1. **Emergence:** alone, unstable Flame, first human residue.
2. **Search/Bergung:** first fights and meeting with the brother.
3. **Escape/Transit:** route or vehicle sequence to the homebase.

Use larger connected spaces, but keep the first implementation authored. Add Roguelike variation only to encounters or route branches that can be verified deterministically.

### Gate 4 — Minimal item proof

Add six run-local modifiers and one three-choice reward. Use the prologue to determine whether items deepen reaction combat or merely raise damage.

### Gate 5 — Content factory

After art, combat and co-op are approved, automate repetition:

- region briefs;
- room briefs;
- enemy variants;
- AI asset candidates;
- promotion into Content;
- scenario captures;
- agent critique;
- owner approval.

## Suggested first playable structure

| Minute | Beat | Existing reuse | New work |
|---:|---|---|---|
| 0–1 | title and emergence | title presentation, Player, atmosphere | new opening composition and short story beat |
| 1–3 | explore first sector | movement, camera, arena bounds | connected map segment and environmental traces |
| 3–5 | first Hollow/Burning encounters | existing enemy AI and combat | reaction window and authored encounter triggers |
| 5–7 | brother recovery | second Player foundation | brother art derivative, dialogue beats, co-op join |
| 7–9 | Devourer pressure and Soul Release | Devourer, Soul lifecycle, Resonance | encounter staging and cooperative opportunity |
| 9–11 | Death-Flame vehicle escape | Cannon/VFX/audio vocabulary | scrolling/rail setpiece and vehicle presentation |
| 11–12 | homebase arrival | cinematic overlay | one exterior arrival scene, no full hub yet |

## Definition of “playable enough”

The first prologue does not need:

- online networking;
- procedural world generation;
- a complete homebase;
- save slots;
- a quest system;
- full dialogue trees;
- final cinematics;
- a full inventory;
- more than the three existing enemy families;
- a boss.

It does need:

- a clean start and ending;
- at least three spatially distinct areas;
- one meaningful Soul Release;
- one reaction-based combat payoff;
- a brother encounter that works in solo and co-op;
- stable restart/checkpoint behavior;
- current visual identity at the approved quality bar;
- screenshots and a short capture for review;
- no lore contradiction with the current context pack.

## Why this is the fastest route

- Existing combat systems remain useful.
- One approved encounter becomes the source for environment, VFX and tuning rules.
- Co-op risk is exposed before building many enemies and maps around a singleton Player.
- The first route uses authored composition, avoiding premature procedural tooling.
- Items are tested as six concrete effects rather than a speculative framework.
- AI receives a durable source of truth and can work autonomously between owner approval gates.
