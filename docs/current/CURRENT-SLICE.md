# Current Slice

Status: owner-reviewable first playable implemented on `prototype/design-polish`.
The refined Golden Combat Slice remains available with `--golden-slice`; normal
launch now starts the authored Death-Layer prologue.

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

### Added by the first playable prologue

- A complete authored route with a beginning, middle and ending: death memory,
  Emergence, Search/Recovery, the already-Warden brother, Escape/Transit and the
  Warden homebase threshold.
- Three distinct combat/exploration compositions plus the exterior threshold,
  using one clean slab/stone/iron material grammar, human residue and restrained
  violet-white Death Flame.
- The real Hollow, Burning, Devourer, Severance and Soul Release systems teach
  the route; no parallel tutorial combat was created.
- Solo receives a bounded combat/rescue companion after the meeting. Two-player
  launch hands the brother to a gamepad or the documented second-keyboard map.
- Sector-scoped retry, full-party failure, clean replay and D1–D4 review jumps.
- A 62-second Death-Flame skiff defense which keeps normal movement, Scythe,
  Cannon, reaction combat and co-op roles rather than becoming a minigame.
- Three authored soundscape derivatives from the approved AMB-01 ambience,
  with sector-specific silence/music balance and no new external generation.
- Ten deterministic prologue fixtures plus semantic solo and two-player full-
  route smoke runs.

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
  on 37 assets, including all three prologue ambience loops.
- Twelve solo and eleven co-op visual scenarios capture successfully, including
  reduced-effects and baseline-quality variants.
- The semantic prologue route reaches the real completion state with no enemy,
  Soul or checkpoint soft-lock; its force-cleared lower-bound run is 107.8 s.
- The two-player semantic route also reaches completion through the shipped
  second-keyboard control source in 105.9 s.

## Current limitations

- The route uses four authored 1800×1000 compositions and instant sector cuts;
  it is not a streaming world or a general scene framework.
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
- The new prologue ambience mixes were validated technically and heard only
  through runtime playback, not approved by the owner on a reference system.
- Human playtime has not been stopwatch-verified; 8–12 minutes is the intended
  first-run pace, while the automated force-clear smoke run is deliberately much
  faster.
- Homebase stops at the exterior threshold. There is no interior hub, save slot,
  dialogue tree, item system or broader progression yet.
- Several older design documents predate the current Warden cosmology and product direction.

## Active objective

Owner playthrough of the complete prologue, with particular attention to first-
run duration, character motion in the larger spaces, the brother handoff, held-
Soul release, vehicle pressure and sector soundscape contrast. Revise this route
before starting items, hub interiors or Content Factory work.
