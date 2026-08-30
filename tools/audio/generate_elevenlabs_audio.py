#!/usr/bin/env python3
"""Generate and master the final Soulfire SFX bank with ElevenLabs.

The script reads ELEVENLABS_API_KEY only from the process environment, makes
one request per cue, and never writes credentials or service caches into the
repository. Raw authoring takes default to /tmp; only mastered 48 kHz PCM WAV
files are written under Content/Audio.
"""

from __future__ import annotations

import argparse
import json
import math
import os
import urllib.error
import urllib.parse
import urllib.request
import wave
from array import array
from dataclasses import dataclass
from pathlib import Path


RATE = 48_000
MODEL = "eleven_text_to_sound_v2"
API_URL = "https://api.elevenlabs.io/v1/sound-generation"
COMMON = (
    " Original isolated sound design for a gothic-industrial supernatural action game."
    " No music, no melody, no voice, no arcade bleep, no sci-fi laser, no trailer braam."
)


@dataclass(frozen=True)
class Cue:
    filename: str
    tier: str
    duration: float
    source_duration: float
    peak_db: float
    prompt: str
    influence: float = 0.72
    loop: bool = False
    stereo: bool = False
    trim: bool = True
    reverse_source: bool = False
    rising_profile: bool = False
    resonance_edit: bool = False


