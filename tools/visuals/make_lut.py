#!/usr/bin/env python3
"""Build a 32^3 colour lookup strip (1024 x 32) for an area's scene grade.

    tools/visuals/.venv/bin/python tools/visuals/make_lut.py arena --register
    tools/visuals/.venv/bin/python tools/visuals/make_lut.py shore --register

Each preset is a small, readable colour script: tint of shadows and highlights, saturation,
contrast and a lift of the blacks. Saturated Death Flame violet and Life Flame orange are
protected — the grade fades out on them — so flame colours read the same in every area
(VISUAL-ART-DIRECTION §7 and §11). --register writes the strip to
Content/Textures/Grading/lut_<preset>.png and the registry entry grade.<preset>.
"""
from __future__ import annotations

import argparse
import colorsys
import json
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import visuals_common as common  # noqa: E402

SIZE = 32
CONTENT = common.REPO_ROOT / "src" / "TheLostSoulOfFire" / "Content"
REGISTRY = CONTENT / "Visuals" / "registry.json"
MGCB = CONTENT / "Content.mgcb"

#: Colour scripts per area. Values are deliberately small; the painting carries the look.
PRESETS = {
    # Casting hall: cold steel shadows, soot-muted mids, a breath of brass in the highlights.
    # Durchgang 4 (Owner: too cinematic, not action): the pale lavender floor (sat ~0.2) sat inside
    # the flame protection and stayed pastel; protection now starts at real flame saturation, the
    # stone loses some lavender and gains depth, so fighters and flames stand out against it.
    "arena": {"shadow_tint": (0.92, 0.94, 1.10), "highlight_tint": (1.05, 1.0, 0.92), "saturation": 0.74,
              "contrast": 1.22, "black_lift": 0.012, "gamma": 1.12, "protect_from": 0.28},
    # Shore: slate-blue night, salt-white highlights, mist lifting the darks a little.
    "shore": {"shadow_tint": (0.88, 0.97, 1.12), "highlight_tint": (0.97, 1.0, 1.04), "saturation": 0.8,
              "contrast": 0.96, "black_lift": 0.03, "gamma": 1.0},
}


def flame_protection(rgb: np.ndarray, violet_from: float = 0.1) -> np.ndarray:
    """1 where a colour is a saturated flame colour (violet 250-310 deg or orange 15-50 deg).
    `violet_from` is the saturation where violet protection begins (fully on 0.15 above)."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    maximum = rgb.max(axis=-1)
    minimum = rgb.min(axis=-1)
    delta = np.maximum(maximum - minimum, 1e-6)
    saturation = np.where(maximum > 0, (maximum - minimum) / np.maximum(maximum, 1e-6), 0)
    hue = np.where(maximum == r, ((g - b) / delta) % 6, np.where(maximum == g, (b - r) / delta + 2, (r - g) / delta + 4)) * 60
    violet = (hue >= 250) & (hue <= 310)
    orange = (hue >= 15) & (hue <= 50)
    bright = np.clip((maximum - 0.25) / 0.2, 0, 1)
    # Death Flame runs from deep violet to near white, so violet is protected from low saturation on.
    violet_strength = np.clip((saturation - violet_from) / 0.15, 0, 1) * bright
    orange_strength = np.clip((saturation - 0.35) / 0.25, 0, 1) * bright
    return np.where(violet, violet_strength, np.where(orange, orange_strength, 0.0))


def grade(rgb: np.ndarray, preset: dict) -> np.ndarray:
    luminance = (rgb * [0.299, 0.587, 0.114]).sum(axis=-1, keepdims=True)
    out = luminance + (rgb - luminance) * preset["saturation"]
    shadow = np.array(preset["shadow_tint"])
    highlight = np.array(preset["highlight_tint"])
    weight = np.clip(luminance, 0, 1)
    out = out * (shadow * (1 - weight) + highlight * weight)
    out = (out - 0.5) * preset["contrast"] + 0.5
    out = np.clip(out, 0, 1) ** preset["gamma"]
    out = preset["black_lift"] + out * (1 - preset["black_lift"])
    keep = flame_protection(rgb, preset.get("protect_from", 0.1))[..., None]
    return np.clip(out * (1 - keep) + rgb * keep, 0, 1)


def strip(preset: dict) -> Image.Image:
    steps = np.arange(SIZE) / (SIZE - 1)
    image = np.zeros((SIZE, SIZE * SIZE, 3))
    for blue in range(SIZE):
        red, green = np.meshgrid(steps, steps)
        rgb = np.stack([red, green, np.full_like(red, steps[blue])], axis=-1)
        image[:, blue * SIZE:(blue + 1) * SIZE] = grade(rgb, preset)
    return Image.fromarray(np.round(image * 255).astype(np.uint8), "RGB")


def compact_json(value: object) -> str:
    text = json.dumps(value, indent=2) + "\n"
    return re.sub(r"\[\s*([-0-9.]+),\s*([-0-9.]+)\s*\]", r"[\1, \2]", text)


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("preset", choices=sorted(PRESETS))
    parser.add_argument("--register", action="store_true")
    parser.add_argument("--out", type=Path)
    args = parser.parse_args(argv)

    texture = f"Textures/Grading/lut_{args.preset}"
    target = args.out or CONTENT / f"{texture}.png"
    visual_id = f"grade.{args.preset}"
    step = common.Step(
        visual_id=visual_id, step="make-lut", tool={"name": "make_lut.py", "version": "1"},
        model={"name": "procedural colour script", "license": "CC0 (eigene Ausgabe)"},
        parameters=PRESETS[args.preset], outputs=[target], decision="angenommen",
        reason="Bereichs-Grading der Scheibe; Flammenfarben geschützt")
    with common.record_step(step):
        target.parent.mkdir(parents=True, exist_ok=True)
        strip(PRESETS[args.preset]).save(target)
        if args.register:
            registry = json.loads(REGISTRY.read_text(encoding="utf-8"))
            registry["visuals"] = [item for item in registry["visuals"] if item["id"] != visual_id]
            registry["visuals"].append({"id": visual_id, "kind": "grade", "palette": "world", "worldSize": [1024, 32], "origin": [0, 0],
                                        "clips": {"default": {"path": texture, "frameSize": [1024, 32], "frames": 1, "fps": 1, "loop": True}}})
            REGISTRY.write_text(compact_json(registry), encoding="utf-8")
            relative = f"{texture}.png"
            text = MGCB.read_text(encoding="utf-8")
            if f"#begin {relative}\n" not in text:
                text = text.rstrip("\n") + (
                    f"\n\n#begin {relative}\n/importer:TextureImporter\n/processor:TextureProcessor\n"
                    "/processorParam:ColorKeyColor=255,0,255,255\n/processorParam:ColorKeyEnabled=False\n"
                    "/processorParam:GenerateMipmaps=False\n/processorParam:PremultiplyAlpha=False\n"
                    "/processorParam:ResizeToPowerOfTwo=False\n/processorParam:MakeSquare=False\n"
                    f"/processorParam:TextureFormat=Color\n/build:{relative}\n")
                MGCB.write_text(text, encoding="utf-8")
    print(f"geschrieben: {common.relative(target)}" + (f", Registry {visual_id}" if args.register else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
