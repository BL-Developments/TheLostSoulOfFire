# Current Handoff

## Branch

`prototype/design-polish`

## Current objective

Session 3, a single ordered job: **correct the character, animation and
pixel-art language before any more content is built on top of it.** No new
content areas, no prologue, no items, no engine architecture.

## State: implemented, awaiting owner review

The Session 2 Golden Slice and co-op proof are unchanged in behaviour. What
changed is what the Player looks like and how he moves.

---

## What the baseline actually was

Captured first, before touching anything: `artifacts/session3/before/`, three
new deterministic fixtures (`facing-sweep`, `run-cycle`, `strafe-read`) plus the
Session 2 set in `artifacts/session2/p1-after/`.

Five findings, ranked by owner impact:

1. **The eight directions were eight different creatures.** The delivered Warden
   sheets were generated one direction at a time, so `move/e` was a side-view
   bat-winged demon holding a trumpet, `move/w` was a bird's-eye blob seen from
   almost straight above, and `move/s` and `move/n` were unreadable dark masses
   from a third camera again. Sweeping the mouse did not turn the character; it
   **morphed** him. This is the whole of the "facing/aim looks wrong or broken"
   report, and no runtime code could have fixed it — the frames disagreed with
   each other.
2. **There was no walk.** Consecutive frames of the run sheets are nearly
   identical. What motion existed did not correspond to the movement speed.
3. **The protagonist was not human.** Bat wings, a horned skull, no readable
   face, no legible legs, no visible hands.
4. **The weapon was a separate illustration.** A 256px hyper-detailed
   rifle/scythe hybrid, drawn at a different scale and a different level of
   detail from the body, floating beside and above the character and rotating
   around him. The two weapon textures were also bound crosswise in code to work
   around swapped delivery filenames.
5. **Sub-pixel display scale.** 128px frames were drawn at 108/128 and then
   through a 1.3 camera zoom — 1.097 screen pixels per art pixel. Hand-placed
   pixels were being resampled onto a fractional grid, which is most of the
   "not pixel-authentic, over-rendered image pretending to be pixel art" note.

---

## What changed

### The protagonist is authored, not generated

`tools/visual-max/warden_forge.py` — new. A deterministic pixel-art forge that
builds the Warden from **one rig**:

- one body plan, one palette, one camera;
- a real ground-plane projection (`Rig.project`) through which *every* part
  passes, so the eight directions are one character rotating rather than eight
  drawings that happen to share a name;
- torso and collar built from projected cross-section rings, so the body has
  chest-to-back depth and does not collapse to a stick in profile;
- painter's ordering solved from ground-plane depth, so limbs occlude correctly
  in all eight directions without a per-direction table;
- flat fills, hard three-value shading from one upper-left key light, no
  dithering, no anti-aliasing.

The character design was reset toward human:

| Removed | Kept / added |
|---|---|
| bat wings | a coat that trails when he moves |
| horned skull silhouette | a bare head, hair, a visible face |
| glowing plate and shoulder ornament | one leather belt, one chest strap |
| the trumpet/rifle hybrid | a plain wood-and-iron reaping scythe |
| a stowed second weapon on the back | nothing — the Soul Cannon is manifested, not carried |
| dozens of near-identical colours | 24 colours in six material families |

The supernatural budget is now spent in exactly three places: **two ember eyes,
a two-pixel bound Soul at the sternum, and a thin line of Death Flame on the
trailing hem.** Nothing else on the body emits.

The one saturated note on the whole figure is a rust-red scarf. It is the only
warm colour, it breaks the silhouette, and it is what makes him read as a person
rather than a shape.

### The brother has his own sheet

Session 2 recorded the weakest co-op result honestly: in grayscale the two
brothers differed only in mass and rim intensity. He is now generated from the
same rig with different parameters — **hood up, long mantle, no scarf, heavier
shoulders** — so the difference is structural and survives grayscale, reduced
effects and distance. Both brothers now draw at the same exact pixel scale.

### Animation

- **Idle**, 12 frames: breathing only. Feet planted, no drift.
- **Run**, 12 frames: a genuine gait — contact, stance, toe-off, swing, with a
  knee bend, counter-swinging arms, a two-footfall body bob and trailing cloth.
- **Attack**, 6 frames — new: wind up, plant, cut, recover. Both hands travel
  along the same ground-plane arc the swing overlay does, so the weapon always
  leaves his grip.