CUES = (
    Cue("scythe_swing_1.wav", "must", 0.18, 0.5, -5.0,
        "Fast thin curved-steel scythe air slice in the first 80 milliseconds, dry sharp transient, restrained low end, tiny dark cloth wake, fully finished by 180 milliseconds, no impact"),
    Cue("scythe_swing_2.wav", "must", 0.24, 0.5, -4.2,
        "Wider second curved-steel scythe swing in the first 80 milliseconds, heavier air displacement, restrained metal edge, faint ghost-harmonic wake, fully finished by 240 milliseconds, no impact"),
    Cue("soul_cleave.wav", "must", 0.45, 0.6, -2.6,
        "Major scythe finisher: immediate violent broad air cut, dark spectral electrical tearing that is supernatural rather than technological, brittle white-violet soul crack, compact controlled tail, no generic explosion"),
    Cue("scythe_hit.wav", "must", 0.15, 0.5, -4.0,
        "Immediate compact scythe contact against ash-covered bone and battered iron, sharp dry transient, crunchy fracture and restrained metal weight, finished within 150 milliseconds, no sword ring"),
    Cue("dash.wav", "must", 0.25, 0.5, -4.0,
        "Immediate short deep Death-Flame ignition whump fused with a fast directional air whoosh, forceful but compact, finished within 250 milliseconds, not a jet engine"),
    Cue("cannon_charge.wav", "must", 0.80, 0.8, -6.0,
        "Soul Cannon charge begins immediately: low occult metal vibration gathering pressure, steadily rising glassy ghost harmonics and compressed supernatural energy, directed and mechanical, ends tense without firing",
        trim=False, reverse_source=True, rising_profile=True),
    Cue("cannon_full.wav", "must", 0.22, 0.5, -3.5,
        "Unmistakable full-charge lock in the first 60 milliseconds: one battered-metal latch clack, compact deep core pulse, bright spectral glass overtone, decisive and complete within 220 milliseconds"),
    Cue("cannon_fire.wav", "must", 0.55, 0.6, -1.8,
        "Violent directed Soul Cannon discharge: immediate compressed physical transient, occult metal chamber weight, focused spectral energy ripping forward, restrained sub pressure, very short ruined-stone reflection, no gunshot realism"),
    Cue("cannon_impact.wav", "must", 0.32, 0.5, -3.0,
        "Focused supernatural cannon projectile strikes ash armor and ruined stone immediately, compact pressure slap, brittle spectral debris and dull metal body, clearly an impact rather than another firing sound"),
    Cue("burning_charge.wav", "must", 0.55, 0.6, -5.0,
        "Unstable contained Death-Flame pressure rises immediately, nervous ember crackle tightening into dangerous compressed supernatural vibration, increasingly erratic, stops before rupture, clearly unlike a directed cannon",
        trim=False, reverse_source=True, rising_profile=True),
    Cue("burning_detonation.wav", "must", 0.70, 0.7, -2.0,
        "Immediate violent failure of contained supernatural flame: inward pressure snap then jagged rupture, tearing ash, unstable spectral fire and compact low weight, chaotic rather than directed, no giant cinematic boom"),
    Cue("core_hit.wav", "must", 0.18, 0.5, -3.0,
        "Immediate bright Soul Core weak-point hit, physical strike fused with a sharp white-violet crystalline crack and tiny ghost harmonic, extremely readable, complete within 180 milliseconds"),
    Cue("soul_release.wav", "must", 0.90, 0.9, -7.0,
        "A trapped soul gently released: soft breath-like weight leaving, delicate upward spectral shimmer, one small clean glass harmonic resolving with melancholy relief, restrained light tail; absolutely not an explosion or impact", trim=False),
    Cue("resonance_ready.wav", "must", 0.35, 0.5, -5.0,
        "Single deep humanless Soul Core heartbeat pulse immediately, dense internal pressure with a subtle bright supernatural overtone, unmistakable readiness feedback, complete within 350 milliseconds"),
    Cue("resonance_activate.wav", "must", 0.80, 0.8, -1.5,
        "Signature Resonance activation: very brief breath-vacuum anticipation, dense Soul pressure, then violent supernatural Death-Flame ignition around 100 milliseconds, spectral tearing and dark physical force, transformation not explosion",
        trim=False, resonance_edit=True),
    Cue("player_hit.wav", "must", 0.20, 0.5, -4.5,
        "Immediate concise player damage cue: muted body and coat impact, small battered-metal armor tick, one dry ember crack, clear but not obnoxious, no grunt, complete within 200 milliseconds"),
    Cue("player_death.wav", "must", 1.20, 1.2, -4.0,
        "The life Flame extinguishes: low supernatural collapse inward, brief air withdrawal, dying ember crackles and soft ash falling into a mournful empty tail, restrained and final, no scream and no horror sting", trim=False),
    Cue("soul_sense_on.wav", "must", 0.45, 0.5, -7.0,
        "Subtle perception opening: immediate breath-like vacuum pulls the ordinary world away, reverse spectral air blooms into a fine high ghost harmonic, restrained transition, no sustained tone",
        trim=False, reverse_source=True),
    Cue("soul_sense_off.wav", "must", 0.30, 0.5, -8.0,
        "Small return-to-world transition immediately: fine spectral harmonic folds downward, a soft breath of physical air returns, gentle and complete within 300 milliseconds"),
    Cue("wave_start.wav", "must", 0.65, 0.7, -6.0,
        "Distant furnace gate drops with industrial weight, followed by one low weathered ritual-bell resonance in a ruined stone arena, restrained wave announcement with negative space"),
    Cue("hollow_swipe.wav", "should", 0.32, 0.5, -5.5,
        "Gaunt Hollow enemy makes a dry crooked arm swipe immediately, rough cloth, bone creak and thin displaced air, threatening but lighter than the player scythe, no vocalization"),
    Cue("devourer_slam.wav", "should", 0.65, 0.7, -2.4,
        "Enormous ash creature slams both arms into a ruined furnace floor immediately, heavy stone impact, metal-foundation resonance and grit, powerful physical weight without a trailer boom, no roar"),
    Cue("devourer_devour.wav", "should", 0.85, 0.9, -4.0,
        "Disturbing supernatural Soul suction: deep pressure pulling inward through bone and wet ash, trapped ghost harmonics stretching toward a torso cavity then snapping shut, no voice or growl"),
    Cue("enemy_death.wav", "should", 0.55, 0.6, -6.0,
        "Ash enemy collapses immediately: brittle body fracture, extinguishing hostile ember and a short spectral residue withdrawing, compact and subordinate to player attacks, no vocal death cry"),
    Cue("wave_clear.wav", "should", 0.75, 0.8, -8.0,
        "Combat pressure falls away: soft ash settles, distant furnace resonance releases tension, one restrained natural glass-soul harmonic offers unresolved relief, sparse and non-musical", trim=False),
    Cue("ending_reveal.wav", "should", 1.60, 1.6, -7.0,
        "A tiny living Flame is revealed in darkness: intimate ember breath blooms into warm glass and aged bronze harmonics, quiet hope after grief, sparse non-musical sound design, no orchestral swell", trim=False),
    Cue("title_confirm.wav", "optional", 0.32, 0.5, -9.0,
        "Tiny tactile title confirmation immediately: one warm ember ignition and restrained dark-glass tap, confident and physical, complete within 320 milliseconds, not an electronic button"),
    Cue("arena_ambience.wav", "ambience", 20.0, 20.0, -14.0,
        "Seamless ruined gothic furnace arena ambience: quiet stone rumble, cold air through broken masonry, very sparse distant heavy chains, occasional far industrial metal resonance, tiny ember, extremely faint nonverbal spectral breath, generous deliberate silence, no rhythm and no music",
        influence=0.62, loop=True, stereo=True, trim=False),
)


