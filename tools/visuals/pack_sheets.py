#!/usr/bin/env python3
"""Pack frames from render_directions.py into game sprite sheets, one per direction.

    tools/visuals/.venv/bin/python tools/visuals/pack_sheets.py \\
        --render art/production/candidates/test.blender-figure/render --animation idle \\
        --visual-id test.blender-figure --texture-dir Textures/Test/BlenderFigure/Animations \\
        [--fps 8] [--margin 3] [--world-size 112] [--register] \\
        [--pixels-per-unit 1.5] [--once] [--progress-distance 180] [--decision angenommen --reason TEXT]

All frames of all eight directions share one crop, so the foot point stays on the same pixel
in every frame and direction. Sheets are laid out row by row like the existing ones
(columns = ceil(sqrt(frames))). Colour sheets go to <texture-dir>/<animation>/<dir>.png, normal
maps to <texture-dir>/<animation>_normal/<dir>.png under src/TheLostSoulOfFire/Content.

--register adds or updates the clip in Content/Visuals/registry.json (origin = foot point) and
the textures in Content.mgcb. Every run appends one entry to art/production/manifest.json.

With --pixels-per-unit each clip keeps its own crop and records its own origin; the entry gets
`pixelsPerUnit`, so a wide scythe sweep and a narrow idle share one scale. Render with
--resolution / --ortho-scale chosen so that this number of pixels covers one world unit
(the slice: 320 px over 3.2 m and 1.5 px per unit, 66.7 units per metre).
"""
from __future__ import annotations

import argparse
import json
import math
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import visuals_common as common  # noqa: E402

DIRECTIONS = ["n", "ne", "e", "se", "s", "sw", "w", "nw"]
GAME = common.REPO_ROOT / "src" / "TheLostSoulOfFire"
CONTENT = GAME / "Content"
REGISTRY = CONTENT / "Visuals" / "registry.json"
MGCB = CONTENT / "Content.mgcb"
FLAT_NORMAL = (128, 128, 255, 0)


def load_frames(directory: Path, frames: int) -> list[np.ndarray]:
    paths = sorted(directory.glob("*.png"))[:frames]
    if len(paths) != frames:
        raise SystemExit(f"{directory}: {len(paths)} statt {frames} Frames")
    return [np.asarray(Image.open(path).convert("RGBA")) for path in paths]


def common_crop(colour: dict[str, list[np.ndarray]], foot: tuple[float, float], margin: int) -> tuple[int, int, int, int]:
    """Union of all visible pixels plus a margin, also containing the foot point."""
    height, width = next(iter(colour.values()))[0].shape[:2]
    alpha = np.zeros((height, width), dtype=bool)
    for frames in colour.values():
        for frame in frames:
            alpha |= frame[..., 3] > 0
    ys, xs = np.nonzero(alpha)
    if len(xs) == 0:
        raise SystemExit("Alle Frames sind leer.")
    foot_x, foot_y = foot[0] * width, foot[1] * height
    left = max(0, int(min(xs.min(), foot_x)) - margin)
    top = max(0, int(min(ys.min(), foot_y)) - margin)
    right = min(width, int(max(xs.max() + 1, foot_x + 1)) + margin)
    bottom = min(height, int(max(ys.max() + 1, foot_y + 1)) + margin)
    if left == 0 or top == 0 or right == width or bottom == height:
        print("Warnung: Die Figur berührt den Renderrand; --ortho-scale in render_directions.py erhöhen.")
    return left, top, right, bottom


