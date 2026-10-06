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
    parser.add_argument("--sector", required=True, choices=["search", "causeway", "deck", "threshold"])
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
        "basalt": kit.stone("basalt", stone, 3.0, tint=(0.46, 0.45, 0.5), spread=0.3, axes="XZ"),
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


# ---- main -----------------------------------------------------------------------------------

BUILDERS = {"search": build_search}


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
    kit.painterly(scene, size=4, sharpness=0.5)
    kit.render(scene, out / "plate.png")
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
        records[name] = {"box": pixel_box(objects, scene)}
    (out / "pieces.json").write_text(json.dumps(records, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(out / f"{args.sector}.blend"))
    print("BUILD_PROLOGUE_DONE " + json.dumps(records))


main()
