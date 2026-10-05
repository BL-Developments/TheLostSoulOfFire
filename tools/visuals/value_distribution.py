#!/usr/bin/env python3
"""Measure the value checks of the Düsternis-Charta (VISUAL-ART-DIRECTION §10) on captures.

Luminance (0-255) is split into seven equal steps. Reported per image:
  W1  share in steps 1-3            (charta: at least 60 %)
  W2  share in step 7               (charta: at most 3 %)
  W4  median value step of --rect, as a stand-in for the combat floor (charta: step 2 or 3)
  W6  largest connected area darker than luminance 8 inside --rect, compared with a
      player figure (about 60 x 110 output pixels at 1920 x 1080)

W3 and W5 need judgement and stay with the reviewer (art/production/QUALITY-RUBRIC.md).

    python tools/visuals/value_distribution.py artifacts/screenshots/*slice_*overview*.png
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

STEP = 256 / 7
PLAYER_AREA = 60 * 110


def largest_dark_area(lum: np.ndarray, threshold: float = 8.0) -> int:
    """Size of the largest 4-connected region below `threshold` (iterative flood fill)."""
    dark = lum < threshold
    seen = np.zeros_like(dark)
    height, width = dark.shape
    best = 0
    for y0, x0 in zip(*np.nonzero(dark)):
        if seen[y0, x0]:
            continue
        stack = [(y0, x0)]
        seen[y0, x0] = True
        size = 0
        while stack:
            y, x = stack.pop()
            size += 1
            for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
                if 0 <= ny < height and 0 <= nx < width and dark[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True
                    stack.append((ny, nx))
        best = max(best, size)
    return best


def measure(path: Path, rect: tuple[int, int, int, int] | None) -> dict:
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    lum = 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]
    steps = np.minimum((lum / STEP).astype(int), 6)
    shares = [float((steps == index).mean()) for index in range(7)]
    region = lum
    if rect is not None:
        x, y, w, h = rect
        region = lum[y:y + h, x:x + w]
    floor_step = int(min(np.median(region) / STEP, 6)) + 1
    # Downsample 4x for the flood fill; areas are scaled back up.
    small = region[::4, ::4]
    dark_area = largest_dark_area(small) * 16
    return {
        "file": str(path),
        "steps_percent": [round(share * 100, 1) for share in shares],
        "W1_steps_1_to_3_percent": round(sum(shares[:3]) * 100, 1),
        "W1_pass": sum(shares[:3]) >= 0.60,
        "W2_step_7_percent": round(shares[6] * 100, 2),
        "W2_pass": shares[6] <= 0.03,
        "W4_floor_median_step": floor_step,
        "W4_pass": floor_step in (2, 3),
        "W6_largest_black_area_px": int(dark_area),
        "W6_pass": dark_area <= PLAYER_AREA * 2.25,  # 1.5x player scale squared, at output resolution
    }


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("images", nargs="+", type=Path)
    parser.add_argument("--rect", nargs=4, type=int, metavar=("X", "Y", "W", "H"),
                        help="combat area in output pixels for W6 (default: whole image)")
    args = parser.parse_args(argv)
    results = [measure(path, tuple(args.rect) if args.rect else None) for path in args.images]
    print(json.dumps(results, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
