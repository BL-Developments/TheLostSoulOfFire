# Weapon — Scythe

**DESIGN STATUS: LOCKED**

## Fantasy

The Scythe is a **Death Flame Conduit**.

It has a physical handle and an incomplete/broken physical metal blade. The Flame of Death completes the cutting edge.

## Visual Identity

- dark physical handle
- silver/gray incomplete blade
- violet-white Death Flame completes the blade
- increasingly strong trails through the combo

## Input

`LMB`

Attacks aim toward the mouse, not the movement direction.

The Player is not fully rooted during attacks.

Small forward movement/impulse is allowed and encouraged.

## Three-Hit Combo

### Hit 1 — Horizontal Slash

- fast
- approximately 120° arc
- small violet trail
- light impact

### Hit 2 — Reverse Slash

- fast
- approximately 140° arc
- stronger flame trail
- slightly stronger impact

### Hit 3 — Soul Cleave

- heavy finisher
- approximately 180–200° arc
- large violet-white flame arc
- forward step
- high damage
- stronger knockback
- stronger hitstop
- camera shake

Target rhythm:

> SWISH → SWISH → BOOM

## Combo Reset

Suggested reset:

- ~0.6 seconds without another attack

**TUNABLE**

## Initial Damage

Suggested starting values:

- Hit 1: 20
- Hit 2: 25
- Hit 3: 40

**TUNABLE**

## Hit Detection

Use simple, robust, generous collision.

Recommended:

- range check
- angle check
- primitive hitboxes

Do not implement pixel-perfect or complex polygon collision.

## Hit Feedback

Normal hit:

- enemy flash
- tiny hitstop
- particles
- small knockback

Soul Cleave:

- stronger flash
- larger particles
- stronger hitstop
- camera shake
- large flame trail

## Soul Sense Interaction

If an exposed Soul Core is hit:

- bonus damage
- bonus Resonance contribution
- unique white-purple impact effect

Exact multipliers are **TUNABLE**.

## Resonance Interaction

Do not create new Scythe animations or abilities.

During Resonance:

- larger flame blade
- increased range
- increased damage
- increased knockback

## Out of Scope

Do not add:

- secondary Scythe ability
- alternate combos
- aerial attacks
- weapon upgrades
- stance system
- combo tree
