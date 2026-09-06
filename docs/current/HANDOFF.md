# Current Handoff

## Branch

`prototype/design-polish`

## Current objective

Session 2, two ordered phases, both complete and awaiting owner review:

1. one disciplined improvement pass over the Session 1 Golden Combat Slice;
2. the local two-player brother co-op proof
   ([`../agent-prompts/02-COOP-BROTHER-PROOF.md`](../agent-prompts/02-COOP-BROTHER-PROOF.md)).

Plus an audio quality pass across both.

## State: implemented, awaiting owner review

Neither phase has been approved. The prologue has **not** been started.

---

## Phase 1 — Golden Slice review and targeted uplift

### What the Session 1 result actually looked like

Captured 12 scenarios at native resolution before touching anything
(`artifacts/session2/p1-before/`) and measured them. The three largest remaining
weaknesses, ranked by owner impact:

1. **The protagonist was the least readable actor in his own game.** The Warden
   sheet has a median luminance of 22/255; the casting floor sits at 21/255. He
   had no value separation from the ground at all. In `arena-idle` the two
   Hollows read more clearly than the Player; in `severance-cut` he was
   invisible; in `final-release` the emotional final beat had no legible
   character in it.
2. **The Devourer slam telegraph read as a neon UI ring.** An even soft ring at
   0.27 alpha; every dab overlapped and additive blending clipped it to a
   saturated violet donut that dominated the frame — the worst remaining
   violation of the 2026-09-06 "no hard lines in combat" revision.
3. **Severance blew the Warden out to a featureless white flare** exactly when
   the Player most needed to read his pose. `severance-window` p99 luminance was
   195/255 with 0.55% of the play area clipped to pure white.

### What was kept

Everything else. The Severance Window contract, the four-beat `EncounterDirector`,
the generated casting floor, the Fallen Ladle, the `SoftShapes` painted-light
grammar, every enemy identity, the Soul lifecycle and all existing art.

### What changed

- `Rendering/ActorLighting.cs` — new. Establishes the Soulfire actor light
  grammar: **living Death Flame light belongs to Wardens and Souls; manifestations
  are separated by cold ash light only.** Silhouettes are traced from the real
  sprite alpha (`ArtAssets.DrawSilhouetteLight` + a cached white-alpha copy of
  each sheet) and stamped in a ring of offsets under an additive linear pass, so
  a near-black character can be lit without a custom shader and without a drawn
  outline.
- `SoftShapes.PressureBand` — new. Replaces `Ring` for the Devourer. Per-angle
  radius wobble, varying density and soft gaps, at roughly a third of the
  previous alpha. The reach is still honest, because the slam hitbox is a circle
  in the same plane.
- `Player.DrawCombatLight` — the Severance gather moved off the body and forward.
  `SoulfireLighting` Severance glows roughly halved.
- `ArenaComposition` — Fallen Ladle residue dimmed ~33%; it was out-competing the
  fight happening east of it.

### Before / after evidence

`artifacts/session2/p1-before/` vs `artifacts/session2/p1-after/`, plus grayscale
sheets in `artifacts/session2/compare/`.

| Scenario | Change |
|---|---|
| `severance-window` | p99 luminance 195 → 108; clipped pixels 0.553% → 0.110% |
| `devourer-slam` | p99 luminance 104 → 66 (the UI ring is gone) |
| everything else | mean luminance within ±0.4 — the frame was **not** globally brightened |

That last row is the point: the fix was local figure/ground contrast, not bloom.

---

## Phase 2 — Brother local co-op proof

### Structure added

Small and concrete, per the prompt. No DI, no entity framework, no network seam.

- `Input/PlayerCommand.cs` — one Warden's intent for one frame, device-agnostic.
- `Input/PlayerInputSources.cs` — `KeyboardMouseInput`, `GamePadInput`,
  `SecondaryKeyboardInput`, `ScriptedInput`.
- `Game/WardenRoster.cs` — `PlayerSlot` (identity + `Player` + input source) and
  the one-or-two roster, including group framing helpers.
- `Entities/WardenField.cs` — the combat-side seam. Enemies were handed one
  `Player` and used it for two different questions; `Target` now answers "who am
  I coming for" and `StrikeCircle`/`StrikeContact` answer "who did that hit".
- `Game/TargetDirector.cs` — stable per-enemy targeting.
- `Game/TeamResonance.cs` — the shared pool.
- `Entities/WardenIdentity.cs` — the whole visual identity seam.

