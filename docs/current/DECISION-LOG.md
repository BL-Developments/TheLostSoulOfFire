# Decision Log

Record owner-approved, rejected or revised product decisions here. Newer dated entries override older conflicting entries.

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
