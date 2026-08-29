# MVP Acceptance Criteria

**DESIGN STATUS: LOCKED**

A green build alone does not mean the MVP is complete.

The full playable loop and its core game feel must work.

## 1. Complete Loop

Must be playable without debug intervention:

`Launch → Start → Arena → Waves → Soul Releases → Resonance → Final Wave → Flame of Life → Prototype Complete`

Death/restart must also work.

## 2. Player

Must support:

- WASD movement
- mouse facing
- HP
- damage
- death
- restart
- Scythe
- Ignition Dash
- Soul Cannon
- Soul Sense
- Resonance

Normal movement must feel responsive.

Resonance must make the Player noticeably faster and more mobile without becoming absurdly overpowered.

## 3. Scythe

Must:

- have three distinguishable combo steps
- reset after a short pause
- aim toward mouse
- damage enemies
- knock enemies back
- allow some attack movement
- make Soul Cleave clearly heavier than hits 1/2
- recognize Core interactions
- become stronger/larger during Resonance

Target read:

> SWISH → SWISH → BOOM

If all three attacks feel identical, this criterion fails.

## 4. Ignition Dash

Must:

- use movement direction
- fall back to facing direction
- provide short i-frames
- pass through enemies
- deal no damage
- have cooldown
- show Death-Flame trail
- show afterimages
- improve during Resonance

## 5. Soul Cannon

Must:

- physically exist
- be visibly carried on back
- draw on RMB hold
- charge continuously
- communicate three charge stages
- communicate full charge visually and audibly
- fire on RMB release
- recoil
- slow Player while charging
- strongly interact with Hollow Core
- detonate charging Burning
- expel consumed Souls from Devourer
- improve during Resonance

No ammo is required.

## 6. Soul Sense

While Q is held:

- world becomes darker/desaturated
- Soul energy is highlighted
- Player eyes glow
- Hollow Core is visible
- Burning Fractures are visible
- trapped Devourer Souls are visible
- Player receives slight movement penalty
- Scythe/Cannon/Dash remain usable
- Dash does not cancel Soul Sense

On release, normal view returns immediately.

During Resonance, Soul Sense is automatic and has no movement penalty.

## 7. Hollow

Must:

- have clear tall/thin/masked silhouette
- slowly pursue Player
- use one telegraphed Swipe
- expose chest Core only in Soul Sense
- react specially to Core hits
- strongly stagger from full Cannon Core hit
- create exactly one Soul on normal death

## 8. Burning

Must:

- be visually distinct
- feel faster/aggressive
- telegraph Charge
- charge Player
- have miss Recovery
- reveal multiple Fractures in Soul Sense
- detonate when Cannon hits during Charge
- damage nearby enemies with detonation
- not damage Player with detonation in MVP
- leave Soul after death/detonation

## 9. Devourer

Must:

- be large/heavy/slow
- prioritize exposed Souls over Player
- visibly communicate target change toward Soul
- use Heavy Slam
- consume Souls
- heal/strengthen after Devour
- reveal trapped Souls in Soul Sense
- allow full Cannon to expel a consumed Soul
- release all remaining Souls on death

If the Player cannot clearly understand that the Devourer wants an exposed Soul, this criterion fails.

## 10. Soul Release

Must follow:

`Manifestation breaks → Soul appears → exposed window → release → Residue → Player Core → Resonance`

Release must:

- be visually readable
- feel calm
- differ clearly from combat death
- be interruptible by Devour
- grant Resonance only after successful release

## 11. Resonance

Must:

- increase from successful releases
- have visible meter
- signal full state
- activate manually with R
- last around 10 seconds as starting balance
- noticeably increase movement/mobility
- buff Scythe
- buff Dash
- buff Cannon
- automatically enable Soul Sense
- remove Soul Sense movement penalty
- add no new abilities
- cleanly return to normal

Exact values are tunable.

## 12. Arena and Waves

Arena must visibly suggest:

- Gothic shapes
- industrial machinery
- furnaces
- pipes
- asymmetry
- dark palette

Required wave structure:

### Wave 1
- 3 Hollow

### Wave 2
- 2 Hollow
- 2 Burning

### Wave 3
- 2 Hollow
- 2 Burning
- 1 Devourer

### Finale
- mixed heavy wave

Finale composition is tunable.

A normal run should realistically permit at least one Resonance activation.

## 13. Ending

After final Soul Release:

- combat stops
- arena becomes quiet
- purple energy calms
- one orange Flame of Life appears
- title / `Prototype Complete` appears
- restart works

## 14. Game Feel

At minimum:

- hitstop
- knockback
- Scythe trails
- Cannon recoil
- Dash afterimages
- particles
- major-action screen flash/impact frame
- camera shake on strong actions
- Soul Release VFX
- Resonance transformation

Mechanically correct but lifeless combat is not complete.

## 15. Audio

At least placeholder audio for:

- Scythe swings/hits
- Dash
- Cannon charge
- Cannon full-charge cue
- Cannon fire
- Burning Charge
- Core hit
- Soul Release
- Resonance ready
- Resonance activation
- Player hit
- Player death

One ambience/music loop is sufficient.

## 16. Debug

Required:

- F1 Debug overlay
- F2 Spawn Hollow
- F3 Spawn Burning
- F4 Spawn Devourer
- F5 Fill Resonance
- F6 Kill enemies
- F7 Toggle/force Soul Sense
- F8 Reset Arena

Overlay minimum:

- FPS
- HP
- Resonance
- Wave
- Enemy Count
- Soul Count
- Player State

## 17. Stability

Before completion:

```bash
dotnet restore
dotnet build
```

must succeed.

Fix obvious:

- compile errors
- null crashes
- broken restart
- enemies permanently stuck
- waves not progressing
- Souls never releasing
- Resonance impossible to activate
- required interactions not firing

## Explicitly Not Required

The MVP may be complete without:

- professional final sprites
- procedural generation
- save system
- settings menu
- controller support
- skill tree
- meta progression
- multiple weapons
- boss
- dialogue
- story system
- inventory
- crafting
- achievements
- multiplayer
- localization
- advanced shader pipeline
- ECS
- perfect architecture

Missing out-of-scope features are not defects.

## Final Definition of Done

The MVP is done when another person can play this one arena for a few minutes and understand the intended identity of **The Lost Soul of Fire**:

> fast Scythe combat + heavy Soul Cannon + Soul Sense + releasing Lost Souls + protecting Souls from Devourers + building Resonance + becoming a faster, more mobile Death-Flame-powered version of the Player.
