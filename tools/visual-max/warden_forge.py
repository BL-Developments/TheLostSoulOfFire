#!/usr/bin/env python3
"""Authored pixel-art forge for the Warden character sheets.

Why this exists
---------------
The delivered Warden sheets were generated per direction, so the eight
directions were eight different creatures seen through eight different cameras:
a bat-winged side view for east, a bird's-eye blob for west, an unreadable mass
for north and south. Sweeping the mouse morphed the protagonist rather than
turning him, which is the "facing looks broken" problem the owner reported. No
amount of runtime code can fix that, because the frames themselves disagree.

So the character is authored here instead, from one rig:

  * one body plan, one palette, one camera;
  * a real ground-plane projection, so the eight directions are one character
    rotating rather than eight drawings;
  * a real gait, so the run cycle actually moves;
  * flat fills, hard value steps, no dithering and no anti-aliasing, so the
    result is pixel art rather than a small painting.

Everything is drawn at 128x128 per frame and displayed at exactly 1:1 on screen
(see WardenIdentity.DisplaySize), so one authored pixel is one screen pixel.

Usage:
    python3 tools/visual-max/warden_forge.py --out <dir> [--sheet-preview]
"""

from __future__ import annotations

import argparse
import math
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np
from PIL import Image

# --------------------------------------------------------------------------
# Palette
#
# Six material families, two or three values each. Deliberately small: the
# owner's note was that the art is "too detailed and too shiny", and most of
# that came from sheets carrying hundreds of near-identical colours.
# --------------------------------------------------------------------------

PALETTE: dict[str, tuple[int, int, int]] = {
    # coat / cloth — the character's mass
    "coat_dk": (0x25, 0x21, 0x33),
    "coat_md": (0x3C, 0x34, 0x50),
    "coat_lt": (0x57, 0x4B, 0x70),
    "coat_hi": (0x76, 0x66, 0x93),
    # under-layer, trousers
    "under_dk": (0x1C, 0x1A, 0x25),
    "under_md": (0x29, 0x25, 0x33),
    # leather straps and belt — warm, so it separates from the violet cloth
    "leather_dk": (0x2E, 0x23, 0x24),
    "leather_md": (0x46, 0x34, 0x32),
    "leather_lt": (0x60, 0x47, 0x40),
    # the one saturated human note: a rust-red scarf
    "scarf_dk": (0x5C, 0x2E, 0x2C),
    "scarf_md": (0x73, 0x3B, 0x36),
    "scarf_lt": (0x8E, 0x4F, 0x43),
    # skin — pale and drained, but the brightest thing on the body, so the face
    # is where the eye lands
    "skin_dk": (0x74, 0x5B, 0x62),
    "skin_md": (0xA4, 0x86, 0x88),
    "skin_hi": (0xC8, 0xAC, 0xA8),
    # hair
    "hair_dk": (0x1F, 0x1B, 0x26),
    "hair_md": (0x33, 0x2C, 0x3A),
    # iron
    "iron_dk": (0x24, 0x27, 0x30),
    "iron_md": (0x44, 0x49, 0x58),
    "iron_hi": (0x6D, 0x74, 0x88),
    # wood
    "wood_dk": (0x2C, 0x22, 0x24),
    "wood_md": (0x46, 0x36, 0x33),
    # Death Flame — used on exactly three notes: the bound Soul at the sternum,
    # the eyes, and the coat hem.
    "flame": (0xA8, 0x6C, 0xFF),
    "flame_hi": (0xDE, 0xD0, 0xFF),
    "soul_white": (0xF6, 0xF0, 0xFF),
}

FRAME = 128
GROUND_Y = 104.0          # feet line inside the frame
CENTER_X = 64.0
SQUASH = 0.52             # how flat the ground plane reads: the camera pitch

# Overall figure size inside the frame. Measured against the delivered enemy
# sheets: at 1.0 the Warden's head reached a Hollow's waist and he read as a
# child in his own encounter. Applied inside Rig.project, so every body-space
# quantity — proportion, stride, reach, weapon — scales together and the rig
# stays one coherent character.
FIGURE = 1.15

IDLE_FRAMES = 12
MOVE_FRAMES = 12
ATTACK_FRAMES = 6
SHEET_COLUMNS = 4

DIRECTIONS: dict[str, tuple[float, float]] = {
    "e": (1.0, 0.0),
    "se": (0.7071, 0.7071),
    "s": (0.0, 1.0),
    "sw": (-0.7071, 0.7071),
    "w": (-1.0, 0.0),
    "nw": (-0.7071, -0.7071),
    "n": (0.0, -1.0),
    "ne": (0.7071, -0.7071),
}


# --------------------------------------------------------------------------
# Canvas
# --------------------------------------------------------------------------


