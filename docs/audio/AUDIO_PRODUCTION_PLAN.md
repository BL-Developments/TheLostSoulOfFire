# The Lost Soul of Fire — Final Audio Package

**Status:** the approved pre-ElevenLabs authored bank was restored on 2026-08-30 after the replacement pass failed subjective quality review. On 2026-10-06 the bank was extended (not replaced) with zone ambiences, zone music and the missing cues, authored locally (see "Zones and authored extension" below).

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


## Zones and authored extension (2026-10-06)

The approved Ludo bank stays authoritative for every cue it covers; nothing of it was replaced.
What the game had no sound for was added, authored locally from recipes (`tools/audio`, see
`Content/Audio/SOURCES.md` for recipe, seed and levels of every file):

- **Zones.** `AudioDirector.SetZone` follows what is on screen: Title, Shore, Harbour,
  Causeway, Crossing (the skiff), Threshold, Hub, Arena. Each zone has its own ambience bed and
  music; beds crossfade over 1.6 s, music fades out (1.4 s), switches and fades in (2.4 s).
  The arena keeps its Ludo bed and loop and its calm/combat mix.
- **Music.** Six beds share the arena music's key (G-sharp minor) and one motif, a falling
  D#–C#–B–G# that stops on A#; the threshold lets it resolve to B. Bells, plucked strings,
  slow string and choir pads, low drones, a skin drum on the causeway and the crossing; no
  exposed synthetic lead. Exploration is quiet (0.26–0.34), fights fuller (0.4–0.46).
- **Cues.** Footsteps on stone and on the skiff's planks (four takes each, chosen at random,
  every 90 units of real movement, never during a dash), menu move/back/open/close, chest,
  currency, one sound per ability, the hub door awakening.
- **Mix review.** Effective levels (file loudness × in-game volume) were compared: a landed
  scythe hit now sits above the swing that carried it, taking damage is clearly above the
  player's own swings, and the Hollow's grab (a danger signal) above the room.
- **Listening.** Candidates were judged by loudness, spectrogram, loop seam and a LAION-CLAP
  ranking against text descriptions (`tools/audio/listen.py`); a final listening pass in the
  game by a person is still open.

## Mixprüfung ohne Gehör (ab 06.10.2026)

`tools/audio/mix_report.py --zone arena|hub|shore|crossing` liest jede Abspielstelle mit ihrer
Lautstärke aus dem Code, misst die lauteste Momentan-Lautheit (400 ms, K-gewichtet) jedes Assets
und stellt sie dem Bett der Zone gegenüber (Atmosphäre und Musik mit ihren Spielpegeln;
Menütöne gegen das pausierte Bett). Bänder in LU über dem Bett: Gefahr +6 … +20, Treffer
+4 … +18, Aktionen 0 … +14, Schritte −14 … +2, Menü −12 … +4, Seelensinn −12 … +6, Ereignisse
+2 … +18. Stand 06.10.: alle vier Zonen ohne Ausreißer (Überfahrt-Bett um 2,6 dB gesenkt, damit
Gefahrensignale auf dem Deck nicht untergehen; Prolog-Kampfmusik etwas voller; Warn- und
Schritt-Cues nachgezogen). Maskierung nach Frequenz und die Dynamik eines echten Kampfs bildet
der Bericht nicht ab; die Hörabnahme im Spiel bleibt offen.

## Kampfschichten und Vorrang (06.10.2026, abends)

Die Ludo-Cues bleiben der Kern jeder Aktion. Darunter liegen lokal gebaute Schichten
(`tools/audio/recipes/combat.py`, Quellen in `Content/Audio/SOURCES.md`): ein Kontakt-Transient
auf Sample 0 mit dem Material des Ziels (Hollow: Stoff, hohler Körper, Porzellanknack; Burning:
Krustenbruch, Glutknistern, Zischen; Devourer: Fleisch, Nachgeben, leeres Grollen;
Übungspuppe: Holz und Stroh), ein Druckstoß unter Seelenspaltung und voller Kanone und ein
Körpertreffer unter dem Spieler-Schmerzlaut. Neu sind Ausholwarnungen für Hollow und Devourer, das Fauchen des Burning
beim Losstürmen und ein eigener Tod je Gegner zusätzlich zum gemeinsamen Todeslaut. Je Gruppe 2–3
Takes, auf ±0,5 LU angeglichen, dazu die Tonhöhenstreuung der Cue-Regeln.

`AudioDirector` gibt Gefahrensignalen (Ausholen, Anlauf, Spielertreffer, Griff, Schlag) Vorrang:
Schwünge, Schritte und Ladegeräusche, die während der folgenden 0,4 s starten, kommen bis zu 40 %
leiser. Laufen schon mehr als sechs Effekte, kommt jeder weitere nicht gefährliche leiser hinzu,
damit ein voller Kampf nicht zu einer gleichmäßig lauten Wand wird. `mix_report.py` kennt die
Klasse „layer“ (−6 … +12 LU über dem Bett) für Schichten unter einem anderen Cue.
