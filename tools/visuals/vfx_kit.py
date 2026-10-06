#!/usr/bin/env python3
"""Death Flame effect flipbooks, drawn from noise and shape fields.

    tools/visuals/.venv/bin/python tools/visuals/vfx_kit.py [--only core_hit,...] [--out DIR] [--preview DIR]

Every effect is coloured by the Death Flame ramp (the stops of DeathFlameRamp.cs: deep violet,
violet, pale violet, soul white) from a heat field, and written premultiplied with less
coverage than light: the hot parts add light to what lies behind them instead of covering it
with paint. The pipeline imports these sheets with PremultiplyAlpha=False (Content.mgcb), so the
values arrive as written.

Frames sit in a grid (columns = width / frame size), left to right, top to bottom, like every
sheet the registry describes. Directional effects point along +X; the game rotates them.
Effects spawned in the air pass (FigureHeights.Air, 70 units above the gameplay position) put
their ground parts that far below the frame centre, squashed by sin 35° like the floor.
"""
from __future__ import annotations

import argparse
import math
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "src" / "TheLostSoulOfFire" / "Content" / "Textures" / "Effects"
SS = 2  # supersampling per axis
SQUASH = 0.57  # sin 35°: a level circle seen by the game camera
AIR = 70.0  # world units between an air-pass effect and the floor under it

# DeathFlameRamp.Stops: intensity, colour, opacity.
STOPS = [
    (0.0, (63, 24, 112), 0.0),
    (0.3, (63, 24, 112), 0.55),
    (0.6, (145, 71, 255), 0.9),
    (0.85, (221, 190, 255), 1.0),
    (1.0, (246, 239, 255), 1.0),
]


# ----------------------------------------------------------------------------- fields

# The Life Flame (art/specs/ending.life-flame.md): ember red, warm orange, gold, a yellow-white core.
LIFE_STOPS = [
    (0.0, (90, 24, 10), 0.0),
    (0.3, (150, 46, 14), 0.55),
    (0.6, (255, 128, 40), 0.9),
    (0.85, (255, 200, 110), 1.0),
    (1.0, (255, 246, 214), 1.0),
]


def ramp(heat: np.ndarray, stops=None) -> tuple[np.ndarray, np.ndarray]:
    stops = stops or STOPS
    h = np.clip(heat, 0.0, 1.0)
    at = [s[0] for s in stops]
    colour = np.stack([np.interp(h, at, [s[1][c] / 255 for s in stops]) for c in range(3)], axis=-1)
    opacity = np.interp(h, at, [s[2] for s in stops])
    return colour, opacity


class Noise:
    """Tileable value noise: a random lattice of `period` cells, smoothly interpolated."""

    def __init__(self, seed: int, period: int = 64):
        self.period = period
        self.lattice = np.random.default_rng(seed).random((period, period))

    def __call__(self, x: np.ndarray, y: np.ndarray) -> np.ndarray:
        p = self.period
        xi, yi = np.floor(x).astype(int), np.floor(y).astype(int)
        fx, fy = x - xi, y - yi
        u, v = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
        lat = self.lattice
        a = lat[yi % p, xi % p]
        b = lat[yi % p, (xi + 1) % p]
        c = lat[(yi + 1) % p, xi % p]
        d = lat[(yi + 1) % p, (xi + 1) % p]
        return (a * (1 - u) + b * u) * (1 - v) + (c * (1 - u) + d * u) * v

    def fbm(self, x, y, octaves: int = 4) -> np.ndarray:
        total, amplitude, weight = 0.0, 1.0, 0.0
        for octave in range(octaves):
            scale = 2 ** octave
            total = total + self(x * scale + octave * 17.3, y * scale + octave * 9.1) * amplitude
            weight += amplitude
            amplitude *= 0.5
        return total / weight