class Canvas:
    """Flat-fill RGBA raster. No anti-aliasing anywhere, by design."""

    def __init__(self, width: int = FRAME, height: int = FRAME):
        self.w = width
        self.h = height
        self.rgb = np.zeros((height, width, 3), dtype=np.uint8)
        self.a = np.zeros((height, width), dtype=bool)
        ys, xs = np.mgrid[0:height, 0:width]
        self._xs = xs.astype(np.float32)
        self._ys = ys.astype(np.float32)

    # -- raw mask painting ------------------------------------------------

    def paint(self, mask: np.ndarray, colour: str | tuple[int, int, int]) -> None:
        rgb = PALETTE[colour] if isinstance(colour, str) else colour
        self.rgb[mask] = rgb
        self.a |= mask

    def paint_shaded(
        self,
        mask: np.ndarray,
        dark: str,
        mid: str,
        light: str | None = None,
        rim: int = 1,
    ) -> np.ndarray:
        """Fill with a three-value read: light on the up-left, dark on the down-right.

        One key light from the upper left, applied identically to every form.
        This is what keeps the sheet reading as one object under one lamp
        instead of a collage.
        """
        if not mask.any():
            return mask
        self.paint(mask, mid)
        lo = mask & ~shift(mask, -rim, -rim)
        self.paint(mask & shift(mask, rim, rim) & ~lo, mid)
        self.paint(mask & ~shift(mask, rim, rim), dark)
        if light is not None:
            self.paint(lo, light)
        return mask

    # -- shape masks ------------------------------------------------------

    def ellipse(self, cx: float, cy: float, rx: float, ry: float) -> np.ndarray:
        rx = max(rx, 0.5)
        ry = max(ry, 0.5)
        dx = (self._xs - cx) / rx
        dy = (self._ys - cy) / ry
        return dx * dx + dy * dy <= 1.0

    def capsule(self, p0: tuple[float, float], p1: tuple[float, float], r: float) -> np.ndarray:
        x0, y0 = p0
        x1, y1 = p1
        dx, dy = x1 - x0, y1 - y0
        length2 = dx * dx + dy * dy
        if length2 < 1e-6:
            return self.ellipse(x0, y0, r, r)
        t = ((self._xs - x0) * dx + (self._ys - y0) * dy) / length2
        t = np.clip(t, 0.0, 1.0)
        px = x0 + t * dx
        py = y0 + t * dy
        return (self._xs - px) ** 2 + (self._ys - py) ** 2 <= r * r

    def polygon(self, points: list[tuple[float, float]]) -> np.ndarray:
        """Even-odd scanline fill."""
        mask = np.zeros((self.h, self.w), dtype=bool)
        n = len(points)
        if n < 3:
            return mask
        for y in range(self.h):
            yc = y + 0.5
            crossings: list[float] = []
            for i in range(n):
                x0, y0 = points[i]
                x1, y1 = points[(i + 1) % n]
                if (y0 <= yc < y1) or (y1 <= yc < y0):
                    crossings.append(x0 + (yc - y0) / (y1 - y0) * (x1 - x0))
            crossings.sort()
            for i in range(0, len(crossings) - 1, 2):
                a = int(math.floor(crossings[i] + 0.5))
                b = int(math.floor(crossings[i + 1] + 0.5))
                if b <= a:
                    b = a + 1
                a = max(a, 0)
                b = min(b, self.w)
                if b > a:
                    mask[y, a:b] = True
        return mask

    def to_image(self) -> Image.Image:
        out = np.zeros((self.h, self.w, 4), dtype=np.uint8)
        out[..., :3] = self.rgb
        out[..., 3] = np.where(self.a, 255, 0)
        return Image.fromarray(out, "RGBA")


def convex_hull(points: list[tuple[float, float]]) -> list[tuple[float, float]]:
    """Andrew monotone chain. Wrapping the projected cross-sections keeps the
    torso a single clean mass in all eight directions."""
    pts = sorted(set(points))
    if len(pts) < 3:
        return pts

    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])

    lower: list[tuple[float, float]] = []
    for p in pts:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    upper: list[tuple[float, float]] = []
    for p in reversed(pts):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    return lower[:-1] + upper[:-1]


def shift(mask: np.ndarray, dx: int, dy: int) -> np.ndarray:
    out = np.zeros_like(mask)
    h, w = mask.shape
    sx0, sx1 = max(0, -dx), min(w, w - dx)
    dx0, dx1 = max(0, dx), min(w, w + dx)
    sy0, sy1 = max(0, -dy), min(h, h - dy)
    dy0, dy1 = max(0, dy), min(h, h + dy)
    if sx1 > sx0 and sy1 > sy0:
        out[dy0:dy1, dx0:dx1] = mask[sy0:sy1, sx0:sx1]
    return out


# --------------------------------------------------------------------------
# Rig
# --------------------------------------------------------------------------


@dataclass
class Build:
    """The parameters that make one Warden a specific person."""

    name: str
    hooded: bool = False
    scarf: bool = True
    shoulder: float = 13.5
    shoulder_depth: float = 9.2
    hip: float = 9.4
    hip_depth: float = 8.0
    chest: float = 8.0
    hem_drop: float = 12.0
    coat_flare: float = 3.6
    coat: tuple[str, str, str, str] = ("coat_dk", "coat_md", "coat_lt", "coat_hi")
    scarf_colours: tuple[str, str, str] = ("scarf_dk", "scarf_md", "scarf_lt")
    head_r: float = 7.4
    stature: float = 1.0


PROTAGONIST = Build(name="warden")
ELDER = Build(
    name="warden_elder",
    hooded=True,
    scarf=False,
    shoulder=14.8,
    shoulder_depth=10.2,
    hip=10.0,
    hip_depth=8.6,
    hem_drop=19.0,
    coat_flare=5.4,
    stature=1.04,
)


