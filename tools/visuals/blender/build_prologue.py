"""The prologue sectors after the shore as 3D scenes, rendered with the game camera.

    blender -b -P tools/visuals/blender/build_prologue.py -- --sector search --out DIR [--samples 48]

Sectors (docs/current/regions/prologue.md, room grammar): `search` (II, the harbour quarter),
`causeway` (III, the broken railway causeway to the ferry landing), `deck` (the Warden skiff
during the crossing) and `threshold` (the Warden threshold). The world is 1800x1000 like the
shore; walkable areas come from PrologueDirector and stay calm and readable. Each run writes
plate.png (2700x1500), the separately rendered pieces as <piece>_full.png and pieces.json.

Night, cold light, wet materials; violet only at Warden fittings (their flames are drawn by
the game). Epochs: harbour and railway around 1900, objects mid-20th century; Warden things
hand-made of iron, stone and wood, never electric.
"""
from __future__ import annotations

import argparse
import json
import math
import random
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import env_kit as kit  # noqa: E402
from env_kit import ground, metres  # noqa: E402

WORLD = (1800, 1000)
WALK = (110, 125, 1690, 875)
TEXTURES = Path(__file__).resolve().parents[3] / "art" / "production" / "candidates" / "environment.prologue"
HUB_TEXTURES = TEXTURES.parent / "environment.hub"

WARDEN_LIGHT = (0.66, 0.50, 1.0)
NIGHT_KEY = (0.62, 0.70, 0.90)


def texture(name: str, folder: Path = TEXTURES) -> str:
    found = sorted(folder.glob(f"tex-{name}-*.png"))
    if not found:
        raise SystemExit(f"Textur {name} fehlt in {folder}")
    return str(found[-1])


