"""The Devourer (enemy.devourer) as a rigged figure with its game actions.

    blender -b -P tools/visuals/blender/build_devourer.py -- --out art/production/candidates/enemy.devourer/devourer.blend

Follows docs/current/characters/devourer.md: the widest silhouette of all, big, heavy and bent,
in a long heavy coat stretched over its mass and torn open in front; the torso is a prison: a
mouth-like opening with a glassy, cracked rim, behind it the trapped souls glow and circle. The
face is small, sunk between the shoulders, half hidden by the turned-up collar; tired, not
greedy. Massive arms held protectively in front of the torso.

Bones added to the rig: "maw" (the opening; its scale widens it) and "souls" (child of maw;
turning it lets the trapped souls circle). Actions: idle, move, slam (announce and strike),
recover, devour, stagger, hit, death.
"""
from __future__ import annotations

import argparse
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from figure_kit import (  # noqa: E402
    FINGERS, THUMB, Poser, add_outline, attach, cloth_shell, dominant_bone, ease, enable_mpfb, flat, ground_feet,
    ground_points, loose_fists, outline_material, rest_ground, toon)
from combat_kit import (  # noqa: E402
    Feet, Keys, add_leg_ik, finish_rekey, fit_pelvis, key_legs, measure_axes, move_pelvis, replace_action, smooth,
    step_arc, window)

COAT = (0.045, 0.042, 0.050)
SKIN = (0.075, 0.068, 0.070)
BOOT = (0.035, 0.030, 0.030)
RIM = (0.30, 0.26, 0.36)
VOID = (0.10, 0.03, 0.20)
SOUL = (0.85, 0.75, 1.0)
EMBER = (0.55, 0.22, 1.0)


