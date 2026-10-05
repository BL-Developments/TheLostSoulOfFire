"""Build the Hollow for sprite rendering (3D path B: MPFB2 body, cloth and mask by script).

    blender -b -P tools/visuals/blender/build_hollow.py -- --out art/production/candidates/enemy.hollow/hollow.blend

Follows docs/current/characters/hollow.md: tall, thin, arms hanging to the knees, slightly
stooped with the head before the shoulders; one long torn anthracite garment hanging to the
floor in strips, a collar and button placket as the last trace of tailoring; a smooth
off-white oval mask with hairline cracks and nothing else. No saturated accent: the core in
the chest is drawn by the game under Soul Sense.

Actions: "idle" (sway, mask still), "move" (a stiff walk; the game adds the pauses) and
"swipe" (0.39 s pulling back with the mask tilting toward the target, then a wide grab).
"""
from __future__ import annotations

import argparse
import importlib
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from figure_kit import (  # noqa: E402
    FINGERS, MPFB, THUMB, Poser, add_outline, attach, cloth_shell, dominant_bone, enable_mpfb, flat, ground_feet,
    outline_material, rest_ground, shaped_coordinates, toon)

CLOTH = (0.075, 0.075, 0.086)
ASH = (0.11, 0.105, 0.11)
MASK = (0.62, 0.59, 0.52)
BUTTON = (0.20, 0.19, 0.17)
INK = (0.012, 0.009, 0.014)


