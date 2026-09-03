# The Lost Soul of Fire — Final Audio Package

**Status:** the approved pre-ElevenLabs authored bank was restored on 2026-08-30 after the replacement pass failed subjective quality review.

## Shipped scope

The package contains **sound effects, arena ambience, and a restrained arena music loop**. It contains no adaptive score, menu music, or Techno Mode. Silence and dynamic headroom remain part of the presentation.

The released game has no authoring-service dependency. It loads local Content Pipeline assets through `AudioDirector`, which retains synthetic emergency fallbacks if an authored asset is absent or unloadable.

## Audio audit

The current runtime expects 27 named `AudioCue` one-shots, one arena ambience loop, and one arena music loop.

### MUST HAVE

- Scythe: `scythe_swing_1`, `scythe_swing_2`, `soul_cleave`, `scythe_hit`
- Mobility: `dash`
- Soul Cannon: `cannon_charge`, `cannon_full`, `cannon_fire`, `cannon_impact`
- Burning: `burning_charge`, `burning_detonation`
- Soul feedback: `core_hit`, `soul_release`
- Resonance: `resonance_ready`, `resonance_activate`
- Player: `player_hit`, `player_death`
- Perception: `soul_sense_on`, `soul_sense_off`
- Encounter: `wave_start`
- World: `arena_ambience`, `arena_loop`

### SHOULD HAVE

- `hollow_swipe`
- `devourer_slam`
- `devourer_devour`
- `enemy_death`
- `wave_clear`
- `ending_reveal`

### OPTIONAL

- `title_confirm`

Every audited cue is currently routed on a real state transition, so all 27 one-shots were authored. MUST assets were completed first; the remaining cues were authored afterward to preserve one sound language across the shipped bank.

## Signature language

- **Physical / Scythe:** dry curved steel, cloth and displaced air, sharp transient, limited sub energy.
- **Soul energy:** glassy ghost harmonics, breath-like spectral motion, electrical tension without laser styling.
- **Industrial world:** weathered furnace metal, stone-room weight, sparse chains and distant foundation resonance.
- **Anime combat impact:** readable onset and short controlled tail, with power expressed through density and layering—not indiscriminate loudness.
- **Silence:** no constant Soul Sense tone, no wall-to-wall enemy vocalization, and a very low ambience bed.

## Authored hierarchy

1. Resonance activation and full Cannon discharge
2. Soul Cleave, Burning rupture, Devourer slam, immediate combat impacts
3. Player damage and enemy-state feedback
4. Soul Sense, Soul Release, completion transitions
5. Arena ambience
6. Restrained arena music bed

Normal Scythe swings are intentionally leaner than Soul Cleave. Cannon is directed and occult-mechanical; Burning is erratic containment failure. Soul Release and death avoid explosion language. Soul Sense is a brief perceptual transition only.

## Runtime integration

- `AudioDirector` remains the sole playback and mix owner.
- Cue cooldown, polyphony, enemy-voice cap, subtle pitch variation, and fallback behavior remain intact.
- Soul Sense suppresses ambience and music without adding a continuous tone.
- Resonance, Soul Release, wave clear, and ending reveal duck ambience and music briefly.
- Ambience runs at 0.12 gameplay gain and 0.035 calm gain before event ducking.
- Music runs at 0.50 gameplay gain and 0.26 calm gain before perception and event ducking, keeping it audible beneath the denser restored effects.
- The complete organic Ludo bank—27 one-shots, ambience, and arena music—and its `MediaPlayer` path were restored byte-for-byte from commit `6f61fc4`.

## Quality decision

The ElevenLabs replacement pass is not shipped. Its production script accepted one take per cue without an audition/selection stage, generated overly short source clips, used whole-clip reversal to manufacture several transitions, peak-normalized every result regardless of source quality, and collapsed ambience to mono before artificial stereo decorrelation. Those choices produced thin, generic effects that did not clear the approved bank's quality bar.

Future generated replacements must be auditioned as multiple full-length candidates, selected in context against the current cue, and layered or edited where one generated take cannot provide the required transient, body, and supernatural tail. A replacement is accepted only when it is clearly stronger in gameplay; otherwise the approved Ludo asset remains authoritative.

## Asset contract

- One-shots: mono 48 kHz, 16-bit PCM WAV.
- Ambience: stereo 48 kHz, 16-bit PCM WAV, 20-second seamless loop.
- Music: stereo 48 kHz Ogg Vorbis, 100-second seamless loop.
- All peaks remain below -1 dBFS.
- Cue durations match the real gameplay windows; no large silence pads are shipped.
- Exact source/model/prompt/mastering records live in `Content/Audio/SOURCES.md` and `tools/audio/master_ludo_audio.py`.

## Validation

Run:

```sh
python3 -B tools/audio/validate_audio.py
dotnet build TheLostSoulOfFire.sln
dotnet run --no-build --project src/TheLostSoulOfFire/TheLostSoulOfFire.csproj -- --audio-runtime-test
dotnet run --no-build --project src/TheLostSoulOfFire/TheLostSoulOfFire.csproj -- --audio-gameplay-test
dotnet run --no-build --project src/TheLostSoulOfFire/TheLostSoulOfFire.csproj -- --audio-death-restart-test
```

The long-loop runtime mode (`--audio-loop-runtime-test`) runs for 102 seconds, crossing the music boundary and multiple 20-second ambience boundaries.
