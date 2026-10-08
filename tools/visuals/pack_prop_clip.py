#!/usr/bin/env python3
"""Pack the frames of an animated prop (a Blender build with the game camera) into a sheet.

    tools/visuals/.venv/bin/python tools/visuals/pack_prop_clip.py \\
        --render-dir art/production/candidates/prop.arena-chest --info chest.json --clip open \\
        --visual-id prop.arena-chest --texture Textures/Props/Arena/chest [--fps 20] [--layer actor]

All frames share one crop (their union box plus a transparent margin), so the foot point stays on
the same pixel. The sheet is laid out row by row (columns = ceil(sqrt(frames))); the first frame
also becomes the `default` clip (<texture>_closed), drawn while the prop rests. Renders already
carry 1.5 pixels per world unit, so nothing is rescaled.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import visuals_common as common  # noqa: E402
from place_pieces import CONTENT, PPU, REGISTRY, compact_json, register_texture  # noqa: E402

MARGIN = 4


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--render-dir", type=Path, required=True)
    parser.add_argument("--info", default="chest.json", help="JSON with foot_px written by the build")
    parser.add_argument("--clip", required=True)
    parser.add_argument("--visual-id", required=True)
    parser.add_argument("--texture", required=True, help="content path of the sheet without .png")
    parser.add_argument("--fps", type=float, default=20.0)
    parser.add_argument("--layer", default="low-prop")
    args = parser.parse_args(argv)

    info = json.loads((args.render_dir / args.info).read_text())
    paths = sorted((args.render_dir / args.clip).glob("*.png"))
    frames = [np.asarray(Image.open(path).convert("RGBA")) for path in paths]
    alpha = np.max([frame[..., 3] for frame in frames], axis=0)
    ys, xs = np.nonzero(alpha > 2)
    left, top = max(0, xs.min() - MARGIN), max(0, ys.min() - MARGIN)
    right, bottom = min(alpha.shape[1], xs.max() + 1 + MARGIN), min(alpha.shape[0], ys.max() + 1 + MARGIN)
    fx, fy = info["foot_px"]
    left, top = min(left, int(fx)), min(top, int(fy))
    right, bottom = max(right, int(fx) + 1), max(bottom, int(fy) + 1)
    left, top, right, bottom = int(left), int(top), int(right), int(bottom)
    width, height = right - left, bottom - top
    columns = math.ceil(math.sqrt(len(frames)))
    rows = math.ceil(len(frames) / columns)
    sheet = np.zeros((rows * height, columns * width, 4), dtype=np.uint8)
    for index, frame in enumerate(frames):
        r, c = divmod(index, columns)
        sheet[r * height:(r + 1) * height, c * width:(c + 1) * width] = frame[top:bottom, left:right]

    target = CONTENT / f"{args.texture}.png"
    closed = CONTENT / f"{args.texture}_closed.png"
    target.parent.mkdir(parents=True, exist_ok=True)
    step = common.Step(
        visual_id=args.visual_id,
        step="pack-prop-clip",
        tool={"name": "Blender + pack_prop_clip.py", "version": "1"},
        model=common.model_info("blender-procedural"),
        parameters={"clip": args.clip, "frames": len(frames), "box": [left, top, right, bottom]},
        inputs=paths,
        outputs=[target, closed],
        decision="angenommen",
        reason="Animiertes Prop aus Blender mit der Spielkamera",
    )
    with common.record_step(step):
        Image.fromarray(sheet, "RGBA").save(target)
        Image.fromarray(np.ascontiguousarray(frames[0][top:bottom, left:right]), "RGBA").save(closed)
        origin = [round((fx - left) / width, 4), round((fy - top) / height, 4)]
        world_size = [round(width / PPU, 2), round(height / PPU, 2)]
        registry = json.loads(REGISTRY.read_text(encoding="utf-8"))
        entry = next((item for item in registry["visuals"] if item["id"] == args.visual_id), None)
        if entry is None:
            entry = {"id": args.visual_id}
            registry["visuals"].append(entry)
        entry.update({"kind": "prop", "palette": "world", "worldSize": world_size, "origin": origin, "layer": args.layer,
                      "clips": {
                          "default": {"path": f"{args.texture}_closed", "frameSize": [width, height], "frames": 1, "fps": 1, "loop": True},
                          args.clip: {"path": args.texture, "frameSize": [width, height], "frames": len(frames), "fps": int(args.fps) if float(args.fps).is_integer() else args.fps, "loop": False},
                      }})
        REGISTRY.write_text(compact_json(registry), encoding="utf-8")
        register_texture(f"{args.texture}.png")
        register_texture(f"{args.texture}_closed.png")
    print(f"{args.visual_id}: {len(frames)} Frames {width}×{height} px, Welt {world_size[0]}×{world_size[1]}, Ursprung {origin}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
