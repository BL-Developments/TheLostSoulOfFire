# Current Slice

Status: Golden Combat Slice, local two-player co-op proof and the Session 3
character/animation/pixel-language reset implemented on `prototype/design-polish`,
awaiting owner review.

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

### Added by Session 5 — the held scythe, the chain, and the dash

- **The scythe is held, not attached.** The pose describes where the weapon is
  and the hands are placed on the haft, so the carry and the swing are the same
  rig at two moments. The guard is level across the body at chest height.
- **Three choreographed hits**: a sweep to the right, an answer back to the left,
  and a full body turn on the third. Each is sampled by the Scythe's own progress
  and eases its grip back onto the carry, so the chain resolves into the hold.
- **The swing overlay is lifted to the grip**, so the blade leaves the Warden's
  hands instead of sweeping along the floor beneath them.
- **The dash interrupts any attack.** A strike already created still lands; the
  chain position survives, so dashing mid-combo and swinging again continues it.
- **Dash invulnerability is visible.** The Warden darkens and thins for exactly
  as long as the i-frames are live.
- New fixtures: `combo-chain` and `dash-cancel`.

### Added by Session 4 — camera, whole-body motion, weapons and VFX

- **Camera follow rebuilt.** Look-ahead from velocity and facing, a soft zone, a
  critically damped approach, vertical restraint, a small bias toward the fight
  and a zoom punch on impact.
- **Whole-body animation.** Pelvis sway onto the stance leg, shoulder
  counter-rotation with a late head, arms crossing inboard; planted feet are
  excluded from all of it so they never slide.
- **Weapons re-shaped.** A deep recurved blade with a back-spur, an iron collar
  carrying one bound Soul, a counterweight spike; the Cannon is a braced
  reliquary with a caged chamber and a flared fluted mouth.
- **All twelve sprite-VFX sheets re-authored** (`tools/visual-max/vfx_forge.py`).
  The Burning detonation's full-frame noise square is gone; peak frame coverage
  fell from 81% to 11%, and there is an asserted ceiling in the tool.
- **Effect glows widened and weakened**, and the particle glow tightened, so the
  authored effects are no longer covered by the light laid over them.
- New deterministic fixture: `burning-detonation`, keyed off the real state
  change rather than a guessed tick.

### Added by Session 3 — character, animation and pixel-language reset

- **The Warden is authored from a rig** (`tools/visual-max/warden_forge.py`), not
  generated per direction. One body plan, one palette, one camera and a real
  ground-plane projection, so the eight directions are one character rotating
  instead of eight different creatures.
- **Human-first protagonist.** Wings, horns and the rifle/scythe hybrid removed;
  visible face, hands and legs; a plain wood-and-iron scythe; a rust-red scarf.
  The supernatural budget is two ember eyes, a two-pixel bound Soul and a thin
  Death Flame line on the trailing hem.
- **Real animation.** 12-frame idle, 12-frame run and a new 6-frame attack. The
  run is driven by distance travelled rather than by a clock, so the gait is
  correct at every movement multiplier.
- **Stable facing.** Bounded body turn rate, eight-sector selection with
  hysteresis, and reversed playback when backpedalling. `FacingDirection` is
  still the raw aim vector, so no combat value changed.
- **Exact pixel scale.** One authored pixel is one screen pixel at combat zoom.
- **Weapon cohesion.** The scythe lives in the sheet at rest; the swing sprite is
  built by the same routine as the carried one, pivots from the butt of the haft,
  and its scale is solved from the arc radius so the blade reaches where the
  light does.
- **The elder brother has his own sheet** from the same rig — hood, mantle, no
  scarf — so the brothers differ structurally rather than only by mass and tint.
- **Two leftover hard-line violations removed** (Soul Cannon charge rings and
  feed line), and **threat telegraphs moved beneath the actors** so the Warden is
  no longer painted over by the attack he is standing in.
- Three new deterministic fixtures: `facing-sweep`, `run-cycle`, `strafe-read`.

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
- The attack is one clip for all three combo steps plus Severance, sampled by the
  Scythe's own progress; per-step poses are the next animation step.
- The particle system still draws filled circles rather than authored sprites;
  it was tuned down rather than re-authored this session.
- No dash, hurt or death clips. The downed pose is still the idle frame rotated
  and darkened — temporary art.
- The enemies and environment are still in the delivered drawing language and
  have not been brought into the authored one. This is the largest remaining
  visual inconsistency.
- The exact 1:1 pixel scale holds at combat zoom only; the title, intro and
  co-op group zooms resample.
- No controller hardware was available for verification.
- The new audio cues have not been auditioned by ear.
- No story flow, zones, dialogue, homebase, co-op, items, save or progression exists yet.
- Several older design documents predate the current Warden cosmology and product direction.

## Active objective

Owner judgement of the corrected character, animation and facing **in motion**,
plus the still-outstanding review of the refined Golden Slice, the co-op proof
and the audio audition. On approval, continue with
[`../agent-prompts/03-DEATH-LAYER-PROLOGUE.md`](../agent-prompts/03-DEATH-LAYER-PROLOGUE.md).

Do not multiply content until the owner approves the resulting combat and visual direction.
