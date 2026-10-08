#!/usr/bin/env python3
"""Pull a cue's body forward to the moment its animation needs it, by trimming its slow start.

    tools/audio/.venv/bin/python tools/audio/retime.py scythe_swing_1 --trim 0.03 --suffix _lead

Reads Content/Audio/Sfx/<name>.wav (and its _v2, _v3 … takes) and writes <name><suffix>.wav
(and <name><suffix>_v2.wav …) with the first --trim seconds removed and a 3 ms fade in, so a
whoosh that swells slowly is already full when the blade is fastest. The approved takes stay as
they are. Prints the ledger lines for SOURCES.md.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
import soundfile as sf

sys.path.insert(0, str(Path(__file__).resolve().parent))
import dsp  # noqa: E402

SFX = Path(__file__).resolve().parents[2] / "src" / "TheLostSoulOfFire" / "Content" / "Audio" / "Sfx"


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("name")
    parser.add_argument("--trim", type=float, required=True)
    parser.add_argument("--suffix", default="_lead")
    args = parser.parse_args(argv)
    sources = [SFX / f"{args.name}.wav"] + sorted(SFX.glob(f"{args.name}_v[0-9].wav"))
    for source in sources:
        x, rate = sf.read(source)
        cut = int(args.trim * rate)
        y = dsp.fades(x[cut:], 0.003, 0.0)
        tail = source.stem[len(args.name):]
        target = SFX / f"{args.name}{args.suffix}{tail}.wav"
        sf.write(target, y, rate, subtype="PCM_16")
        print(f"| `Audio/Sfx/{target.name}` | {source.name}, erste {args.trim * 1000:.0f} ms gekürzt (Körper früher, Animation) "
              f"| {len(y) / rate:.2f} s, 1 Kanal, {dsp.loudness(y):.1f} LUFS, Spitze {20 * np.log10(np.abs(y).max()):.1f} dBFS |")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
