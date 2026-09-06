# Decision Log

Record owner-approved, rejected or revised product decisions here. Newer dated entries override older conflicting entries.

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
