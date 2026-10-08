#!/usr/bin/env python3
"""Turn a generated prop image into a game prop: cut out, crop, foot point, register.

    tools/visuals/.venv/bin/python tools/visuals/make_prop.py IMAGE.png \\
        --visual-id prop.arena-lockers --texture Textures/Props/Arena/lockers \\
        --height 220 [--layer high-prop] [--model flux2-klein-4b] [--already-cut]

The image is cut out with rembg/BiRefNet (unless --already-cut), cropped to its visible
pixels plus a transparent margin, scaled so the prop is --height world units tall at output
resolution (x1.5) and written to src/TheLostSoulOfFire/Content/<texture>.png. The foot point
is the bottom of the opaque body (the lowest row with mostly solid pixels), so the prop
stands where its base touches the floor; the registry origin records it.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import visuals_common as common  # noqa: E402

CONTENT = common.REPO_ROOT / "src" / "TheLostSoulOfFire" / "Content"
REGISTRY = CONTENT / "Visuals" / "registry.json"
MGCB = CONTENT / "Content.mgcb"
OUTPUT_SCALE = 1.5
MARGIN = 4


def compact_json(value: object) -> str:
    text = json.dumps(value, indent=2) + "\n"
    return re.sub(r"\[\s*([-0-9.]+),\s*([-0-9.]+)\s*\]", r"[\1, \2]", text)


def foot_row(alpha: np.ndarray) -> int:
    """Lowest row whose opaque run is at least a sixth of the widest row: the base or the feet
    of legs, not a stray shadow tip."""
    widths = (alpha > 200).sum(axis=1)
    threshold = widths.max() / 6
    rows = np.nonzero(widths >= threshold)[0]
    return int(rows.max()) if len(rows) else alpha.shape[0] - 1


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("image", type=Path)
    parser.add_argument("--visual-id", required=True)
    parser.add_argument("--texture", required=True)
    parser.add_argument("--height", type=float, required=True, help="world units from foot to top")
    parser.add_argument("--layer", default="high-prop")
    parser.add_argument("--model", default="flux2-klein-4b")
    parser.add_argument("--already-cut", action="store_true")
    args = parser.parse_args(argv)

    target = CONTENT / f"{args.texture}.png"
    target.parent.mkdir(parents=True, exist_ok=True)
    step = common.Step(
        visual_id=args.visual_id,
        step="make-prop",
        tool={"name": "make_prop.py + rembg", "version": "1"},
        model=common.model_info(args.model),
        parameters={"height": args.height, "layer": args.layer},
        inputs=[args.image],
        outputs=[target],
        decision="angenommen",
        reason="Prop für die gemalte Scheibe (Schnellpfad, Basismodell vor LoRA)",
    )
    with common.record_step(step) as recorded:
        with tempfile.TemporaryDirectory() as temporary:
            source = args.image
            if not args.already_cut:
                source = Path(temporary) / "cut.png"
                subprocess.run(["rembg", "i", "-m", "birefnet-general", str(args.image), str(source)], check=True,
                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            image = Image.open(source).convert("RGBA")

        pixels = np.asarray(image)
        alpha = pixels[..., 3]
        ys, xs = np.nonzero(alpha > 8)
        if len(xs) == 0:
            raise SystemExit("Nach dem Freistellen ist nichts übrig.")
        left, top, right, bottom = xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
        cropped = image.crop((left, top, right, bottom))
        foot = foot_row(np.asarray(cropped)[..., 3])

        scale = args.height * OUTPUT_SCALE / max(foot, 1)
        size = (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale)))
        resized = cropped.resize(size, Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (size[0] + 2 * MARGIN, size[1] + 2 * MARGIN), (0, 0, 0, 0))
        canvas.paste(resized, (MARGIN, MARGIN))
        origin_y = (MARGIN + foot * scale) / canvas.height
        world_size = [round(canvas.width / OUTPUT_SCALE, 1), round(canvas.height / OUTPUT_SCALE, 1)]
        recorded.parameters.update({"world_size": world_size, "origin_y": round(origin_y, 4)})
        canvas.save(target)
        registry = json.loads(REGISTRY.read_text(encoding="utf-8"))
        entry = next((item for item in registry["visuals"] if item["id"] == args.visual_id), None)
        if entry is None:
            entry = {"id": args.visual_id}
            registry["visuals"].append(entry)
        entry.update({"kind": "prop", "palette": "world", "worldSize": world_size, "origin": [0.5, round(origin_y, 4)],
                      "layer": args.layer,
                      "clips": {"default": {"path": args.texture, "frameSize": [canvas.width, canvas.height], "frames": 1, "fps": 1, "loop": True}}})
        REGISTRY.write_text(compact_json(registry), encoding="utf-8")

        relative = f"{args.texture}.png"
        text = MGCB.read_text(encoding="utf-8")
        if f"#begin {relative}\n" not in text:
            text = text.rstrip("\n") + (
                f"\n\n#begin {relative}\n/importer:TextureImporter\n/processor:TextureProcessor\n"
                "/processorParam:ColorKeyColor=255,0,255,255\n/processorParam:ColorKeyEnabled=False\n"
                "/processorParam:GenerateMipmaps=True\n/processorParam:PremultiplyAlpha=True\n"
                "/processorParam:ResizeToPowerOfTwo=False\n/processorParam:MakeSquare=False\n"
                f"/processorParam:TextureFormat=Color\n/build:{relative}\n")
            MGCB.write_text(text, encoding="utf-8")

    print(f"{args.visual_id}: {canvas.width}×{canvas.height} px, Welt {world_size[0]}×{world_size[1]}, Fußpunkt {origin_y:.3f}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
