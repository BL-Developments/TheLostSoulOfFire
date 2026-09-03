# Enemy — Hollow

**DESIGN STATUS: LOCKED**

## Fantasy

A Hollow is a Lost Soul that has remained trapped so long that almost all personality and emotion have burned away.

Very little remains except an empty manifestation.

## Visual Identity

- tall
- thin
- unnaturally proportioned
- dark/anthracite body
- long arms
- slightly hunched
- cloth/mantle-like torn body shapes
- off-white, featureless mask
- very little visible purple energy outside Soul Sense

## Movement

Slowly approaches the Player.

Movement should feel subtly broken:

- normal step
- pause
- occasional unnaturally fast small step
- irregular timing

Do not make the AI mechanically complicated.

## Attack

One attack only: **Swipe**

Sequence:

1. reaches melee distance
2. arm pulls back
3. mask turns/tilts toward Player
4. clear ~0.4 s telegraph
5. large melee swipe
6. short recovery

Telegraph duration is **TUNABLE**.

## Soul Sense

Without Soul Sense:

- Core is not visible

With Soul Sense:

- near-white/violet Soul Core appears in chest

## Core Interaction

Scythe Core Hit:

- bonus damage
- bonus Resonance contribution
- unique impact

Full Soul Cannon Core Hit:

- very strong stagger
- strong Core crack effect

## Health

Suggested:

- 100 HP

**TUNABLE**

Initial Scythe values make one normal full combo almost but not quite kill a Hollow, making weakpoint usage valuable.

## Death / Soul Release

1. Hollow freezes.
2. Mask cracks.
3. Dark body breaks into particles.
4. Mask remains briefly.
5. Mask breaks.
6. Lost Soul appears.
7. Soul enters normal Soul Release flow.

## Audio

- faint breathing
- distorted whispering
- creaking movement
- distinctive bright Core-hit crack

## PROTOTYPE FALLBACK

- elongated dark humanoid
- white oval mask
- purple chest circle only while Soul Sense is active

## Out of Scope

No:

- projectiles
- teleport
- second phase
- combos
- status effects
- Soul consumption
- special abilities
