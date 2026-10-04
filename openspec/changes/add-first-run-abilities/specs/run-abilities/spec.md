## ADDED Requirements

### Requirement: Two distinct arena abilities
The game SHALL allow two distinct selections from the first six abilities before arena combat or in intermission and SHALL freeze the simulation during selection.

#### Scenario: Equip and cast
- **WHEN** the player chooses a slot and one of the six abilities
- **THEN** that slot shows the selected name and cost
- **AND** Z/X casts that slot during arena combat
- **AND** a duplicate selection is rejected

### Requirement: Transactional resource spending
A successful cast SHALL charge only the mystic run account exactly once. Invalid state, cooldown, full-health healing, insufficient funds or fully blocked retreat SHALL not charge.

#### Scenario: Insufficient run Glut
- **WHEN** secured Glut exists but run Glut is insufficient
- **THEN** casting fails with feedback and neither balance changes

### Requirement: Six distinct effects
The game SHALL provide capped self-healing, a piercing projectile with one hit per enemy, bounded retreat without invulnerability, non-damaging vortex, one-hit revenge protection and solo follow-up marks. Both existing weapons SHALL consume eligible weapon bonuses.

#### Scenario: Prepare and follow up
- **WHEN** Setup is active and a weapon first hits a living enemy
- **THEN** the enemy gains a temporary mark without consuming it on the same hit
- **AND** the next weapon hit consumes exactly one follow-up bonus

### Requirement: Lifecycle and presentation
The game SHALL clear transient effects on death, reset and run completion and freeze clocks during pause. HUD SHALL show cost, cooldown and insufficient resource feedback. Native validation SHALL use a separate test profile.

#### Scenario: Reset during active effects
- **WHEN** the run resets
- **THEN** projectiles, fields, protection and pending bonuses are cleared
- **AND** the equipped pair is retained

