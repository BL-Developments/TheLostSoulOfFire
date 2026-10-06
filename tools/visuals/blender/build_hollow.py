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
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from figure_kit import (  # noqa: E402
    FINGERS, MPFB, THUMB, Poser, add_outline, attach, blend_placement, cloth_shell, dominant_bone, ease, enable_mpfb,
    flat, ground_feet, ground_points, outline_material, place_bone, placement_of, rest_ground, shaped_coordinates, toon)
from combat_kit import (  # noqa: E402
    Feet, Keys, add_leg_ik, finish_rekey, fit_pelvis, key_legs, measure_axes, move_pelvis, replace_action, smooth,
    step_arc, twist, window)

CLOTH = (0.075, 0.075, 0.086)
ASH = (0.11, 0.105, 0.11)
MASK = (0.62, 0.59, 0.52)
BUTTON = (0.20, 0.19, 0.17)
INK = (0.012, 0.009, 0.014)


def parse(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(prog="build_hollow.py")
    parser.add_argument("--rekey", help="comma-separated actions to key again on the opened .blend (no rebuild)")
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
        attach(part, rig, "mask")
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
    action = replace_action(poser.rig, "idle")
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
    action = replace_action(poser.rig, "move")
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


#: The swipe clip: the first frames follow the 0.42 s telegraph, the rest the 0.13 s grab
#: (Hollow.SwipeFrames / SwipeTelegraphFrames read the same split).
SWIPE_FRAMES = 20
SWIPE_TELEGRAPH_FRAMES = 13

#: Arm of the grab in stage directions (forward, down, out) per segment, from drawn back to across.
ARM_DRAWN = {"upperarm": (-0.55, 0.45, 0.95), "lowerarm": (-0.15, 0.05, 0.75), "hand": (0.25, 0.1, 0.55)}
ARM_REACH = {"upperarm": (1.0, 0.35, 0.35), "lowerarm": (1.0, 0.3, 0.05), "hand": (1.0, 0.35, -0.2)}
ARM_ACROSS = {"upperarm": (1.1, 0.4, -0.25), "lowerarm": (1.1, 0.55, -0.75), "hand": (0.9, 0.6, -0.85)}

SWIPE_KEYS = Keys([
    # Notice: the mask snaps to the target, the body straightens a little.
    (0.00, dict(lean=0, head=0, twist=0, crouch=0.0, draw=0.0, reach=0.0, guard=0.0, hips=0, rise=0.0)),
    (0.10, dict(lean=-6, head=14, twist=-4, crouch=0.0, draw=0.12, reach=0.0, guard=0.25, hips=-2, rise=0.02)),
    # Coil: the right arm draws back and up, the body sinks onto the back foot and turns away.
    (0.55, dict(lean=-4, head=10, twist=-24, crouch=0.05, draw=0.78, reach=0.0, guard=0.6, hips=-10, rise=0.0)),
    (0.632, dict(lean=-3, head=10, twist=-30, crouch=0.05, draw=1.0, reach=0.0, guard=0.7, hips=-13, rise=0.0)),
    # Grab: a step in, the torso whips round, the arm sweeps across at shoulder height.
    (0.74, dict(lean=12, head=6, twist=6, crouch=0.03, draw=1.0, reach=0.45, guard=0.3, hips=4, rise=0.0)),
    (0.84, dict(lean=20, head=2, twist=30, crouch=0.02, draw=1.0, reach=0.85, guard=0.0, hips=14, rise=0.0)),
    (1.00, dict(lean=24, head=0, twist=40, crouch=0.02, draw=1.0, reach=1.0, guard=-0.2, hips=18, rise=0.0)),
])


def pose_arm_path(poser: Poser, side: str, draw: float, reach: float) -> None:
    """The grabbing arm along hang -> drawn back -> reaching -> across."""
    if reach <= 0.0:
        pose_arm(poser, side, ARM_HANG, ARM_DRAWN, draw)
    elif reach < 0.5:
        pose_arm(poser, side, ARM_DRAWN, ARM_REACH, reach / 0.5)
    else:
        pose_arm(poser, side, ARM_REACH, ARM_ACROSS, (reach - 0.5) / 0.5)


def swipe_feet(p: float) -> dict:
    """Weight back during the coil (the front heel lifts), then a step in on the left foot."""
    w = 0.24
    split = SWIPE_TELEGRAPH_FRAMES - 1
    front, lift = step_arc(p, split / (SWIPE_FRAMES - 1) + 0.02, 0.86, Vector((w - 0.02, -0.19, 0)), Vector((w - 0.06, -0.44, 0)), 0.06)
    return {"l": (front, -6 * window(p, 0.65, 0.9), 12 * window(p, 0.2, 0.6) * (1 - window(p, 0.64, 0.75)), lift),
            "r": (Vector((-w, -0.13, 0)), 8 * window(p, 0.3, 0.6), 22 * window(p, 0.78, 1.0), 0.0)}


def pose_swipe(poser: Poser, p: float) -> None:
    keys = SWIPE_KEYS
    planter = Feet(poser)
    for side, (ball, yaw, heel, lift) in swipe_feet(p).items():
        planter.plant(side, Vector((ball.x * poser.left, ball.y, 0.0)), yaw=yaw, heel=heel, lift=lift, knee_out=0.05)
    poser.turn("pelvis", keys("hips", p))
    move_pelvis(poser, Vector((0.0, -0.20 * keys("reach", p) + 0.04 * keys("draw", p) * (1 - keys("reach", p)), -keys("crouch", p) + keys("rise", p))))
    stoop(poser, lean=keys("lean", p))
    poser.set("head", "forward", keys("head", p))
    twist(poser, keys("twist", p) - keys("hips", p))
    poser.turn("neck_01", -keys("twist", p) * 0.45)
    fit_pelvis(poser, reach=0.975)
    planter.settle()
    draw, reach = keys("draw", p), keys("reach", p)
    pose_arm_path(poser, "r", draw, reach)
    # The other arm comes up a little as a counterweight, then swings back as the grab lands.
    guard = keys("guard", p)
    hang_arm(poser, "l", 0.25 * guard, out=0.12 + 0.2 * abs(guard))
    # Tension: the drawn hand trembles more the longer the coil holds.
    tremble = 0.03 * window(p, 0.3, 0.632) * (1 - window(p, 0.632, 0.7)) * math.sin(p * 140.0)
    if tremble:
        poser.set("hand_r", "down", tremble * 400)
    claws(poser, 30 - 26 * draw * (1 - reach) + 70 * reach ** 2)


def key_swipe(poser: Poser, frames: int) -> bpy.types.Action:
    """The Hollow notices, coils back with the arm drawn and trembling (the telegraph), then
    steps in and whips one wide grab across at shoulder height, overreaching."""
    action = replace_action(poser.rig, "swipe")
    for frame in range(frames):
        p = frame / (frames - 1)
        poser.clear()
        pose_swipe(poser, p)
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 1.0)
    return action