def parse(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(prog="build_prologue.py")
    parser.add_argument("--sector", required=True, choices=["search", "causeway", "deck", "threshold", "sea", "passing"])
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--samples", type=int, default=48)
    return parser.parse_args(argv[argv.index("--") + 1:] if "--" in argv else [])


def materials() -> dict[str, bpy.types.Material]:
    concrete, wood, brick, granite, water = (texture("concrete"), texture("wood"), texture("brick"),
                                             texture("granite"), texture("water"))
    stone = texture("stone", HUB_TEXTURES)
    setts = texture("setts")
    return {
        "slab": kit.stone("slab", concrete, 7.0, tint=(0.50, 0.53, 0.58), spread=0.3, ash_amount=0.0),
        "kerb": kit.stone("kerb", granite, 3.0, tint=(0.42, 0.43, 0.47), spread=0.25, axes="XY"),
        "kerb_face": kit.textured("kerb_face", granite, 2.5, tint=(0.24, 0.25, 0.28), axes="XZ", bump=0.3),
        "plank": kit.stone("plank", wood, 3.5, tint=(0.62, 0.58, 0.56), spread=0.35),
        "plank_face": kit.stone("plank_face", wood, 3.5, tint=(0.5, 0.47, 0.46), spread=0.35, axes="XZ"),
        "brick": kit.stone("brick", brick, 3.0, tint=(0.33, 0.24, 0.23), spread=0.3, axes="XZ"),
        "basalt": kit.stone("basalt", stone, 3.0, tint=(0.62, 0.61, 0.68), spread=0.3, axes="XZ"),
        "basalt_floor": kit.stone("basalt_floor", stone, 4.0, tint=(0.55, 0.54, 0.6), spread=0.3),
        "ballast": kit.painted("ballast", (0.05, 0.05, 0.058), (0.13, 0.125, 0.135), scale=6.0, bump=0.6),
        "water": water_material(water),
        "iron": kit.painted("iron", (0.045, 0.045, 0.052), (0.11, 0.105, 0.115), scale=3.0, roughness=0.5, bump=0.4),
        "rust": kit.painted("rust", (0.06, 0.035, 0.03), (0.16, 0.085, 0.06), scale=4.0, roughness=0.8, bump=0.5),
        "rail": kit.painted("rail", (0.10, 0.10, 0.11), (0.24, 0.24, 0.26), scale=8.0, roughness=0.35, bump=0.1),
        "green": kit.painted("green", (0.05, 0.085, 0.07), (0.11, 0.16, 0.13), scale=3.0, roughness=0.6),
        "leather": kit.painted("leather", (0.08, 0.045, 0.035), (0.17, 0.10, 0.07), scale=5.0, roughness=0.55),
        "card": kit.painted("card", (0.20, 0.17, 0.13), (0.34, 0.30, 0.24), scale=5.0, roughness=0.9),
        "paper": kit.painted("paper", (0.55, 0.53, 0.48), (0.72, 0.70, 0.64), scale=8.0, roughness=0.9, bump=0.0),
        "rope": kit.painted("rope", (0.16, 0.14, 0.11), (0.28, 0.25, 0.2), scale=10.0, roughness=0.95),
        "dark": kit.painted("dark", (0.004, 0.004, 0.006), (0.012, 0.010, 0.016), bump=0.0),
        "ember": kit.emissive("ember", (0.55, 0.22, 1.0), 3.0),
        "setts": kit.textured("setts", setts, 2.2, tint=(0.70, 0.72, 0.78), roughness=0.55, bump=0.35, variation=0.3, wet=0.5),
        "hull": kit.painted("hull", (0.035, 0.035, 0.042), (0.09, 0.085, 0.095), scale=2.0, roughness=0.6, bump=0.5),
        "bone": kit.painted("bone", (0.32, 0.30, 0.26), (0.48, 0.46, 0.40), scale=4.0, roughness=0.6),
        "backing": kit.painted("backing", (0.012, 0.011, 0.015), (0.03, 0.028, 0.034), bump=0.0),
        "grout_dark": kit.painted("grout_dark", (0.02, 0.02, 0.024), (0.05, 0.05, 0.055), scale=2.0, roughness=1.0, bump=0.0),
        "slate": kit.painted("slate", (0.016, 0.018, 0.024), (0.045, 0.05, 0.06), scale=4.0, roughness=0.7, bump=0.3),
    }


def water_material(image: str) -> bpy.types.Material:
    """Dark, faintly glossy sea: the painted water texture over a still surface with ripples."""
    material = kit.textured("water", image, 9.0, tint=(0.55, 0.6, 0.7), roughness=0.18, bump=0.15, variation=0.25)
    return material


# ---- shared pieces --------------------------------------------------------------------------

def water(m, level: float = -0.9) -> bpy.types.Object:
    return kit.plane("sea", ground(-300, -300, level), ground(WORLD[0] + 300, WORLD[1] + 500, level), m["water"])


def quay(m, x0: float, x1: float, y0: float, y1: float, top: float = 0.0, seed: int = 1,
         kerb_south: bool = True, kerb_north: bool = False, slab=(2.0, 3.4), row: float = 1.5,
         slabs: bool = True) -> list[bpy.types.Object]:
    """A quay: concrete slabs on a block whose faces drop to the water, granite kerbs on its edges."""
    a, b = ground(x0, y0), ground(x1, y1)
    # The body sits just under the slabs; only its faces toward the water show.
    parts = [kit.box("quay_body", Vector(((a.x + b.x) / 2, (a.y + b.y) / 2, top - 1.75)), (b.x - a.x, a.y - b.y, 3.2), m["kerb_face"])]
    if slabs:
        parts += kit.paving("slab", a.x, b.x, b.y + (0.5 if kerb_south else 0), a.y - (0.5 if kerb_north else 0), m["slab"],
                            seed=seed, row=row, lengths=slab, gap=0.05, height=0.14, jitter=0.02)
    for slab_obj in parts[1:]:
        slab_obj.location.z += top
    if kerb_south:
        parts += kerb(m, a.x, b.x, b.y + 0.25, top, seed)
    if kerb_north:
        parts += kerb(m, a.x, b.x, a.y - 0.25, top, seed + 1)
    return parts


def kerb(m, x0: float, x1: float, y: float, top: float, seed: int) -> list[bpy.types.Object]:
    rng = random.Random(seed)
    stones, x = [], x0
    while x < x1:
        length = rng.uniform(1.0, 1.8)
        stones.append(kit.box(f"kerb_{seed}_{len(stones)}", Vector((x + length / 2, y, top - 0.12 + rng.uniform(-0.01, 0.01))),
                              (length - 0.03, 0.5, 0.36), m["kerb"], bevel=0.03))
        x += length
    return stones


def rails(m, y: float, x0: float, x1: float, top: float, gauge: float = 1.435) -> list[bpy.types.Object]:
    """A pair of rails set into the surface (east-west), with their groove."""
    parts = []
    for side in (-0.5, 0.5):
        yy = y + side * gauge
        parts.append(kit.box(f"rail_{y:.1f}_{side}", Vector(((x0 + x1) / 2, yy, top + 0.01)), (x1 - x0, 0.07, 0.05), m["rail"]))
        parts.append(kit.box(f"groove_{y:.1f}_{side}", Vector(((x0 + x1) / 2, yy - 0.06, top - 0.005)), (x1 - x0, 0.05, 0.02), m["dark"]))
    return parts


def bollard(m, at: Vector, name: str) -> list[bpy.types.Object]:
    body = kit.cylinder(f"{name}_body", at + Vector((0, 0, 0.35)), 0.16, 0.7, m["iron"], vertices=16)
    cap = kit.cylinder(f"{name}_cap", at + Vector((0, 0, 0.74)), 0.22, 0.1, m["iron"], vertices=16)
    return [body, cap]


def warden_marker(m, at: Vector, name: str) -> list[bpy.types.Object]:
    """A Warden way-mark: a forged iron post on a basalt foot with a cup for a calm flame."""
    parts = [kit.box(f"{name}_foot", at + Vector((0, 0, 0.12)), (0.5, 0.5, 0.24), m["basalt"], bevel=0.03),
             kit.box(f"{name}_post", at + Vector((0, 0, 0.75)), (0.1, 0.1, 1.1), m["iron"]),
             kit.cylinder(f"{name}_cup", at + Vector((0, 0, 1.33)), 0.13, 0.1, m["iron"], vertices=12)]
    for angle in range(0, 360, 90):
        offset = Vector((math.cos(math.radians(angle)) * 0.12, math.sin(math.radians(angle)) * 0.12, 1.47))
        parts.append(kit.box(f"{name}_prong_{angle}", at + offset, (0.025, 0.025, 0.22), m["iron"]))
    kit.point_light(f"{name}_light", at + Vector((0, -0.2, 1.7)), 90.0, WARDEN_LIGHT, radius=0.15)
    return parts


def suitcase(m, at: Vector, size=(0.62, 0.22, 0.44), yaw: float = 0.0, standing: bool = True, name: str = "case",
             material: str = "leather") -> list[bpy.types.Object]:
    w, d, h = size if standing else (size[0], size[2], size[1])
    body = kit.box(f"{name}_body", at + Vector((0, 0, h / 2)), (w, d, h), m[material], bevel=0.02, rotation=(0, 0, yaw))
    handle = kit.box(f"{name}_handle", at + Vector((0, 0, h + 0.03)), (0.14, 0.03, 0.05), m["iron"], rotation=(0, 0, yaw))
    tag = kit.box(f"{name}_tag", at + Vector((0.06, -d / 2 - 0.01, h * 0.75)), (0.06, 0.005, 0.09), m["paper"], rotation=(0, 0, yaw))
    return [body, handle, tag]


def common_lights(scene, key: float = 1.8, fill: float = 380.0, centre=(900, 500)) -> None:
    kit.key_light(scene, energy=key, colour=NIGHT_KEY, angle_deg=8.0)
    kit.area_light("fill", ground(centre[0], centre[1], 10.0), 16.0, fill, (0.70, 0.76, 0.92))


# ---- II Searchway ---------------------------------------------------------------------------

def setts_floor(m, x0: float, y0: float, x1: float, y1: float, z: float = 0.0) -> bpy.types.Object:
    return kit.plane("setts", ground(x0, y0, z), ground(x1, y1, z), m["setts"])


def track(m, y: float, x0: float, x1: float, seed: int) -> list[bpy.types.Object]:
    """Harbour rails set flush into the setts: worn steel heads over tarred sleepers."""
    rng = random.Random(seed)
    yy = ground(0, y).y
    parts = rails(m, yy, metres(x0), metres(x1), 0.0)
    x = metres(x0)
    while x < metres(x1):
        parts.append(kit.box(f"sleeper_{y}_{x:.1f}", Vector((x, yy, -0.012)), (0.24, 2.5, 0.04), m["plank"],
                             rotation=(0, 0, rng.uniform(-0.03, 0.03))))
        x += rng.uniform(0.62, 0.75)
    return parts


def drowned_roofs(m, seed: int, y_range=(10, 90), x_range=(0, 1800), count: int = 7) -> list[bpy.types.Object]:
    """Gables of the drowned harbour quarter breaking the water: slate roofs, a chimney or two."""
    rng = random.Random(seed)
    parts = []
    x = x_range[0] + rng.uniform(0, 120)
    while x < x_range[1] and len(parts) < count * 3:
        length = rng.uniform(3.0, 5.5)
        centre = ground(x, rng.uniform(*y_range), -1.9 + rng.uniform(-0.2, 0.2))
        centre.x += length / 2
        parts.append(kit.roof(f"roof_{len(parts)}", centre, length, rng.uniform(2.6, 3.6), rng.uniform(1.2, 1.7), m["slate"],
                              yaw=rng.uniform(-0.08, 0.08)))
        if rng.random() < 0.6:
            parts.append(kit.box(f"chimney_{len(parts)}", centre + Vector((rng.uniform(-1.2, 1.2), 0.3, 1.5)),
                                 (0.45, 0.45, 0.9), m["brick"], bevel=0.02))
        x += metres(length * 66.67) + rng.uniform(40, 160)
    return parts


def build_search(m, scene) -> dict[str, list]:
    """II, the searchway: a broad harbour quay through the drowned quarter. Wet granite setts
    with harbour rails, Warden planking over broken places, mooring along the kerb, luggage the
    passengers left, Warden way-marks and, in the south-west, the harbour signal mast carrying
    the Warden search fire. Roofs of the drowned warehouses break the water to the north."""
    pieces: dict[str, list] = {}
    water(m)
    quay(m, 40, 1760, 112, 925, seed=21, kerb_north=True, slabs=False)
    setts_floor(m, 40, 118, 1760, 914, 0.002)
    for index, y in enumerate((300, 790)):
        track(m, y, 40, 1760, seed=31 + index)
    drowned_roofs(m, 41)
    drowned_roofs(m, 43, y_range=(945, 990), count=4)
    rng = random.Random(5)
    # Warden planking bolted over places where the quay broke.
    for index, (x0, x1, y0, y1) in enumerate(((240, 430, 470, 610), (690, 930, 520, 660), (1180, 1300, 430, 560))):
        a, b = ground(x0, y0), ground(x1, y1)
        hole = kit.box(f"hole_{index}", Vector(((a.x + b.x) / 2, (a.y + b.y) / 2, -0.05)), (b.x - a.x - 0.6, a.y - b.y - 0.6, 0.12), m["dark"])
        x = a.x
        while x < b.x:
            width = rng.uniform(0.24, 0.32)
            kit.box(f"strip_{index}_plank_{x:.2f}", Vector((x + width / 2, (a.y + b.y) / 2, 0.04 + rng.uniform(-0.004, 0.01))),
                    (width - 0.03, a.y - b.y, 0.06), m["plank"], bevel=0.012, rotation=(0, 0, rng.uniform(-0.01, 0.01)))
            x += width
        for yy in (a.y - 0.3, b.y + 0.3):
            kit.box(f"strip_{index}_band_{yy:.1f}", Vector(((a.x + b.x) / 2, yy, 0.08)), (b.x - a.x + 0.1, 0.09, 0.02), m["iron"])
    # Loose and lifted setts around the patched places.
    for index, (x0, x1, y0, y1) in enumerate(((240, 430, 470, 610), (690, 930, 520, 660), (1180, 1300, 430, 560))):
        for k in range(14):
            side = rng.random()
            px = rng.uniform(x0 - 14, x1 + 14)
            py = y0 - rng.uniform(4, 16) if side < 0.5 else y1 + rng.uniform(4, 16)
            kit.box(f"loose_{index}_{k}", ground(px, py, 0.02 + rng.uniform(0, 0.03)), (0.24, 0.16, 0.1), m["kerb"], bevel=0.015,
                    rotation=(rng.uniform(-0.25, 0.25), rng.uniform(-0.25, 0.25), rng.uniform(-0.6, 0.6)))
    # Two timber jetties and a stair down into the water along the south kerb.
    for index, (x, length) in enumerate(((700, 2.6), (1350, 3.4))):
        a = ground(x, 925)
        for k in range(int(length / 0.28)):
            kit.box(f"jetty_{index}_{k}", Vector((a.x, a.y - 0.2 - k * 0.28, -0.12 + rng.uniform(-0.02, 0.01))), (2.2, 0.25, 0.08), m["plank"],
                    rotation=(rng.uniform(-0.02, 0.02), 0, rng.uniform(-0.02, 0.02)))
        for k in range(3):
            for side in (-1, 1):
                kit.cylinder(f"pile_{index}_{k}_{side}", Vector((a.x + side * 1.05, a.y - 0.4 - k * length / 3, -0.6)), 0.12, 1.3, m["plank_face"], vertices=10)
    stair = ground(1060, 925)
    for k in range(5):
        kit.box(f"stair_{k}", Vector((stair.x, stair.y - 0.25 - k * 0.35, -0.2 - k * 0.18)), (1.6, 0.35, 0.18), m["kerb"], bevel=0.02)
    for index, x in enumerate((420, 820, 1220, 1620)):
        bollard(m, ground(x, 902), f"bollard_{index}")
    # Coiled rope and a fallen crate on the quay edges (outside the walk area).
    for index, x in enumerate((180, 640, 1080, 1520)):
        for k in range(rng.randint(1, 2)):
            kit.box(f"crate_{index}_{k}", ground(x + k * 40 + rng.uniform(-6, 6), 104 - (k % 2) * 4, 0.3),
                    (0.6, 0.6, 0.6), m["plank_face"], bevel=0.02, rotation=(0, 0, rng.uniform(-0.2, 0.2)))
    for index, x in enumerate((560, 1380)):
        kit.cylinder(f"rope_{index}", ground(x, 896, 0.06), 0.28, 0.12, m["rope"], vertices=20)
    marks = []
    for index, (x, y) in enumerate(((548, 432), (1038, 652), (1416, 533))):
        marks.append(warden_marker(m, ground(x, y), f"mark_{index}"))
    pieces["marker"] = marks[0]
    for group in marks:
        kit.camera_only_hidden(group)
    luggage = []
    base = ground(835, 735)
    for index, (dx, dy, yaw, standing, material, size) in enumerate((
            (-0.5, 0.1, 0.3, True, "leather", (0.62, 0.22, 0.44)),
            (0.1, -0.1, -0.2, False, "card", (0.7, 0.2, 0.48)),
            (0.55, 0.25, 0.9, True, "card", (0.5, 0.18, 0.38)),
            (-0.05, 0.55, 0.1, False, "leather", (0.9, 0.5, 0.45)))):
        luggage += suitcase(m, base + Vector((dx, dy, 0)), size=size, yaw=yaw, standing=standing, name=f"case_{index}", material=material)
    pieces["luggage"] = luggage
    kit.camera_only_hidden(luggage)
    mast = signal_mast(m, ground(330, 968))
    pieces["mast"] = mast
    kit.camera_only_hidden(mast)
    common_lights(scene)
    return pieces


def signal_mast(m, foot: Vector) -> list[bpy.types.Object]:
    parts = [kit.box("mast_plinth", foot + Vector((0, 0, -0.2)), (1.6, 1.6, 1.4), m["kerb_face"], bevel=0.05)]
    height = 7.5
    for sx in (-1, 1):
        for sy in (-1, 1):
            leg = kit.box(f"mast_leg_{sx}_{sy}", foot + Vector((sx * 0.38, sy * 0.38, 0.5 + height / 2)), (0.09, 0.09, height), m["iron"],
                          rotation=(sy * -0.04, sx * 0.04, 0))
            parts.append(leg)
    for k in range(1, 8):
        z = 0.5 + k * height / 8
        shrink = 1 - k * 0.04
        for axis in ("x", "y"):
            size = (0.8 * shrink, 0.05, 0.05) if axis == "x" else (0.05, 0.8 * shrink, 0.05)
            for side in (-1, 1):
                offset = Vector((0, side * 0.38 * shrink, z)) if axis == "x" else Vector((side * 0.38 * shrink, 0, z))
                parts.append(kit.box(f"mast_brace_{k}_{axis}_{side}", foot + offset, size, m["iron"]))
    top = foot + Vector((0, 0, 0.5 + height))
    # Gallery: a grating of bars on a ring, a waist-high railing ring on posts.
    for k in range(-5, 6):
        parts.append(kit.box(f"mast_grate_{k}", top + Vector((k * 0.12, 0, 0)), (0.03, 1.3, 0.04), m["iron"]))
    bpy.ops.mesh.primitive_torus_add(major_radius=0.68, minor_radius=0.025, major_segments=32, minor_segments=6, location=top + Vector((0, 0, 0.0)))
    ring = bpy.context.active_object
    ring.data.materials.append(m["iron"])
    parts.append(ring)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.68, minor_radius=0.02, major_segments=32, minor_segments=6, location=top + Vector((0, 0, 0.62)))
    rail = bpy.context.active_object
    rail.data.materials.append(m["iron"])
    parts.append(rail)
    for angle in range(0, 360, 45):
        parts.append(kit.box(f"mast_post_{angle}", top + Vector((math.cos(math.radians(angle)) * 0.68, math.sin(math.radians(angle)) * 0.68, 0.31)),
                             (0.03, 0.03, 0.62), m["iron"]))
    # The fire basket: a cage of forged bars around a bed of cinders (the flame is the game's).
    parts.append(kit.cylinder("mast_basket_base", top + Vector((0, 0, 0.12)), 0.3, 0.08, m["iron"], vertices=16))
    for angle in range(0, 360, 30):
        parts.append(kit.box(f"mast_cage_{angle}", top + Vector((math.cos(math.radians(angle)) * 0.28, math.sin(math.radians(angle)) * 0.28, 0.4)),
                             (0.025, 0.025, 0.5), m["iron"]))
    parts.append(kit.cylinder("mast_coals", top + Vector((0, 0, 0.2)), 0.26, 0.06, m["dark"], vertices=16))
    kit.point_light("mast_light", top + Vector((0, -0.4, 1.4)), 4200.0, WARDEN_LIGHT, radius=0.4)
    return parts


