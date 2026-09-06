#!/usr/bin/env python3
"""Authored sprite-VFX forge for The Lost Soul of Fire.

Why this exists
---------------
The delivered effect sheets were generated as images rather than as effects, and
it shows in the measurements. `fx_burning_detonation` covers 79-81% of its frame
for three frames in the middle and then spends nine frames as a field of
speckled noise across the whole 256px cell — drawn additively, that is the
literal square the owner reported. `fx_resonance_activate` peaks at 72%.
`fx_soul_release` is a grey blob that fades. None of them have particles that
travel, none have a shockwave that thins as it expands, and none have a decay
that resolves to nothing.

Effects are authored here instead, as effects:

  * every sheet is built from an **energy field** which is then quantised into a
    small number of alpha bands, so the result is banded pixel light rather than
    a smooth painted gradient — these sheets are drawn PointClamp;
  * sparks and motes are **simulated once and sampled per frame**, so they
    actually travel, decelerate and separate instead of being re-scattered every
    frame;
  * every effect has an explicit attack, a body and a decay that reaches zero;
  * peak coverage is bounded and asserted. Nothing may become a square again.

Usage:
    python3 tools/visual-max/vfx_forge.py --out <dir>
"""

from __future__ import annotations

import argparse
import math
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image

# --------------------------------------------------------------------------
# Colour
#
# One ramp for every effect. Death Flame is violet-white: the hottest part of
# any Soulfire effect is white, the body is violet, the edge falls to a deep
# violet that is nearly the room's own colour.
# --------------------------------------------------------------------------

RAMP: list[tuple[float, tuple[int, int, int]]] = [
    (0.00, (0x3A, 0x1E, 0x62)),   # deep violet — the outermost edge
    (0.28, (0x6E, 0x33, 0xC4)),
    (0.52, (0xA8, 0x6C, 0xFF)),   # Death Flame
    (0.74, (0xDE, 0xD0, 0xFF)),   # flame bright
    (1.00, (0xFF, 0xFB, 0xFF)),   # soul white — the core only
]

LIFE_RAMP: list[tuple[float, tuple[int, int, int]]] = [
    (0.00, (0x5A, 0x24, 0x10)),
    (0.35, (0xC2, 0x5A, 0x1C)),
    (0.70, (0xFF, 0xA8, 0x48)),
    (1.00, (0xFF, 0xF0, 0xCC)),
]

BANDS = 7
COVERAGE_LIMIT = 0.34


def ramp_lookup(values: np.ndarray, ramp=RAMP) -> np.ndarray:
    stops = np.array([s for s, _ in ramp], dtype=np.float32)
    colours = np.array([c for _, c in ramp], dtype=np.float32)
    out = np.zeros(values.shape + (3,), dtype=np.float32)
    for channel in range(3):
        out[..., channel] = np.interp(values, stops, colours[:, channel])
    return out


# --------------------------------------------------------------------------
# Energy field
# --------------------------------------------------------------------------


