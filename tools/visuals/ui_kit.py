#!/usr/bin/env python3
"""Interface pieces in the Warden-iron style: blackened, hammered iron lit from the upper left,
with the violet Death Flame bouncing on lower edges and soul-white enamel inlays.

    tools/visuals/.venv/bin/python tools/visuals/ui_kit.py [--out DIR] [--preview FILE]

Every piece is drawn from signed distance fields at 4x supersampling and reduced to the output
grid (1.5 px per logical unit), so it lands 1:1 on screen pixels. Slices and sizes are mirrored in
src/TheLostSoulOfFire/Rendering/UiKit.cs; change both together.

Pieces (output pixels):
  panel.png        72x72  nine-slice, corner 24: iron frame, chamfered corners with studs, dark fill
  panel_inlay.png  72x72  same slicing: the enamel line inside the frame, white (tinted in game)
  panel_glow.png   96x96  nine-slice, corner 36: soft outer glow of the frame, white (additive)
  key.png          36x36  nine-slice, corner 12: keycap, iron face with a raised rim
  bar.png          36x24  three-slice, caps 15: iron channel with pointed caps; track 6..18 inside
  bar_fill.png      4x12  vertical enamel gradient, white (tinted)
  gem.png          66x66  soul medallion: iron diamond setting with rivets, empty socket
  gem_core.png     66x66  the faceted soul crystal for the socket, white (tinted, pulses)
  coin.png         30x30  Geld: struck brass coin with the Warden diamond
  ember.png        30x30  Glut: a coal with glowing cracks
  divider.png     180x18  three parts of 60: left fade, centre ornament, right fade
"""
from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "src" / "TheLostSoulOfFire" / "Content" / "Textures" / "Ui"
SS = 4  # supersampling

IRON = np.array([44, 41, 50], float) / 255
IRON_DARK = np.array([9, 8, 12], float) / 255
FILL = np.array([14, 11, 21], float) / 255
EMBER = np.array([255, 128, 52], float) / 255
DEATH_FLAME = np.array([145, 71, 255], float) / 255  # the violet Death Flame lights the iron from below
BRASS = np.array([196, 150, 78], float) / 255
LIGHT = np.array([-0.55, -0.62, 0.56])
LIGHT /= np.linalg.norm(LIGHT)


# ----------------------------------------------------------------------------- fields

def grid(width: int, height: int) -> tuple[np.ndarray, np.ndarray]:
    """Pixel-centre coordinates in output pixels at supersampled density."""
    ys, xs = np.mgrid[0:height * SS, 0:width * SS].astype(float)
    return (xs + 0.5) / SS, (ys + 0.5) / SS


def chamfer_box(x, y, cx, cy, hw, hh, chamfer):
    """Signed distance to a rectangle with 45-degree chamfered corners (negative inside)."""
    px, py = np.abs(x - cx), np.abs(y - cy)
    box = np.maximum(px - hw, py - hh)
    cut = (px + py - (hw + hh - chamfer)) / np.sqrt(2)
    return np.maximum(box, cut)


def diamond(x, y, cx, cy, r):
    return (np.abs(x - cx) + np.abs(y - cy) - r) / np.sqrt(2)


def circle(x, y, cx, cy, r):
    return np.hypot(x - cx, y - cy) - r


def coverage(d, soft=0.5):
    """Antialiased inside-ness of a distance field (in output pixels)."""
    return np.clip(0.5 - d / (2 * soft), 0, 1)


def noise(width, height, scale, seed, octaves=3):
    """Smooth value noise in 0..1 at supersampled density."""
    rng = np.random.default_rng(seed)
    total = np.zeros((height * SS, width * SS))
    amplitude, weight = 1.0, 0.0
    for octave in range(octaves):
        cells = max(2, int(max(width, height) / scale * 2 ** octave))
        coarse = rng.random((cells + 1, cells + 1))
        image = Image.fromarray((coarse * 255).astype(np.uint8)).resize((width * SS, height * SS), Image.BICUBIC)
        total += np.asarray(image, float) / 255 * amplitude
        weight += amplitude
        amplitude *= 0.5
    return total / weight


def streaks(width, height, seed, along="x"):
    """Grain that only varies across one axis, so a stretched slice looks like drawn iron."""
    rng = np.random.default_rng(seed)
    n = height * SS if along == "x" else width * SS
    line = np.convolve(rng.random(n), np.ones(SS * 2) / (SS * 2), mode="same")
    line = (line - line.min()) / (np.ptp(line) + 1e-9)
    return np.tile(line[:, None], (1, width * SS)) if along == "x" else np.tile(line[None, :], (height * SS, 1))


