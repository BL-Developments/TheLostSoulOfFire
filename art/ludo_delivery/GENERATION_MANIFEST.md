# Ludo MVP Generation Manifest

Finalized: 2026-08-29 (Europe/Berlin)

## Outcome

All required MVP art is generated, normalized, reviewed, processed by the MonoGame Content Pipeline, and integrated. The accepted delivery contains **7 required static assets**, **96 required directional character sheets**, and **12 required VFX sheets**. The three locked sources retain their preflight SHA-256 hashes. No optional-polish credits were spent.

The current Ludo MCP exposes no balance endpoint, so starting and ending balances are unavailable. Estimated consumption is **469.5 credits**, calculated from the endpoint prices returned by the MCP for 162 successful billed calls: 8 static-generation calls including the Hollow reroll, one background removal, 28 directional rotations, 96 character animations, 14 VFX-master calls, and 15 VFX-animation calls including the bounded cleave/core-hit/soul-release rerolls. This estimate is not a balance reading.

## Locked references

| Asset ID | Locked path | SHA-256 | Dimensions / transparency | Visual audit | Decision |
|---|---|---|---|---|---|
| `reference.style_anchor` | `art/ludo_delivery/00_reference/style_anchor_gameplay.png` | `7651721ff0bcf4d806dbfcd0d3c2ae9e7de8c227032160c203bdba214bcebba8` | 1344×768 RGBA, opaque | High three-quarter industrial Gothic arena; dark clear center; restrained violet indicators | Preserved unchanged |
| `character.player.master` | `art/ludo_delivery/02_characters/player/player_master_128.png` | `537c624dcdcc2eab2a6d7aa1c1d7a8e122963429143392c2ca27d5742aecec64` | 768×768 RGBA, transparent | Asymmetric high-angle long-coat silhouette; no baked Core | Preserved unchanged; animation derived without mirroring |
| `weapon.scythe.physical` | `art/ludo_delivery/03_weapons/scythe_physical_256.png` | `864b0dbe3c204e36a7bcebca4bcb02d7139e29f047ccf2994d27874641959eb3` | 1024×1024 RGBA, transparent | Angular segmented weapon; detached violet point at far right | Preserved unchanged; artifact masked only in `scythe_physical_256_clean.png` |

## Required static assets

All rows are required, `complete`, and `accepted`.

| Asset ID | Source references | Ludo tool | Original download | Normalized integration file | Dimensions / layout / direction | Review notes | Content destination |
|---|---|---|---|---|---|---|---|
| `environment.arena_base` | style anchor; `docs/mvp/14_ARENA.md` | `generateWithStyle:fixed_background` | `01_environment/candidates/arena_base_ludo_original.webp` | `01_environment/arena_base_1800x1000.png` | 1800×1000 static; high three-quarter | Clear center; perimeter machinery; no painted collision obstacles | `Textures/Environment/arena_base_1800x1000.png` |
| `character.hollow.master` | style anchor; player scale; Hollow spec | `generateWithStyle:sprite` | `02_characters/hollow/candidates/hollow_master_ludo_reroll.webp` | `02_characters/hollow/hollow_master_128.png` | 128×128 static; S master | First candidate rejected; accepted thin malformed body with dominant off-white mask | `Textures/Enemies/Hollow/hollow_master_128.png` |
| `character.burning.master` | style anchor; player scale; Burning spec | `generateWithStyle:sprite` | `02_characters/burning/candidates/burning_master_ludo_original.webp` | `02_characters/burning/burning_master_128.png` | 128×128 static; S master | Dark industrial body; localized violet fractures | `Textures/Enemies/Burning/burning_master_128.png` |
| `character.devourer.master` | style anchor; player scale; Devourer spec | `generateWithStyle:sprite` | `02_characters/devourer/candidates/devourer_master_ludo_original.webp` | `02_characters/devourer/devourer_master_192.png` | 192×192 static; S master | Heavy silhouette; fixed torso prison; not a generic purple demon | `Textures/Enemies/Devourer/devourer_master_192.png` |
| `weapon.soul_cannon` | locked scythe; style anchor; cannon spec | `generateWithStyle:asset` + `removeBackground` | `03_weapons/candidates/soul_cannon_ludo_original.webp` | `03_weapons/soul_cannon_256.png` | 256×256 static; E-facing | Opaque generation background repaired; constrained energy chamber | `Textures/Weapons/soul_cannon_256.png` |
| `pickup.lost_soul` | style anchor; Soul Release spec | `generateWithStyle:sprite` | `05_pickups/candidates/lost_soul_ludo_original.webp` | `05_pickups/lost_soul_64.png` | 64×64 static; camera-facing | Bright white and pale violet; intentionally brighter than world accents | `Textures/Pickups/lost_soul_64.png` |
| `ending.life_flame` | style anchor; arena/ending spec | `generateWithStyle:sprite-vfx` | `06_ending/candidates/life_flame_ludo_original.webp` | `06_ending/life_flame_128.png` | 128×128 static; camera-facing | Warm orange/gold payoff distinct from combat VFX | `Textures/Ending/life_flame_128.png` |

