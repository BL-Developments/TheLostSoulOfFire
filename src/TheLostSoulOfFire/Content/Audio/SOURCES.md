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

## Lokal synthetisierte Klänge (tools/audio, ab 2026-10-06)

Für Bereiche und Aktionen, die bisher keinen eigenen Klang hatten, entstanden Atmosphären,
Musikbetten und Cues lokal aus Rezepten in `tools/audio/recipes/*.py` (numpy/scipy, Seed
festgehalten). Es gibt keine Samples, keine fremden Aufnahmen und keinen Netzdienst. Die
Ergebnisse sind eigene Arbeit des Projekts.

- **Werkzeuge:** `tools/audio/dsp.py` (Rauschen, Filter, Glocken, Zupf- und Streicherklänge,
  Chor, synthetischer Raum), `author.py` (rendert ein Rezept mit Seed, Spektrogramm und
  Bericht mit LUFS, Spitze und Naht), `install.py` (übernimmt die gewählte Aufnahme nach
  `Content/Audio`).
- **Musik:** sechs Stücke in der Tonart der Arena-Musik (gis-Moll) mit einem gemeinsamen
  Motiv dis–cis–h–gis, das auf ais stehen bleibt. Erst an der Schwelle löst es sich nach H.
  Kodiert als Ogg Vorbis q5 mit 48 kHz Stereo.
- **Loops:** Der Raumhall läuft über die Loopgrenze zurück an den Anfang. An den Rändern
  liegt eine Blende von 8 ms.
- **Prüfung (Hören ersetzt):**
  - Lautheit nach EBU R128 mit pyloudnorm und Spitzenpegel.
  - Spektrogramme.
  - Nahtprüfung (`validate_audio.py`).
  - LAION-CLAP (`laion/clap-htsat-unfused`, Apache-2.0) über `tools/audio/listen.py`: Es ordnet
    jeden Kandidaten gegen Textbeschreibungen ein, damit er nicht als Rauschen oder
    Tanzmusik gelesen wird.
  - Die endgültige Abnahme im Spiel durch Hören steht beim Owner aus.

