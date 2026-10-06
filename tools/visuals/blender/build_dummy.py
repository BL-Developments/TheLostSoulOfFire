"""Sandbox training dummy: a Warden practice post. A worn timber post in an iron foot, a crossbar
for arms, a body of bound straw with a stitched burlap hood, and the core marked on its chest in
violet chalk, where a Warden learns to strike.

    blender -b -P tools/visuals/blender/build_dummy.py -- --out art/production/candidates/prop.training-dummy

Rendered with the game camera (35°, 1.5 px per world unit) on transparency:
    dummy_full.png, pieces.json (box and foot point), dummy.blend
place_pieces.py then registers it as prop.training-dummy.
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
from env_kit import ground  # noqa: E402

FRAME = (120, 180)
FOOT = (60, 160)
TEXTURES = Path(__file__).resolve().parents[3] / "art" / "production" / "candidates" / "environment.hub"


def parse(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(prog="build_dummy.py")
    parser.add_argument("--out", type=Path, required=True)
    return parser.parse_args(argv[argv.index("--") + 1:] if "--" in argv else [])


def main() -> None:
    args = parse(sys.argv)
    out = args.out.resolve()
    scene = kit.reset()
    kit.plate_camera(scene, FRAME[0], FRAME[1])
    kit.key_light(scene, energy=3.2)
    kit.point_light("fill", ground(FOOT[0] - 60, FOOT[1] + 60, 1.8), 110.0, (0.6, 0.5, 0.95), radius=0.6)
    kit.point_light("rim", ground(FOOT[0] + 60, FOOT[1] - 60, 1.6), 70.0, (0.75, 0.55, 1.0), radius=0.4)
    timber = sorted(TEXTURES.glob("tex-timber-*.png"))
    wood = kit.textured("wood", str(timber[-1]), 0.8, tint=(0.62, 0.52, 0.46), axes="BOX", bump=0.5) if timber \
        else kit.painted("wood", (0.07, 0.05, 0.04), (0.16, 0.11, 0.08), scale=5.0)
    iron = kit.painted("iron", (0.04, 0.038, 0.046), (0.15, 0.145, 0.16), scale=6.0, roughness=0.45, bump=0.5)
    straw = kit.painted("straw", (0.40, 0.30, 0.13), (0.66, 0.53, 0.28), scale=14.0, roughness=0.95, bump=0.8)
    burlap = kit.painted("burlap", (0.22, 0.18, 0.13), (0.36, 0.30, 0.22), scale=22.0, roughness=0.95, bump=0.5)
    rope = kit.painted("rope", (0.14, 0.11, 0.08), (0.26, 0.21, 0.15), scale=18.0, roughness=0.9, bump=0.4)
    chalk = kit.emissive("chalk", (0.57, 0.28, 1.0), 1.4)
    base = ground(*FOOT)
    parts = [
        kit.box("foot_plate", base + Vector((0, 0, 0.03)), (0.6, 0.6, 0.06), iron, bevel=0.01),
        kit.box("foot_collar", base + Vector((0, 0, 0.16)), (0.2, 0.2, 0.22), iron, bevel=0.01),
        kit.box("post", base + Vector((0, 0, 0.9)), (0.12, 0.12, 1.8), wood, bevel=0.01),
        kit.box("crossbar", base + Vector((0, 0, 1.3)), (1.0, 0.09, 0.09), wood, bevel=0.01, rotation=(0, 0.04, 0)),
    ]
    for side in (-1, 1):
        parts.append(kit.box(f"brace_{side}", base + Vector((side * 0.13, 0, 0.2)), (0.04, 0.04, 0.34), iron,
                             rotation=(0, -side * 0.75, 0)))
    body = kit.cylinder("body", base + Vector((0, 0, 1.0)), 0.24, 0.72, straw, vertices=20)
    body.scale = (1.0, 0.8, 1.0)
    parts.append(body)
    for k, z in enumerate((0.72, 0.98, 1.24)):
        band = kit.cylinder(f"binding_{k}", base + Vector((0, 0, z)), 0.25, 0.04, rope, vertices=20)
        band.scale = (1.0, 0.8, 1.0)
        parts.append(band)
    head = kit.cylinder("hood", base + Vector((0, 0, 1.6)), 0.15, 0.3, burlap, vertices=16)
    parts.append(head)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.16, location=base + Vector((0, 0, 1.76)), segments=16, ring_count=8)
    top = bpy.context.active_object
    top.scale = (1.0, 1.0, 0.6)
    top.data.materials.append(burlap)
    parts.append(top)
    parts.append(kit.cylinder("neck_tie", base + Vector((0, 0, 1.46)), 0.12, 0.04, rope, vertices=16))
    # The chalked core on the chest, facing the camera (-y): a ring with a dot.
    bpy.ops.mesh.primitive_torus_add(major_radius=0.08, minor_radius=0.012, major_segments=32, minor_segments=6,
                                     location=base + Vector((0, -0.2, 1.08)), rotation=(math.radians(90), 0, 0))
    ring = bpy.context.active_object
    ring.data.materials.append(chalk)
    parts.append(ring)
    parts.append(kit.cylinder("core_dot", base + Vector((0, -0.2, 1.08)), 0.025, 0.01, chalk, vertices=12, rotation=(math.radians(90), 0, 0)))
    for k in range(5):
        tuft = kit.box(f"tuft_{k}", base + Vector((-0.2 + k * 0.1, -0.05, 0.62)), (0.03, 0.03, 0.12), straw,
                       rotation=(0.3, (k - 2) * 0.25, 0))
        parts.append(tuft)
    scene.render.film_transparent = True
    kit.painterly(scene, size=3, sharpness=0.6)
    kit.render(scene, out / "dummy_full.png", transparent=True)
    from bpy_extras.object_utils import world_to_camera_view
    width, height = scene.render.resolution_x, scene.render.resolution_y
    xs, ys = [], []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in parts:
        evaluated = obj.evaluated_get(depsgraph)
        for corner in evaluated.bound_box:
            co = world_to_camera_view(scene, scene.camera, evaluated.matrix_world @ Vector(corner))
            xs.append(co.x * width)
            ys.append((1 - co.y) * height)
    box = [max(0, math.floor(min(xs)) - 4), max(0, math.floor(min(ys)) - 4), min(width, math.ceil(max(xs)) + 4), min(height, math.ceil(max(ys)) + 4)]
    (out / "pieces.json").write_text(json.dumps({"dummy": {"box": box, "foot": [FOOT[0] * 1.5, FOOT[1] * 1.5]}}, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(out / "dummy.blend"))
    print("BUILD_DUMMY_DONE " + json.dumps(box))


main()
