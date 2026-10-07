#!/usr/bin/env python3
"""Matter on the floor: the shattered ground of a Devourer's slam and the dust it throws up.

    tools/visuals/.venv/bin/python tools/visuals/ground_kit.py [--preview DIR]

Writes premultiplied RGBA (Content.mgcb imports them with PremultiplyAlpha=False):

- ground_shatter.png    the broken floor after the blow: a pressed crater of tilted slabs, radial
                        fissures with a few spider-web arcs between them, rubble. Dark matter with
                        a lit lip on each fissure (light from the upper left, like every scene).
- ground_fissures.png   the same fissures as white light, 8 frames (4 x 2): in the first seven the
                        soul light the Devourer drives into the floor seeps up along the main
                        fissures and creeps out to the edge of its blow; the eighth lights every
                        crack for the moment the floor breaks. Tinted violet in game.
- dust_puffs.png        four soft, ragged clouds of stone dust (2 x 2), pale; tinted in game.

The crack field is one seeded layout, so the fissures of the windup and the shattered floor are
the same cracks. The blow's radius sits at EDGE px from the centre of a SIZE canvas; the game
scales by radius / EDGE (Rendering/GroundImpacts.cs). Circles are not squashed: the floor maps
world units 1:1 onto the picture.
"""
from __future__ import annotations

import argparse
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from vfx_kit import Noise, blur

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "src" / "TheLostSoulOfFire" / "Content" / "Textures" / "Effects"
SIZE = 512
EDGE = 232.0  # px from the centre where the blow ends
SS = 4  # drawing supersampling
GROWTH_FRAMES = 8
SEED = 4207


# ----------------------------------------------------------------------------- layout

class Segment:
    __slots__ = ("a", "b", "width", "r", "main")

    def __init__(self, a, b, width, main=False):
        self.a, self.b, self.width, self.main = a, b, width, main
        self.r = math.hypot(b[0], b[1])  # reach of the segment's far end


def crack_layout(rng: np.random.Generator):
    """Radial fissures from the crater rim to the edge with side branches, a few web arcs between
    them, and the crater's slab seeds."""
    segments: list[Segment] = []
    crater = EDGE * 0.24
    count = 9
    base = rng.uniform(0, math.tau)
    main_angles: list[float] = []

    def walk(start, heading, length, width0, width1, wander, branchy, main=False):
        x, y = start
        travelled = 0.0
        step = 8.0
        while travelled < length:
            # Stone breaks in angular runs: a sharp kink per step, pulled back toward radial.
            heading += rng.normal(0, wander)
            outward = math.atan2(y, x)
            heading += (((outward - heading + math.pi) % math.tau) - math.pi) * 0.22
            nx, ny = x + math.cos(heading) * step, y + math.sin(heading) * step
            if math.hypot(nx, ny) > EDGE * rng.uniform(0.985, 1.02):
                break
            t = travelled / length
            width = width0 + (width1 - width0) * t
            segments.append(Segment((x, y), (nx, ny), width, main))
            if branchy and t > 0.08 and rng.random() < 0.08:
                side = rng.choice([-1.0, 1.0])
                walk((nx, ny), heading + side * rng.uniform(0.35, 0.75), length * rng.uniform(0.18, 0.4) * (1 - t * 0.5),
                     width * 0.7, 0.5, wander * 1.2, False)
            x, y = nx, ny
            travelled += step

    for i in range(count):
        angle = base + i * math.tau / count + rng.uniform(-0.22, 0.22)
        start = (math.cos(angle) * crater * rng.uniform(0.85, 1.05), math.sin(angle) * crater * rng.uniform(0.85, 1.05))
        reach = EDGE * (0.97 if i % 3 else rng.uniform(0.7, 0.85))
        walk(start, angle, reach - crater, rng.uniform(7.0, 9.5), 1.0, 0.3, True, True)
        main_angles.append(angle)

    # Spider-web arcs: short jagged pieces between neighbouring fissures, never a full ring.
    for radius, pieces in ((EDGE * 0.42, 5), (EDGE * 0.68, 4)):
        for _ in range(pieces):
            a0 = rng.uniform(0, math.tau)
            span = rng.uniform(0.25, 0.55)
            n = max(3, int(radius * span / 6))
            prev = None
            for k in range(n + 1):
                a = a0 + span * k / n
                r = radius + rng.normal(0, 3.0)
                point = (math.cos(a) * r, math.sin(a) * r)
                if prev is not None:
                    taper = math.sin(math.pi * k / n)
                    segments.append(Segment(prev, point, 0.6 + 1.6 * taper))
                prev = point

    slabs = []
    for _ in range(16):
        a = rng.uniform(0, math.tau)
        r = crater * math.sqrt(rng.uniform(0.0, 1.0)) * 1.05
        slabs.append((math.cos(a) * r, math.sin(a) * r, rng.uniform(0, math.tau)))
    return segments, slabs, sorted(a % math.tau for a in main_angles)