def parse(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(prog="build_hollow.py")
    parser.add_argument("--out", type=Path, required=True)
    return parser.parse_args(argv[argv.index("--") + 1:] if "--" in argv else [])


# ---- Body and cloth -------------------------------------------------------------------------

def zone(bone: str) -> str:
    if bone.startswith(("hand_", "thumb", "index", "middle", "ring", "pinky")):
        return "hands"
    if bone in ("head", "neck_01"):
        return "head"
    if bone.startswith(("thigh_", "calf_", "foot_", "ball_")):
        return "legs"
    return "torso"


def paint_body(body: bpy.types.Object, rig: bpy.types.Object, materials: dict[str, bpy.types.Material]) -> list[str]:
    """Body and clothing are one worn-out surface; only the hands are a shade lighter (ash)."""
    body.data.materials.clear()
    body.data.materials.append(materials["cloth"])
    body.data.materials.append(materials["ash"])
    per_vertex = [zone(bone) for bone in dominant_bone(body, {bone.name for bone in rig.data.bones})]
    for polygon in body.data.polygons:
        hands = sum(per_vertex[index] == "hands" for index in polygon.vertices)
        polygon.material_index = 1 if hands * 2 > len(polygon.vertices) else 0
    return per_vertex


def robe(rig: bpy.types.Object, cloth: bpy.types.Material, waist: float) -> bpy.types.Object:
    """A long garment from the waist to the floor that frays into strips of uneven length, with
    a few gaps between the strips. Weighted by height to pelvis, thighs and calves."""
    left = 1.0 if rig.data.bones["thigh_l"].head_local.x > 0 else -1.0
    rng = random.Random(1709)
    rings, segments = 12, 30
    bottom = 0.03
    bm = bmesh.new()
    rows = []
    for ring in range(rings):
        t = ring / (rings - 1)
        z = waist - t * (waist - bottom)
        rx, ry = 0.17 + 0.10 * t, 0.13 + 0.08 * t
        rows.append([bm.verts.new((rx * math.cos(a), ry * math.sin(a) + 0.01, z))
                     for a in (s / segments * math.tau for s in range(segments))])
    reach = [rings - 1 - rng.choice((0, 0, 1, 2, 3, 4)) for _ in range(segments)]
    gaps = set(rng.sample(range(segments), 5))
    for ring in range(rings - 1):
        for segment in range(segments):
            if ring + 1 > reach[segment] or (segment in gaps and ring >= rings // 2):
                continue
            a, b = rows[ring][segment], rows[ring][(segment + 1) % segments]
            c, d = rows[ring + 1][(segment + 1) % segments], rows[ring + 1][segment]
            bm.faces.new((a, b, c, d))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    loose = [vertex for vertex in bm.verts if not vertex.link_faces]
    bmesh.ops.delete(bm, geom=loose, context="VERTS")
    mesh = bpy.data.meshes.new("robe")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("robe", mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(cloth)
    obj.modifiers.new("Thickness", "SOLIDIFY").thickness = 0.01

    knee = (rig.matrix_world @ rig.data.bones["calf_l"].head_local).z
    for name in ("pelvis", "thigh_l", "thigh_r", "calf_l", "calf_r"):
        obj.vertex_groups.new(name=name)
    for vertex in mesh.vertices:
        z = vertex.co.z
        side = "l" if vertex.co.x * left > 0 else "r"
        upper = max(0.0, min(1.0, (waist - z) / max(waist - knee, 1e-3)))
        lower = max(0.0, min(1.0, (knee - z) / max(knee - bottom, 1e-3)))
        obj.vertex_groups["pelvis"].add([vertex.index], 1.0 - upper * 0.8, "REPLACE")
        obj.vertex_groups[f"thigh_{side}"].add([vertex.index], upper * 0.8 * (1.0 - lower * 0.6), "REPLACE")
        obj.vertex_groups[f"calf_{side}"].add([vertex.index], upper * 0.8 * lower * 0.6, "REPLACE")
    armature = obj.modifiers.new("Armature", "ARMATURE")
    armature.object = rig
    obj.parent = rig
    return obj


def build_mask(body: bpy.types.Object, rig: bpy.types.Object, mask: bpy.types.Material, ink: bpy.types.Material) -> list[bpy.types.Object]:
    """A smooth, slightly oval half-shell over the face, placed from MPFB's eye helpers, with a
    few hairline cracks. No eyes, no mouth."""
    shaped = shaped_coordinates(body)
    points = []
    for side in ("l", "r"):
        group = body.vertex_groups.get(f"helper-{side}-eye")
        if group is not None:
            points += [body.matrix_world @ shaped[v.index] for v in body.data.vertices
                       if any(g.group == group.index and g.weight > 0.5 for g in v.groups)]
    eyes = sum(points, Vector()) / len(points)
    front = min(point.y for point in points)
    # Far enough forward that nose and lips stay behind the shell.
    centre = Vector((0.0, front + 0.045, eyes.z - 0.025))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=16, radius=0.1, location=centre)
    shell = bpy.context.active_object
    shell.name = "mask"
    bm = bmesh.new()
    bm.from_mesh(shell.data)
    for vertex in list(bm.verts):
        if vertex.co.y > -0.012:  # keep the half facing forward (-Y)
            bm.verts.remove(vertex)
    bm.to_mesh(shell.data)
    bm.free()
    shell.scale = (0.90, 0.95, 1.12)
    shell.data.materials.append(mask)
    shell.modifiers.new("Thickness", "SOLIDIFY").thickness = 0.006
    bpy.ops.object.shade_smooth()
    parts = [shell]
    rng = random.Random(4)
    for index in range(3):
        x = rng.uniform(-0.03, 0.03)
        z = rng.uniform(-0.04, 0.05)
        bpy.ops.mesh.primitive_cube_add(size=1, location=(centre.x + x, front - 0.049 + abs(x) * 0.45, centre.z + z))
        crack = bpy.context.active_object
        crack.name = f"mask_crack_{index}"
        crack.scale = (0.0018, 0.002, rng.uniform(0.03, 0.05))
        crack.rotation_euler = (0, rng.uniform(-0.6, 0.6), 0)
        crack.data.materials.append(ink)
        parts.append(crack)
    for part in parts:
        attach(part, rig, "head")
    return parts


def build_placket(rig: bpy.types.Object, cloth: bpy.types.Material, button: bpy.types.Material, waist: float) -> list[bpy.types.Object]:
    """The last trace of tailoring: a turned collar and a button placket down the chest."""
    neck = rig.matrix_world @ rig.data.bones["neck_01"].head_local
    parts = []
    bpy.ops.mesh.primitive_torus_add(major_radius=0.085, minor_radius=0.018, major_segments=20, minor_segments=6,
                                     location=(0, neck.y - 0.005, neck.z - 0.01))
    collar = bpy.context.active_object
    collar.name = "collar"
    collar.scale = (1.15, 1.0, 0.8)
    collar.data.materials.append(cloth)
    parts.append(collar)
    chest = rig.matrix_world @ rig.data.bones["spine_03"].head_local
    top, bottom = neck.z - 0.07, waist + 0.02
    count = 4
    for index in range(count):
        z = top - (top - bottom) * index / (count - 1)
        depth = chest.y - 0.135 - 0.02 * (1 - index / (count - 1))
        bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=6, radius=0.009, location=(0.0, depth, z))
        knob = bpy.context.active_object
        knob.name = f"button_{index}"
        knob.data.materials.append(button)
        parts.append(knob)
    for part in parts:
        attach(part, rig, "spine_03")
    return parts


# ---- Animation ------------------------------------------------------------------------------

def stoop(poser: Poser, lean: float = 0.0) -> None:
    """Slightly bent, head before the shoulders; the face stays level enough to show the mask."""
    poser.set("spine_01", "forward", 5 + lean * 0.4)
    poser.set("spine_02", "forward", 7 + lean * 0.3)
    poser.set("spine_03", "forward", 8 + lean * 0.3)
    poser.set("neck_01", "forward", 16)
    poser.set("head", "forward", -10)


def hang_arm(poser: Poser, side: str, swing: float = 0.0, out: float = 0.12) -> None:
    """Long arms hanging to the knees, forearm a little forward; `swing` moves the arm forward."""
    poser.aim(f"upperarm_{side}", poser.world(0.10 + swing, 1.0, out, side))
    poser.aim(f"lowerarm_{side}", poser.world(0.22 + swing * 1.3, 1.0, out * 0.5, side))
    poser.aim(f"hand_{side}", poser.world(0.18 + swing, 1.0, 0.0, side))


def claws(poser: Poser, curl: float) -> None:
    for side in ("l", "r"):
        for bone in FINGERS:
            poser.set(f"{bone}_{side}", "down", curl * (0.5 if bone.endswith("01") else 1.0))
        for bone in THUMB[1:]:
            poser.set(f"{bone}_{side}", "down", curl * 0.3)


def key_idle(poser: Poser, frames: int) -> bpy.types.Action:
    action = bpy.data.actions.new("idle")
    poser.rig.animation_data_create().action = action
    for frame in range(frames + 1):
        phase = frame / frames * math.tau
        poser.clear()
        stoop(poser)
        poser.set("spine_01", "left", 2.2 * math.sin(phase))
        poser.set("spine_03", "left", -1.2 * math.sin(phase))
        poser.set("neck_01", "left", -1.0 * math.sin(phase))  # the mask stays still while the body sways
        hang_arm(poser, "l", 0.04 * math.sin(phase - 0.7))
        hang_arm(poser, "r", -0.04 * math.sin(phase - 0.7))
        claws(poser, 30)
        poser.key(frame + 1)
    return action


def key_move(poser: Poser, frames: int) -> bpy.types.Action:
    """A stiff walk with little knee: legs swing from the hip, arms dangle a beat behind."""
    action = bpy.data.actions.new("move")
    poser.rig.animation_data.action = action
    ground = rest_ground(poser.rig)
    for frame in range(frames + 1):
        phase = frame / frames * math.tau
        poser.clear()
        stoop(poser, lean=4)
        poser.turn("spine_02", 4 * math.sin(phase))
        for side, offset in (("l", 0.0), ("r", math.pi)):
            leg_phase = phase + offset
            poser.set(f"thigh_{side}", "forward", 19 * math.sin(leg_phase) + 3)
            lift = max(0.0, math.cos(leg_phase + 0.3)) ** 2
            poser.set(f"calf_{side}", "back", 6 + 34 * lift)
        ground_feet(poser, ground)
        hang_arm(poser, "l", -0.16 * math.sin(phase - 0.9))
        hang_arm(poser, "r", 0.16 * math.sin(phase - 0.9))
        claws(poser, 30)
        poser.key(frame + 1)
    return action


def key_swipe(poser: Poser, frames: int, telegraph_frames: int) -> bpy.types.Action:
    """Pull back (the telegraph, the mask tilting toward the target), then one wide grab with
    the right arm sweeping in from the side; fingers open in the reach and close at the end."""
    action = bpy.data.actions.new("swipe")
    poser.rig.animation_data.action = action
    ground = rest_ground(poser.rig)
    for frame in range(frames):
        poser.clear()
        if frame < telegraph_frames:
            t = frame / max(1, telegraph_frames - 1)
            wind = 1.0 - (1.0 - t) ** 2
            stoop(poser, lean=-3 * wind)
            poser.set("head", "forward", 10 * wind)  # the mask leans toward the player
            poser.turn("spine_01", -8 * wind)
            poser.turn("spine_03", -14 * wind)
            # Right arm drawn back and out, elbow bent, hand open.
            poser.aim("upperarm_r", poser.world(-0.55 * wind, 1.0 - 0.45 * wind, 0.35 + 0.55 * wind, "r"))
            poser.aim("lowerarm_r", poser.world(-0.1 * wind + 0.2 * (1 - wind), 1.0 - 0.9 * wind, 0.6 * wind + 0.06, "r"))
            poser.aim("hand_r", poser.world(0.2, 1.0 - 0.9 * wind, 0.4 * wind, "r"))
            hang_arm(poser, "l", 0.08 * wind)
            claws(poser, 30 * (1 - wind) + 4 * wind)
        else:
            t = (frame - telegraph_frames + 1) / (frames - telegraph_frames)
            stoop(poser, lean=10 * t)
            poser.set("head", "forward", 10)
            poser.turn("spine_01", -8 + 20 * t)
            poser.turn("spine_03", -14 + 36 * t)
            # The grab sweeps from the right side across the front at shoulder height.
            across = -0.3 + 1.3 * t  # out (right) -> in front -> across
            poser.aim("upperarm_r", poser.world(0.5 + 0.6 * t, 0.35, 0.9 - across, "r"))
            poser.aim("lowerarm_r", poser.world(0.8 + 0.4 * t, 0.25, 0.6 - across * 1.2, "r"))
            poser.aim("hand_r", poser.world(1.0, 0.3, 0.3 - across, "r"))
            hang_arm(poser, "l", -0.1 * t)
            claws(poser, 4 + 60 * t ** 2)
            poser.set("thigh_l" if poser.left > 0 else "thigh_r", "forward", 14 * t)
        ground_feet(poser, ground)
        poser.key(frame + 1)
    return action


# ---- Main -----------------------------------------------------------------------------------

def main() -> None:
    args = parse(sys.argv)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    human_service, target_service = enable_mpfb()
    macro = target_service.get_default_macro_info_dict()
    macro.update({"gender": 0.5, "age": 0.6, "muscle": 0.2, "weight": 0.08, "proportions": 0.35, "height": 1.0})
    body = human_service.create_human(macro_detail_dict=macro)
    # Arms long enough to hang to the knees: MPFB's arm-length measure targets, before rigging.
    data = Path(importlib.import_module(MPFB).__file__).parent / "data" / "targets" / "arms"
    for name, weight in (("measure-upperarm-length-incr", 0.9), ("measure-lowerarm-length-incr", 1.0)):
        target_service.load_target(body, str(data / f"{name}.target.gz"), weight=weight, name=name)
    rig = human_service.add_builtin_rig(body, "game_engine")
    rig.name = "figure"

    materials = {"cloth": toon("cloth", CLOTH), "ash": toon("ash", ASH, brush=0.08),
                 "mask": toon("mask", MASK, brush=0.04), "button": toon("button", BUTTON, brush=0.04)}
    outline = outline_material()
    ink = flat("ink", INK)

    per_vertex = paint_body(body, rig, materials)
    shell = cloth_shell(body, per_vertex, {"torso"}, rig, materials["cloth"], "garment", push=0.014)
    pelvis_z = (rig.matrix_world @ rig.data.bones["pelvis"].head_local).z
    gown = robe(rig, materials["cloth"], waist=pelvis_z + 0.10)
    parts = build_mask(body, rig, materials["mask"], ink)
    parts += build_placket(rig, materials["cloth"], materials["button"], waist=pelvis_z + 0.10)
    for obj in (body, shell, gown, parts[0]):
        add_outline(obj, outline)

    toe = (rig.matrix_world @ rig.data.bones["ball_l"].tail_local) - (rig.matrix_world @ rig.data.bones["foot_l"].head_local)
    poser = Poser(rig, Vector((0, toe.y, 0)).normalized())
    poser.measure_rest([f"{bone}_{side}" for bone in FINGERS + THUMB for side in ("l", "r")], "down")
    actions = [key_idle(poser, 12), key_move(poser, 12), key_swipe(poser, 10, 7)]
    for action in actions:
        action.use_fake_user = True
    rig.animation_data.action = actions[0]
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, 12
    bpy.context.scene.render.fps = 12

    args.out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(args.out.resolve()))
    print(f"BUILD_HOLLOW_DONE {args.out} height={rig.dimensions.z:.2f}")


main()