class Frame:
    """Accumulates light (heat × density) and coverage for one frame at supersampled size."""

    def __init__(self, size: int):
        self.size = size
        n = size * SS
        ys, xs = np.mgrid[0:n, 0:n].astype(float)
        self.x = (xs + 0.5) / SS - size / 2
        self.y = (ys + 0.5) / SS - size / 2
        self.heat = np.zeros((n, n))
        self.density = np.zeros((n, n))

    def add(self, density: np.ndarray, heat) -> None:
        """Adds a layer; heat combines as a density-weighted maximum so cores stay hot."""
        density = np.clip(density, 0, None)
        heat = np.broadcast_to(heat, density.shape)
        self.heat = np.where(density * heat > self.density * self.heat * 0.999, np.maximum(self.heat, heat * np.clip(density * 1.5, 0, 1)), self.heat)
        self.density = 1 - (1 - np.clip(self.density, 0, 1)) * (1 - np.clip(density, 0, 1))

    def image(self, glow: float = 1.0, cover: float = 0.55, halo: float = 0.35, stops=None) -> np.ndarray:
        if halo > 0:
            # A soft halo of low heat around everything bright: light spilling into the air.
            spill = blur(self.density * np.clip(self.heat, 0, 1), self.size * SS * 0.03)
            self.add(spill * halo * 1.6, np.full(spill.shape, 0.42))
        colour, opacity = ramp(self.heat, stops)
        light = np.clip(self.density * (0.35 + 0.85 * self.heat) * glow, 0, 1)
        rgb = colour * light[..., None]
        alpha = np.clip(self.density * opacity * cover, 0, 1)
        alpha = np.maximum(alpha, rgb.max(axis=-1) * 0.15)  # a whisper of coverage keeps cores solid
        out = np.concatenate([np.clip(rgb, 0, 1), alpha[..., None]], axis=-1)
        n = self.size
        out = out.reshape(n, SS, n, SS, 4).mean(axis=(1, 3))
        # Fade light and coverage out toward the frame edge: the outermost ring stays clear and
        # nothing is cut off hard where a flame reaches the border.
        edge = np.minimum(np.arange(n) + 0.5, n - 0.5 - np.arange(n)) - 1.0
        window = np.clip(edge / (n * 0.07), 0, 1) ** 1.5
        return out * (window[:, None] * window[None, :])[..., None]


def blur(field: np.ndarray, radius: float) -> np.ndarray:
    """Two box-blur passes per axis (close to a gaussian), radius in supersampled pixels."""
    r = max(1, int(radius))
    out = field
    for _ in range(2):
        for axis in (0, 1):
            padded = np.pad(out, [(r + 1, r) if a == axis else (0, 0) for a in range(2)], mode="edge")
            c = np.cumsum(padded, axis=axis)
            hi = np.take(c, np.arange(2 * r + 1, c.shape[axis]), axis=axis)
            lo = np.take(c, np.arange(0, c.shape[axis] - 2 * r - 1), axis=axis)
            out = (hi - lo) / (2 * r + 1)
    return out


def gauss(d2: np.ndarray, radius: float) -> np.ndarray:
    return np.exp(-d2 / (2 * max(radius, 1e-3) ** 2))


def ring(f: Frame, cx: float, cy: float, rx: float, ry: float, width: float) -> np.ndarray:
    """A thin ellipse outline as a soft band."""
    nx, ny = (f.x - cx) / max(rx, 1e-3), (f.y - cy) / max(ry, 1e-3)
    r = np.sqrt(nx * nx + ny * ny)
    return np.exp(-((r - 1) * max(rx, ry) / max(width, 0.5)) ** 2)


def wash(f: Frame, cx: float, cy: float, rx: float, ry: float) -> np.ndarray:
    """Light washing out to an ellipse: full in the middle, falling off softly to the edge, no outline."""
    nx, ny = (f.x - cx) / max(rx, 1e-3), (f.y - cy) / max(ry, 1e-3)
    return np.exp(-(nx * nx + ny * ny) * 2.2)


def star(f: Frame, cx: float, cy: float, radius: float, strength: float, spikes: int = 4, angle: float = 0.0) -> None:
    """A flash: a small hot core with thin spikes, not a disc."""
    dx, dy = f.x - cx, f.y - cy
    f.add(gauss(dx * dx + dy * dy, radius * 0.35) * strength, 1.25 * min(strength, 1.0) + 0.05)
    f.add(gauss(dx * dx + dy * dy, radius) * strength * 0.45, 0.85 * min(strength, 1.0))
    for k in range(spikes):
        a = angle + k * math.pi / spikes
        along = dx * math.cos(a) + dy * math.sin(a)
        across = -dx * math.sin(a) + dy * math.cos(a)
        spike = np.exp(-(across / max(radius * 0.08, 0.6)) ** 2) * np.exp(-np.abs(along) / (radius * (1.6 if k % 2 == 0 else 1.0)))
        f.add(spike * strength, 1.1 * min(strength, 1.0))


def tongues(along: np.ndarray, across: np.ndarray, length: float, width: float, noise: Noise, phase: float,
            scale: float = 0.07, bite: float = 0.6, taper: float = 0.7) -> np.ndarray:
    """A flame body along +`along`: a rounded base, a tapered body, eaten by scrolling noise that
    bites harder toward the tip, so the outline breaks into tongues."""
    h = along / length
    w = np.maximum(width * np.clip(1 - h, 0, 1) ** taper, 0.3)
    body = 1 - (across / w) ** 2
    cap = 1 - (across / width) ** 2 - (h / 0.14) ** 2
    shape = np.where(h < 0, cap, body * np.clip(1 - h, 0, 1) ** 0.25)
    n = (noise.fbm(across * scale, along * scale * 0.7 - phase, 4) - 0.5) * 2.6  # about -1..1
    erode = n * bite * (0.55 + np.clip(h, 0, 1)) + np.clip(h, 0, 1) ** 2 * 0.45
    density = np.clip((shape - erode) * 2.0, 0, 1) * (h < 1)
    # How close to the hot axis near the root: 1 on the axis at the base, 0 at the rim and tip.
    core = np.clip(1 - np.abs(across) / w, 0, 1) * np.clip(1 - np.clip(h, 0, 1), 0, 1) ** 0.8
    return density, core