class Rig:
    """One character, one direction, one moment in time.

    Body-space is a right-handed ground plane plus a height axis:
      a  = forward along the facing direction
      b  = to the character's right
      h  = up from the floor

    project() is the only place the camera exists. Because every part goes
    through it, the eight directions cannot drift apart.
    """

    def __init__(self, build: Build, facing: tuple[float, float]):
        self.build = build
        self.fx, self.fy = facing
        # Frontality: 1 facing the camera, -1 facing away, 0 in profile.
        self.front = self.fy
        self.profile = abs(self.fx)
        self.bob = 0.0
        self.lean = 0.0
        # Whole-body motion. Owner note: the first pass read as legs moving
        # under a static torso. A person does not run with their feet; the
        # pelvis shifts onto the stance leg, the shoulders counter-rotate
        # against the hips, and the head arrives late. All three live here so
        # every part inherits them and nothing can drift out of phase.
        self.sway = 0.0
        self.twist = 0.0

    # -- camera -----------------------------------------------------------

    def s(self, value: float) -> float:
        """Scale a screen-space length (limb thickness, head radius) with the figure."""
        return value * FIGURE

    def ground(self, a: float, b: float, h: float) -> tuple[float, float]:
        """Projection with no body motion. Planted feet use this: a foot on the
        floor must not sway with the hips or it slides."""
        a *= FIGURE
        b *= FIGURE
        h *= FIGURE
        gx = a * self.fx + b * self.fy
        gy = a * self.fy - b * self.fx
        return (CENTER_X + gx, GROUND_Y + gy * SQUASH - h + self.bob)

    def project(self, a: float, b: float, h: float) -> tuple[float, float]:
        # Twist ramps in from the hips to the shoulders and slightly overshoots
        # at head height, which is what makes the head read as arriving late.
        if self.twist != 0.0 or self.sway != 0.0:
            reach = max(self.shoulder_h - self.hip_h, 0.001)
            t = min(max((h - self.hip_h * 0.45) / reach, 0.0), 1.35)
            angle = self.twist * t
            ca, sa = math.cos(angle), math.sin(angle)
            a, b = a * ca - b * sa, a * sa + b * ca
            b += self.sway * min(1.0, 0.35 + t * 0.75)
        a *= FIGURE
        b *= FIGURE
        h *= FIGURE
        gx = a * self.fx + b * self.fy
        gy = a * self.fy - b * self.fx
        return (
            CENTER_X + gx,
            GROUND_Y + gy * SQUASH - h + self.bob,
        )

    def depth(self, a: float, b: float) -> float:
        """How close to the camera a ground point is. Drives draw order."""
        return a * self.fy - b * self.fx

    # -- proportion -------------------------------------------------------

    def width(self, front_value: float, profile_value: float) -> float:
        """Body-space blend between the front-on and the profile measurement."""
        return front_value * (1.0 - self.profile) + profile_value * self.profile

    @property
    def shoulder_h(self) -> float:
        return 60.0 * self.build.stature

    @property
    def hip_h(self) -> float:
        return 34.0 * self.build.stature

    @property
    def head_h(self) -> float:
        return 71.5 * self.build.stature

    def ring(self, h: float, half_width: float, depth: float, lean: float = 0.0,
             samples: int = 14) -> list[tuple[float, float]]:
        """Project one horizontal cross-section of the body.

        Without this the torso had width only along the b axis, so in profile
        both shoulders projected onto the same screen column and the character
        collapsed to a stick. A body has depth; the camera has to see it.
        """
        points = []
        for index in range(samples):
            angle = math.tau * index / samples
            a = math.cos(angle) * depth + lean
            b = math.sin(angle) * half_width
            points.append(self.project(a, b, h))
        return points

    @property
    def shoulder_b(self) -> float:
        return self.width(self.build.shoulder, self.build.shoulder_depth)

    @property
    def hip_b(self) -> float:
        return self.width(self.build.hip, self.build.hip_depth)


# --------------------------------------------------------------------------
# Motion
# --------------------------------------------------------------------------


@dataclass
class Pose:
    """Everything a frame needs, in body space."""

    leg: list[tuple[float, float]] = field(default_factory=list)   # (along, lift) per leg
    arm: list[tuple[float, float, float]] = field(default_factory=list)  # (along, lift, out)
    bob: float = 0.0
    lean: float = 0.0
    sway: float = 0.0
    twist: float = 0.0
    coat_trail: float = 0.0
    scarf_trail: float = 0.0
    head_tilt: float = 0.0
    weapon: str = "rest"
    weapon_angle: float = 0.0
    weapon_raise: float = 0.0
    # Body-space (a, b, h) hand targets. When set they override the swing arms,
    # so the hands travel along the same arc the weapon does.
    hand_body: list[tuple[float, float, float]] | None = None


def idle_pose(t: float) -> Pose:
    """Breathing only. Feet planted, no drift: an idle that wanders is the
    single fastest way to make a character look cheap."""
    breathe = math.sin(t * math.tau)
    settle = math.sin(t * math.tau * 2.0)
    return Pose(
        leg=[(1.4, 0.0), (-1.6, 0.0)],
        arm=[
            (0.4 + breathe * 0.25, -17.0 + breathe * 0.5, 0.6),
            (0.2 + breathe * 0.25, -17.0 + breathe * 0.5, 0.5),
        ],
        bob=breathe * 0.6 + settle * 0.15,
        # A slow shift of weight from one foot to the other. Without it an idle
        # reads as a paused frame rather than as someone waiting.
        sway=math.sin(t * math.tau) * 0.9,
        twist=math.sin(t * math.tau - 0.6) * 0.045,
        lean=0.5,
        coat_trail=1.2 + breathe * 0.7,
        scarf_trail=2.4 + breathe * 1.2,
        head_tilt=breathe * 0.35,
        weapon="rest",
        weapon_raise=breathe * 0.5,
    )


def run_pose(t: float) -> Pose:
    """A run, not a shuffle.

    Phase convention per leg: 0 = foot forward and planting, 0.25 = under the
    body, 0.5 = fully behind at toe-off, 0.75 = swinging through, lifted.
    """
    stride = 11.5
    lift = 6.2

    def leg(phase: float) -> tuple[float, float]:
        ph = (t + phase) % 1.0
        along = math.cos(ph * math.tau) * stride
        raised = max(0.0, -math.sin(ph * math.tau))
        return along, (raised ** 1.35) * lift

    def arm(phase: float) -> tuple[float, float, float]:
        ph = (t + phase) % 1.0
        along = -math.cos(ph * math.tau) * 8.5
        swing = math.sin(ph * math.tau)
        # Arms cross slightly inboard as they come forward, the way they do on a
        # real runner, instead of pistoning straight fore and aft.
        return along, -16.0 + swing * 2.6, 0.4 - max(0.0, along) * 0.045

    # Two footfalls per cycle: the body drops on contact and rises through flight.
    contact = math.cos(t * math.tau * 2.0)
    return Pose(
        leg=[leg(0.0), leg(0.5)],
        arm=[arm(0.5), arm(0.0)],
        bob=-1.9 - contact * 1.5,
        # Weight rolls onto the stance leg, and the shoulders answer the hips.
        sway=-math.sin(t * math.tau) * 2.6,
        twist=math.sin(t * math.tau) * 0.19,
        lean=3.4 + contact * 0.6,
        coat_trail=5.0 + math.sin(t * math.tau * 2.0) * 1.4,
        scarf_trail=6.0 + math.sin(t * math.tau * 2.0 + 0.8) * 1.8,
        head_tilt=-0.5,
        weapon="carry",
        weapon_raise=math.sin(t * math.tau * 2.0) * 1.2,
    )