class Field:
    """A frame's light, accumulated as energy and resolved once at the end."""

    def __init__(self, size: int):
        self.size = size
        self.e = np.zeros((size, size), dtype=np.float32)
        ys, xs = np.mgrid[0:size, 0:size]
        self.x = xs.astype(np.float32) + 0.5
        self.y = ys.astype(np.float32) + 0.5

    # -- primitives -------------------------------------------------------

    def radial(self, cx: float, cy: float, radius: float, strength: float = 1.0, power: float = 2.0) -> None:
        if radius <= 0.1 or strength <= 0.001:
            return
        d = np.sqrt((self.x - cx) ** 2 + (self.y - cy) ** 2) / radius
        self.e += strength * np.clip(1.0 - d, 0.0, 1.0) ** power

    def disc(self, cx: float, cy: float, radius: float, strength: float = 1.0, edge: float = 2.0) -> None:
        """Hard-edged core. Effects need a solid centre, not only falloff."""
        if radius <= 0.1:
            return
        d = np.sqrt((self.x - cx) ** 2 + (self.y - cy) ** 2)
        self.e += strength * np.clip((radius - d) / max(edge, 0.5), 0.0, 1.0)

    def ring(self, cx: float, cy: float, radius: float, thickness: float, strength: float = 1.0,
             *, wobble: float = 0.14, gaps: float = 0.55, seed: int = 0) -> None:
        """A shockwave, not a circle.

        A perfectly round, evenly bright ring is a shiny vector ring, which the
        owner rejected outright for the fighting plane. So the radius is
        modulated around the circumference, the density varies, and parts of it
        are simply missing. It is still an honest reach — the gameplay circle it
        describes is unchanged — but it reads as pressure escaping instead of as
        interface.
        """
        if radius <= 0.1 or thickness <= 0.1 or strength <= 0.001:
            return
        angle = np.arctan2(self.y - cy, self.x - cx)
        phase = seed * 0.7
        warp = (np.sin(angle * 3.0 + phase) * 0.55
                + np.sin(angle * 5.0 - phase * 1.7) * 0.30
                + np.sin(angle * 8.0 + phase * 2.3) * 0.15)
        local_radius = radius * (1.0 + wobble * warp)
        density = (0.62
                   + 0.38 * np.sin(angle * 4.0 - phase * 2.1)
                   + 0.24 * np.sin(angle * 7.0 + phase * 0.9))
        density = np.clip((density - gaps) / max(1.0 - gaps, 0.05), 0.0, 1.0)
        d = np.abs(np.sqrt((self.x - cx) ** 2 + (self.y - cy) ** 2) - local_radius)
        self.e += strength * np.clip(1.0 - d / thickness, 0.0, 1.0) ** 1.4 * density

    def arc(
        self,
        cx: float,
        cy: float,
        radius: float,
        thickness: float,
        start: float,
        sweep: float,
        strength: float = 1.0,
        taper: float = 1.0,
        shred: float = 0.0,
    ) -> None:
        """A crescent. `taper` fades the trailing end, which is what makes a
        slash read as a blade passing rather than as a painted smile."""
        if radius <= 0.1 or thickness <= 0.1 or abs(sweep) < 0.001:
            return
        angle = np.arctan2(self.y - cy, self.x - cx)
        rel = np.mod((angle - start) * np.sign(sweep), math.tau)
        span = abs(sweep)
        inside = rel <= span
        along = np.where(inside, rel / span, 0.0)
        radial = np.abs(np.sqrt((self.x - cx) ** 2 + (self.y - cy) ** 2) - radius)
        profile = np.clip(1.0 - radial / thickness, 0.0, 1.0) ** 1.3
        weight = np.clip(along ** taper, 0.0, 1.0) if taper != 0 else 1.0
        if shred > 0.0:
            # The trailing half of a cut comes apart into strands rather than
            # ending as a clean painted band.
            strands = 0.5 + 0.5 * np.sin(rel * 34.0 + radial * 0.55)
            weight = weight * (1.0 - shred + shred * strands)
        self.e += np.where(inside, profile * weight * strength, 0.0)

    def streak(self, p0: tuple[float, float], p1: tuple[float, float], width: float, strength: float = 1.0,
               head_bias: float = 0.0) -> None:
        x0, y0 = p0
        x1, y1 = p1
        dx, dy = x1 - x0, y1 - y0
        length2 = dx * dx + dy * dy
        if length2 < 1e-5 or width <= 0.05:
            return
        t = np.clip(((self.x - x0) * dx + (self.y - y0) * dy) / length2, 0.0, 1.0)
        px = x0 + t * dx
        py = y0 + t * dy
        d = np.sqrt((self.x - px) ** 2 + (self.y - py) ** 2)
        taper = 1.0 - head_bias * (1.0 - t)
        self.e += strength * np.clip(1.0 - d / width, 0.0, 1.0) ** 1.5 * taper

    def cone(self, cx: float, cy: float, angle: float, length: float, half_width: float,
             strength: float = 1.0) -> None:
        """A directional bloom. Used for muzzles and contact sprays."""
        if length <= 1.0:
            return
        ux, uy = math.cos(angle), math.sin(angle)
        along = (self.x - cx) * ux + (self.y - cy) * uy
        across = np.abs(-(self.x - cx) * uy + (self.y - cy) * ux)
        t = np.clip(along / length, 0.0, 1.0)
        width = half_width * (0.22 + 0.78 * np.clip(np.sin(t * math.pi), 0.0, 1.0) ** 0.7)
        inside = (along > 0) & (across < width)
        falloff = np.clip(1.0 - across / np.maximum(width, 0.001), 0.0, 1.0) ** 1.2
        falloff *= np.clip(1.0 - t, 0.0, 1.0) ** 0.85
        self.e += np.where(inside, falloff * strength, 0.0)

    def chunk(self, cx: float, cy: float, size: float, strength: float = 1.0) -> None:
        """A hard pixel cluster. Sparks and embers are chunks, never gradients."""
        half = max(size * 0.5, 0.5)
        mask = (np.abs(self.x - cx) <= half) & (np.abs(self.y - cy) <= half)
        self.e[mask] = np.maximum(self.e[mask], strength)

    # -- resolve ----------------------------------------------------------

    def to_image(self, ramp=RAMP, gain: float = 1.0, bands: int = BANDS) -> Image.Image:
        e = np.clip(self.e * gain, 0.0, 1.0)
        # Quantise. Banded light is what makes this read as pixel art under the
        # PointClamp sampler these sheets are actually drawn with.
        q = np.ceil(e * bands) / bands
        rgb = ramp_lookup(np.clip(q, 0.0, 1.0), ramp)
        out = np.zeros((self.size, self.size, 4), dtype=np.uint8)
        out[..., :3] = np.clip(rgb, 0, 255).astype(np.uint8)
        out[..., 3] = np.clip(q * 255.0, 0, 255).astype(np.uint8)
        out[..., :3][q <= 0.0001] = 0
        return Image.fromarray(out, "RGBA")

    def coverage(self, gain: float = 1.0) -> float:
        return float((np.clip(self.e * gain, 0, 1) > 0.06).mean())


