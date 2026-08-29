# The Lost Soul of Fire — Audio Production Plan

## Diagnosis

The current sounds are deliberate placeholders, not operating-system sounds. `AudioDirector` synthesizes short mono PCM tones and noise in memory at 22.05 kHz. No `.wav`, `.ogg`, `.mp3`, `.flac`, or `.aiff` files were present in the repository, so the game could not sound authored or cinematic.

The gameplay event wiring is already useful: scythe attacks and hits, dash, cannon charge/full/fire, Burning charge/detonation, core hit, soul release, Resonance ready/activate, player hit/death, Soul Sense, and wave start all emit named cues.

`AudioDirector` now prefers real content-pipeline assets at the paths below and automatically retains the synthesized sounds when an asset is absent. This keeps CI, incomplete checkouts, and the current game working while audio is produced incrementally.

## Recommended sourcing approach

Use MCP or an audio-generation skill only during development. Do not make the released game call a sound service. Every approved asset should be downloaded, normalized, committed under `Content/Audio`, and loaded locally through MonoGame.

### Free, release-safe baseline: Freesound CC0

Use the community [Freesound MCP server](https://github.com/johnkimdw/freesound-mcp-server) to search and preview. It is small and unofficial, so inspect/pin the source before running it. Freesound itself provides the supported API and license metadata. Search only for `Creative Commons 0`, verify the license on the individual sound page, and record the source URL and sound ID even though CC0 does not require attribution.

The server currently focuses on search, metadata, and preview URLs. Original-quality Freesound downloads require OAuth2; previews are available without OAuth2. It is acceptable to use the MCP for discovery and download the approved original manually from the sound page.

### Fast custom production: ElevenLabs skills

ElevenLabs maintains official `sound-effects` and `music` agent skills at [elevenlabs/skills](https://github.com/elevenlabs/skills). This is a better match than its hosted MCP, which is presently described primarily as an agent-management connector. The free plan is useful for private prototyping; current pricing reserves commercial-use licensing for Starter or higher, so do not ship free-plan output in a commercial release without re-checking the terms.

Install the official skills with:

```sh
npx skills add elevenlabs/skills
export ELEVENLABS_API_KEY="your-key"
```

### Why not a live audio MCP in the game?

- It would add network, credentials, latency, quota, and service availability to a local action game.
- Generated output can change between calls, which prevents a stable mix and repeatable tests.
- Shipping local assets makes licensing and attribution auditable.
- MonoGame already has the right runtime path: `SoundEffect` for short `.wav` cues and `Song`/`MediaPlayer` for compressed music.

## Optional Freesound MCP setup in Codex

Prerequisites: Python 3.10+, `uv`, a Freesound account, and a Freesound API key from <https://freesound.org/apiv2/apply>.

1. Clone and inspect `https://github.com/johnkimdw/freesound-mcp-server` into a tools directory outside the game source tree.
2. Run `uv sync` in that checkout.
3. Put `FREESOUND_API_KEY` in the environment that launches Codex. Do not commit the key.
4. In ChatGPT desktop, open **Settings → MCP servers → Add server**, choose **STDIO**, and configure:
   - Command: the absolute path to `uv`
   - Arguments: `--directory`, the absolute server checkout, `run`, `freesound-mcp`
   - Forwarded environment variable: `FREESOUND_API_KEY`
5. Save, restart the local Codex host, and use `/mcp` to verify the server.

Equivalent project-scoped `.codex/config.toml` for a trusted project:

```toml
[mcp_servers.freesound]
command = "/absolute/path/to/uv"
args = ["--directory", "/absolute/path/to/freesound-mcp-server", "run", "freesound-mcp"]
env_vars = ["FREESOUND_API_KEY"]
startup_timeout_sec = 20
tool_timeout_sec = 60
default_tools_approval_mode = "prompt"
```

Codex officially supports both local STDIO and remote Streamable HTTP MCP servers, and the desktop app, CLI, and IDE extension share host configuration.

## Asset contract

Create these exact files. Short effects should be 16-bit PCM `.wav`, preferably mono at 48 kHz. Music should be a stereo `.ogg` loop. Trim leading silence, add 3–10 ms fades where necessary, avoid clipping, and preserve transient attack.

| Runtime asset | Target | Production prompt / search brief |
|---|---:|---|
| `Audio/Sfx/scythe_swing_1.wav` | 0.18 s | Tight curved steel scythe air slice, dry sharp swish, dark fantasy, no impact, no music |
| `Audio/Sfx/scythe_swing_2.wav` | 0.24 s | Wider heavier scythe swoosh, low-mid body, fast anime combat motion, no impact |
| `Audio/Sfx/soul_cleave.wav` | 0.45 s | Massive spectral scythe arc, deep whoom into brittle soul crack, controlled short tail |
| `Audio/Sfx/scythe_hit.wav` | 0.15 s | Scythe striking ash-covered bone and armor, compact crunchy impact, no sword ring |
| `Audio/Sfx/dash.wav` | 0.25 s | Short deep flame whump plus fast directional whoosh, ignition dash, not a jet engine |
| `Audio/Sfx/cannon_charge.wav` | 0.8 s | Loopable supernatural-mechanical charge, low hum rising into soul-energy vibration |
| `Audio/Sfx/cannon_full.wav` | 0.22 s | Unmistakable full-charge lock cue, bright soul ping plus compact core pulse |
| `Audio/Sfx/cannon_fire.wav` | 0.55 s | Heavy soul cannon discharge, strong transient, sub weight, occult metallic character, short reverb |
| `Audio/Sfx/burning_charge.wav` | 0.55 s | Unstable enemy flame escalating rapidly, crackle and pressure rise, threatening telegraph |
| `Audio/Sfx/burning_detonation.wav` | 0.7 s | Dense magical fire detonation, bass pressure, ash crack, aggressive but not cinematic-boomy |
| `Audio/Sfx/core_hit.wav` | 0.18 s | Bright crystalline soul-core crack, high readability, rewarding weak-point hit |
| `Audio/Sfx/soul_release.wav` | 0.9 s | Gentle ascending soul tone, soft bell release, quiet residue whoosh, relief after combat |
| `Audio/Sfx/resonance_ready.wav` | 0.35 s | Single deep heartbeat/core pulse with a subtle bright soul overtone, clear UI feedback |
| `Audio/Sfx/resonance_activate.wav` | 0.8 s | Heartbeat, very brief vacuum, deep impact, supernatural flame eruption, powerful but clean |
| `Audio/Sfx/player_hit.wav` | 0.2 s | Muted armored body hit with ember crack, no voice, concise gameplay feedback |
| `Audio/Sfx/player_death.wav` | 1.2 s | Death Flame collapsing and extinguishing, low implosion, ash fall, mournful short tail, no voice |
| `Audio/Sfx/soul_sense_on.wav` | 0.45 s | Reverse whoosh into a pure high soul tone, perception shift, restrained |
| `Audio/Sfx/soul_sense_off.wav` | 0.3 s | Short inverse transition, high tone folding downward into normal space |
| `Audio/Sfx/wave_start.wav` | 0.65 s | Distant furnace gate impact and low ritual bell, combat wave announcement |
| `Audio/Ambience/arena_ambience.wav` | 20–30 s loop | Seamless ruined furnace arena ambience, deep rumble, sparse distant chain, wind, extremely faint whispers; lots of silence |
| `Audio/Music/arena_loop.ogg` | 90–150 s loop | Seamless dark Gothic-industrial combat underscore, 72 BPM, D minor, low strings, distant choir texture without words, restrained percussion, furnace pulse, no melody resembling existing music, leave room for combat SFX |

For repeated high-frequency actions, make three closely matched variations during production and select the best one for the first pass. A later pass can extend the bank to randomize variants without changing gameplay events.

## Integration checklist

1. Put approved files in `src/TheLostSoulOfFire/Content/Audio/Sfx`, `Audio/Ambience`, and `Audio/Music`.
2. Add every file to `Content.mgcb`. Use the `SoundEffect` processor for `.wav` one-shots/ambience and the `Song` processor for the `.ogg` music loop.
3. Keep names exactly as listed; `AudioDirector` loads asset names without extensions.
4. Create `Content/Audio/SOURCES.md` with filename, source URL or generation service, creator, license, download date, and edits performed.
5. Build with `dotnet build TheLostSoulOfFire.sln` and confirm zero missing-content errors.
6. Play all four waves. Check charge cues without looking at the HUD, cue/animation synchronization, no clipping during dense combat, and seamless loops.
7. Compare with music and ambience muted separately. Combat-critical cues must remain readable over both.
8. Keep the generated fallback path until every supported build packages the audio correctly.

## Follow-up cue coverage

The existing implementation does not yet emit dedicated audio for Hollow swipe, Devourer slam/devour, generic enemy death, cannon non-core impact, title confirm, wave clear, or ending reveal. Add those only after the first bank is mixed; otherwise they will create more placeholder noise. The second pass should add named cues at state transitions, apply per-cue cooldowns, cap simultaneous enemy voices, randomize pitch by at most about ±3%, and duck ambience/music briefly for Resonance and Soul Release.

## Copy/paste production prompt

```text
Produce and integrate the complete audio pass for The Lost Soul of Fire in this repository.

Read docs/mvp/16_AUDIO_DIRECTION.md and docs/audio/AUDIO_PRODUCTION_PLAN.md first. Preserve all current gameplay, timing, controls, VFX, and fallback behavior. Treat MCP/services as authoring tools only; the game must ship and run with local assets and no network dependency.

Source or generate every asset in the Asset contract. Prefer Freesound samples explicitly marked Creative Commons 0; verify each item on its original page and record its URL, author, sound ID, license, and download date in Content/Audio/SOURCES.md. Do not use CC-BY-NC, unknown-license, ripped, trademarked, voice-clone, or recognizable copyrighted material. If an official ElevenLabs sound-effects/music skill is available, it may be used for custom assets only when the account license permits this project’s intended release; record the service, model, prompt, date, and plan/license status.

Audition multiple candidates. Layer and edit when needed so the bank shares one identity: Gothic atmosphere, industrial weight, supernatural soul energy, anime-readable combat impact, and deliberate silence. Avoid generic arcade bleeps, excessive reverb, constant drones, and frequency masking. Export short cues and ambience as 48 kHz 16-bit PCM WAV and music as a seamless stereo OGG. Trim silence, use short anti-click fades, peak below -1 dBFS, and keep loudness consistent by category.

Place files at the exact paths in the plan and add them to Content.mgcb with the appropriate MonoGame processors. Build and run the game. Verify every existing AudioCue, full-charge readability without the HUD, cue-to-animation sync, dense-combat headroom, loop seams, death/restart, and arena completion. Keep the synthesized fallback for any missing or unloadable asset. Do not commit secrets, raw bulk libraries, rejected candidates, or service caches.

After the first bank passes, add dedicated cues for Hollow swipe, Devourer slam/devour, generic enemy death, cannon impact, title confirm, wave clear, and ending reveal. Trigger them only on state transitions, add sensible cooldown/polyphony limits, and preserve current game behavior. Finish with a concise report listing sources/licenses, files added, cue coverage, build/test results, and any remaining subjective mix decisions.
```

## Reference links

- [MonoGame sound effects and music](https://docs.monogame.net/articles/tutorials/building_2d_games/14_soundeffects_and_music/index.html)
- [Freesound API overview](https://freesound.org/docs/api/overview.html)
- [Freesound API search fields and license filter](https://freesound.org/docs/api/resources_apiv2.html?highlight=license)
- [Creative Commons CC0](https://creativecommons.org/publicdomain/zero/1.0/)
- [Codex MCP configuration](https://developers.openai.com/codex/mcp)
- [ElevenLabs agent tooling](https://elevenlabs.io/docs/eleven-api/resources/agent-tooling)
- [ElevenLabs sound-effects API](https://elevenlabs.io/docs/api-reference/text-to-sound-effects/convert)
