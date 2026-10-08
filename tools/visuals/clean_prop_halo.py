"""Removes the pale ground patch a generated prop sometimes keeps after cut-out.

Image models paint a light, grey "floor" under an object; background removal keeps it
as part of the subject. In the game it reads as a flat white blob beside the prop.
This finds large connected regions of pale, desaturated pixels that touch the
transparent surround and turns them into a soft dark contact shadow instead.

    python tools/visuals/clean_prop_halo.py --check <png>...
    python tools/visuals/clean_prop_halo.py <png> [--min-area 200] [--shadow 0.42]
"""
from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter


def pale_mask(rgba: np.ndarray) -> np.ndarray:
    rgb = rgba[..., :3].astype(float)
    alpha = rgba[..., 3]
    high = rgb.max(-1)
    low = rgb.min(-1)
    saturation = (high - low) / (high + 1e-3)
    return (alpha > 0) & (high > 150) & (saturation < 0.18)


def components(mask: np.ndarray) -> list[np.ndarray]:
    seen = np.zeros_like(mask, dtype=bool)
    height, width = mask.shape
    found: list[np.ndarray] = []
    for y0, x0 in zip(*np.nonzero(mask)):
        if seen[y0, x0]:
            continue
        queue = deque([(y0, x0)])
        seen[y0, x0] = True
        pixels = []
        while queue:
            y, x = queue.popleft()
            pixels.append((y, x))
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < height and 0 <= nx < width and mask[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True
                    queue.append((ny, nx))
        found.append(np.array(pixels))
    return found


def touches_outside(pixels: np.ndarray, alpha: np.ndarray) -> bool:
    height, width = alpha.shape
    for y, x in pixels:
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if not (0 <= ny < height and 0 <= nx < width) or alpha[ny, nx] < 32:
                return True
    return False


def halo(rgba: np.ndarray, min_area: int) -> np.ndarray:
    mask = np.zeros(rgba.shape[:2], dtype=bool)
    for pixels in components(pale_mask(rgba)):
        if len(pixels) >= min_area and touches_outside(pixels, rgba[..., 3]):
            mask[pixels[:, 0], pixels[:, 1]] = True
    if not mask.any():
        return mask
    # Take in the anti-aliased rim around the patch as well: light or half-transparent pixels.
    rgb = rgba[..., :3].astype(float)
    alpha = rgba[..., 3]
    high = rgb.max(-1)
    saturation = (high - rgb.min(-1)) / (high + 1e-3)
    rim = (alpha > 0) & (((high > 105) & (saturation < 0.22)) | (alpha < 200))
    grown = Image.fromarray(mask.astype(np.uint8) * 255).filter(ImageFilter.MaxFilter(5))
    return mask | ((np.array(grown) > 0) & rim)


def clean(path: Path, min_area: int, shadow: float) -> int:
    image = Image.open(path).convert("RGBA")
    rgba = np.array(image)
    mask = halo(rgba, min_area)
    if not mask.any():
        return 0
    alpha = rgba[..., 3].astype(float) / 255.0
    subject = (alpha > 0.5) & ~mask
    # Contact shadow: dark where the patch hugs the subject, fading outward.
    hug = Image.fromarray((subject * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(4))
    near = np.array(hug).astype(float) / 255.0
    strength = np.clip(near * 2.2, 0.0, 1.0) * shadow
    soft = np.array(Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.5))).astype(float) / 255.0
    out = rgba.copy()
    out[mask, 0] = 14
    out[mask, 1] = 11
    out[mask, 2] = 20
    out[mask, 3] = np.clip(strength[mask] * soft[mask] * 255.0, 0, 255).astype(np.uint8)
    Image.fromarray(out).save(path)
    return int(mask.sum())


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("paths", nargs="+", type=Path)
    parser.add_argument("--check", action="store_true", help="only report pale patches")
    parser.add_argument("--min-area", type=int, default=200)
    parser.add_argument("--shadow", type=float, default=0.42)
    args = parser.parse_args()
    for path in args.paths:
        if args.check:
            rgba = np.array(Image.open(path).convert("RGBA"))
            count = int(halo(rgba, args.min_area).sum())
            if count:
                print(f"{path}: {count} px pale patch")
        else:
            print(f"{path}: {clean(path, args.min_area, args.shadow)} px cleaned")


if __name__ == "__main__":
    main()
