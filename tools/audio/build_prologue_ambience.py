#!/usr/bin/env python3
"""Build three prologue soundscape loops from the approved AMB-01 master.

No new generated material is introduced. The Golden Slice ambience remains the
source; broad filtering, stereo motion and dynamics produce bounded sector
variants while preserving the project's existing acoustic grain.
"""

from __future__ import annotations

import math
import wave
from array import array
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
AMBIENCE = REPO / "src" / "TheLostSoulOfFire" / "Content" / "Audio" / "Ambience"
SOURCE = AMBIENCE / "arena_ambience.wav"


def read_stereo(path: Path) -> tuple[list[float], list[float], int]:
    with wave.open(str(path), "rb") as source:
        if source.getnchannels() != 2 or source.getsampwidth() != 2:
            raise ValueError(f"Expected stereo 16-bit PCM: {path}")
        rate = source.getframerate()
        samples = array("h")
        samples.frombytes(source.readframes(source.getnframes()))
    return (
        [samples[index] / 32768.0 for index in range(0, len(samples), 2)],
        [samples[index] / 32768.0 for index in range(1, len(samples), 2)],
        rate,
    )


def low_pass(signal: list[float], cutoff: float, rate: int, poles: int = 1) -> list[float]:
    alpha = 1.0 - math.exp(-math.tau * cutoff / rate)
    output = list(signal)
    # Repeating the source first settles the filter close to its periodic state,
    # preventing a fresh-filter transient at the loop boundary.
    for _ in range(poles):
        value = 0.0
        repeated = output + output
        for index, sample in enumerate(repeated):
            value += alpha * (sample - value)
            if index >= len(output):
                output[index - len(output)] = value
    return output


def high_pass(signal: list[float], cutoff: float, rate: int) -> list[float]:
    low = low_pass(signal, cutoff, rate)
    return [sample - low[index] for index, sample in enumerate(signal)]


def normalize(left: list[float], right: list[float], peak_db: float) -> tuple[list[float], list[float]]:
    peak = max(max(map(abs, left), default=0.0), max(map(abs, right), default=0.0))
    scale = 10.0 ** (peak_db / 20.0) / max(peak, 1e-9)
    return ([sample * scale for sample in left], [sample * scale for sample in right])


def seamless(left: list[float], right: list[float], rate: int) -> tuple[list[float], list[float]]:
    overlap = int(rate * 0.5)

    def fold(channel: list[float]) -> list[float]:
        body = channel[overlap:-overlap]
        tail = channel[-overlap:]
        head = channel[:overlap]
        joined: list[float] = []
        for index in range(overlap):
            amount = index / max(1, overlap - 1)
            amount = amount * amount * (3.0 - 2.0 * amount)
            joined.append(tail[index] * (1.0 - amount) + head[index] * amount)
        return body + joined

    return fold(left), fold(right)


def write_stereo(path: Path, left: list[float], right: list[float], rate: int) -> None:
    frames = array("h")
    for l_sample, r_sample in zip(left, right):
        frames.append(int(round(max(-1.0, min(1.0, l_sample)) * 32767.0)))
        frames.append(int(round(max(-1.0, min(1.0, r_sample)) * 32767.0)))
    with wave.open(str(path), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(rate)
        output.writeframes(frames.tobytes())


def build() -> None:
    left, right, rate = read_stereo(SOURCE)
    seconds = len(left) / rate

    # Emergence: low, narrow and mostly airless. The occasional human-world
    # residue already exists in the scene; the bed leaves deliberate room for it.
    emergence_l = low_pass(left, 1050.0, rate, poles=2)
    emergence_r = low_pass(right, 1050.0, rate, poles=2)
    middle = [(l + r) * 0.5 for l, r in zip(emergence_l, emergence_r)]
    emergence_l = [middle[i] * 0.82 + emergence_l[i] * 0.18 for i in range(len(middle))]
    emergence_r = [middle[i] * 0.82 + emergence_r[i] * 0.18 for i in range(len(middle))]

    # Search: the same ruin has more lateral movement and a little more upper
    # masonry/chain presence, suggesting Wardens have recently crossed it.
    search_l = low_pass(high_pass(left, 58.0, rate), 2600.0, rate)
    search_r = low_pass(high_pass(right, 58.0, rate), 2600.0, rate)
    delay = int(rate * 0.115)
    search_l = [search_l[i] * 0.86 + search_r[(i - delay) % len(search_r)] * 0.14 for i in range(len(search_l))]
    search_r = [search_r[i] * 0.86 + search_l[(i + delay) % len(search_l)] * 0.14 for i in range(len(search_r))]

    # Transit: high moving air over the original low stone body. The modulation
    # completes whole cycles across the source length, so it remains loop-safe.
    wind_l = high_pass(left, 170.0, rate)
    wind_r = high_pass(right, 170.0, rate)
    engine_l = low_pass(left, 190.0, rate, poles=2)
    engine_r = low_pass(right, 190.0, rate, poles=2)
    transit_l: list[float] = []
    transit_r: list[float] = []
    for index in range(len(left)):
        time = index / rate
        thrust = 0.78 + math.sin(math.tau * 0.1 * time) * 0.10 + math.sin(math.tau * 0.25 * time) * 0.05
        transit_l.append(wind_l[index] * 0.66 + engine_l[index] * thrust)
        transit_r.append(wind_r[index] * 0.66 + engine_r[index] * thrust)

    variants = {
        "prologue_emergence.wav": normalize(emergence_l, emergence_r, -13.5),
        "prologue_search.wav": normalize(search_l, search_r, -12.5),
        "prologue_transit.wav": normalize(transit_l, transit_r, -11.5),
    }
    for name, channels in variants.items():
        loop_left, loop_right = seamless(*channels, rate)
        write_stereo(AMBIENCE / name, loop_left, loop_right, rate)
        peak = max(max(map(abs, loop_left)), max(map(abs, loop_right)))
        print(f"{name}: {len(loop_left) / rate:.2f}s stereo {rate}Hz peak={20 * math.log10(max(peak, 1e-9)):.2f}dBFS source={seconds:.2f}s")


if __name__ == "__main__":
    build()