# --------------------------------------------------------------------------
# Particles
#
# Simulated once for the whole sheet and sampled per frame. This is the single
# thing that separates an effect from a sequence of pictures: an ember has to
# leave, slow down and go out, and it has to be the *same* ember each frame.
# --------------------------------------------------------------------------


@dataclass
class Sparks:
    pos: np.ndarray
    vel: np.ndarray
    size: np.ndarray
    life: np.ndarray
    born: np.ndarray

    @staticmethod
    def burst(
        rng: np.random.Generator,
        count: int,
        origin: tuple[float, float],
        speed: tuple[float, float],
        angle: tuple[float, float] = (0.0, math.tau),
        size: tuple[float, float] = (2.0, 4.0),
        life: tuple[float, float] = (0.4, 0.9),
        born: tuple[float, float] = (0.0, 0.0),
    ) -> "Sparks":
        a = rng.uniform(angle[0], angle[1], count)
        s = rng.uniform(speed[0], speed[1], count)
        return Sparks(
            pos=np.stack([np.full(count, origin[0]), np.full(count, origin[1])], axis=1),
            vel=np.stack([np.cos(a) * s, np.sin(a) * s], axis=1),
            size=rng.uniform(size[0], size[1], count),
            life=rng.uniform(life[0], life[1], count),
            born=rng.uniform(born[0], born[1], count),
        )

    def sample(self, time: float, dt: float, drag: float = 3.4, gravity: float = 0.0, steps: int = 60):
        """Integrate from zero to `time`. Deterministic and cheap at these counts."""
        pos = self.pos.copy()
        vel = self.vel.copy()
        n = max(1, int(time / dt * steps / max(steps, 1)))
        step = time / max(n, 1)
        for _ in range(max(n, 1)):
            alive = (self.born <= time)[:, None]
            vel *= math.exp(-drag * step)
            vel[:, 1] += gravity * step
            pos += vel * step * alive
        age = np.clip((time - self.born) / np.maximum(self.life, 0.001), 0.0, 1.0)
        return pos, age

    def draw(self, field: Field, time: float, dt: float, strength: float = 1.0,
             drag: float = 3.4, gravity: float = 0.0, trail: float = 0.0) -> None:
        pos, age = self.sample(time, dt, drag, gravity)
        for i in range(len(age)):
            if self.born[i] > time or age[i] >= 1.0:
                continue
            fade = (1.0 - age[i]) ** 1.5
            if fade <= 0.02:
                continue
            x, y = pos[i]
            if trail > 0.0:
                back = pos[i] - self.vel[i] * trail * (1.0 - age[i]) * 0.02
                field.streak((back[0], back[1]), (x, y), max(self.size[i] * 0.42, 0.7),
                             strength * fade * 0.55)
            field.chunk(x, y, max(self.size[i] * (1.0 - age[i] * 0.5), 1.0), strength * fade)


