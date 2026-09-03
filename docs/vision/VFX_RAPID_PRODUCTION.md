# Rapid VFX Production Pack

**PURPOSE:** Generate and integrate the MVP VFX while Codex implements gameplay.

This document assumes the mechanics in `docs/mvp/` are implemented exactly as specified.

The goal is speed.

Do not manually perfect every effect before seeing it in-game.

---

# 1. Recommended Workflow

For this MonoGame prototype, prefer **rendered transparent sprite sheets + simple runtime particles** over engine-specific VFX formats.

Why:

- MonoGame can load ordinary PNG textures easily.
- AI tools can produce sprite sheets quickly.
- Effects can be replaced later without redesigning gameplay.
- Runtime code only needs generic sprite-sheet playback, tint/scale/rotation, and simple particles.

Use a hybrid:

### Generated sprite-sheet VFX
For:
- major impact bursts
- Cannon muzzle blast
- Cannon projectile/beam texture
- Burning explosion
- Resonance activation
- Soul Release burst
- Core crack/impact

### Procedural/runtime VFX
For:
- small particles
- trails
- afterimages
- Core pulse
- Soul Sense overlay
- screen flash
- camera shake
- simple glow
- drifting ambient Death Flame

---

# 2. Tool Recommendation

## Fastest one-tool experiment: Ludo

Ludo is attractive for this sprint because it can generate sprites, animations, VFX-style animated assets, audio, and other game assets from one platform, and exposes API/MCP workflows.

Use it if the priority is:

> generate many different asset types quickly and keep the workflow agent-friendly.

## Best if strict pixel-art control becomes important: PixelLab

Use PixelLab when:

- character sprites become the bottleneck
- directional sprites are needed
- true pixel-art consistency matters
- tilesets are needed

## Best later for a trained house style: Scenario

Use Scenario after a small approved art library exists.

Train the game's own style from curated images and use it for consistent future world/prop/character generation.

This is more valuable for the full game than for tomorrow's one-day MVP VFX sprint.

---

# 3. VFX Folder Contract

Create:

```text
Content/Textures/Effects/
├── Scythe/
├── Dash/
├── Cannon/
├── SoulSense/
├── Souls/
├── Resonance/
├── Enemies/
└── Ambient/
```

Recommended file naming:

```text
fx_scythe_hit_01.png
fx_scythe_hit_02.png
fx_scythe_cleave.png
fx_core_hit.png
fx_dash_ignition.png
fx_cannon_muzzle_full.png
fx_cannon_projectile_full.png
fx_burning_detonation.png
fx_soul_release.png
fx_resonance_activate.png
fx_death_flame_loop.png
```

Do not rename files repeatedly after Codex references them.

---

# 4. Universal Generation Rules

Every generated VFX prompt should include:

```text
isolated 2D game visual effect
transparent background
centered
no character
no environment
no text
no UI
no border
high contrast silhouette
readable at small scale
violet and deep purple energy
near-white energetic core
sharp supernatural flame fragments
unnatural sideways and downward flame motion
dark gothic anime action-game aesthetic
```

Avoid:

```text
photorealistic
3D render
realistic smoke simulation
orange fire
blue magic
rainbow colors
complex background
character holding effect
text
logo
```

Unless the specific effect requires otherwise.

---

# 5. Effect List — Generate in This Order

## VFX-01 Scythe Slash 1

Purpose:
Fast light slash.

Target:
- thin ~120° arc
- deep violet
- small light-violet inner edge
- minimal white
- short lifetime

Prompt core:

```text
isolated 2D game VFX sprite sheet, fast thin crescent scythe slash,
roughly 120 degree arc, deep violet supernatural flame, light purple inner edge,
sharp fragmented death-fire particles moving sideways and downward,
very fast attack, transparent background, no weapon, no character,
dark gothic anime action game, readable top-down effect
```

Recommended:
- 6–8 frames
- one-shot
- fast

---

## VFX-02 Scythe Slash 2

Target:
- wider ~140° reverse arc
- brighter than Slash 1
- more fragments

Prompt:
Use VFX-01 prompt but request a larger reverse crescent and stronger inner glow.

---

## VFX-03 Soul Cleave

This matters a lot.

Target:
- ~180–200° massive arc
- dark violet exterior
- vivid violet body
- near-white core
- violent fragmented edges
- readable impact peak

