# Final Audio Source and Production Ledger

Authored on **2026-08-30** for *The Lost Soul of Fire*.

## Source, model, and security

- **Generation service:** ElevenLabs Sound Effects API, used only as an authoring tool.
- **Model:** `eleven_text_to_sound_v2`.
- **Source format:** `pcm_48000`; ElevenLabs stereo PCM was downmixed for one-shots.
- **Authentication:** `ELEVENLABS_API_KEY` was read from the process environment only. No key, `.env`, response metadata, or credential is stored in Content or source control.
- **Release dependency:** none. The game loads committed local WAV files and never contacts ElevenLabs at runtime.
- **Terms:** use of generated output remains governed by the ElevenLabs account plan and terms attached to the user-provided API key. No third-party recordings, voices, music, or recognizable copyrighted material were used.
- **Raw sources:** one retained source take per shipped cue was kept only in `/tmp/soulfire-elevenlabs-sources` during authoring. Raw takes and rejected output are not shipped.
- **Reproducibility:** exact prompts, durations, prompt influence, priorities, loop settings, and mastering targets are in `tools/audio/generate_elevenlabs_audio.py`.

## Audit classification

| Priority | Runtime cues |
|---|---|
| MUST HAVE | `scythe_swing_1`, `scythe_swing_2`, `soul_cleave`, `scythe_hit`, `dash`, `cannon_charge`, `cannon_full`, `cannon_fire`, `cannon_impact`, `burning_charge`, `burning_detonation`, `core_hit`, `soul_release`, `resonance_ready`, `resonance_activate`, `player_hit`, `player_death`, `soul_sense_on`, `soul_sense_off`, `wave_start`, `arena_ambience` |
| SHOULD HAVE | `hollow_swipe`, `devourer_slam`, `devourer_devour`, `enemy_death`, `wave_clear`, `ending_reveal` |
| OPTIONAL | `title_confirm` |

Every cue above is routed by the current game. MUST cues were authored first; SHOULD and OPTIONAL cues were then authored individually to avoid mixing the previous Ludo bank with the final ElevenLabs sound language.

## Final asset ledger

All one-shots are mono 48 kHz/16-bit PCM WAV. The ambience is stereo 48 kHz/16-bit PCM WAV and sample-continuous at the loop boundary. Peak targets deliberately vary by dramatic role rather than normalizing every cue equally.

| Final filename | Intent | Duration | Peak target |
|---|---|---:|---:|
| `Audio/Sfx/scythe_swing_1.wav` | Thin, dry first air cut | 0.18 s | -5.0 dBFS |
| `Audio/Sfx/scythe_swing_2.wav` | Wider metal/spectral second cut | 0.24 s | -4.2 dBFS |
| `Audio/Sfx/soul_cleave.wav` | Violent broad cut and soul crack | 0.45 s | -2.6 dBFS |
| `Audio/Sfx/scythe_hit.wav` | Compact ash/bone/iron contact | 0.15 s | -4.0 dBFS |
| `Audio/Sfx/dash.wav` | Death-Flame ignition and air displacement | 0.25 s | -4.0 dBFS |
| `Audio/Sfx/cannon_charge.wav` | Directed occult-mechanical pressure rise | 0.80 s | -6.0 dBFS |
| `Audio/Sfx/cannon_full.wav` | Latch, core pulse, spectral readiness | 0.22 s | -3.5 dBFS |
| `Audio/Sfx/cannon_fire.wav` | Violent compressed directed release | 0.55 s | -1.8 dBFS |
| `Audio/Sfx/cannon_impact.wav` | Projectile pressure and spectral debris | 0.32 s | -3.0 dBFS |
| `Audio/Sfx/burning_charge.wav` | Erratic contained flame pressure | 0.55 s | -5.0 dBFS |
| `Audio/Sfx/burning_detonation.wav` | Unstable inward snap and jagged rupture | 0.70 s | -2.0 dBFS |
| `Audio/Sfx/core_hit.wav` | Bright white-violet crystalline weak-point crack | 0.18 s | -3.0 dBFS |
| `Audio/Sfx/soul_release.wav` | Melancholy weight leaving and gentle resolution | 0.90 s | -7.0 dBFS |
| `Audio/Sfx/resonance_ready.wav` | Single Soul Core heartbeat | 0.35 s | -5.0 dBFS |
| `Audio/Sfx/resonance_activate.wav` | Vacuum, pressure, supernatural ignition | 0.80 s | -1.5 dBFS |
| `Audio/Sfx/player_hit.wav` | Short body/armor/ember damage read | 0.20 s | -4.5 dBFS |
| `Audio/Sfx/player_death.wav` | Life Flame collapse and ash fall | 1.20 s | -4.0 dBFS |
| `Audio/Sfx/soul_sense_on.wav` | World withdrawal and spectral opening | 0.45 s | -7.0 dBFS |
| `Audio/Sfx/soul_sense_off.wav` | Small return-to-world fold | 0.30 s | -8.0 dBFS |
| `Audio/Sfx/wave_start.wav` | Furnace gate and distant ritual metal | 0.65 s | -6.0 dBFS |
| `Audio/Sfx/hollow_swipe.wav` | Bone/cloth enemy swipe, lighter than Scythe | 0.32 s | -5.5 dBFS |
| `Audio/Sfx/devourer_slam.wav` | Stone/foundation weight and grit | 0.65 s | -2.4 dBFS |
| `Audio/Sfx/devourer_devour.wav` | Inward Soul suction through bone and ash | 0.85 s | -4.0 dBFS |
| `Audio/Sfx/enemy_death.wav` | Subordinate ash fracture and ember withdrawal | 0.55 s | -6.0 dBFS |
| `Audio/Sfx/wave_clear.wav` | Ash settling and unresolved relief | 0.75 s | -8.0 dBFS |
| `Audio/Sfx/ending_reveal.wav` | Intimate living-Flame hopeful bloom | 1.60 s | -7.0 dBFS |
| `Audio/Sfx/title_confirm.wav` | Tiny ember and dark-glass confirmation | 0.32 s | -9.0 dBFS |
| `Audio/Ambience/arena_ambience.wav` | Sparse furnace ruin, chains, air, distant Soul presence | 20.0 s loop | -14.0 dBFS source target |

## Processing

- Each cue was generated separately; no combined sound sequence was generated or sliced into unrelated events.
- Short API source takes were transient-trimmed, DC-corrected, faded, and cut to the actual gameplay window.
- Harmonic transitions retain their leading breath and were not transient-trimmed.
- Combat sources are downmixed to mono so overlapping voices remain focused and predictable in MonoGame.
- The ambience uses a restrained stereo decorrelation and a 60 ms loop-edge reconciliation; first and last samples are identical.
- No music was generated or retained. The previous `Audio/Music/arena_loop.ogg` asset and runtime playback were removed for the SFX + ambience-only freeze.

Run `python3 -B tools/audio/validate_audio.py` to verify formats, duration, headroom, loop seam, cue routing, duplicate registrations, source coverage, and the no-music constraint.
