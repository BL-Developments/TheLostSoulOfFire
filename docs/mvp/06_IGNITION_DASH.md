# Ignition Dash

**DESIGN STATUS: LOCKED**

## Fantasy

The Player routes the Flame of Death from the Core through the spine and legs and explosively propels himself across the arena.

## Input

`Space`

## Direction

If movement input exists:

- dash in movement-input direction

If no movement input exists:

- dash in current facing direction

## Behavior

- fast burst movement
- short invulnerability window
- can pass through enemies
- deals no damage
- no stamina
- cooldown-based

Suggested starting values:

- distance: ~150 px
- cooldown: ~0.6 s

**TUNABLE**

## Visual Sequence

`CORE → LEGS → violet-white ignition → rapid movement → 2–3 afterimages → Death-Flame trail`

Use:

- dark/violet afterimages
- violet-white initial burst
- short flame trail
- small camera kick

## Audio

- deep short "whump" at start
- fast flame whoosh

## Resonance Interaction

During Resonance:

- slightly longer dash
- lower cooldown
- larger/more energetic trail
- overall mobility is noticeably improved

Do not make the Player uncontrollably fast.

## Out of Scope

Do not add:

- dash damage
- dash attack
- multiple dash charges
- air dash
- complex upgrades