# ---- III Causeway --------------------------------------------------------------------------

def skiff(m, bow: Vector, length: float = 11.0, beam: float = 4.2, deck: float = 0.0, cannons: bool = True,
          name: str = "skiff") -> dict[str, list]:
    """The Warden skiff, bow toward +x: a riveted iron hull with a timber deck, a low railing, an
    iron cage over its Death-Flame engine at the stern and two mounted Soul Cannon housings.
    Hand-made, no electrics (VISUAL-ART-DIRECTION E8). Returns its parts by group."""
    groups: dict[str, list] = {"hull": [], "rail_front": [], "rail_back": []}
    stern_x = bow.x - length
    centre = Vector(((bow.x + stern_x) / 2 - 0.4, bow.y, deck))
    hull = kit.box(f"{name}_hull", centre + Vector((0, 0, -0.9)), (length - 1.2, beam, 1.8), m["hull"], bevel=0.25)
    groups["hull"].append(hull)
    # Pointed bow: a wedge from the full beam to the stem.
    import bmesh
    mesh = bpy.data.meshes.new(f"{name}_bow")
    bm = bmesh.new()
    x0, x1 = bow.x - 1.8, bow.x + 0.2
    top, bottom = deck + 0.05, deck - 1.7
    verts = [bm.verts.new(v) for v in ((x0, bow.y - beam / 2, top), (x0, bow.y + beam / 2, top), (x1, bow.y, top + 0.25),
                                       (x0, bow.y - beam / 2, bottom), (x0, bow.y + beam / 2, bottom), (x1 - 0.6, bow.y, bottom))]
    for face in ((0, 2, 1), (3, 4, 5), (0, 3, 5, 2), (1, 2, 5, 4), (0, 1, 4, 3)):
        bm.faces.new([verts[i] for i in face])
    bm.to_mesh(mesh)
    bm.free()
    bow_obj = bpy.data.objects.new(f"{name}_bow", mesh)
    bpy.context.collection.objects.link(bow_obj)
    mesh.materials.append(m["hull"])
    groups["hull"].append(bow_obj)
    groups["hull"].append(kit.box(f"{name}_bowdeck", Vector((bow.x - 1.2, bow.y, deck + 0.04)), (1.6, beam * 0.55, 0.06), m["plank"]))
    groups["hull"].append(kit.box(f"{name}_wale", centre + Vector((0, 0, -0.05)), (length - 1.0, beam + 0.12, 0.12), m["iron"]))
    # Deck planks running fore and aft.
    rng = random.Random(7)
    y = bow.y - beam / 2 + 0.15
    while y < bow.y + beam / 2 - 0.15:
        width = rng.uniform(0.2, 0.26)
        groups["hull"].append(kit.box(f"{name}_deck_{y:.2f}", Vector((centre.x, y + width / 2, deck + 0.02)), (length - 1.6, width - 0.02, 0.05),
                                      m["plank"], bevel=0.008))
        y += width
    # Engine cage at the stern: forged bars around a basalt hearth (its flame is the game's).
    engine = Vector((stern_x + 1.6, bow.y, deck))
    groups["hull"].append(kit.cylinder(f"{name}_hearth", engine + Vector((0, 0, 0.3)), 0.9, 0.6, m["basalt_floor"], vertices=24))
    for angle in range(0, 360, 24):
        groups["hull"].append(kit.box(f"{name}_cagebar_{angle}", engine + Vector((math.cos(math.radians(angle)) * 0.95, math.sin(math.radians(angle)) * 0.95, 0.85)),
                                      (0.05, 0.05, 1.1), m["iron"]))
    bpy.ops.mesh.primitive_torus_add(major_radius=0.95, minor_radius=0.04, location=engine + Vector((0, 0, 1.4)))
    ring = bpy.context.active_object
    ring.data.materials.append(m["iron"])
    groups["hull"].append(ring)
    kit.point_light(f"{name}_engine_light", engine + Vector((0, -0.5, 1.6)), 500.0, WARDEN_LIGHT, radius=0.4)
    # Deck furniture: a hatch, coiled lines, lashed crates, the tiller at the stern and two Warden
    # lamps on short iron posts.
    groups["hull"].append(kit.box(f"{name}_hatch", Vector((centre.x + 0.6, bow.y + beam * 0.2, deck + 0.1)), (1.3, 1.0, 0.16), m["plank_face"], bevel=0.03))
    for k, (dx, dy) in enumerate(((-1.4, 0.32), (2.9, 0.3))):
        groups["hull"].append(kit.cylinder(f"{name}_coil_{k}", Vector((centre.x + dx, bow.y + beam * dy, deck + 0.06)), 0.32, 0.1, m["rope"], vertices=20))
    for k, dx in enumerate((-2.6, -2.0)):
        groups["hull"].append(kit.box(f"{name}_crate_{k}", Vector((centre.x + dx, bow.y + beam * 0.3, deck + 0.3)), (0.55, 0.55, 0.55), m["plank_face"], bevel=0.02,
                                      rotation=(0, 0, 0.1 * k)))
    groups["hull"].append(kit.box(f"{name}_tiller", Vector((stern_x + 0.2, bow.y, deck + 0.55)), (1.2, 0.08, 0.08), m["plank_face"], rotation=(0, -0.25, 0)))
    for k, dx in enumerate((-0.2, 0.25)):
        post = Vector((centre.x + dx * length, bow.y + beam * 0.38, deck))
        groups["hull"].append(kit.box(f"{name}_lamppost_{k}", post + Vector((0, 0, 0.7)), (0.07, 0.07, 1.4), m["iron"]))
        groups["hull"].append(kit.cylinder(f"{name}_lampcup_{k}", post + Vector((0, 0, 1.45)), 0.11, 0.1, m["iron"], vertices=10))
        kit.point_light(f"{name}_lamp_{k}", post + Vector((0, -0.2, 1.7)), 120.0, WARDEN_LIGHT, radius=0.15)
    if cannons:
        for side, offset in (("aft", -2.2), ("fore", 2.2)):
            base = Vector((centre.x + offset, bow.y - beam * 0.18, deck))
            groups["hull"].append(kit.cylinder(f"{name}_mount_{side}", base + Vector((0, 0, 0.25)), 0.5, 0.5, m["iron"], vertices=20))
            groups["hull"].append(kit.box(f"{name}_gun_{side}", base + Vector((0, -0.55, 0.65)), (0.42, 1.4, 0.42), m["iron"], bevel=0.04,
                                          rotation=(0.12, 0, 0)))
            groups["hull"].append(kit.box(f"{name}_gunband_{side}", base + Vector((0, -0.9, 0.7)), (0.5, 0.12, 0.5), m["bone"], bevel=0.02,
                                          rotation=(0.12, 0, 0)))
    # Low railing: posts and a top rail along both sides; the near (south) side is its own group.
    for side, group in ((-1, "rail_front"), (1, "rail_back")):
        yy = bow.y + side * (beam / 2 - 0.08)
        x = stern_x + 0.8
        while x < bow.x - 1.6:
            groups[group].append(kit.box(f"{name}_post_{side}_{x:.1f}", Vector((x, yy, deck + 0.45)), (0.06, 0.06, 0.9), m["iron"]))
            x += 1.1
        groups[group].append(kit.box(f"{name}_toprail_{side}", Vector(((stern_x + bow.x) / 2 - 0.4, yy, deck + 0.9)), (length - 2.4, 0.07, 0.07), m["iron"]))
    return groups