`Player.Update` now takes a `PlayerCommand` instead of `InputState`;
`ScytheCombat` and `SoulCannon` followed. Solo is the one-slot case of the same
loop, so there is no separate single-player path to drift.

### Controls

| | Player 1 — the protagonist | Player 2 — the brother |
|---|---|---|
| Join | always present | gamepad `Start`/`A`, or `F12` for the keyboard layout |
| Move | `WASD` | left stick / arrow keys |
| Aim | mouse | right stick / movement direction |
| Scythe | `LMB` | `X` / `NumPad1` |
| Soul Cannon | hold `RMB` | hold `RT` / `NumPad2` |
| Dash | `Space` | `A` / `NumPad0` |
| Soul Sense | hold `Q` | hold `LT` / `NumPad3` |
| Resonance | `R` | `Y` / `NumPad5` |
| Stabilise | hold `E` | hold `B` / `NumPad.` |

`--players 2` starts two-player directly. Player 1's controls are **unchanged**
from the approved slice.

### Brother identity

Same Warden sheet, same animations, same kit. He differs by:

- **flame temperature** — both violet-white per canon, but the elder's is colder
  and paler (`WardenIdentity.Elder`), at `LightScale` 0.74 because a cold flame
  separates from this violet-grey room at much lower intensity;
- **body tint** — cloth and iron pushed toward blue-steel;
- **silhouette mass** — 120 px display size against the protagonist's 108, so the
  two stay apart in grayscale and with effects reduced;
- **audio** — his weapon and dash are pitched ~0.08 down so two simultaneous
  swings do not phase into one.

### Camera / targeting

- Group camera targets the centroid; zoom is fitted to the pair's bounding span
  plus padding and **clamped to 0.92–1.30**. One Warden gets exactly the Session 1
  constant, so solo framing is untouched.
- Aim is unprojected through the live camera transform, so mouse aim stays correct
  while the group zoom changes.
- Separation is handled by a **shared Death Flame tether**: invisible while the
  brothers fight together, visible as it strains past 760 units, and past 980 it
  draws them back gradually. Never a teleport, never split-screen.
- Targeting: a committed enemy never re-targets, targets are held for at least
  1.15 s, a switch needs a clear distance advantage, and downed Wardens are never
  chosen.
- Wardens push each other apart at close range so melee never body-blocks.
- Area attacks hit **everyone in the area**, not only the enemy's chosen target.

### Soul / Resonance ownership

One **team pool**. Anything that earns Resonance credits it, either brother may
spend it, and spending it lights **both**. Residue drifts to the nearest standing
Warden and feeds the same pool, so there is no pickup to race for. Arithmetically
identical to Session 1 in solo.

### Down / stabilisation / failure

- A Warden who runs out of health while a brother stands **goes down**, not dead:
  15 s on the clock, cannot act, cannot be hit again, and cannot be finished off.
- The other brother holds `E`/`B` within 118 units for 2.1 s. While holding he
  **cannot attack and moves at 42%** — that is the danger window.
- Interrupted progress bleeds away rather than resetting.
- Success restores 45 HP with a bounded 1.2 s grace. Never endless.
- Each stabilisation costs 55 from the shared pool and makes the next hold 60%
  longer, so it never becomes routine.
- **Both down ends the encounter.** Solo never enters the downed state at all.

---

## Audio pass

### Owner feedback taken mid-session

The first attempt shipped raw ElevenLabs output and was rejected on listening as
"raw and not professional". That was correct and the diagnosis was structural,
not a matter of picking different sounds:

- clips were requested at 0.6–1.4 s, which returns squashed artifacts with no
  transient and no decay — the project's own `master_ludo_audio.py` workflow
  generates long and trims locally, and I had departed from it;
- prompts were long negation lists, which generative audio handles badly;
- output was 128 kbps MP3, which smears exactly the transients these cues need.

### What replaced it

`tools/audio/generate_soulfire_sfx.py` now asks for **3–4 s of headroom**, writes
**lossless PCM**, and uses short foley briefs. `tools/audio/build_session2_sfx.py`
treats every generation as **source material only** and produces each cue as:

> an already-approved sample from this bank carries the **body**, a filtered and
> gated slice of a generation adds **texture** 11–15 dB underneath, and the tail
> is an explicit decay.

Chain: resample 44.1 → 48 kHz, DC block, 55 Hz high-pass, downward expander at
−40 dBFS, onset detect and trim, hard low-pass into the bank's measured
brightness band, transient shaping, exponential decay, layer, 6 ms fades, peak
normalise by category.

### Cues added or rebuilt