def attack_pose(t: float) -> Pose:
    """Wind up, plant, cut, recover. Six frames, so every one has to be a pose
    the Player can read rather than an in-between.

    The hands travel along the same ground-plane arc the swing overlay does, so
    the weapon always leaves the Warden's grip instead of orbiting near him.
    """
    coil = max(0.0, 1.0 - abs(t - 0.16) / 0.30)
    drive = max(0.0, 1.0 - abs(t - 0.52) / 0.36)
    recover = max(0.0, (t - 0.66) / 0.34)

    # -1.15 rad behind the shoulder to +1.15 rad past it.
    swing = -1.15 + 2.30 * min(1.0, max(0.0, (t - 0.14) / 0.50))
    reach = 13.0 + drive * 4.0
    lead = (math.cos(swing) * reach, math.sin(swing) * reach, 54.0 - coil * 3.0 + drive * 1.5)
    trail_hand = (math.cos(swing + 0.55) * reach * 0.62,
                  math.sin(swing + 0.55) * reach * 0.62,
                  50.0 - coil * 2.0)

    return Pose(
        leg=[
            (5.0 + drive * 6.5 - coil * 3.5, 0.0),
            (-5.5 - coil * 2.5 + drive * 1.5, 0.0),
        ],
        arm=[(0.0, -6.0, 1.0), (0.0, -6.0, 1.0)],
        hand_body=[trail_hand, lead],
        bob=-1.2 * coil - 2.4 * drive + recover * 1.1,
        # The whole body turns through the cut. This is most of the difference
        # between a swing and an arm being waved.
        sway=-2.2 * coil + 3.0 * drive,
        twist=-0.34 * coil + 0.30 * min(1.0, max(0.0, (t - 0.18) / 0.42)) * 2.0,
        lean=-2.0 * coil + 7.5 * drive + 2.0 * recover,
        coat_trail=2.5 + coil * 3.0 + drive * 5.0,
        scarf_trail=3.0 + coil * 3.5 + drive * 6.0,
        head_tilt=-0.9 * coil + 1.4 * drive,
        weapon="swing",
        weapon_angle=swing,
    )


# --------------------------------------------------------------------------
# Drawing
# --------------------------------------------------------------------------


def draw_frame(build: Build, facing: tuple[float, float], pose: Pose, *, carry_weapon: bool) -> Image.Image:
    canvas = Canvas()
    rig = Rig(build, facing)
    rig.bob = pose.bob
    rig.sway = pose.sway
    rig.twist = pose.twist

    coat_dk, coat_md, coat_lt, coat_hi = build.coat
    parts: list[tuple[float, object]] = []

    # ---- legs -----------------------------------------------------------
    for index, (along, lift) in enumerate(pose.leg):
        side = -1.0 if index == 0 else 1.0
        b = side * rig.hip_b * 0.52
        parts.append((rig.depth(along * 0.5, b), _leg_drawer(canvas, rig, along, lift, b)))

    # ---- coat and torso -------------------------------------------------
    parts.append((rig.depth(-pose.coat_trail * 0.5, 0.0) - 0.6, _coat_back_drawer(canvas, rig, pose, coat_dk)))
    parts.append((rig.depth(0.0, 0.0), _torso_drawer(canvas, rig, pose, coat_dk, coat_md, coat_lt, coat_hi)))

    # ---- weapon ---------------------------------------------------------
    if carry_weapon:
        hand_a, hand_lift, _ = pose.arm[1]
        anchor_b = rig.shoulder_b * 1.32
        parts.append((rig.depth(hand_a, anchor_b) - 0.35, _weapon_drawer(canvas, rig, pose, hand_a, hand_lift, anchor_b)))

    # ---- arms -----------------------------------------------------------
    for index, (along, lift, out) in enumerate(pose.arm):
        side = -1.0 if index == 0 else 1.0
        b = side * rig.shoulder_b * 0.86
        target = pose.hand_body[index] if pose.hand_body else None
        depth = rig.depth(target[0], target[1]) if target else rig.depth(along * 0.6, b * (1.0 + out * 0.05))
        parts.append((depth, _arm_drawer(canvas, rig, along, lift, out, b, target)))

    # ---- head -----------------------------------------------------------
    parts.append((rig.depth(pose.lean * 0.35 + 1.0, 0.0) + 0.4, _head_drawer(canvas, rig, pose)))

    # ---- scarf ----------------------------------------------------------
    if build.scarf:
        parts.append((rig.depth(-pose.scarf_trail * 0.5, 0.0) + 0.5, _scarf_drawer(canvas, rig, pose)))

    parts.sort(key=lambda item: item[0])
    for _, drawer in parts:
        drawer()

    _soul_ember(canvas, rig, pose)
    _hem_flame(canvas, rig, pose, coat_dk)
    return canvas.to_image()


def _leg_drawer(canvas: Canvas, rig: Rig, along: float, lift: float, b: float):
    def draw() -> None:
        hip = rig.project(0.0, b, rig.hip_h)
        foot = rig.ground(along, b, lift)
        # A real knee. Straight sticks are the fastest way to make a run cycle
        # look like a slide.
        knee_swayed = rig.project(along * 0.40 + 2.4 + lift * 0.55, b, rig.hip_h * 0.50 + lift * 0.52)
        knee_planted = rig.ground(along * 0.40 + 2.4 + lift * 0.55, b, rig.hip_h * 0.50 + lift * 0.52)
        knee = ((knee_swayed[0] + knee_planted[0]) * 0.5, (knee_swayed[1] + knee_planted[1]) * 0.5)
        thigh = canvas.capsule(hip, knee, rig.s(4.0))
        shin = canvas.capsule(knee, (foot[0], foot[1] - rig.s(3.0)), rig.s(3.2))
        canvas.paint_shaded(thigh | shin, "under_dk", "under_md", "coat_md")
        heel = rig.ground(along - 2.0, b, lift)
        toe = rig.ground(along + 3.4, b, lift + 0.4)
        boot = canvas.capsule((heel[0], heel[1] - rig.s(2.4)), (toe[0], toe[1] - rig.s(2.4)), rig.s(3.4))
        canvas.paint_shaded(boot, "leather_dk", "leather_md", "leather_lt")

    return draw