| Datei | Rezept und Seed | Eigenschaften |
|---|---|---|
| `Audio/Ambience/shore_ambience.wav` | ambience-shore Seed 2 | 40.00 s, 2 Kanal/Kanäle, -29.0 LUFS, Spitze -16.8 dBFS |
| `Audio/Ambience/hub_ambience.wav` | ambience-hub Seed 2 (07.10.2026 ohne eingebackene Flammen) | 32.00 s, 2 Kanal/Kanäle, -30.0 LUFS, Spitze -11.9 dBFS |
| `Audio/Ambience/harbour_ambience.wav` | ambience-harbour Seed 1 | 36.00 s, 2 Kanal/Kanäle, -29.0 LUFS, Spitze -15.0 dBFS |
| `Audio/Ambience/causeway_ambience.wav` | ambience-causeway Seed 1 | 36.00 s, 2 Kanal/Kanäle, -27.0 LUFS, Spitze -14.4 dBFS |
| `Audio/Ambience/crossing_ambience.wav` | ambience-crossing Seed 1, Naht 1,5 s überblendet | 22.50 s, 2 Kanal/Kanäle, -26.0 LUFS, Spitze -13.1 dBFS |
| `Audio/Ambience/threshold_ambience.wav` | ambience-threshold Seed 5 | 32.00 s, 2 Kanal/Kanäle, -34.0 LUFS, Spitze -25.7 dBFS |
| `Audio/Music/title_theme.ogg` | music-title Seed 1 | 58.18 s, 2 Kanal/Kanäle, -20.0 LUFS, Spitze -6.1 dBFS |
| `Audio/Music/shore_theme.ogg` | music-shore Seed 1 | 68.57 s, 2 Kanal/Kanäle, -23.0 LUFS, Spitze -8.0 dBFS |
| `Audio/Music/hub_theme.ogg` | music-hub Seed 1 | 64.00 s, 2 Kanal/Kanäle, -22.0 LUFS, Spitze -9.7 dBFS |
| `Audio/Music/causeway_theme.ogg` | music-causeway Seed 4 | 53.33 s, 2 Kanal/Kanäle, -20.0 LUFS, Spitze -4.9 dBFS |
| `Audio/Music/crossing_theme.ogg` | music-crossing Seed 3 | 55.38 s, 2 Kanal/Kanäle, -18.0 LUFS, Spitze -3.0 dBFS |
| `Audio/Music/threshold_theme.ogg` | music-threshold Seed 1 | 49.66 s, 2 Kanal/Kanäle, -22.0 LUFS, Spitze -7.2 dBFS |
| `Audio/Sfx/footstep_stone_1.wav` | footstep-stone Seed 1 | 0.30 s, 1 Kanal/Kanäle, -26.0 LUFS, Spitze -6.8 dBFS |
| `Audio/Sfx/footstep_stone_2.wav` | footstep-stone Seed 2 | 0.30 s, 1 Kanal/Kanäle, -26.0 LUFS, Spitze -7.3 dBFS |
| `Audio/Sfx/footstep_stone_3.wav` | footstep-stone Seed 3 | 0.30 s, 1 Kanal/Kanäle, -28.2 LUFS, Spitze -6.0 dBFS |
| `Audio/Sfx/footstep_stone_4.wav` | footstep-stone Seed 4 | 0.30 s, 1 Kanal/Kanäle, -26.0 LUFS, Spitze -7.9 dBFS |
| `Audio/Sfx/footstep_wood_1.wav` | footstep-wood Seed 1 | 0.34 s, 1 Kanal/Kanäle, -26.2 LUFS, Spitze -6.0 dBFS |
| `Audio/Sfx/footstep_wood_2.wav` | footstep-wood Seed 2 | 0.34 s, 1 Kanal/Kanäle, -26.0 LUFS, Spitze -7.4 dBFS |
| `Audio/Sfx/footstep_wood_3.wav` | footstep-wood Seed 3 | 0.34 s, 1 Kanal/Kanäle, -26.0 LUFS, Spitze -7.2 dBFS |
| `Audio/Sfx/footstep_wood_4.wav` | footstep-wood Seed 4 | 0.34 s, 1 Kanal/Kanäle, -26.0 LUFS, Spitze -6.1 dBFS |
| `Audio/Sfx/ui_move.wav` | ui-move Seed 1 | 0.18 s, 1 Kanal/Kanäle, -30.0 LUFS, Spitze -17.3 dBFS |
| `Audio/Sfx/ui_back.wav` | ui-back Seed 1 | 0.20 s, 1 Kanal/Kanäle, -31.0 LUFS, Spitze -17.0 dBFS |
| `Audio/Sfx/ui_open.wav` | ui-open Seed 1 | 0.45 s, 1 Kanal/Kanäle, -30.0 LUFS, Spitze -17.0 dBFS |
| `Audio/Sfx/ui_close.wav` | ui-close Seed 1 | 0.32 s, 1 Kanal/Kanäle, -29.7 LUFS, Spitze -17.0 dBFS |
| `Audio/Sfx/chest_open.wav` | chest-open Seed 1 | 0.90 s, 1 Kanal/Kanäle, -22.4 LUFS, Spitze -3.0 dBFS |
| `Audio/Sfx/currency_gain.wav` | currency-gain Seed 1 | 0.50 s, 1 Kanal/Kanäle, -27.0 LUFS, Spitze -14.5 dBFS |
| `Audio/Sfx/ability_heal.wav` | ability-heal Seed 1 | 1.10 s, 1 Kanal/Kanäle, -20.0 LUFS, Spitze -7.5 dBFS |
| `Audio/Sfx/ability_pierce.wav` | ability-pierce Seed 1 | 0.60 s, 1 Kanal/Kanäle, -17.0 LUFS, Spitze -5.6 dBFS |
| `Audio/Sfx/ability_leap.wav` | ability-leap Seed 1 | 0.55 s, 1 Kanal/Kanäle, -17.0 LUFS, Spitze -5.6 dBFS |
| `Audio/Sfx/ability_vortex.wav` | ability-vortex Seed 1 | 1.20 s, 1 Kanal/Kanäle, -18.0 LUFS, Spitze -4.5 dBFS |
| `Audio/Sfx/ability_guard.wav` | ability-guard Seed 1 | 1.00 s, 1 Kanal/Kanäle, -18.0 LUFS, Spitze -2.7 dBFS |
| `Audio/Sfx/ability_mark.wav` | ability-mark Seed 1 | 0.50 s, 1 Kanal/Kanäle, -20.0 LUFS, Spitze -3.4 dBFS |
| `Audio/Sfx/door_awaken.wav` | door-awaken Seed 1 | 1.80 s, 1 Kanal/Kanäle, -18.0 LUFS, Spitze -2.2 dBFS |
| `Audio/Sfx/step_hollow_1.wav` | step-hollow Seed 1 | 0.42 s, 1 Kanal/Kanäle, -29.0 LUFS, Spitze -15.2 dBFS |
| `Audio/Sfx/step_hollow_2.wav` | step-hollow Seed 2 | 0.42 s, 1 Kanal/Kanäle, -29.0 LUFS, Spitze -15.8 dBFS |
| `Audio/Sfx/step_hollow_3.wav` | step-hollow Seed 3 | 0.42 s, 1 Kanal/Kanäle, -29.0 LUFS, Spitze -15.7 dBFS |
| `Audio/Sfx/step_hollow_4.wav` | step-hollow Seed 4 | 0.42 s, 1 Kanal/Kanäle, -29.0 LUFS, Spitze -15.4 dBFS |
| `Audio/Sfx/step_burning_1.wav` | step-burning Seed 1 | 0.26 s, 1 Kanal/Kanäle, -28.0 LUFS, Spitze -11.2 dBFS |
| `Audio/Sfx/step_burning_2.wav` | step-burning Seed 2 | 0.26 s, 1 Kanal/Kanäle, -28.0 LUFS, Spitze -13.5 dBFS |
| `Audio/Sfx/step_burning_3.wav` | step-burning Seed 3 | 0.26 s, 1 Kanal/Kanäle, -28.0 LUFS, Spitze -11.6 dBFS |
| `Audio/Sfx/step_burning_4.wav` | step-burning Seed 4 | 0.26 s, 1 Kanal/Kanäle, -28.0 LUFS, Spitze -12.4 dBFS |
| `Audio/Sfx/step_devourer_1.wav` | step-devourer Seed 1 | 0.70 s, 1 Kanal/Kanäle, -25.2 LUFS, Spitze -4.0 dBFS |
| `Audio/Sfx/step_devourer_2.wav` | step-devourer Seed 2 | 0.70 s, 1 Kanal/Kanäle, -25.0 LUFS, Spitze -4.0 dBFS |
| `Audio/Sfx/step_devourer_3.wav` | step-devourer Seed 3 | 0.70 s, 1 Kanal/Kanäle, -24.9 LUFS, Spitze -4.0 dBFS |
| `Audio/Sfx/step_devourer_4.wav` | step-devourer Seed 4 | 0.70 s, 1 Kanal/Kanäle, -24.0 LUFS, Spitze -4.6 dBFS |
| `Audio/Sfx/enemy_emerge.wav` | enemy-emerge Seed 1 | 0.90 s, 1 Kanal/Kanäle, -21.0 LUFS, Spitze -4.0 dBFS |
| `Audio/Sfx/scythe_swing_1_v2.wav` | Ableitung von scythe_swing_1.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.17 s, 1 Kanal, -13.7 LUFS, Spitze -2.8 dBFS |
| `Audio/Sfx/scythe_swing_1_v3.wav` | Ableitung von scythe_swing_1.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.19 s, 1 Kanal, -13.7 LUFS, Spitze -3.0 dBFS |
| `Audio/Sfx/scythe_swing_2_v2.wav` | Ableitung von scythe_swing_2.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.23 s, 1 Kanal, -14.5 LUFS, Spitze -3.7 dBFS |
| `Audio/Sfx/scythe_swing_2_v3.wav` | Ableitung von scythe_swing_2.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.25 s, 1 Kanal, -14.5 LUFS, Spitze -2.9 dBFS |
| `Audio/Sfx/scythe_hit_v2.wav` | Ableitung von scythe_hit.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.14 s, 1 Kanal, -16.9 LUFS, Spitze -3.0 dBFS |
| `Audio/Sfx/scythe_hit_v3.wav` | Ableitung von scythe_hit.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.16 s, 1 Kanal, -16.9 LUFS, Spitze -2.5 dBFS |
| `Audio/Sfx/core_hit_v2.wav` | Ableitung von core_hit.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.17 s, 1 Kanal, -12.9 LUFS, Spitze -3.1 dBFS |
| `Audio/Sfx/core_hit_v3.wav` | Ableitung von core_hit.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.19 s, 1 Kanal, -12.9 LUFS, Spitze -2.0 dBFS |
| `Audio/Sfx/cannon_impact_v2.wav` | Ableitung von cannon_impact.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.31 s, 1 Kanal, -14.0 LUFS, Spitze -3.1 dBFS |
| `Audio/Sfx/cannon_impact_v3.wav` | Ableitung von cannon_impact.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.34 s, 1 Kanal, -14.0 LUFS, Spitze -2.8 dBFS |
| `Audio/Sfx/dash_v2.wav` | Ableitung von dash.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.24 s, 1 Kanal, -17.2 LUFS, Spitze -3.4 dBFS |
| `Audio/Sfx/dash_v3.wav` | Ableitung von dash.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.26 s, 1 Kanal, -17.2 LUFS, Spitze -2.5 dBFS |
| `Audio/Sfx/hollow_swipe_v2.wav` | Ableitung von hollow_swipe.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.31 s, 1 Kanal, -16.3 LUFS, Spitze -4.1 dBFS |
| `Audio/Sfx/hollow_swipe_v3.wav` | Ableitung von hollow_swipe.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.34 s, 1 Kanal, -16.3 LUFS, Spitze -4.3 dBFS |
| `Audio/Sfx/enemy_death_v2.wav` | Ableitung von enemy_death.wav (Tonhöhe ×1.025, Klangneigung +0.8/-0.6 dB) | 0.54 s, 1 Kanal, -14.3 LUFS, Spitze -4.4 dBFS |
| `Audio/Sfx/enemy_death_v3.wav` | Ableitung von enemy_death.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.58 s, 1 Kanal, -14.3 LUFS, Spitze -3.9 dBFS |

