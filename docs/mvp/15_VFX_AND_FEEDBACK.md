# VFX and Combat Feedback

**DESIGN STATUS: LOCKED**

## Priority

If implementation time is constrained:

1. Combat readability
2. Death Flame
3. Hit feedback
4. Soul Release
5. Soul Sense
6. Resonance transformation
7. Enemy silhouettes
8. Environment detail

## Core Rule

Strong actions are constructed from:

`ATTACK → TRAIL → CONTACT FLASH → HITSTOP → PARTICLES → KNOCKBACK → CAMERA RESPONSE`

Game feel is more important than sprite complexity.

## Hitstop

Suggested starting ranges:

- normal Scythe hit: ~30–50 ms
- Soul Cleave: ~70–100 ms
- full Soul Cannon: ~100–130 ms
- Burning detonation: ~80–120 ms

**TUNABLE**

Avoid making gameplay feel stuttery.

## Camera Shake

- normal hit: none/tiny
- Soul Cleave: small
- Soul Cannon: medium
- Burning detonation: medium
- Resonance activation: medium/strong

Use sparingly.

## Impact Frames

For major actions, allow ~1–2 frames where:

- background becomes nearly black
- attack/Player becomes white-violet silhouette

Best uses:

- Soul Cleave
- full Soul Cannon
- Resonance activation

## Afterimages

Use for:

- Ignition Dash
- occasional Resonance movement

Do not use constantly during normal movement.

## Scythe Trails

- Hit 1: thin violet arc
- Hit 2: larger/brighter violet arc
- Hit 3: large dark-violet → violet → white-core arc

Trails disappear quickly.

## Soul Cannon Charge

Charge must be readable without UI.

Stage 1:

- small violet particles move toward Cannon

Stage 2:

- more particles
- barrel begins glowing

Stage 3:

- strong purple compression
- near-white Core
- weapon vibration
- unmistakable audio cue

## Soul Sense

Do not turn everything purple.

Instead:

- suppress/desaturate world
- emphasize Soul energy
- make Cores near-white
- use vignette and eye glow

## Soul Release

Never treat release as another explosion.

Use calm movement, clean light, and gentle upward disappearance.

## Resonance

During Resonance:

- near-white Core
- violet flames around silhouette
- purple cracks/veins
- increased particles
- slight afterimages
- silhouette remains readable

## Enemy Visual Language

### Hollow
- tall
- thin
- white mask
- slow

### Burning
- medium
- cracked
- flaming
- fast

### Devourer
- large
- wide
- heavy
- slow

## Death Flame Particle Behavior

Particles may:

- fall
- move sideways
- reverse
- form angular fragments
- briefly orbit or return toward source

Normal upward flame behavior should not dominate.

## PROTOTYPE FALLBACK

If no dedicated assets exist, implement effects using:

- circles
- rectangles
- lines
- simple textures
- particles
- alpha fades
- scale
- rotation
- camera effects

VFX should sell placeholder geometry.
