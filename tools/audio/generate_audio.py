#!/usr/bin/env python3
"""Render The Lost Soul of Fire's original authored audio bank.

The renderer intentionally uses only Python's standard library and Apple's
``afconvert`` for the final Vorbis encode.  It contains no downloaded samples,
model output, or runtime service dependency.  Every pseudo-random layer uses a
fixed seed so committed assets are reproducible.
"""

from __future__ import annotations

import argparse
import math
import random
import shutil
import subprocess
import wave
from array import array
from pathlib import Path


SAMPLE_RATE = 48_000
TAU = math.tau


def envelope(index: int, count: int, attack: float, release: float) -> float:
    attack_samples = max(1, int(attack * SAMPLE_RATE))
    release_samples = max(1, int(release * SAMPLE_RATE))
    return min(1.0, index / attack_samples, (count - 1 - index) / release_samples)


def hump(progress: float, power: float = 1.0) -> float:
    return max(0.0, math.sin(math.pi * progress)) ** power


def chirp(
    duration: float,
    start_frequency: float,
    end_frequency: float,
    amplitude: float = 1.0,
    attack: float = 0.004,
    release: float = 0.04,
    harmonic: float = 0.0,
) -> list[float]:
    count = int(duration * SAMPLE_RATE)
    output = [0.0] * count
    phase = 0.0
    for i in range(count):
        progress = i / max(1, count - 1)
        frequency = start_frequency * ((end_frequency / start_frequency) ** progress)
        phase += TAU * frequency / SAMPLE_RATE
        value = math.sin(phase) + harmonic * math.sin(phase * 2.01)
        output[i] = value * amplitude * envelope(i, count, attack, release)
    return output


def tone(
    duration: float,
    frequency: float,
    amplitude: float = 1.0,
    decay: float = 0.0,
    attack: float = 0.003,
    release: float = 0.04,
    phase_offset: float = 0.0,
) -> list[float]:
    count = int(duration * SAMPLE_RATE)
    output = [0.0] * count
    for i in range(count):
        progress = i / max(1, count - 1)
        level = math.exp(-progress * decay) if decay else 1.0
        output[i] = (
            math.sin(TAU * frequency * i / SAMPLE_RATE + phase_offset)
            * amplitude
            * level
            * envelope(i, count, attack, release)
        )
    return output


def colored_noise(
    duration: float,
    seed: int,
    cutoff: float,
    amplitude: float = 1.0,
    attack: float = 0.003,
    release: float = 0.04,
    high_pass: bool = False,
    shape_power: float = 0.0,
) -> list[float]:
    count = int(duration * SAMPLE_RATE)
    rng = random.Random(seed)
    output = [0.0] * count
    alpha = 1.0 - math.exp(-TAU * cutoff / SAMPLE_RATE)
    low = 0.0
    for i in range(count):
        white = rng.uniform(-1.0, 1.0)
        low += alpha * (white - low)
        value = white - low if high_pass else low
        progress = i / max(1, count - 1)
        shape = hump(progress, shape_power) if shape_power else 1.0
        output[i] = value * amplitude * shape * envelope(i, count, attack, release)
    return output


def crackle(duration: float, seed: int, density: float, amplitude: float) -> list[float]:
    count = int(duration * SAMPLE_RATE)
    rng = random.Random(seed)
    output = [0.0] * count
    impulses = max(1, int(duration * density))
    for _ in range(impulses):
        start = rng.randrange(max(1, count - 180))
        length = rng.randrange(35, 180)
        gain = rng.uniform(0.35, 1.0) * amplitude
        for j in range(length):
            if start + j >= count:
                break
            output[start + j] += rng.uniform(-1.0, 1.0) * gain * math.exp(-j / 32.0)
    return output


def mix(duration: float, *layers: tuple[list[float], float, float]) -> list[float]:
    output = [0.0] * int(duration * SAMPLE_RATE)
    for signal, gain, offset_seconds in layers:
        offset = int(offset_seconds * SAMPLE_RATE)
        for i, sample in enumerate(signal):
            destination = offset + i
            if 0 <= destination < len(output):
                output[destination] += sample * gain
    return output