def parse(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(prog="build_devourer.py")
    parser.add_argument("--rekey", help="comma-separated actions to key again on the opened .blend (no rebuild)")
    parser.add_argument("--out", type=Path, required=True)
    return parser.parse_args(argv[argv.index("--") + 1:] if "--" in argv else [])


def zone(bone: str) -> str:
    if bone in ("head", "neck_01") or bone.startswith(("hand_", "thumb", "index", "middle", "ring", "pinky")):
        return "skin"
    if bone.startswith(("foot_", "ball_")):
        return "boots"
    if bone.startswith(("calf_",)):
        return "legs"
    return "coat"


def paint_body(body, rig, materials) -> list[str]:
    body.data.materials.clear()
    for name in ("skin", "boots", "coat"):
        body.data.materials.append(materials[name])
    per_vertex = [zone(bone) for bone in dominant_bone(body, {bone.name for bone in rig.data.bones})]
    index = {"skin": 0, "boots": 1, "coat": 2, "legs": 2}
    for polygon in body.data.polygons:
        zones = [per_vertex[i] for i in polygon.vertices]
        polygon.material_index = index[max(set(zones), key=zones.count)]
    return per_vertex


def open_front(shell: bpy.types.Object, chest: Vector, radius: Vector) -> None:
    """Tear the coat open over the chest: remove the shell's faces in an ellipse in front."""
    bm = bmesh.new()
    bm.from_mesh(shell.data)
    rng = random.Random(3)
    remove = []
    for face in bm.faces:
        c = shell.matrix_world @ face.calc_center_median()
        if c.y > chest.y + 0.05:
            continue
        dx, dz = (c.x - chest.x) / radius.x, (c.z - chest.z) / radius.z
        if dx * dx + dz * dz < 1.0 + rng.uniform(-0.25, 0.25):
            remove.append(face)
    bmesh.ops.delete(bm, geom=remove, context="FACES")
    bm.to_mesh(shell.data)
    bm.free()


def coat_skirt(rig, material, waist: float, hem: float) -> bpy.types.Object:
    """The long coat below the waist, heavy and closed at the back, open a little in front."""
    rings, segments = 9, 32
    bm = bmesh.new()
    rows = []
    for ring in range(rings):
        t = ring / (rings - 1)
        z = waist - t * (waist - hem)
        rx, ry = 0.25 + 0.09 * t, 0.2 + 0.08 * t
        rows.append([bm.verts.new((rx * math.cos(a), ry * math.sin(a) + 0.02, z)) for a in (s / segments * math.tau for s in range(segments))])
    front = {s for s in range(segments) if abs(math.sin(s / segments * math.tau) + 1.0) < 0.03}
    for ring in range(rings - 1):
        for s in range(segments):
            if s in front:
                continue
            a, b = rows[ring][s], rows[ring][(s + 1) % segments]
            c, d = rows[ring + 1][(s + 1) % segments], rows[ring + 1][s]
            bm.faces.new((a, b, c, d))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new("coat_skirt")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("coat_skirt", mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(material)
    obj.modifiers.new("Thickness", "SOLIDIFY").thickness = 0.015
    left = 1.0 if rig.data.bones["thigh_l"].head_local.x > 0 else -1.0
    knee = (rig.matrix_world @ rig.data.bones["calf_l"].head_local).z
    for name in ("pelvis", "thigh_l", "thigh_r", "calf_l", "calf_r"):
        obj.vertex_groups.new(name=name)
    for vertex in mesh.vertices:
        z = vertex.co.z
        side = "l" if vertex.co.x * left > 0 else "r"
        upper = max(0.0, min(1.0, (waist - z) / max(waist - knee, 1e-3)))
        lower = max(0.0, min(1.0, (knee - z) / max(knee - hem, 1e-3)))
        obj.vertex_groups["pelvis"].add([vertex.index], 1.0 - upper * 0.7, "REPLACE")
        obj.vertex_groups[f"thigh_{side}"].add([vertex.index], upper * 0.7 * (1 - lower * 0.5), "REPLACE")
        obj.vertex_groups[f"calf_{side}"].add([vertex.index], upper * 0.7 * lower * 0.5, "REPLACE")
    armature = obj.modifiers.new("Armature", "ARMATURE")
    armature.object = rig
    obj.parent = rig
    return obj


def add_bones(rig, chest: Vector) -> None:
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    bones = rig.data.edit_bones
    to_rig = rig.matrix_world.inverted()
    maw = bones.new("maw")
    maw.head = to_rig @ chest
    maw.tail = to_rig @ (chest + Vector((0, -0.2, 0)))  # Y points forward, out of the opening
    maw.parent = bones["spine_02"]
    maw.use_deform = False
    souls = bones.new("souls")
    souls.head, souls.tail = maw.head.copy(), maw.tail.copy()
    souls.parent = maw
    souls.use_deform = False
    bpy.ops.object.mode_set(mode="OBJECT")


def build_maw(chest: Vector, materials, ember) -> tuple[list, list]:
    """The opening: a recessed dark cavity, a jagged glassy rim with violet cracks, three
    trapped souls inside (attached to `souls`)."""
    parts, souls = [], []
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, radius=0.2, location=chest + Vector((0, 0.1, 0)))
    cavity = bpy.context.active_object
    cavity.name = "cavity"
    cavity.scale = (1.0, 0.55, 1.25)
    cavity.data.materials.append(materials["void"])
    parts.append(cavity)
    rng = random.Random(11)
    for k in range(18):
        angle = k / 18 * math.tau
        r = Vector((math.cos(angle) * 0.2, 0, math.sin(angle) * 0.25))
        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=0.028, radius2=0.0, depth=rng.uniform(0.05, 0.1),
                                        location=chest + r + Vector((0, -0.03, 0)))
        shard = bpy.context.active_object
        shard.name = f"rim_{k}"
        inward = (-r).normalized() + Vector((0, -0.25, 0))
        shard.rotation_euler = inward.to_track_quat("Z", "Y").to_euler()
        shard.data.materials.append(materials["rim"] if k % 2 else ember)
        parts.append(shard)
    for k in range(3):
        angle = k / 3 * math.tau
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.05,
                                             location=chest + Vector((math.cos(angle) * 0.09, 0.0, math.sin(angle) * 0.11)))
        soul = bpy.context.active_object
        soul.name = f"soul_{k}"
        soul.data.materials.append(materials["soul"])
        souls.append(soul)
    return parts, souls


def build_collar(rig, coat) -> bpy.types.Object:
    neck = rig.matrix_world @ rig.data.bones["neck_01"].head_local
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.17, radius2=0.22, depth=0.26, location=(neck.x, neck.y + 0.02, neck.z + 0.08), end_fill_type="NOTHING")
    collar = bpy.context.active_object
    collar.name = "collar"
    collar.scale = (1.3, 1.05, 1.0)
    collar.data.materials.append(coat)
    collar.modifiers.new("Thickness", "SOLIDIFY").thickness = 0.02
    attach(collar, rig, "spine_03")
    return collar