def bevel_light(height_field, strength=1.0):
    """Lambert plus a little specular for a height field (supersampled pixels)."""
    gy, gx = np.gradient(height_field * strength * SS)
    normal = np.stack([-gx, -gy, np.ones_like(gx)], axis=-1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    lambert = np.clip(normal @ LIGHT, 0, 1)
    half = LIGHT + np.array([0, 0, 1.0])
    half /= np.linalg.norm(half)
    specular = np.clip(normal @ half, 0, 1) ** 14
    bounce = np.clip(normal @ np.array([0.1, 0.75, 0.35]), 0, 1)  # warm light from below
    return lambert, specular, bounce


def reduce(rgba: np.ndarray) -> Image.Image:
    """Premultiplied box reduction from the supersampled grid to output pixels."""
    rgb, alpha = rgba[..., :3], rgba[..., 3:4]
    premultiplied = np.concatenate([rgb * alpha, alpha], axis=-1)
    h, w = rgba.shape[0] // SS, rgba.shape[1] // SS
    small = premultiplied.reshape(h, SS, w, SS, 4).mean(axis=(1, 3))
    a = small[..., 3:4]
    colour = np.where(a > 1e-6, small[..., :3] / np.maximum(a, 1e-6), 0)
    out = np.concatenate([colour, a], axis=-1)
    return Image.fromarray((np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA")


def iron_shade(height_field, inside, grain, seed_tint=0.0, bounce_amount=0.22):
    lambert, specular, bounce = bevel_light(height_field)
    base = IRON * (0.82 + 0.3 * (grain - 0.5) + seed_tint)[..., None]
    colour = IRON_DARK + base * (0.15 + 1.55 * lambert[..., None] ** 1.6)
    colour += specular[..., None] * np.array([1.0, 0.93, 0.86]) * 0.75
    colour += bounce[..., None] * DEATH_FLAME * bounce_amount * (1 - lambert[..., None])
    return np.concatenate([np.clip(colour, 0, 1), inside[..., None]], axis=-1)


def band_height(d_outer, inset, width, bevel):
    """Height of a band between inset and inset+width inside an outline: a low dome across its
    width (so the whole band carries a light gradient) with steeper rounded edges."""
    e = np.minimum(-d_outer - inset, inset + width + d_outer)  # distance into the band
    across = np.clip(e / (width / 2), 0, 1)
    dome = np.sin(across * np.pi / 2)
    return np.clip(e / bevel, 0, 1) ** 0.6 * (0.55 + 0.45 * dome), e


# ----------------------------------------------------------------------------- pieces

PANEL = 72
PANEL_CORNER = 24
CHAMFER = 11
BAND = 6


def panel_outline(x, y, size, margin=0.0):
    half = size / 2
    return chamfer_box(x, y, half, half, half - margin, half - margin, CHAMFER - margin * 0.41)


def panel() -> Image.Image:
    x, y = grid(PANEL, PANEL)
    d = panel_outline(x, y, PANEL, 0.5)
    rim = coverage(d)
    h, e = band_height(d, 0.0, BAND, 2.2)
    grain = np.where(np.abs(x - PANEL / 2) > np.abs(y - PANEL / 2),
                     streaks(PANEL, PANEL, 3, along="y"), streaks(PANEL, PANEL, 4, along="x"))
    hammer = noise(PANEL, PANEL, 9, 7)
    # Studs: small diamonds on the four chamfers.
    studs = np.full_like(x, np.inf)
    for sx in (1, -1):
        for sy in (1, -1):
            cx = PANEL / 2 + sx * (PANEL / 2 - 0.5 - CHAMFER * 0.5 + 0.2)
            cy = PANEL / 2 + sy * (PANEL / 2 - 0.5 - CHAMFER * 0.5 + 0.2)
            studs = np.minimum(studs, diamond(x, y, cx, cy, 4.6))
    stud_h = np.clip(-studs / 2.2, 0, 1) ** 0.6 * 1.7
    height_field = np.maximum(h * (0.85 + 0.25 * hammer), stud_h)
    band = coverage(-e)  # inside the band
    iron = iron_shade(height_field, band, grain * 0.6 + hammer * 0.4)
    # Fill: dark ash with an inner shadow along the frame.
    inner = np.clip((-d - BAND) / 10.0, 0, 1)
    fill_colour = FILL * (0.75 + 0.25 * inner[..., None])
    fill_alpha = 0.9 - 0.06 * inner
    fill = np.concatenate([fill_colour, (fill_alpha * coverage(d + BAND - 0.5))[..., None]], axis=-1)
    # A thin dark outer line keeps the frame readable on bright ground.
    out = np.zeros(x.shape + (4,))
    out[..., 3] = rim
    out[..., :3] = IRON_DARK * 0.6
    out = over(out, fill)
    out = over(out, iron)
    stud_cover = coverage(studs)
    stud = iron_shade(stud_h * 1.4, stud_cover, hammer, 0.12)
    return reduce(over(out, stud))


def panel_inlay() -> Image.Image:
    x, y = grid(PANEL, PANEL)
    d = panel_outline(x, y, PANEL, 0.5)
    line = coverage(np.abs(d + BAND + 2.6) - 0.85)
    out = np.zeros(x.shape + (4,))
    out[..., :3] = 1
    out[..., 3] = line
    return reduce(out)


def panel_glow() -> Image.Image:
    size, pad = PANEL + 24, 12
    x, y = grid(size, size)
    d = chamfer_box(x, y, size / 2, size / 2, PANEL / 2 - 0.5, PANEL / 2 - 0.5, CHAMFER)
    glow = np.exp(-np.clip(d, 0, None) / 4.2) * coverage(-d + 0.5)  # outside only
    glow += np.exp(-np.abs(d + 1.5) / 2.0) * 0.6
    out = np.zeros(x.shape + (4,))
    out[..., :3] = 1
    out[..., 3] = np.clip(glow * 0.9, 0, 1)
    del pad
    return reduce(out)


KEY = 36
KEY_CORNER = 12


def key() -> Image.Image:
    x, y = grid(KEY, KEY)
    d = chamfer_box(x, y, KEY / 2, KEY / 2, KEY / 2 - 0.5, KEY / 2 - 0.5, 6)
    hammer = noise(KEY, KEY, 6, 11)
    rim_h, rim_e = band_height(d, 0, 4.5, 1.8)
    face = np.clip((-d - 4.5) / 6, 0, 1)
    height_field = np.maximum(rim_h, 0.55 + 0.05 * face)
    lambert, specular, bounce = bevel_light(height_field * 1.2)
    base = np.array([50, 46, 56], float) / 255 * (0.85 + 0.25 * hammer)[..., None]
    colour = IRON_DARK + base * (0.4 + 0.95 * lambert[..., None]) + specular[..., None] * 0.25
    colour += bounce[..., None] * DEATH_FLAME * 0.15
    colour = np.where((-d > 4.5)[..., None], colour * 0.78, colour)  # sunken face
    out = np.concatenate([np.clip(colour, 0, 1), coverage(d)[..., None]], axis=-1)
    return reduce(out)


BAR_W, BAR_H, BAR_CAP = 36, 24, 15


def bar() -> Image.Image:
    x, y = grid(BAR_W, BAR_H)
    cy = BAR_H / 2
    # Channel: a long box with pointed (diamond) caps at both ends.
    body = np.maximum(np.abs(y - cy) - 7.5, np.maximum(BAR_CAP - 3 - x, x - (BAR_W - BAR_CAP + 3)))
    left = (np.abs(x - (BAR_CAP - 3)) * 0.0 + (np.abs(y - cy) + (BAR_CAP - 3 - x)) - 9.0) / np.sqrt(2)
    right = (np.abs(y - cy) + (x - (BAR_W - BAR_CAP + 3)) - 9.0) / np.sqrt(2)
    left = np.where(x < BAR_CAP - 3, left, np.inf)
    right = np.where(x > BAR_W - BAR_CAP + 3, right, np.inf)
    outline = np.minimum(np.minimum(np.where((x >= BAR_CAP - 3) & (x <= BAR_W - BAR_CAP + 3), body, np.inf), left), right)
    track = np.maximum(np.abs(y - cy) - 6.0, np.maximum(BAR_CAP - x, x - (BAR_W - BAR_CAP)))
    grain = streaks(BAR_W, BAR_H, 21, along="x")
    h = np.clip(-outline / 2.0, 0, 1) ** 0.6
    iron = iron_shade(h, coverage(outline), grain, bounce_amount=0.3)
    # Sunken track: nearly black, receives the fill in game.
    hollow = np.concatenate([np.broadcast_to(FILL * 0.6, x.shape + (3,)), coverage(track + 0.3)[..., None]], axis=-1)
    return reduce(over(iron, hollow))


def bar_fill() -> Image.Image:
    x, y = grid(4, 12)
    t = y / 12
    shade = 0.62 + 0.38 * np.cos((t - 0.28) * np.pi * 0.9) ** 2
    shade = np.where(t < 0.12, 1.0, shade)
    shade = np.where(t > 0.84, shade * 0.72, shade)
    out = np.zeros(x.shape + (4,))
    out[..., :3] = shade[..., None]
    out[..., 3] = 1
    return reduce(out)


GEM = 66


def gem() -> Image.Image:
    x, y = grid(GEM, GEM)
    c = GEM / 2
    outer = diamond(x, y, c, c, 31)
    socket = diamond(x, y, c, c, 19)
    hammer = noise(GEM, GEM, 8, 31)
    setting = np.maximum(outer, -socket)
    h = np.clip(-setting / 3.2, 0, 1) ** 0.55
    # A cross bar behind the gem (the old HUD line), forged into the setting.
    crossbar = np.maximum(np.abs(y - c) - 2.0, np.abs(x - c) - 32.5)
    crossbar = np.maximum(crossbar, -outer - 0.5)  # only outside the diamond
    rivets = np.full_like(x, np.inf)
    for ox, oy in ((0, -24), (24, 0), (0, 24), (-24, 0)):
        rivets = np.minimum(rivets, circle(x, y, c + ox, c + oy, 2.3))
    rivet_h = np.clip(-rivets / 2.3, 0, 1) ** 0.5
    cross_h = np.clip(-crossbar / 1.6, 0, 1) ** 0.6 * 0.7
    height_field = np.maximum(np.maximum(h, rivet_h * 1.3), cross_h)
    cover = np.maximum(coverage(setting), np.maximum(coverage(rivets), coverage(crossbar)))
    iron = iron_shade(height_field * (0.9 + 0.2 * hammer), cover, hammer, bounce_amount=0.32)
    hollow = np.concatenate([np.broadcast_to(FILL * 0.5, x.shape + (3,)), coverage(socket)[..., None]], axis=-1)
    return reduce(over(hollow, iron))


def gem_core() -> Image.Image:
    x, y = grid(GEM, GEM)
    c = GEM / 2
    d = diamond(x, y, c, c, 18.2)
    dx, dy = x - c, y - c
    # Four facets of a pyramid plus a flat table: each facet catches the light differently.
    table = (np.abs(dx) + np.abs(dy)) < 7.5
    facet = np.select([dx <= 0, True], [np.where(dy <= 0, 0, 2), np.where(dy <= 0, 1, 3)])
    brightness = np.array([1.0, 0.8, 0.7, 0.5])[facet]
    brightness = np.where(table, 0.92, brightness)
    edge = np.exp(-np.abs(np.abs(dx) - np.abs(dy)) * 1.4) * 0.15 * ~table
    shade = np.clip(brightness + edge, 0, 1)
    out = np.zeros(x.shape + (4,))
    out[..., :3] = shade[..., None]
    out[..., 3] = coverage(d)
    return reduce(out)


ICON = 30


def coin() -> Image.Image:
    x, y = grid(ICON, ICON)
    c = ICON / 2
    d = circle(x, y, c, c, 12.6)
    rim_h = np.clip(np.minimum(-d, d + 3.2) / 1.2, 0, 1) ** 0.6
    emblem = diamond(x, y, c, c, 7.0)
    emblem_h = 0.4 + np.clip(-emblem / 4.6, 0, 1) * 0.55  # a struck pyramid: four lit facets
    face = 0.38
    height_field = np.maximum(np.maximum(rim_h, np.where(emblem < 0, emblem_h, 0)), np.where(-d > 3.2, face, 0))
    lambert, specular, bounce = bevel_light(height_field * 1.4)
    wear = noise(ICON, ICON, 5, 41)
    colour = BRASS * (0.25 + 0.95 * lambert[..., None]) * (0.85 + 0.25 * wear[..., None])
    colour += specular[..., None] * np.array([1.0, 0.92, 0.7]) * 0.6
    colour += bounce[..., None] * DEATH_FLAME * 0.1
    shadow = np.concatenate([np.zeros(x.shape + (3,)), (coverage(circle(x, y, c + 1.2, c + 1.6, 12.8), 1.4) * 0.5)[..., None]], axis=-1)
    body = np.concatenate([np.clip(colour, 0, 1), coverage(d)[..., None]], axis=-1)
    return reduce(over(shadow, body))


def ember() -> Image.Image:
    x, y = grid(ICON, ICON)
    c = ICON / 2
    angle = np.arctan2(y - c, x - c)
    lumpy = 11.2 + 1.4 * np.sin(angle * 3 + 0.6) + 0.9 * np.sin(angle * 5 + 2.1)
    d = np.hypot(x - c, (y - c) * 1.12) - lumpy
    n = noise(ICON, ICON, 4, 51, octaves=4)
    veins = noise(ICON, ICON, 3.2, 53, octaves=2)
    ridged = 1 - np.abs(2 * veins - 1)
    glow = np.clip((ridged - 0.86) / 0.1, 0, 1) * np.clip(-d / 2.0, 0, 1)
    core = np.clip(1 - np.hypot(x - c + 2, y - c + 2) / 9, 0, 1)
    h = np.clip(-d / 3.0, 0, 1) ** 0.5 * (0.8 + 0.4 * n)
    lambert, specular, _ = bevel_light(h * 1.6)
    coal = np.array([34, 26, 28], float) / 255 * (0.5 + 1.1 * lambert[..., None])
    hot = EMBER * (0.6 + 0.4 * core[..., None]) + np.array([1, 0.9, 0.55]) * (glow * core)[..., None] * 0.5
    colour = coal * (1 - glow[..., None]) + hot * glow[..., None]
    colour += EMBER * core[..., None] * 0.25
    halo = np.exp(-np.clip(d, 0, None) / 2.4) * coverage(-d) * 0.55
    out = np.concatenate([np.clip(colour, 0, 1), coverage(d)[..., None]], axis=-1)
    under = np.concatenate([np.broadcast_to(EMBER, x.shape + (3,)), halo[..., None]], axis=-1)
    return reduce(over(under, out))


DIV_W, DIV_H = 180, 18


def divider() -> Image.Image:
    x, y = grid(DIV_W, DIV_H)
    cy = DIV_H / 2
    out = np.zeros(x.shape + (4,))
    # Left and right thirds: a hairline that fades toward the outside.
    line = coverage(np.abs(y - cy) - 0.55)
    fade = np.where(x < 60, x / 60, np.where(x >= 120, (180 - x) / 60, 1.0)) ** 1.3
    # Centre: a diamond between two small studs.
    centre = np.minimum(diamond(x, y, 90, cy, 6.5), np.minimum(diamond(x, y, 76, cy, 2.4), diamond(x, y, 104, cy, 2.4)))
    hollow = diamond(x, y, 90, cy, 3.4)
    ornament = coverage(np.maximum(centre, -hollow))
    dot = coverage(diamond(x, y, 90, cy, 1.6))
    alpha = np.maximum(line * fade * np.where((x > 72) & (x < 108), 0, 1), np.maximum(ornament, dot))
    out[..., :3] = 1
    out[..., 3] = alpha
    return reduce(out)


def over(under: np.ndarray, top: np.ndarray) -> np.ndarray:
    a_top, a_under = top[..., 3:4], under[..., 3:4]
    a = a_top + a_under * (1 - a_top)
    rgb = (top[..., :3] * a_top + under[..., :3] * a_under * (1 - a_top)) / np.maximum(a, 1e-6)
    return np.concatenate([rgb, a], axis=-1)


PIECES = {
    "panel": panel, "panel_inlay": panel_inlay, "panel_glow": panel_glow, "key": key,
    "bar": bar, "bar_fill": bar_fill, "gem": gem, "gem_core": gem_core,
    "coin": coin, "ember": ember, "divider": divider,
}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", type=Path, default=OUT)
    parser.add_argument("--preview", type=Path, help="contact sheet, 3x enlarged")
    parser.add_argument("--preview-background", type=Path, help="screenshot to lay the contact sheet on")
    args = parser.parse_args()
    args.out.mkdir(parents=True, exist_ok=True)
    images = {}
    for name, make in PIECES.items():
        image = make()
        image.save(args.out / f"{name}.png")
        images[name] = image
        print(f"{name}.png {image.size[0]}x{image.size[1]}")
    if args.preview:
        background = Image.open(args.preview_background).convert("RGBA") if args.preview_background else None
        sheet = Image.new("RGBA", (420, 200), (60, 54, 66, 255))
        if background is not None:
            sheet = background.crop((0, 0, 420, 200))
        x, y, row = 6, 6, 0
        for image in images.values():
            if x + image.size[0] > sheet.size[0]:
                x, y, row = 6, y + row + 6, 0
            sheet.alpha_composite(image, (x, y))
            x += image.size[0] + 6
            row = max(row, image.size[1])
        sheet.resize((sheet.size[0] * 3, sheet.size[1] * 3), Image.NEAREST).save(args.preview)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
