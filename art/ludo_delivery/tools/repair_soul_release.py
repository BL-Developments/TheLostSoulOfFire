#!/usr/bin/env python3
"""Build a clean fixed-grid sheet from the accepted Ludo soul-release reroll master.

The Ludo animation reroll retained a baked transparency-preview field. This repair
uses the transparent Ludo reroll master itself and deterministic nearest-neighbor
motion/fade, preserving the selected design without another credit-consuming call.
"""

from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
DELIVERY = ROOT / "art" / "ludo_delivery"
SOURCE = DELIVERY / "04_vfx/candidates/fx_soul_release_base_reroll.webp"
DESTINATION = DELIVERY / "04_vfx/fx_soul_release.png"


def with_alpha(image: Image.Image, multiplier: float) -> Image.Image:
    result = image.copy()
    alpha = result.getchannel("A").point(lambda value: round(value * multiplier))
    result.putalpha(alpha)
    return result


source = Image.open(SOURCE).convert("RGBA")
bounds = source.getchannel("A").getbbox()
if bounds is None:
    raise SystemExit("Ludo reroll master has no alpha content")
source = source.crop(bounds)

sheet = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
for index in range(1, 15):
    progress = (index - 1) / 13
    rise = round(30 * progress)
    size = round(50 + 22 * min(1, progress * 1.6))
    alpha = 1.0 if progress < 0.58 else max(0.08, 1 - (progress - 0.58) / 0.42)
    scale = min(size / source.width, size / source.height)
    sprite = source.resize(
        (max(1, round(source.width * scale)), max(1, round(source.height * scale))),
        Image.Resampling.NEAREST,
    )
    sprite = with_alpha(sprite, alpha)
    frame = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    x = (128 - sprite.width) // 2
    y = 69 - sprite.height // 2 - rise
    if progress > 0.45:
        echo = with_alpha(sprite, alpha * 0.18)
        frame.alpha_composite(echo, (x, y + 8))
    frame.alpha_composite(sprite, (x, y))
    sheet.alpha_composite(frame, ((index % 4) * 128, (index // 4) * 128))

DESTINATION.parent.mkdir(parents=True, exist_ok=True)
sheet.save(DESTINATION, "PNG", optimize=True)
