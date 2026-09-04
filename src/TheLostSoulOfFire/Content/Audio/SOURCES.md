# Audio Sources and Production Ledger

Authored and downloaded on **2026-08-29** for *The Lost Soul of Fire*.

## Release and license status

- **Generation service:** [Ludo.ai game audio tools](https://ludo.ai/tools/game-sound-effects-generator), used only during authoring. The released game loads the committed files below and has no network, API-key, or Ludo dependency.
- **Creator / model:** Ludo.ai Sound Effects, Ambiance, and Music generators. The API does not expose a model name/version, so this ledger records **Ludo audio model (undisclosed)** rather than guessing.
- **License:** Ludo states that generated assets may be used in commercial games and grants a nonexclusive worldwide right to use, modify, distribute, and create derivatives. See [Game Asset Generation](https://ludo.ai/docs/game-asset-generation) and the [current Terms of Service and EULA](https://ludo.ai/documents/Jet%20Play%20Terms%20of%20Service%20and%20EULA%20%284%20Dec%202025%29.pdf).
- **Plan status:** the connected API key was validated and every listed request completed through the metered Ludo MCP/API. The named account tier is not exposed by the service. Ludo states commercial-use rights for generated assets on every plan. No ElevenLabs free-plan output, Freesound material, third-party recordings, voices, or recognizable copyrighted material is present.
- **Source retention:** Ludo URLs expire after seven days. Approved results were downloaded immediately, mastered locally, and committed. Request IDs below are the durable generation identifiers; temporary MP3/WAV inputs are intentionally not committed.
- **Mastering:** `tools/audio/master_ludo_audio.py` selects the useful physical transient/texture, removes DC, adds 6 ms anti-click fades, and peak-normalizes by category. Effects are mono 48 kHz/16-bit PCM WAV; ambience is stereo 48 kHz/16-bit PCM WAV; music is stereo 48 kHz Ogg Vorbis q5. All peaks are below -1 dBFS.

Run `python3 tools/audio/validate_audio.py` to verify formats, durations, levels, loop endpoints, manifests, and cue coverage.

## Approved generation catalog

All prompts requested original game audio and excluded voices, recognizable melodies, arcade bleeps, and electronic-laser styling where relevant. Returned sources were downloaded on 2026-08-29.

| ID / request ID | Service / model | Exact generation prompt |
|---|---|---|
| SFX-01 `lost-soul-fire-organic-scythe-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: a tight curved steel scythe slicing through air, dry sharp organic cloth-and-metal swish, fast physical motion, subtle low body, no impact, no melody, no voice, no arcade synth, no electronic laser, clean isolated studio sound |
| SFX-02 `lost-soul-fire-organic-soul-magic-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: a massive spectral scythe arc opening with a deep physical air whoom, followed by brittle ghostly ash crack and a soft breathy soul wake, organic layered texture, no melody, no voice, no arcade synth, no electronic laser, isolated |
| SFX-03 `lost-soul-fire-organic-impact-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: a compact scythe blade striking ash-covered bone and battered iron armor, dry crunchy fracture with dull metal body, no clean sword ring, no voice, no music, no arcade synth, isolated close impact |
| SFX-04 `lost-soul-fire-organic-dash-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: a short deep fire ignition whump and fast directional cloth-and-air whoosh for a supernatural dash, organic flame texture, not a jet engine, no voice, no music, no arcade synth, isolated |
| SFX-05 `lost-soul-fire-organic-cannon-charge-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: supernatural soul cannon charging, low occult mechanical vibration rising steadily into tense glassy energy, tactile metal resonance and breathy spectral texture, no electronic laser, no arcade synth, no melody, no voice, isolated |
| SFX-06 `lost-soul-fire-organic-cannon-fire-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: heavy soul cannon discharge with brutal physical transient, deep pressure, occult battered-metal character, ash debris and very short stone-room tail, no gunshot realism, no electronic laser, no arcade synth, no music, no voice |
| SFX-07 `lost-soul-fire-organic-fire-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: unstable magical furnace flame rapidly escalating through dense crackle and pressure into a compact violent ash detonation, deep physical fire texture, aggressive but controlled, no cinematic trailer boom, no voice, no music, no arcade synth |
| SFX-08 `lost-soul-fire-organic-soul-release-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: a trapped soul gently released after battle, quiet humanless breath of air, delicate struck-glass shimmer rising into a warm spectral bloom and fading residue, mournful relief, no voice, no melody, no arcade bleep, isolated |
| SFX-09 `lost-soul-fire-organic-death-flame-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: supernatural life flame collapsing inward and extinguishing, low organic implosion, dry ash fall, small dying ember crackles and a brief mournful room tail, no voice, no music, no electronic synth, isolated |
| SFX-10 `lost-soul-fire-organic-devourer-slam-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: enormous ash creature slams both arms into a ruined stone furnace floor, massive low physical impact, stone grit and debris, short heavy room tail, no roar, no voice, no music, no cinematic trailer boom |
| SFX-11 `lost-soul-fire-organic-devourer-devour-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: disturbing supernatural soul suction, hollow inhaling wind through bone and wet ash, deep pressure pulling inward then snapping shut, creature has no voice, no growl, no music, no electronic synth, isolated |
| SFX-12 `lost-soul-fire-organic-wave-start-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: a distant furnace gate dropping shut followed by one low ritual bronze bell announcing danger, heavy physical metal and stone, restrained short arena tail, no melody, no voice, no electronic synth, isolated |
| SFX-13 `lost-soul-fire-organic-title-confirm-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game UI sound: restrained tactile confirmation made from a tiny ember ignition and one delicate dark glass tap, warm and confident, extremely short, no arcade bleep, no melody, no voice, no electronic button tone |
| SFX-14 `lost-soul-fire-organic-wave-clear-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: brief relief after surviving combat, soft ash settling followed by three natural struck-glass and small bronze resonances forming an original unresolved minor-color cadence, subtle and physical, no orchestra, no voice, no arcade synth |
| SFX-15 `lost-soul-fire-organic-ending-reveal-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game sound effect: a tiny living flame revealed in darkness, quiet ember breath blossoms into warm glass and bronze harmonics with an original hopeful resolution, intimate physical textures, no orchestra, no voice, no recognizable melody, no electronic synth |
| SFX-16 `lost-soul-fire-organic-burning-charge-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game warning sound: unstable enemy furnace flame pressure rises continuously from a low ember into furious crackling over exactly one second, clearly escalating and stopping before any explosion, threatening organic fire telegraph, no detonation, no voice, no music, no synth |
| SFX-17 `lost-soul-fire-organic-cannon-full-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game readiness cue: unmistakable soul cannon reaches full charge, one tactile battered-metal latch clack immediately followed by a compact struck-glass soul resonance and deep core thump, short and decisive, no laser, no arcade bleep, no melody, no voice |
| SFX-18 `lost-soul-fire-organic-core-hit-20260829` | Ludo Sound Effects / undisclosed | Original dark-fantasy game weak-point hit sound: bright crystalline soul core cracking under a physical projectile impact, sharp glassy fracture with a compact rewarding low body, very short and readable, no arcade bleep, no melody, no voice, no electronic laser |
| AMB-01 `lost-soul-fire-organic-ambience-20260829` | Ludo Ambiance / undisclosed | Original seamless dark-fantasy ruined furnace arena ambience for a game: deep natural stone-room rumble, cold wind through broken masonry, sparse distant heavy chain movement, occasional tiny ember crackle, extremely faint nonverbal spectral breath texture, lots of quiet negative space, no music, no voices, no industrial machine rhythm, no melody, seamless loop |
| MUS-01 `lost-soul-fire-organic-music-20260829` | Ludo Music / undisclosed | Original seamless-loop dark Gothic industrial combat underscore for an indie action game, 72 BPM, D minor atmosphere, restrained low bowed strings, distant wordless choir-like texture used as pad not melody, sparse frame drums and furnace pulse, evolving dynamics with generous space for combat sound effects, ominous and mournful rather than heroic, no vocals, no lyrics, no recognizable melody or imitation of any existing soundtrack, clean loop-compatible ending |

## Final asset ledger

Every entry uses the Ludo commercial-use license and verified metered API entitlement described above.

| Final filename | Source ID | Date | Edits performed |
|---|---|---|---|
| `Audio/Sfx/scythe_swing_1.wav` | SFX-01 | 2026-08-29 | First tight air transient; mono; 180 ms; fades; peak -3.0 dBFS |
| `Audio/Sfx/scythe_swing_2.wav` | SFX-01 | 2026-08-29 | Wider later motion; mono; 240 ms; fades; peak -3.0 dBFS |
| `Audio/Sfx/soul_cleave.wav` | SFX-02 | 2026-08-29 | Physical whoom/ash-crack body; 450 ms; peak -2.4 dBFS |
| `Audio/Sfx/scythe_hit.wav` | SFX-03 | 2026-08-29 | Removed lead silence; isolated crunchy impact; 150 ms; peak -2.6 dBFS |
| `Audio/Sfx/dash.wav` | SFX-04 | 2026-08-29 | Ignition/air peak; 250 ms; peak -3.0 dBFS |
| `Audio/Sfx/cannon_charge.wav` | SFX-05 | 2026-08-29 | Rising second half; 800 ms; peak -4.0 dBFS |
| `Audio/Sfx/cannon_full.wav` | SFX-17 | 2026-08-29 | Isolated latch/glass/core transient; 220 ms; peak -2.4 dBFS |
| `Audio/Sfx/cannon_fire.wav` | SFX-06 | 2026-08-29 | Discharge transient and short room body; 550 ms; peak -2.0 dBFS |
| `Audio/Sfx/burning_charge.wav` | SFX-16 | 2026-08-29 | Reversed approved organic flame so energy rises; 550 ms; peak -4.0 dBFS |
| `Audio/Sfx/burning_detonation.wav` | SFX-07 | 2026-08-29 | Dense opening detonation; 700 ms; peak -2.2 dBFS |
| `Audio/Sfx/core_hit.wav` | SFX-18 | 2026-08-29 | Glass fracture/impact transient; 180 ms; peak -2.3 dBFS |
| `Audio/Sfx/soul_release.wav` | SFX-08 | 2026-08-29 | Organic breath and struck-glass bloom; 900 ms; peak -4.0 dBFS |
| `Audio/Sfx/resonance_ready.wav` | SFX-10 | 2026-08-29 | Low-passed slam transient as non-vocal core heartbeat; 350 ms; peak -4.0 dBFS |
| `Audio/Sfx/resonance_activate.wav` | SFX-10 + SFX-02 | 2026-08-29 | Layered low slam with delayed spectral ash crack; 800 ms; peak -1.8 dBFS |
| `Audio/Sfx/player_hit.wav` | SFX-03 | 2026-08-29 | Low-passed armor/bone impact variant; 200 ms; peak -3.0 dBFS |
| `Audio/Sfx/player_death.wav` | SFX-09 | 2026-08-29 | Flame collapse, ash, and mournful room tail; 1.2 s; peak -2.8 dBFS |
| `Audio/Sfx/soul_sense_on.wav` | SFX-02 | 2026-08-29 | Reversed breathy spectral section into perception swell; 450 ms; peak -4.5 dBFS |
| `Audio/Sfx/soul_sense_off.wav` | SFX-02 | 2026-08-29 | Contracting spectral tail; 300 ms; peak -4.5 dBFS |
| `Audio/Sfx/wave_start.wav` | SFX-12 | 2026-08-29 | Furnace gate and ritual bell body; 650 ms; peak -4.0 dBFS |
| `Audio/Sfx/hollow_swipe.wav` | SFX-01 | 2026-08-29 | Rougher later cloth/metal air texture; 320 ms; peak -4.0 dBFS |
| `Audio/Sfx/devourer_slam.wav` | SFX-10 | 2026-08-29 | Ground transient, grit, and short room; 650 ms; peak -1.8 dBFS |
| `Audio/Sfx/devourer_devour.wav` | SFX-11 | 2026-08-29 | Bone-wind/wet-ash suction and close; 850 ms; peak -2.8 dBFS |
| `Audio/Sfx/enemy_death.wav` | SFX-09 | 2026-08-29 | Short flame-collapse/ash variant; 550 ms; peak -4.0 dBFS |
| `Audio/Sfx/cannon_impact.wav` | SFX-06 | 2026-08-29 | Compact body/debris variant without long tail; 320 ms; peak -3.2 dBFS |
| `Audio/Sfx/title_confirm.wav` | SFX-13 | 2026-08-29 | Isolated ember/glass confirmation; 320 ms; peak -5.0 dBFS |
| `Audio/Sfx/wave_clear.wav` | SFX-14 | 2026-08-29 | Natural glass/bronze cadence; 750 ms; peak -5.0 dBFS |
| `Audio/Sfx/ending_reveal.wav` | SFX-15 | 2026-08-29 | Ember breath and warm harmonic bloom; 1.6 s; peak -4.5 dBFS |
| `Audio/Ambience/arena_ambience.wav` | AMB-01 | 2026-08-29 | Core Audio resample 44.1 to 48 kHz; stereo; full 20 s generated loop; click-free endpoints; peak -11.0 dBFS |
| `Audio/Music/arena_loop.ogg` | MUS-01 | 2026-08-29 | MP3 decoded to 48 kHz stereo PCM; returned 80 s performance uniformly resampled to 100 s; 20 ms loop-edge fades; peak -6.0 dBFS pre-encode; Vorbis q5 |

## Runtime verification modes

- `--audio-runtime-test` plays every cue and exercises ducking, cooldowns, and polyphony limits.
- `--audio-loop-runtime-test` crosses the music boundary and multiple ambience boundaries.
- `--audio-gameplay-test` runs all four waves, ending reveal, completion, and restart.
- `--audio-death-restart-test` verifies fatal damage, death cue/state, and restart.
- Add `--expect-audio-fallback` after making one built SFX XNB unavailable; the test fails unless the synthesized emergency fallback was created.
