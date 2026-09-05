# Current Slice

Status: Golden Combat Slice implemented on `work/golden-combat`, awaiting owner review.

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
- Encounter lifecycle passes through four beats, completion and restart.
- Death and restart check passes.
- Thirteen visual scenarios capture successfully, plus reduced-effects and
  baseline-quality variants.
- Play-area black coverage reduced from 90–96% to under 2.5%.

## Current limitations

- `GameWorld` owns one arena and one player.
- Input, camera, HUD, enemy targeting and Soul interactions assume a single player.
- The environment is one fixed 1800×1000 arena.
- No story flow, zones, dialogue, homebase, co-op, items, save or progression exists yet.
- Several older design documents predate the current Warden cosmology and product direction.

## Active objective

Owner review of the Golden Combat Slice. On approval, continue with
[`../agent-prompts/02-COOP-BROTHER-PROOF.md`](../agent-prompts/02-COOP-BROTHER-PROOF.md).

Do not multiply content until the owner approves the resulting combat and visual direction.