def ease_out(t: float, power: float = 2.4) -> float:
    return 1.0 - (1.0 - min(max(t, 0.0), 1.0)) ** power


def ease_in(t: float, power: float = 2.2) -> float:
    return min(max(t, 0.0), 1.0) ** power


# --------------------------------------------------------------------------
# Effects
# --------------------------------------------------------------------------


def burning_detonation(size: int, frames: int) -> list[Field]:
    """A Burning coming apart. Implosion, flash, shockwave, embers, nothing.

    The old sheet spent nine of sixteen frames as a full-frame speckle field.
    Here the decay is explicit and the shockwave thins as it grows, so the last
    frame is empty.
    """
    rng = np.random.default_rng(20260906)
    c = size / 2
    dt = 1.0 / 24.0
    embers = Sparks.burst(rng, 44, (c, c), speed=(120.0, 520.0), size=(2.0, 5.5),
                          life=(0.30, 0.62), born=(0.045, 0.075))
    motes = Sparks.burst(rng, 9, (c, c), speed=(28.0, 70.0), angle=(-math.pi * 0.85, -math.pi * 0.15),
                         size=(2.0, 3.0), life=(0.5, 0.66), born=(0.10, 0.16))
    out: list[Field] = []
    for i in range(frames):
        t = i * dt
        f = Field(size)
        p = i / (frames - 1)

        if i <= 1:
            # Gather: the manifestation pulls in on itself before it lets go.
            k = 1.0 - i / 2.0
            f.ring(c, c, 14.0 + k * 34.0, 5.0 + k * 4.0, 0.34 + (1.0 - k) * 0.3)
            f.radial(c, c, 12.0 + (1.0 - k) * 12.0, 0.5 + (1.0 - k) * 0.5, 1.6)
        else:
            burst = (i - 2) / (frames - 3)
            # Core: violent, then gone by a third of the way through.
            core = max(0.0, 1.0 - burst * 3.1)
            if core > 0.0:
                f.disc(c, c, 8.0 + core * 17.0, core, edge=3.0)
                f.radial(c, c, 22.0 + core * 34.0, core * 0.85, 1.9)
                # Chunky spikes, uneven on purpose.
                for k in range(9):
                    a = math.tau * k / 9 + 0.21
                    reach = (26.0 + 34.0 * ((k * 37) % 7) / 7.0) * (0.5 + core)
                    f.streak((c, c), (c + math.cos(a) * reach, c + math.sin(a) * reach),
                             3.4 + core * 2.2, core * 0.75, head_bias=0.85)
            # Shockwave: expands and thins to nothing.
            radius = 20.0 + ease_out(burst, 2.0) * (size * 0.44 - 20.0)
            thickness = max(1.6, 13.0 * (1.0 - burst) ** 1.1)
            f.ring(c, c, radius, thickness, 0.92 * (1.0 - burst) ** 1.45,
                   wobble=0.17, gaps=0.42, seed=1)
            f.ring(c, c, radius * 0.70, thickness * 0.75, 0.34 * (1.0 - burst) ** 1.9,
                   wobble=0.22, gaps=0.58, seed=5)
            # Torn sheets of the manifestation, thrown clear and turning.
            for k in range(4):
                a = 0.55 + k * 1.61
                span = 0.44 * (1.0 - burst * 0.55)
                f.arc(c, c, radius * (0.86 + 0.12 * (k % 2)), thickness * 0.85,
                      a - span * 0.5 + burst * 0.5, span,
                      0.5 * (1.0 - burst) ** 1.7, taper=0.0, shred=0.5)

        embers.draw(f, t, dt, strength=0.95, drag=4.2, trail=1.4)
        motes.draw(f, t, dt, strength=0.6, drag=1.5, gravity=-24.0)
        out.append(f)
    return out