Prompt core:

```text
massive isolated 2D scythe finisher VFX sprite sheet,
wide 190 degree crescent slash,
dark violet outer flame, saturated purple energy,
near-white compressed core along the cutting edge,
sharp supernatural fragments exploding backward,
anime impact attack, violent but clean silhouette,
transparent background, no character, no weapon, top-down action game
```

Recommended:
- 8–12 frames

---

## VFX-04 Soul Core Hit

Target:
- tiny but extremely bright
- white center
- violet crack burst
- radial fragments

Prompt:

```text
isolated 2D weakpoint impact VFX,
tiny near-white soul core cracking open with violet energy,
sharp radial purple fractures, bright center flash,
compact anime hit effect, transparent background,
very readable at small scale
```

---

## VFX-05 Dash Ignition

Target:
- initial propulsion burst
- not a long trail
- violet-white
- directional

Prompt:

```text
isolated 2D supernatural ignition burst,
violet-white death flame exploding backward from a fast dash,
sharp directional fragments, compressed white core,
unnatural purple flame curling sideways and downward,
short explosive propulsion effect, transparent background
```

Runtime should still create afterimages/trail.

---

## VFX-06 Cannon Charge Loop

Target:
- energy converging inward
- not exploding outward
- purple particles collapsing toward white center

Prompt:

```text
looping isolated 2D soul energy charge VFX,
deep violet particles and sharp flame fragments pulled inward
toward a tiny near-white compressed core,
gothic supernatural weapon charge,
energy converges rather than explodes,
transparent background, centered
```

Recommended:
- 8–12 frame loop

Scale/tint it at runtime for charge stages.

---

## VFX-07 Full Cannon Muzzle Blast

Target:
One of the strongest effects.

Prompt:

```text
isolated 2D heavy supernatural cannon muzzle blast,
violent violet-white energy eruption,
near-white compressed center,
deep purple jagged outer flame,
sharp fragments thrown backward by recoil,
huge anime impact but clean readable silhouette,
transparent background, no weapon, no character
```

---

## VFX-08 Cannon Projectile / Beam

Prefer a texture that can be stretched or moved by code.

Target:
- white core
- violet body
- dark purple unstable edges

Prompt:

```text
isolated horizontal supernatural soul cannon beam texture,
bright near-white central energy core,
violet body, deep purple unstable fragmented flame edges,
sharp gothic death-energy aesthetic,
transparent background, no muzzle, no impact, no environment
```

If a projectile is implemented instead of a beam, generate an elongated projectile.

---

## VFX-09 Burning Detonation

Target:
Violent explosion with purple identity.

Prompt:

```text
isolated 2D enemy detonation VFX sprite sheet,
unstable violet soul flame violently collapsing then exploding,
near-white center flash, dark purple smoke-like fragments,
sharp cracked energy pieces, anime action impact,
transparent background, no character, no environment
```

Recommended:
- 10–16 frames

---

## VFX-10 Soul Release

Do NOT make this an explosion.

Target:
- calm
- vertical
- violet to white
- clean
- beautiful

Prompt:

```text
isolated 2D peaceful soul release animation,
small violet spirit light slowly becoming light purple then near-white,
soft upward dissolution, delicate supernatural particles,
calm sacred feeling, no explosion, no violence,
transparent background, dark gothic fantasy game VFX
```

Runtime can separately draw the connection to Player and Residue.

---

## VFX-11 Soul Residue

Target:
Small violet particle/orb.

This may be procedural instead of generated.

If generated:

```text
small isolated violet soul residue wisp,
tiny bright purple supernatural ember with near-white center,
few sharp drifting fragments, transparent background
```

---

## VFX-12 Resonance Activation

Highest-priority transformation effect.

Prompt:

```text
isolated 2D transformation eruption VFX sprite sheet,
violent death-flame burst expanding from a central human-sized core,
deep violet outer flame, saturated purple body,
near-white center, sharp supernatural fragments,
flame moves upward sideways and downward simultaneously,
gothic anime power-up impact,
transparent background, no character
```

Runtime should layer:
- impact frame
- Core white flash
- camera shake
- Player outline
- cracks
- particles

Do not rely on one sprite sheet for the whole transformation.

---

## VFX-13 Death Flame Ambient Loop

