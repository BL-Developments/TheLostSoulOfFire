# Current Handoff

## Branch

`prototype/design-polish`

## Completed objective — Phase 0 gate and first playable prologue

Normal launch is now a complete authored Death-Layer first playable. The refined
Golden Slice remains runnable with `--golden-slice`.

### Phase 0 result

- Fresh before and final native captures: `artifacts/phase0/baseline/` and
  `artifacts/phase0/final/`.
- The current Session 3–5 reset, not the superseded Session 2 sheets, resolves
  the fundamental gate: human-first design, one eight-direction rig, bounded
  mouse-facing with hysteresis, distance-driven gait, per-step attack poses and
  one coherent Warden/scythe construction.
- Final captures re-verified the 360° facing sweep, six-frame run sample,
  backpedal read, three-hit chain, dash cancel, solo Golden encounter and co-op
  Golden encounter. No prologue code regressed them.
- Remaining character gaps are polish: no bespoke hurt/collapse clips and an
  enemy/environment drawing hand that remains less pixel-disciplined than the
  Warden.
- Ludo MCP was actively tested with a bounded two-candidate human-Warden brief.
  The configured credential was rejected, so no candidate was generated or
  promoted. The current authored Warden remained the stronger available result.

### Implemented first playable

- `Game/PrologueDirector.cs`: concrete sector/stage state, objectives, story
  lines, movement bounds and authored coordinates.
- `Rendering/PrologueEnvironment.cs`: Emergence memory platform, Searchway,
  Warden signs, extraction causeway, moving Death-Flame skiff and homebase
  exterior/threshold, all using broad clustered pixel value groups.
- `Rendering/ProloguePresentation.cs`: restrained in-world story captions,
  objectives, title, failure and completion presentation.
- `GameWorld`: actual route gates, encounter composition, held-Soul Devourer,
  Soul Release wait, brother meeting, solo combat/rescue companion, two-player
  control handoff, sector retry, full-party failure, 62-second transit defense,
  threshold crossing and replay.
- Normal start enters the prologue; `--golden-slice` retains the benchmark.
- D1 Emergence, D2 Search, D3 Escape and D4 Transit provide review access; F8
  restarts the current authored checkpoint.

### Audio / soundscape

- `tools/audio/build_prologue_ambience.py` derives three 19.5-second stereo loops
  from approved AMB-01 only. No new AI/generated audio was used.
- Emergence is narrow and sparse; Search adds lateral environmental movement;
  Escape/Transit adds moving air and low vehicle body; the threshold returns to
  deliberate quiet.
- The approved `arena_loop.ogg` is used provisionally at very low level during
  Emergence/threshold and raised only for danger/transit. It remains subordinate
  to combat cues.
- The runtime soundscape switch, all 32 cues and music load with `fallbacks=0`.

### Visual quality loop and evidence

- Pass 1: `artifacts/prologue/pass1/`.
- Native review found repeating sector titles, objective/HUD collision, weak
  floor material rhythm, a blocky skiff and an over-large threshold door.
- Pass 2 correction: persistent sector timing, separated UI, large hand-set slab
  clusters, grounded rubble, coherent skiff ribs/rail/prow, and a human-scale
  door nested inside the monumental gate.
- Final captures: `artifacts/prologue/final/` for title, Emergence, trace, Search,
  brother, held-Soul pressure, transit, threshold, completion and sector retry.
- Semantic full route: `artifacts/prologue/end-to-end/prologue-route.*`; reached
  `Complete` at tick 6590 with `runTime=107.8s`, zero enemies and zero Souls. This
  force-cleared run proves sequence plumbing and is not a human duration claim.
- Semantic two-player route:
  `artifacts/prologue/end-to-end-coop/prologue-route.*`; reached `Complete` at
  tick 6479 with `runTime=105.9s`, and records the second-keyboard control source.

### Verification