# ---- Poses ----------------------------------------------------------------------------------

def hunch(poser: Poser, bend: float = 0.0) -> None:
    """Bent forward under its own weight, head low between the shoulders."""
    poser.set("spine_01", "forward", 14 + bend * 0.5)
    poser.set("spine_02", "forward", 12 + bend * 0.3)
    poser.set("spine_03", "forward", 8 + bend * 0.2)
    poser.set("neck_01", "forward", 4)
    poser.set("head", "back", 16)
    for side in ("l", "r"):
        poser.set(f"clavicle_{side}", "up", 8)
        poser.set(f"thigh_{side}", "forward", 10)
        poser.set(f"calf_{side}", "back", 16)


def guard(poser: Poser, open_amount: float = 0.0) -> None:
    """Massive arms held in front of the torso, as if holding something."""
    for side in ("l", "r"):
        poser.aim(f"upperarm_{side}", poser.world(0.35 + 0.2 * open_amount, 1.0, 0.45 + 0.4 * open_amount, side))
        poser.aim(f"lowerarm_{side}", poser.world(1.0, 0.3, -0.55 + 0.9 * open_amount, side))
    loose_fists(poser, 60)


def key_extras(poser: Poser, frame: int, maw: float, spin: float) -> None:
    rig = poser.rig
    bone = rig.pose.bones["maw"]
    bone.scale = (maw, 1.0, maw)
    bone.keyframe_insert("scale", frame=frame)
    souls = rig.pose.bones["souls"]
    souls.rotation_mode = "XYZ"
    souls.rotation_euler = (0, spin, 0)
    souls.keyframe_insert("rotation_euler", frame=frame)


def key_idle(poser: Poser, frames: int) -> bpy.types.Action:
    action = replace_action(poser.rig, "idle")
    ground = rest_ground(poser.rig)
    for frame in range(frames + 1):
        phase = frame / frames * math.tau
        poser.clear()
        hunch(poser, bend=3 * math.sin(phase))
        for side in ("l", "r"):
            poser.set(f"clavicle_{side}", "up", 8 + 3 * math.sin(phase))
        ground_feet(poser, ground)
        guard(poser, open_amount=0.05 * math.sin(phase))
        poser.key(frame + 1)
        key_extras(poser, frame + 1, 1.0 + 0.04 * math.sin(phase), phase)
    return action


def key_move(poser: Poser, frames: int) -> bpy.types.Action:
    """Slow and relentless: heavy steps, the body rolling from side to side."""
    action = replace_action(poser.rig, "move")
    ground = rest_ground(poser.rig)
    for frame in range(frames + 1):
        phase = frame / frames * math.tau
        poser.clear()
        hunch(poser, bend=6)
        poser.set("spine_01", "left", 5 * math.sin(phase))
        for side, offset in (("l", 0.0), ("r", math.pi)):
            leg = phase + offset
            poser.set(f"thigh_{side}", "forward", 18 * math.sin(leg) + 8)
            lift = max(0.0, math.cos(leg + 0.3)) ** 2
            poser.set(f"calf_{side}", "back", 14 + 38 * lift)
        ground_feet(poser, ground)
        guard(poser, open_amount=0.1 + 0.05 * math.sin(phase))
        poser.key(frame + 1)
        key_extras(poser, frame + 1, 1.0, phase)
    return action


def slam_pose(poser: Poser, ground: float, raise_: float, strike: float) -> None:
    """Arms and torso rise overhead (raise_), then crash down (strike)."""
    hunch(poser, bend=-24 * raise_ * (1 - strike) + 30 * strike)
    poser.set("spine_02", "back", 10 * raise_ * (1 - strike))
    for side in ("l", "r"):
        poser.set(f"thigh_{side}", "forward", 10 + 20 * strike)
        poser.set(f"calf_{side}", "back", 16 + 30 * strike)
    ground_feet(poser, ground)
    for side in ("l", "r"):
        up = poser.world(0.3, -1.0, 0.35, side)
        down = poser.world(1.0, 0.9, 0.15, side)
        rest = poser.world(0.35, 1.0, 0.45, side)
        arm = (rest.lerp(up, raise_)).lerp(down, strike).normalized()
        poser.aim(f"upperarm_{side}", arm)
        poser.aim(f"lowerarm_{side}", (arm + Vector((0, 0, -0.3 * strike))).normalized())
    loose_fists(poser, 100)