def add_mask_bone(rig: bpy.types.Object) -> None:
    """A carrier bone for the mask, at the head; it follows the head until the death clip lets
    the mask come off and fall."""
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    bones = rig.data.edit_bones
    head = bones["head"]
    mask = bones.new("mask")
    mask.head, mask.tail, mask.roll = head.head.copy(), head.tail.copy(), head.roll
    mask.parent = head
    mask.use_deform = False
    bpy.ops.object.mode_set(mode="OBJECT")


ARM_HANG = {"upperarm": (0.10, 1.0, 0.12), "lowerarm": (0.22, 1.0, 0.06), "hand": (0.18, 1.0, 0.0)}
ARM_GRAB_END = {"upperarm": (1.1, 0.35, -0.1), "lowerarm": (1.2, 0.25, -0.6), "hand": (1.0, 0.3, -0.7)}
ARM_FLUNG = {"upperarm": (-0.25, 0.55, 0.85), "lowerarm": (-0.35, 0.7, 0.75), "hand": (-0.2, 0.9, 0.5)}


def pose_arm(poser: Poser, side: str, a: dict, b: dict, t: float) -> None:
    """Arm between two sets of stage directions (forward, down, out per segment)."""
    for segment in ("upperarm", "lowerarm", "hand"):
        mixed = [x + (y - x) * t for x, y in zip(a[segment], b[segment])]
        poser.aim(f"{segment}_{side}", poser.world(*mixed, side))