def add_echo(signal: list[float], taps: tuple[tuple[float, float], ...]) -> list[float]:
    output = signal.copy()
    for delay_seconds, gain in taps:
        delay = int(delay_seconds * SAMPLE_RATE)
        for i in range(delay, len(output)):
            output[i] += signal[i - delay] * gain
    return output


def bell(duration: float, fundamental: float, amplitude: float = 1.0) -> list[float]:
    partials = ((1.0, 1.0, 5.0), (2.01, 0.42, 7.5), (2.97, 0.22, 9.0), (4.13, 0.12, 12.0))
    layers = []
    for ratio, gain, decay in partials:
        layers.append((tone(duration, fundamental * ratio, amplitude * gain, decay, 0.001, 0.06), 1.0, 0.0))
    return mix(duration, *layers)


def impact(duration: float, seed: int, frequency: float, weight: float = 1.0) -> list[float]:
    body = chirp(duration, frequency * 1.7, frequency * 0.62, 0.9 * weight, 0.001, 0.09, 0.28)
    transient = colored_noise(duration * 0.42, seed, 1700.0, 1.3, 0.0005, 0.035, True)
    grit = colored_noise(duration, seed + 1, 240.0, 0.8 * weight, 0.001, 0.08)
    result = mix(duration, (body, 1.0, 0.0), (transient, 0.75, 0.0), (grit, 0.72, 0.0))
    for i in range(len(result)):
        result[i] *= math.exp(-5.0 * i / len(result))
    return result


def normalize(signal: list[float], peak_db: float) -> list[float]:
    peak = max((abs(value) for value in signal), default=1.0)
    target = 10.0 ** (peak_db / 20.0)
    gain = target / max(peak, 1e-9)
    return [max(-1.0, min(1.0, value * gain)) for value in signal]


def write_wav(path: Path, channels: list[list[float]], peak_db: float) -> None:
    normalized = [normalize(channel, peak_db) for channel in channels]
    if len(normalized) > 1:
        shared_peak = max(abs(value) for channel in normalized for value in channel)
        target = 10.0 ** (peak_db / 20.0)
        scale = target / max(shared_peak, 1e-9)
        normalized = [[value * scale for value in channel] for channel in normalized]

    path.parent.mkdir(parents=True, exist_ok=True)
    frames = array("h")
    count = len(normalized[0])
    for i in range(count):
        for channel in normalized:
            frames.append(int(max(-1.0, min(1.0, channel[i])) * 32767.0))

    with wave.open(str(path), "wb") as output:
        output.setnchannels(len(normalized))
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(frames.tobytes())