def build_causeway(m, scene) -> dict[str, list]:
    """III, the causeway: the railway dam out to the ferry landing, broken at its edges. Two
    tracks on ballast down the middle, paved shoulders, a fallen signal gantry, rubble, and at the
    eastern end the timber landing where the Warden skiff is moored."""
    pieces: dict[str, list] = {}
    water(m)
    rng = random.Random(13)
    # The embankment: a granite-faced body, paved shoulders, a ballast bed for the tracks.
    a, b = ground(40, 130), ground(1400, 880)
    kit.box("dam_body", Vector(((a.x + b.x) / 2, (a.y + b.y) / 2, -1.75)), (b.x - a.x, a.y - b.y, 3.2), m["kerb_face"])
    kit.plane("dam_shoulders", ground(40, 130, 0.002), ground(1400, 880, 0.002), m["setts"])
    bed_a, bed_b = ground(40, 430), ground(1400, 740)
    kit.box("ballast_bed", Vector(((bed_a.x + bed_b.x) / 2, (bed_a.y + bed_b.y) / 2, 0.06)), (bed_b.x - bed_a.x, bed_a.y - bed_b.y, 0.14),
            m["ballast"], bevel=0.05)
    for y in (510, 660):
        yy = ground(0, y).y
        x = metres(40)
        while x < metres(1360):
            kit.box(f"tie_{y}_{x:.1f}", Vector((x, yy, 0.14)), (0.24, 2.5, 0.08), m["plank"], rotation=(0, 0, rng.uniform(-0.03, 0.03)))
            x += rng.uniform(0.6, 0.72)
        rails(m, yy, metres(40), metres(1340), 0.19)
    # Buffer stops where the tracks end before the landing.
    for y in (510, 660):
        kit.box(f"buffer_{y}", ground(1350, y, 0.5), (0.4, 2.6, 0.8), m["rust"], bevel=0.04)
    # Broken edges: kerb only in pieces, chunks fallen toward the water.
    for edge_y, kerb_y in ((130, 128), (880, 882)):
        x = 40
        while x < 1400:
            length = rng.uniform(50, 110)
            if rng.random() < 0.72:
                kit.box(f"edge_{edge_y}_{x}", ground(x + length / 2, kerb_y, -0.12), (metres(length) - 0.05, 0.5, 0.36), m["kerb"], bevel=0.03,
                        rotation=(0, 0, rng.uniform(-0.03, 0.03)))
            else:
                for k in range(3):
                    kit.box(f"fallen_{edge_y}_{x}_{k}", ground(x + rng.uniform(0, length), kerb_y + (rng.uniform(15, 40) if edge_y > 500 else -rng.uniform(15, 40)),
                                                             -0.7 + rng.uniform(-0.2, 0.2)), (0.7, 0.5, 0.4), m["kerb"], bevel=0.04,
                            rotation=(rng.uniform(-0.6, 0.6), rng.uniform(-0.6, 0.6), rng.uniform(0, 3)))
            x += length
    # The fallen signal gantry on the north shoulder: a riveted lattice beam lying askew.
    gantry = ground(610, 280, 0.25)
    for k in range(2):
        kit.box(f"gantry_chord_{k}", gantry + Vector((0, k * 0.7, 0)), (9.0, 0.12, 0.18), m["rust"], rotation=(0, 0, 0.12))
    for k in range(13):
        kit.box(f"gantry_web_{k}", gantry + Vector((-4.2 + k * 0.7, 0.35, 0.02)), (0.08, 0.8, 0.08), m["rust"],
                rotation=(0, 0, 0.12 + (0.6 if k % 2 else -0.6)))
    kit.box("gantry_signal", gantry + Vector((4.6, 0.9, 0.2)), (0.5, 0.3, 0.9), m["iron"], bevel=0.03, rotation=(1.3, 0, 0.12))
    # Rubble near the south-east.
    for k in range(9):
        kit.box(f"rubble_{k}", ground(1285 + rng.uniform(-70, 70), 770 + rng.uniform(-25, 25), 0.15), (rng.uniform(0.3, 0.7), rng.uniform(0.3, 0.6), 0.3),
                m["kerb"], bevel=0.04, rotation=(rng.uniform(-0.4, 0.4), rng.uniform(-0.4, 0.4), rng.uniform(0, 3)))
    # The landing: a timber pier on piles, east of the dam.
    pier_a, pier_b = ground(1395, 300), ground(1780, 560)
    x = pier_a.x
    while x < pier_b.x:
        kit.box(f"pier_plank_{x:.2f}", Vector((x + 0.13, (pier_a.y + pier_b.y) / 2, -0.05 + rng.uniform(-0.01, 0.01))), (0.24, pier_a.y - pier_b.y, 0.08),
                m["plank"], bevel=0.008)
        x += 0.27
    for px in (1410, 1560, 1720):
        for py in (310, 550):
            kit.cylinder(f"pier_pile_{px}_{py}", ground(px, py, -0.6), 0.16, 1.4, m["plank_face"], vertices=10)
    # The moored skiff along the south side of the landing, bow east.
    boat = skiff(m, ground(1790, 642), length=8.6, beam=3.4, deck=-0.25, cannons=False, name="moored")
    for x in (1460, 1640):
        bollard(m, ground(x, 548), f"pier_bollard_{x}")
    # A gangplank from the pier onto the skiff's deck.
    kit.box("gangplank", ground(1560, 572, -0.1), (1.0, 1.1, 0.06), m["plank"], rotation=(0.1, 0, 0))
    marks = [warden_marker(m, ground(1395, 505), "dock_mark")]
    for group in marks:
        kit.camera_only_hidden(group)
    common_lights(scene)
    return pieces


