#!/usr/bin/env python3
"""Deterministic PNG normalization for accepted Ludo raster assets."""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


def clear_hidden_rgb(image: Image.Image) -> Image.Image:
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            red, green, blue, alpha = pixels[x, y]
            if alpha == 0:
                pixels[x, y] = (0, 0, 0, 0)
    return image


def keep_largest_component(image: Image.Image, threshold: int = 16) -> Image.Image:
    alpha = image.getchannel("A")
    alpha_pixels = alpha.load()
    visited: set[tuple[int, int]] = set()
    components: list[list[tuple[int, int]]] = []

    for y in range(image.height):
        for x in range(image.width):
            if (x, y) in visited or alpha_pixels[x, y] < threshold:
                continue
            component: list[tuple[int, int]] = []
            queue = deque([(x, y)])
            visited.add((x, y))
            while queue:
                point = queue.popleft()
                component.append(point)
                px, py = point
                for neighbor in ((px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1)):
                    nx, ny = neighbor
                    if (
                        0 <= nx < image.width
                        and 0 <= ny < image.height
                        and neighbor not in visited
                        and alpha_pixels[nx, ny] >= threshold
                    ):
                        visited.add(neighbor)
                        queue.append(neighbor)
            components.append(component)

    if not components:
        raise ValueError("No visible alpha component found")

    largest = max(components, key=len)
    pixels = image.load()
    for component in components:
        if component is largest:
            continue
        for x, y in component:
            pixels[x, y] = (0, 0, 0, 0)
    return image


def normalize_sprite(source: Path, destination: Path, size: int, margin: float) -> None:
    image = clear_hidden_rgb(Image.open(source).convert("RGBA"))
    image = keep_largest_component(image)
    bounds = image.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError("Sprite has no visible pixels")
    image = image.crop(bounds)

    usable = max(1, int(size * (1.0 - margin * 2.0)))
    scale = min(usable / image.width, usable / image.height)
    resized = image.resize(
        (max(1, round(image.width * scale)), max(1, round(image.height * scale))),
        Image.Resampling.NEAREST,
    )
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.alpha_composite(resized, ((size - resized.width) // 2, (size - resized.height) // 2))
    destination.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(destination, format="PNG", optimize=True)


def normalize_arena(source: Path, destination: Path, width: int, height: int) -> None:
    image = Image.open(source).convert("RGBA")
    target_ratio = width / height
    source_ratio = image.width / image.height
    if source_ratio > target_ratio:
        crop_width = round(image.height * target_ratio)
        left = (image.width - crop_width) // 2
        image = image.crop((left, 0, left + crop_width, image.height))
    elif source_ratio < target_ratio:
        crop_height = round(image.width / target_ratio)
        top = (image.height - crop_height) // 2
        image = image.crop((0, top, image.width, top + crop_height))
    image = image.resize((width, height), Image.Resampling.NEAREST)
    destination.parent.mkdir(parents=True, exist_ok=True)
    image.save(destination, format="PNG", optimize=True)


def normalize_sheet(
    source: Path,
    destination: Path,
    columns: int,
    rows: int,
    clear_ends: bool = False,
    frame_size: int | None = None,
) -> None:
    image = clear_hidden_rgb(Image.open(source).convert("RGBA"))
    if image.width % columns or image.height % rows:
        raise ValueError(f"Sheet {image.size} is not divisible by {columns}x{rows}")
    if clear_ends:
        frame_width = image.width // columns
        frame_height = image.height // rows
        empty = Image.new("RGBA", (frame_width, frame_height), (0, 0, 0, 0))
        image.paste(empty, (0, 0))
        last = columns * rows - 1
        image.paste(empty, ((last % columns) * frame_width, (last // columns) * frame_height))
    if frame_size is not None:
        image = image.resize(
            (columns * frame_size, rows * frame_size),
            Image.Resampling.NEAREST,
        )
    destination.parent.mkdir(parents=True, exist_ok=True)
    image.save(destination, format="PNG", optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--size", type=int)
    parser.add_argument("--margin", type=float, default=0.08)
    parser.add_argument("--arena", nargs=2, type=int, metavar=("WIDTH", "HEIGHT"))
    parser.add_argument("--sheet", nargs=2, type=int, metavar=("COLUMNS", "ROWS"))
    parser.add_argument("--frame-size", type=int)
    parser.add_argument("--clear-ends", action="store_true")
    args = parser.parse_args()

    if args.sheet:
        normalize_sheet(
            args.source,
            args.destination,
            args.sheet[0],
            args.sheet[1],
            clear_ends=args.clear_ends,
            frame_size=args.frame_size,
        )
    elif args.arena:
        normalize_arena(args.source, args.destination, args.arena[0], args.arena[1])
    elif args.size:
        normalize_sprite(args.source, args.destination, args.size, args.margin)
    else:
        parser.error("provide --size, --sheet COLUMNS ROWS, or --arena WIDTH HEIGHT")


if __name__ == "__main__":
    main()