def pack(frames: list[np.ndarray], crop: tuple[int, int, int, int], fill: tuple[int, int, int, int] | None) -> Image.Image:
    left, top, right, bottom = crop
    frame_w, frame_h = right - left, bottom - top
    columns = math.ceil(math.sqrt(len(frames)))
    rows = math.ceil(len(frames) / columns)
    sheet = Image.new("RGBA", (columns * frame_w, rows * frame_h), (0, 0, 0, 0) if fill is None else fill)
    for index, frame in enumerate(frames):
        piece = frame[top:bottom, left:right].copy()
        if fill is not None:
            piece[piece[..., 3] == 0] = fill
        sheet.paste(Image.fromarray(piece, "RGBA"), (index % columns * frame_w, index // columns * frame_h))
    return sheet


def compact_json(value: object) -> str:
    text = json.dumps(value, indent=2) + "\n"
    return re.sub(r"\[\s*([-0-9.]+),\s*([-0-9.]+)\s*\]", r"[\1, \2]", text)


def register(args: argparse.Namespace, frame: tuple[int, int], frames: int, origin: tuple[float, float]) -> None:
    registry = json.loads(REGISTRY.read_text(encoding="utf-8"))
    entry = next((item for item in registry["visuals"] if item["id"] == args.visual_id), None)
    if entry is None:
        entry = {"id": args.visual_id, "kind": "character", "palette": "world",
                 "worldSize": [args.world_size, args.world_size], "origin": [0.5, 0.5],
                 "fallbackClip": args.animation, "clips": {}}
        registry["visuals"].append(entry)
    clip = {
        "path": f"{args.texture_dir}/{args.animation}/{{dir}}",
        "frameSize": [frame[0], frame[1]],
        "frames": frames,
        "fps": args.fps,
        "loop": not args.once,
        "normalMap": f"{args.texture_dir}/{args.animation}_normal/{{dir}}",
    }
    if args.progress_distance:
        clip["progress"] = "distance"
        clip["cycleDistance"] = args.progress_distance
    if args.pixels_per_unit:
        entry["pixelsPerUnit"] = args.pixels_per_unit
        clip["origin"] = [round(origin[0], 4), round(origin[1], 4)]
    else:
        entry["origin"] = [round(origin[0], 4), round(origin[1], 4)]
    entry["clips"][args.animation] = clip
    REGISTRY.write_text(compact_json(registry), encoding="utf-8")

    text = MGCB.read_text(encoding="utf-8")
    for direction in DIRECTIONS:
        for suffix, premultiply in (("", True), ("_normal", False)):
            relative = f"{args.texture_dir}/{args.animation}{suffix}/{direction}.png"
            if f"#begin {relative}\n" in text:
                continue
            text = text.rstrip("\n") + (
                f"\n\n#begin {relative}\n/importer:TextureImporter\n/processor:TextureProcessor\n"
                "/processorParam:ColorKeyColor=255,0,255,255\n/processorParam:ColorKeyEnabled=False\n"
                "/processorParam:GenerateMipmaps=True\n"
                f"/processorParam:PremultiplyAlpha={'True' if premultiply else 'False'}\n"
                "/processorParam:ResizeToPowerOfTwo=False\n/processorParam:MakeSquare=False\n"
                f"/processorParam:TextureFormat=Color\n/build:{relative}\n")
    MGCB.write_text(text, encoding="utf-8")


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--render", type=Path, required=True)
    parser.add_argument("--animation", default="idle")
    parser.add_argument("--visual-id", required=True)
    parser.add_argument("--texture-dir", required=True, help="content path, e.g. Textures/Player/Animations")
    parser.add_argument("--fps", type=float, default=8)
    parser.add_argument("--margin", type=int, default=3)
    parser.add_argument("--world-size", type=float, default=112)
    parser.add_argument("--model", default="blender-procedural", help="licence-table key of the source model")
    parser.add_argument("--register", action="store_true")
    parser.add_argument("--pixels-per-unit", type=float, help="texture pixels per world unit; per-clip origins")
    parser.add_argument("--once", action="store_true", help="a one-shot clip (no loop), e.g. an attack")
    parser.add_argument("--progress-distance", type=float, help="advance by distance, one cycle per this many world units")
    parser.add_argument("--decision", default="kandidat", choices=["kandidat", "angenommen", "verworfen"])
    parser.add_argument("--reason", default="Pipeline-Probe mit Testfigur (Aufgabe 6.3), keine Spielgrafik")
    args = parser.parse_args(argv)

    info_path = args.render / f"{args.animation}.render.json"
    info = json.loads(info_path.read_text(encoding="utf-8"))
    frames = int(info["frames"])
    colour = {d: load_frames(args.render / args.animation / d, frames) for d in DIRECTIONS}
    normal = {d: load_frames(args.render / f"{args.animation}_normal" / d, frames) for d in DIRECTIONS}
    crop = common_crop(colour, tuple(info["foot"]), args.margin)
    frame_size = (crop[2] - crop[0], crop[3] - crop[1])
    resolution = info["resolution"]
    origin = ((info["foot"][0] * resolution - crop[0]) / frame_size[0], (info["foot"][1] * resolution - crop[1]) / frame_size[1])

    outputs = []
    step = common.Step(
        visual_id=args.visual_id,
        step="render-pack",
        tool={"name": "Blender + pack_sheets.py", "version": f"Blender {info['blender']}, {info['engine']}"},
        model=common.model_info(args.model),
        parameters={"animation": args.animation, "frames": frames, "fps": args.fps, "frame_size": list(frame_size),
                    "origin": [round(origin[0], 4), round(origin[1], 4)], "elevation_deg": info["elevation_deg"],
                    "ortho_scale": info["ortho_scale"], "render_seconds": info["seconds"]},
        inputs=[info_path],
        decision=args.decision,
        reason=args.reason,
    )
    with common.record_step(step) as recorded:
        for direction in DIRECTIONS:
            for suffix, source, fill in (("", colour, None), ("_normal", normal, FLAT_NORMAL)):
                target = CONTENT / args.texture_dir / f"{args.animation}{suffix}" / f"{direction}.png"
                target.parent.mkdir(parents=True, exist_ok=True)
                pack(source[direction], crop, fill).save(target)
                outputs.append(target)
        recorded.outputs = outputs
        if args.register:
            register(args, frame_size, frames, origin)

    print(f"{len(outputs)} Sheets, Frame {frame_size[0]}×{frame_size[1]}, Ursprung {origin[0]:.3f}/{origin[1]:.3f}"
          + (", Registry und Content.mgcb aktualisiert" if args.register else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