def key_hit(poser: Poser, frames: int) -> bpy.types.Action:
    """Hit: the blow lands at once: the mask snaps back, the chest caves and turns, the arms
    are flung out and the knees give; then it hangs itself back into its stoop."""
    action = replace_action(poser.rig, "hit")
    ground = rest_ground(poser.rig)
    for frame in range(frames):
        p = frame / (frames - 1)
        k = math.exp(-p * 3.2) * (1.0 - p) ** 0.6
        poser.clear()
        stoop(poser, lean=-20 * k)
        poser.set("head", "back", 24 * k)
        poser.turn("spine_03", 14 * k)
        poser.turn("spine_01", 5 * k)
        for side in ("l", "r"):
            poser.set(f"thigh_{side}", "forward", 10 * k)
            poser.set(f"calf_{side}", "back", 18 * k)
        ground_feet(poser, ground)
        for side in ("l", "r"):
            pose_arm(poser, side, ARM_HANG, ARM_FLUNG, 0.5 * k)
        claws(poser, 30 + 26 * k)
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 0.0)
    return action


def key_recover(poser: Poser, frames: int) -> bpy.types.Action:
    """Recovery (0.48 s): overreached after the grab, it hangs there a moment, then drags the
    arm back, unwinds and steps back into its stoop."""
    action = replace_action(poser.rig, "recover")
    end_feet = swipe_feet(1.0)
    rest = {"l": Vector((0.24, -0.173, 0.0)), "r": Vector((-0.24, -0.173, 0.0))}
    for frame in range(frames):
        p = frame / (frames - 1)
        back = smooth(max(0.0, (p - 0.18) / 0.82))
        poser.clear()
        planter = Feet(poser)
        for side in ("l", "r"):
            ball, yaw, heel, _ = end_feet[side]
            moved, lift = step_arc(p, 0.35, 0.8, ball, rest[side], 0.04 if side == "l" else 0.0)
            planter.plant(side, Vector((moved.x * poser.left, moved.y, 0.0)), yaw=yaw * (1 - back), heel=heel * (1 - window(p, 0.1, 0.4)),
                          lift=lift, knee_out=0.05)
        settle = 1.0 - back
        # A heavy hang at the end of the reach: a little further, then back.
        sag = 0.06 * math.sin(math.pi * min(1.0, p / 0.3))
        poser.turn("pelvis", SWIPE_KEYS("hips", 1.0) * settle)
        move_pelvis(poser, Vector((0.0, -0.20 * settle, -(SWIPE_KEYS("crouch", 1.0) + sag) * settle)))
        stoop(poser, lean=SWIPE_KEYS("lean", 1.0) * settle + 8 * sag)
        twist(poser, (SWIPE_KEYS("twist", 1.0) - SWIPE_KEYS("hips", 1.0)) * settle)
        poser.turn("neck_01", -SWIPE_KEYS("twist", 1.0) * 0.45 * settle)
        fit_pelvis(poser, reach=0.975)
        planter.settle()
        pose_arm(poser, "r", ARM_ACROSS, ARM_HANG, smooth(max(0.0, (p - 0.25) / 0.75)))
        hang_arm(poser, "l", -0.05 * settle, out=0.12 + 0.04 * settle)
        claws(poser, 70 - 40 * back)
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 1.0)
    return action