def _arm_drawer(
    canvas: Canvas,
    rig: Rig,
    along: float,
    lift: float,
    out: float,
    b: float,
    target: tuple[float, float, float] | None = None,
):
    """Sleeves are a value darker than the coat body, so the arms stay separate
    masses instead of dissolving into the torso."""

    def draw() -> None:
        shoulder = rig.project(0.0, b, rig.shoulder_h - 1.5)
        if target is not None:
            hand = rig.project(*target)
            elbow = rig.project(
                (target[0] + 0.0) * 0.5,
                (target[1] + b) * 0.5,
                (target[2] + rig.shoulder_h - 1.5) * 0.5 - 1.5,
            )
        else:
            hand = rig.project(along, b * (1.0 + out * 0.14), rig.shoulder_h + lift)
            elbow = rig.project(along * 0.42, b * (1.0 + out * 0.22), rig.shoulder_h + lift * 0.52)
        upper = canvas.capsule(shoulder, elbow, rig.s(3.5))
        fore = canvas.capsule(elbow, hand, rig.s(3.0))
        canvas.paint_shaded(upper | fore, "under_dk", "coat_dk", "coat_md")
        canvas.paint_shaded(canvas.ellipse(hand[0], hand[1], rig.s(2.6), rig.s(2.5)), "skin_dk", "skin_md", "skin_hi")

    return draw


def _torso_drawer(canvas: Canvas, rig: Rig, pose: Pose, dk: str, md: str, lt: str, hi: str):
    def draw() -> None:
        build = rig.build
        lean = pose.lean
        chest = build.chest
        hem_h = rig.hip_h - build.hem_drop
        trail = pose.coat_trail

        shoulder_ring = rig.ring(rig.shoulder_h, build.shoulder, chest, lean * 0.7)
        waist_ring = rig.ring(rig.hip_h + 8.0, build.shoulder * 0.76, chest * 0.86, lean * 0.28)
        hem_ring = rig.ring(hem_h, build.hip + build.coat_flare, chest * 0.94, -trail * 0.45)

        body = canvas.polygon(convex_hull(shoulder_ring + waist_ring))
        skirt = canvas.polygon(convex_hull(waist_ring + hem_ring))
        neck = canvas.capsule(
            rig.project(lean * 0.8, 0.0, rig.shoulder_h + 4.0),
            rig.project(lean * 0.5, 0.0, rig.shoulder_h - 2.0),
            rig.s(3.0),
        )
        canvas.paint_shaded(neck, "skin_dk", "skin_dk", "skin_md")
        canvas.paint_shaded(body | skirt, dk, md, lt)

        # Shoulder caps: a single lighter cluster where the key light lands.
        caps = canvas.polygon(convex_hull(rig.ring(rig.shoulder_h + 1.0, build.shoulder * 0.99, chest * 0.9, lean * 0.7)))
        canvas.paint_shaded(caps & body, dk, lt, hi)

        # Belt: one clean band, no buckle filigree.
        belt = canvas.polygon(convex_hull(rig.ring(rig.hip_h + 6.0, build.shoulder * 0.79, chest * 0.89, lean * 0.28)))
        belt &= ~canvas.polygon(convex_hull(rig.ring(rig.hip_h + 10.0, build.shoulder * 0.79, chest * 0.89, lean * 0.28)))
        canvas.paint_shaded(belt & (body | skirt), "leather_dk", "leather_md", "leather_lt")

        # A single seam down the back, so the away-facing directions are not a
        # flat slab. One line, in shadow, no ornament.
        if rig.front < 0.35:
            seam = canvas.capsule(
                rig.project(lean * 0.7 - chest * 0.85, 0.0, rig.shoulder_h - 2.0),
                rig.project(-trail * 0.4 - chest * 0.85, 0.0, hem_h + 1.0),
                rig.s(1.1),
            ) & (body | skirt)
            canvas.paint(seam, dk)

        # One strap across the chest. One accessory, not five.
        if rig.front > -0.4:
            strap = canvas.capsule(
                rig.project(lean * 0.6 + chest * 0.6, -build.shoulder * 0.55, rig.shoulder_h - 2.0),
                rig.project(lean * 0.3 + chest * 0.7, build.shoulder * 0.55, rig.hip_h + 9.0),
                rig.s(1.4),
            ) & body
            canvas.paint_shaded(strap, "leather_dk", "leather_md", "leather_lt")

    return draw


def _coat_back_drawer(canvas: Canvas, rig: Rig, pose: Pose, dk: str):
    """The trailing half of the coat. This is what replaced the wings: the
    supernatural read is now motion in cloth, not anatomy."""

    def draw() -> None:
        build = rig.build
        trail = pose.coat_trail
        if trail < 1.0:
            return
        hb = build.hip + build.coat_flare
        hem_h = rig.hip_h - build.hem_drop - trail * 0.26
        top_l = rig.project(-build.chest * 0.7, -build.shoulder * 0.84, rig.shoulder_h - 3.0)
        top_r = rig.project(-build.chest * 0.7, build.shoulder * 0.84, rig.shoulder_h - 3.0)
        tip_l = rig.project(-trail - build.chest, -hb * 0.88, hem_h)
        tip_r = rig.project(-trail - build.chest, hb * 0.88, hem_h)
        mid = rig.project(-trail * 1.3 - build.chest, 0.0, hem_h + 2.5)
        cloth = canvas.polygon([top_l, top_r, tip_r, mid, tip_l])
        canvas.paint_shaded(cloth, "under_dk", dk, "coat_md")

    return draw


def _scarf_drawer(canvas: Canvas, rig: Rig, pose: Pose):
    def draw() -> None:
        dk, md, lt = rig.build.scarf_colours
        neck_h = rig.shoulder_h + 2.0
        collar = canvas.polygon(convex_hull(
            rig.ring(neck_h, rig.build.shoulder * 0.42, rig.build.chest * 0.46, pose.lean * 0.6)))
        collar |= canvas.polygon(convex_hull(
            rig.ring(neck_h - 3.0, rig.build.shoulder * 0.46, rig.build.chest * 0.50, pose.lean * 0.5)))
        canvas.paint_shaded(collar, dk, md, lt)

        trail = pose.scarf_trail
        a = rig.project(pose.lean * 0.2, -rig.shoulder_b * 0.30, neck_h - 1.0)
        bpt = rig.project(-trail * 0.55, -rig.shoulder_b * 0.55, neck_h - 2.0 + trail * 0.18)
        c = rig.project(-trail, -rig.shoulder_b * 0.30, neck_h - 5.0 + trail * 0.26)
        tail = canvas.capsule(a, bpt, rig.s(1.9)) | canvas.capsule(bpt, c, rig.s(1.5))
        canvas.paint_shaded(tail, dk, md, lt)

    return draw


