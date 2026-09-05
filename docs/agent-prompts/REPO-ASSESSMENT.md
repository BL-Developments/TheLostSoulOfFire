# Repository Assessment

Assessment snapshot: this records the repository before `AGENTS.md`, `docs/current/` and the implementation prompts were added. For current authority, follow [`../current/README.md`](../current/README.md).

## Verdict

The prototype is worth continuing. It is no longer a trivial starter: it already proves most of the mechanical vocabulary needed for The Lost Soul of Fire. Its strongest reuse value is as a **combat, animation, audio and VFX laboratory**.

It is not yet a foundation for a complete game without a small structural transition. The current runtime assumes one Player, one baked arena, one hardcoded four-wave loop and one ending. Story, explorable regions, local/online co-op, items, progression, save data, dialogue and a homebase do not exist.

The fastest sensible route is therefore:

> Keep the working combat and presentation systems, turn the current arena into a gold-standard encounter, prove two-player combat in that encounter, then build the first Death-Layer story route from the proven pieces.

## What was verified

- `dotnet build TheLostSoulOfFire.sln -c Release` passed with 0 warnings and 0 errors.
- The built-in automated loop completed all four waves, reached completion and restarted successfully.
- Twelve real renderer scenarios captured successfully: title, idle, dash, Scythe combo, all three enemy signatures, Cannon + Soul Sense, busy Resonance, Soul Release, death/retry and ending.
- The game currently targets .NET 9 and MonoGame DesktopGL.

## What already works and should be reused

| System | Current value | Reuse decision |
|---|---|---|
| Player movement | responsive top-down movement, dash, aim and combat state | keep and tune |
| Scythe | three-step combo, arcs, knockback, hit timing and VFX hooks | keep as primary melee foundation |
| Soul Cannon | draw, charge stages, recoil, projectile, Core interaction | keep as primary heavy weapon foundation |
| Soul Sense | weak-point/Core reveal and presentation layer | keep; expand into narrative investigation later |
| Resonance | resource, activation and enhanced movement/weapons | keep; ownership must change for co-op |
| Soul lifecycle | Exposed, BeingDevoured, Releasing, Residue, Released, Consumed | strongly reusable and aligned with new lore |
| Hollow | basic approach/telegraph/swipe role | keep as teaching enemy |
| Burning | charge, Cannon-triggered detonation and enemy-only AoE | keep; useful seed for reactive combat |
| Devourer | prioritizes exposed Souls, devours and can be interrupted/extracted | keep; unusually strong expression of the game’s identity |
| Combat presentation | hitstop, shake, recoil, impact frames, trails, flashes | keep; tune after gameplay changes |
| Renderer | scene target, emission pass, soft halo, Soul Sense layer, reduced-effects mode | keep and extend |
| Art assets | Player, three enemy families, directional animations, weapons, twelve VFX sheets | reuse as prototype/gold-slice assets |
| Audio | music, ambience and 27 SFX assets with centralized playback | reuse; re-author selectively after style approval |
| Capture harness | twelve fixed scenarios and state sidecars | keep; this is essential for autonomous AI iteration |
| GameBalance | centralized combat constants | keep as tuning surface |

## What the screenshots show

### Strengths

- The violet-white Death Flame reads consistently.
- Player attacks and Soul Cannon have distinct silhouettes.
- Hollow, Burning and Devourer are distinguishable.
- Soul Release is visually separate from enemy destruction.
- The orange Life Flame is rare and immediately noticeable.
- The visual capture tooling makes before/after review feasible.

### Current visual ceiling

- The combat floor is almost entirely black and carries little place, material or traversal information.
- Most authored architecture is confined to the upper edge and corners.
- Player and enemies occupy a small part of a large empty frame.
- Actor grounding and contact with the floor are weak.
- Threat shapes are technically readable but sometimes too thin or low-contrast for dense future combat.
- The Player silhouette is striking but currently feels more like a creature/boss than a newly dead human whose vulnerability should remain emotionally legible.
- The environment does not yet have the Children-of-Morta-like spatial depth required by the new art direction.