#: The slam clip: the first frames follow the 0.88 s announce, the rest the 0.18 s blow
#: (Devourer.SlamFrames / SlamAnnounceFrames read the same split).
SLAM_FRAMES = 24
SLAM_ANNOUNCE_FRAMES = 18

SLAM_SPLIT = (SLAM_ANNOUNCE_FRAMES - 1) / (SLAM_FRAMES - 1)

SLAM_KEYS = Keys([
    # Inhale: the chest swells, the maw widens, the shoulders come up.
    (0.00, dict(bend=0, arch=0, arms=0.0, crash=0.0, crouch=0.0, maw=1.0, lift=0.0)),
    (0.12, dict(bend=-6, arch=4, arms=0.12, crash=0.0, crouch=0.0, maw=1.12, lift=0.03)),
    # The arms rise overhead, the torso arches back, the weight goes onto the back foot.
    (0.45, dict(bend=-20, arch=12, arms=0.75, crash=0.0, crouch=0.02, maw=1.18, lift=0.04)),
    # The top: fists together high, still rising a little: the hold that reads as "now".
    (SLAM_SPLIT, dict(bend=-28, arch=18, arms=1.0, crash=0.0, crouch=0.04, maw=1.22, lift=0.05)),
    # The blow: everything comes down at once and folds into the floor.
    (SLAM_SPLIT + 0.08, dict(bend=6, arch=0, arms=1.0, crash=0.55, crouch=0.10, maw=1.05, lift=0.0)),
    (SLAM_SPLIT + 0.17, dict(bend=34, arch=0, arms=1.0, crash=0.95, crouch=0.20, maw=0.92, lift=0.0)),
    (1.00, dict(bend=36, arch=0, arms=1.0, crash=1.0, crouch=0.22, maw=0.94, lift=0.0)),
])


def slam_feet(p: float) -> dict:
    """Braced: the left foot steps forward as the arms rise and both feet take the blow."""
    w = 0.239
    front, lift = step_arc(p, 0.18, 0.5, Vector((w, -0.166, 0)), Vector((w - 0.02, -0.40, 0)), 0.05)
    return {"l": (front, -10 * window(p, 0.2, 0.5), 0.0, lift),
            "r": (Vector((-w - 0.03, -0.10, 0)), 14 * window(p, 0.2, 0.5), 8 * window(p, 0.3, SLAM_SPLIT) * (1 - window(p, SLAM_SPLIT, 0.9)), 0.0)}


def pose_slam(poser: Poser, keys: Keys, p: float, feet: dict) -> None:
    planter = Feet(poser)
    for side, (ball, yaw, heel, lift) in feet.items():
        planter.plant(side, Vector((ball.x * poser.left, ball.y, 0.0)), yaw=yaw, heel=heel, lift=lift, knee_out=0.12)
    crash = keys("crash", p)
    move_pelvis(poser, Vector((0.0, 0.05 * keys("arms", p) * (1 - crash) - 0.12 * crash, -keys("crouch", p) + keys("lift", p))))
    hunch(poser, bend=keys("bend", p))
    poser.set("spine_02", "back", keys("arch", p))
    poser.set("head", "back", 6 * keys("arms", p) * (1 - crash))
    fit_pelvis(poser, reach=0.97)
    planter.settle()
    raised = keys("arms", p)
    for side in ("l", "r"):
        rest = poser.world(0.35, 1.0, 0.45, side)
        # Raised: upper arms up and out with the elbows flared, so the fists meet over the head.
        up = poser.world(0.05, -1.0, 0.55, side)
        down = poser.world(1.0, 1.1, 0.08, side)
        rising = rest.lerp(poser.world(0.7, 0.0, 0.6, side), min(1.0, raised * 2.0)).lerp(up, max(0.0, raised * 2.0 - 1.0))
        arm = rising.lerp(down, crash).normalized()
        poser.aim(f"upperarm_{side}", arm)
        # Forearms fold in and back over the head (fists together), then hammer down.
        inward = poser.world(-0.35, -0.2, -1.0, side)
        fore = (arm + inward * (0.95 * raised * (1 - crash)) + Vector((0, 0, -0.35 * crash))).normalized()
        poser.aim(f"lowerarm_{side}", fore)
    loose_fists(poser, 100)


