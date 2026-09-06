# Decision Log

Record owner-approved, rejected or revised product decisions here. Newer dated entries override older conflicting entries.

## 2026-09-06 — Owner revision: the scythe is a held weapon, and the dash is a real out

### Reviewed

Camera, whole-body motion, weapons and VFX accepted. Two things named, then one
correction on the first.

### Revised

- **The scythe must read as a weapon someone is holding, not a prop attached to
  him.** First it was planted upright like a staff; corrected, it hung down at
  the floor like a farm tool; corrected again, it is now level across the body —
  a combat guard, seen side-on.
- **The chain keeps three hits**, choreographed: a sweep to the right, an answer
  back to the left, and a full turn on the third.
- **Every attack must resolve back into the hold**, not cut to it.
- **The dash interrupts any attack.** Waiting out a swing before being allowed to
  move was rejected outright.
- **Dash invulnerability must be visible.** The Warden goes "a little shadowy"
  for exactly as long as he cannot be hit.
- The Soul Cannon is finished.

### Implemented in response

- The pose now describes **where the scythe is**, and the hands are placed *on
  the haft*. Before this the hands and the weapon were posed independently,
  which is the reason the carry never flowed into a swing — they were never
  connected.
- The guard sits at about −56° from the aim, which is **exactly where the first
  hit winds up from**, so the hold is the start of the swing rather than a pose
  it has to leave.
- Three attack clips, each placing the hands on the overlay's haft using the
  runtime's own swing angle, so the Warden always holds the weapon being drawn
  for him. The third turns the whole body through a full revolution and arrives
  back where it started. All three ease their grip back onto the carry.
- The overlay is lifted to chest height so the blade and the hands meet; this is
  the one place the flat combat plane and the three-quarter character view are
  made to agree, and the constant is shared by the runtime and the forge.
- `ScytheCombat.CancelForDash` — a strike already created still lands, everything
  after it is abandoned, and the chain position is kept so dashing out of a swing
  and swinging again continues the combo.
- `Player.PhaseAmount` drives a darkening and thinning of the body that rises
  with the dash and falls with the i-frame window, so what is seen is exactly how
  long he is untouchable.

### Still open

Owner has not judged the guard, the chain choreography or the dash.

## 2026-09-06 — Owner revision: camera, whole-body motion, weapons and VFX

### Reviewed

The corrected character was accepted ("the character looks decent now"). Four
things were rejected as still below a professional bar.

### Revised

- **The camera must follow like a good action game does.** Children of Morta was
  named as the reference for *how the frame behaves*, not for its content. A
  camera that sits exactly on the player and tracks at a constant rate is not
  acceptable.
- **Animation must move the whole body.** Feet moving under a static torso reads
  as a puppet. Weight shift, counter-rotation and secondary motion are required.
- **The scythe and the Soul Cannon are not imposing enough.** They read as a farm
  tool and a pipe.
- **Every VFX is below standard.** Named example: the Burning's detonation "is
  like a square at the end". The complaint was explicitly general — all effects.
- **Enemy behaviour is not to change.** Only how it looks.

### Implemented in response

- **Camera rebuilt** (`Rendering/Camera2D.cs`): velocity and facing look-ahead
  with its own smoothing, a soft zone so small adjustments do not move the frame,
  critical damping instead of a fixed-rate lerp, vertical restraint, and a small
  bias toward the fight. Plus a zoom punch on impact.
- **Whole-body motion** in the rig: pelvis sway onto the stance leg, shoulder
  counter-rotation against the hips with the head overshooting late, arms
  crossing inboard, and planted feet excluded from all of it so nothing slides.
- **Weapons re-shaped**: a deep recurved blade with a back-spur, an iron collar
  holding one bound Soul, a counterweight spike and an S-curved haft; the Cannon
  became a braced reliquary with a caged chamber and a flared fluted mouth.
  Detail level unchanged — the weight comes from silhouette.