Paths in the table are relative to `art/ludo_delivery/` and Content destinations are relative to `src/TheLostSoulOfFire/Content/`.

## Required directional animation packs

Every pack is `complete` and `accepted`. Each pack contains eight independent, non-mirrored sheets in direction order `n, ne, e, se, s, sw, w, nw`. Originals are `02_characters/<character>/candidates/animations/<action>/<direction>.webp`; normalized files are `02_characters/<character>/animations/<action>/<direction>.png`; corresponding Content destinations preserve the same action/direction structure. All sheets use row-major order, transparent fixed canvases, and center-origin pivots; player/Hollow/Burning frames are 128×128, Devourer frames are 192×192.

| Asset ID | Ludo tool | Sheets / grid | FPS | Looping | Runtime selection and review notes |
|---|---|---:|---:|---|---|
| `anim.player.idle` | `rotateSprite` + `animateSprite:forge-pixel` | 8 × 9 frames, 3×3 | 9 | loop | Idle state; locked identity preserved; no Core baked in |
| `anim.player.move` | same | 8 × 9 frames, 3×3 | 12 | loop | Velocity-selected; no mirroring; stable crop |
| `anim.hollow.idle` | same | 8 × 9 frames, 3×3 | 8 | loop | Pause/recovery states; off-white mask preserved |
| `anim.hollow.move` | same | 8 × 9 frames, 3×3 | 12 | loop | Approach state; readable irregular gait |
| `anim.hollow.swipe` | same | 8 × 9 frames, 3×3 | 18 | one-shot/state-clamped | Telegraph/swipe states; gameplay hit timing remains 0.42/0.13 s |
| `anim.burning.idle` | same | 8 × 9 frames, 3×3 | 9 | loop | Recovery/stalk states; fractures remain localized |
| `anim.burning.move` | same | 8 × 9 frames, 3×3 | 14 | loop | Approach state; fixed crop and silhouette |
| `anim.burning.charge` | same | 8 × 9 frames, 3×3 | 15 | one-shot/state-clamped | Charge state; gameplay telegraph/charge remain 0.62/0.58 s |
| `anim.devourer.idle` | same | 8 × 9 frames, 3×3 | 7 | loop | Recovery/idle; torso prison remains fixed |
| `anim.devourer.move` | same | 8 × 9 frames, 3×3 | 9 | loop | Player/soul approach; heavy stable contact |
| `anim.devourer.slam` | same | 8 × 16 frames, 4×4 | 16 | one-shot/state-clamped | Telegraph/slam states; gameplay contact remains 0.88/0.18 s |
| `anim.devourer.devour` | same | 8 × 9 frames, 3×3 | 9 | loop while state active | Devour state; gameplay window remains 1.1 s |

Recommended origins: frame center for all characters. Visual feet sit below center by design; collision and world coordinates remain independent of visible bounds.

## Required VFX sheets

All rows are required, `complete`, `accepted`, transparent, row-major, and integrated under `Content/Textures/Effects/`. Source masters/sheets are preserved under `04_vfx/candidates/`.