def render_sfx() -> dict[str, list[float]]:
    sounds: dict[str, list[float]] = {}

    sounds["scythe_swing_1"] = mix(
        0.18,
        (colored_noise(0.18, 101, 1350, 1.0, 0.002, 0.025, True, 1.5), 1.0, 0.0),
        (chirp(0.14, 3200, 780, 0.35, 0.001, 0.025), 1.0, 0.015),
    )
    sounds["scythe_swing_2"] = mix(
        0.24,
        (colored_noise(0.24, 102, 740, 1.2, 0.003, 0.035, True, 1.2), 1.0, 0.0),
        (chirp(0.2, 1500, 260, 0.45, 0.002, 0.04, 0.15), 1.0, 0.012),
    )
    sounds["soul_cleave"] = add_echo(
        mix(
            0.45,
            (colored_noise(0.39, 103, 430, 1.2, 0.006, 0.06, True, 1.0), 0.9, 0.0),
            (chirp(0.38, 180, 52, 0.95, 0.004, 0.08, 0.35), 1.0, 0.0),
            (crackle(0.11, 104, 220, 1.1), 1.0, 0.29),
            (bell(0.14, 1180, 0.34), 1.0, 0.3),
        ),
        ((0.052, 0.16), (0.091, 0.08)),
    )
    sounds["scythe_hit"] = mix(
        0.15,
        (impact(0.15, 105, 112, 0.85), 1.0, 0.0),
        (crackle(0.09, 106, 280, 0.75), 1.0, 0.006),
    )
    sounds["dash"] = mix(
        0.25,
        (impact(0.18, 107, 62, 0.72), 1.0, 0.0),
        (colored_noise(0.24, 108, 510, 1.1, 0.003, 0.035, True, 1.15), 0.9, 0.008),
        (chirp(0.2, 920, 180, 0.34, 0.002, 0.04), 1.0, 0.02),
    )
    sounds["cannon_charge"] = mix(
        0.8,
        (chirp(0.8, 78, 168, 0.64, 0.025, 0.06, 0.35), 1.0, 0.0),
        (chirp(0.8, 360, 980, 0.3, 0.04, 0.04), 1.0, 0.0),
        (colored_noise(0.8, 109, 900, 0.35, 0.02, 0.06, True, 0.7), 1.0, 0.0),
    )
    sounds["cannon_full"] = mix(
        0.22,
        (impact(0.2, 110, 76, 0.55), 1.0, 0.0),
        (bell(0.21, 960, 0.9), 1.0, 0.008),
        (tone(0.18, 1440, 0.25, 5.0, 0.001, 0.04), 1.0, 0.015),
    )
    sounds["cannon_fire"] = add_echo(
        mix(
            0.55,
            (impact(0.55, 111, 51, 1.1), 1.0, 0.0),
            (colored_noise(0.36, 112, 1800, 1.0, 0.0005, 0.055, True), 0.7, 0.0),
            (crackle(0.24, 113, 180, 0.75), 1.0, 0.025),
            (bell(0.34, 176, 0.28), 1.0, 0.018),
        ),
        ((0.074, 0.15), (0.132, 0.08)),
    )
    sounds["burning_charge"] = mix(
        0.55,
        (chirp(0.55, 105, 330, 0.62, 0.01, 0.035, 0.38), 1.0, 0.0),
        (colored_noise(0.55, 114, 720, 0.9, 0.008, 0.035, True), 0.55, 0.0),
        (crackle(0.55, 115, 92, 0.65), 1.0, 0.0),
    )
    sounds["burning_detonation"] = add_echo(
        mix(
            0.7,
            (impact(0.7, 116, 44, 1.05), 1.0, 0.0),
            (colored_noise(0.6, 117, 520, 1.0, 0.001, 0.11), 0.8, 0.0),
            (crackle(0.45, 118, 160, 0.9), 1.0, 0.018),
        ),
        ((0.086, 0.13),),
    )
    sounds["core_hit"] = mix(
        0.18,
        (crackle(0.08, 119, 330, 0.55), 1.0, 0.0),
        (bell(0.18, 1240, 1.0), 1.0, 0.0),
        (chirp(0.14, 2200, 980, 0.28, 0.001, 0.035), 1.0, 0.008),
    )
    sounds["soul_release"] = add_echo(
        mix(
            0.9,
            (chirp(0.72, 310, 740, 0.4, 0.035, 0.12, 0.08), 1.0, 0.0),
            (bell(0.62, 620, 0.62), 1.0, 0.19),
            (colored_noise(0.72, 120, 900, 0.26, 0.08, 0.15, True, 1.0), 1.0, 0.04),
        ),
        ((0.115, 0.12), (0.22, 0.06)),
    )
    sounds["resonance_ready"] = mix(
        0.35,
        (impact(0.3, 121, 48, 0.85), 1.0, 0.0),
        (bell(0.3, 510, 0.42), 1.0, 0.035),
    )
    sounds["resonance_activate"] = add_echo(
        mix(
            0.8,
            (impact(0.14, 122, 52, 0.68), 1.0, 0.0),
            (impact(0.55, 123, 42, 1.15), 1.0, 0.215),
            (colored_noise(0.5, 124, 630, 1.0, 0.002, 0.09), 0.65, 0.22),
            (chirp(0.5, 160, 620, 0.32, 0.005, 0.1, 0.18), 1.0, 0.25),
        ),
        ((0.083, 0.12),),
    )
    sounds["player_hit"] = mix(
        0.2,
        (impact(0.2, 125, 88, 0.75), 1.0, 0.0),
        (crackle(0.11, 126, 210, 0.48), 1.0, 0.015),
    )
    sounds["player_death"] = add_echo(
        mix(
            1.2,
            (chirp(0.85, 92, 31, 0.82, 0.01, 0.16, 0.3), 1.0, 0.0),
            (colored_noise(1.0, 127, 220, 0.75, 0.008, 0.24), 0.8, 0.0),
            (crackle(0.95, 128, 44, 0.48), 1.0, 0.15),
            (bell(0.68, 146, 0.25), 1.0, 0.28),
        ),
        ((0.14, 0.12), (0.31, 0.06)),
    )
    sounds["soul_sense_on"] = mix(
        0.45,
        (colored_noise(0.36, 129, 850, 0.72, 0.08, 0.015, True), 0.75, 0.0),
        (chirp(0.42, 260, 1120, 0.48, 0.045, 0.06, 0.06), 1.0, 0.015),
    )
    sounds["soul_sense_off"] = mix(
        0.3,
        (colored_noise(0.27, 130, 780, 0.55, 0.01, 0.07, True), 0.7, 0.0),
        (chirp(0.28, 980, 240, 0.46, 0.003, 0.065, 0.04), 1.0, 0.0),
    )
    sounds["wave_start"] = add_echo(
        mix(
            0.65,
            (impact(0.5, 131, 48, 0.92), 1.0, 0.0),
            (bell(0.58, 118, 0.62), 1.0, 0.04),
            (crackle(0.15, 132, 100, 0.42), 1.0, 0.0),
        ),
        ((0.105, 0.11),),
    )

    sounds["hollow_swipe"] = mix(
        0.32,
        (colored_noise(0.31, 201, 640, 1.15, 0.002, 0.045, True, 1.35), 1.0, 0.0),
        (chirp(0.25, 1050, 190, 0.42, 0.002, 0.04, 0.1), 1.0, 0.018),
        (crackle(0.12, 202, 110, 0.25), 1.0, 0.04),
    )
    sounds["devourer_slam"] = add_echo(
        mix(
            0.65,
            (impact(0.64, 203, 38, 1.18), 1.0, 0.0),
            (colored_noise(0.46, 204, 280, 0.78, 0.001, 0.1), 0.72, 0.0),
            (crackle(0.24, 205, 130, 0.5), 1.0, 0.01),
        ),
        ((0.092, 0.1),),
    )
    sounds["devourer_devour"] = mix(
        0.85,
        (chirp(0.82, 310, 54, 0.54, 0.07, 0.12, 0.3), 1.0, 0.0),
        (colored_noise(0.82, 206, 420, 0.9, 0.08, 0.12, False, 0.75), 0.65, 0.0),
        (tone(0.68, 67, 0.45, 2.2, 0.04, 0.12), 1.0, 0.11),
    )
    sounds["enemy_death"] = add_echo(
        mix(
            0.55,
            (chirp(0.5, 180, 48, 0.54, 0.004, 0.11, 0.15), 1.0, 0.0),
            (colored_noise(0.48, 207, 260, 0.75, 0.002, 0.12), 0.7, 0.0),
            (crackle(0.38, 208, 78, 0.62), 1.0, 0.01),
        ),
        ((0.076, 0.08),),
    )
    sounds["cannon_impact"] = mix(
        0.32,
        (impact(0.32, 209, 66, 0.92), 1.0, 0.0),
        (bell(0.23, 245, 0.25), 1.0, 0.012),
        (crackle(0.18, 210, 140, 0.38), 1.0, 0.0),
    )
    sounds["title_confirm"] = add_echo(
        mix(0.32, (bell(0.31, 440, 0.58), 1.0, 0.0), (bell(0.24, 660, 0.32), 1.0, 0.045)),
        ((0.071, 0.1),),
    )
    sounds["wave_clear"] = add_echo(
        mix(
            0.75,
            (bell(0.68, 293.665, 0.42), 1.0, 0.0),
            (bell(0.6, 349.228, 0.34), 1.0, 0.105),
            (bell(0.52, 440.0, 0.3), 1.0, 0.205),
        ),
        ((0.13, 0.12),),
    )
    sounds["ending_reveal"] = add_echo(
        mix(
            1.6,
            (chirp(1.25, 146.832, 293.665, 0.3, 0.08, 0.22, 0.1), 1.0, 0.0),
            (bell(1.25, 293.665, 0.52), 1.0, 0.24),
            (bell(1.05, 440.0, 0.34), 1.0, 0.43),
            (colored_noise(1.2, 211, 1000, 0.18, 0.18, 0.3, True, 1.0), 1.0, 0.02),
        ),
        ((0.18, 0.12), (0.36, 0.055)),
    )

    return sounds


