#!/usr/bin/env python3
"""Make a painted texture seamless so it can repeat without mirroring.

    tools/visuals/.venv/bin/python tools/visuals/make_tileable.py IN.png OUT.png [--feather 0.18]

The image is blended with a copy shifted by half its size; a mask that is opaque in the
middle and fades toward the borders keeps the original everywhere except near the edges,
where the shifted copy (whose own edges meet in the middle) takes over. The borders of the
result then match on both axes.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def tileable(image: Image.Image, feather: float) -> Image.Image:
    a = np.asarray(image.convert("RGB")).astype(np.float32)
    h, w = a.shape[:2]
    shifted = np.roll(np.roll(a, h // 2, axis=0), w // 2, axis=1)
    y = np.minimum(np.arange(h), h - 1 - np.arange(h)) / (h * feather)
    x = np.minimum(np.arange(w), w - 1 - np.arange(w)) / (w * feather)
    mask = np.clip(np.minimum.outer(y, x), 0, 1)
    mask = mask * mask * (3 - 2 * mask)
    out = a * mask[..., None] + shifted * (1 - mask[..., None])
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("source", type=Path)
    parser.add_argument("target", type=Path)
    parser.add_argument("--feather", type=float, default=0.18)
    args = parser.parse_args(argv)
    tileable(Image.open(args.source), args.feather).save(args.target)
    print(f"geschrieben: {args.target}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
