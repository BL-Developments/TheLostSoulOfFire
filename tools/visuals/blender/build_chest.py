"""Arena chest: a Warden reliquary of dark timber with iron straps, brass corners, a curved lid and
a soul-glass lock; the lid swings open on its back hinge while gold light wells up inside.

    blender -b -P tools/visuals/blender/build_chest.py -- --out art/production/candidates/prop.arena-chest

Rendered with the game camera (35°, 1.5 px per world unit) on transparency, one image per frame:
    open/0001.png … open/NNNN.png   frame 1 closed, the last fully open
    chest.json                      frame count and the foot point (floor centre) in pixels
pack_prop_clip.py turns the frames into the sheet the game samples by ArenaChest.OpenProgress.
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

FRAME = (140, 130)  # world units framed by the camera
FOOT = (70, 92)  # where the chest stands in that frame (floor centre)
FRAMES = 10
WIDTH, DEPTH, HEIGHT = 0.80, 0.50, 0.34  # body, metres
LID = 0.13  # height of the curved lid above the body

TEXTURES = Path(__file__).resolve().parents[3] / "art" / "production" / "candidates" / "environment.hub"


def parse(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(prog="build_chest.py")
    parser.add_argument("--out", type=Path, required=True)
    return parser.parse_args(argv[argv.index("--") + 1:] if "--" in argv else [])


def materials() -> dict[str, bpy.types.Material]:
    timber = sorted(TEXTURES.glob("tex-timber-*.png"))
    return {
        "timber": kit.textured("timber", str(timber[-1]), 0.9, tint=(0.62, 0.50, 0.46), axes="BOX", bump=0.5)
        if timber else kit.painted("timber", (0.05, 0.035, 0.03), (0.11, 0.08, 0.06), scale=4.0),
        "iron": kit.painted("iron", (0.04, 0.038, 0.046), (0.16, 0.155, 0.17), scale=6.0, roughness=0.45, bump=0.5),
        "brass": kit.painted("brass", (0.42, 0.29, 0.10), (0.85, 0.64, 0.30), scale=8.0, roughness=0.3, bump=0.2),
        "lining": kit.painted("lining", (0.05, 0.035, 0.025), (0.11, 0.075, 0.05), scale=6.0, roughness=0.9, bump=0.1),
        "cloth": kit.painted("cloth", (0.06, 0.025, 0.04), (0.12, 0.05, 0.08), scale=9.0, roughness=0.95, bump=0.1),
        "soul": kit.emissive("soul", (0.57, 0.28, 1.0), 2.2),
        "gold": kit.emissive("gold", (1.0, 0.80, 0.42), 0.0),
    }


def dome(name: str, x: float, radius: float, length: float, height: float, material) -> bpy.types.Object:
    """A half cylinder along x (a curved lid or its band), flattened to `height`, flat side down."""
    obj = kit.cylinder(name, Vector((x, 0, 0)), radius, length, material, vertices=32, rotation=(0, math.pi / 2, 0))
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    obj.scale = (1.0, 1.0, height / radius)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.bisect(plane_co=(0, 0, 0), plane_no=(0, 0, 1), clear_inner=True, use_fill=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    return obj


def build(m) -> tuple[list[bpy.types.Object], bpy.types.Object, bpy.types.Material]:
    base = ground(*FOOT)
    parts: list[bpy.types.Object] = []
    # Body: a shell of planks (open top), lined inside.
    wall = 0.035
    for name, centre, size in (
        ("front", (0, -DEPTH / 2 + wall / 2, HEIGHT / 2), (WIDTH, wall, HEIGHT)),
        ("back", (0, DEPTH / 2 - wall / 2, HEIGHT / 2), (WIDTH, wall, HEIGHT)),
        ("left", (-WIDTH / 2 + wall / 2, 0, HEIGHT / 2), (wall, DEPTH, HEIGHT)),
        ("right", (WIDTH / 2 - wall / 2, 0, HEIGHT / 2), (wall, DEPTH, HEIGHT)),
        ("floor", (0, 0, 0.04), (WIDTH, DEPTH, 0.04)),
    ):
        parts.append(kit.box(f"body_{name}", base + Vector(centre), size, m["timber"], bevel=0.006))
    parts.append(kit.box("lining", base + Vector((0, 0, HEIGHT * 0.45)), (WIDTH - 2 * wall - 0.01, DEPTH - 2 * wall - 0.01, HEIGHT * 0.8), m["lining"]))
    # Iron straps around the body, brass corner caps, iron feet.
    for x in (-WIDTH * 0.28, WIDTH * 0.28):
        parts.append(kit.box(f"strap_{x:+.2f}", base + Vector((x, 0, HEIGHT / 2)), (0.05, DEPTH + 0.012, HEIGHT + 0.006), m["iron"], bevel=0.004))
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(kit.box(f"corner_{sx}{sy}", base + Vector((sx * (WIDTH / 2 - 0.02), sy * (DEPTH / 2 - 0.02), HEIGHT / 2)),
                                 (0.055, 0.055, HEIGHT + 0.01), m["brass"], bevel=0.008))
            parts.append(kit.box(f"foot_{sx}{sy}", base + Vector((sx * (WIDTH / 2 - 0.05), sy * (DEPTH / 2 - 0.05), 0.012)),
                                 (0.07, 0.07, 0.024), m["iron"], bevel=0.005))
    # Gold light inside, revealed as the lid rises, over a heap of Geld: coins in loose stacks.
    gold = kit.box("gold", base + Vector((0, 0, HEIGHT * 0.62)), (WIDTH - 0.12, DEPTH - 0.12, 0.02), m["gold"])
    parts.append(gold)
    import random
    rng = random.Random(23)
    for index in range(46):
        x = rng.uniform(-WIDTH / 2 + 0.09, WIDTH / 2 - 0.09)
        y = rng.uniform(-DEPTH / 2 + 0.08, DEPTH / 2 - 0.08)
        heap = 1.0 - (abs(x) / (WIDTH / 2)) ** 2 - (abs(y) / (DEPTH / 2)) ** 2
        z = HEIGHT * 0.66 + 0.07 * max(0.0, heap) + rng.uniform(0.0, 0.015)
        parts.append(kit.cylinder(f"coin_{index}", base + Vector((x, y, z)), 0.028, 0.008, m["brass"], vertices=12,
                                  rotation=(rng.uniform(-0.5, 0.5), rng.uniform(-0.5, 0.5), 0.0)))

    # Lid: a flattened half cylinder along x, hinged on the back top edge. The camera looks
    # along +y from the south, so "front" (toward the viewer) is -y in Blender.
    hinge = bpy.data.objects.new("lid_hinge", None)
    bpy.context.collection.objects.link(hinge)
    hinge.location = base + Vector((0, DEPTH / 2, HEIGHT))
    lid_parts = [dome("lid", 0.0, DEPTH / 2, WIDTH, LID, m["timber"])]
    for x in (-WIDTH * 0.28, WIDTH * 0.28):
        lid_parts.append(dome(f"lid_band_{x:+.2f}", x, DEPTH / 2 + 0.008, 0.05, LID + 0.008, m["iron"]))
    # The lid's underside is lined with dark red velvet: open, it stands behind the box and catches the
    # gold light, so the open lid reads against the dark body.
    lid_parts.append(kit.box("lid_lining", Vector((0.0, 0.0, -0.006)), (WIDTH - 0.06, DEPTH - 0.05, 0.01), m["cloth"]))
    for obj in lid_parts:
        obj.location = Vector((obj.location.x, -DEPTH / 2, 0))
    # Lock: a brass plate with a violet soul glass on the front of the lid.
    plate = kit.box("lock_plate", Vector((0, -DEPTH - 0.008, -0.02)), (0.14, 0.02, 0.12), m["brass"], bevel=0.01)
    glass = kit.cylinder("lock_soul", Vector((0, -DEPTH - 0.022, -0.02)), 0.028, 0.015, m["soul"], vertices=16, rotation=(math.pi / 2, 0, 0))
    lid_parts += [plate, glass]
    for obj in lid_parts:
        obj.parent = hinge
    parts += lid_parts
    light = kit.point_light("chest_glow", base + Vector((0, -0.06, HEIGHT * 0.95)), 0.0, (1.0, 0.8, 0.45), radius=0.2)
    return parts, hinge, light


def main() -> None:
    args = parse(sys.argv)
    out = args.out.resolve()
    scene = kit.reset()
    kit.plate_camera(scene, FRAME[0], FRAME[1])
    kit.key_light(scene, energy=3.4)
    kit.point_light("fill", ground(FOOT[0] - 60, FOOT[1] + 80, 1.6), 120.0, (0.6, 0.5, 0.95), radius=0.6)
    kit.point_light("rim", ground(FOOT[0] + 70, FOOT[1] - 60, 1.4), 80.0, (0.75, 0.55, 1.0), radius=0.4)
    m = materials()
    parts, hinge, light = build(m)
    gold = m["gold"].node_tree.nodes.get("Principled BSDF")
    scene.render.film_transparent = True
    for frame in range(FRAMES):
        t = frame / (FRAMES - 1)
        # Ease out with a small settle at the end, as a heavy lid falls back against its stop.
        swing = 1 - (1 - min(1.0, t / 0.8)) ** 2.4
        settle = math.sin(max(0.0, (t - 0.8) / 0.2) * math.pi) * 0.04
        hinge.rotation_euler = (-math.radians(108) * (swing - settle), 0.0, 0.0)
        gold.inputs["Emission Strength"].default_value = 7.0 * swing
        light.data.energy = 100.0 * swing
        kit.render(scene, out / "open" / f"{frame + 1:04d}.png", transparent=True)
    info = {"frames": FRAMES, "frame": FRAME, "foot_px": [FOOT[0] * 1.5, FOOT[1] * 1.5], "ppu": 1.5}
    (out / "chest.json").write_text(json.dumps(info, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(out / "chest.blend"))
    print("BUILD_CHEST_DONE " + json.dumps(info))


main()