def key_stagger(poser: Poser, frames: int) -> bpy.types.Action:
    """Full cannon on the core (1.15 s, sampled by the stagger timer): the blast throws it back,
    arms flung, the mask snapping up, and drives it two stumbling steps backward (right, then
    left); it sways dazed with the mask lolling, then shuffles forward into its stoop again."""
    action = replace_action(poser.rig, "stagger")
    w = 0.12
    rest = {"l": Vector((w, -0.12, 0)), "r": Vector((-w, -0.12, 0))}
    for frame in range(frames):
        p = frame / (frames - 1)
        throw = smooth(min(1.0, p / 0.1)) * (1.0 - smooth(max(0.0, (p - 0.14) / 0.36)))
        daze = smooth(min(1.0, max(0.0, (p - 0.12) / 0.2))) * (1.0 - smooth(max(0.0, (p - 0.78) / 0.22)))
        sway = math.sin(p * math.tau * 1.6)
        planter = Feet(poser)
        r1, lr1 = step_arc(p, 0.04, 0.2, rest["r"], Vector((-w - 0.03, 0.12, 0)), 0.06)
        l1, ll1 = step_arc(p, 0.16, 0.34, rest["l"], Vector((w + 0.02, 0.2, 0)), 0.05)
        r2, lr2 = step_arc(p, 0.66, 0.86, r1, rest["r"], 0.04) if p > 0.66 else (r1, 0.0)
        l2, ll2 = step_arc(p, 0.74, 0.95, l1, rest["l"], 0.04) if p > 0.74 else (l1, 0.0)
        poser.clear()
        planter.plant("r", Vector((r2.x * poser.left, r2.y, 0.0)), yaw=10 * throw, heel=8 * throw, lift=lr1 + lr2)
        planter.plant("l", Vector((l2.x * poser.left, l2.y, 0.0)), yaw=-6 * throw, heel=10 * daze, lift=ll1 + ll2)
        move_pelvis(poser, Vector((0.03 * sway * daze * poser.left, 0.08 * throw, -0.03 * throw - 0.02 * daze)))
        stoop(poser, lean=-34 * throw + 6 * daze)
        poser.set("head", "back", 32 * throw)
        poser.set("neck_01", "left", 18 * daze * sway)
        poser.set("head", "forward", 8 * daze)
        poser.set("spine_02", "left", 10 * daze * sway)
        poser.set("spine_01", "left", -5 * daze * sway)
        fit_pelvis(poser, reach=0.97)
        planter.settle()
        for side, phase in (("l", 0.0), ("r", math.pi)):
            limp = {k: (v[0] + 0.14 * daze * math.sin(p * math.tau * 1.6 + phase), v[1], v[2]) for k, v in ARM_HANG.items()}
            pose_arm(poser, side, limp, ARM_FLUNG, throw)
        claws(poser, 20 + 30 * throw)
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 1.0)
    return action


def key_death(poser: Poser, frames: int) -> bpy.types.Action:
    """Dying: the blow, then the strings are cut: knees fold, the body slumps forward onto its
    heels, the mask comes off and falls face up in front. The game dissolves the last pose."""
    action = bpy.data.actions.new("death")
    poser.rig.animation_data.action = action
    ground = rest_ground(poser.rig)
    contact = [("foot_l", "head"), ("foot_r", "head"), ("ball_l", "tail"), ("ball_r", "tail"),
               ("calf_l", "head"), ("calf_r", "head"), ("hand_l", "tail"), ("hand_r", "tail")]
    poser.clear()
    start = placement_of(poser, "mask")
    turn = Matrix.Rotation(math.radians(-95), 3, Vector((1, 0, 0))) @ Matrix.Rotation(math.radians(28), 3, Vector((0, 0, 1)))
    lying = (Vector((0.30 * poser.left, -1.02, 0.07)), turn @ start[1], turn @ start[2])
    for frame in range(frames):
        p = frame / (frames - 1)
        blow = max(0.0, 1.0 - p / 0.2)
        fold = ease((p - 0.08) / 0.55)
        slump = ease((p - 0.45) / 0.55)
        poser.clear()
        stoop(poser, lean=-18 * blow + 40 * fold + 25 * slump)
        poser.set("head", "back", 20 * blow)
        poser.set("head", "forward", 30 * fold)
        for side in ("l", "r"):
            poser.set(f"thigh_{side}", "forward", 78 * fold)
            poser.set(f"calf_{side}", "back", 120 * fold)
        if fold > 0.0:
            ground_points(poser, ground, contact)
        else:
            ground_feet(poser, ground)
        for side in ("l", "r"):
            pose_arm(poser, side, ARM_FLUNG if blow > 0.5 else ARM_HANG,
                     {"upperarm": (0.35, 1.0, 0.25), "lowerarm": (0.6, 0.9, 0.15), "hand": (0.6, 0.9, 0.0)}, fold)
        claws(poser, 30 - 20 * fold)
        bpy.context.view_layer.update()
        attached = placement_of(poser, "mask")
        drop = ease((p - 0.12) / 0.45)
        centre, axis, up = blend_placement(attached, lying, drop)
        # Thrown a little up and forward as the head snaps, then down onto the floor.
        lift = 0.22 * math.sin(math.pi * min(1.0, drop * 1.25)) * (1.0 - drop)
        bounce = 0.05 * math.sin(math.pi * min(1.0, max(0.0, (p - 0.6) / 0.15)))
        place_bone(poser, "mask", centre + Vector((0.0, 0.0, lift + bounce)), axis, up)
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
    add_mask_bone(rig)
    parts = build_mask(body, rig, materials["mask"], ink)
    parts += build_placket(rig, materials["cloth"], materials["button"], waist=pelvis_z + 0.10)
    for obj in (body, shell, gown, parts[0]):
        add_outline(obj, outline)

    toe = (rig.matrix_world @ rig.data.bones["ball_l"].tail_local) - (rig.matrix_world @ rig.data.bones["foot_l"].head_local)
    poser = Poser(rig, Vector((0, toe.y, 0)).normalized())
    poser.measure_rest([f"{bone}_{side}" for bone in FINGERS + THUMB for side in ("l", "r")], "down")
    add_leg_ik(rig)
    actions = [key_idle(poser, LOOP_FRAMES["idle"]), key_move(poser, LOOP_FRAMES["move"]), key_death(poser, 9)]
    actions += [combat_action(poser, name) for name in COMBAT_FRAMES]
    for action in actions:
        action.use_fake_user = True
    rig.animation_data.action = actions[0]
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, LOOP_FRAMES["idle"]
    bpy.context.scene.render.fps = 12

    args.out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(args.out.resolve()))
    print(f"BUILD_HOLLOW_DONE {args.out} height={rig.dimensions.z:.2f}")