def periodic_value_noise(time_seconds: float, duration: float, values: list[float]) -> float:
    position = (time_seconds % duration) / duration * len(values)
    index = int(position)
    fraction = position - index
    smooth = fraction * fraction * (3.0 - 2.0 * fraction)
    first = values[index % len(values)]
    second = values[(index + 1) % len(values)]
    return first + (second - first) * smooth


def render_ambience(path: Path) -> None:
    duration = 24.0
    count = int(duration * SAMPLE_RATE)
    rng = random.Random(301)
    wind_left = [rng.uniform(-1.0, 1.0) for _ in range(144)]
    wind_right = [rng.uniform(-1.0, 1.0) for _ in range(144)]
    left = [0.0] * count
    right = [0.0] * count
    chain_times = (4.4, 11.25, 18.7)
    for i in range(count):
        t = i / SAMPLE_RATE
        rumble = 0.16 * math.sin(TAU * (43.0 * t + 0.06 * math.sin(TAU * t / duration)))
        rumble += 0.07 * math.sin(TAU * 64.5 * t + 0.8)
        wind_l = periodic_value_noise(t, duration, wind_left) * 0.12
        wind_r = periodic_value_noise(t, duration, wind_right) * 0.12
        whisper = 0.018 * math.sin(TAU * 311.0 * t + 1.8 * math.sin(TAU * 5.0 * t / duration))
        chain_l = 0.0
        chain_r = 0.0
        for event_index, event_time in enumerate(chain_times):
            local = t - event_time
            if 0.0 <= local < 0.55:
                decay = math.exp(-local * 7.5)
                metal = math.sin(TAU * (720.0 + event_index * 93.0) * local)
                metal += 0.55 * math.sin(TAU * (1180.0 + event_index * 71.0) * local)
                if event_index % 2:
                    chain_r += metal * decay * 0.11
                    chain_l += metal * decay * 0.04
                else:
                    chain_l += metal * decay * 0.11
                    chain_r += metal * decay * 0.04
        bell_local = t - 15.8
        distant_bell = 0.0
        if 0.0 <= bell_local < 4.0:
            distant_bell = math.sin(TAU * 58.0 * bell_local) * math.exp(-bell_local * 0.9) * 0.12
            distant_bell += math.sin(TAU * 117.0 * bell_local) * math.exp(-bell_local * 1.3) * 0.045
        edge_fade = min(1.0, t / 0.02, (duration - t) / 0.02)
        left[i] = (rumble + wind_l + whisper + chain_l + distant_bell) * edge_fade
        right[i] = (rumble * 0.96 + wind_r - whisper * 0.7 + chain_r + distant_bell * 0.94) * edge_fade
    write_wav(path, [left, right], -8.0)