# ----------------------------------------------------------------------------- drawing helpers

def to_px(point, size=SIZE):
    return ((point[0] + size / 2) * SS, (point[1] + size / 2) * SS)


def draw_segments(segments, keep=lambda s: True, width_scale=1.0) -> np.ndarray:
    """Rasterises the fissures as tapered capsules at SS x, returns coverage at SIZE."""
    image = Image.new("L", (SIZE * SS, SIZE * SS), 0)
    draw = ImageDraw.Draw(image)
    for s in segments:
        if not keep(s):
            continue
        w = max(1.0, s.width * width_scale * SS)
        a, b = to_px(s.a), to_px(s.b)
        draw.line([a, b], fill=255, width=int(round(w)))
        r = w / 2
        draw.ellipse([b[0] - r, b[1] - r, b[0] + r, b[1] + r], fill=255)
    field = np.asarray(image, dtype=float) / 255.0
    return field.reshape(SIZE, SS, SIZE, SS).mean(axis=(1, 3))


def shift(field: np.ndarray, dx: int, dy: int) -> np.ndarray:
    out = np.zeros_like(field)
    h, w = field.shape
    ys = slice(max(0, dy), h + min(0, dy))
    xs = slice(max(0, dx), w + min(0, dx))
    yd = slice(max(0, -dy), h + min(0, -dy))
    xd = slice(max(0, -dx), w + min(0, -dx))
    out[ys, xs] = field[yd, xd]
    return out


def grid():
    ys, xs = np.mgrid[0:SIZE, 0:SIZE].astype(float)
    return xs + 0.5 - SIZE / 2, ys + 0.5 - SIZE / 2


def over(dst_rgb, dst_a, rgb, a):
    """Premultiplied 'over': a layer of colour `rgb` (straight) with coverage `a` onto dst."""
    a = np.clip(a, 0, 1)[..., None]
    dst_rgb = np.asarray(rgb)[None, None, :] * a + dst_rgb * (1 - a) if np.ndim(rgb) == 1 else rgb * a + dst_rgb * (1 - a)
    dst_a = a[..., 0] + dst_a * (1 - a[..., 0])
    return dst_rgb, dst_a


# ----------------------------------------------------------------------------- images

