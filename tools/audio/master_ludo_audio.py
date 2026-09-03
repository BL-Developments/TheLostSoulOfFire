#!/usr/bin/env python3
"""Master approved Ludo audio generations into the shipped local audio bank.

The input directory is an authoring workspace, not a runtime dependency. Inputs
must be decoded 48 kHz, 16-bit PCM WAV files with the names used in SOURCES.md.
Only the mastered WAV/OGG files are written into Content/Audio.
"""

from __future__ import annotations

import argparse
import math
import shutil
import subprocess
import wave
from array import array
from pathlib import Path


RATE = 48_000


def read_wav(path: Path) -> list[list[float]]:
    with wave.open(str(path), "rb") as source:
        if source.getframerate() != RATE or source.getsampwidth() != 2:
            raise ValueError(f"{path}: expected 48 kHz, 16-bit PCM")
        channels = source.getnchannels()
        samples = array("h")
        samples.frombytes(source.readframes(source.getnframes()))
    return [
        [samples[index] / 32768.0 for index in range(channel, len(samples), channels)]
        for channel in range(channels)
    ]


def segment(signal: list[float], start: float, duration: float) -> list[float]:
    first = max(0, round(start * RATE))
    count = round(duration * RATE)
    return (signal[first : first + count] + [0.0] * count)[:count]


def reverse(signal: list[float]) -> list[float]:
    return list(reversed(signal))


def low_pass(signal: list[float], cutoff: float) -> list[float]:
    alpha = 1.0 - math.exp(-math.tau * cutoff / RATE)
    value = 0.0
    output: list[float] = []
    for sample in signal:
        value += alpha * (sample - value)
        output.append(value)
    return output


def mix(duration: float, *layers: tuple[list[float], float, float]) -> list[float]:
    output = [0.0] * round(duration * RATE)
    for signal, gain, offset_seconds in layers:
        offset = round(offset_seconds * RATE)
        for index, sample in enumerate(signal):
            destination = offset + index
            if 0 <= destination < len(output):
                output[destination] += sample * gain
    return output