This is a presentation problem, not a reason to replace the renderer or engine.

## Structural gaps

| Gap | Evidence in current runtime | Why it matters |
|---|---|---|
| World flow | `GameWorld` owns title → intro → combat → transition → complete | cannot yet support maps, homebase or story route cleanly |
| One fixed arena | `Arena` and one 1800×1000 baked background | cannot express a Diablo-like region without a minimal zone/segment layer |
| Single Player | one `_player` field is passed directly into enemies, Souls, HUD, camera and combat | co-op becomes expensive if content is multiplied first |
| Single input model | one keyboard/mouse `InputState` | second player/controller requires a player-command boundary |
| Single-target AI | enemy methods receive one concrete Player | enemy targeting must become player-set-aware before co-op |
| Single-player camera/HUD | camera follows one position; HUD renders one Player | needs group framing and two-player identity |
| Hardcoded encounter | four waves are directly spawned in `GameWorld` | usable as test encounter, not scalable world content |
| No narrative state | no dialogue, story beat, trigger or region progression model | first story section needs a very small data-driven beat layer |
| No items | explicitly excluded by old MVP scope | first item proof must be deliberately small, not a full inventory |
| No save/progression | old MVP intentionally excluded both | acceptable for the first playable; required before larger production |
| Monolithic world class | `GameWorld.cs` is over 1,100 lines | should be separated only along immediate scene/encounter/player seams |

## Documentation problems that must be fixed first

The repository contains strong planning, but some documents now conflict with the new vision:

- `docs/mvp/00_MVP_VISION.md` calls the Player a Lost Soul and treats co-op/items/hub as forbidden scope.
- `docs/vision/FULL_VISION.md` predates the refined Death Flame → transition → Warden cosmology.
- `docs/vision/CODEX_CURRENT_REPO_ADDENDUM.md` says the repository is still a minimal starter, which is no longer true.
- `docs/visual-max/HANDOFF.md` and `tools/visual-max/README.md` link to Astra files/scripts that are not present in this branch.
- At assessment time there was no root `AGENTS.md`, so future sessions could accidentally treat stale `DESIGN STATUS: LOCKED` documents as current authority. This has since been corrected.

This is the largest risk to an AI-heavy workflow: the AI can execute extremely well against the wrong source of truth.

## What should not be rebuilt

- Do not migrate away from MonoGame to chase editor convenience.
- Do not rewrite combat into an ECS or generic ability framework.
- Do not replace the Soul lifecycle.
- Do not discard the current sprites, VFX or audio before a playable replacement is visibly better.
- Do not build procedural generation before one authored Death-Layer route is fun.
- Do not build online networking before local two-player combat proves the design.
- Do not build a large inventory, crafting system or item database before six meaningful modifiers are fun.

## Minimal new architecture

Only introduce seams required by the next playable:

```text
Game1
  └── GameFlow / SceneDirector
        ├── TitleScene
        ├── DeathLayerRouteScene
        ├── CombatEncounter
        └── HomebaseArrivalScene

PlayerSlot
  ├── Player
  └── PlayerCommandSource

WorldZone
  ├── bounds and camera limits
  ├── authored segment connections
  ├── encounter triggers
  └── story beats
```

This is enough for the first story slice. It does not require a general-purpose level editor, quest engine, event bus or network stack.

## Items recommendation

Items are absent because the old MVP explicitly excluded them. Do not solve this with a generic inventory.

The first item slice should contain six **Soul Echoes** or another lore-compatible working name:

- three simple numerical/behavior modifiers;
- two reaction-based modifiers;
- one risky Soul-Resonance modifier;
- one choice between three after a major encounter;
- run-local only;
- no rarity tiers, shops, crafting, equipment grid or save persistence.

This proves build identity quickly and gives AI a bounded pattern to expand later.

## Overall recommendation

The prototype already answers “can the core verbs exist?” The next build must answer:

> Does one encounter look, sound and react strongly enough that we want the entire game to follow this exact grammar?

Do not build the full prologue until that answer is yes.
