#!/usr/bin/env python3
"""Compose a platform plate from painted textures so the walkable area matches exactly.

    tools/visuals/.venv/bin/python tools/visuals/compose_platform.py \\
        --surround water.png --surface slabs.png --out plate.png \\
        [--world 1800 1000] [--walkable 110 125 1690 875] [--kerb 16] [--scale 1.5]

Image models do not keep a layout to the pixel, but gameplay needs the painted edge exactly on
the collision edge. So the surround (water) fills the plate, the surface texture fills the
walkable rectangle, and a granite kerb plus a soft shadow toward the lower right (key light
from the upper left) are built from the surface texture itself. Output is at output
resolution (world x --scale), ready for place_environment.py.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter, ImageOps


def cover(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    return ImageOps.fit(image.convert("RGB"), size, Image.Resampling.LANCZOS)


def tiled(image: Image.Image, size: tuple[int, int], repeat: int) -> Image.Image:
    """Repeat the texture repeat x repeat times, mirrored so the seams line up, then fit."""
    if repeat <= 1:
        return cover(image, size)
    cell = cover(image, (max(1, size[0] // repeat + 1), max(1, size[1] // repeat + 1)))
    sheet = Image.new("RGB", (cell.width * repeat, cell.height * repeat))
    for row in range(repeat):
        for column in range(repeat):
            piece = cell
            if column % 2:
                piece = ImageOps.mirror(piece)
            if row % 2:
                piece = ImageOps.flip(piece)
            sheet.paste(piece, (column * cell.width, row * cell.height))
    return sheet.crop((0, 0, size[0], size[1]))


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--surround", type=Path, required=True)
    parser.add_argument("--surface", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--world", nargs=2, type=int, default=[1800, 1000])
    parser.add_argument("--walkable", nargs=4, type=int, default=[110, 125, 1690, 875], metavar=("L", "T", "R", "B"))
    parser.add_argument("--kerb", type=float, default=16, help="kerb width in world units")
    parser.add_argument("--scale", type=float, default=1.5)
    parser.add_argument("--surface-repeat", type=int, default=1, help="repeat the surface texture n x n (smaller slabs)")
    args = parser.parse_args(argv)

    s = args.scale
    width, height = round(args.world[0] * s), round(args.world[1] * s)
    left, top, right, bottom = (round(value * s) for value in args.walkable)
    kerb = round(args.kerb * s)

    plate = cover(Image.open(args.surround), (width, height))

    # Shadow of the platform on the water, cast toward the lower right.
    shadow = Image.new("L", (width, height), 0)
    offset = round(12 * s)
    shadow.paste(150, (left - kerb + offset, top - kerb + offset, right + kerb + offset, bottom + kerb + offset))
    shadow = shadow.filter(ImageFilter.GaussianBlur(radius=10 * s))
    plate = Image.composite(Image.new("RGB", (width, height), (6, 9, 14)), plate, shadow)

    # Granite kerb: the surface texture, cooler, lighter and calmer, with a dark outer line.
    kerb_box = (left - kerb, top - kerb, right + kerb, bottom + kerb)
    surface = tiled(Image.open(args.surface), (kerb_box[2] - kerb_box[0], kerb_box[3] - kerb_box[1]), args.surface_repeat)
    granite = np.asarray(surface.filter(ImageFilter.GaussianBlur(radius=1.5 * s))).astype(np.float32)
    luminance = granite.mean(axis=-1, keepdims=True)
    granite = np.clip(luminance * 1.25 + (granite - luminance) * 0.4 + [4, 6, 10], 0, 255)
    plate.paste(Image.fromarray(granite.astype(np.uint8)), kerb_box[:2])
    edge = Image.new("L", plate.size, 0)
    edge.paste(255, kerb_box)
    edge = edge.filter(ImageFilter.FIND_EDGES).filter(ImageFilter.MaxFilter(3))
    plate = Image.composite(Image.new("RGB", plate.size, (14, 16, 22)), plate, edge.point(lambda v: 200 if v > 0 else 0))

    # Walkable surface, with a faint inner shade along the kerb so the edge reads as a step.
    inner = tiled(Image.open(args.surface), (right - left, bottom - top), args.surface_repeat)
    plate.paste(inner, (left, top))
    shade = Image.new("L", plate.size, 0)
    band = round(10 * s)
    shade.paste(90, (left, top, right, bottom))
    shade.paste(0, (left + band, top + band, right - band, bottom - band))
    shade = shade.filter(ImageFilter.GaussianBlur(radius=band / 2))
    mask = Image.new("L", plate.size, 0)
    mask.paste(255, (left, top, right, bottom))
    shade = Image.fromarray(np.minimum(np.asarray(shade), np.asarray(mask)).astype(np.uint8))
    plate = Image.composite(Image.new("RGB", plate.size, (20, 24, 32)), plate, shade)

    args.out.parent.mkdir(parents=True, exist_ok=True)
    plate.save(args.out)
    print(f"geschrieben: {args.out} ({width}×{height}, begehbar {left},{top}–{right},{bottom} px)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
