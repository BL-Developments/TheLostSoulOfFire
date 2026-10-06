#!/usr/bin/env python3
"""Derive variants of a frequently played cue, so a hit or swing never repeats exactly.

    tools/audio/.venv/bin/python tools/audio/derive_variants.py scythe_hit [--count 2]

Reads Content/Audio/Sfx/<name>.wav (an approved take) and writes <name>_v2.wav, <name>_v3.wav …
next to it. Each variant shifts pitch by a few percent (resampling, so the body and the
transient move together), tilts the tone a little (low and high shelves of ±1.5 dB) and, for
some, roughens or smooths the attack; it is then matched to the original's loudness and peak.
Variants are derivations of the Ludo bank (allowed by its terms, Content/Audio/SOURCES.md).
Prints the ledger lines for SOURCES.md.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy import signal

sys.path.insert(0, str(Path(__file__).resolve().parent))
import dsp  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
SFX = ROOT / "src" / "TheLostSoulOfFire" / "Content" / "Audio" / "Sfx"

#: (pitch factor, low-shelf dB, high-shelf dB, attack gain) per variant.
SHAPES = [
    (1.045, -1.0, 1.5, 1.15),
    (0.955, 1.5, -1.2, 0.9),
    (1.025, 0.8, -0.6, 1.0),
]


def shelf(x: np.ndarray, cutoff: float, gain_db: float, high: bool) -> np.ndarray:
    """A gentle one-pole shelf: the band above (or below) the cutoff raised by gain_db."""
    if abs(gain_db) < 0.05:
        return x
    b, a = signal.butter(1, cutoff, btype="highpass" if high else "lowpass", fs=dsp.RATE)
    band = signal.lfilter(b, a, x)
    return x + band * (10 ** (gain_db / 20) - 1)


def derive(x: np.ndarray, pitch: float, low_db: float, high_db: float, attack: float) -> np.ndarray:
    length = int(round(len(x) / pitch))
    y = signal.resample(x, length)
    y = shelf(y, 250, low_db, high=False)
    y = shelf(y, 3500, high_db, high=True)
    if abs(attack - 1.0) > 0.01:
        n = min(len(y), dsp.seconds(0.03))
        envelope = np.ones(len(y))
        envelope[:n] = np.linspace(attack, 1.0, n)
        y = y * envelope
    return y


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("name")
    parser.add_argument("--count", type=int, default=2)
    parser.add_argument("--shapes", type=int, nargs="+", help="indices into SHAPES instead of the first --count")
    args = parser.parse_args(argv)
    source = SFX / f"{args.name}.wav"
    x, rate = sf.read(str(source))
    if rate != dsp.RATE:
        raise SystemExit(f"{source}: {rate} Hz")
    if x.ndim > 1:
        x = x.mean(axis=1)
    reference_lufs = dsp.loudness(x)
    reference_peak = 20 * np.log10(np.max(np.abs(x)) + 1e-12)
    shapes = [SHAPES[i] for i in args.shapes] if args.shapes else SHAPES[: args.count]
    for index, shape in enumerate(shapes, start=2):
        y = derive(x, *shape)
        y = dsp.normalise_loudness(y, reference_lufs, peak_ceiling_db=max(reference_peak, -1.0))
        target = SFX / f"{args.name}_v{index}.wav"
        sf.write(str(target), y, rate, subtype="PCM_16")
        peak = 20 * np.log10(np.max(np.abs(y)) + 1e-12)
        print(f"| `Audio/Sfx/{target.name}` | Ableitung von {args.name}.wav (Tonhöhe ×{shape[0]:.3f}, "
              f"Klangneigung {shape[1]:+.1f}/{shape[2]:+.1f} dB) | {len(y) / rate:.2f} s, 1 Kanal, "
              f"{dsp.loudness(y):.1f} LUFS, Spitze {peak:.1f} dBFS |")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
