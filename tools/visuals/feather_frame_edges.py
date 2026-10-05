#!/usr/bin/env python3
"""Fade the outer pixels of every frame in a sprite sheet to transparent.

Sheets whose content touches the frame edge show hard cut lines in game and fail the
"transparent border" asset check (`dotnet test`, Visuals/AssetChecks). This scales alpha
from 0 at the frame edge to 1 at `--width` pixels inside, frame by frame, in place.

    python tools/visuals/feather_frame_edges.py SHEET.png --frame 128 128 --frames 9 [--width 3]
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def feather(sheet: np.ndarray, frame_w: int, frame_h: int, frames: int, width: int) -> np.ndarray:
    out = sheet.copy()
    columns = sheet.shape[1] // frame_w
    ramp_x = np.minimum(np.arange(frame_w), np.arange(frame_w)[::-1]).astype(np.float32)
    ramp_y = np.minimum(np.arange(frame_h), np.arange(frame_h)[::-1]).astype(np.float32)
    distance = np.minimum(ramp_y[:, None], ramp_x[None, :])
    factor = np.clip(distance / float(width), 0.0, 1.0)
    for index in range(frames):
        x = (index % columns) * frame_w
        y = (index // columns) * frame_h
        alpha = out[y:y + frame_h, x:x + frame_w, 3].astype(np.float32)
        out[y:y + frame_h, x:x + frame_w, 3] = np.round(alpha * factor).astype(np.uint8)
    return out


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("sheets", nargs="+", type=Path)
    parser.add_argument("--frame", nargs=2, type=int, required=True, metavar=("W", "H"))
    parser.add_argument("--frames", type=int, required=True)
    parser.add_argument("--width", type=int, default=3)
    args = parser.parse_args(argv)

    for path in args.sheets:
        image = Image.open(path).convert("RGBA")
        result = feather(np.asarray(image), args.frame[0], args.frame[1], args.frames, args.width)
        Image.fromarray(result, "RGBA").save(path)
        print(f"feathered {path}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