def quantized_frequency(frequency: float, loop_duration: float) -> float:
    return round(frequency * loop_duration) / loop_duration


def music_sample(t: float, channel: int, duration: float, noise_values: list[float]) -> float:
    beat_duration = 60.0 / 72.0
    bar_duration = beat_duration * 4.0
    bar = int(t / bar_duration) % 30
    bar_time = t % bar_duration
    beat_time = t % beat_duration
    progression = (
        (73.416, 87.307, 110.0),   # D minor
        (58.270, 73.416, 87.307),  # B-flat
        (65.406, 82.407, 97.999),  # C
        (73.416, 87.307, 110.0),
        (48.999, 58.270, 73.416),  # G minor
        (55.0, 69.296, 82.407),    # A tension
    )
    chord = progression[bar % len(progression)]
    pan_phase = 0.28 if channel == 0 else -0.31

    drone_frequency = quantized_frequency(36.708, duration)
    drone = 0.18 * math.sin(TAU * drone_frequency * t + pan_phase)
    drone += 0.08 * math.sin(TAU * drone_frequency * 2.0 * t + 0.7 - pan_phase)

    pad_envelope = 0.5 - 0.5 * math.cos(TAU * bar_time / bar_duration)
    pad = 0.0
    for note_index, frequency in enumerate(chord):
        quantized = quantized_frequency(frequency, duration)
        phase = pan_phase * (note_index + 1)
        pad += math.sin(TAU * quantized * t + phase) * (0.052 - note_index * 0.008)
        pad += math.sin(TAU * quantized * 2.002 * t - phase) * 0.012
    pad *= 0.38 + pad_envelope * 0.62

    choir_swell = (0.5 - 0.5 * math.cos(TAU * (t % (bar_duration * 2.0)) / (bar_duration * 2.0))) ** 1.7
    choir = 0.025 * choir_swell * math.sin(TAU * quantized_frequency(146.832, duration) * t + pan_phase)
    choir += 0.018 * choir_swell * math.sin(TAU * quantized_frequency(174.614, duration) * t - pan_phase)

    pulse = 0.0
    if beat_time < 0.19:
        progress = beat_time / 0.19
        pulse = math.sin(TAU * (59.0 - progress * 19.0) * beat_time) * math.exp(-progress * 5.5) * 0.18
    if bar % 3 == 2 and beat_time < 0.08 and int(t / beat_duration) % 4 == 2:
        pulse += (1.0 if channel == 0 else -1.0) * math.sin(TAU * 910.0 * beat_time) * math.exp(-beat_time * 43.0) * 0.035

    furnace = periodic_value_noise(t, duration, noise_values) * 0.025
    total = drone + pad + choir + pulse + furnace
    edge_fade = min(1.0, t / 0.02, (duration - t) / 0.02)
    return math.tanh(total * 1.35) * 0.62 * edge_fade


