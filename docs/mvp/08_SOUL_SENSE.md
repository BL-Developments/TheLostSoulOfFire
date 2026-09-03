# Soul Sense

**DESIGN STATUS: LOCKED**

## Fantasy

Soul Sense allows the Player to perceive the truth beneath the physical world: Souls, Soul anatomy, unstable Soul structures, and traces of Soul energy.

## Input

`Q Hold`

Release `Q` to immediately return to normal vision.

## Activation

`Q Down → CORE → NECK/HEAD → EYES → eyes ignite violet → world presentation changes`

## World Presentation

While active:

- world becomes darker and strongly desaturated
- ordinary lights/fire become less prominent
- Death Flame and Lost Souls become bright violet
- Soul Cores become near-white with violet edges
- Soul Traces appear as thin purple trails
- Player eyes glow violet
- slight dark vignette

Do not simply tint the entire screen purple.

## Gameplay Information

Soul Sense reveals:

### Hollow
A stable Soul Core in the chest.

### Burning
Multiple unstable Soul Fractures rather than one stable Core.

### Devourer
Consumed/trapped Souls visible inside the body.

### Lost Souls
Highly visible during the release window.

### Soul Traces
Visual-only in the MVP.

Do not build tracking, secrets, quests, navigation, or investigation systems around Soul Traces yet.

## Movement Tradeoff

While manually using Soul Sense:

- movement speed is reduced by approximately 15%

**TUNABLE**

No mana.

No cooldown.

## Combat

The Player can:

- Scythe attack
- charge/fire Soul Cannon
- Dash

while Soul Sense is active.

Dash does not cancel Soul Sense.

## Resonance Interaction

During Resonance:

- Soul Sense is automatically active
- Soul Sense movement penalty is removed

## Technical Fallback

Preferred if easy:

- desaturation/shader treatment

Fallback:

- dark transparent overlay
- highlighted Soul entities
- highlighted weakpoints
- particles
- vignette

Gameplay must not be blocked by shader work.

## Out of Scope

Do not add:

- scan menu
- separate Soul dimension
- mana
- cooldown
- investigation system
- hidden-object progression