| Check | Result |
|---|---|
| Release build, zero warnings/errors | PASS |
| `git diff --check` | PASS |
| `python3 tools/audio/validate_audio.py` (37 assets) | PASS |
| Audio runtime, every soundscape/cue, zero fallbacks | PASS |
| Semantic solo + two-player routes to real completion | PASS |
| Ten final prologue native capture fixtures | PASS |
| Fatal damage → Search checkpoint restart at full health | PASS |
| Phase 0 motion + solo/co-op Golden regression captures | PASS |
| Native-resolution agent inspection | PASS |
| Physical controller test | NOT_RUN — no controller hardware |
| Stopwatch human 8–12 minute playthrough | NOT_RUN — owner review gate |
| Owner reference-system audio audition | NOT_RUN — owner review gate |

### Known defects / limitations

- Sector transitions are authored cuts with brief light/audio punctuation, not
  streaming traversal.
- The solo brother is deliberately bounded follow/attack/dodge/rescue logic, not
  a tactical companion system.
- Transit uses the Wardens' normal Soul Cannons beside mounted-cannon housings;
  there is no unrelated turret minigame.
- No hurt or bespoke collapse clip; the existing rotated/darkened down pose
  remains temporary.
- No controller hardware was available. Two-player keyboard mapping and the
  pre-existing deterministic co-op command path were regression-captured.
- Ludo access remains blocked by the configured credential.

### Exact next action

Owner plays from normal launch in solo and, if possible, `--players 2`, checks
the 8–12 minute pace and listens to sector transitions. Revise this route before
starting items, Content Factory, a hub interior or broader progression.

## Current objective

Session 5 responds to owner review of Session 4. The camera, the whole-body
motion, the weapon shapes and the VFX were accepted. Two things were named.

### Session 5 — what changed

**The scythe is held.** The real fault was structural: the hands and the weapon
were posed independently, so the carry could never flow into a swing because
they were never connected. A pose now describes **where the scythe is** — two
body-space endpoints — and the hands are placed *on the haft*. Everything else
follows from that.

The guard went through three readings before it was right. Planted upright like
a staff was wrong. Hanging down at the floor like a farm tool was wrong. It now
sits **level across the body at chest height, blade out ahead of the leading
shoulder** — a combat guard, seen side-on. It sits at about −56° from the aim,
which is exactly where the first hit of the chain winds up from, so the hold *is*
the start of the swing.

**The chain**, three hits, choreographed as asked:

| hit | arc | body |
|---|---|---|
| 1 | 120° left to right | winds up on the left, drives across |
| 2 | 140° right back to left | answers from where hit 1 finished |
| 3 | 198° | the whole body turns through a full revolution and arrives back where it started |

Each clip places the hands on the overlay's haft using **the runtime's own swing
angle**, copied from `ScytheCombat.BuildSwingArc`, so the Warden is always
holding the weapon being drawn for him. Each then eases its grip back onto the
carry over the last 30%, so the chain resolves into the hold instead of cutting
to it. A Severance cut borrows the third hit's turning body.

The overlay is now **lifted to chest height** (`ScytheCombat.GripRise`). This is
the one place the flat combat plane and the three-quarter character view have to
agree, and the constant is shared by the runtime and the forge. Without the lift
the weapon was drawn on the floor while the hands were at chest height, and it
never looked held.

**The dash interrupts anything.** `ScytheCombat.CancelForDash`: a strike already
created still lands — the blade was through the enemy before he moved — and
everything after it is abandoned. The chain position is kept, so dashing out of
a swing and swinging again continues the combo rather than restarting it; the
dash becomes part of the rhythm instead of a punishment for using it.

**The dash is visibly invulnerable.** `Player.PhaseAmount` rises almost instantly
and falls with the i-frame window, darkening the body toward its own flame and
thinning it. What the Player sees is exactly how long he cannot be hit — never a
flourish that outlives the rule it describes. He stays a readable pose the whole
way through; the point is to show the frames, not to remove the character from
his own dodge.

The Soul Cannon was left alone, as instructed.

### Session 4 — the previous job The character was accepted; the
camera, the animation's whole-body motion, the weapons' shape language and every
VFX were rejected as still below a professional bar. Enemy behaviour was
explicitly out of scope — only how it looks.

### Session 4 — what changed