## Kampfschichten (Polish-Durchgang, 06.10.2026)

Rezepte in `tools/audio/recipes/combat.py`. Sie ersetzen nichts aus der Ludo-Bank, sondern liegen
darunter: ein Kontakt-Transient auf Sample 0 (der Ludo-Treffer setzt erst nach 40–55 ms ein), das
Material des Ziels, Warnungen für die bisher stummen Ausholbewegungen von Hollow und Devourer, der
Anlauf des Burning und ein eigener Tod je Gegner. Takes einer Gruppe auf ±0,5 LU angeglichen.

| Datei | Quelle | Messung |
| --- | --- | --- |
| `Audio/Sfx/hit_hollow_1.wav` | hit-hollow Seed 1 | 0.32 s, 1 Kanal, -20.8 LUFS, Spitze -3.0 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_hollow_2.wav` | hit-hollow Seed 2 | 0.32 s, 1 Kanal, -21.3 LUFS, Spitze -2.5 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_hollow_3.wav` | hit-hollow Seed 3 | 0.32 s, 1 Kanal, -20.8 LUFS, Spitze -2.5 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_burning_1.wav` | hit-burning Seed 1 | 0.42 s, 1 Kanal, -22.1 LUFS, Spitze -5.2 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_burning_2.wav` | hit-burning Seed 2 | 0.42 s, 1 Kanal, -22.6 LUFS, Spitze -2.5 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_burning_3.wav` | hit-burning Seed 3 | 0.42 s, 1 Kanal, -22.1 LUFS, Spitze -5.9 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_devourer_1.wav` | hit-devourer Seed 1 | 0.55 s, 1 Kanal, -22.1 LUFS, Spitze -3.2 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_devourer_2.wav` | hit-devourer Seed 2 | 0.55 s, 1 Kanal, -22.6 LUFS, Spitze -2.0 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_devourer_3.wav` | hit-devourer Seed 3 | 0.55 s, 1 Kanal, -22.1 LUFS, Spitze -3.4 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_dummy_1.wav` | hit-dummy Seed 1 | 0.36 s, 1 Kanal, -22.2 LUFS, Spitze -3.0 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_dummy_2.wav` | hit-dummy Seed 2 | 0.36 s, 1 Kanal, -22.7 LUFS, Spitze -3.0 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_dummy_3.wav` | hit-dummy Seed 3 | 0.36 s, 1 Kanal, -22.2 LUFS, Spitze -4.4 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_heavy_1.wav` | hit-heavy Seed 1 | 0.40 s, 1 Kanal, -22.0 LUFS, Spitze -2.0 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_heavy_2.wav` | hit-heavy Seed 2 | 0.40 s, 1 Kanal, -21.6 LUFS, Spitze -2.0 dBFS (Takes angeglichen) |
| `Audio/Sfx/hit_heavy_3.wav` | hit-heavy Seed 3 | 0.40 s, 1 Kanal, -21.5 LUFS, Spitze -2.4 dBFS (Takes angeglichen) |
| `Audio/Sfx/body_hit_1.wav` | body-hit Seed 1 | 0.24 s, 1 Kanal, -19.1 LUFS, Spitze -3.5 dBFS (Takes angeglichen) |
| `Audio/Sfx/body_hit_2.wav` | body-hit Seed 2 | 0.24 s, 1 Kanal, -19.2 LUFS, Spitze -2.5 dBFS (Takes angeglichen) |
| `Audio/Sfx/body_hit_3.wav` | body-hit Seed 3 | 0.24 s, 1 Kanal, -19.6 LUFS, Spitze -2.5 dBFS (Takes angeglichen) |
| `Audio/Sfx/hollow_windup_1.wav` | hollow-windup Seed 1 | 0.44 s, 1 Kanal, -18.5 LUFS, Spitze -2.9 dBFS (Takes angeglichen, +3,5 dB nach Mixprüfung) |
| `Audio/Sfx/hollow_windup_2.wav` | hollow-windup Seed 2 | 0.44 s, 1 Kanal, -18.5 LUFS, Spitze -2.8 dBFS (Takes angeglichen, +3,5 dB nach Mixprüfung) |
| `Audio/Sfx/devourer_windup_1.wav` | devourer-windup Seed 1 | 0.86 s, 1 Kanal, -21.0 LUFS, Spitze -5.2 dBFS (Takes angeglichen) |
| `Audio/Sfx/devourer_windup_2.wav` | devourer-windup Seed 2 | 0.86 s, 1 Kanal, -21.0 LUFS, Spitze -5.8 dBFS (Takes angeglichen) |
| `Audio/Sfx/burning_rush_1.wav` | burning-rush Seed 1 | 0.62 s, 1 Kanal, -22.1 LUFS, Spitze -3.7 dBFS (Takes angeglichen) |
| `Audio/Sfx/burning_rush_2.wav` | burning-rush Seed 2 | 0.62 s, 1 Kanal, -22.6 LUFS, Spitze -3.0 dBFS (Takes angeglichen) |
| `Audio/Sfx/death_hollow_1.wav` | death-hollow Seed 1 | 0.90 s, 1 Kanal, -20.2 LUFS, Spitze -2.5 dBFS (Takes angeglichen) |
| `Audio/Sfx/death_hollow_2.wav` | death-hollow Seed 2 | 0.90 s, 1 Kanal, -20.5 LUFS, Spitze -2.5 dBFS (Takes angeglichen) |
| `Audio/Sfx/death_burning_1.wav` | death-burning Seed 1 | 0.90 s, 1 Kanal, -25.2 LUFS, Spitze -3.0 dBFS (Takes angeglichen) |
| `Audio/Sfx/death_burning_2.wav` | death-burning Seed 2 | 0.90 s, 1 Kanal, -24.7 LUFS, Spitze -6.0 dBFS (Takes angeglichen) |
| `Audio/Sfx/death_devourer_1.wav` | death-devourer Seed 1 | 1.40 s, 1 Kanal, -20.0 LUFS, Spitze -2.3 dBFS (Takes angeglichen) |
| `Audio/Sfx/death_devourer_2.wav` | death-devourer Seed 2 | 1.40 s, 1 Kanal, -20.5 LUFS, Spitze -2.0 dBFS (Takes angeglichen) |
| `Audio/Sfx/scythe_swing_1_lead.wav` | scythe_swing_1.wav, erste 30 ms gekürzt (Körper früher, Animation) | 0.15 s, 1 Kanal, -12.8 LUFS, Spitze -3.0 dBFS |
| `Audio/Sfx/scythe_swing_1_lead_v2.wav` | scythe_swing_1_v2.wav, erste 30 ms gekürzt (Körper früher, Animation) | 0.14 s, 1 Kanal, -12.8 LUFS, Spitze -2.8 dBFS |
| `Audio/Sfx/scythe_swing_1_lead_v3.wav` | scythe_swing_1_v3.wav, erste 30 ms gekürzt (Körper früher, Animation) | 0.16 s, 1 Kanal, -12.9 LUFS, Spitze -3.0 dBFS |

## Ende (Durchgang 3, 06.10.2026)

Rezept `life-flame` in `tools/audio/recipes/ambiences.py`: das Feuer der Life Flame im kalten Ofen
nach der letzten Welle, eine nahtlose Mono-Schleife (das Spiel setzt sie in Richtung des Ofens und
blendet sie mit der wachsenden Flamme ein). Tiefes, leises Brausen; Knistern in unregelmäßigen
Büscheln mit wenigen lauten Pops (Holzkörper bei etwa einem Drittel), ein feines Zischbett. CLAP
ordnet die Takes als „fireplace with burning wood“ (0,78) und „campfire crackling“ (0,18) ein; die
erste Fassung mit stärkerem Brausen hörte es als Wind und wurde verworfen.

| Datei | Quelle | Messung |
| --- | --- | --- |
| `Audio/Sfx/life_flame_loop.wav` | life-flame Seed 2 | 9.00 s, 1 Kanal, -27.7 LUFS, Spitze -4.0 dBFS, Naht -90 dBFS |

## Varianten weiterer häufiger Töne (Durchgang 3, 06.10.2026)

Abgeleitet mit `tools/audio/derive_variants.py` (Tonhöhe ±4,5 %, Klangneigung, Lautheit wie das Original). CLAP-Ähnlichkeit zum Original 0,86–0,99, zu anderen Tönen höchstens 0,60; zufällig gewählt wie die übrigen Varianten.

| Datei | Quelle | Messung |
| --- | --- | --- |
| `Audio/Sfx/soul_cleave_v2.wav` | Ableitung von soul_cleave.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.43 s, 1 Kanal, -18.2 LUFS, Spitze -3.1 dBFS |
| `Audio/Sfx/soul_cleave_v3.wav` | Ableitung von soul_cleave.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.47 s, 1 Kanal, -18.2 LUFS, Spitze -1.7 dBFS |
| `Audio/Sfx/soul_release_v2.wav` | Ableitung von soul_release.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.86 s, 1 Kanal, -17.2 LUFS, Spitze -3.4 dBFS |
| `Audio/Sfx/soul_release_v3.wav` | Ableitung von soul_release.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.94 s, 1 Kanal, -17.2 LUFS, Spitze -4.4 dBFS |
| `Audio/Sfx/enemy_emerge_v2.wav` | Ableitung von enemy_emerge.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.86 s, 1 Kanal, -21.0 LUFS, Spitze -4.1 dBFS |
| `Audio/Sfx/enemy_emerge_v3.wav` | Ableitung von enemy_emerge.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.94 s, 1 Kanal, -21.0 LUFS, Spitze -4.0 dBFS |
| `Audio/Sfx/cannon_fire_v2.wav` | Ableitung von cannon_fire.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.53 s, 1 Kanal, -15.3 LUFS, Spitze -2.4 dBFS |
| `Audio/Sfx/cannon_fire_v3.wav` | Ableitung von cannon_fire.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.58 s, 1 Kanal, -15.3 LUFS, Spitze -1.8 dBFS |
| `Audio/Sfx/burning_detonation_v2.wav` | Ableitung von burning_detonation.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.67 s, 1 Kanal, -16.1 LUFS, Spitze -1.6 dBFS |
| `Audio/Sfx/burning_detonation_v3.wav` | Ableitung von burning_detonation.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.73 s, 1 Kanal, -16.1 LUFS, Spitze -2.8 dBFS |
| `Audio/Sfx/burning_charge_v2.wav` | Ableitung von burning_charge.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.53 s, 1 Kanal, -21.5 LUFS, Spitze -3.0 dBFS |
| `Audio/Sfx/burning_charge_v3.wav` | Ableitung von burning_charge.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.58 s, 1 Kanal, -21.5 LUFS, Spitze -3.4 dBFS |
| `Audio/Sfx/player_hit_v2.wav` | Ableitung von player_hit.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.19 s, 1 Kanal, -19.5 LUFS, Spitze -3.1 dBFS |
| `Audio/Sfx/player_hit_v3.wav` | Ableitung von player_hit.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.21 s, 1 Kanal, -19.5 LUFS, Spitze -3.0 dBFS |
| `Audio/Sfx/currency_gain_v2.wav` | Ableitung von currency_gain.wav (Tonhöhe ×1.045, Klangneigung -1.0/+1.5 dB) | 0.48 s, 1 Kanal, -27.0 LUFS, Spitze -14.4 dBFS |
| `Audio/Sfx/currency_gain_v3.wav` | Ableitung von currency_gain.wav (Tonhöhe ×0.955, Klangneigung +1.5/-1.2 dB) | 0.52 s, 1 Kanal, -27.0 LUFS, Spitze -15.4 dBFS |

## Hallfahnen (Durchgang 3, 06.10.2026)

Mit `tools/audio/hall_tails.py`: der trockene Take gefaltet mit der Impulsantwort seiner Halle, nur der Hall, auf −8 LU unter dem Take gesetzt. Das Spiel spielt ihn nur in dieser Halle mit dem Ton zusammen (Anteil 0,3–0,4, `AudioDirector.HallSends`). CLAP: Trocken + Fahne bei 0,35 wird als „hit in a large reverberant stone hall“ eingeordnet (0,90–0,94, trocken 0,1–0,6), eine Folge von fünf Treffern im Abstand von 0,25 s nicht als „washy reverb“ (0,05).

| Datei | Quelle | Messung |
| --- | --- | --- |
| `Audio/Sfx/scythe_hit_hall.wav` | Hallfahne von scythe_hit.wav (Gießhalle, RT60 2.4 s) | 2.51 s, 1 Kanal, -24.9 LUFS, Spitze -9.1 dBFS |
| `Audio/Sfx/core_hit_hall.wav` | Hallfahne von core_hit.wav (Gießhalle, RT60 2.4 s) | 2.44 s, 1 Kanal, -20.9 LUFS, Spitze -4.4 dBFS |
| `Audio/Sfx/cannon_fire_hall.wav` | Hallfahne von cannon_fire.wav (Gießhalle, RT60 2.4 s) | 2.71 s, 1 Kanal, -23.3 LUFS, Spitze -5.0 dBFS |
| `Audio/Sfx/cannon_impact_hall.wav` | Hallfahne von cannon_impact.wav (Gießhalle, RT60 2.4 s) | 2.62 s, 1 Kanal, -22.0 LUFS, Spitze -3.7 dBFS |
| `Audio/Sfx/burning_detonation_hall.wav` | Hallfahne von burning_detonation.wav (Gießhalle, RT60 2.4 s) | 2.89 s, 1 Kanal, -24.1 LUFS, Spitze -7.0 dBFS |
| `Audio/Sfx/devourer_slam_hall.wav` | Hallfahne von devourer_slam.wav (Gießhalle, RT60 2.4 s) | 2.71 s, 1 Kanal, -21.0 LUFS, Spitze -3.5 dBFS |
| `Audio/Sfx/enemy_death_hall.wav` | Hallfahne von enemy_death.wav (Gießhalle, RT60 2.4 s) | 2.88 s, 1 Kanal, -22.3 LUFS, Spitze -4.6 dBFS |
| `Audio/Sfx/soul_cleave_hall.wav` | Hallfahne von soul_cleave.wav (Gießhalle, RT60 2.4 s) | 2.64 s, 1 Kanal, -26.2 LUFS, Spitze -6.1 dBFS |
| `Audio/Sfx/player_hit_hall.wav` | Hallfahne von player_hit.wav (Gießhalle, RT60 2.4 s) | 2.51 s, 1 Kanal, -27.5 LUFS, Spitze -11.8 dBFS |
| `Audio/Sfx/wave_start_hall.wav` | Hallfahne von wave_start.wav (Gießhalle, RT60 2.4 s) | 2.96 s, 1 Kanal, -28.3 LUFS, Spitze -14.6 dBFS |
| `Audio/Sfx/footstep_stone_1_hall.wav` | Hallfahne von footstep_stone_1.wav (Vorhalle, RT60 3.2 s) | 3.26 s, 1 Kanal, -34.0 LUFS, Spitze -15.6 dBFS |

## Seelenkanone ziehen und verstauen (Durchgang 3, 06.10.2026)

Rezepte `cannon-draw` und `cannon-stow` in `tools/audio/recipes/cues.py`: Riemen, schweres Eisen, Kammerraste; beim Verstauen ohne harten Anschlag (die erste Fassung hörte CLAP als Schuss). CLAP: Ziehen „metal clank“ 0,66 + „heavy metal weapon“ 0,26; Verstauen „leather creaking“ 0,31 + „heavy metal weapon being put away“ 0,24.

| Datei | Quelle | Messung |
| --- | --- | --- |
| `Audio/Sfx/cannon_draw.wav` | cannon-draw Seed 1 | 0.42 s, 1 Kanal/Kanäle, -24.5 LUFS, Spitze -3.0 dBFS |
| `Audio/Sfx/cannon_stow.wav` | cannon-stow Seed 2 | 0.36 s, 1 Kanal/Kanäle, -23.0 LUFS, Spitze -8.5 dBFS |
| `Audio/Sfx/footstep_stone_1_hall_foundry.wav` | Hallfahne von footstep_stone_1.wav (Gießhalle, RT60 2.4 s) | 2.46 s, 1 Kanal, -34.0 LUFS, Spitze -15.3 dBFS |

## Präsenz der Gegner (Durchgang 3, 06.10.2026)

Rezepte `presence-burning`, `presence-hollow`, `presence-devourer` in `tools/audio/recipes/ambiences.py`, nach 16_AUDIO_DIRECTION (Burning: Knistern, instabiles Flammengrollen; Hollow: leises Atmen, verzerrtes Flüstern; Devourer: verzerrte Seelenstimmen aus dem Rumpf). Nahtlose Mono-Schleifen; das Spiel setzt je Art eine Schleife in Richtung des nächsten Gegners und blendet sie nach Abstand. CLAP: Burning „fire crackling and roaring“ 0,66; Hollow „choir of ghostly voices“ 0,68; Devourer „deep monster growl“ 0,38 + „ghostly voices“ 0,27 (die rauschhafte erste Fassung hörte CLAP als Wind, verworfen).

| Datei | Quelle | Messung |
| --- | --- | --- |
| `Audio/Sfx/presence_burning.wav` | presence-burning Seed 1 | 5.00 s, 1 Kanal/Kanäle, -24.0 LUFS, Spitze -4.8 dBFS |
| `Audio/Sfx/presence_hollow.wav` | presence-hollow Seed 1 | 6.00 s, 1 Kanal/Kanäle, -26.0 LUFS, Spitze -8.5 dBFS |
| `Audio/Sfx/presence_devourer.wav` | presence-devourer Seed 1 | 7.00 s, 1 Kanal/Kanäle, -25.0 LUFS, Spitze -8.0 dBFS |

## Resonanz (Durchgang 3, 06.10.2026)

Rezept `resonance-rumble`: die eigene Death Flame während der Resonanz (16_AUDIO_DIRECTION: dezentes Flammengrollen, keine Sirene). Tiefes Grollen mit zwei langsamen Schüben, leiser Herzschlag (60 bpm), wenige weiche Funken. CLAP „deep rumbling fire“ 0,78, „alarm siren“ 0,002. Verworfen: schnelles Flackern im Pegel (CLAP: „wind“ 0,6).

| Datei | Quelle | Messung |
| --- | --- | --- |
| `Audio/Sfx/resonance_rumble.wav` | resonance-rumble Seed 2 | 4.00 s, 1 Kanal/Kanäle, -25.0 LUFS, Spitze -9.4 dBFS |

## Ladebrummen der Seelenkanone (Durchgang 3, 06.10.2026)

Rezept `cannon-hum` in `tools/audio/recipes/cues.py`: Brummen der Kammer (55–330 Hz), Seelenwimmern (880/1320 Hz mit Vibrato), Vibrieren (Tremolo 18 Hz); alle Frequenzen in ganzen Perioden über 2 s, also nahtlos. Das Spiel hebt Tonhöhe (−0,4 bis +0,2) und Pegel mit der Ladung (16_AUDIO_DIRECTION: hörbar ansteigende Ladung). CLAP „electric hum of a charging energy weapon“ 0,85.

| Datei | Quelle | Messung |
| --- | --- | --- |
| `Audio/Sfx/cannon_hum.wav` | cannon-hum Seed 1 | 2.00 s, 1 Kanal/Kanäle, -22.0 LUFS, Spitze -11.8 dBFS |

## Sanfte Seelenfreigabe (Durchgang 3, 06.10.2026)

Owner: Das Einsammeln der Seelen und die Piep-Töne werden in Kämpfen mit vielen Gegnern nervig. Der Ludo-Take `soul_release` (Klangschwerpunkt 8,4 kHz, CLAP „piercing electronic beep“ 0,64) wird durch das Rezept `soul-release-soft` ersetzt: weiche tiefe Glocke in der Tonart der Musik (D#4/F#4/G#4), leises Ausatmen, Tiefpass 5 kHz; Schwerpunkt 1,8 kHz, CLAP „bell“ 0,64 + „soft calm chime“ 0,17, „beep“ 0,09. Eine Höhenabsenkung des alten Takes allein half nicht (Grundton zu hoch; verworfen). Der alte Take bleibt im Bestand, wird aber nicht mehr gespielt.

| Datei | Quelle | Messung |
| --- | --- | --- |
| `Audio/Sfx/soul_release_soft_1.wav` | soul-release-soft Seed 2 | 1.10 s, 1 Kanal/Kanäle, -22.0 LUFS, Spitze -9.0 dBFS |
| `Audio/Sfx/soul_release_soft_2.wav` | soul-release-soft Seed 4 | 1.10 s, 1 Kanal/Kanäle, -22.0 LUFS, Spitze -6.9 dBFS |
| `Audio/Sfx/soul_release_soft_3.wav` | soul-release-soft Seed 1 | 1.10 s, 1 Kanal/Kanäle, -22.0 LUFS, Spitze -7.8 dBFS |

## Warden-Flammen als Punktquellen (Durchgang 3, 07.10.2026)

Die Flammen der Vorhalle waren in die Atmosphäre eingebacken (links −0,35, rechts 0,3) und standen still, wo immer man ging. Rezept `warden-flame`: weiches, tiefes Brausen, das schnell flattert, wenig Luft darüber, ein leises Summen in Gis, selten ein weicher Glutknack; Mono-Schleife. Das Spiel spielt sie dort, wo Feuerschalen (voll) und Wandleuchter (schwächer) brennen, lauter im Vorbeigehen. CLAP: „torch flame burning“ 0,92, „wind“ 0,04, „rain“ 0,02. Erste Fassungen mit hellem Zischen und Ticken hörte CLAP als „rain“ (bis 0,57), mit langsamem Flattern als „wind“ (bis 0,52). `ambience-hub` neu ohne die eingebackene Flammenschicht (gleicher Seed, Zufallsstrom unverändert, damit alle übrigen Schichten gleich bleiben).

| Datei | Quelle | Messung |
| --- | --- | --- |
| `Audio/Sfx/warden_flame_loop.wav` | warden-flame Seed 3 | 6.00 s, 1 Kanal/Kanäle, -26.0 LUFS, Spitze -10.5 dBFS |

## Pochen der gebundenen Seele (Durchgang 3, 06.10.2026)

Wenig Leben (30 % oder weniger) meldete nur die Lebensleiste in der Ecke. Rezept `soul-throb`: dumpfer, tiefer Doppelschlag (Gis1 fallend nach E1, weicher Einsatz, gedämpfter Stoß, Tiefpass 520 Hz), danach ein leises dunkles Glimmen. Das Spiel löst ihn je Schlag aus, schneller, je weniger Leben bleibt, und Bildrand und Lebensleiste pulsieren im selben Takt. CLAP (vier Schläge im langsamsten Takt): „slow heartbeat“ 0,68/0,65 + „muffled heartbeat“ 0,25/0,20, „bass synth note“ 0,02/0,07. Erste Fassungen mit längerem Sinuston hörte CLAP als „bass synth note“ (bis 0,62).

| Datei | Quelle | Messung |
| --- | --- | --- |
| `Audio/Sfx/soul_throb_1.wav` | soul-throb Seed 3 | 0.62 s, 1 Kanal/Kanäle, -23.0 LUFS, Spitze -4.7 dBFS |
| `Audio/Sfx/soul_throb_2.wav` | soul-throb Seed 4 | 0.62 s, 1 Kanal/Kanäle, -23.0 LUFS, Spitze -1.7 dBFS |

## Wucht der Sensenhiebe (Durchgang 3, 06.10.2026)

Owner: Der Klang beim Schlagen muss mächtiger werden. Rezepte `scythe-weight-1/2/3` in `tools/audio/recipes/combat.py` liegen unter den Ludo-Schwüngen: schwerer Luftstoß mit aufwärts gleitendem Band, am lautesten zur Kontaktzeit (0,062/0,085/0,155 s nach Hiebbeginn, gemessen 0,067/0,088/0,167 s), Auflodern der Death Flame zu Beginn des Durchziehens, kurzer dunkler Klingenklang; beim dritten Hieb ein tiefer Druckstoß. CLAP „heavy sword swing whoosh“ 0,99.

| Datei | Quelle | Messung |
| --- | --- | --- |
| `Audio/Sfx/scythe_weight_1_1.wav` | scythe-weight-1 Seed 1 | 0.42 s, 1 Kanal/Kanäle, -19.8 LUFS, Spitze -2.0 dBFS |
| `Audio/Sfx/scythe_weight_1_2.wav` | scythe-weight-1 Seed 2 | 0.42 s, 1 Kanal/Kanäle, -18.0 LUFS, Spitze -2.3 dBFS |
| `Audio/Sfx/scythe_weight_2_1.wav` | scythe-weight-2 Seed 1 | 0.48 s, 1 Kanal/Kanäle, -18.9 LUFS, Spitze -2.0 dBFS |
| `Audio/Sfx/scythe_weight_2_2.wav` | scythe-weight-2 Seed 2 | 0.48 s, 1 Kanal/Kanäle, -19.6 LUFS, Spitze -2.0 dBFS |
| `Audio/Sfx/scythe_weight_3_1.wav` | scythe-weight-3 Seed 1 | 0.70 s, 1 Kanal/Kanäle, -19.3 LUFS, Spitze -2.0 dBFS |
| `Audio/Sfx/scythe_weight_3_2.wav` | scythe-weight-3 Seed 2 | 0.70 s, 1 Kanal/Kanäle, -19.9 LUFS, Spitze -2.0 dBFS |
