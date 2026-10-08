#!/usr/bin/env python3
"""Make a painted texture seamless so it can repeat without mirroring.

    tools/visuals/.venv/bin/python tools/visuals/make_tileable.py IN.png OUT.png [--feather 0.18]
    tools/visuals/.venv/bin/python tools/visuals/make_tileable.py IN.png OUT.png --flatten 60 [--saturation 0.7]

--flatten R removes the image's own large-scale light and dark patches (a high pass: the image
minus a blur of radius R, plus its mean): a fine pattern such as setts then repeats without the
repeated stains that make tiling obvious; the renderer adds large-scale variation that never
repeats. With --flatten the half-shift blend is skipped (the texture is mirrored instead).

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


def flatten(image: Image.Image, radius: float, saturation: float) -> Image.Image:
    from PIL import ImageFilter
    a = np.asarray(image).astype(np.float32)
    blurred = np.asarray(image.filter(ImageFilter.GaussianBlur(radius))).astype(np.float32)
    out = a - blurred + a.reshape(-1, 3).mean(axis=0)
    grey = out.mean(axis=-1, keepdims=True)
    out = grey + (out - grey) * saturation
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("source", type=Path)
    parser.add_argument("target", type=Path)
    parser.add_argument("--feather", type=float, default=0.18)
    parser.add_argument("--flatten", type=float, help="blur radius of the removed large-scale variation (pixels)")
    parser.add_argument("--saturation", type=float, default=1.0)
    args = parser.parse_args(argv)
    image = Image.open(args.source).convert("RGB")
    if args.flatten:
        image = flatten(image, args.flatten, args.saturation)
    else:
        image = tileable(image, args.feather)
    image.save(args.target)
    print(f"geschrieben: {args.target}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