# ---- Crossing (deck) ------------------------------------------------------------------------

def build_deck(m, scene) -> dict[str, list]:
    """The Warden skiff under way: the deck fills the middle of the frame (walkable deck
    475..1325 x 330..720), dark sea all round. The passing drowned town is the game's parallax;
    the near railing is a foreground piece so figures stand behind it."""
    pieces: dict[str, list] = {}
    sea = water(m, level=-1.4)
    sea.hide_render = True  # the game scrolls the sea under the skiff
    deck_y = (330 + 720) / 2
    boat = skiff(m, ground(1520, deck_y), length=metres(1520 - 380), beam=kit.depth(470), deck=0.0, name="skiff")
    pieces["rail"] = boat["rail_front"]
    kit.camera_only_hidden(boat["rail_front"])
    common_lights(scene, key=1.5, fill=450.0, centre=(900, 525))
    scene["transparent_plate"] = True
    return pieces


def build_sea(m, scene) -> dict[str, list]:
    """A 1200-unit wide stretch of sea that repeats seamlessly sideways (the water texture
    mirrors every 9 m), scrolled by the game under the skiff during the crossing."""
    water(m, level=-1.4)
    common_lights(scene, key=1.5, fill=450.0, centre=(600, 500))
    scene["plate_size"] = (1200, 1000)
    return {}