def soul_release(size: int, frames: int) -> list[Field]:
    """A Soul letting go. The quietest effect in the game, and the one that has
    to carry the most: it is the moment the whole premise is about."""
    rng = np.random.default_rng(4242)
    c = size / 2
    dt = 1.0 / 12.0
    motes = Sparks.burst(rng, 16, (c, c + 8.0), speed=(14.0, 46.0),
                         angle=(-math.pi * 0.92, -math.pi * 0.08),
                         size=(1.6, 3.0), life=(0.62, 1.05), born=(0.16, 0.62))
    out: list[Field] = []
    for i in range(frames):
        t = i * dt
        p = i / (frames - 1)
        f = Field(size)

        # The knot: bright, tight, and it lifts as it unwinds.
        knot = max(0.0, 1.0 - ease_in(p / 0.62, 1.7)) if p < 0.62 else 0.0
        cy = c + 10.0 - p * 30.0
        if knot > 0.0:
            f.disc(c, cy, 2.4 + knot * 3.2, knot * 0.95, edge=1.6)
            f.radial(c, cy, 10.0 + knot * 9.0, knot * 0.7, 1.8)

        # The ribbon: a rising helix, drawn as short arcs so it stays a shape
        # rather than a smear.
        rise = ease_out(p, 1.6)
        turns = 2.4
        segments = 16
        for s in range(segments):
            u = s / (segments - 1)
            if u > rise + 0.12:
                break
            a = u * math.tau * turns + p * 1.6
            width = (10.0 + 5.0 * math.sin(u * math.pi)) * (1.0 - u * 0.55)
            x = c + math.cos(a) * width
            y = cy - u * (size * 0.34)
            strength = 0.85 * (1.0 - u * 0.7) * (1.0 - p * 0.55) * min(1.0, (rise + 0.12 - u) * 5.0)
            if strength > 0.02:
                f.radial(x, y, 5.4 + 2.6 * (1.0 - u), strength, 1.3)
                f.chunk(x, y, 2.0, min(1.0, strength * 1.25))
        # A soft column the ribbon climbs, so the release has somewhere to go.
        f.streak((c, cy + 4.0), (c, cy - rise * size * 0.36), 6.0 + 4.0 * (1.0 - p),
                 0.26 * (1.0 - p * 0.6), head_bias=0.85)

        motes.draw(f, t, dt, strength=0.72, drag=0.9, gravity=-16.0)
        out.append(f)
    return out


def core_hit(size: int, frames: int) -> list[Field]:
    """A hit landing. Reads along +x; the runtime rotates it to the strike."""
    rng = np.random.default_rng(1717)
    c = size / 2
    dt = 1.0 / 30.0
    spray = Sparks.burst(rng, 20, (c, c), speed=(180.0, 620.0),
                         angle=(-0.85, 0.85), size=(2.0, 4.4),
                         life=(0.13, 0.28), born=(0.0, 0.02))
    out: list[Field] = []
    for i in range(frames):
        t = i * dt
        p = i / (frames - 1)
        f = Field(size)
        flash = max(0.0, 1.0 - p * 2.6)
        if flash > 0.0:
            f.disc(c, c, 3.0 + flash * 7.0, flash, edge=2.0)
            # Four-point star: the classic impact read, kept chunky.
            for a, reach in ((0.0, 40.0), (math.pi, 22.0), (math.pi / 2, 17.0), (-math.pi / 2, 17.0)):
                r = reach * (0.35 + flash)
                f.streak((c, c), (c + math.cos(a) * r, c + math.sin(a) * r),
                         2.6 + flash * 3.4, flash * 0.9, head_bias=0.9)
        # A short perpendicular shock, so the hit has a plane and not just a blob.
        shock = max(0.0, 1.0 - abs(p - 0.22) * 4.2)
        if shock > 0.0:
            f.arc(c, c, 14.0 + p * 26.0, 4.5 * (1.0 - p) + 1.2, -1.15, 2.30, shock * 0.7, taper=0.0)
        spray.draw(f, t, dt, strength=0.9, drag=8.5, trail=1.9)
        out.append(f)
    return out