def api_generate(cue: Cue, api_key: str) -> bytes:
    query = urllib.parse.urlencode({"output_format": "pcm_48000"})
    payload = json.dumps({
        "text": cue.prompt + COMMON,
        "duration_seconds": cue.source_duration,
        "prompt_influence": cue.influence,
        "model_id": MODEL,
        "loop": cue.loop,
    }).encode("utf-8")
    request = urllib.request.Request(
        f"{API_URL}?{query}",
        data=payload,
        headers={
            "xi-api-key": api_key,
            "Content-Type": "application/json",
            "Accept": "audio/pcm",
            "User-Agent": "TheLostSoulOfFire-audio-authoring/1.0",
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=180) as response:
            return response.read()
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"ElevenLabs rejected {cue.filename} with HTTP {exc.code}: {detail}") from exc


def decode_pcm(raw: bytes, expected_duration: float) -> list[float]:
    if len(raw) % 2:
        raise ValueError("ElevenLabs PCM response has an odd byte count")
    samples = array("h")
    samples.frombytes(raw)
    expected_frames = round(expected_duration * RATE)
    mono_difference = abs(len(samples) - expected_frames) / expected_frames
    stereo_difference = abs(len(samples) / 2 - expected_frames) / expected_frames
    if stereo_difference <= 0.12:
        return [
            (samples[index] + samples[index + 1]) / 65536.0
            for index in range(0, len(samples) - 1, 2)
        ]
    if mono_difference <= 0.12:
        return [sample / 32768.0 for sample in samples]
    raise ValueError(
        f"unexpected PCM sample count {len(samples)} for approximately {expected_frames} frames"
    )


def find_content_start(samples: list[float]) -> int:
    threshold = 10.0 ** (-42.0 / 20.0)
    window = max(1, round(0.002 * RATE))
    for index in range(0, max(1, len(samples) - window), window):
        peak = max(abs(sample) for sample in samples[index:index + window])
        if peak >= threshold:
            return max(0, index - round(0.004 * RATE))
    return 0


def edit_resonance(samples: list[float], duration: float) -> list[float]:
    """Move the generated ignition to the visual eruption at roughly 65 ms."""
    count = round(duration * RATE)
    anticipation_count = round(0.065 * RATE)
    eruption_start = min(len(samples), round(0.60 * RATE))
    eruption = samples[eruption_start:]
    tail_source = samples[round(0.15 * RATE):eruption_start]
    output = samples[:anticipation_count] + eruption + tail_source
    output = output[:count]

    # Make the first beat a pressure intake, then let the post-ignition body decay.
    for index in range(min(anticipation_count, len(output))):
        progress = index / max(1, anticipation_count - 1)
        output[index] *= 0.18 + progress * 0.34
    tail_start = min(len(output), anticipation_count + len(eruption))
    tail_length = max(1, len(output) - tail_start)
    for index in range(tail_start, len(output)):
        progress = (index - tail_start) / tail_length
        output[index] *= 0.75 * math.exp(-3.0 * progress) + 0.07
    return output