Prompt:

```text
looping isolated 2D violet death flame,
small supernatural flame with deep purple edges,
light violet interior and tiny near-white core,
unnatural flame motion drifting sideways and downward,
sharp fragmented tips, transparent background
```

Use sparingly.

---

# 6. Soul Sense Effects

Do not spend generation credits on a full-screen Soul Sense animation first.

Implement primarily in code:

- dark/desaturated overlay
- vignette
- purple/white highlights
- Player eye glow
- Core markers
- trapped Soul markers

Optional generated assets:

- small Soul Core loop
- Soul Fracture texture
- Soul Trace wisp

---

# 7. Enemy-Specific VFX

## Hollow

Generate only if needed:

- mask crack
- Core crack
- dark dissolve fragments

Most can be procedural.

## Burning

Worth generating:

- Charge telegraph flare
- Detonation

## Devourer

Prefer procedural:

- trapped Soul glow
- Soul pull lines
- torso cracks

Optional:
- Soul-prison break burst for death

---

# 8. Quick Integration Contract for Codex

While Codex works, add generated files only under the agreed Effects folders.

Then give Codex this instruction:

```text
Integrate the new VFX assets under Content/Textures/Effects without changing any locked gameplay behavior.

Use the generated sprite sheets for their matching effects.
Preserve all existing timings and mechanics unless a purely visual timing adjustment is necessary.

If metadata is not present, infer a uniform horizontal or grid frame layout from the texture dimensions and keep the animation configuration centralized.

Do not redesign the VFX system.
Do not introduce a third-party runtime.
Keep procedural particles, camera shake, hitstop, overlays and afterimages in code.

Build and verify after integration.
```

---

# 9. Sprite Sheet Runtime Requirements

The game only needs a small generic animated-effect primitive.

Conceptually:

```text
Texture
FrameWidth
FrameHeight
FrameCount
FramesPerSecond
Loop
Position
Rotation
Scale
Origin
```

Do not build an animation framework for future characters.

For one-shot VFX:

`Spawn → animate → remove`

For loops:

`Spawn/attach → loop → remove when owning state ends`

---

# 10. Practical Production Sprint

While Codex implements Phases 01–04:

Generate:

1. Scythe Slash 1
2. Scythe Slash 2
3. Soul Cleave
4. Dash Ignition
5. Core Hit

While Codex implements Phases 05–08:

Generate:

6. Soul Release
7. Cannon Charge
8. Cannon Muzzle
9. Cannon Beam/Projectile

While Codex implements Phases 09–12:

Generate:

10. Burning Detonation
11. Resonance Activation
12. Ambient Death Flame

Then integrate everything before the final polish phases.

---

# 11. Selection Rule

For each effect:

1. Generate 4–8 variants.
2. Pick the one with the cleanest silhouette.
3. Ignore tiny detail differences.
4. Test it at actual gameplay size.
5. Reject effects that only look good enlarged.
6. Reject effects with muddy alpha/background contamination.
7. Reject effects that introduce blue, orange, green, or rainbow magic into Death-Flame VFX.
8. Prefer readable motion over detailed artwork.

---

# 12. Consistency Reference

Once 3–5 effects look correct, use them as visual references for later generations.

Create a reference set:

```text
STYLE_REFERENCE/
├── scythe_cleave.png
├── core_hit.png
├── cannon_muzzle.png
├── soul_release.png
└── resonance_activate.png
```

These establish:

- purple hue
- white-core intensity
- fragmentation language
- line sharpness
- effect density

Do not continuously reinvent the style through new prompts.

---

# 13. What Not to Generate

Do not waste time generating:

- camera shake
- hitstop
- vignette
- screen flash
- basic afterimages
- simple projectile trails
- basic Core pulse
- simple Soul pull lines
- tiny ambient particles

Code handles these better.

---

# 14. Definition of Done

The VFX sprint is successful when:

- Scythe hits have three clearly different weights
- Dash has a recognizable Death-Flame ignition
- Core hit is instantly readable
- Cannon charge visibly escalates
- full Cannon shot feels huge
- Burning detonation is unmistakable
- Soul Release feels calm rather than violent
- Resonance activation feels like the largest Player power moment
- all Death-Flame effects share the same violet-white visual language
- effects remain readable during mixed combat