def ease_out(t: float, power: float = 2.5) -> float:
    return 1 - (1 - min(max(t, 0.0), 1.0)) ** power


def sparks(f: Frame, rng: np.random.Generator, count: int, t: float, reach: float, spread: float,
           bias: float = 0.0, size: float = 1.6, gravity: float = 0.0, heat: float = 1.0, ground: float | None = None):
    """Embers flying out from the centre; angles cluster around `bias` with `spread` (radians)."""
    for _ in range(count):
        angle = bias + rng.normal(0, spread)
        speed = rng.uniform(0.45, 1.0)
        rise = rng.uniform(-0.2, 0.6)
        distance = reach * speed * ease_out(t, 2.0)
        px = math.cos(angle) * distance
        py = math.sin(angle) * distance * (SQUASH if ground is not None else 1.0) - rise * reach * 0.4 * t + gravity * t * t
        if ground is not None:
            py += ground * (1 - t) * 0.0
        life = 1 - t / rng.uniform(0.7, 1.0)
        if life <= 0:
            continue
        stretch = 1.0 + 2.5 * (1 - t)
        dx, dy = f.x - px, f.y - py
        along = (dx * math.cos(angle) + dy * math.sin(angle)) / stretch
        across = -dx * math.sin(angle) + dy * math.cos(angle)
        f.add(gauss(along * along + across * across, size) * life, heat * (0.6 + 0.4 * life))


# ----------------------------------------------------------------------------- effects

def core_hit(i: int, n: int, size: int) -> np.ndarray:
    """Impact on a breaking point: a white star, a wash of light and embers thrown along +X."""
    t = i / (n - 1)
    f = Frame(size)
    rng = np.random.default_rng(11)
    r2 = f.x ** 2 + f.y ** 2
    flash = (1 - t) ** 1.6
    star(f, 0, 0, size * (0.07 + 0.04 * t), flash * 1.2, 4, 0.2)
    for angle, length in ((0.0, 1.0), (0.55, 0.7), (-0.55, 0.7), (1.25, 0.45), (-1.25, 0.45), (2.6, 0.3), (-2.6, 0.3)):
        along = f.x * math.cos(angle) + f.y * math.sin(angle)
        across = -f.x * math.sin(angle) + f.y * math.cos(angle)
        reach = size * (0.18 + 0.22 * ease_out(t)) * length
        ray = np.exp(-(across / (1.2 + 2.0 * (1 - t))) ** 2) * np.clip(1 - along / reach, 0, 1) * (along > 0)
        f.add(ray * (1 - t) ** 1.2, 0.95 * (1 - t) + 0.15)
    shock = size * (0.08 + 0.3 * ease_out(t))
    f.add(wash(f, 0, 0, shock, shock) * (1 - t) ** 2 * 0.3, 0.6 * (1 - t) + 0.2)
    sparks(f, rng, 16, t, size * 0.45, 0.6, bias=0.0, size=1.3, heat=1.0)
    return f.image(glow=1.15)


def cannon_muzzle(i: int, n: int, size: int) -> np.ndarray:
    """Muzzle blast along +X from the frame centre: a star, a tongue of flame with two side
    jets, a faint shock disc, embers."""
    t = i / (n - 1)
    f = Frame(size)
    rng = np.random.default_rng(23)
    noise = Noise(5)
    life = (1 - t) ** 1.1
    star(f, 0, 0, size * 0.06, (1 - t) ** 2 * 1.3, 4, 0.0)
    for angle, scale in ((0.0, 1.0), (0.75, 0.42), (-0.75, 0.42)):
        along = f.x * math.cos(angle) + f.y * math.sin(angle)
        across = -f.x * math.sin(angle) + f.y * math.cos(angle)
        length = size * (0.2 + 0.24 * ease_out(t)) * scale
        flame, core = tongues(along, across, length, size * 0.09 * scale + 2, noise, t * 6 + angle * 3, scale=0.06, bite=0.5 + 0.5 * t, taper=0.5)
        heat = np.clip(0.42 + 0.85 * core - t * 0.45, 0.05, 1.2)
        f.add(flame * life, heat)
    disc = ring(f, 4 + t * 10, 0, 2 + 8 * ease_out(t), size * (0.05 + 0.15 * ease_out(t)), 0.8 + 1.5 * (1 - t))
    f.add(disc * (1 - t) ** 2 * 0.6, 0.65)
    sparks(f, rng, 16, t, size * 0.42, 0.3, bias=0.0, size=1.3, heat=1.0)
    return f.image(glow=1.2)