| Asset ID | Ludo tool | Production file | Frame layout | FPS / loop | Direction / runtime event | QA and selection notes |
|---|---|---|---|---|---|---|
| `vfx.scythe_slash_01` | `createImage` + `animateSprite:forge-pixel` | `fx_scythe_slash_01.png` | 9 × 256, 3×3 | 24 / no | attack-facing; combo step 1 | Thin blade-following arc |
| `vfx.scythe_slash_02` | same | `fx_scythe_slash_02.png` | 9 × 256, 3×3 | 24 / no | attack-facing; combo step 2 | Brighter reverse/readable arc |
| `vfx.scythe_cleave` | same | `fx_scythe_cleave.png` | 9 × 256, 3×3 | 22 / no | attack-facing; combo step 3 | First sheet rejected; single permitted reroll accepted as white-cored crescent |
| `vfx.core_hit` | same | `fx_core_hit.png` | 9 × 128, 3×3 | 30 / no | radial; scythe/cannon weak-point hit | First sheet had baked checker field; single permitted reroll accepted |
| `vfx.dash_ignition` | same | `fx_dash_ignition.png` | 9 × 128, 3×3 | 30 / no | dash-facing; dash start | Directional ignition; procedural trail retained |
| `vfx.cannon_charge_loop` | same | `fx_cannon_charge_loop.png` | 9 × 128, 3×3 | 12 / yes | cannon muzzle; charging | Inward chamber-energy loop |
| `vfx.cannon_muzzle_full` | same | `fx_cannon_muzzle_full.png` | 9 × 256, 3×3 | 24 / no | shot-facing; fire event | Scale/tint reflects charge while retaining shared energy language |
| `vfx.cannon_projectile_full` | same | `fx_cannon_projectile_full.png` | 9 × 128, 3×3 | 18 / while alive | shot-facing; projectile owner | White-core projectile layered over procedural trail |
| `vfx.burning_detonation` | same | `fx_burning_detonation.png` | 16 × 256, 4×4 | 24 / no | radial; detonation event | Collapse/burst remains locally violet-white |
| `vfx.soul_release` | same + deterministic repair | `fx_soul_release.png` | 16 × 128, 4×4 | 12 / no | upward; release/extraction event | Animation reroll still baked a checker field; rebuilt from its accepted transparent Ludo reroll master with nearest-neighbor rise/fade, no new generation |
| `vfx.resonance_activate` | same | `fx_resonance_activate.png` | 16 × 256, 4×4 | 24 / no | radial; activation event | Character-free eruption layered with procedural feedback |
| `vfx.death_flame_loop` | same | `fx_death_flame_loop.png` | 9 × 128, 3×3 | 12 / yes | camera-facing; player death | Violet Death Flame remains distinct from orange Life Flame |

Recommended origin for every VFX clip: frame center. One-shots have clean blank first/last cells where appropriate; looping clips retain continuous content.

## Integration and verification

- Production asset root: `src/TheLostSoulOfFire/Content/Textures/` (**116 PNG files**: 96 animation sheets, 12 VFX sheets, and 8 static/runtime source textures including the derived clean scythe).
- Content Pipeline: `src/TheLostSoulOfFire/Content/Content.mgcb`, PointClamp-compatible textures, no mipmaps, no color key, premultiplied alpha, no power-of-two resize.
- Runtime: `Rendering/ArtAssets.cs` provides clip metadata, frame timing, eight-way direction selection, looping/one-shot playback, and presentation draws. `Effects/SpriteVfxSystem.cs` provides one-shot lifetime/cleanup.
- State mapping: player idle/move; Hollow idle/move/swipe; Burning idle/move/charge; Devourer idle/move/slam/devour. Existing gameplay timers remain authoritative.
- Separate Core: the locked player sheets contain no Core; `Player.Draw` retains the state-driven runtime Core overlay and Soul Sense/resonance response.
- Weapon layering: the derived clean physical scythe and Soul Cannon are rendered as base assets; generated slash/cleave, charge, muzzle, and projectile effects are layered around them.
- Procedural equivalents intentionally retained: collision/debug geometry, telegraphs, hit pause, screen shake, flashes/impact frames, chromatic/presentation effects, afterimages, trails, small particles, Soul pull lines, Soul Sense overlays, runtime Core pulse, and ambient lighting.
- Automated image audit: all required paths exist; static dimensions match; 96 sheets have exact 3×3/4×4 grids and transparent pixels; 12 VFX sheets have exact grids, stable canvases, and transparent pixels; rejected checker-field candidates are preserved under `candidates/`.
- Build: `dotnet build --no-restore` passes with 0 warnings and 0 errors after Content processing.
- Tests: no test project exists in the solution; no automated test suite was available.
- Runtime presentation: DesktopGL launch succeeded, content loaded, first-wave gameplay ran, and `review/06_final_ingame.png` was captured.
- No commit or push was performed.

## Review artifacts

Exactly six derived review PNGs are present in `art/ludo_delivery/review/`: locked references, static cast/weapons, arena/gameplay scale, character animation keyframes, VFX, and final in-game capture.