def master_mono(cue: Cue, samples: list[float]) -> list[float]:
    if cue.reverse_source:
        samples = list(reversed(samples))
    if cue.resonance_edit:
        samples = edit_resonance(samples, cue.duration)
    if cue.trim:
        samples = samples[find_content_start(samples):]
    count = round(cue.duration * RATE)
    samples = samples[:count]
    mean = sum(samples) / max(1, len(samples))
    samples = [sample - mean for sample in samples]
    samples = (samples + [0.0] * count)[:count]
    if cue.rising_profile:
        samples = [
            sample * (0.14 + 0.86 * (index / max(1, count - 1)) ** 1.25)
            for index, sample in enumerate(samples)
        ]

    fade_in = min(round((0.002 if cue.trim else 0.008) * RATE), max(1, count // 10))
    fade_out = min(round((0.008 if cue.duration < 0.5 else 0.018) * RATE), max(1, count // 8))
    for index in range(fade_in):
        samples[index] *= index / fade_in
    for index in range(fade_out):
        samples[-1 - index] *= index / fade_out

    peak = max((abs(sample) for sample in samples), default=1.0)
    target = 10.0 ** (cue.peak_db / 20.0)
    gain = target / max(peak, 1e-9)
    return [max(-1.0, min(1.0, sample * gain)) for sample in samples]


def stereo_loop(mono: list[float]) -> list[list[float]]:
    count = len(mono)
    shift = round(0.113 * RATE)
    shifted = mono[-shift:] + mono[:-shift]
    left = [sample * 0.96 for sample in mono]
    right = [mono[index] * 0.78 + shifted[index] * 0.18 for index in range(count)]

    # Preserve the generated loop while guaranteeing sample-continuous endpoints.
    seam_fade = round(0.06 * RATE)
    for channel in (left, right):
        first = channel[0]
        for offset in range(seam_fade):
            index = count - seam_fade + offset
            amount = offset / max(1, seam_fade - 1)
            channel[index] = channel[index] * (1.0 - amount) + first * amount
        channel[-1] = channel[0]
    return [left, right]


def write_wav(path: Path, channels: list[list[float]]) -> None:
    if not channels or any(len(channel) != len(channels[0]) for channel in channels):
        raise ValueError(f"invalid channel data for {path}")
    path.parent.mkdir(parents=True, exist_ok=True)
    frames = array("h")
    for index in range(len(channels[0])):
        for channel in channels:
            frames.append(round(max(-1.0, min(1.0, channel[index])) * 32767.0))
    with wave.open(str(path), "wb") as output:
        output.setnchannels(len(channels))
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(frames.tobytes())


def write_source_wav(path: Path, samples: list[float]) -> None:
    write_wav(path, [samples])


def cue_output_path(content_root: Path, cue: Cue) -> Path:
    category = "Ambience" if cue.tier == "ambience" else "Sfx"
    return content_root / "Audio" / category / cue.filename


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--tier", choices=("must", "should", "optional", "ambience", "all"), default="all")
    parser.add_argument("--only", help="generate one exact filename")
    parser.add_argument("--force", action="store_true", help="replace an existing raw take")
    parser.add_argument(
        "--content-root", type=Path,
        default=Path(__file__).resolve().parents[2] / "src" / "TheLostSoulOfFire" / "Content",
    )
    parser.add_argument(
        "--source-root", type=Path,
        default=Path("/tmp/soulfire-elevenlabs-sources"),
    )
    args = parser.parse_args()
    selected = [cue for cue in CUES if args.tier == "all" or cue.tier == args.tier]
    if args.only:
        selected = [cue for cue in CUES if cue.filename == args.only]
    if not selected:
        raise SystemExit("no matching cues")

    args.source_root.mkdir(parents=True, exist_ok=True)
    for index, cue in enumerate(selected, start=1):
        source_path = args.source_root / cue.filename
        if source_path.exists() and not args.force:
            with wave.open(str(source_path), "rb") as source:
                raw = source.readframes(source.getnframes())
            samples = decode_pcm(raw, cue.source_duration)
            action = "reused"
        else:
            api_key = os.environ.get("ELEVENLABS_API_KEY")
            if not api_key:
                raise SystemExit("ELEVENLABS_API_KEY is not available in the environment")
            print(f"GENERATE {index}/{len(selected)} {cue.filename} tier={cue.tier}", flush=True)
            raw = api_generate(cue, api_key)
            samples = decode_pcm(raw, cue.source_duration)
            write_source_wav(source_path, samples)
            action = "generated"

        mastered = master_mono(cue, samples)
        channels = stereo_loop(mastered) if cue.stereo else [mastered]
        output_path = cue_output_path(args.content_root, cue)
        write_wav(output_path, channels)
        print(f"MASTER {cue.filename} {action} -> {output_path}", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