**Camera** (`Rendering/Camera2D.cs`, rebuilt). The old camera was one lerp
straight onto the Warden at a fixed rate, which is why it felt mechanical: it sat
exactly on him, answered every one-pixel adjustment, never looked where he was
going and turned as instantly as he did. It now has the three ingredients of a
good action follow and nothing more — look-ahead from velocity and facing with
its own slow smoothing, a soft zone so small adjustments leave the frame still,
and critical damping so it eases instead of tracking. Vertical response is held
to 0.62 of horizontal. A small bounded bias pulls the frame toward the fight.
Impacts add a spring-released zoom punch.

Evidence: in `run-cycle` the Warden sits clearly left of frame centre while
running east. The frame leads him.

**Whole-body motion.** The owner's note was that the feet moved under a static
torso. The rig now carries pelvis sway onto the stance leg, shoulder
counter-rotation against the hips that overshoots at head height so the head
arrives late, arms crossing inboard as they come forward, and a lean that
breathes with the footfalls. Planted feet use a projection that skips all of it,
so nothing slides. The attack turns from the hips, which is most of the
difference between a swing and an arm being waved.

**Weapons.** Both were re-shaped, not re-detailed — the detail level is
unchanged, the weight comes from silhouette. The scythe gained a deep recurved
blade with a back-spur and a hooked point, an iron collar holding one bound Soul,
a counterweight spike at the butt and a shallow S in the haft. The blade plane is
yawed 45° off the forward axis so it never foreshortens to nothing in the north
and south sheets. The Soul Cannon became a braced reliquary: shoulder hook,
under-brace, a caged chamber where the Soul burns, and a flared fluted mouth.

**VFX** (`tools/visual-max/vfx_forge.py`, new; all twelve sheets replaced). The
delivered sheets were images rather than effects, and the measurements say so:
`fx_burning_detonation` covered 79–81% of its frame for three frames and then
spent nine frames as full-frame speckle. Drawn additively, that is the literal
square the owner reported. The authored sheets are built from quantised energy
fields — banded pixel light, since these are drawn PointClamp — with particles
simulated once and sampled per frame so embers actually travel and go out,
shockwaves that thin as they expand, and rings that are deliberately broken and
uneven rather than perfect circles. Every effect has an attack, a body and a
decay that reaches zero, and the tool asserts a coverage ceiling so nothing can
become a square again.

| Sheet | Peak frame coverage before | after |
|---|---|---|
| `fx_burning_detonation` | 0.81 | 0.11 |
| `fx_resonance_activate` | 0.72 | 0.07 |
| `fx_soul_release` | 0.07 (a grey blob) | 0.06 (a Soul unwinding upward) |

Two supporting fixes were needed before the authored effects could be seen at
all. Each sprite VFX lays a soft glow disc over itself, sized for sheets with no
internal light; at the old intensities that disc simply covered the new ones, so
a detonation with a shockwave, torn fragments and travelling embers arrived as a
white ball. Every glow is now wider and much weaker. Separately, each particle
contributed a disc 3.4× its own size, and two dozen emitted at one point stacked
into the same white ball; that is now 2.3× at 0.10, and the detonation throws its
residue clear instead of leaving it in a pile.

### Session 3 — the previous job: **correct the character, animation and
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

**Ludo MCP: NOT AVAILABLE, therefore NOT USED.** No MCP server is registered in
this environment — `~/.config/opencode/opencode.jsonc` contains only a schema
reference and there is no project-level `.opencode` config — so no Ludo tool is
callable from here. `~/.secrets/ludo-auth` was never read.

Reporting it as requested, with an honest assessment of whether it was needed:
**it was not, for any of this session's work.** Every item the owner raised is a
motion, timing or shape-language problem — camera damping, gait mechanics,
silhouette, effect lifecycles — and none of them are solved by generating more
images. The one place external concept generation would genuinely help is the
*next* problem, which is bringing the enemies and the environment into the
authored drawing language; that is a design-exploration job where bounded
candidate generation is worth having. If the owner wants Ludo used there, an MCP
server has to be registered first.

**ElevenLabs: AVAILABLE but NOT USED.** The key is present at
`/tmp/soulfire-elevenlabs.key` and was not read. This session's brief was camera,
animation, weapons and VFX; the owner's message ended "the rest is good", and the
audio bank is already under an owner revision that requires local production from
approved samples. Generating audio here would have been scope the owner did not
ask for.

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