**The run is driven by distance travelled, not by a clock.**
`GameBalance.WardenGaitCycleDistance` defines one two-step cycle in world units;
`Player.GaitPhase` advances by `speed * dt / cycle`. The gait is therefore
correct at every movement multiplier — Soul Sense, Resonance, stabilising, full
sprint — instead of being right at one speed and sliding at all the others.

### Aim and facing

`FacingDirection` is untouched: it is still the raw aim vector, and every
hitbox, telegraph, projectile and light still reads it. **No combat value
changed.** Three presentation-only additions sit beside it:

- `Player.BodyFacing` — the body turns toward the aim at a bounded rate
  (`WardenTurnRate` + `WardenTurnAcceleration`) instead of teleporting his
  shoulders onto the mouse every frame;
- `Player.FacingSector` — the eight-way sheet choice, with **hysteresis**. A
  sector is only surrendered once the body is 28° past its centre, so a mouse
  resting on a boundary can no longer flip the sheet every frame;
- `Player.IsBackpedalling` — running against the way he is looking plays the run
  cycle **backwards**, so aiming at an enemy while retreating no longer
  moon-walks.

Idle↔run also became hysteretic (`WardenRunEnterSpeed` / `WardenRunExitSpeed`).

### Pixel language

- `GameBalance.WardenDisplaySize = 128 / CombatCameraZoom`. **One authored
  pixel is exactly one screen pixel** at combat zoom.
- The figure was scaled up ~15% inside the frame after the first in-game pass:
  at the original size his head reached a Hollow's waist and he read as a child.
- The authored sheet measures median luminance **56/255** against a 21/255
  floor, where the delivered sheet measured 22/255. The character now carries
  his own figure/ground separation, so the light could come down:
  - `ActorLighting` Warden silhouette light: base strength 0.50 → **0.24**,
    radius 2.5 → **1.7**, all state boosts scaled with it;
  - `Player.DrawCombatLight` bound-Soul core: radius 16–22 → **9–13**. It was a
    glowing egg laid over his chest;
  - Severance gather moved fully off the body and reduced again.

### Weapon cohesion

- The scythe is **in the sheet** at rest and while running, drawn in the same
  palette and the same value steps as the body.
- `ScytheCombat.DrawRestingScythe` deleted. The only time a separate weapon
  sprite is drawn is during a swing.
- The swing sprite is authored by the **same `_blade` routine** as the carried
  weapon, so they are literally the same object, and its origin is the butt of
  the haft so it pivots out of his grip.
- Swing scale is now **solved from the arc radius** rather than hand-picked per
  combo step, so the blade arrives where the light does.
- `SoulCannon.DrawBack` removed — the Cannon is Death Flame given a shape, so
  there is nothing on his back and the carried silhouette stays one object.
- The crossed texture binding in `ArtAssets` is gone; both weapons are authored
  and named for what they are.

### Two remaining "no hard lines in combat" violations, fixed

The 2026-09-06 owner revision was applied to telegraphs and trails but missed
the Soul Cannon, which was still drawing `FillCircle`/`DrawCircle` rings at the
muzzle and a `DrawLine` into the chest, in the sprite pass. Those are now
painted with the feathered brush in the additive pass
(`SoulCannon.DrawChargeLight`).

### Threat cues moved under the actors

`ThreatPresentation.Draw` was the last thing drawn, over everything. A Hollow's
swipe band was therefore painted straight across the Warden standing inside it —
the frame where the Player most needs to read his own position was the frame
where he was hardest to see. Same cues, same alpha, drawn between the actor
light and the actor sprites. Verified in `hollow-swipe` and `devourer-slam`.

---

## Ludo MCP and ElevenLabs

**Ludo MCP: NOT USED.** No Ludo MCP server is registered in this environment
(`~/.config/opencode/opencode.jsonc` declares no MCP servers), so no Ludo tool
was callable. `~/.secrets/ludo-auth` was therefore never read.

That is also the right answer on the merits. The failure being corrected is
specifically what per-direction generative delivery produces: eight independently
imagined characters under eight cameras. Coherent eight-way rotation with a real
gait is a rig problem, not a prompt problem.

**ElevenLabs: NOT USED.** No audio work was in scope this session and no audio
asset was touched.

---

