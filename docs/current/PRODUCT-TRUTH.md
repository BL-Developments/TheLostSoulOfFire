# Product Truth

Status: authoritative working direction for `prototype/design-polish`.

Planning revision 2026-09-08: the owner's concept recording takes priority in
conflicts. See [`DECISION-LOG.md`](DECISION-LOG.md) and
[`RECORDING-SUMMARY-2026-09-08.md`](RECORDING-SUMMARY-2026-09-08.md).
The current runtime remains the baseline; regular runs now target one selected
main weapon, a three-offer/two-choice ability draft, a persistent hub and
bankable release resources. The six-item proof below is superseded as an
inventory/loot direction; useful effects must be reframed as abilities or
weapon progression. This is a planned direction, not implemented gameplay.

## Game identity

**The Lost Soul of Fire** is a story-driven cooperative 2D action roguelike set between a dystopian future Earth, the Death Layer and the unreachable true afterlife.

Its identity combines:

- Soulfire Gothic visual language;
- tragic, human stories embedded in supernatural regions;
- readable Scythe and Soul Cannon combat;
- reaction timing that creates expressive attacks and cooperative opportunities;
- morally meaningful treatment of Souls;
- replayable large-map exploration without becoming a generic loot game.

## Player promise

The player should feel like a newly dead person learning to exist as a Warden, not a conventional power fantasy hero. Combat is forceful and precise, but releasing a Soul remains distinct from destroying a hostile manifestation.

## Cooperative premise

The game must remain cooperative by construction. In cooperative story play, the second player is the protagonist's brother, who became a Warden earlier. The Wardens recover newly emerged Wardens because their arrival point in the Death Layer cannot be predicted precisely.

Local shared-screen co-op is the first technical proof. Online networking is deferred until the local design is approved.

## First playable target

Build in this order:

1. one 60–90 second gold-standard encounter in the current arena;
2. a local two-player brother proof in the same room;
3. an 8–12 minute Death Layer prologue across three authored sectors;
4. a minimal six-item Soul Echo proof;
5. an AI-assisted content factory based on the approved quality bar.

## Quality bar

### Visual

Use Children of Morta as the primary quality reference for:

- authored spatial depth;
- grounded characters and props;
- readable value grouping;
- emotionally legible small characters;
- layered environments;
- restrained but atmospheric lighting;
- strong silhouettes during dense combat.

Do not copy its characters, palette, layouts or assets. Translate those principles into violet-white Death Flame, rare warm Life Flame, ruined human infrastructure and Soulfire Gothic materials.

### Combat

Preserve the existing Scythe, Soul Cannon, dash, Soul Sense, Resonance and Soul lifecycle. Raise combat quality with readable anticipation, impact, recovery and reaction timing. A correct response should create an interesting action or position, not merely negate damage.

God of War is only a reference for the satisfaction of well-timed reactions unlocking expressive follow-ups. The result must remain native to a cooperative 2D action game.

## Content rule

Every region begins with:

> human place + human event + emotional residue + Soul distortion

Every enemy, item and mechanic should connect to Flame, Soul, Anchor, human history or release.

## Owner/agent contract

The owner supplies intent, taste and final approval. Agents inspect the real game, implement the strongest bounded hypothesis, verify it through play and captures, document decisions and present a result for `APPROVE`, `REVISE` or `REJECT`.

Agents should not stop at plans when implementation was requested.