def key_slam(poser: Poser, frames: int) -> bpy.types.Action:
    """Heavy slam: an inhale, the arms rising overhead with a bracing step, a tense hold at the
    top (the announce), then the blow folds the whole body into the floor."""
    action = replace_action(poser.rig, "slam")
    for frame in range(frames):
        p = frame / (frames - 1)
        poser.clear()
        pose_slam(poser, SLAM_KEYS, p, slam_feet(p))
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 1.0)
        tremble = 0.03 * window(p, 0.5, SLAM_SPLIT) * (1 - window(p, SLAM_SPLIT, SLAM_SPLIT + 0.03)) * math.sin(frame * 2.9)
        key_extras(poser, frame + 1, SLAM_KEYS("maw", p) + tremble, frame * 0.35)
    return action


def key_recover(poser: Poser, frames: int) -> bpy.types.Action:
    """After the slam (0.78 s): it stays folded over its fists a moment, then heaves itself up
    on its arms, draws the front foot back and settles into its hunch."""
    action = replace_action(poser.rig, "recover")
    end = slam_feet(1.0)
    rest = {"l": Vector((0.239, -0.166, 0)), "r": Vector((-0.239, -0.166, 0))}
    for frame in range(frames):
        p = frame / (frames - 1)
        up = smooth(max(0.0, (p - 0.2) / 0.8))
        heave = 0.04 * math.sin(math.pi * min(1.0, max(0.0, (p - 0.15) / 0.4)))
        keys = Keys([(0.0, {c: SLAM_KEYS(c, 1.0) * (1 - up) for c in ("bend", "crouch", "crash", "arms")} | {"arch": 0.0, "lift": heave, "maw": 1.0})])
        feet = {}
        for side in ("l", "r"):
            ball, yaw, heel, _ = end[side]
            moved, lift = step_arc(p, 0.45, 0.85, ball, rest[side], 0.04 if side == "l" else 0.0)
            feet[side] = (moved, yaw * (1 - up), heel * (1 - up), lift)
        poser.clear()
        pose_slam(poser, keys, 0.0, feet)
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 1.0)
        key_extras(poser, frame + 1, 0.94 + 0.06 * up + 0.05 * math.sin(p * math.tau * 1.5), frame * 0.3)
    return action


def key_devour(poser: Poser, frames: int) -> bpy.types.Action:
    """Devouring (1.1 s): it bends over the soul, the opening widens, arms reach down to it."""
    action = bpy.data.actions.new("devour")
    poser.rig.animation_data.action = action
    ground = rest_ground(poser.rig)
    for frame in range(frames):
        t = frame / (frames - 1)
        lean = ease(min(1.0, t / 0.35))
        pull = 0.5 + 0.5 * math.sin(t * math.tau * 2)
        poser.clear()
        hunch(poser, bend=26 * lean)
        for side in ("l", "r"):
            poser.set(f"thigh_{side}", "forward", 10 + 18 * lean)
            poser.set(f"calf_{side}", "back", 16 + 26 * lean)
        ground_feet(poser, ground)
        for side in ("l", "r"):
            poser.aim(f"upperarm_{side}", poser.world(0.9, 0.8, 0.6, side))
            poser.aim(f"lowerarm_{side}", poser.world(0.7, 1.0, -0.2 - 0.2 * pull, side))
        loose_fists(poser, 30)
        poser.key(frame + 1)
        key_extras(poser, frame + 1, 1.0 + 0.45 * lean + 0.1 * pull, t * math.tau * 2)
    return action