def render_music(path: Path) -> None:
    duration = 100.0  # 30 complete 4/4 bars at 72 BPM.
    count = int(duration * SAMPLE_RATE)
    rng = random.Random(401)
    noise_left = [rng.uniform(-1.0, 1.0) for _ in range(300)]
    noise_right = [rng.uniform(-1.0, 1.0) for _ in range(300)]
    temporary_wav = path.with_suffix(".source.wav")
    temporary_wav.parent.mkdir(parents=True, exist_ok=True)

    with wave.open(str(temporary_wav), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        chunk_size = 4096
        for start in range(0, count, chunk_size):
            frames = array("h")
            end = min(count, start + chunk_size)
            for i in range(start, end):
                t = i / SAMPLE_RATE
                left = music_sample(t, 0, duration, noise_left)
                right = music_sample(t, 1, duration, noise_right)
                frames.append(int(max(-1.0, min(1.0, left)) * 32767.0))
                frames.append(int(max(-1.0, min(1.0, right)) * 32767.0))
            output.writeframes(frames.tobytes())

    if path.exists():
        path.unlink()
    oggenc = shutil.which("oggenc")
    if oggenc is not None:
        subprocess.run(
            [oggenc, "--quiet", "-q", "5", "-o", str(path), str(temporary_wav)],
            check=True,
        )
    else:
        afconvert = shutil.which("afconvert")
        if afconvert is None:
            raise RuntimeError("oggenc or afconvert is required to encode the stereo OGG music loop")
        subprocess.run(
            [afconvert, "-f", "Oggf", "-d", "vorb", str(temporary_wav), str(path)],
            check=True,
        )
    temporary_wav.unlink()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--content-root",
        type=Path,
        default=Path(__file__).resolve().parents[2] / "src" / "TheLostSoulOfFire" / "Content",
    )
    args = parser.parse_args()

    sfx_root = args.content_root / "Audio" / "Sfx"
    sfx = render_sfx()
    for name, signal in sfx.items():
        write_wav(sfx_root / f"{name}.wav", [signal], -2.2 if name not in {"title_confirm", "wave_clear", "ending_reveal"} else -4.0)

    render_ambience(args.content_root / "Audio" / "Ambience" / "arena_ambience.wav")
    render_music(args.content_root / "Audio" / "Music" / "arena_loop.ogg")
    print(f"Rendered {len(sfx)} SFX, ambience, and music under {args.content_root / 'Audio'}")


if __name__ == "__main__":
    main()