## Exact build and run commands

```bash
dotnet build -c Release TheLostSoulOfFire.sln

dotnet run -c Release --project src/TheLostSoulOfFire                  # solo
dotnet run -c Release --project src/TheLostSoulOfFire -- --players 2   # co-op

# regenerate the character and weapon art from the rig
python3 tools/visual-max/warden_forge.py \
  --out /tmp/warden --builds warden,warden_elder --weapons

# the three new character fixtures
dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- \
  --visual-scenario facing-sweep --capture-output artifacts/session3/after
dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- \
  --visual-scenario run-cycle    --capture-output artifacts/session3/after
dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- \
  --visual-scenario strafe-read  --capture-output artifacts/session3/after
```

Debug keys are unchanged.

## What the owner should look at first

1. `artifacts/session3/compare/facing-sweep-before-after.png` — eight frames of
   one continuous mouse sweep. Top row: eight different creatures. Bottom row:
   one person turning. This is the single most important image of the session.
2. `artifacts/session3/compare/run-cycle-before-after.png` — six frames of one
   run.
3. `artifacts/session3/compare/golden-encounter-before-after.png` — the same
   authored encounter beat.
4. `artifacts/session3/compare/coop-golden-encounter-before-after.png` — the
   brothers, now structurally distinct.
5. The game itself, moving the mouse, which is where the complaint came from.

## Capture locations

- `artifacts/session3/before/` — three character fixtures, before any change.
- `artifacts/session3/after/` — 18 solo scenarios.
- `artifacts/session3/after-coop/` — 10 two-player scenarios.
- `artifacts/session3/compare/` — 21 stacked before/after boards.

## Verification

| Check | Result |
|---|---|
| Release build, zero warnings, zero errors | PASS |
| 18 solo visual scenarios at HIGH FULL | PASS |
| 10 co-op visual scenarios, incl. reduced effects | PASS |
| Solo encounter lifecycle (4 beats → completion → restart) | PASS |
| Two-player encounter lifecycle | PASS |
| Two-player both-down failure / restart | PASS |
| Audio runtime, all 32 cues, `fallbacks=0` | PASS |
| `visual_assets.py self-test` and `inventory` | PASS (145 textures, 128 directional sheets) |
| Every capture inspected at native resolution by the agent | PASS |
| Physical controller play | **NOT_RUN — no controller hardware available** |
| Judged by the owner in motion | **NOT_RUN — this is the ask** |

## Known limitations

- **The attack is one clip for all three combo steps**, sampled by the Scythe's
  own progress. Step 1, step 2, step 3 and a Severance cut therefore share a
  pose vocabulary and differ only in timing and in the light arc. Distinct poses
  per step are the obvious next animation step, and they are cheap now that the
  rig exists.
- **No dash, hurt or death clips.** Dash uses the run frames plus the existing
  streak; the downed pose is still the idle frame rotated and darkened.
- **The exact 1:1 pixel scale only holds at combat zoom.** The title and intro
  cameras (1.10 and 1.16) and the co-op group zoom (0.92–1.30) resample. Combat
  at full zoom, which is the overwhelming majority of play, is exact.
- **The enemies were not re-authored.** They are delivered sheets in a different
  drawing language from the new protagonist. They read acceptably next to him —
  the Hollows are gaunt and pale where he is compact and warm — but the
  environment, the enemies and the Warden are not yet one hand. This is the
  largest remaining visual inconsistency and the natural next session.
- **The face is two ember eyes and a brow.** At 1:1 that is the correct amount
  of detail, but it means expression cannot yet carry a story beat.
- The generated PNGs in `Content/` are derived output; `warden_forge.py` is the
  master. Regenerating is deterministic.

## Next action

Owner judgement on the corrected baseline, in motion, with the mouse. If the
direction is approved, the ranked follow-ups are: per-step attack poses, a dash
and a collapse clip, then bringing the enemies into the same drawing language.
Do not start the prologue
([`../agent-prompts/03-DEATH-LAYER-PROLOGUE.md`](../agent-prompts/03-DEATH-LAYER-PROLOGUE.md))
until the character direction is judged.

## Important constraint

Children of Morta defines a quality target and a set of visual principles, not
content to reproduce. Every implementation must remain original and consistent
with Soulfire Gothic, Death Flame, Life Flame and the human emotional residue of
the world.