def shatter(segments, slabs, angles, rng) -> np.ndarray:
    x, y = grid()
    r = np.hypot(x, y)
    noise = Noise(SEED + 3, 48)
    grain = noise.fbm(x / 9 + 50, y / 9 + 50, 4)
    rgb = np.zeros((SIZE, SIZE, 3))
    alpha = np.zeros((SIZE, SIZE))
    dark = np.array([0.035, 0.03, 0.05])
    lit = np.array([0.78, 0.74, 0.82])
    dust = np.array([0.62, 0.58, 0.62])

    # Pulverised stone: a ragged pale dusting over the inner floor, thinning out.
    dust_mask = np.clip(1 - r / (EDGE * 0.78), 0, 1) ** 1.2 * np.clip((grain - 0.35) * 2.2, 0, 1)
    rgb, alpha = over(rgb, alpha, dust, dust_mask * 0.22)

    # Wedges: between the radial fissures the floor broke into plates whose inner ends heaved up
    # or sank; each wedge takes a tilt, strongest at the crater rim, gone by half the radius.
    theta = np.arctan2(y, x) % math.tau
    sector = np.searchsorted(np.array(angles), theta) % len(angles)
    tilts = rng.uniform(-1, 1, len(angles))
    heave = np.clip(1 - (r - EDGE * 0.22) / (EDGE * 0.38), 0, 1) * (r > EDGE * 0.2)
    wedge = tilts[sector] * heave
    rgb, alpha = over(rgb, alpha, lit, np.clip(wedge, 0, 1) * 0.2)
    rgb, alpha = over(rgb, alpha, dark, np.clip(-wedge, 0, 1) * 0.32)

    # Crater: the floor pressed down. The upper-left inner wall falls into shadow, the lower-right
    # one catches the light; a raised lip outside, lit on the upper left.
    crater = EDGE * 0.26
    inside = np.clip(1 - r / crater, 0, 1)
    toward_light = -(x + y) / (np.hypot(x, y) + 1e-6) / math.sqrt(2)  # 1 on the upper-left side
    wall = np.clip(r / crater, 0, 1) ** 1.5 * (r < crater)
    rgb, alpha = over(rgb, alpha, dark, np.clip(inside ** 0.6 * 0.45 + wall * np.clip(toward_light, 0, 1) * 0.35, 0, 0.8))
    rgb, alpha = over(rgb, alpha, lit, wall * np.clip(-toward_light, 0, 1) * 0.22)
    lip = np.exp(-((r - crater * 1.12) / (crater * 0.12)) ** 2)
    rgb, alpha = over(rgb, alpha, lit, lip * np.clip(toward_light, 0, 1) * 0.2)
    rgb, alpha = over(rgb, alpha, dark, lip * np.clip(-toward_light, 0, 1) * 0.3)

    # Slabs: Voronoi cells in the crater, each tilted (a gradient across it), dark gaps between.
    seeds = np.array([[s[0], s[1]] for s in slabs])
    d = np.stack([np.hypot(x - sx, y - sy) for sx, sy in seeds])
    order = np.sort(d, axis=0)
    nearest = np.argmin(d, axis=0)
    gap = np.clip(1 - (order[1] - order[0]) / 2.6, 0, 1)
    in_slabs = np.clip((crater * 1.05 - r) / 6, 0, 1)
    tilt = np.zeros_like(r)
    for index, (sx, sy, angle) in enumerate(slabs):
        cell = nearest == index
        along = ((x - sx) * math.cos(angle) + (y - sy) * math.sin(angle)) / 14.0
        tilt = np.where(cell, np.clip(along, -1, 1) * rng.uniform(0.4, 1.0), tilt)
    rgb, alpha = over(rgb, alpha, lit, np.clip(tilt, 0, 1) * 0.28 * in_slabs)
    rgb, alpha = over(rgb, alpha, dark, np.clip(-tilt, 0, 1) * 0.4 * in_slabs)
    rgb, alpha = over(rgb, alpha, dark, gap * in_slabs * 0.95)

    # Fissures: soft occlusion around them, the dark groove, a lit lip on the lower right.
    crack = draw_segments(segments)
    occlusion = blur(crack, 3.0)
    rgb, alpha = over(rgb, alpha, dark, np.clip(occlusion * 0.55, 0, 0.5))
    lip_lit = np.clip(shift(crack, 1, 1) * 0.8 + shift(crack, 2, 2) * 0.4 - crack * 1.4, 0, 1)
    rgb, alpha = over(rgb, alpha, lit, lip_lit * 0.38)
    rgb, alpha = over(rgb, alpha, dark, np.clip(crack * 1.15, 0, 0.96))

    # Rubble: small stones thrown about, denser near the centre, each lit upper left.
    stones = Image.new("L", (SIZE * SS, SIZE * SS), 0)
    lights = Image.new("L", (SIZE * SS, SIZE * SS), 0)
    ds, dl = ImageDraw.Draw(stones), ImageDraw.Draw(lights)
    for _ in range(140):
        rr = EDGE * (rng.random() ** 1.6) * 1.02
        a = rng.uniform(0, math.tau)
        cx, cy = to_px((math.cos(a) * rr, math.sin(a) * rr))
        s = rng.uniform(1.0, 3.4 - 1.6 * rr / EDGE) * SS
        sy = s * rng.uniform(0.55, 0.9)
        ds.ellipse([cx - s, cy - sy, cx + s, cy + sy], fill=255)
        dl.ellipse([cx - s * 0.9, cy - sy * 1.0, cx + s * 0.4, cy + sy * 0.15], fill=255)
    stone = np.asarray(stones, dtype=float).reshape(SIZE, SS, SIZE, SS).mean(axis=(1, 3)) / 255
    light = np.asarray(lights, dtype=float).reshape(SIZE, SS, SIZE, SS).mean(axis=(1, 3)) / 255
    shadow = np.clip(shift(stone, 1, 1) - stone, 0, 1)
    rgb, alpha = over(rgb, alpha, dark, shadow * 0.6)
    rgb, alpha = over(rgb, alpha, np.array([0.3, 0.28, 0.33]), stone * 0.95)
    rgb, alpha = over(rgb, alpha, lit, light * stone * 0.55)

    # Nothing reaches past the blow: fade the outermost rim of everything.
    fade = np.clip((EDGE * 1.06 - r) / (EDGE * 0.08), 0, 1)
    rgb *= fade[..., None]
    alpha *= fade
    return np.concatenate([np.clip(rgb, 0, 1), np.clip(alpha, 0, 1)[..., None]], axis=-1)