def _head_drawer(canvas: Canvas, rig: Rig, pose: Pose):
    def draw() -> None:
        build = rig.build
        a = pose.lean * 0.35 + 1.0
        head_h = rig.head_h + pose.head_tilt
        cx, cy = rig.project(a, 0.0, head_h)
        rx = rig.s(rig.width(build.head_r, build.head_r * 0.90))
        ry = rig.s(build.head_r * 1.08)

        skull = canvas.ellipse(cx, cy, rx, ry)
        canvas.paint_shaded(skull, "skin_dk", "skin_md", "skin_hi")
        canvas.paint(canvas.ellipse(cx, cy + ry * 0.45, rx * 0.78, ry * 0.5) & skull, "skin_md")

        # Hair / cowl. Mass on the crown and the back, so the head is never a
        # bald ball and the away-facing directions still own a silhouette.
        back = rig.project(a - 2.6, 0.0, head_h + 1.6)
        if build.hooded:
            hood = canvas.ellipse(back[0], back[1] + 0.6, rx + 2.0, ry + 2.0)
            hood |= canvas.polygon([
                (back[0] - rx - 2.2, back[1] + 1.0),
                (back[0] + rx + 2.2, back[1] + 1.0),
                rig.project(a - 3.2, rig.shoulder_b * 0.62, rig.shoulder_h + 1.0),
                rig.project(a - 3.2, -rig.shoulder_b * 0.62, rig.shoulder_h + 1.0),
            ])
            face_gap = canvas.ellipse(cx + rig.fx * 1.8, cy + 1.6, rx * 0.92, ry * 0.82)
            canvas.paint_shaded(hood & ~face_gap, build.coat[0], build.coat[1], build.coat[2])
        else:
            hair = canvas.ellipse(back[0], back[1] - 1.2, rx + 1.1, ry * 0.94)
            hair &= ~canvas.ellipse(cx + rig.fx * 2.6, cy + 3.0, rx * 0.94, ry * 0.76)
            canvas.paint_shaded(hair, "hair_dk", "hair_md", "coat_md")

        if rig.front <= -0.35:
            return

        # Face. A thin brow line and two ember eyes. At this scale a mouth is
        # noise; the eyes carry the whole emotional read.
        brow = canvas.ellipse(cx + rig.fx * 1.4, cy - rig.s(1.2), rx * 0.72, rig.s(1.0)) & skull
        canvas.paint(brow, "skin_dk")

        spread = rig.s(rig.width(2.8, 0.0))
        base_x = cx + rig.fx * 2.4
        eye_y = cy + 0.8
        eyes = [base_x + spread, base_x - spread] if spread > 1.0 else [base_x + rig.fx * 1.0]
        for ex in eyes:
            canvas.paint(canvas.ellipse(ex, eye_y, rig.s(1.0), rig.s(0.9)) & skull, "flame_hi")

    return draw


def _weapon_drawer(canvas: Canvas, rig: Rig, pose: Pose, hand_a: float, hand_lift: float, b: float):
    """The scythe, placed in body space so it rotates with the character instead
    of being pasted on per direction.

    Owner note: the first pass read as a farm tool. The shape language is now
    deliberately imposing — a deep recurved blade with a back-spur, an iron
    collar carrying a bound Soul, and a counterweight spike at the butt — while
    the *detail* level stays exactly where the body is. Imposing comes from
    silhouette, not from ornament.
    """

    def draw() -> None:
        raise_h = pose.weapon_raise
        if pose.weapon == "carry":
            butt_a, butt_h, tip_a, tip_h = 8.0, 6.0 + raise_h, -8.0, 62.0 + raise_h
        else:
            butt_a, butt_h, tip_a, tip_h = 2.5, -2.0, -3.0, 56.0 + raise_h

        butt = rig.project(butt_a, b, butt_h)
        tip = rig.project(tip_a, b, tip_h)
        # A shallow S in the haft. A straight stick reads as a broom handle.
        bow_a = (butt_a + tip_a) * 0.5 + 2.2
        mid = rig.project(bow_a, b, (butt_h + tip_h) * 0.5)
        haft = canvas.capsule(butt, mid, rig.s(2.1)) | canvas.capsule(mid, tip, rig.s(2.0))
        canvas.paint_shaded(haft, "wood_dk", "wood_md", "leather_lt")

        # Counterweight spike at the butt: the asymmetry that makes it a weapon.
        spur = rig.project(butt_a + 1.0, b, butt_h - 7.0)
        canvas.paint_shaded(canvas.capsule(butt, spur, rig.s(1.6)), "iron_dk", "iron_md", "iron_hi")
        ferrule = rig.project(butt_a, b, butt_h + 4.0)
        canvas.paint_shaded(canvas.capsule(butt, ferrule, rig.s(2.6)), "iron_dk", "iron_md", "iron_hi")

        # Two chunky grip bands where the hands sit.
        for t in (0.42, 0.58):
            a = butt_a + (tip_a - butt_a) * t
            h = butt_h + (tip_h - butt_h) * t
            p0 = rig.project(a, b, h - 3.0)
            p1 = rig.project(a, b, h + 3.0)
            canvas.paint_shaded(canvas.capsule(p0, p1, rig.s(2.5)), "leather_dk", "leather_md", "leather_lt")

        _blade(canvas, rig, tip_a, b, tip_h)

    return draw


# Blade control points in body space, relative to the socket: (forward, up, radius).
# A long reach, a deep belly and a hooked point — read as a threat at a glance,
# and still only three values of iron.
_BLADE_ARC = [
    (0.0, 0.0, 3.8),
    (5.5, 6.2, 3.6),
    (14.5, 8.6, 2.9),
    (24.0, 5.6, 2.1),
    (31.5, -1.6, 1.4),
    (35.0, -9.5, 0.9),
]