def build_passing(m, scene) -> dict[str, list]:
    """What the skiff passes: gables, chimneys and mast stumps of the drowned town standing in
    the sea, rendered alone on transparency in two bands (far above the deck, near below it) and
    laid out to wrap around 1800 units, so the game can scroll them at different speeds."""
    rng = random.Random(29)
    far, near = [], []
    for k in range(6):
        x = k * 200 + rng.uniform(-30, 30)
        base = ground(x, rng.uniform(150, 290), -2.2)
        roof = kit.roof(f"far_roof_{k}", base, rng.uniform(3.5, 6.0), rng.uniform(2.8, 3.8), rng.uniform(1.6, 2.4), m["slate"], yaw=rng.uniform(-0.1, 0.1))
        far.append(roof)
        if rng.random() < 0.7:
            far.append(kit.box(f"far_chimney_{k}", base + Vector((rng.uniform(-1.2, 1.2), 0.2, 2.2)), (0.45, 0.45, 1.2), m["brick"], bevel=0.02))
    for k in range(4):
        x = k * 300 + rng.uniform(-40, 40)
        base = ground(x, rng.uniform(850, 960), -1.4)
        height = rng.uniform(2.5, 4.5)
        near.append(kit.cylinder(f"near_mast_{k}", base + Vector((0, 0, height / 2)), 0.12, height, m["plank_face"], vertices=10,
                                 rotation=(rng.uniform(-0.25, 0.25), rng.uniform(-0.25, 0.25), 0)))
        if rng.random() < 0.6:
            near.append(kit.box(f"near_yard_{k}", base + Vector((0, 0, height * 0.8)), (1.8, 0.08, 0.08), m["plank_face"], rotation=(0, rng.uniform(-0.3, 0.3), 0)))
    # Wrap: copies one strip-width to the left so the right edge continues into the left.
    for group in (far, near):
        for obj in list(group):
            copy = obj.copy()
            copy.data = obj.data
            bpy.context.collection.objects.link(copy)
            copy.location.x -= metres(1200)
            group.append(copy)
    common_lights(scene, key=1.5, fill=450.0, centre=(900, 500))
    scene["transparent_plate"] = True
    scene["plate_size"] = (1200, 1000)
    scene["full_width_pieces"] = True
    return {"far": far, "near": near}


