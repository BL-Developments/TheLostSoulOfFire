# Current Slice

Status: Golden Combat Slice refined and local two-player co-op proof implemented on `prototype/design-polish`, awaiting owner review.

## What exists

- C# / .NET 9 / MonoGame DesktopGL project.
- Player movement and dash.
- Three-hit Scythe combo.
- Chargeable Soul Cannon with projectile and recoil.
- Soul Sense and Soul Resonance.
- Soul states for exposure, devouring, release, residue and consumption.
- Hollow, Burning and Devourer enemy roles.
- Burning detonation through the Cannon.
- Scene rendering, emission, halos and reduced-effects presentation.
- Deterministic visual scenario capture system.
- Directional character animation, VFX and audio asset base.

### Added by Session 2 — Golden Slice uplift

- **Soulfire actor light grammar** (`Rendering/ActorLighting.cs`). Silhouettes
  traced from real sprite alpha. Wardens and Souls carry living Death Flame;
  manifestations are separated by cold ash light only.
- **Devourer slam telegraph rebuilt** as gathering ground pressure
  (`SoftShapes.PressureBand`) instead of a saturated ring.
- **Severance stopped blowing the Warden out**; the gather moved off-body and
  forward so the pose stays readable.

### Added by Session 2 — local co-op proof

- **One or two local Wardens** through `PlayerCommand` / `PlayerSlot` /
  `WardenRoster`. Player 1 keyboard and mouse; Player 2 gamepad or number pad.
- **The brother**: same sheet, same kit, colder paler flame, blue-steel tint,
  larger silhouette mass.
- **Group camera** with bounded 0.92–1.30 zoom and a shared Death Flame tether.
- **Stable enemy targeting** that never re-targets mid-commitment.
- **Team Resonance pool**; area attacks hit everyone in the area.
- **Down / stabilise / both-down failure.**
- **Six new or rebuilt audio cues**, produced locally from the approved bank plus
  generated texture.

### Added by the Golden Slice

- **Severance Window.** Dashing into a committed enemy attack opens a short
  per-Player window; the next Scythe swing cuts the enemy's Anchor rather than
  its body. Each family answers differently; the Devourer releases a held Soul.
- **Authored four-beat encounter** (`EncounterDirector`) with named beats and
  staged arrivals, replacing the anonymous four-wave loop.
- **Generated casting floor** and **authored arena composition**: recessed basin,
  the Fallen Ladle landmark, grounded props, local light pools, foreground frame.
- **All combat feedback painted as light** (`SoftShapes`) instead of vector
  strokes, per owner revision 2026-09-06.

## Verified baseline

- Release build passes with zero warnings and zero errors.
- Solo and two-player encounter lifecycles pass through four beats, completion
  and restart.
- Solo death/restart and two-player both-down failure/restart both pass.
- All 32 audio cues load real content (`fallbacks=0`); `validate_audio.py` passes
  on 34 assets.
- Twelve solo and eleven co-op visual scenarios capture successfully, including
  reduced-effects and baseline-quality variants.

## Current limitations

- `GameWorld` owns one arena.
- The environment is one fixed 1800×1000 arena.
- Grayscale separation between the two brothers relies on silhouette mass and rim
  intensity; a bespoke silhouette accent for the elder is the recommended next
  art step.
- The downed pose is the idle frame rotated and darkened — temporary art.
- No controller hardware was available for verification.
- The new audio cues have not been auditioned by ear.
- No story flow, zones, dialogue, homebase, co-op, items, save or progression exists yet.
- Several older design documents predate the current Warden cosmology and product direction.

## Active objective

Owner review of the refined Golden Slice, the co-op proof and the audio audition.
On approval, continue with
[`../agent-prompts/03-DEATH-LAYER-PROLOGUE.md`](../agent-prompts/03-DEATH-LAYER-PROLOGUE.md).

Do not multiply content until the owner approves the resulting combat and visual direction.