# The back-spur: a short second point behind the socket.
_BLADE_SPUR = [(0.0, 0.0, 2.4), (-6.5, 4.8, 1.6), (-10.0, 10.0, 0.9)]


# The blade plane is turned out from the character's forward axis. Aligned with
# it, the blade pointed straight at or away from the camera in the north and
# south sheets and foreshortened to nothing; turned out, some of its length
# always crosses the screen, so the weapon reads in all eight directions.
BLADE_YAW = 0.78


def _blade(canvas: Canvas, rig: Rig, tip_a: float, b: float, tip_h: float) -> None:
    """One deep recurved reaping blade, a back-spur and an iron collar."""
    yaw_a = math.cos(BLADE_YAW)
    yaw_b = math.sin(BLADE_YAW)

    def sweep(arc, widen: float = 1.0) -> np.ndarray:
        mask = np.zeros((canvas.h, canvas.w), dtype=bool)
        previous = None
        for da, dh, radius in arc:
            point = rig.project(tip_a + da * yaw_a, b + da * yaw_b, tip_h + dh)
            if previous is not None:
                mask |= canvas.capsule(previous[0], point,
                                       max(rig.s((previous[1] + radius) * 0.5 * widen), 0.8))
            previous = (point, radius)
        return mask

    body = sweep(_BLADE_ARC)
    canvas.paint_shaded(body, "iron_dk", "iron_md", "iron_hi")
    # The cutting edge: a thinner pass along the same arc, one value up. This is
    # what makes a blade look sharp instead of look like a bar.
    canvas.paint(sweep(_BLADE_ARC, 0.42) & body, PALETTE["iron_hi"])
    canvas.paint_shaded(sweep(_BLADE_SPUR), "iron_dk", "iron_md", "iron_hi")

    socket = rig.project(tip_a, b, tip_h)
    collar = canvas.ellipse(socket[0], socket[1], rig.s(3.2), rig.s(3.2))
    canvas.paint_shaded(collar, "iron_dk", "iron_md", "iron_hi")
    # One bound Soul in the collar. The weapon's entire supernatural budget.
    canvas.paint(canvas.ellipse(socket[0], socket[1], rig.s(1.3), rig.s(1.3)), "flame")
    canvas.paint(canvas.ellipse(socket[0], socket[1], rig.s(0.7), rig.s(0.7)), "flame_hi")


def _soul_ember(canvas: Canvas, rig: Rig, pose: Pose) -> None:
    """The bound Soul. Small on purpose: the owner asked for less shine, and a
    2px ember at the sternum says more than a lantern."""
    if rig.front <= -0.55:
        return
    cx, cy = rig.project(pose.lean * 0.5 + 1.6, 0.0, rig.shoulder_h - 9.0)
    canvas.paint(canvas.ellipse(cx, cy, rig.s(2.2), rig.s(2.0)) & canvas.a, "flame")
    canvas.paint(canvas.ellipse(cx, cy, rig.s(1.1), rig.s(1.0)) & canvas.a, "soul_white")


def _hem_flame(canvas: Canvas, rig: Rig, pose: Pose, dk: str) -> None:
    """A thin line of Death Flame along the trailing hem. The entire supernatural
    budget for the silhouette, spent in one place instead of on wings."""
    trail = pose.coat_trail
    if trail < 3.5:
        return
    hb = rig.build.hip + rig.build.coat_flare
    hem_h = rig.hip_h - rig.build.hem_drop - trail * 0.26
    tip_l = rig.project(-trail - rig.build.chest, -hb * 0.88, hem_h)
    tip_r = rig.project(-trail - rig.build.chest, hb * 0.88, hem_h)
    mid = rig.project(-trail * 1.3 - rig.build.chest, 0.0, hem_h + 2.5)
    edge = canvas.ellipse(tip_l[0], tip_l[1], rig.s(1.2), rig.s(1.0)) | canvas.ellipse(tip_r[0], tip_r[1], rig.s(1.2), rig.s(1.0))
    edge |= canvas.ellipse(mid[0], mid[1], rig.s(1.0), rig.s(0.9))
    canvas.paint(edge & canvas.a, "flame")


# --------------------------------------------------------------------------
# Sheets
# --------------------------------------------------------------------------


