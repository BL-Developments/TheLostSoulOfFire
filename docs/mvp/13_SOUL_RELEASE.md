# Soul Release

**DESIGN STATUS: LOCKED**

## Purpose

Soul Release is the emotional and mechanical center of the game.

Combat destroys a hostile manifestation.

The Flame of Death then releases the actual Lost Soul.

The release must feel fundamentally different from killing an enemy.

## Flow

`Enemy HP 0 → Manifestation breaks → Lost Soul appears → Exposed window → Release begins → Death Flame connects → Soul brightens → Soul leaves world → Soul Residue remains → Residue enters Player → Resonance increases`

## Exposed Window

Release is not instant.

Suggested duration before completion:

- ~1–2 seconds

**TUNABLE**

This window exists primarily so Devourers can threaten exposed Souls.

No release button is required.

Release is automatic if not interrupted by Devour.

## Soul States

Recommended simple states:

- `Exposed`
- `BeingDevoured`
- `Releasing`
- `Released`
- `Consumed`

Implementation may vary if simpler while preserving behavior.

## Visual Presentation

Combat death:

- violent
- fast
- dark
- loud

Soul Release:

- calm
- clean
- slower
- bright
- peaceful

Suggested visual sequence:

1. Soul floats.
2. Player Core reacts.
3. Thin violet Death-Flame connection forms.
4. Soul transitions violet → light violet → near-white.
5. Soul rises/dissolves out of the world.
6. Small violet Residue remains.
7. Residue travels into Player Core.

## Resonance

Only a successful release grants normal Soul Residue / Resonance.

A Soul consumed by a Devourer does not grant Resonance until it is later recovered and released.

## Devourer Interaction

During exposed/releasing state, Devourer may:

- retarget toward Soul
- pull Soul toward itself
- consume Soul

If interrupted and the Soul survives, release may continue/restart using the simplest implementation consistent with the intended window.

## Audio

Release should use:

- quiet Soul tone
- soft chime/bell-like resolution
- light Residue whoosh

Avoid explosive death sounds.

## Important Lore Rule

The actual Soul is never converted into Player fuel.

The Soul leaves.

Only Residue remains.

## PROTOTYPE FALLBACK

A Soul may be represented by:

- glowing violet-white circle
- simple particles
- vertical dissolve
- line/particle connection to Player
- small Residue particle flying to chest Core