## What the owner should look at first (Session 5)

1. `artifacts/session5/compare/combo-chain.png` — hold, right sweep, left sweep,
   spin, hold. Ten frames of one continuous chain.
2. `artifacts/session5/compare/dash-cancel.png` — the dash cutting out of the
   middle of the third hit, and the Warden going shadowy for the i-frames.
3. `artifacts/session5/compare/facing-sweep-session4-session5.png` — the guard in
   all eight directions.
4. The game, swinging and dashing.

## What the owner should look at first (Session 4)

1. `artifacts/session4/compare/burning-detonation-before-after.png` — the square,
   and its absence.
2. The game itself, **moving**, which is the only way to judge a camera.
3. `artifacts/session4/compare/run-cycle-session3-session4.png` — the Warden now
   sits left of frame centre while running east, and the body moves as one.
4. `artifacts/session4/compare/severance-cut-session3-session4.png` and
   `scythe-combo-…` — the weapon.
5. `artifacts/session4/compare/golden-encounter-session3-session4.png`.

## What the owner should look at first (Session 3)

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

- `artifacts/session5/after/` — 21 solo scenarios.
- `artifacts/session5/after-coop/` — 10 two-player scenarios.
- `artifacts/session5/compare/` — 30 boards.
- `artifacts/session4/before/` — the detonation, with the old effect sheets.
- `artifacts/session4/after/` — 19 solo scenarios.
- `artifacts/session4/after-coop/` — 10 two-player scenarios.
- `artifacts/session4/compare/` — 29 stacked boards.
- `artifacts/session3/before/` — three character fixtures, before any change.
- `artifacts/session3/after/` — 18 solo scenarios.
- `artifacts/session3/after-coop/` — 10 two-player scenarios.
- `artifacts/session3/compare/` — 21 stacked before/after boards.

## Verification

| Check | Result |
|---|---|
| Release build, zero warnings, zero errors | PASS |
| 21 solo visual scenarios at HIGH FULL | PASS |
| No authored character frame clips its 128px cell (768 frames checked) | PASS |
| Every effect sheet under the 0.34 coverage ceiling | PASS |
| Audio loop runtime (music + ambience boundaries) | PASS |
| 18 solo visual scenarios at HIGH FULL (Session 3) | PASS |
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

- **The particle system was tuned, not re-authored.** Particles are still filled
  circles drawn by `ShapeRenderer` with a glow disc each. They now read as sparks
  rather than fog, but they are the last part of the effects pipeline that is not
  authored pixel art.
- **The camera has no manual look control and no arena-specific framing.** It is
  one model for the whole game; large regions will want more.
- **Ludo could not be reached**, so no external concept exploration supported the
  weapon shapes. They are authored judgement calls.
- **A Severance cut reuses the third hit's clip.** It reaches much further than
  a normal third hit, so the body is right but the reach is carried entirely by
  the overlay and the light.
- **No dash, hurt or death clips.** Dash uses the run frames plus the phase-out
  and the existing streak; the downed pose is still the idle frame rotated and
  darkened.
- **Dash-cancelling removes commitment from the third hit.** That is what was
  asked for, but it is a real combat change and wants play-testing: the heavy
  swing no longer costs anything to throw out.
- **On the diagonals the blade's reach is bounded by the 128px cell**, because
  forward and lateral offsets both project onto screen x there. The weapon is
  sized to fit rather than to a free artistic choice.
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

Owner judgement, in motion. The camera and the whole-body animation cannot be
judged from stills at all.

If approved, the ranked follow-ups are: bring the enemies and the environment
into the authored drawing language (the largest remaining inconsistency, and the
one place bounded external concept generation would genuinely help — register a
Ludo MCP server first); per-step attack poses; dash and collapse clips; then
author the particle system.
Do not start the prologue
([`../agent-prompts/03-DEATH-LAYER-PROLOGUE.md`](../agent-prompts/03-DEATH-LAYER-PROLOGUE.md))
until the character direction is judged.

## Important constraint

Children of Morta defines a quality target and a set of visual principles, not
content to reproduce. Every implementation must remain original and consistent
with Soulfire Gothic, Death Flame, Life Flame and the human emotional residue of
the world.