- **All twelve effect sheets re-authored** (`tools/visual-max/vfx_forge.py`) as
  effects rather than pictures: quantised energy fields, particles simulated once
  and sampled per frame so embers actually travel, shockwaves that thin as they
  expand, broken rather than perfectly circular rings, and an asserted coverage
  ceiling so nothing can become a square again.

### Measured

The old `fx_burning_detonation` covered 79–81% of its frame for three frames and
then spent nine frames as full-frame speckle — that is the square. The authored
one peaks at 11%. `fx_resonance_activate` went from 72% to 7%.

### Still open

Owner has not judged the camera, the reworked animation, the weapons or the VFX.

## 2026-09-06 — Owner revision: the character and the pixel language were wrong

### Revised

- **The protagonist is human first and supernatural second.** The winged,
  horned, over-accessorised reading is rejected. He is a person carrying a
  scythe who happens to be dead.
- **Detail is not quality.** Sheets that carry hundreds of near-identical
  colours and one-pixel ornament read as an over-rendered image pretending to be
  pixel art. Larger forms, stronger clusters, cleaner materials and fewer shiny
  surfaces are the standard.
- **Animation quality is a first-class requirement**, not something to be
  revisited after content. Walk smoothness, idle stability and readable attacks
  are acceptance criteria.
- **Facing must never look broken when the mouse moves.**
- This correction happens **before** any further content.

### Implemented in response

- `tools/visual-max/warden_forge.py` — the protagonist and his brother are now
  **authored from one rig** rather than generated per direction. One body plan,
  one palette, one camera, a real ground-plane projection and depth-sorted
  parts, so the eight directions are one character rotating.
- Character reset: wings, horns and the rifle/scythe hybrid removed; a visible
  face, hands and legs, a plain wood-and-iron scythe, one leather belt, one
  strap and a rust-red scarf. The entire supernatural budget is two ember eyes,
  a two-pixel bound Soul at the sternum, and a thin Death Flame line on the
  trailing hem.
- 24 colours in six material families; flat fills, hard three-value shading from
  one key light, no dithering, no anti-aliasing.
- Real 12-frame idle and run and a new 6-frame attack. **The run is driven by
  distance travelled, not by a clock**, so the gait is correct at every movement
  multiplier.
- Facing: bounded body turn rate, eight-sector selection with hysteresis, and
  reversed playback when backpedalling. `FacingDirection` remains the raw aim
  vector, so **no combat value changed**.
- `WardenDisplaySize = 128 / CombatCameraZoom`: one authored pixel is one screen
  pixel at combat zoom.
- The elder brother gets his own sheet from the same rig — hood, mantle, no
  scarf — resolving the Session 2 finding that the brothers separated only by
  mass and tint.

### Follow-on corrections found while implementing

- **The Soul Cannon still drew hard vector circles and a line in the fighting
  plane.** The 2026-09-06 "no hard lines in combat" revision had been applied to
  telegraphs and trails but missed the Cannon. Now painted with the feathered
  brush in the additive pass.
- **Threat telegraphs were drawn over the actors.** A Hollow's swipe band was
  painted across the Warden standing in it. Telegraphs are floor light and are
  now drawn beneath the actors. Same cues, same alpha, correct layer.
- **Actor light was over-driven.** It had been raised in Session 2 because the
  delivered sheet had no value of its own (median 22/255 against a 21/255
  floor). The authored sheet measures 56/255, so the Warden's silhouette light
  and bound-Soul core were roughly halved. Anything else washed the new art out.

### Still open

Owner has not judged the corrected character, animation or facing.

## 2026-09-06 — Owner revision: generated audio must not ship raw

### Revised

- **Raw ElevenLabs output is rejected.** Reviewed during Session 2 and described
  as sounding "raw and not professional" next to the existing bank.