def slash(size: int, frames: int, *, radius: float, thickness: float, sweep: float,
          strength: float, second: bool, seed: int, shred: int = 0) -> list[Field]:
    """A blade passing. Authored along +x; the runtime rotates it to the swing."""
    rng = np.random.default_rng(seed)
    c = size / 2
    dt = 1.0 / 24.0
    chips = Sparks.burst(rng, shred, (c + radius * 0.72, c), speed=(90.0, 300.0),
                         angle=(-1.5, 1.5), size=(1.8, 3.6), life=(0.16, 0.34),
                         born=(0.0, 0.05)) if shred else None
    out: list[Field] = []
    for i in range(frames):
        t = i * dt
        p = i / (frames - 1)
        f = Field(size)
        # The arc is born wide and bright at the leading edge, then the tail
        # catches up and the whole thing thins out.
        travel = ease_out(p, 2.2)
        start = -sweep * 0.5 - 0.25 + travel * 0.5
        span = sweep * (0.34 + 0.66 * travel)
        life = (1.0 - ease_in(p, 2.6))
        f.arc(c, c, radius, thickness * (1.0 - p * 0.55) + 1.0, start, span,
              strength * life, taper=1.5, shred=0.34 + p * 0.3)
        # Leading edge: a brighter, thinner head so the cut has a direction.
        head = start + span
        f.arc(c, c, radius, thickness * 0.42 * (1.0 - p * 0.4) + 0.8,
              head - 0.36, 0.36, strength * life * 1.25, taper=0.0)
        if second:
            f.arc(c, c, radius * 0.78, thickness * 0.5 * (1.0 - p * 0.6) + 0.8,
                  start + 0.14, span * 0.86, strength * life * 0.55, taper=1.8, shred=0.5)
        if chips is not None:
            chips.draw(f, t, dt, strength=0.8 * life, drag=6.0, trail=1.5)
        out.append(f)
    return out


def dash_ignition(size: int, frames: int) -> list[Field]:
    """The floor the Warden left. Spawned unrotated, so it is radial."""
    rng = np.random.default_rng(9091)
    c = size / 2
    dt = 1.0 / 45.0
    dust = Sparks.burst(rng, 18, (c, c), speed=(90.0, 260.0), size=(1.6, 3.2),
                        life=(0.08, 0.17), born=(0.0, 0.02))
    out: list[Field] = []
    for i in range(frames):
        t = i * dt
        p = i / (frames - 1)
        f = Field(size)
        life = 1.0 - ease_in(p, 1.5)
        f.ring(c, c, 6.0 + ease_out(p, 1.8) * 34.0, 6.5 * life + 1.2, 0.75 * life,
               wobble=0.2, gaps=0.5, seed=2)
        f.radial(c, c, 10.0 + p * 8.0, 0.45 * life, 2.0)
        dust.draw(f, t, dt, strength=0.7 * life, drag=9.0, trail=1.1)
        out.append(f)
    return out


def cannon_charge_loop(size: int, frames: int) -> list[Field]:
    """Soul-stuff drawn inward. Loops, so every term is periodic in `p`."""
    c = size / 2
    out: list[Field] = []
    for i in range(frames):
        p = i / frames
        f = Field(size)
        pulse = 0.5 + 0.5 * math.sin(p * math.tau)
        f.disc(c, c, 3.0 + pulse * 1.6, 0.75 + pulse * 0.2, edge=1.5)
        f.radial(c, c, 13.0 + pulse * 4.0, 0.45, 1.8)
        # Motes spiralling in. Each has its own phase offset, and every one
        # completes a whole revolution across the sheet, so the loop is seamless.
        for k in range(7):
            phase = (p + k / 7.0) % 1.0
            a = phase * math.tau * 1.0 + k * 0.9
            r = 8.0 + (1.0 - phase) * 34.0
            f.chunk(c + math.cos(a) * r, c + math.sin(a) * r, 2.2, 0.55 * (0.25 + phase * 0.75))
            f.streak((c + math.cos(a + 0.30) * (r + 5.0), c + math.sin(a + 0.30) * (r + 5.0)),
                     (c + math.cos(a) * r, c + math.sin(a) * r), 1.5, 0.28 * phase)
        out.append(f)
    return out