def build_sheet(build: Build, facing: tuple[float, float], action: str) -> Image.Image:
    if action == "idle":
        count, poser, carry = IDLE_FRAMES, idle_pose, True
    elif action == "move":
        count, poser, carry = MOVE_FRAMES, run_pose, True
    else:
        count, poser, carry = ATTACK_FRAMES, attack_pose, False

    rows = (count + SHEET_COLUMNS - 1) // SHEET_COLUMNS
    sheet = Image.new("RGBA", (SHEET_COLUMNS * FRAME, rows * FRAME), (0, 0, 0, 0))
    for index in range(count):
        pose = poser(index / count)
        frame = draw_frame(build, facing, pose, carry_weapon=carry)
        sheet.paste(frame, ((index % SHEET_COLUMNS) * FRAME, (index // SHEET_COLUMNS) * FRAME))
    return sheet


def build_swing_weapon() -> Image.Image:
    """The scythe as a standalone sprite for the swing overlay.

    Built from the same _blade routine as the carried weapon through an
    east-facing rig, so the swung weapon and the carried weapon are literally
    the same object. Drawn haft-down with the butt at the bottom edge; the
    runtime rotates it about that butt.
    """

    class _Straight(Rig):
        def project(self, a: float, b: float, h: float) -> tuple[float, float]:
            return (96.0 + a * FIGURE, 176.0 - h * FIGURE)

    canvas = Canvas(192, 200)
    rig = _Straight(PROTAGONIST, (1.0, 0.0))
    butt = rig.project(0.0, 0.0, 0.0)
    tip = rig.project(0.0, 0.0, 108.0)
    mid = rig.project(2.6, 0.0, 54.0)
    canvas.paint_shaded(canvas.capsule(butt, mid, rig.s(2.6)) | canvas.capsule(mid, tip, rig.s(2.4)),
                        "wood_dk", "wood_md", "leather_lt")
    canvas.paint_shaded(canvas.capsule(butt, rig.project(1.2, 0.0, -8.0), rig.s(1.8)),
                        "iron_dk", "iron_md", "iron_hi")
    canvas.paint_shaded(canvas.capsule(butt, rig.project(0.0, 0.0, 5.0), rig.s(3.0)),
                        "iron_dk", "iron_md", "iron_hi")
    for h in (34.0, 56.0):
        canvas.paint_shaded(
            canvas.capsule(rig.project(0.0, 0.0, h - 4.0), rig.project(0.0, 0.0, h + 4.0), rig.s(3.0)),
            "leather_dk", "leather_md", "leather_lt")
    _blade(canvas, rig, 0.0, 0.0, 108.0)
    return canvas.to_image()


def build_cannon() -> Image.Image:
    """The Soul Cannon: a braced reliquary, not a rifle.

    Drawn along +x with the stock at the left, because SoulCannon.DrawWeapon
    rotates it from stock to muzzle. Weight comes from the flared mouth, the
    caged chamber and the under-brace — three big forms, not a hundred rivets.
    """
    canvas = Canvas(192, 80)
    y = 40.0

    # Under-brace: the mass that says this thing kicks.
    brace = canvas.polygon([(38.0, y + 4.0), (104.0, y + 3.0), (96.0, y + 15.0), (44.0, y + 14.0)])
    canvas.paint_shaded(brace, "wood_dk", "wood_md", "leather_lt")

    # Barrel.
    body = canvas.capsule((30.0, y), (132.0, y), 6.4)
    canvas.paint_shaded(body, "iron_dk", "iron_md", "iron_hi")

    # Shoulder stock with a hook.
    stock = canvas.polygon([(10.0, y - 5.0), (36.0, y - 8.0), (36.0, y + 8.0), (14.0, y + 12.0)])
    stock |= canvas.capsule((10.0, y - 4.0), (6.0, y + 6.0), 3.0)
    canvas.paint_shaded(stock, "leather_dk", "leather_md", "leather_lt")

    # Caged chamber: four ribs over a hollow where the Soul burns.
    cage = canvas.capsule((58.0, y), (88.0, y), 9.2)
    canvas.paint_shaded(cage, "iron_dk", "iron_md", "iron_hi")
    hollow = canvas.capsule((61.0, y), (85.0, y), 6.2)
    canvas.paint(hollow, "under_dk")
    for x in (62.0, 70.0, 78.0, 86.0):
        canvas.paint_shaded(canvas.capsule((x, y - 9.0), (x, y + 9.0), 1.8),
                            "iron_dk", "iron_md", "iron_hi")

    # Flared, fluted mouth.
    mouth = canvas.polygon([(126.0, y - 7.0), (168.0, y - 15.0), (168.0, y + 15.0), (126.0, y + 7.0)])
    mouth &= ~canvas.polygon([(132.0, y - 4.0), (172.0, y - 10.0), (172.0, y + 10.0), (132.0, y + 4.0)])
    canvas.paint_shaded(mouth, "iron_dk", "iron_md", "iron_hi")
    for dy in (-9.0, 0.0, 9.0):
        canvas.paint_shaded(canvas.capsule((140.0, y + dy * 0.72), (166.0, y + dy), 1.7),
                            "iron_dk", "iron_md", "iron_hi")

    # One bound Soul in the chamber. Nothing else emits.
    canvas.paint(canvas.ellipse(73.0, y, 3.4, 3.2) & canvas.a, "flame")
    canvas.paint(canvas.ellipse(73.0, y, 1.7, 1.6) & canvas.a, "flame_hi")
    return canvas.to_image()


# --------------------------------------------------------------------------
# Entry point
# --------------------------------------------------------------------------


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--builds", default="warden", help="comma separated: warden,warden_elder")
    parser.add_argument("--weapons", action="store_true")
    parser.add_argument("--contact-sheet", type=Path, default=None)
    args = parser.parse_args()

    catalogue = {"warden": PROTAGONIST, "warden_elder": ELDER}
    selected = [catalogue[name] for name in args.builds.split(",") if name in catalogue]

    for build in selected:
        for action in ("idle", "move", "attack"):
            folder = args.out / build.name / action
            folder.mkdir(parents=True, exist_ok=True)
            for direction, facing in DIRECTIONS.items():
                sheet = build_sheet(build, facing, action)
                sheet.save(folder / f"{direction}.png")
                print(f"wrote {folder / f'{direction}.png'} {sheet.size}")

    if args.weapons:
        folder = args.out / "weapons"
        folder.mkdir(parents=True, exist_ok=True)
        build_swing_weapon().save(folder / "scythe_physical_256.png")
        build_cannon().save(folder / "soul_cannon_256.png")
        print(f"wrote {folder}/scythe_physical_256.png and soul_cannon_256.png")

    if args.contact_sheet:
        _contact_sheet(selected[0], args.contact_sheet)
    return 0


def _contact_sheet(build: Build, path: Path) -> None:
    """One board with every direction of every action, for inspection."""
    order = ["n", "ne", "e", "se", "s", "sw", "w", "nw"]
    actions = [("idle", IDLE_FRAMES, idle_pose, True), ("move", MOVE_FRAMES, run_pose, True),
               ("attack", ATTACK_FRAMES, attack_pose, False)]
    cols = max(count for _, count, _, _ in actions)
    rows = sum(len(order) for _ in actions)
    board = Image.new("RGBA", (cols * FRAME, rows * FRAME), (26, 24, 32, 255))
    row = 0
    for _, count, poser, carry in actions:
        for direction in order:
            for index in range(count):
                frame = draw_frame(build, DIRECTIONS[direction], poser(index / count), carry_weapon=carry)
                board.alpha_composite(frame, (index * FRAME, row * FRAME))
            row += 1
    path.parent.mkdir(parents=True, exist_ok=True)
    board.save(path)
    print(f"wrote contact sheet {path} {board.size}")


if __name__ == "__main__":
    raise SystemExit(main())
