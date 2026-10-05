"""The Ashen Antechamber (hub) as a 3D scene, rendered as a painted plate plus separate pieces.

    blender -b -P tools/visuals/blender/build_hub.py -- --out art/production/candidates/environment.hub

Layout from SoulFurnaceAntechamber: world 1500x900, the north wall stands on y = 340 and
carries seven doors (I, II, III, final, IV, V, VI); door openings match HubDoor.Bounds exactly,
so the leaves the game slides open sit in them. A Warden place: dressed dark basalt, iron
bands, timber, fine ash; Warden flames are vertical and calm in hand-made iron fittings (the
flames themselves are animated by the game, the plate only carries their fittings and light);
the open ring of the Keeper appears once, carved and unfilled, above the final door.

Outputs in --out:
    plate.png                     floor and wall, 2250x1350 (1.5 px per world unit)
    leaf_biome.png, leaf_final.png   the pair of closed leaves of one door (game slides them)
    seal_biome.png, seal_final.png   iron bar and seal over a sealed door
    brazier.png                   Warden brazier, a high prop; foot point in pieces.json
    pieces.json                   pixel boxes and foot points of the pieces
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import env_kit as kit  # noqa: E402
from env_kit import ground, metres, rise  # noqa: E402

WORLD = (1500, 900)
WALL_FOOT = 340
DOORS = [("I", 190, False), ("II", 370, False), ("III", 550, False), ("", 750, True),
         ("IV", 950, False), ("V", 1130, False), ("VI", 1310, False)]
PILASTERS = [280, 460, 640, 860, 1040, 1220]
BRAZIERS = [(560, 610), (940, 610)]
MOVEMENT = (72, 385, 1428, 805)

BASALT = (0.060, 0.058, 0.068)
BASALT_LIGHT = (0.135, 0.128, 0.145)
MORTAR = (0.020, 0.019, 0.024)
ASH = (0.30, 0.29, 0.30)
IRON = (0.050, 0.050, 0.058)
TIMBER = (0.060, 0.042, 0.034)
DEATH_FLAME = (0.55, 0.22, 1.0)
#: Light cast by a calm Warden flame: violet, less saturated than the flame itself.
WARDEN_LIGHT = (0.66, 0.50, 1.0)


def parse(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(prog="build_hub.py")
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--samples", type=int, default=48)
    return parser.parse_args(argv[argv.index("--") + 1:] if "--" in argv else [])


def door_size(final: bool) -> tuple[float, float]:
    """Opening width and height in metres (HubDoor.Bounds: 120x200 or 220x290 screen units)."""
    return (metres(220), rise(290)) if final else (metres(120), rise(200))


TEXTURES = Path(__file__).resolve().parents[3] / "art" / "production" / "candidates" / "environment.hub"


def texture(name: str) -> str | None:
    found = sorted(TEXTURES.glob(f"tex-{name}-*.png"))
    return str(found[-1]) if found else None


def materials() -> dict[str, bpy.types.Material]:
    stone, ash, timber = texture("stone"), texture("ash"), texture("timber")
    return {
        "paving": kit.stone("paving", stone, 4.0, tint=(0.60, 0.58, 0.64), spread=0.3, ash_amount=0.12,
                            ash_tint=(0.21, 0.205, 0.215)),
        "ashlar": kit.stone("ashlar", stone, 3.0, tint=(0.40, 0.385, 0.43), spread=0.4, axes="XZ"),
        "stone": kit.stone("dressed", stone, 2.5, tint=(0.5, 0.48, 0.53), spread=0.2, axes="XZ"),
        "grout": kit.painted("grout", (0.030, 0.029, 0.033), (0.07, 0.068, 0.072), scale=2.0, roughness=1.0, bump=0.0),
        "backing": kit.painted("backing", (0.012, 0.011, 0.015), (0.03, 0.028, 0.034), bump=0.0),
        "dark": kit.painted("dark", (0.004, 0.004, 0.006), (0.012, 0.010, 0.016), bump=0.0),
        "iron": kit.painted("iron", IRON, (0.12, 0.12, 0.13), scale=3.0, roughness=0.55, bump=0.4),
        "timber": kit.textured("timber", timber, 2.4, tint=(0.42, 0.38, 0.40), axes="XZ", bump=0.4) if timber
        else kit.painted("timber", TIMBER, (0.12, 0.085, 0.062), scale=2.0, roughness=0.8),
        "ash": kit.painted("ash", (0.20, 0.195, 0.205), (0.36, 0.35, 0.36), scale=1.5, roughness=1.0, bump=0.15),
        "glow": kit.emissive("glow", DEATH_FLAME, 6.0),
        "cinder": kit.painted("cinder", (0.02, 0.018, 0.024), (0.07, 0.05, 0.09), scale=8.0, roughness=1.0),
    }


def build_floor(m) -> list[bpy.types.Object]:
    # Ash-filled joints under the paving, then the stones themselves in running rows.
    parts = [kit.plane("grout", ground(-100, WALL_FOOT - 5, -0.02), ground(WORLD[0] + 100, WORLD[1] + 220, -0.02), m["grout"])]
    a, b = ground(-60, WALL_FOOT + 2), ground(WORLD[0] + 60, 850)
    parts += kit.paving("paving", a.x, b.x, b.y, a.y, m["paving"], seed=11, row=0.95, lengths=(1.1, 2.3), gap=0.045)
    # A shallow step down along the south edge, into the dark below the room.
    for index, y in enumerate((850, 885, 920)):
        parts.append(kit.box(f"step_{index}", ground(WORLD[0] / 2, y + 17, -0.18 * (index + 1)),
                             (metres(WORLD[0] + 200), kit.depth(36), 0.18), m["stone"], bevel=0.02))
    # Ash drifted against the wall foot and in the corners.
    import random
    rng = random.Random(7)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=1.0)
    heap = bpy.context.active_object
    heap.data.materials.append(m["ash"])
    points = []
    for x in range(20, WORLD[0], 46):
        if any(abs(x - cx) < (125 if final else 75) for _, cx, final in DOORS):
            continue
        points.append(ground(x + rng.uniform(-14, 14), WALL_FOOT + rng.uniform(4, 16), -0.05))
    heaps = kit.scatter("ash", heap, points, seed=3, scale_range=(0.18, 0.42))
    for obj in heaps:
        obj.scale.z *= 0.35
    return parts + heaps


def build_wall(m) -> list[bpy.types.Object]:
    thickness, height = 1.0, 9.0
    wall_front = ground(0, WALL_FOOT).y
    # A dark core with the door openings cut out, faced with dressed basalt blocks.
    wall = kit.box("north_wall", Vector((metres(WORLD[0]) / 2, wall_front + 0.1 + thickness / 2, height / 2)),
                   (metres(WORLD[0]) + 4.0, thickness, height), m["backing"])
    parts = [wall]
    openings = []
    for _, cx, final in DOORS:
        width, door_height = door_size(final)
        jamb, lintel_h = (0.32, 0.62) if final else (0.24, 0.42)
        openings.append((metres(cx) - width / 2 - jamb, metres(cx) + width / 2 + jamb, door_height + lintel_h))
    parts += kit.ashlar("ashlar", -2.0, metres(WORLD[0]) + 2.0, 0.0, height, wall_front, m["ashlar"], seed=5,
                        openings=openings, depth=0.12)
    for name, cx, final in DOORS:
        width, door_height = door_size(final)
        x = metres(cx)
        cutter = kit.box(f"cut_{name or 'final'}", Vector((x, wall_front + 0.45, door_height / 2 - 0.01)),
                         (width, 1.0, door_height + 0.02), m["dark"])
        modifier = wall.modifiers.new(f"door_{name or 'final'}", "BOOLEAN")
        modifier.object = cutter
        modifier.operation = "DIFFERENCE"
        cutter.hide_render = True
        cutter.hide_viewport = True
        # Black depth behind the leaves, seen only when a door opens.
        parts.append(kit.box(f"void_{name or 'final'}", Vector((x, wall_front + 0.95, door_height / 2)),
                             (width + 0.1, 0.1, door_height + 0.1), m["dark"]))
        jamb = 0.32 if final else 0.24
        for side in (-1, 1):
            parts.append(kit.box(f"jamb_{name}_{side}", Vector((x + side * (width / 2 + jamb / 2), wall_front - 0.12, door_height / 2)),
                                 (jamb, 0.32, door_height), m["stone"], bevel=0.03))
        lintel_h = 0.62 if final else 0.42
        parts.append(kit.box(f"lintel_{name}", Vector((x, wall_front - 0.14, door_height + lintel_h / 2)),
                             (width + 2 * jamb + 0.2, 0.36, lintel_h), m["stone"], bevel=0.03))
        # Iron frame inside the jambs.
        for side in (-1, 1):
            parts.append(kit.box(f"frame_{name}_{side}", Vector((x + side * (width / 2 - 0.03), wall_front - 0.02, door_height / 2)),
                                 (0.07, 0.1, door_height), m["iron"]))
        parts.append(kit.box(f"frame_top_{name}", Vector((x, wall_front - 0.02, door_height - 0.03)), (width, 0.1, 0.07), m["iron"]))
        # Threshold stone, worn.
        parts.append(kit.box(f"sill_{name}", Vector((x, wall_front - 0.2, 0.04)), (width + 0.3, 0.5, 0.08), m["stone"], bevel=0.03))
        if final:
            parts += keeper_ring(m, Vector((x, wall_front - 0.33, door_height + lintel_h + 1.05)))
        else:
            parts += numeral_plaque(m, name, Vector((x, wall_front - 0.33, door_height + lintel_h + 0.42)))
    for px in PILASTERS:
        x = metres(px)
        parts.append(kit.box(f"pilaster_{px}", Vector((x, wall_front - 0.25, height / 2)), (0.62, 0.5, height), m["stone"], bevel=0.04))
        parts.append(kit.box(f"plinth_{px}", Vector((x, wall_front - 0.3, 0.3)), (0.8, 0.62, 0.6), m["stone"], bevel=0.04))
        for z in (1.6, 3.4, 5.2):
            parts.append(kit.box(f"band_{px}_{z}", Vector((x, wall_front - 0.25, z)), (0.66, 0.54, 0.08), m["iron"]))
        parts += sconce(m, Vector((x, wall_front - 0.62, 2.55)))
    # Cornice and a crane rail high up: the same family as the furnace hall beyond the doors.
    parts.append(kit.box("cornice", Vector((metres(WORLD[0]) / 2, wall_front - 0.3, 6.6)), (metres(WORLD[0]) + 4, 0.6, 0.35), m["stone"], bevel=0.03))
    parts.append(kit.box("rail", Vector((metres(WORLD[0]) / 2, wall_front - 0.7, 7.3)), (metres(WORLD[0]) + 4, 0.25, 0.3), m["iron"]))
    for index, x in enumerate((2.2, 7.9, 13.4, 19.6)):
        parts += chain(m, Vector((x, wall_front - 0.75, 7.15)), 1.6 + 0.6 * (index % 2), seed=index)
    return parts


def numeral_plaque(m, numeral: str, at: Vector) -> list[bpy.types.Object]:
    plaque = kit.box(f"plaque_{numeral}", at, (0.72, 0.08, 0.46), m["iron"], bevel=0.02)
    bpy.ops.object.text_add(location=(at.x, at.y - 0.05, at.z - 0.15), rotation=(math.radians(90), 0, 0))
    text = bpy.context.active_object
    text.name = f"numeral_{numeral}"
    text.data.body = numeral
    text.data.align_x = "CENTER"
    text.data.size = 0.36
    text.data.extrude = 0.012
    text.data.materials.append(m["ash"])
    return [plaque, text]


def keeper_ring(m, at: Vector) -> list[bpy.types.Object]:
    """The open ring: carved, a gap at the top, never filled or lit (VISUAL-ART-DIRECTION S12-S14)."""
    bpy.ops.mesh.primitive_torus_add(major_radius=0.62, minor_radius=0.07, major_segments=48, minor_segments=10,
                                     location=at, rotation=(math.radians(90), 0, 0))
    ring = bpy.context.active_object
    ring.name = "keeper_ring"
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="DESELECT")
    bpy.ops.object.mode_set(mode="OBJECT")
    for vertex in ring.data.vertices:
        world = ring.matrix_world @ vertex.co
        if world.z > at.z + 0.48 and abs(world.x - at.x) < 0.24:
            vertex.select = True
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.delete(type="VERT")
    bpy.ops.object.mode_set(mode="OBJECT")
    ring.data.materials.append(m["iron"])
    disc = kit.cylinder("ring_stone", at + Vector((0, 0.06, 0)), 0.86, 0.1, m["stone"], vertices=48, rotation=(math.radians(90), 0, 0))
    return [ring, disc]


def sconce(m, at: Vector) -> list[bpy.types.Object]:
    """Hand-forged iron fitting for a Warden flame (the flame itself is drawn by the game)."""
    parts = [kit.box("sconce_arm", at + Vector((0, 0.18, -0.12)), (0.08, 0.36, 0.06), m["iron"]),
             kit.cylinder("sconce_cup", at, 0.13, 0.12, m["iron"], vertices=12)]
    for angle in range(0, 360, 90):
        offset = Vector((math.cos(math.radians(angle)) * 0.12, math.sin(math.radians(angle)) * 0.12, 0.16))
        parts.append(kit.box(f"sconce_prong_{angle}", at + offset, (0.025, 0.025, 0.26), m["iron"]))
    kit.point_light("sconce_light", at + Vector((0, -0.25, 0.35)), 22.0, WARDEN_LIGHT, radius=0.15)
    return parts


def chain(m, top: Vector, length: float, seed: int = 0) -> list[bpy.types.Object]:
    links = []
    count = int(length / 0.11)
    for index in range(count):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.05, minor_radius=0.014, major_segments=10, minor_segments=5,
                                         location=top - Vector((0, 0, index * 0.105)),
                                         rotation=(0, math.radians(90 if index % 2 else 0), math.radians(90)))
        link = bpy.context.active_object
        link.name = f"chain_{seed}_{index}"
        link.data.materials.append(m["iron"])
        links.append(link)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.14, minor_radius=0.03, location=top - Vector((0, 0, count * 0.105 + 0.12)),
                                     rotation=(math.radians(90), 0, 0))
    hook = bpy.context.active_object
    hook.data.materials.append(m["iron"])
    links.append(hook)
    return links


def build_brazier(m, foot: Vector) -> list[bpy.types.Object]:
    """Warden brazier: a basalt bowl on a forged iron tripod (flame by the game)."""
    parts = []
    for angle in (90, 210, 330):
        direction = Vector((math.cos(math.radians(angle)), math.sin(math.radians(angle)), 0))
        leg = kit.box(f"leg_{angle}", foot + direction * 0.22 + Vector((0, 0, 0.55)), (0.055, 0.055, 1.15), m["iron"],
                      rotation=(direction.y * 0.22, -direction.x * 0.22, 0))
        parts.append(leg)
    parts.append(kit.cylinder("ring", foot + Vector((0, 0, 0.62)), 0.26, 0.05, m["iron"], vertices=20))
    bowl = kit.cylinder("bowl", foot + Vector((0, 0, 1.18)), 0.36, 0.22, m["stone"], vertices=24)
    taper = bowl.modifiers.new("Taper", "SIMPLE_DEFORM")
    taper.deform_method = "TAPER"
    taper.factor = -0.6
    parts.append(bowl)
    parts.append(kit.cylinder("coals", foot + Vector((0, 0, 1.30)), 0.28, 0.04, m["cinder"], vertices=20))
    kit.point_light("brazier_light", foot + Vector((0, -0.1, 1.75)), 320.0, WARDEN_LIGHT, radius=0.25)
    return parts


def leaves(m, name: str, cx: float, final: bool) -> list[bpy.types.Object]:
    """A closed pair of timber leaves with iron bands and studs, in the opening."""
    width, height = door_size(final)
    wall_front = ground(0, WALL_FOOT).y
    x = metres(cx)
    parts = []
    for side in (-1, 1):
        leaf_x = x + side * width / 4
        parts.append(kit.box(f"leaf_{name}_{side}", Vector((leaf_x, wall_front + 0.12, height / 2)), (width / 2 - 0.02, 0.1, height), m["timber"], bevel=0.01))
        for z in [height * f for f in (0.14, 0.38, 0.62, 0.86)]:
            parts.append(kit.box(f"leafband_{name}_{side}_{z:.2f}", Vector((leaf_x, wall_front + 0.06, z)), (width / 2 - 0.08, 0.03, 0.09), m["iron"]))
            for stud in (-0.32, 0.0, 0.32):
                parts.append(kit.cylinder(f"stud_{name}_{side}_{z:.2f}_{stud}", Vector((leaf_x + stud * width / 4, wall_front + 0.04, z)),
                                          0.025, 0.04, m["iron"], vertices=8, rotation=(math.radians(90), 0, 0)))
        parts.append(kit.box(f"ringpull_{name}_{side}", Vector((x + side * 0.12, wall_front + 0.04, height * 0.47)), (0.05, 0.04, 0.16), m["iron"]))
    return parts


def seal(m, name: str, cx: float, final: bool) -> list[bpy.types.Object]:
    """Sealed: an iron bar across the leaves and a round seal plate over the seam."""
    width, height = door_size(final)
    wall_front = ground(0, WALL_FOOT).y
    x = metres(cx)
    z = height * 0.58
    parts = [kit.box(f"bar_{name}", Vector((x, wall_front - 0.05, z)), (width + 0.5, 0.1, 0.16 if final else 0.12), m["iron"], bevel=0.01)]
    radius = 0.5 if final else 0.3
    parts.append(kit.cylinder(f"sealplate_{name}", Vector((x, wall_front - 0.11, z)), radius, 0.06, m["iron"], vertices=32,
                              rotation=(math.radians(90), 0, 0)))
    parts.append(kit.cylinder(f"sealboss_{name}", Vector((x, wall_front - 0.15, z)), radius * 0.42, 0.06, m["stone"], vertices=24,
                              rotation=(math.radians(90), 0, 0)))
    return parts


def pixel_box(objects: list[bpy.types.Object], scene: bpy.types.Scene) -> tuple[int, int, int, int]:
    """Pixel bounds (left, top, right, bottom) of objects in the plate frame."""
    from bpy_extras.object_utils import world_to_camera_view
    camera = scene.camera
    width, height = scene.render.resolution_x, scene.render.resolution_y
    xs, ys = [], []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
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
    kit.key_light(scene, energy=0.4, colour=(0.78, 0.80, 0.95))
    # Overhead fill: the room is brightest where it is played and falls off toward its edges.
    kit.area_light("fill", ground(WORLD[0] / 2, 600, 9.0), 12.0, 1500.0, (0.80, 0.80, 0.92))
    m = materials()
    build_floor(m)
    architecture = build_wall(m)
    kit.no_shadows([obj for obj in architecture if obj.name.startswith(("north_wall", "ashlar", "pilaster", "plinth", "cornice", "rail", "band"))])
    braziers = [build_brazier(m, ground(x, y + 72)) for x, y in BRAZIERS]
    door_leaves = {name or "final": leaves(m, name or "final", cx, final) for name, cx, final in DOORS}
    seals = {name or "final": seal(m, name or "final", cx, final) for name, cx, final in DOORS}
    kit.painterly(scene, size=4, sharpness=0.5)

    pieces: dict[str, dict] = {}
    # The plate: everything fixed; braziers only cast their shadows; leaves and seals left out.
    for group in list(door_leaves.values()) + list(seals.values()):
        for obj in group:
            obj.hide_render = True
    for group in braziers:
        kit.camera_only_hidden(group)
    kit.render(scene, out / "plate.png")
    for group in braziers:
        kit.camera_only_hidden(group, hidden=False)

    def piece(name: str, objects: list[bpy.types.Object], foot: tuple[float, float] | None = None) -> None:
        for obj in objects:
            obj.hide_render = False
        restore = kit.isolate(objects)
        kit.render(scene, out / f"{name}_full.png", transparent=True)
        restore()
        box = pixel_box(objects, scene)
        record = {"box": box}
        if foot is not None:
            record["foot"] = [foot[0] * 1.5, foot[1] * 1.5]
        pieces[name] = record

    piece("leaf_biome", door_leaves["II"])
    piece("leaf_final", door_leaves["final"])
    piece("seal_biome", seals["II"])
    piece("seal_final", seals["final"])
    piece("brazier", braziers[0], foot=(BRAZIERS[0][0], BRAZIERS[0][1] + 72))
    (out / "pieces.json").write_text(json.dumps(pieces, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(out / "hub.blend"))
    print("BUILD_HUB_DONE " + json.dumps(pieces))


main()