`severance_window`, `severance_cut`, `soul_exposed`, `warden_down`,
`warden_stabilize` (new) and `resonance_ready` (rebuilt as a doubled pulse).

**Retained unchanged:** `hollow_swipe`, `enemy_death`, `soul_cleave`,
`scythe_hit`, `devourer_slam`, `devourer_devour`, `burning_charge`,
`burning_detonation`, ambience and music. Nothing about them was failing.

### Honest limitation

**No audio was auditioned by ear during authoring.** Candidate selection was made
on measurement — residual noise floor after gating, crest factor against the
intended envelope class, and a brightness penalty. `artifacts/session2/audio-audition/`
contains the shipped `hybrid` build and a `bank-only` build with no generated
content for direct A/B. The owner must approve on listening.

---

## Exact build and run commands

```bash
dotnet build -c Release TheLostSoulOfFire.sln

dotnet run -c Release --project src/TheLostSoulOfFire                  # solo
dotnet run -c Release --project src/TheLostSoulOfFire -- --players 2   # co-op

dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- --audio-gameplay-test
dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- --audio-gameplay-test --players 2
dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- --audio-death-restart-test
dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- --audio-death-restart-test --players 2
dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- --audio-runtime-test
dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- --audio-loop-runtime-test
python3 tools/audio/validate_audio.py

dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- \
  --visual-scenario coop-stabilize --players 2 --capture-output artifacts/session2/coop
```

Debug: `F1` overlay, `F2`–`F4` spawn, `F5` fill shared Resonance, `F6` clear
floor, `F7` force Soul Sense, `F8` reset, `F9` screenshot, `F10` reduced effects,
`F11` quality, `F12` join the brother on the keyboard layout.

## Capture locations

- `artifacts/session2/p1-before/` — Session 1 result, 12 scenarios.
- `artifacts/session2/p1-after/` — after the Phase 1 uplift, same 12.
- `artifacts/session2/coop/` — 11 two-player fixtures.
- `artifacts/session2/compare/` — grayscale before/after and co-op readability.
- `artifacts/session2/audio-audition/` — `hybrid` and `bank-only` builds.

New scenarios: `coop-idle`, `coop-split-targets`, `coop-severance`, `coop-down`,
`coop-stabilize`, `coop-separation`, `coop-soul-release`, `coop-resonance`,
`coop-golden-encounter`, `coop-reduced`.

## Verification

| Check | Result |
|---|---|
| Release build, zero warnings | PASS |
| Solo encounter lifecycle (4 beats → completion → restart) | PASS |
| Two-player encounter lifecycle | PASS |
| Solo death / restart | PASS |
| Two-player both-down failure / restart | PASS |
| Audio runtime, all 32 cues, `fallbacks=0` | PASS |
| Audio loop runtime (music + ambience boundaries) | PASS |
| `validate_audio.py`, 34 assets | PASS |
| 12 solo scenarios at HIGH FULL | PASS |
| 11 co-op scenarios, incl. reduced effects and baseline quality | PASS |
| Grayscale readability inspection | PASS with reservation (see below) |
| Physical controller play | **NOT_RUN — no controller hardware available** |
| Audio auditioned by ear | **NOT_RUN — cannot listen; owner must approve** |

## Known issues

- **Grayscale separation between the two brothers is the weakest result.** They
  are clearly separable from the environment, but from each other only by
  silhouette mass and rim intensity. Recommended next art step: one bespoke
  silhouette accent for the elder (mantle or hood), which would make the
  distinction structural rather than tonal.
- The downed pose is the standing idle frame rotated and darkened. It reads
  correctly at gameplay scale but wants a real collapse animation. Documented as
  temporary art.
- The delivered west-facing Warden sheet has a much weaker silhouette than the
  east-facing one — the wings spread horizontally and the body reads as a mass.
  Pre-existing, not a co-op regression, but the new rim light makes it more
  visible.
- No controller was available, so gamepad mapping is verified by code path and
  the deterministic scripted-input fixtures only.
- `art/audio_candidates/` is generated authoring material and is gitignored,
  matching how the Ludo inputs were handled.

## Next action

Owner review of both phases and of the audio audition. Do not start the prologue
([`../agent-prompts/03-DEATH-LAYER-PROLOGUE.md`](../agent-prompts/03-DEATH-LAYER-PROLOGUE.md))
until the co-op direction and the audio are judged.

## Important constraint

Children of Morta defines a quality target and a set of visual principles, not
content to reproduce. Every implementation must remain original and consistent
with Soulfire Gothic, Death Flame, Life Flame and the human emotional residue of
the world.