def key_stagger(poser: Poser, frames: int) -> bpy.types.Action:
    """Staggered (a full cannon on the torso, 1.4 s, or a devour broken off, 0.38 s; sampled by the
    stagger timer): the blast throws the torso back and the arms wide, the maw gapes, the right
    foot is driven back a heavy step; it reels on the back foot, the left foot slides after it,
    the body sways, and then it hauls itself forward into its hunch and guard again."""
    action = replace_action(poser.rig, "stagger")
    w = 0.239
    rest = {"l": Vector((w, -0.166, 0)), "r": Vector((-w, -0.166, 0))}
    for frame in range(frames):
        p = frame / (frames - 1)
        throw = smooth(min(1.0, p / 0.1)) * (1.0 - smooth(max(0.0, (p - 0.18) / 0.62)))
        sway = math.sin(max(0.0, p - 0.08) / 0.6 * math.tau * 1.25) * (1.0 - smooth(max(0.0, (p - 0.3) / 0.5)))
        gather = smooth(max(0.0, (p - 0.55) / 0.4))
        planter = Feet(poser)
        back_r, lift_r = step_arc(p, 0.04, 0.22, rest["r"], Vector((-w - 0.04, 0.14, 0)), 0.06)
        back_r, lift_r2 = step_arc(p, 0.62, 0.9, back_r, rest["r"], 0.05) if p > 0.62 else (back_r, 0.0)
        slide_l, lift_l = step_arc(p, 0.18, 0.4, rest["l"], Vector((w + 0.02, 0.0, 0)), 0.03)
        slide_l, lift_l2 = step_arc(p, 0.7, 0.95, slide_l, rest["l"], 0.04) if p > 0.7 else (slide_l, 0.0)
        poser.clear()
        planter.plant("r", Vector((back_r.x * poser.left, back_r.y, 0.0)), yaw=12 * throw, heel=6 * throw,
                      lift=lift_r + lift_r2, knee_out=0.12)
        planter.plant("l", Vector((slide_l.x * poser.left, slide_l.y, 0.0)), yaw=-6 * throw, heel=14 * throw,
                      lift=lift_l + lift_l2, knee_out=0.12)
        move_pelvis(poser, Vector((0.025 * sway * poser.left, 0.1 * throw, -0.04 * throw)))
        hunch(poser, bend=-34 * throw)
        poser.set("spine_02", "back", 10 * throw)
        poser.turn("spine_02", 9 * sway)
        poser.set("head", "back", 24 * throw)
        fit_pelvis(poser, reach=0.97)
        planter.settle()
        # Arms flung wide and up by the blast, flailing with the sway, then back into the guard.
        for side in ("l", "r"):
            flung = poser.world(0.25, -0.15, 1.0, side)
            rest_arm = poser.world(0.35, 1.0, 0.45, side)
            out = smooth(min(1.0, p / 0.08)) * (1.0 - gather)
            reach = rest_arm.lerp(flung, out) + Vector((0, 0, 0.25 * sway * (1 if side == "l" else -1) * (1 - gather)))
            poser.aim(f"upperarm_{side}", reach.normalized())
            poser.aim(f"lowerarm_{side}", poser.world(0.9, 0.2 + 0.4 * gather, 0.4 - 0.9 * gather, side).lerp(reach, 0.4 * (1 - gather)).normalized())
        loose_fists(poser, 40 + 20 * gather)
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 1.0)
        key_extras(poser, frame + 1, 1.0 + 0.45 * throw + 0.08 * sway, frame * 0.6)
    return action


def key_hit(poser: Poser, frames: int) -> bpy.types.Action:
    """Hit: too heavy to be thrown, it rocks back on its heels, the maw clenches, the arms
    jerk up; then the weight rolls forward again."""
    action = replace_action(poser.rig, "hit")
    ground = rest_ground(poser.rig)
    for frame in range(frames):
        p = frame / (frames - 1)
        k = math.exp(-p * 2.6) * (1.0 - p) ** 0.5
        poser.clear()
        hunch(poser, bend=-14 * k)
        poser.set("head", "back", 10 * k)
        poser.turn("spine_03", 10 * k)
        ground_feet(poser, ground)
        guard(poser, open_amount=0.35 * k)
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 0.0)
        key_extras(poser, frame + 1, 1.0 - 0.12 * k, 0.0)
    return action


