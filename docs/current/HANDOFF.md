# Current Handoff

## Branch

`work/golden-combat`

## Current objective

Deliver the Golden Combat Slice: one authored 60–90 second encounter in the
Abandoned Soul Furnace that establishes the combat and visual grammar the rest of
the game inherits. See [`../agent-prompts/01-GOLDEN-COMBAT-SLICE.md`](../agent-prompts/01-GOLDEN-COMBAT-SLICE.md).

## State: implemented, awaiting owner review

The slice is playable end to end. It has not been approved.

## Completed work

### 1. Arena visual foundation

- `Rendering/ArenaFloorSurface.cs` — rewritten. Generates the casting floor as a
  deterministic CPU texture (cast plates, pour trough, Warden seal, rivets,
  chips, scorch, residue). Masked to the painted asset's dark basin by sampling
  the asset itself, so the perimeter machinery is never painted over.
- `Rendering/ArenaComposition.cs` — new. Recessed basin, the Fallen Ladle
  landmark, ingot stacks, tipped cart, pillar drums, soul lamps, floor light
  pools, foreground gantry, soft actor contact shadows.
- Camera framed closer (`GameBalance.CombatCameraZoom = 1.3`), actor display
  sizes raised for scale hierarchy.

### 2. Severance Window (reaction thesis)

- `Enemy.CommitmentRemaining` / `AnchorPosition` / `CommitmentThreatRange` /
  `ApplySeverance()` — one honest contract, each family answers differently.
- Dash into a committed attack inside `SeveranceReadTime` opens a window on the
  Player; the next Scythe swing becomes a Severance cut that targets the Anchor
  without Soul Sense.
- Hollow: swipe cancelled, staggered. Burning: detonates if charging, else broken
  open. Devourer: slam cancelled, staggered, **one held Soul expelled**.
- All values in `GameBalance` under the Severance Window block.

### 3. Authored encounter

- `Game/EncounterDirector.cs` — new. Four named beats with staged arrivals,
  replacing four anonymous waves. Pending arrivals are pulled forward when the
  floor is clear, so fast play is not punished and the lifecycle check stays
  deterministic.
- Beat III seeds the Devourer with held Souls via `Devourer.SeedHeldSoul`, which
  is what makes "destroying a manifestation ≠ releasing a Soul" playable.

### 4. Owner revision: no hard lines in combat

Owner instruction during the session: remove hard shiny vector lines from
gameplay; combat should be animation and VFX only.

- `Rendering/SoftShapes.cs` — new. Feathered brush plus Blob/Streak/ArcBand/
  Ring/Lane/Pool helpers, with arc-length-aware dab density and a flat-plateau
  brush profile so painted bands do not bead under additive blending.
- `ThreatPresentation` rewritten: telegraphs are gathering light, not outlines.
- Removed procedural strokes from Player, Scythe trail, Soul, Hollow core,
  Burning fractures/charge/detonation, Devourer tether/cavity/death, the arena
  centre pulse ring and the aim crosshair.
- New additive combat-light pass: `GameWorld.DrawCombatLight`, drawn with
  `SamplerState.LinearClamp` so soft light is never point-sampled into hard edges
  by the pixel-art sampler.
- Debug overlays (F1) intentionally still use primitives.

## Assumptions recorded

- Beat titles/lines are authored flavour consistent with canon (tragic humans);
  they are not established region canon.
- A Soul held by a Devourer is owned by `GameWorld._souls` and referenced by the
  Devourer, so it keeps updating and drawing while consumed.
- `WaveNumber` is retained as the beat index so existing tooling keeps working.

## Exact build and run commands

```bash
dotnet build -c Release TheLostSoulOfFire.sln
dotnet run  -c Release --project src/TheLostSoulOfFire            # play
dotnet run  -c Release --no-build --project src/TheLostSoulOfFire -- --audio-gameplay-test
dotnet run  -c Release --no-build --project src/TheLostSoulOfFire -- --audio-death-restart-test
dotnet run  -c Release --no-build --project src/TheLostSoulOfFire -- \
  --visual-scenario golden-encounter --visual-quality high --capture-output artifacts/after
```

Controls: `WASD` move, mouse aim, `LMB` Scythe, hold `RMB` Soul Cannon,
`Space` dash, hold `Q` Soul Sense, `R` Resonance / restart.
Debug: `F1` overlay, `F2`–`F4` spawn, `F5` fill Resonance, `F6` clear floor,
`F7` force Soul Sense, `F8` reset, `F9` screenshot, `F10` reduced effects,
`F11` quality.

## Capture locations

- `artifacts/baseline/` — pre-change evidence (12 scenarios).
- `artifacts/after/` — post-change evidence, plus `reduced/` and `baseline/`.
- New scenarios: `golden-encounter`, `severance-window`, `severance-cut`,
  `final-release`. The Severance fixtures react to real combat state via
  `GameWorld.SeveranceOpportunityReady`, not hard-coded ticks.

## Verification

| Check | Result |
|---|---|
| Release build, zero warnings | PASS |
| Encounter lifecycle (4 beats → completion → restart) | PASS |
| Death / restart | PASS |
| 13 visual scenarios at HIGH FULL | PASS |
| Reduced effects readable | PASS |
| Baseline quality readable | PASS |
| Play-area black pixels 90–96% → <2.5% | PASS |

## Known issues

- Beats III/IV can crowd the frame when the Player lets arrivals stack.
- The Fallen Ladle sits west of centre; it leaves frame during east-side fights.
- Hollow/Burning death dissolves still use primitive silhouette shapes (dark, not
  shiny) and were left alone.
- Severance is tuned against the Devourer's 0.88s telegraph; the Hollow's 0.42s
  window is deliberately much harder.
- No bespoke Severance sound yet; it layers existing `soul_cleave` + `core_hit`.
  ElevenLabs was offered but the supplied credential was an API key **ID**, not a
  usable `sk_` key, so the API rejected every request.

## Next action

Owner review of the Golden Slice. If approved, continue with
[`../agent-prompts/02-COOP-BROTHER-PROOF.md`](../agent-prompts/02-COOP-BROTHER-PROOF.md).
The Severance Window was written per-Player specifically so a second Warden can
hold an independent window against the same telegraph.

## Important constraint

Children of Morta defines a quality target and a set of visual principles, not
content to reproduce. Every implementation must remain original and consistent
with Soulfire Gothic, Death Flame, Life Flame and the human emotional residue of
the world.