def cannon_projectile(i: int, n: int, size: int) -> np.ndarray:
    """A soul-fire bolt flying along +X: a white star of a head and a streaming tail of flame
    tongues that loops seamlessly."""
    f = Frame(size)
    noise = Noise(31, period=4)
    phase = i / n * 4  # one noise period per loop
    head_x = size * 0.16
    dx = f.x - head_x
    star(f, head_x, 0, size * 0.06, 1.1, 2, 0.0)
    f.add(gauss(dx * dx + (f.y * 1.4) ** 2, size * 0.06), 1.05)
    back = -dx
    length = size * 0.62
    flame, core = tongues(back, f.y, length, size * 0.1, noise, -phase, scale=0.09, bite=0.65, taper=0.9)
    heat = np.clip(0.4 + 0.85 * core, 0.05, 1.15)
    f.add(flame * 0.95, heat)
    return f.image(glow=1.15)


def cannon_charge(i: int, n: int, size: int) -> np.ndarray:
    """Charging: motes spiral into a pulsing orb (seamless loop)."""
    f = Frame(size)
    t = i / n
    r2 = f.x ** 2 + f.y ** 2
    pulse = 0.5 + 0.5 * math.sin(t * math.tau)
    f.add(gauss(r2, size * (0.07 + 0.015 * pulse)), 0.95 + 0.2 * pulse)
    f.add(gauss(r2, size * 0.03), 1.25)
    f.add(gauss(r2, size * (0.15 + 0.02 * pulse)) * 0.18, 0.6)
    count = 12
    for k in range(count):
        local = (t + k / count) % 1.0
        radius = size * 0.42 * (1 - local) ** 1.3 + size * 0.04
        angle = k * 2.399 + local * 3.2
        px, py = math.cos(angle) * radius, math.sin(angle) * radius
        fade = math.sin(local * math.pi)
        f.add(gauss((f.x - px) ** 2 + (f.y - py) ** 2, 1.6 + 1.2 * local) * fade, 0.55 + 0.5 * local)
    return f.image(glow=1.1)


def burning_detonation(i: int, n: int, size: int) -> np.ndarray:
    """The Burning bursts: flash, a fireball that rises and hollows out, light washing over the
    floor, embers, violet smoke lingering."""
    t = i / (n - 1)
    f = Frame(size)
    rng = np.random.default_rng(41)
    noise = Noise(43)
    ground = AIR / 0.88  # spawned at scale 0.88 in the air pass
    if t < 0.3:
        star(f, 0, 0, size * 0.1, (1 - t / 0.3) ** 1.5 * 1.3, 4, 0.4)
    rise = size * 0.12 * ease_out(t, 1.4)
    radius = size * (0.1 + 0.26 * ease_out(t, 2.4))
    px, py = f.x, f.y + rise
    d = np.sqrt(px * px + py * py) / radius
    erosion = noise.fbm(px * 0.03 + t * 1.2, py * 0.03 - t * 3.0, 4)
    field = (1 - d) + 0.35 - erosion * (0.7 + 0.5 * t)
    ball = np.clip(field * 3.0, 0, 1)
    # Late in the blast the core burns out first and the rest breaks into rising tongues.
    hollow = np.clip((d + (erosion - 0.5) * 0.6 - (t - 0.45) * 1.6) * 3, 0, 1) if t > 0.45 else 1.0
    fade = (1 - t) ** 0.8
    heat = np.clip(1.3 - d * 0.75 - t * 1.0 + (erosion - 0.5) * 0.4, 0.05, 1.25)
    f.add(ball * hollow * fade, heat)
    # The blast's light washes over the floor under it (no ring).
    gr = size * (0.1 + 0.42 * ease_out(t, 2.0))
    f.add(wash(f, 0, ground, gr, gr * SQUASH) * (1 - t) ** 1.6 * 0.6, 0.62 * (1 - t) + 0.12)
    f.add(gauss(f.x ** 2 + ((f.y - ground) / SQUASH) ** 2, gr * 0.5) * (1 - t) ** 2 * 0.4, 0.45)
    # Smoke: dim violet billows climbing late.
    if t > 0.35:
        s = (t - 0.35) / 0.65
        sy = f.y + rise + size * 0.18 * s
        smoke = np.clip(1 - np.sqrt(f.x ** 2 + sy ** 2) / (radius * 1.1), 0, 1) * noise.fbm(f.x * 0.03, sy * 0.03 - s * 2, 3)
        f.add(smoke * (1 - s) * 0.8 * math.sin(s * math.pi) * 1.6, 0.32)
    sparks(f, rng, 26, t, size * 0.48, 1.6, bias=-math.pi / 2, size=1.6, gravity=size * 0.25, heat=1.0)
    return f.image(glow=1.15, cover=0.6)