def key_death(poser: Poser, frames: int) -> bpy.types.Action:
    """Dying (the first three quarters of 0.82 s; the dissolve then takes the last pose): the prison
    cracks open, it rears back with the arms thrown up, the knees give, it drops onto them and
    topples forward onto its fists and face, a heavy heap; the game dissolves it and every
    trapped soul comes free."""
    action = replace_action(poser.rig, "death")
    ground = rest_ground(poser.rig)
    contact = [("foot_l", "head"), ("foot_r", "head"), ("ball_l", "tail"), ("ball_r", "tail"),
               ("calf_l", "head"), ("calf_r", "head"), ("hand_l", "tail"), ("hand_r", "tail"),
               ("head", "tail"), ("spine_03", "tail"), ("pelvis", "head")]
    for frame in range(frames):
        t = frame / (frames - 1)
        rear = smooth(min(1.0, t / 0.22)) * (1.0 - smooth(max(0.0, (t - 0.22) / 0.25)))
        kneel = smooth((t - 0.18) / 0.32)
        fall = smooth((t - 0.48) / 0.42) ** 1.4
        poser.clear()
        poser.set("pelvis", "forward", 70 * fall)
        hunch(poser, bend=-30 * rear + 26 * kneel * (1 - fall))
        poser.set("head", "back", 22 * rear)
        poser.set("neck_01", "forward", 18 * fall)
        for side, lean in (("l", 1.0), ("r", 0.85)):
            poser.set(f"thigh_{side}", "forward", 10 + 70 * kneel * lean * (1 - fall) + 8 * fall)
            poser.set(f"calf_{side}", "back", 16 + 100 * kneel * (1 - fall) + 30 * fall)
        if kneel > 0.0 or fall > 0.0:
            ground_points(poser, ground, contact)
        else:
            ground_feet(poser, ground)
        # Arms thrown up as it cracks, hanging as it kneels, then out in front on the floor.
        for side in ("l", "r"):
            held = poser.world(0.35, 1.0, 0.45, side)
            up = poser.world(0.2, -0.7, 0.7, side)
            hang = poser.world(0.2, 1.0, 0.35, side)
            ahead = poser.world(1.0, 0.25, 0.45, side)
            arm = held.lerp(up, smooth(min(1.0, t / 0.12))).lerp(hang, smooth(max(0.0, (t - 0.2) / 0.25))).lerp(ahead, fall).normalized()
            poser.aim(f"upperarm_{side}", arm)
            poser.aim(f"lowerarm_{side}", (arm + Vector((0, 0, -0.25 * (1 - fall)))).normalized())
        loose_fists(poser, 30 + 40 * fall)
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 0.0)
        key_extras(poser, frame + 1, 1.0 + 0.8 * min(1.0, t / 0.3), t * math.tau)
    return action


def main() -> None:
    args = parse(sys.argv)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    human_service, target_service = enable_mpfb()
    macro = target_service.get_default_macro_info_dict()
    macro.update({"gender": 0.9, "age": 0.7, "muscle": 0.65, "weight": 1.0, "proportions": 0.3, "height": 0.95})
    body = human_service.create_human(macro_detail_dict=macro)
    # The widest silhouette of all: a barrel of a torso, broad shoulders, massive arms, a thick
    # neck sunk into it. MPFB's measure and scale targets, before rigging.
    import importlib
    from figure_kit import MPFB
    data = Path(importlib.import_module(MPFB).__file__).parent / "data" / "targets"
    bulk = [("torso", "torso-scale-horiz-incr", 1.0), ("torso", "torso-scale-depth-incr", 1.0),
            ("torso", "measure-shoulder-dist-incr", 1.0), ("torso", "measure-waist-circ-incr", 1.0),
            ("torso", "measure-underbust-circ-incr", 0.8), ("hip", "hip-scale-horiz-incr", 0.6),
            ("neck", "neck-scale-horiz-incr", 1.0), ("neck", "neck-scale-depth-incr", 0.8), ("neck", "measure-neck-height-decr", 0.8)]
    for side in ("l", "r"):
        for part in ("upperarm", "lowerarm"):
            for kind in ("fat", "muscle", "scale-horiz", "scale-depth"):
                bulk.append(("arms", f"{side}-{part}-{kind}-incr", 0.9))
    for folder, name, weight in bulk:
        path = data / folder / f"{name}.target.gz"
        if path.exists():
            target_service.load_target(body, str(path), weight=weight, name=name)
    rig = human_service.add_builtin_rig(body, "game_engine")
    rig.name = "figure"

    materials = {"skin": toon("skin", SKIN, brush=0.1), "boots": toon("boots", BOOT), "coat": toon("coat", COAT, brush=0.14),
                 "rim": toon("rim", RIM, brush=0.04), "void": flat("void", VOID), "soul": flat("soul", SOUL)}
    ember = flat("ember", EMBER)
    outline = outline_material()
    per_vertex = paint_body(body, rig, materials)
    coat = cloth_shell(body, per_vertex, {"coat"}, rig, materials["coat"], "coat", push=0.03)
    chest_bone = rig.matrix_world @ rig.data.bones["spine_03"].head_local
    front = min((rig.matrix_world @ v.co).y for v in body.data.vertices if abs((rig.matrix_world @ v.co).z - chest_bone.z) < 0.05)
    chest = Vector((0.0, front + 0.02, chest_bone.z - 0.06))
    open_front(coat, chest, Vector((0.24, 0, 0.3)))
    open_front(body, chest, Vector((0.2, 0, 0.26)))
    pelvis_z = (rig.matrix_world @ rig.data.bones["pelvis"].head_local).z
    calf_z = (rig.matrix_world @ rig.data.bones["calf_l"].head_local).z
    skirt = coat_skirt(rig, materials["coat"], waist=pelvis_z + 0.08, hem=calf_z - 0.22)
    collar = build_collar(rig, materials["coat"])
    add_bones(rig, chest)
    parts, souls = build_maw(chest, materials, ember)
    for part in parts:
        attach(part, rig, "maw")
    for soul in souls:
        attach(soul, rig, "souls")
    for obj in (body, coat, skirt, collar):
        add_outline(obj, outline)

    toe = (rig.matrix_world @ rig.data.bones["ball_l"].tail_local) - (rig.matrix_world @ rig.data.bones["foot_l"].head_local)
    poser = Poser(rig, Vector((0, toe.y, 0)).normalized())
    poser.measure_rest([f"{bone}_{side}" for bone in FINGERS + THUMB for side in ("l", "r")], "down")
    add_leg_ik(rig)
    actions = [key_idle(poser, LOOP_FRAMES["idle"]), key_move(poser, LOOP_FRAMES["move"]), key_devour(poser, 10)]
    actions += [combat_action(poser, name) for name in COMBAT_FRAMES]
    for action in actions:
        action.use_fake_user = True
    rig.animation_data.action = actions[0]
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, LOOP_FRAMES["idle"]
    bpy.context.scene.render.fps = 12
    args.out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(args.out.resolve()))
    print(f"BUILD_DEVOURER_DONE {args.out} height={rig.dimensions.z:.2f} chest={tuple(round(v, 2) for v in chest)}")


