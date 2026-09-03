# Gameplay Loop

**DESIGN STATUS: LOCKED**

## Prototype Flow

`Launch → Title → Press Any Key → Arena → Waves → Final Soul Release → Flame of Life → Prototype Complete`

On death:

`Player Death → Fade → Press R to Retry → Full Arena Reset`

On completion:

`Prototype Complete → Press R to Restart`

## Core Combat Loop

1. Move and position.
2. Use fast Scythe attacks for normal combat.
3. Dash through danger.
4. Use Soul Sense to reveal Soul anatomy.
5. Use the heavy Soul Cannon for deliberate high-impact opportunities.
6. Destroy enemy manifestations.
7. Protect exposed Souls.
8. Release Souls.
9. Gain Soul Residue.
10. Fill Resonance.
11. Activate enhanced Death-Flame mode.
12. Become faster, more mobile, and stronger for a short period.

## Combat Decisions

The three enemies create different pressures:

- Hollow teaches Soul Cores and basic combat.
- Burning pressures movement and rewards Cannon timing.
- Devourer threatens exposed Souls and rewards target prioritization.

Mixed encounters should create decisions such as:

- detonate a charging Burning to damage nearby enemies
- stop a Devourer before it reaches an exposed Soul
- use Soul Sense to identify a valuable weakpoint
- save or use a full Cannon charge
- activate Resonance for mobility and pressure relief

## Controls

MVP keyboard/mouse:

- `WASD` — Move
- `Mouse` — Aim / facing
- `LMB` — Scythe
- `RMB Hold/Release` — Soul Cannon charge/fire
- `Space` — Ignition Dash
- `Q Hold` — Soul Sense
- `R` — Activate Resonance when full

Controller support is out of scope unless trivial and does not delay the MVP.

## Combat Pace

Movement should be fast and responsive.

No stamina system.

The Player should have near-immediate acceleration.

The intended rhythm is:

> reposition → fast melee pressure → dodge → identify opening → heavy Cannon payoff → Soul Release → repeat

## Out of Scope

Do not add:

- stamina
- mana
- ammo
- loot
- XP
- upgrades
- weapon switching
- combo score
- procedural rooms
- additional abilities