def soul_release(i: int, n: int, size: int) -> np.ndarray:
    """A soul set free: a burst of light, then a white wisp that rises and sways, motes spiralling."""
    t = i / (n - 1)
    f = Frame(size)
    noise = Noise(53)
    if t < 0.3:
        b = t / 0.3
        f.add(wash(f, 0, 0, size * (0.06 + 0.3 * b), size * (0.06 + 0.3 * b)) * (1 - b) ** 1.5 * 0.35, 0.8)
        f.add(gauss(f.x ** 2 + f.y ** 2, size * 0.08) * (1 - b), 1.1)
    rise = size * 0.38 * ease_out(t, 1.6)
    sway = math.sin(t * math.tau * 1.3) * size * 0.04
    cx, cy = sway, -rise + size * 0.06
    dx, dy = f.x - cx, f.y - cy
    # Teardrop: round head, tail trailing downward.
    tail = np.clip(dy, 0, None)
    width = size * 0.07 * np.clip(1 - tail / (size * 0.3), 0, 1) + 0.6
    flow = noise.fbm(dx * 0.08, dy * 0.06 + t * 6, 3)
    wisp = np.exp(-(dx / width) ** 2) * np.where(dy < 0, gauss(dy * dy, size * 0.06) / np.maximum(gauss(0 * dy, size * 0.06), 1e-6), 1) * (0.55 + 0.7 * flow)
    life = math.sin(min(t / 0.85, 1.0) * math.pi) ** 0.6
    f.add(wisp * life, np.clip(1.15 - tail / (size * 0.3), 0.3, 1.2))
    for k in range(7):
        a = k * 0.9 + t * 7
        r = size * (0.06 + 0.12 * t)
        mx, my = cx + math.cos(a) * r, cy + size * 0.1 + math.sin(a) * r * 0.5 + k * 2
        f.add(gauss((f.x - mx) ** 2 + (f.y - my) ** 2, 1.3) * life * 0.9, 0.85)
    return f.image(glow=1.1)


def resonance_activate(i: int, n: int, size: int) -> np.ndarray:
    """Resonance erupts: light races out over the floor, a pillar of flame climbs from the feet,
    sparks rise."""
    t = i / (n - 1)
    f = Frame(size)
    rng = np.random.default_rng(61)
    noise = Noise(67)
    feet = AIR / 0.78  # spawned at scale 0.78 in the air pass
    gr = size * (0.06 + 0.42 * ease_out(t, 2.4))
    f.add(wash(f, 0, feet, gr, gr * SQUASH) * (1 - t) ** 1.2 * 0.65, 0.85 * (1 - t) + 0.15)
    climb = ease_out(t / 0.4, 2.0)
    height = (feet + size * 0.48) * climb + 1
    life = (1 - t) ** 0.9
    along = feet - f.y  # upward from the feet
    flame, core = tongues(along, f.x, height, size * 0.09 * (1 - 0.4 * t), noise, t * 9, scale=0.06, bite=0.5 + 0.4 * t, taper=0.45)
    heat = np.clip(0.4 + 0.9 * core - t * 0.55, 0.05, 1.2)
    f.add(flame * life, heat)
    if t < 0.3:
        star(f, 0, 0, size * 0.08, (1 - t / 0.3) ** 1.5 * 1.2, 4, 0.0)
    sparks(f, rng, 22, t, size * 0.45, 0.5, bias=-math.pi / 2, size=1.5, gravity=size * 0.05, heat=1.0)
    return f.image(glow=1.15)


def dash_ignition(i: int, n: int, size: int) -> np.ndarray:
    """Dash start: Death Flame thrown off behind the dash (the dash runs along +X)."""
    t = i / (n - 1)
    f = Frame(size)
    rng = np.random.default_rng(71)
    noise = Noise(73)
    life = (1 - t) ** 1.1
    length = size * (0.22 + 0.24 * ease_out(t))
    flame, core = tongues(-f.x, f.y, length, size * 0.16, noise, t * 7, scale=0.07, bite=0.55 + 0.4 * t, taper=0.35)
    f.add(flame * life, np.clip(0.38 + 0.85 * core - t * 0.5, 0.05, 1.2))
    star(f, 0, 0, size * 0.06, (1 - t) ** 2 * 1.1, 2, math.pi / 2)
    sparks(f, rng, 12, t, size * 0.42, 0.4, bias=math.pi, size=1.3, heat=0.95)
    return f.image(glow=1.15)