def cannon_muzzle(size: int, frames: int) -> list[Field]:
    """A full-charge discharge, authored along +x."""
    rng = np.random.default_rng(3113)
    c = size / 2
    dt = 1.0 / 24.0
    spit = Sparks.burst(rng, 26, (c + 8.0, c), speed=(260.0, 780.0), angle=(-0.55, 0.55),
                        size=(2.0, 4.6), life=(0.14, 0.30), born=(0.0, 0.03))
    out: list[Field] = []
    for i in range(frames):
        t = i * dt
        p = i / (frames - 1)
        f = Field(size)
        life = 1.0 - ease_in(p, 2.0)
        f.cone(c, c, 0.0, 46.0 + ease_out(p, 1.6) * 78.0, 30.0 * life + 4.0, 0.82 * life)
        f.streak((c - 6.0, c), (c + 40.0 + p * 70.0, c), 6.0 * life + 1.5, 0.9 * life, head_bias=0.8)
        f.disc(c + 4.0, c, 5.0 + life * 8.0, life, edge=2.5)
        # Petals: four chunky flames pushed out of the mouth, uneven.
        for k, a in enumerate((-0.62, -0.24, 0.24, 0.62)):
            reach = (22.0 + k % 2 * 14.0) * (0.4 + life)
            f.streak((c + 6.0, c), (c + 6.0 + math.cos(a) * reach, c + math.sin(a) * reach),
                     4.2 * life + 1.0, 0.55 * life, head_bias=0.9)
        # Backblast ring at the brace.
        f.ring(c - 2.0, c, 8.0 + p * 22.0, 4.5 * life + 1.0, 0.32 * life,
               wobble=0.24, gaps=0.55, seed=6)
        spit.draw(f, t, dt, strength=0.9, drag=7.0, trail=2.2)
        out.append(f)
    return out


def cannon_projectile(size: int, frames: int) -> list[Field]:
    """The travelling Soul bolt. Head at +x, loops on a flicker."""
    c = size / 2
    out: list[Field] = []
    for i in range(frames):
        p = i / frames
        f = Field(size)
        flick = 0.5 + 0.5 * math.sin(p * math.tau)
        flick2 = 0.5 + 0.5 * math.sin(p * math.tau * 2.0 + 1.1)
        head = c + 22.0
        # A bolt with weight: a solid head, a wrapped body and a shedding tail.
        f.disc(head, c, 6.6 + flick * 1.4, 1.0, edge=2.0)
        f.radial(head, c, 20.0 + flick * 4.0, 0.78, 1.6)
        f.streak((c - 34.0 - flick2 * 8.0, c), (head, c), 9.0 + flick * 2.0, 0.78, head_bias=0.9)
        f.streak((c - 16.0, c), (head, c), 4.6, 0.72, head_bias=0.7)
        # Two counter-rotating strands wrapped around the shaft.
        for strand in (0, 1):
            for k in range(6):
                u = k / 5.0
                a = p * math.tau + strand * math.pi + u * 3.4
                x = head - 6.0 - u * 34.0
                y = c + math.sin(a) * (3.0 + u * 6.5)
                f.chunk(x, y, 2.6 - u * 1.0, 0.62 * (1.0 - u * 0.7))
        # Shed motes falling behind the bolt.
        for k in range(3):
            phase = (p + k / 3.0) % 1.0
            x = head - 14.0 - phase * 38.0
            y = c + math.sin(phase * math.tau + k * 2.1) * (4.0 + phase * 10.0)
            f.chunk(x, y, 2.4, 0.5 * (1.0 - phase))
        out.append(f)
    return out


def resonance_activate(size: int, frames: int) -> list[Field]:
    """Soul Resonance. Two rings offset in time, and the room's residue rising."""
    rng = np.random.default_rng(555)
    c = size / 2
    dt = 1.0 / 24.0
    rising = Sparks.burst(rng, 22, (c, c + 26.0), speed=(40.0, 130.0),
                          angle=(-math.pi * 0.88, -math.pi * 0.12), size=(2.0, 3.6),
                          life=(0.30, 0.58), born=(0.0, 0.22))
    out: list[Field] = []
    for i in range(frames):
        t = i * dt
        p = i / (frames - 1)
        f = Field(size)
        core = max(0.0, 1.0 - ease_in(p / 0.5, 1.6)) if p < 0.5 else 0.0
        if core > 0.0:
            f.disc(c, c, 4.0 + core * 8.0, core * 0.9, edge=2.2)
            f.radial(c, c, 20.0 + core * 18.0, core * 0.55, 1.8)
        for offset, weight in ((0.0, 1.0), (0.22, 0.62)):
            q = (p - offset) / (1.0 - offset)
            if q <= 0.0:
                continue
            radius = 12.0 + ease_out(q, 1.9) * (size * 0.45 - 12.0)
            thickness = max(1.4, 10.5 * (1.0 - q) ** 1.15)
            f.ring(c, c, radius, thickness, 0.78 * weight * (1.0 - q) ** 1.35,
                   wobble=0.13, gaps=0.40, seed=3 if offset == 0.0 else 8)
        rising.draw(f, t, dt, strength=0.62, drag=1.9, gravity=-40.0, trail=1.0)
        out.append(f)
    return out


