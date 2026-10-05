#!/usr/bin/env python3
"""Register a piece of a Blender environment render (a prop, door leaves, a seal) as a sprite.

    tools/visuals/.venv/bin/python tools/visuals/place_pieces.py \\
        --render-dir art/production/candidates/environment.hub --piece brazier \\
        --visual-id prop.hub-brazier --texture Textures/Props/Hub/brazier [--layer high-prop]

The environment builders (tools/visuals/blender/build_*.py) render each piece on its own, in
the frame of the plate, as <piece>_full.png, and write its pixel box (and, for props, its
foot point) to pieces.json. Because the plate camera already draws 1.5 texture pixels per
world unit, the world size is the box size / 1.5 and nothing is rescaled: the piece lines up
with the plate to the pixel. The origin is --anchor when given (a world point the game
places the piece by), else the foot point when there is one (props sorted against figures),
otherwise the top-left corner.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import visuals_common as common  # noqa: E402

CONTENT = common.REPO_ROOT / "src" / "TheLostSoulOfFire" / "Content"
REGISTRY = CONTENT / "Visuals" / "registry.json"
MGCB = CONTENT / "Content.mgcb"
PPU = 1.5


def compact_json(value: object) -> str:
    text = json.dumps(value, indent=2) + "\n"
    return re.sub(r"\[\s*([-0-9.]+),\s*([-0-9.]+)\s*\]", r"[\1, \2]", text)


def register_texture(relative: str) -> None:
    text = MGCB.read_text(encoding="utf-8")
    if f"#begin {relative}\n" in text:
        return
    text = text.rstrip("\n") + (
        f"\n\n#begin {relative}\n/importer:TextureImporter\n/processor:TextureProcessor\n"
        "/processorParam:ColorKeyColor=255,0,255,255\n/processorParam:ColorKeyEnabled=False\n"
        "/processorParam:GenerateMipmaps=True\n/processorParam:PremultiplyAlpha=True\n"
        "/processorParam:ResizeToPowerOfTwo=False\n/processorParam:MakeSquare=False\n"
        f"/processorParam:TextureFormat=Color\n/build:{relative}\n")
    MGCB.write_text(text, encoding="utf-8")


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--render-dir", type=Path, required=True)
    parser.add_argument("--piece", required=True)
    parser.add_argument("--visual-id", required=True)
    parser.add_argument("--texture", required=True, help="content path without .png")
    parser.add_argument("--layer", default="high-prop")
    parser.add_argument("--kind", default="prop")
    parser.add_argument("--anchor", nargs=2, type=float, metavar=("X", "Y"),
                        help="world point the game places the piece by (e.g. the foot of a door's centre)")
    args = parser.parse_args(argv)

    pieces = json.loads((args.render_dir / "pieces.json").read_text())
    piece = pieces[args.piece]
    left, top, right, bottom = piece["box"]
    if args.anchor:
        # The origin must lie on the sprite: grow the crop (transparent) to reach the anchor.
        ax, ay = args.anchor[0] * PPU, args.anchor[1] * PPU
        left, top = min(left, int(ax)), min(top, int(ay))
        right, bottom = max(right, int(ax) + 1), max(bottom, int(ay) + 1)
    source = args.render_dir / f"{args.piece}_full.png"
    target = CONTENT / f"{args.texture}.png"
    target.parent.mkdir(parents=True, exist_ok=True)
    step = common.Step(
        visual_id=args.visual_id,
        step="place-piece",
        tool={"name": "Blender + place_pieces.py", "version": "1"},
        model=common.model_info("blender-procedural"),
        parameters={"piece": args.piece, "box": piece["box"], "layer": args.layer},
        inputs=[source],
        outputs=[target],
        decision="angenommen",
        reason="Teil einer 3D-Umgebung (Blender, Spielkamera), pixelgenau zur Platte",
    )
    with common.record_step(step):
        image = Image.open(source).convert("RGBA").crop((left, top, right, bottom))
        image.save(target)
        width, height = image.size
        if args.anchor:
            origin = [round((args.anchor[0] * PPU - left) / width, 4), round((args.anchor[1] * PPU - top) / height, 4)]
        elif "foot" in piece:
            origin = [round((piece["foot"][0] - left) / width, 4), round((piece["foot"][1] - top) / height, 4)]
        else:
            origin = [0.0, 0.0]
        world_size = [round(width / PPU, 2), round(height / PPU, 2)]
        registry = json.loads(REGISTRY.read_text(encoding="utf-8"))
        entry = next((item for item in registry["visuals"] if item["id"] == args.visual_id), None)
        if entry is None:
            entry = {"id": args.visual_id}
            registry["visuals"].append(entry)
        entry.update({"kind": args.kind, "palette": "world", "worldSize": world_size, "origin": origin, "layer": args.layer,
                      "clips": {"default": {"path": args.texture, "frameSize": [width, height], "frames": 1, "fps": 1, "loop": True}}})
        REGISTRY.write_text(compact_json(registry), encoding="utf-8")
        register_texture(f"{args.texture}.png")
    print(f"{args.visual_id}: {width}×{height} px, Welt {world_size[0]}×{world_size[1]}, Ursprung {origin}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