def death_flame(i: int, n: int, size: int) -> np.ndarray:
    """The Death Flame itself: a living flame on the frame centre (seamless loop)."""
    f = Frame(size)
    noise = Noise(83, period=4)
    base, tip = size * 0.24, -size * 0.42
    length = base - tip
    along = base - f.y
    # tongues() scrolls its noise by `phase` in units of along*scale*0.7; one period per loop.
    phase = i / n * 4  # one noise period per loop
    sway = (noise.fbm(f.y * 0.02 + 5.0, i / n * 4, 2) - 0.5) * size * 0.06 * np.clip(along / length, 0, 1) ** 1.5
    flame, core = tongues(along, f.x + sway, length, size * 0.2, noise, phase, scale=0.075, bite=0.7, taper=0.55)
    heat = np.clip(0.36 + 0.85 * core, 0.05, 1.15)
    f.add(flame, heat)
    for k in range(4):
        local = (i / n + k / 4) % 1.0
        ex = math.sin(k * 2.1 + local * 4) * size * 0.1
        ey = base * 0.2 - local * size * 0.78
        f.add(gauss((f.x - ex) ** 2 + (f.y - ey) ** 2, 1.1) * math.sin(local * math.pi), 0.8)
    return f.image(glow=1.15)


def lost_soul(i: int, n: int, size: int) -> np.ndarray:
    """A Lost Soul (art/specs/pickup.lost-soul.md): a small round light, almost white, with a
    softly flickering violet rim and a few motes circling it; no face, no flame shape, so it is
    never mistaken for an effect (seamless loop)."""
    f = Frame(size)
    noise = Noise(97, period=4)
    t = i / n
    bob = math.sin(t * math.tau) * size * 0.012
    x, y = f.x, (f.y - bob) * 0.92
    r = np.sqrt(x * x + y * y)
    angle = np.arctan2(y, x)
    # The rim breathes: radius modulated by looping noise around the circle.
    wobble = noise.fbm(np.cos(angle) * 1.2 + 2.0, np.sin(angle) * 1.2 + t * 4, 3) - 0.5
    radius = size * (0.13 + 0.025 * math.sin(t * math.tau * 2)) * (1 + wobble * 0.35)
    body = np.clip(1 - r / radius, 0, 1) ** 0.6
    f.add(body, np.clip(0.5 + 0.75 * (1 - r / radius), 0.2, 1.2))
    f.add(gauss(x * x + y * y, size * 0.05), 1.2)
    for k in range(4):
        local = (t + k / 4) % 1.0
        a = k * math.tau / 4 + t * math.tau
        mx, my = math.cos(a) * size * 0.2, math.sin(a) * size * 0.07 + bob - size * 0.02
        f.add(gauss((f.x - mx) ** 2 + (f.y - my) ** 2, 1.2) * (0.5 + 0.5 * math.sin(local * math.tau)) * 0.8, 0.8)
    return f.image(glow=1.05, halo=0.6)


def life_flame(i: int, n: int, size: int) -> np.ndarray:
    """The Life Flame at the end of the arena: soft, round, rising; warm where the Death Flame is
    cold, calmer in its motion (seamless loop)."""
    f = Frame(size)
    noise = Noise(109, period=4)
    phase = i / n * 4
    base, tip = size * 0.22, -size * 0.36
    length = base - tip
    along = base - f.y
    sway = (noise.fbm(f.y * 0.02 + 7.0, i / n * 4, 2) - 0.5) * size * 0.05 * np.clip(along / length, 0, 1) ** 1.4
    flame, core = tongues(along, f.x + sway, length, size * 0.21, noise, phase, scale=0.06, bite=0.45, taper=0.5)
    f.add(flame, np.clip(0.38 + 0.9 * core, 0.05, 1.2))
    f.add(gauss(f.x ** 2 + ((f.y - base * 0.3) * 1.2) ** 2, size * 0.08) * 0.9, 1.2)
    for k in range(5):
        local = (i / n + k / 5) % 1.0
        ex = math.sin(k * 1.7 + local * 3) * size * 0.12
        ey = base * 0.1 - local * size * 0.7
        f.add(gauss((f.x - ex) ** 2 + (f.y - ey) ** 2, 1.2) * math.sin(local * math.pi) * 0.9, 0.85)
    return f.image(glow=1.15, halo=0.55, stops=LIFE_STOPS)


#: effect -> (file stem, frame size, frames, builder)
EFFECTS = {
    "core_hit": ("fx_core_hit", 128, 9, core_hit),
    "cannon_muzzle": ("fx_cannon_muzzle_full", 256, 9, cannon_muzzle),
    "cannon_projectile": ("fx_cannon_projectile_full", 128, 9, cannon_projectile),
    "cannon_charge": ("fx_cannon_charge_loop", 128, 9, cannon_charge),
    "burning_detonation": ("fx_burning_detonation", 256, 16, burning_detonation),
    "soul_release": ("fx_soul_release", 128, 16, soul_release),
    "resonance_activate": ("fx_resonance_activate", 256, 16, resonance_activate),
    "dash_ignition": ("fx_dash_ignition", 128, 9, dash_ignition),
    "death_flame": ("fx_death_flame_loop", 128, 16, death_flame),
    "lost_soul": ("../Pickups/lost_soul", 128, 12, lost_soul),
    "life_flame": ("../Ending/life_flame", 128, 16, life_flame),
}