# ---- Threshold ------------------------------------------------------------------------------

def build_threshold(m, scene) -> dict[str, list]:
    """The Warden threshold: a broad approach of dressed basalt (walkable 110..1690 x 545..875)
    before the long wall of the Warden city; in the middle the gatehouse stands forward with a
    door made for shoulders and hands, a slit above it holding the Warden flame. The gatehouse is
    its own piece, so whoever walks through the door disappears into it."""
    pieces: dict[str, list] = {}
    rng = random.Random(17)
    a, b = ground(0, 520), ground(1800, 1000)
    kit.plane("approach_bed", ground(-100, 500, -0.02), ground(1900, 1100, -0.02), m["grout_dark"])
    kit.paving("approach", a.x - 1.0, b.x + 1.0, b.y - 1.0, a.y, m["basalt_floor"], seed=19, row=1.1, lengths=(1.2, 2.4), gap=0.05, height=0.12)
    # Three broad steps up to the gate in the middle.
    for k in range(3):
        y = 760 - k * 30
        kit.box(f"step_{k}", ground(900, y, 0.08 + k * 0.16), (metres(520 - k * 70), kit.depth(30), 0.16 + k * 0.16), m["basalt"], bevel=0.02)
    # The city wall: dressed basalt in courses with buttresses, foot at y = 540.
    wall_front = ground(0, 540).y
    kit.box("city_wall_core", Vector((metres(900), wall_front + 0.8, 7.0)), (metres(1800) + 4, 1.4, 14.0), m["backing"])
    kit.ashlar("city_wall", -2.0, metres(1800) + 2.0, 0.0, 14.0, wall_front, m["basalt"], seed=23, course=0.6, lengths=(1.0, 1.9), depth=0.14,
               openings=[(metres(760), metres(1040), 14.0)])
    for x in (120, 380, 620, 1180, 1420, 1680):
        kit.box(f"buttress_{x}", ground(x, 540, 6.0) + Vector((0, -0.45, 0)), (1.1, 0.9, 12.0), m["basalt"], bevel=0.05)
        kit.box(f"buttress_cap_{x}", ground(x, 540, 0.5) + Vector((0, -0.75, 0)), (1.4, 1.5, 1.0), m["basalt"], bevel=0.05)
    marks = [warden_marker(m, ground(754, 660), "gate_mark_l"), warden_marker(m, ground(1046, 660), "gate_mark_r")]
    for group in marks:
        kit.camera_only_hidden(group)
    # The passage behind the door: dark floor running north.
    kit.plane("passage", ground(840, 400, 0.01), ground(960, 700, 0.01), m["dark"])
    gate = gatehouse(m, ground(900, 690))
    pieces["gate"] = gate
    kit.camera_only_hidden(gate)
    for x in (300, 1500):
        kit.point_light(f"wall_glow_{x}", ground(x, 560, 3.0) + Vector((0, -1.0, 0)), 400.0, WARDEN_LIGHT, radius=0.5)
    common_lights(scene, key=2.2, fill=700.0, centre=(900, 650))
    return pieces


