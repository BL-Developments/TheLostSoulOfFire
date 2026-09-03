# Enemy — Devourer

**DESIGN STATUS: LOCKED**

## Fantasy

The Devourer is a Lost Soul defined by hunger and possession.

It cannot move on, so it attempts to imprison other exposed Lost Souls inside its own manifestation.

Its body is effectively a **Soul prison**.

## Visual Identity

- large
- wide
- heavy
- hunched
- black/dark body
- oversized torso opening / mouth-like fracture
- faint violet glow inside
- trapped Souls may appear as small lights within the torso
- massive arms/legs

Avoid a generic fat zombie.

## Target Priority

Primary decision:

```text
if exposed Soul exists:
    target exposed Soul
else:
    target Player
```

The Player must clearly see when a Devourer changes target toward a Soul.

## Movement

Slow and persistent.

Its threat comes from objective pressure, not speed.

## Player Attack

One normal attack: **Heavy Slam**

Sequence:

1. raise arms/body
2. clear long telegraph
3. heavy slam
4. small shockwave
5. recovery

Keep it readable and dodgeable.

## Devour

When an exposed Soul exists:

1. Devourer approaches.
2. At close range, Soul begins being pulled toward torso opening.
3. Player has a short opportunity to stop/stagger the Devourer.
4. If completed, Soul becomes `Consumed`.
5. Player receives no Resonance for that Soul at that time.

## Devour Buff

Each consumed Soul:

- heals Devourer
- slightly increases damage
- may slightly increase size/glow

Suggested maximum:

- 3 consumed-Soul stacks

**TUNABLE**

Do not create a generic buff framework.

A simple counter is sufficient.

## Soul Sense

Soul Sense reveals consumed Souls moving inside the Devourer's body.

The torso becomes a readable mass of trapped Soul energy.

## Soul Cannon Interaction

A fully charged Soul Cannon hit can strongly stagger the Devourer.

If it has consumed Souls:

- one consumed Soul is expelled
- expelled Soul returns to an exposed/releasing state
- it can then be successfully released

This prevents Devour from being an irreversible punishment.

## Scythe Interaction

Scythe deals normal damage.

Soul Sense may allow bonus damage to the torso/Soul mass.

Scythe does not extract consumed Souls.

Soul Cannon owns that special interaction.

## Health

Suggested:

- 200 HP

**TUNABLE**

## Death

On death:

1. torso develops many violet cracks
2. Soul prison breaks
3. all remaining consumed Souls burst out
4. Souls remain calm after the violent break
5. each enters release flow
6. successful releases grant appropriate Resonance

## PROTOTYPE FALLBACK

- large dark geometric humanoid/blob
- obvious torso opening
- purple circles inside during Soul Sense

## Out of Scope

Do not add:

- ranged attacks
- multiple slam combos
- complex stack abilities
- permanent Soul destruction
- boss phases