- Generated audio is **source material only**. Every shipped cue must be produced
  locally: an already-approved sample carries the body, generated material sits
  underneath as texture, and the result must measure inside the existing bank's
  duration, level, brightness and noise-floor band.

### Implemented in response

`tools/audio/generate_soulfire_sfx.py` now requests seconds of headroom and
lossless PCM with short foley-brief prompts. `tools/audio/build_session2_sfx.py`
performs onset trimming, downward expansion, brightness matching, transient
shaping, explicit decay, layering and category peak normalisation. Six cues were
produced this way; ten existing cues were reviewed and deliberately left alone.

### Still open

The new cues have **not been auditioned by ear** — the agent cannot listen.
`artifacts/session2/audio-audition/` holds the shipped `hybrid` build and a
`bank-only` build for A/B. Owner approval required.

## 2026-09-06 — Session 2 direction (implemented, not yet approved)

### Proposed and implemented

- **Soulfire actor light grammar.** Living Death Flame light — violet-white —
  belongs to Wardens and Souls. Manifestations are separated by cold ash light
  only; their violet appears at the Anchor and the fractures. Introduced because
  measurement showed the Warden sheet and the casting floor sat at the same
  luminance, leaving the protagonist with no figure/ground separation at all.
- **Local co-op is a team, not two soloists.** Resonance is one shared pool;
  earning it credits the team, either brother may spend it, and spending it
  lights both. Residue feeds the same pool, so there is no pickup to race for.
- **The brothers share one Death Flame tether.** Separation is expressed as
  strain on that tether and a gradual draw-back, never a teleport, never
  split-screen.
- **Going down is not dying.** A Warden whose flame gutters while a brother
  stands cannot act, cannot be hit, and cannot be finished off. Both down ends
  the encounter. Solo death is unchanged.
- **Brother identity is temperature, tint and mass** over the same Warden sheet
  and the same kit — no second class.

### Still open

Owner has not judged the refined Golden Slice, the co-op proof, or the audio.

## 2026-09-06 — Owner revision: no hard lines in combat

### Revised

- **Combat feedback must be animation and VFX only.** Hard, shiny vector lines in
  the fighting plane are rejected: they read as interface laid over the pixel art
  and make the game feel inorganic.
- Applies to telegraphs, swing trails, auras, tethers, weak-point markers,
  detonations and death effects. Environment/prop linework and debug overlays
  (F1) are not affected.

### Implemented in response

All combat cues are painted with a feathered brush in a dedicated additive,
linear-filtered pass (`Rendering/SoftShapes.cs`, `GameWorld.DrawCombatLight`).
Telegraphs became gathering light instead of outlines. The arena centre pulse
ring and the aim crosshair were removed.

### Still open

Owner has not yet judged the Golden Combat Slice as a whole.

## 2026-09-05 — Current foundation

### Approved direction

- Vaelor is the First/Highest Warden, not a god.
- The Keeper is an unknown entity beyond the boundary.
- The Stillness is the exceptional communication path toward the true afterlife.
- Wardens are a rare new existence formed through mastery or resonance with the Death Flame.
- Wardens age extremely slowly and pass fully onward when their Warden form is destroyed.
- The story begins in the uncivilized Death Layer.
- In cooperative play, the second player is the protagonist's brother and is already a Warden.
- Children of Morta is the visual quality reference; Soulfire Gothic remains the original identity.
- Combat should reward well-timed reactions with interesting attacks or opportunities.
- The first milestone is a gold-standard encounter, followed immediately by a local co-op proof.

### Unresolved

- Exact metaphysical selection process for new Wardens.
- Precise nature and intent of the Keeper.
- Exact rules, limits and origin of the Stillness.
- Final form of Soul-based alternative attacks such as detonating residue.
- Final item taxonomy and long-term progression structure.
- Exact scale and generation method of later Death Layer regions.

Use [`CANON-STATUS.md`](CANON-STATUS.md) for the full Canon / Non-Canon / Unresolved classification.