def gatehouse(m, foot: Vector) -> list[bpy.types.Object]:
    """The gatehouse standing forward of the wall: a massive basalt block with a human-sized
    doorway (open, dark) and above it a narrow vertical slit framed in iron for the Warden flame."""
    width, depth, height = metres(280), 4.6, 13.0
    centre = foot + Vector((0, depth / 2, height / 2))
    block = kit.box("gate_block", centre, (width, depth, height), m["basalt"], bevel=0.06)
    door_w, door_h = 1.15, 2.35
    cutter = kit.box("gate_door_cut", foot + Vector((0, depth / 2, door_h / 2 - 0.01)), (door_w, depth + 1.0, door_h + 0.02), m["dark"])
    modifier = block.modifiers.new("door", "BOOLEAN")
    modifier.object, modifier.operation = cutter, "DIFFERENCE"
    cutter.hide_render = cutter.hide_viewport = True
    slit = kit.box("gate_slit_cut", foot + Vector((0, 0.2, 5.6)), (0.32, 1.0, 2.0), m["dark"])
    modifier = block.modifiers.new("slit", "BOOLEAN")
    modifier.object, modifier.operation = slit, "DIFFERENCE"
    slit.hide_render = slit.hide_viewport = True
    parts = [block]
    for side in (-1, 1):
        parts.append(kit.box(f"gate_jamb_{side}", foot + Vector((side * (door_w / 2 + 0.12), -0.08, door_h / 2)), (0.24, 0.3, door_h), m["iron"]))
        parts.append(kit.box(f"gate_slitframe_{side}", foot + Vector((side * 0.2, -0.06, 5.6)), (0.08, 0.2, 2.1), m["iron"]))
    parts.append(kit.box("gate_lintel", foot + Vector((0, -0.1, door_h + 0.15)), (door_w + 0.7, 0.34, 0.3), m["iron"], bevel=0.02))
    parts.append(kit.box("gate_slit_sill", foot + Vector((0, -0.1, 4.55)), (0.6, 0.3, 0.12), m["iron"]))
    # The open ring of the Keeper: once, carved, unfilled, high on the gatehouse.
    bpy.ops.mesh.primitive_torus_add(major_radius=0.7, minor_radius=0.07, major_segments=48, minor_segments=8,
                                     location=foot + Vector((0, -0.05, 8.6)), rotation=(math.radians(90), 0, 0))
    ring = bpy.context.active_object
    ring.name = "keeper_ring"
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(ring.data)
    gap = [v for v in bm.verts if v.co.y > 0.52 and abs(v.co.x) < 0.26]
    bmesh.ops.delete(bm, geom=gap, context="VERTS")
    bm.to_mesh(ring.data)
    bm.free()
    ring.data.materials.append(m["iron"])
    parts.append(ring)
    kit.point_light("gate_slit_light", foot + Vector((0, -0.6, 5.6)), 260.0, WARDEN_LIGHT, radius=0.2)
    kit.point_light("gate_door_light", foot + Vector((0, 1.2, 1.2)), 120.0, WARDEN_LIGHT, radius=0.3)
    return parts


# ---- main -----------------------------------------------------------------------------------

BUILDERS = {"search": build_search, "causeway": build_causeway, "deck": build_deck, "threshold": build_threshold,
            "sea": build_sea, "passing": build_passing}


def pixel_box(objects, scene):
    from bpy_extras.object_utils import world_to_camera_view
    camera = scene.camera
    width, height = scene.render.resolution_x, scene.render.resolution_y
    xs, ys = [], []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        if obj.type != "MESH":
            continue
        evaluated = obj.evaluated_get(depsgraph)
        for corner in evaluated.bound_box:
            co = world_to_camera_view(scene, camera, evaluated.matrix_world @ Vector(corner))
            xs.append(co.x * width)
            ys.append((1 - co.y) * height)
    return (max(0, math.floor(min(xs)) - 4), max(0, math.floor(min(ys)) - 4),
            min(width, math.ceil(max(xs)) + 4), min(height, math.ceil(max(ys)) + 4))


def main() -> None:
    args = parse(sys.argv)
    out = args.out.resolve()
    scene = kit.reset()
    scene.eevee.taa_render_samples = args.samples
    kit.plate_camera(scene, *WORLD)
    m = materials()
    pieces = BUILDERS[args.sector](m, scene)
    if "plate_size" in scene:
        width, height = scene["plate_size"]
        bpy.data.objects.remove(scene.camera)
        kit.plate_camera(scene, width, height)
    kit.painterly(scene, size=4, sharpness=0.5)
    if args.sector != "passing":
        kit.render(scene, out / "plate.png", transparent=bool(scene.get("transparent_plate", False)))
    records = {}
    for name, objects in pieces.items():
        if objects and isinstance(objects[0], list):
            objects = objects[0]
        kit.camera_only_hidden(objects, hidden=False)
        restore = kit.isolate(objects)
        # A piece takes the key light; point lights right next to it would burn its iron white.
        dimmed = [(light, light.data.energy) for light in bpy.data.objects if light.type == "LIGHT" and light.data.type == "POINT"]
        for light, energy in dimmed:
            light.data.energy = 0.0
        kit.render(scene, out / f"{name}_full.png", transparent=True)
        for light, energy in dimmed:
            light.data.energy = energy
        restore()
        box = pixel_box(objects, scene)
        if scene.get("full_width_pieces"):
            box = (0, box[1], scene.render.resolution_x, box[3])
        records[name] = {"box": box}
    (out / "pieces.json").write_text(json.dumps(records, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(out / f"{args.sector}.blend"))
    print("BUILD_PROLOGUE_DONE " + json.dumps(records))


main()