#: Frames of the loops, keyed per frame from a phase: the count only sets how densely the same
#: motion is sampled. The walk advances with the distance covered, so denser frames keep it
#: from stepping at the game's 60 Hz; the idle keeps its duration (the pack step sets the fps).
LOOP_FRAMES = {"idle": 24, "move": 24}

#: Frames of the combat actions; render and pack take the same counts.
COMBAT_FRAMES = {"swipe": SWIPE_FRAMES, "hit": 6, "recover": 10, "stagger": 16}


def combat_action(poser: Poser, name: str) -> bpy.types.Action:
    frames = COMBAT_FRAMES[name]
    return {"swipe": key_swipe, "hit": key_hit, "recover": key_recover, "stagger": key_stagger}[name](poser, frames)


def rekey(args: argparse.Namespace) -> None:
    rig = bpy.data.objects["figure"]
    toe = (rig.matrix_world @ rig.data.bones["ball_l"].tail_local) - (rig.matrix_world @ rig.data.bones["foot_l"].head_local)
    poser = Poser(rig, Vector((0, toe.y, 0)).normalized())
    add_leg_ik(rig)
    measure_axes(poser, [(f"{bone}_{side}", "down") for bone in FINGERS + THUMB for side in ("l", "r")]
                 + [(bone, way) for bone in ("pelvis", "spine_01", "spine_02", "spine_03", "neck_01", "head")
                    for way in ("forward", "back", "left")]
                 + [(f"{bone}_{side}", way) for bone in ("thigh", "calf", "foot", "hand") for side in ("l", "r")
                    for way in ("forward", "back", "down")])
    names = [name.strip() for name in args.rekey.split(",") if name.strip()]
    for name in names:
        if name in LOOP_FRAMES:
            {"idle": key_idle, "move": key_move}[name](poser, LOOP_FRAMES[name])
            print(f"REKEYED {name} frames={LOOP_FRAMES[name]}")
            continue
        combat_action(poser, name)
        print(f"REKEYED {name} frames={COMBAT_FRAMES[name]}")
    finish_rekey(rig, "idle", args.out.resolve())
    print(f"REKEY_HOLLOW_DONE {args.out} actions={','.join(names)}")


if __name__ == "__main__":
    arguments = parse(sys.argv)
    if arguments.rekey:
        rekey(arguments)
    else:
        main()