def fissure_frame(segments, progress: float, final: bool) -> np.ndarray:
    """Light seeping up through the fissures, out to `progress` of the blow's radius. While the
    blow gathers only the main fissures carry it (a seam of light under the stone, soft, not a
    bolt); the last frame, used when the floor breaks, lights every crack. The front is hottest."""
    x, y = grid()
    r = np.hypot(x, y)
    front = EDGE * progress
    keep = (lambda s: True) if final else (lambda s: s.main and s.r <= front + 1.0)
    reach = draw_segments(segments, keep=keep, width_scale=0.8 if final else 1.1)
    heat = 0.6 + 0.4 * np.exp(-((r - front) / 30.0) ** 2)
    if final:
        core = reach
        halo = blur(core, 3.0) * 0.7 + blur(core, 9.0) * 0.5
        light = np.clip(core * heat + halo * heat * 0.6, 0, 1)
        alpha = np.clip(core * 0.5, 0, 1)
    else:
        seam = blur(reach, 1.6)
        halo = blur(reach, 7.0) * 0.9 + blur(reach, 16.0) * 0.7
        light = np.clip(seam * 0.55 * heat + halo * heat * 0.75, 0, 1)
        alpha = np.clip(seam * 0.12, 0, 1)
    pool = np.exp(-(r / (EDGE * 0.1)) ** 2) * (0.1 + 0.2 * progress)
    light = np.clip(light + pool * 0.5, 0, 1)
    return np.stack([light, light, light, alpha], axis=-1)


def dust_puff(seed: int, size: int = 128) -> np.ndarray:
    ys, xs = np.mgrid[0:size, 0:size].astype(float)
    x, y = (xs + 0.5 - size / 2) / (size / 2), (ys + 0.5 - size / 2) / (size / 2)
    noise = Noise(seed, 32)
    n = noise.fbm(xs / 14 + seed, ys / 14, 5)
    warp = noise.fbm(xs / 30 + 7, ys / 30 + 3, 3) - 0.5
    r = np.hypot(x + warp * 0.35, y * 1.15 + warp * 0.25)
    body = np.clip(1 - r, 0, 1) ** 1.4
    density = np.clip(body * (0.45 + n * 0.9) - 0.12, 0, 1)
    density = blur(density, 1.5)
    # Lit on the upper left, a little denser and darker underneath.
    shade = 0.78 + 0.22 * np.clip(-(x + y) * 0.7, -1, 1)
    rgb = np.stack([shade * 0.92, shade * 0.88, shade * 0.95], axis=-1) * density[..., None]
    return np.concatenate([rgb, density[..., None]], axis=-1)


def tile(frames: list[np.ndarray], columns: int) -> np.ndarray:
    size = frames[0].shape[0]
    rows = math.ceil(len(frames) / columns)
    out = np.zeros((rows * size, columns * size, 4))
    for index, frame in enumerate(frames):
        r, c = divmod(index, columns)
        out[r * size:(r + 1) * size, c * size:(c + 1) * size] = frame
    return out


def save(array: np.ndarray, path: Path) -> None:
    Image.fromarray((np.clip(array, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(path)
    print(f"{path.name} {array.shape[1]}x{array.shape[0]}")


def preview(shatter_image: np.ndarray, fissures: list[np.ndarray], puffs: list[np.ndarray], path: Path) -> None:
    floor = np.array([0.36, 0.33, 0.42])
    tint = np.array([0.57, 0.28, 1.0])

    def comp(image, colour=None):
        rgb = image[..., :3] * (colour if colour is not None else 1)
        return rgb + floor * (1 - image[..., 3:4])

    panels = [comp(shatter_image)]
    for f in (fissures[2], fissures[5], fissures[-1]):
        panels.append(np.clip(comp(f, tint), 0, 1))
    both = comp(shatter_image)
    both = np.clip(both + fissures[-1][..., :3] * tint * 0.8, 0, 1)
    panels.append(both)
    strip = np.concatenate(panels, axis=1)
    puff_row = np.concatenate([comp(np.pad(p, ((192, 192), (192, 192), (0, 0)))) for p in puffs] + [np.tile(floor, (SIZE, SIZE, 1))], axis=1)
    out = np.concatenate([strip, puff_row], axis=0)
    Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8), "RGB").save(path)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", type=Path, default=OUT)
    parser.add_argument("--preview", type=Path, help="file for a preview over a floor tone")
    args = parser.parse_args()
    rng = np.random.default_rng(SEED)
    segments, slabs, angles = crack_layout(rng)
    shatter_image = shatter(segments, slabs, angles, np.random.default_rng(SEED + 1))
    fissures = [fissure_frame(segments, (k + 1) / (GROWTH_FRAMES - 1), k == GROWTH_FRAMES - 1) for k in range(GROWTH_FRAMES)]
    puffs = [dust_puff(SEED + 10 + k) for k in range(4)]
    args.out.mkdir(parents=True, exist_ok=True)
    save(shatter_image, args.out / "ground_shatter.png")
    save(tile(fissures, 4), args.out / "ground_fissures.png")
    save(tile(puffs, 2), args.out / "dust_puffs.png")
    if args.preview:
        preview(shatter_image, fissures, puffs, args.preview)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
