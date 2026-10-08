#!/usr/bin/env python3
"""Draw a control image (value layout) from a room's collision layout.

    tools/visuals/.venv/bin/python tools/visuals/control_image.py arena --out layout.png [--width 1440]

The image has the proportions of the world (arena: 1800 x 1000 world units). The walkable
combat area is a calm mid-dark floor; architecture belongs only to the margins outside it,
so painted walls never cover ground the player can walk on. Landmark positions follow the
region contract (docs/current/regions/industrial-cathedral.md, "Gold-Standard-Raum").
Used as init image for generate.py (--init) or as control image in comfy/control.json.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

#: World size and walkable area, mirrored from GameBalance.ArenaBounds and CombatBounds.
ROOMS = {
    "arena": {
        "world": (1800, 1000),
        "combat": (105, 95, 1695, 905),
        # Landmarks in world units: furnace and rose window north, left of centre; gate south.
        "furnace": (560, 0, 820, 95),
        "rose": (690, 18, 34),
        "gate": (820, 905, 980, 1000),
        "columns_x": (55, 1745),
        "columns_y": (230, 500, 770),
    },
    # Prologue, section I: walkable area from PrologueDirector.ExplorationBounds, trace at
    # SoulTrace, the first Hollow at (1160, 545), exit marker at (1535, 515).
    "shore": {
        "world": (1800, 1000),
        "combat": (110, 125, 1690, 875),
    },
}


def draw_shore(width: int) -> Image.Image:
    spec = ROOMS["shore"]
    world_w, world_h = spec["world"]
    scale = width / world_w
    height = round(world_h * scale)

    def box(rect: tuple[float, float, float, float]) -> tuple[int, int, int, int]:
        return tuple(round(value * scale) for value in rect)  # type: ignore[return-value]

    image = Image.new("RGB", (width, height), (22, 30, 38))  # dark still water
    pen = ImageDraw.Draw(image)
    left, top, right, bottom = spec["combat"]
    pen.rectangle(box((left - 15, top - 12, right + 15, bottom + 12)), fill=(88, 92, 98))  # granite kerb
    pen.rectangle(box(spec["combat"]), fill=(70, 74, 80))  # wet concrete platform
    for x in range(left, right, 180):
        pen.line(box((x, top, x, bottom)), fill=(62, 66, 72), width=max(1, round(3 * scale)))
    # Memory group at the trace (805, 520): a bench facing the water, the suitcase beside it.
    pen.rectangle(box((690, 440, 880, 470)), fill=(78, 58, 44))
    pen.rectangle(box((835, 560, 885, 595)), fill=(92, 60, 40))
    # Landmark: the tilted departure board rising out of the water north of the platform.
    pen.polygon([tuple(round(v * scale) for v in point) for point in ((1060, 112), (1110, 6), (1290, 30), (1250, 118))], fill=(46, 50, 56))
    # Cast-iron canopy pillars along the north kerb, a lamp post near the exit.
    for x in (330, 720, 1450):
        pen.rectangle(box((x - 12, top - 10, x + 12, top + 26)), fill=(60, 82, 70))
    return image.filter(ImageFilter.GaussianBlur(radius=max(1, round(3 * scale))))


def draw(room: str, width: int) -> Image.Image:
    if room == "shore":
        return draw_shore(width)
    spec = ROOMS[room]
    world_w, world_h = spec["world"]
    scale = width / world_w
    height = round(world_h * scale)

    def box(rect: tuple[float, float, float, float]) -> tuple[int, int, int, int]:
        return tuple(round(value * scale) for value in rect)  # type: ignore[return-value]

    image = Image.new("RGB", (width, height), (20, 18, 24))  # dark architecture band
    pen = ImageDraw.Draw(image)
    left, top, right, bottom = spec["combat"]
    pen.rectangle(box(spec["combat"]), fill=(74, 70, 78))  # calm mid-dark casting floor
    # Large stone slabs as a gentle value cadence, never stronger than the figures will be.
    for x in range(left, right, 265):
        pen.line(box((x, top, x, bottom)), fill=(64, 60, 68), width=max(1, round(4 * scale)))
    for y in range(top, bottom, 270):
        pen.line(box((left, y, right, y)), fill=(64, 60, 68), width=max(1, round(4 * scale)))
    # Wall foot: a lighter rim where the floor meets the walls (the collision edge).
    pen.rectangle(box(spec["combat"]), outline=(96, 90, 98), width=max(2, round(10 * scale)))
    # North: furnace like an altar with a glowing tapping hole, rose window above.
    pen.rectangle(box(spec["furnace"]), fill=(40, 36, 44))
    fx0, fy0, fx1, fy1 = spec["furnace"]
    pen.ellipse(box(((fx0 + fx1) / 2 - 22, fy1 - 46, (fx0 + fx1) / 2 + 22, fy1 - 6)), fill=(150, 90, 230))
    cx, cy, r = spec["rose"]
    pen.ellipse(box((cx - r, cy - r * 0.6, cx + r, cy + r * 0.6)), outline=(110, 104, 112), width=max(1, round(5 * scale)))
    # Side aisles: column feet in the left and right margins.
    for x in spec["columns_x"]:
        for y in spec["columns_y"]:
            pen.rectangle(box((x - 30, y - 30, x + 30, y + 30)), fill=(48, 44, 52))
    # South: the works gate.
    pen.rectangle(box(spec["gate"]), fill=(36, 32, 40))
    return image.filter(ImageFilter.GaussianBlur(radius=max(1, round(3 * scale))))


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("room", choices=sorted(ROOMS))
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--width", type=int, default=1440)
    args = parser.parse_args(argv)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    draw(args.room, args.width).save(args.out)
    print(f"geschrieben: {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