# ----------------------------------------------------------------------------- world marks
# Single images for telegraphs and tethers (Rendering/WorldMarks.cs): white light, tinted in game.

MARK_RING_RADIUS = 116  # px on a 256 canvas; WorldMarks scales by radius / this


def light_image(core: np.ndarray, glow: np.ndarray) -> np.ndarray:
    """White light: the core covers a little, the glow only adds light."""
    light = np.clip(core + glow, 0, 1)
    alpha = np.clip(core * 0.55, 0, 1)
    return np.stack([light, light, light, alpha], axis=-1)


def canvas_fade(x: np.ndarray, y: np.ndarray) -> np.ndarray:
    """1 inside, falling smoothly to 0 before the canvas border: the glow of a ring near the edge
    of its 256 px canvas would otherwise be cut off square and show as a rectangle in the game."""
    r = np.sqrt(x * x + y * y)
    t = np.clip((127.0 - r) / 7.0, 0, 1)
    return t * t * (3 - 2 * t)


def mark_ring(width: float, glow: float) -> np.ndarray:
    n = 256 * SS
    ys, xs = np.mgrid[0:n, 0:n].astype(float)
    x, y = (xs + 0.5) / SS - 128, (ys + 0.5) / SS - 128
    d = np.sqrt(x * x + y * y) - MARK_RING_RADIUS
    fade = canvas_fade(x, y)
    image = light_image(np.exp(-(d / width) ** 2) * fade, 0.45 * np.exp(-(d / glow) ** 2) * fade)
    return image.reshape(256, SS, 256, SS, 4).mean(axis=(1, 3))


def mark_zone(rim_width: float, fill: float, rim: float = 0.8) -> np.ndarray:
    """A zone of light on the floor instead of a line: the area inside fills faintly, rising
    toward the edge, and the edge itself is a soft band at exactly MARK_RING_RADIUS, so the
    radius still reads precisely without a hairline."""
    n = 256 * SS
    ys, xs = np.mgrid[0:n, 0:n].astype(float)
    x, y = (xs + 0.5) / SS - 128, (ys + 0.5) / SS - 128
    r = np.sqrt(x * x + y * y)
    d = r - MARK_RING_RADIUS
    fade = canvas_fade(x, y)
    edge = np.exp(-(d / rim_width) ** 2)
    inside = np.clip(r / MARK_RING_RADIUS, 0, 1) ** 2.6 * np.exp(-(np.clip(d, 0, None) / (rim_width * 0.8)) ** 2)
    core = edge * rim * fade
    glow = (fill * inside + 0.3 * np.exp(-(d / (rim_width * 3.0)) ** 2)) * fade
    image = light_image(core, glow)
    return image.reshape(256, SS, 256, SS, 4).mean(axis=(1, 3))


def mark_sector(span: float = 1.6) -> np.ndarray:
    """The swipe's reach as a filled sector: faint near the body, brighter toward the soft edge at
    MARK_RING_RADIUS, the sides fading out; a zone, not a line."""
    n = 256 * SS
    ys, xs = np.mgrid[0:n, 0:n].astype(float)
    x, y = (xs + 0.5) / SS - 128, (ys + 0.5) / SS - 128
    r = np.sqrt(x * x + y * y)
    d = r - MARK_RING_RADIUS
    angle = np.abs(np.arctan2(y, x))
    ends = np.clip((span / 2 - angle) / 0.3, 0, 1) ** 1.3
    fade = canvas_fade(x, y)
    edge = np.exp(-(d / 6.5) ** 2)
    inside = np.clip((r - MARK_RING_RADIUS * 0.3) / (MARK_RING_RADIUS * 0.7), 0, 1) ** 1.8 * np.exp(-(np.clip(d, 0, None) / 5.0) ** 2)
    core = edge * 0.75 * ends * fade
    glow = (0.32 * inside + 0.3 * np.exp(-(d / 18) ** 2)) * ends * fade
    image = light_image(core, glow)
    return image.reshape(256, SS, 256, SS, 4).mean(axis=(1, 3))