def master(signal: list[float], duration: float, peak_db: float) -> list[float]:
    count = round(duration * RATE)
    output = (signal + [0.0] * count)[:count]
    if not output:
        return output

    mean = sum(output) / len(output)
    output = [sample - mean for sample in output]
    fade = min(round(0.006 * RATE), max(1, len(output) // 8))
    for index in range(fade):
        gain = index / fade
        output[index] *= gain
        output[-1 - index] *= gain

    peak = max((abs(sample) for sample in output), default=1.0)
    target = 10.0 ** (peak_db / 20.0)
    gain = target / max(peak, 1e-9)
    return [max(-1.0, min(1.0, sample * gain)) for sample in output]


def write_wav(path: Path, channels: list[list[float]]) -> None:
    if not channels or any(len(channel) != len(channels[0]) for channel in channels):
        raise ValueError(f"{path}: invalid channel data")
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


def render_sfx(source_root: Path, output_root: Path) -> None:
    sources = {
        name: read_wav(source_root / f"{name}.wav")[0]
        for name in (
            "scythe", "soul_magic", "impact", "dash", "cannon_charge",
            "cannon_full", "cannon_fire", "burning_charge", "fire",
            "core_hit", "soul_release", "death_flame", "devourer_slam",
            "devourer_devour", "wave_start", "title_confirm", "wave_clear",
            "ending_reveal",
        )
    }

    slam_pulse = low_pass(segment(sources["devourer_slam"], 0.0, 0.35), 260.0)
    resonance = mix(
        0.8,
        (segment(sources["devourer_slam"], 0.0, 0.32), 0.72, 0.0),
        (segment(sources["soul_magic"], 0.55, 0.58), 0.55, 0.20),
    )

    cues: dict[str, tuple[list[float], float, float]] = {
        "scythe_swing_1": (segment(sources["scythe"], 0.00, 0.18), 0.18, -3.0),
        "scythe_swing_2": (segment(sources["scythe"], 0.09, 0.24), 0.24, -3.0),
        "soul_cleave": (segment(sources["soul_magic"], 0.55, 0.45), 0.45, -2.4),
        "scythe_hit": (segment(sources["impact"], 0.14, 0.15), 0.15, -2.6),
        "dash": (segment(sources["dash"], 0.51, 0.25), 0.25, -3.0),
        "cannon_charge": (segment(sources["cannon_charge"], 0.79, 0.80), 0.80, -4.0),
        "cannon_full": (segment(sources["cannon_full"], 0.00, 0.22), 0.22, -2.4),
        "cannon_fire": (segment(sources["cannon_fire"], 0.00, 0.55), 0.55, -2.0),
        "burning_charge": (segment(reverse(sources["burning_charge"]), 0.04, 0.55), 0.55, -4.0),
        "burning_detonation": (segment(sources["fire"], 0.00, 0.70), 0.70, -2.2),
        "core_hit": (segment(sources["core_hit"], 0.00, 0.18), 0.18, -2.3),
        "soul_release": (segment(sources["soul_release"], 0.00, 0.90), 0.90, -4.0),
        "resonance_ready": (slam_pulse, 0.35, -4.0),
        "resonance_activate": (resonance, 0.80, -1.8),
        "player_hit": (low_pass(segment(sources["impact"], 0.13, 0.20), 2600.0), 0.20, -3.0),
        "player_death": (segment(sources["death_flame"], 0.08, 1.20), 1.20, -2.8),
        "soul_sense_on": (reverse(segment(sources["soul_magic"], 0.20, 0.45)), 0.45, -4.5),
        "soul_sense_off": (segment(sources["soul_magic"], 1.08, 0.30), 0.30, -4.5),
        "wave_start": (segment(sources["wave_start"], 0.22, 0.65), 0.65, -4.0),
        "hollow_swipe": (segment(sources["scythe"], 0.08, 0.32), 0.32, -4.0),
        "devourer_slam": (segment(sources["devourer_slam"], 0.00, 0.65), 0.65, -1.8),
        "devourer_devour": (segment(sources["devourer_devour"], 0.16, 0.85), 0.85, -2.8),
        "enemy_death": (segment(sources["death_flame"], 0.12, 0.55), 0.55, -4.0),
        "cannon_impact": (segment(sources["cannon_fire"], 0.02, 0.32), 0.32, -3.2),
        "title_confirm": (segment(sources["title_confirm"], 1.20, 0.32), 0.32, -5.0),
        "wave_clear": (segment(sources["wave_clear"], 1.68, 0.75), 0.75, -5.0),
        "ending_reveal": (segment(sources["ending_reveal"], 0.08, 1.60), 1.60, -4.5),
    }

    for name, (signal, duration, peak_db) in cues.items():
        write_wav(output_root / f"{name}.wav", [master(signal, duration, peak_db)])


def render_ambience(source_root: Path, output_path: Path) -> None:
    channels = read_wav(source_root / "arena_ambience.wav")
    mastered = [master(channel, 20.0, -11.0) for channel in channels]
    # Both endpoints are zeroed by master(), preventing a click at the wrap.
    write_wav(output_path, mastered)


def render_music(source_root: Path, output_path: Path) -> None:
    source_path = source_root / "arena_music.wav"
    with wave.open(str(source_path), "rb") as source:
        if source.getnchannels() != 2 or source.getsampwidth() != 2 or source.getframerate() != RATE:
            raise ValueError(f"{source_path}: expected 48 kHz stereo 16-bit PCM")
        input_frames = source.getnframes()
        samples = array("h")
        samples.frombytes(source.readframes(input_frames))

    output_frames = 100 * RATE
    peak = max((abs(sample) for sample in samples), default=1)
    scale = (10.0 ** (-6.0 / 20.0)) * 32767.0 / peak
    fade = round(0.02 * RATE)
    rendered = array("h")
    for frame in range(output_frames):
        position = frame * (input_frames - 1) / (output_frames - 1)
        left = int(position)
        fraction = position - left
        right = min(left + 1, input_frames - 1)
        edge = min(1.0, frame / fade, (output_frames - 1 - frame) / fade)
        for channel in range(2):
            first = samples[left * 2 + channel]
            second = samples[right * 2 + channel]
            value = (first + (second - first) * fraction) * scale * edge
            rendered.append(round(max(-32767.0, min(32767.0, value))))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    temporary = output_path.with_suffix(".source.wav")
    with wave.open(str(temporary), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(rendered.tobytes())

    encoder = shutil.which("oggenc")
    if encoder is None:
        raise RuntimeError("oggenc is required to encode the mastered music")
    if output_path.exists():
        output_path.unlink()
    subprocess.run([encoder, "--quiet", "-q", "5", "-o", str(output_path), str(temporary)], check=True)
    temporary.unlink()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source_root", type=Path, help="directory of approved decoded Ludo WAV inputs")
    parser.add_argument(
        "--content-root",
        type=Path,
        default=Path(__file__).resolve().parents[2] / "src" / "TheLostSoulOfFire" / "Content",
    )
    args = parser.parse_args()
    audio_root = args.content_root / "Audio"
    render_sfx(args.source_root, audio_root / "Sfx")
    render_ambience(args.source_root, audio_root / "Ambience" / "arena_ambience.wav")
    render_music(args.source_root, audio_root / "Music" / "arena_loop.ogg")
    print(f"Mastered 27 organic SFX, ambience, and music under {audio_root}")


if __name__ == "__main__":
    main()
