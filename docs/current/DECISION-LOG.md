# Decision Log

Record owner-approved, rejected or revised product decisions here. Newer dated entries override older conflicting entries.

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