#: Frames of the loops, keyed per frame from a phase: the count only sets how densely the same
#: motion is sampled. The walk advances with the distance covered, so denser frames keep it
#: from stepping at the game's 60 Hz; the idle keeps its duration (the pack step sets the fps).
LOOP_FRAMES = {"idle": 20, "move": 20}

#: Frames of the combat actions; render and pack take the same counts.
COMBAT_FRAMES = {"slam": SLAM_FRAMES, "recover": 12, "hit": 6, "stagger": 18, "death": 14}


def combat_action(poser: Poser, name: str) -> bpy.types.Action:
    return {"slam": key_slam, "recover": key_recover, "hit": key_hit, "stagger": key_stagger,
            "death": key_death}[name](poser, COMBAT_FRAMES[name])


def rekey(args: argparse.Namespace) -> None:
    rig = bpy.data.objects["figure"]
    toe = (rig.matrix_world @ rig.data.bones["ball_l"].tail_local) - (rig.matrix_world @ rig.data.bones["foot_l"].head_local)
    poser = Poser(rig, Vector((0, toe.y, 0)).normalized())
    add_leg_ik(rig)
    measure_axes(poser, [(f"{bone}_{side}", "down") for bone in FINGERS + THUMB for side in ("l", "r")]
                 + [(bone, way) for bone in ("pelvis", "spine_01", "spine_02", "spine_03", "neck_01", "head")
                    for way in ("forward", "back")]
                 + [(f"{bone}_{side}", way) for bone in ("thigh", "calf", "clavicle") for side in ("l", "r")
                    for way in ("forward", "back", "up")])
    names = [name.strip() for name in args.rekey.split(",") if name.strip()]
    for name in names:
        if name in LOOP_FRAMES:
            {"idle": key_idle, "move": key_move}[name](poser, LOOP_FRAMES[name])
            print(f"REKEYED {name} frames={LOOP_FRAMES[name]}")
            continue
        combat_action(poser, name)
        print(f"REKEYED {name} frames={COMBAT_FRAMES[name]}")
    finish_rekey(rig, "idle", args.out.resolve())
    print(f"REKEY_DEVOURER_DONE {args.out} actions={','.join(names)}")


if __name__ == "__main__":
    arguments = parse(sys.argv)
    if arguments.rekey:
        rekey(arguments)
    else:
        main()
