#!/usr/bin/env python3
"""Bring a painted environment plate to output size, split it into tiles and register it.

    tools/visuals/.venv/bin/python tools/visuals/place_environment.py PLATE.png \\
        --visual-id environment.arena --texture Textures/Environment/arena_floor \\
        --world 1800 1000 [--scale 1.5] [--tile-max 2048] [--layer ground]

The plate is resized to world size x --scale (arena: 2700 x 1500, output resolution at
1920 x 1080), split into tiles no larger than --tile-max and written to
src/TheLostSoulOfFire/Content/<texture>_c<column>r<row>.png. The registry entry becomes a
tiled environment clip and Content.mgcb lists every tile. Every run appends a manifest entry.
"""
from __future__ import annotations

import argparse
import json
import math
import re
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import visuals_common as common  # noqa: E402

CONTENT = common.REPO_ROOT / "src" / "TheLostSoulOfFire" / "Content"
REGISTRY = CONTENT / "Visuals" / "registry.json"
MGCB = CONTENT / "Content.mgcb"


def compact_json(value: object) -> str:
    text = json.dumps(value, indent=2) + "\n"
    return re.sub(r"\[\s*([-0-9.]+),\s*([-0-9.]+)\s*\]", r"[\1, \2]", text)


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("plate", type=Path)
    parser.add_argument("--visual-id", required=True)
    parser.add_argument("--texture", required=True, help="content path without the tile suffix")
    parser.add_argument("--world", nargs=2, type=int, required=True, metavar=("W", "H"))
    parser.add_argument("--scale", type=float, default=1.5)
    parser.add_argument("--tile-max", type=int, default=2048)
    parser.add_argument("--layer", default="ground")
    parser.add_argument("--origin", nargs=2, type=float, default=[0.0, 0.0], metavar=("X", "Y"),
                        help="anchor as a share of the plate, e.g. 0 1 for a wall standing on its bottom-left corner")
    parser.add_argument("--crop", nargs=4, type=int, metavar=("LEFT", "TOP", "RIGHT", "BOTTOM"),
                        help="crop the source first, e.g. to remove a painted paper margin")
    parser.add_argument("--model", default="flux2-klein-4b", help="licence-table key of the source model")
    args = parser.parse_args(argv)

    width, height = round(args.world[0] * args.scale), round(args.world[1] * args.scale)
    columns, rows = math.ceil(width / args.tile_max), math.ceil(height / args.tile_max)
    # Equal tiles: round the output up so every tile has the same size.
    tile_w, tile_h = math.ceil(width / columns), math.ceil(height / rows)
    width, height = tile_w * columns, tile_h * rows

    plate = Image.open(args.plate).convert("RGBA")
    if args.crop:
        plate = plate.crop(tuple(args.crop))
    plate = plate.resize((width, height), Image.Resampling.LANCZOS)
    outputs: list[Path] = []
    step = common.Step(
        visual_id=args.visual_id,
        step="place-environment",
        tool={"name": "place_environment.py", "version": "1"},
        model=common.model_info(args.model),
        parameters={"world": args.world, "output": [width, height], "tiles": [columns, rows], "layer": args.layer,
                    "origin": args.origin, "crop": args.crop},
        inputs=[args.plate],
        reason="Bodenplatte für das Spiel aufbereitet",
    )
    with common.record_step(step) as recorded:
        for row in range(rows):
            for column in range(columns):
                target = CONTENT / f"{args.texture}_c{column}r{row}.png"
                target.parent.mkdir(parents=True, exist_ok=True)
                plate.crop((column * tile_w, row * tile_h, (column + 1) * tile_w, (row + 1) * tile_h)).save(target)
                outputs.append(target)
        recorded.outputs = outputs

        registry = json.loads(REGISTRY.read_text(encoding="utf-8"))
        entry = next((item for item in registry["visuals"] if item["id"] == args.visual_id), None)
        if entry is None:
            entry = {"id": args.visual_id, "kind": "environment", "palette": "world"}
            registry["visuals"].append(entry)
        entry.update({"worldSize": list(args.world), "origin": [args.origin[0], args.origin[1]], "layer": args.layer})
        clip = {"path": f"{args.texture}_{{tile}}" if columns * rows > 1 else args.texture,
                "frameSize": [tile_w, tile_h], "frames": 1, "fps": 1, "loop": True}
        if columns * rows > 1:
            clip["tiles"] = [columns, rows]
        entry["clips"] = {"default": clip}
        REGISTRY.write_text(compact_json(registry), encoding="utf-8")

        text = MGCB.read_text(encoding="utf-8")
        for target in outputs:
            relative = target.relative_to(CONTENT).as_posix()
            if f"#begin {relative}\n" not in text:
                text = text.rstrip("\n") + (
                    f"\n\n#begin {relative}\n/importer:TextureImporter\n/processor:TextureProcessor\n"
                    "/processorParam:ColorKeyColor=255,0,255,255\n/processorParam:ColorKeyEnabled=False\n"
                    "/processorParam:GenerateMipmaps=True\n/processorParam:PremultiplyAlpha=True\n"
                    "/processorParam:ResizeToPowerOfTwo=False\n/processorParam:MakeSquare=False\n"
                    f"/processorParam:TextureFormat=Color\n/build:{relative}\n")
        MGCB.write_text(text, encoding="utf-8")

    print(f"{len(outputs)} Kacheln zu {tile_w}×{tile_h}, Ausgabe {width}×{height}, Registry '{args.visual_id}' aktualisiert")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