def death_flame_loop(size: int, frames: int) -> list[Field]:
    """A small standing Death Flame. Loops."""
    c = size / 2
    out: list[Field] = []
    for i in range(frames):
        p = i / frames
        f = Field(size)
        lick = math.sin(p * math.tau)
        lick2 = math.sin(p * math.tau * 2.0 + 0.7)
        base = c + 14.0
        height = 26.0 + lick * 5.0
        # Teardrop: a stack of shrinking discs bent by the flicker.
        segments = 9
        for s in range(segments):
            u = s / (segments - 1)
            x = c + lick2 * u * u * 4.6
            y = base - u * height
            r = (6.4 * (1.0 - u * 0.82)) + 0.6
            f.radial(x, y, r * 2.0, 0.42 + 0.5 * (1.0 - u), 1.5)
        f.disc(c, base - 5.0, 3.0, 0.85, edge=1.6)
        out.append(f)
    return out


# --------------------------------------------------------------------------
# Sheets
# --------------------------------------------------------------------------

SPEC: dict[str, tuple[int, int, int, object, float]] = {
    # name: (frame size, frames, columns, builder, gain)
    "fx_burning_detonation": (256, 16, 4, burning_detonation, 1.0),
    "fx_resonance_activate": (256, 16, 4, resonance_activate, 1.0),
    "fx_soul_release": (128, 16, 4, soul_release, 1.0),
    "fx_core_hit": (128, 9, 3, core_hit, 1.0),
    "fx_dash_ignition": (128, 9, 3, dash_ignition, 1.0),
    "fx_cannon_charge_loop": (128, 9, 3, cannon_charge_loop, 1.0),
    "fx_cannon_muzzle_full": (256, 9, 3, cannon_muzzle, 1.0),
    "fx_cannon_projectile_full": (128, 9, 3, cannon_projectile, 1.0),
    "fx_death_flame_loop": (128, 9, 3, death_flame_loop, 1.0),
}


def build_sheet(name: str) -> tuple[Image.Image, float]:
    if name == "fx_scythe_slash_01":
        size, frames, cols = 256, 9, 3
        fields = slash(size, frames, radius=86.0, thickness=9.0, sweep=2.05,
                       strength=0.80, second=False, seed=11)
    elif name == "fx_scythe_slash_02":
        size, frames, cols = 256, 9, 3
        fields = slash(size, frames, radius=94.0, thickness=12.0, sweep=2.35,
                       strength=0.92, second=True, seed=22, shred=9)
    elif name == "fx_scythe_cleave":
        size, frames, cols = 256, 9, 3
        fields = slash(size, frames, radius=104.0, thickness=17.0, sweep=3.05,
                       strength=1.0, second=True, seed=33, shred=16)
    else:
        size, frames, cols, builder, _ = SPEC[name]
        fields = builder(size, frames)

    rows = (frames + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * size, rows * size), (0, 0, 0, 0))
    peak = 0.0
    for index, field in enumerate(fields):
        peak = max(peak, field.coverage())
        sheet.paste(field.to_image(), ((index % cols) * size, (index // cols) * size))
    return sheet, peak


ALL = list(SPEC.keys()) + ["fx_scythe_slash_01", "fx_scythe_slash_02", "fx_scythe_cleave"]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--only", default=None)
    args = parser.parse_args()
    args.out.mkdir(parents=True, exist_ok=True)

    names = [args.only] if args.only else ALL
    failed = False
    for name in names:
        sheet, peak = build_sheet(name)
        sheet.save(args.out / f"{name}.png")
        flag = ""
        if peak > COVERAGE_LIMIT:
            flag = f"  ** OVER COVERAGE LIMIT {COVERAGE_LIMIT:.2f} **"
            failed = True
        print(f"wrote {name}.png {sheet.size} peak-coverage {peak:.3f}{flag}")

    if failed:
        print("\nAn effect fills too much of its frame. That is how the old "
              "detonation became a square; fix the effect rather than the limit.")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