def mark_arc(span: float = 1.6) -> np.ndarray:
    n = 256 * SS
    ys, xs = np.mgrid[0:n, 0:n].astype(float)
    x, y = (xs + 0.5) / SS - 128, (ys + 0.5) / SS - 128
    d = np.sqrt(x * x + y * y) - MARK_RING_RADIUS
    angle = np.abs(np.arctan2(y, x))
    ends = np.clip((span / 2 - angle) / 0.22, 0, 1) ** 1.5
    fade = canvas_fade(x, y)
    image = light_image(np.exp(-(d / 3.2) ** 2) * ends * fade, 0.5 * np.exp(-(d / 11) ** 2) * ends * fade)
    return image.reshape(256, SS, 256, SS, 4).mean(axis=(1, 3))


def mark_lane() -> np.ndarray:
    """A charge lane along +X: a soft band, chevrons pointing forward, an arrowhead at the end."""
    w, h = 256, 64
    ys, xs = np.mgrid[0:h * SS, 0:w * SS].astype(float)
    x, y = (xs + 0.5) / SS, (ys + 0.5) / SS - h / 2
    fade_in = np.clip(x / 40, 0, 1)
    band = np.exp(-(y / 16) ** 4) * 0.35 * fade_in * (x < 222)
    period = 42
    local = (x % period) + (np.abs(y) * 0.9)  # tips lead: chevrons point along +X
    chevron = np.exp(-((local - 36) / 3.0) ** 2) * (np.abs(y) < 18) * fade_in * (x < 214)
    head = np.clip(1 - (np.abs(y) / 26 + (x - 222) / 32), 0, 1) * (x >= 222)
    head_edge = np.exp(-((np.abs(y) / 26 + (x - 222) / 32 - 1) * 12) ** 2) * (x >= 214)
    core = np.clip(chevron * 0.9 + head * 0.5 + head_edge, 0, 1)
    glow = np.clip(band + chevron * 0.3 + head * 0.25, 0, 1)
    image = light_image(core, glow)
    return image.reshape(h, SS, w, SS, 4).mean(axis=(1, 3))


def mark_beam() -> np.ndarray:
    """A tether: uniform along X, a hot core line with a soft glow across."""
    w, h = 64, 32
    ys, xs = np.mgrid[0:h * SS, 0:w * SS].astype(float)
    y = (ys + 0.5) / SS - h / 2
    image = light_image(np.exp(-(y / 2.2) ** 2) + 0 * xs, 0.5 * np.exp(-(y / 7) ** 2) + 0 * xs)
    return image.reshape(h, SS, w, SS, 4).mean(axis=(1, 3))


MARKS = {
    # Zones of light, not lines (Durchgang 3): same radius, filled toward a soft edge.
    "mark_ring_thin": lambda: mark_zone(6.0, 0.16, 0.7),
    "mark_ring_bold": lambda: mark_zone(9.0, 0.3, 1.0),
    "mark_arc": mark_sector,
    "mark_lane": mark_lane,
    "mark_beam": mark_beam,
}


def sheet(frames: list[np.ndarray]) -> Image.Image:
    count = len(frames)
    columns = math.ceil(math.sqrt(count))
    rows = math.ceil(count / columns)
    size = frames[0].shape[0]
    out = np.zeros((rows * size, columns * size, 4))
    for index, frame in enumerate(frames):
        r, c = divmod(index, columns)
        out[r * size:(r + 1) * size, c * size:(c + 1) * size] = frame
    return Image.fromarray((np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA")


def preview(frames: list[np.ndarray], path: Path) -> None:
    """A strip of all frames composited (premultiplied) over a mid-violet floor tone."""
    size = frames[0].shape[0]
    floor = np.array([0.36, 0.33, 0.42])
    strip = np.zeros((size, size * len(frames), 3))
    for index, frame in enumerate(frames):
        strip[:, index * size:(index + 1) * size] = frame[..., :3] + floor * (1 - frame[..., 3:4])
    Image.fromarray((np.clip(strip, 0, 1) * 255).astype(np.uint8), "RGB").save(path)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--only", help="comma-separated effect names")
    parser.add_argument("--out", type=Path, default=OUT)
    parser.add_argument("--preview", type=Path, help="directory for preview strips over a floor tone")
    args = parser.parse_args()
    names = args.only.split(",") if args.only else list(EFFECTS) + list(MARKS)
    args.out.mkdir(parents=True, exist_ok=True)
    for name in names:
        if name in MARKS:
            image = Image.fromarray((np.clip(MARKS[name](), 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA")
            image.save(args.out / f"{name}.png")
            print(f"{name}.png {image.size[0]}x{image.size[1]}")
            continue
        stem, size, count, build = EFFECTS[name]
        frames = [build(i, count, size) for i in range(count)]
        image = sheet(frames)
        image.save(args.out / f"{stem}.png")
        if args.preview:
            args.preview.mkdir(parents=True, exist_ok=True)
            preview(frames, args.preview / f"{Path(stem).name}.png")
        print(f"{stem}.png {image.size[0]}x{image.size[1]} frames={count}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
