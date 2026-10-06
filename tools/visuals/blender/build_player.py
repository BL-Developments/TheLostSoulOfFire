"""Build the player figure for sprite rendering (3D path B: MPFB2 body, clothes by script).

    blender -b -P tools/visuals/blender/build_player.py -- --out art/production/candidates/player/player.blend

Follows docs/current/characters/protagonist.md: a slender young man in a long asymmetric dark
wool coat (longer on the weapon side), dark shirt and trousers, boots, messy dark chin-length
hair, a leather chest strap with a brass compass whose carmine lid is the one saturated accent.
The look is painted, not rendered: toon ramps with brush noise and a dark inverted-hull
outline. Scythe and Soul Cannon are drawn by the game as their own sprites for now.

The rig is MPFB2's game_engine rig. Actions "idle" and "run" are keyed by script; bone axes
are found by measuring, not assumed, so the script survives other rigs with the same bone names.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from figure_kit import (  # noqa: E402
    FINGERS, THUMB, Poser, add_outline, attach, best_pole_angle, blend_placement, bone_world, cloth_shell, dominant_bone,
    ease, enable_mpfb, flat, ground_feet, ground_points, loose_fists, outline_material, place_bone, placement_of,
    rest_ground, shaped_coordinates, toon)
from combat_kit import (  # noqa: E402
    Feet, Keys, add_leg_ik, bone_point, chest_point, finish_rekey, fit_pelvis, impulse_drift, key_legs, measure_axes,
    move_pelvis, replace_action, smooth, stage_coordinates, step_arc, twist, window)

# Palette (linear-ish values chosen by eye under the toon ramp). Dark, but one value step above
# the arena floor's shadows, so folds and the coat's edge still read on the painted ground.
COAT = (0.052, 0.047, 0.064)
SHIRT = (0.075, 0.066, 0.090)
TROUSERS = (0.040, 0.037, 0.048)
BOOTS = (0.10, 0.062, 0.040)
SKIN = (0.56, 0.41, 0.33)
HAIR = (0.024, 0.020, 0.026)
INK = (0.012, 0.009, 0.014)
WOOD = (0.10, 0.060, 0.034)
IRON = (0.060, 0.060, 0.070)
STEEL = (0.20, 0.20, 0.23)
DEATH_FLAME = (0.55, 0.22, 1.0)
BONE_WHITE = (0.50, 0.47, 0.40)

#: Scythe in weapon space: the shaft runs along +Y from the butt to the blade, the origin is the
#: point between the hands; the blade stands out along +Z from the top.
SHAFT_BOTTOM, SHAFT_TOP = -0.55, 1.05
GRIPS = {"r": -0.16, "l": 0.16}
LEATHER = (0.11, 0.065, 0.040)
BRASS = (0.42, 0.30, 0.12)
CARMINE = (0.42, 0.015, 0.05)


def parse(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(prog="build_player.py")
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--rekey", help="comma-separated actions to key again on the opened .blend (no rebuild)")
    parser.add_argument("--paths", type=Path, help="JSON file for the blade paths of the swings")
    return parser.parse_args(argv[argv.index("--") + 1:] if "--" in argv else [])


# ---- Body ---------------------------------------------------------------------------------

def zone(bone: str) -> str:
    if bone in ("head", "neck_01") or bone.startswith(("hand_", "thumb", "index", "middle", "ring", "pinky")):
        return "skin"
    if bone.startswith(("foot_", "ball_")):
        return "boots"
    if bone.startswith(("thigh_", "calf_")):
        return "trousers"
    return "shirt"


def paint_body(body: bpy.types.Object, rig: bpy.types.Object, materials: dict[str, bpy.types.Material]) -> list[str]:
    body.data.materials.clear()
    order = ["shirt", "trousers", "boots", "skin"]
    for key in order:
        body.data.materials.append(materials[key])
    bones = {bone.name for bone in rig.data.bones}
    per_vertex = [zone(bone) for bone in dominant_bone(body, bones)]
    for polygon in body.data.polygons:
        votes: dict[str, int] = {}
        for index in polygon.vertices:
            votes[per_vertex[index]] = votes.get(per_vertex[index], 0) + 1
        polygon.material_index = order.index(max(votes, key=votes.get))
    return per_vertex


def coat_skirt(rig: bpy.types.Object, coat: bpy.types.Material, waist: float, knee: float) -> bpy.types.Object:
    """The long skirt of the coat from the waist to below the knee, longer on the right
    (weapon) side, weighted to pelvis and thighs by height and side."""
    right_sign = 1.0 if rig.data.bones["thigh_r"].head_local.x > 0 else -1.0
    bm = bmesh.new()
    rings, segments = 9, 28
    rows = []
    for ring in range(rings):
        t = ring / (rings - 1)
        z = waist - t * (waist - knee + 0.08)
        row = []
        for segment in range(segments):
            angle = segment / segments * math.tau
            side = math.cos(angle) * right_sign  # +1 on the figure's right side
            hem_drop = 0.07 * max(0.0, side) * t
            rx = 0.17 + 0.07 * t
            ry = 0.12 + 0.06 * t
            row.append(bm.verts.new((rx * math.cos(angle), ry * math.sin(angle), z - hem_drop)))
        rows.append(row)
    front_sign = -1.0  # MPFB figures face -Y
    for ring in range(rings - 1):
        for segment in range(segments):
            # An open coat: below the waist band the front panel is cut away between the flaps.
            middle = (segment + 0.5) / segments * math.tau
            facing_front = math.sin(middle) * front_sign > math.cos(math.radians(26))
            if ring >= 1 and facing_front:
                continue
            a, b = rows[ring][segment], rows[ring][(segment + 1) % segments]
            c, d = rows[ring + 1][(segment + 1) % segments], rows[ring + 1][segment]
            bm.faces.new((a, b, c, d))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new("coat_skirt")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("coat_skirt", mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(coat)
    solidify = obj.modifiers.new("Thickness", "SOLIDIFY")
    solidify.thickness = 0.012

    for name in ("pelvis", "thigh_l", "thigh_r", "calf_l", "calf_r"):
        obj.vertex_groups.new(name=name)
    thigh_l = rig.pose.bones["thigh_l"].head
    for vertex in mesh.vertices:
        z = vertex.co.z
        down = max(0.0, min(1.0, (waist - z) / (waist - knee)))
        left = vertex.co.x * (1 if thigh_l.x > 0 else -1) > 0
        leg = "thigh_l" if left else "thigh_r"
        obj.vertex_groups["pelvis"].add([vertex.index], 1.0 - down * 0.85, "REPLACE")
        obj.vertex_groups[leg].add([vertex.index], down * 0.85, "REPLACE")
    armature = obj.modifiers.new("Armature", "ARMATURE")
    armature.object = rig
    obj.parent = rig
    return obj


def build_hair(rig: bpy.types.Object, material: bpy.types.Material) -> bpy.types.Object:
    head, tail = bone_world(rig, "head")
    centre = head + (tail - head) * 0.62 + Vector((0, 0.012, 0))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=14, radius=0.112, location=centre)
    hair = bpy.context.active_object
    hair.name = "hair"
    hair.scale = (1.02, 1.12, 1.0)
    bm = bmesh.new()
    bm.from_mesh(hair.data)
    face_y = min(v.co.y for v in bm.verts)
    for vertex in list(bm.verts):
        # Open the face: drop the front-lower part of the sphere.
        if vertex.co.y < face_y * 0.35 and vertex.co.z < 0.03:
            bm.verts.remove(vertex)
    bm.to_mesh(hair.data)
    bm.free()
    displace = hair.modifiers.new("Messy", "DISPLACE")
    texture = bpy.data.textures.new("hair-noise", "CLOUDS")
    texture.noise_scale = 0.06
    displace.texture = texture
    displace.strength = 0.03
    hair.data.materials.append(material)
    bpy.ops.object.shade_smooth()
    attach(hair, rig, "head")
    return hair


def build_strap_and_compass(rig: bpy.types.Object, leather: bpy.types.Material, brass: bpy.types.Material, carmine: bpy.types.Material) -> list[bpy.types.Object]:
    chest_head, chest_tail = bone_world(rig, "spine_03")
    shoulder_l = bone_world(rig, "clavicle_l")[1]
    hip_r = bone_world(rig, "thigh_r")[0]
    curve = bpy.data.curves.new("strap", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = 0.008
    spline = curve.splines.new("POLY")
    points = [shoulder_l + Vector((0, -0.02, 0.02)), chest_head + Vector((0, -0.135, 0.06)), hip_r + Vector((0, -0.12, 0.02))]
    spline.points.add(len(points) - 1)
    for point, position in zip(spline.points, points):
        point.co = (*position, 1)
    strap = bpy.data.objects.new("strap", curve)
    bpy.context.collection.objects.link(strap)
    curve.materials.append(leather)
    curve.extrude = 0.012
    attach(strap, rig, "spine_03")

    compass_at = shoulder_l + (chest_head - shoulder_l) * 0.35 + Vector((0, -0.145, -0.04))
    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.034, depth=0.014, location=compass_at, rotation=(math.radians(90), 0, 0))
    case = bpy.context.active_object
    case.name = "compass"
    case.data.materials.append(brass)
    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.031, depth=0.006, location=compass_at + Vector((0, -0.009, 0)), rotation=(math.radians(90), 0, 0))
    lid = bpy.context.active_object
    lid.name = "compass_lid"
    lid.data.materials.append(carmine)
    for part in (case, lid):
        attach(part, rig, "spine_03")
    return [strap, case, lid]


def build_collar(rig: bpy.types.Object, coat: bpy.types.Material) -> bpy.types.Object:
    """A low standing collar from the shoulders up the neck, open at the front; it also hides
    the stepped edge where the coat's shirt zone meets the skin."""
    neck = rig.matrix_world @ rig.data.bones["neck_01"].head_local
    neck_top = rig.matrix_world @ rig.data.bones["neck_01"].tail_local
    lean = (neck_top.y - neck.y) / max(neck_top.z - neck.z, 1e-3)
    bm = bmesh.new()
    segments = 28
    # (height above the neck's base, half width, half depth)
    profile = [(-0.09, 0.19, 0.14), (-0.04, 0.14, 0.11), (0.0, 0.095, 0.088), (0.04, 0.078, 0.076), (0.072, 0.074, 0.072)]
    rows = []
    for lift, rx, ry in profile:
        centre_y = neck.y + max(0.0, lift) * lean
        rows.append([bm.verts.new((rx * math.cos(a), centre_y + ry * math.sin(a), neck.z + lift))
                     for a in (index / segments * math.tau for index in range(segments))])
    for ring in range(len(rows) - 1):
        for segment in range(segments):
            middle = (segment + 0.5) / segments * math.tau
            # MPFB figures face -Y: a short slit at the throat in the top row only.
            if ring == len(rows) - 2 and -math.sin(middle) > math.cos(math.radians(16)):
                continue
            a, b = rows[ring][segment], rows[ring][(segment + 1) % segments]
            c, d = rows[ring + 1][(segment + 1) % segments], rows[ring + 1][segment]
            bm.faces.new((a, b, c, d))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new("collar")
    bm.to_mesh(mesh)
    bm.free()
    collar = bpy.data.objects.new("collar", mesh)
    bpy.context.collection.objects.link(collar)
    mesh.materials.append(coat)
    collar.modifiers.new("Thickness", "SOLIDIFY").thickness = 0.01
    attach(collar, rig, "spine_03")
    return collar


def build_belt(rig: bpy.types.Object, waist: float, leather: bpy.types.Material, brass: bpy.types.Material) -> list[bpy.types.Object]:
    """Leather belt over the coat's waist band, with a small brass buckle at the front."""
    curve = bpy.data.curves.new("belt", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = 0.006
    curve.extrude = 0.014
    spline = curve.splines.new("POLY")
    segments = 32
    spline.points.add(segments - 1)
    for index, point in enumerate(spline.points):
        angle = index / segments * math.tau
        point.co = (0.176 * math.cos(angle), 0.128 * math.sin(angle), waist - 0.01, 1)
    spline.use_cyclic_u = True
    belt = bpy.data.objects.new("belt", curve)
    bpy.context.collection.objects.link(belt)
    curve.materials.append(leather)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, -0.134, waist - 0.01))
    buckle = bpy.context.active_object
    buckle.name = "buckle"
    buckle.scale = (0.036, 0.008, 0.03)
    buckle.data.materials.append(brass)
    for part in (belt, buckle):
        attach(part, rig, "pelvis")
    return [belt, buckle]


def build_face(body: bpy.types.Object, rig: bpy.types.Object, ink: bpy.types.Material, hair: bpy.types.Material) -> list[bpy.types.Object]:
    """Dark eyes and brows on the face, found from MPFB's eye helper geometry, so the face reads
    at game size (protagonist sheet: clear brows, calm dark eyes)."""
    parts = []
    shaped = shaped_coordinates(body)
    for side in ("l", "r"):
        group = body.vertex_groups.get(f"helper-{side}-eye")
        if group is None:
            continue
        points = [body.matrix_world @ shaped[v.index] for v in body.data.vertices if any(g.group == group.index and g.weight > 0.5 for g in v.groups)]
        if not points:
            continue
        centre = sum(points, Vector()) / len(points)
        front = min(point.y for point in points)
        eye_at = Vector((centre.x, front - 0.002, centre.z))
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.0105, location=eye_at)
        eye = bpy.context.active_object
        eye.name = f"eye_{side}"
        eye.scale = (1.0, 0.45, 0.62)
        eye.data.materials.append(ink)
        outward = 1.0 if centre.x > 0 else -1.0
        bpy.ops.mesh.primitive_cube_add(size=1, location=(centre.x + outward * 0.002, front - 0.004, centre.z + 0.021))
        brow = bpy.context.active_object
        brow.name = f"brow_{side}"
        brow.scale = (0.03, 0.006, 0.0075)
        brow.rotation_euler = (0, math.radians(-9 * outward), 0)
        brow.data.materials.append(hair)
        parts += [eye, brow]
    for part in parts:
        attach(part, rig, "head")
    return parts


# ---- Scythe -------------------------------------------------------------------------------

def build_scythe(materials: dict[str, bpy.types.Material], outline: bpy.types.Material) -> bpy.types.Object:
    """Protagonist sheet: S-curved wooden shaft, deeply curved blade with a back spur, iron collar
    holding a Death Flame core, counterweight spike at the butt. Built in weapon space."""
    parts = []
    # Shaft: a gentle S in the X-Y plane.
    curve = bpy.data.curves.new("scythe_shaft", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = 0.016
    curve.bevel_resolution = 2
    spline = curve.splines.new("POLY")
    steps = 16
    spline.points.add(steps)
    for index, point in enumerate(spline.points):
        t = index / steps
        y = SHAFT_BOTTOM + (SHAFT_TOP - SHAFT_BOTTOM) * t
        point.co = (0.03 * math.sin(t * math.tau), y, 0.0, 1)
    shaft = bpy.data.objects.new("scythe_shaft", curve)
    bpy.context.collection.objects.link(shaft)
    curve.materials.append(materials["wood"])
    parts.append(shaft)

    # Blade: a crescent along a spine curving back toward the butt, edge on the inner side.
    bm = bmesh.new()
    samples = 14
    outer, inner = [], []
    for index in range(samples + 1):
        u = index / samples
        spine = Vector((0.0, SHAFT_TOP + 0.02 - 0.30 * u * u, 0.03 + 0.70 * u))
        tangent = Vector((0.0, -0.60 * u, 0.70)).normalized()
        inward = Vector((0.0, -tangent.z, tangent.y))  # toward the butt side of the curve
        width = 0.12 * (1.0 - u) ** 0.75 + 0.004
        outer.append(bm.verts.new(spine - inward * width * 0.15))
        inner.append(bm.verts.new(spine + inward * width * 0.85))
    for index in range(samples):
        bm.faces.new((outer[index], outer[index + 1], inner[index + 1], inner[index]))
    # Back spur on the outer side near the root.
    spur_base = Vector((0.0, SHAFT_TOP + 0.04, 0.06))
    a = bm.verts.new(spur_base + Vector((0, 0, -0.03)))
    b = bm.verts.new(spur_base + Vector((0, 0.13, 0.05)))
    c = bm.verts.new(spur_base + Vector((0, 0, 0.05)))
    bm.faces.new((a, b, c))
    mesh = bpy.data.meshes.new("scythe_blade")
    bm.to_mesh(mesh)
    bm.free()
    blade = bpy.data.objects.new("scythe_blade", mesh)
    bpy.context.collection.objects.link(blade)
    mesh.materials.append(materials["steel"])
    blade.modifiers.new("Thickness", "SOLIDIFY").thickness = 0.008
    parts.append(blade)

    # Iron collar with the Death Flame core, and the counterweight spike.
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.028, depth=0.09, location=(0, SHAFT_TOP - 0.03, 0), rotation=(math.radians(90), 0, 0))
    collar = bpy.context.active_object
    collar.name = "scythe_collar"
    collar.data.materials.append(materials["iron"])
    parts.append(collar)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.017, location=(0.024, SHAFT_TOP - 0.03, 0.0))
    core = bpy.context.active_object
    core.name = "scythe_core"
    core.data.materials.append(materials["death_flame"])
    parts.append(core)
    bpy.ops.mesh.primitive_cone_add(vertices=10, radius1=0.022, depth=0.12, location=(0, SHAFT_BOTTOM - 0.05, 0), rotation=(math.radians(90), 0, 0))
    spike = bpy.context.active_object
    spike.name = "scythe_spike"
    spike.data.materials.append(materials["iron"])
    parts.append(spike)

    for part in (shaft, blade, collar, spike):
        if part.type == "MESH":
            add_outline(part, outline, thickness=0.005)
    root = bpy.data.objects.new("scythe", None)
    bpy.context.collection.objects.link(root)
    for part in parts:
        part.parent = root
    return root


def add_weapon_rig(rig: bpy.types.Object) -> None:
    """A non-deforming `weapon` bone (child of the pelvis) carries the scythe; grip bones on the
    shaft are IK targets for both forearms, pole bones behind the elbows keep them outward."""
    left = 1.0 if rig.data.bones["upperarm_l"].head_local.x > 0 else -1.0
    to_rig = rig.matrix_world.inverted()
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    bones = rig.data.edit_bones
    hold = to_rig @ Vector((0.0, -0.30, 1.0))
    weapon = bones.new("weapon")
    weapon.head = hold
    weapon.tail = hold + Vector((left * 0.25, 0, 0))  # bone Y = shaft, toward the blade (figure's left)
    weapon.align_roll(Vector((0, 0, 1)))  # bone Z = up: the blade stands up in the rest hold
    weapon.parent = bones["pelvis"]
    weapon.use_deform = False
    for side, offset in GRIPS.items():
        grip = bones.new(f"grip_{side}")
        grip.head = hold + Vector((left * offset, 0, 0))
        grip.tail = grip.head + Vector((0, 0, 0.05))
        grip.parent = weapon
        grip.use_deform = False
    for side, sign in (("l", left), ("r", -left)):
        pole = bones.new(f"pole_{side}")
        pole.head = to_rig @ Vector((sign * 0.55, 0.35, 1.05))
        pole.tail = pole.head + Vector((0, 0, 0.05))
        pole.parent = bones["spine_02"]
        pole.use_deform = False
    bpy.ops.object.mode_set(mode="OBJECT")

    for side in ("l", "r"):
        # A little stretch closes the last centimetres when a swing reaches across the body.
        for bone in (f"upperarm_{side}", f"lowerarm_{side}"):
            rig.pose.bones[bone].ik_stretch = 0.08
        constraint = rig.pose.bones[f"lowerarm_{side}"].constraints.new("IK")
        constraint.name = "IK scythe"
        constraint.target, constraint.subtarget = rig, f"grip_{side}"
        constraint.pole_target, constraint.pole_subtarget = rig, f"pole_{side}"
        constraint.chain_count = 2
        constraint.pole_angle = best_pole_angle(rig, side, constraint)


def place_weapon(poser: Poser, centre: Vector, shaft: Vector, blade: Vector) -> None:
    """The scythe in rig space: grip midpoint at `centre`, shaft toward the blade, blade standing out."""
    place_bone(poser, "weapon", centre, shaft, blade)


def build_cannon(materials: dict[str, bpy.types.Material], outline: bpy.types.Material) -> bpy.types.Object:
    """Soul Cannon from the sheet: a braced reliquary box of blackened iron with bone-white
    fittings, a grated chamber with Death Flame behind the bars, and a flared, ribbed muzzle.
    Cannon space: origin at the pistol grip, barrel along +Y, top along +Z."""
    parts = []

    def box(name: str, size: tuple[float, float, float], at: tuple[float, float, float], material: str) -> bpy.types.Object:
        bpy.ops.mesh.primitive_cube_add(size=1, location=at)
        obj = bpy.context.active_object
        obj.name = name
        obj.scale = size
        obj.data.materials.append(materials[material])
        parts.append(obj)
        return obj

    body = box("cannon_body", (0.13, 0.46, 0.13), (0, 0.05, 0.075), "iron")
    for index, y in enumerate((-0.17, 0.27)):
        box(f"cannon_band_{index}", (0.15, 0.025, 0.15), (0, y, 0.075), "bone")
    for side in (-1, 1):
        box(f"cannon_brace_{side}", (0.012, 0.42, 0.018), (side * 0.07, 0.05, 0.14), "bone")
        box(f"cannon_chamber_{side}", (0.006, 0.15, 0.08), (side * 0.064, 0.06, 0.075), "death_flame")
        for bar in range(3):
            box(f"cannon_bar_{side}_{bar}", (0.008, 0.012, 0.09), (side * 0.069, 0.01 + bar * 0.05, 0.075), "iron")
    box("cannon_grip", (0.04, 0.05, 0.10), (0, 0.0, -0.035), "wood")
    # Flared, ribbed muzzle.
    bpy.ops.mesh.primitive_cone_add(vertices=16, radius1=0.055, radius2=0.105, depth=0.16, end_fill_type="NOTHING",
                                    location=(0, 0.36, 0.075), rotation=(math.radians(-90), 0, 0))
    muzzle = bpy.context.active_object
    muzzle.name = "cannon_muzzle"
    muzzle.data.materials.append(materials["iron"])
    muzzle.modifiers.new("Thickness", "SOLIDIFY").thickness = 0.012
    parts.append(muzzle)
    for index, (y, radius) in enumerate(((0.315, 0.072), (0.375, 0.090), (0.435, 0.108))):
        bpy.ops.mesh.primitive_torus_add(major_radius=radius, minor_radius=0.007, major_segments=16, minor_segments=6,
                                         location=(0, y, 0.075), rotation=(math.radians(90), 0, 0))
        rib = bpy.context.active_object
        rib.name = f"cannon_rib_{index}"
        rib.data.materials.append(materials["bone"])
        parts.append(rib)

    bpy.context.view_layer.update()
    for part in parts:
        if part.data.materials[0] != materials["death_flame"]:
            add_outline(part, outline, thickness=0.004)
    root = bpy.data.objects.new("cannon", None)
    bpy.context.collection.objects.link(root)
    for part in parts:
        world = part.matrix_world.copy()
        part.parent = root
        part.matrix_world = world
    return root


def add_cannon_rig(rig: bpy.types.Object) -> None:
    """A `cannon` bone on the back (child of spine_03), muzzle over the right shoulder; while
    aiming the right forearm switches its IK from the scythe grip to the cannon's grip."""
    left = 1.0 if rig.data.bones["upperarm_l"].head_local.x > 0 else -1.0
    to_rig = rig.matrix_world.inverted()
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    bones = rig.data.edit_bones
    cannon = bones.new("cannon")
    cannon.head = to_rig @ Vector((0.02 * left, 0.15, 1.24))
    barrel = Vector((-0.42 * left, 0.0, 0.91)).normalized()
    cannon.tail = cannon.head + barrel * 0.2
    cannon.align_roll(to_rig.to_3x3() @ Vector((0, 1, 0)))  # the box's top faces away from the back
    cannon.parent = bones["spine_03"]
    cannon.use_deform = False
    bpy.ops.object.mode_set(mode="OBJECT")
    arm = rig.pose.bones["lowerarm_r"]
    aim = arm.constraints.new("IK")
    aim.name = "IK cannon"
    aim.target, aim.subtarget = rig, "cannon"
    aim.pole_target, aim.pole_subtarget = rig, "pole_r"
    aim.chain_count = 2
    aim.pole_angle = arm.constraints["IK scythe"].pole_angle
    aim.influence = 0.0


def key_hands(rig: bpy.types.Object, frame: int, cannon: float = 0.0, holding: float = 1.0) -> None:
    """IK weights, keyed per frame so every action sets them: `cannon` moves the right hand from
    the scythe's grip to the cannon's grip; `holding` 0 lets both hands go to their FK pose."""
    right = rig.pose.bones["lowerarm_r"].constraints
    left = rig.pose.bones["lowerarm_l"].constraints
    right["IK scythe"].influence = (1.0 - cannon) * holding
    right["IK cannon"].influence = cannon * holding
    left["IK scythe"].influence = holding
    for constraint in (right["IK scythe"], right["IK cannon"], left["IK scythe"]):
        constraint.keyframe_insert("influence", frame=frame)


# ---- Animation ----------------------------------------------------------------------------

def key_idle(poser: Poser, frames: int) -> bpy.types.Action:
    action = replace_action(poser.rig, "idle")
    for frame in range(frames + 1):
        phase = frame / frames * math.tau
        poser.clear()
        poser.set("spine_02", "forward", 2.0 + math.sin(phase) * 1.2)
        poser.set("spine_03", "forward", math.sin(phase + 0.6) * 1.0)
        poser.set("neck_01", "forward", 3.0 + math.sin(phase + 1.2) * 1.0)
        # Combat stance from the sheet: the scythe lies level across the body, blade up on the left.
        place_weapon(poser, Vector((0.0, -0.30, 1.0 + 0.008 * math.sin(phase + 0.4))),
                     Vector((poser.left, 0.0, 0.04)), Vector((0.0, 0.0, 1.0)))
        loose_fists(poser, 70)
        poser.key(frame + 1)
        key_hands(poser.rig, frame + 1)
    return action


def key_run(poser: Poser, frames: int) -> bpy.types.Action:
    """A jog: each thigh swings sinusoidally, the knee folds most early in the swing (heel
    toward the seat) and stays nearly straight on contact; feet are kept on the ground."""
    action = replace_action(poser.rig, "run")
    ground = rest_ground(poser.rig)
    for frame in range(frames + 1):
        phase = frame / frames * math.tau
        poser.clear()
        poser.set("spine_01", "forward", 8)
        poser.set("spine_03", "forward", 3)
        poser.set("spine_02", "forward", 2 * math.sin(phase * 2))
        for side, offset in (("l", 0.0), ("r", math.pi)):
            leg_phase = phase + offset
            # Thigh range about -23..+33 degrees: a running stride, so the game's cycle
            # distance stays near the distance the feet cover.
            poser.set(f"thigh_{side}", "forward", 28 * math.sin(leg_phase) + 5)
            # Heel kick peaks while the thigh passes under the body, so the shin never lies
            # flat behind the figure (that read as kneeling from above).
            lift = max(0.0, math.cos(leg_phase + 0.2)) ** 1.5
            poser.set(f"calf_{side}", "back", 16 + 66 * lift)
            poser.set(f"foot_{side}", "down", 12 * lift)
        ground_feet(poser, ground, settle=0.45)
        # Scythe carried diagonally across the chest, blade behind the left shoulder.
        bob = (poser.rig.pose.bones["pelvis"].head - poser.rig.data.bones["pelvis"].head_local).z
        place_weapon(poser, Vector((-0.02 * poser.left, -0.27, 1.12 + bob)),
                     Vector((0.8 * poser.left, 0.0, 0.62)), Vector((0.0, 1.0, 0.0)))
        loose_fists(poser, 70)
        poser.key(frame + 1)
        key_hands(poser.rig, frame + 1)
    return action


# ---- Scythe combo ---------------------------------------------------------------------------
# Each swing is a few key poses over the game's swing progress (ScytheCombat: swing 1 lasts
# 0.205 s and strikes at 30 %, swing 2 0.255 s / 33 %, swing 3 0.42 s / 37 %). The swings differ
# in character: a quick flat cut stepping onto the right foot, a heavier rising backhand
# stepping through onto the left, and the Soul Cleave, which winds high over the left shoulder
# and spins into a low lunge. Feet stand on IK; the game pushes the figure forward during a
# swing (Player._attackImpulse), so planted feet are moved back by that drift and stay put in
# the world. Blade headings are game degrees from the aim, positive clockwise on screen.

SWINGS = {
    # step: (seconds, forward impulse in units/s, swing direction: +1 clockwise)
    1: (0.205, 105.0, 1.0),
    2: (0.255, 132.0, -1.0),
    3: (0.42, 225.0, 1.0),
}

#: Ball of each foot on the floor in the rest pose (rig space), for stances below.
STANCE_WIDTH = 0.21

SWING_KEYS = {
    1: Keys([
        # Loaded: blade back on the left at shoulder height, chest coiled toward it.
        (0.00, dict(blade=-80, reach=0.13, height=1.22, tilt=0.12, roll=12, chest=34, hips=10, lean=4, crouch=0.0, hip_fwd=-0.03, hip_side=0.02)),
        (0.12, dict(blade=-68, reach=0.16, height=1.20, tilt=0.10, chest=30, hips=8)),
        (0.22, dict(blade=-24, reach=0.26, height=1.12, tilt=0.0, roll=4, chest=8, hips=-2, lean=8)),
        # Contact: arms long, the cut flat at waist height, weight arriving on the right foot.
        (0.30, dict(blade=22, reach=0.31, height=1.05, tilt=-0.12, roll=0, chest=-18, hips=-12, lean=11, crouch=0.05, hip_fwd=0.10, hip_side=-0.03)),
        (0.42, dict(blade=56, reach=0.28, height=1.00, tilt=-0.20, chest=-36, hips=-18, lean=12)),
        # Follow-through: the blade carries on past the arc, the chest over-rotates.
        (0.60, dict(blade=74, reach=0.22, height=0.98, tilt=-0.24, roll=-6, chest=-44, hips=-20, lean=12, crouch=0.06, hip_fwd=0.14)),
        (0.82, dict(blade=68, reach=0.20, height=1.02, tilt=-0.20, chest=-38, hips=-17, lean=10)),
        (1.00, dict(blade=62, reach=0.19, height=1.06, tilt=-0.15, roll=0, chest=-32, hips=-14, lean=8, crouch=0.04, hip_fwd=0.13, hip_side=-0.02)),
    ]),
    2: Keys([
        # Coiled low on the right, blade near the floor behind: the rising backhand starts here.
        (0.00, dict(blade=84, reach=0.12, height=0.92, tilt=-0.46, roll=-14, chest=-40, hips=-15, lean=14, crouch=0.07, hip_fwd=0.0, hip_side=-0.03)),
        (0.14, dict(blade=64, reach=0.15, height=0.90, tilt=-0.50, chest=-32, hips=-12, lean=15, crouch=0.08)),
        (0.24, dict(blade=22, reach=0.26, height=0.99, tilt=-0.26, roll=-6, chest=-10, hips=-3, lean=12)),
        # Contact: rising through the aim, the step onto the left foot lands.
        (0.33, dict(blade=-18, reach=0.31, height=1.12, tilt=0.04, roll=6, chest=16, hips=8, lean=8, crouch=0.04, hip_fwd=0.14, hip_side=0.03)),
        (0.46, dict(blade=-52, reach=0.28, height=1.26, tilt=0.30, roll=14, chest=34, hips=16, lean=5)),
        # Follow-through high on the left, the body opening up.
        (0.62, dict(blade=-76, reach=0.21, height=1.33, tilt=0.46, roll=18, chest=42, hips=19, lean=2, crouch=0.02, hip_fwd=0.18)),
        (0.82, dict(blade=-72, reach=0.19, height=1.29, tilt=0.40, chest=38, hips=17, lean=3)),
        (1.00, dict(blade=-68, reach=0.18, height=1.25, tilt=0.36, roll=14, chest=34, hips=15, lean=4, crouch=0.02, hip_fwd=0.17, hip_side=0.02)),
    ]),
    3: Keys([
        # From the backhand's end, the Soul Cleave winds back: blade raised high behind the
        # left shoulder, body sinking and leaning away, the right foot unweighted.
        (0.00, dict(blade=-100, reach=0.15, height=1.26, tilt=0.38, roll=14, chest=38, hips=15, lean=4, crouch=0.02, hip_fwd=0.0, hip_side=0.02)),
        (0.10, dict(blade=-112, reach=0.11, height=1.38, tilt=0.72, roll=24, chest=54, hips=22, lean=-3, crouch=0.06, hip_fwd=-0.06)),
        (0.20, dict(blade=-120, reach=0.10, height=1.42, tilt=0.86, roll=28, chest=62, hips=26, lean=-2, crouch=0.10, hip_fwd=-0.08, hip_side=0.04)),
        # Release: the blade comes down and round while the right foot lunges far forward.
        (0.28, dict(blade=-68, reach=0.22, height=1.12, tilt=0.12, roll=8, chest=30, hips=10, lean=10, crouch=0.10)),
        (0.37, dict(blade=6, reach=0.33, height=0.86, tilt=-0.34, roll=-4, chest=-6, hips=-8, lean=22, crouch=0.17, hip_fwd=0.30, hip_side=-0.04)),
        (0.50, dict(blade=62, reach=0.33, height=0.80, tilt=-0.40, chest=-40, hips=-24, lean=24, crouch=0.19)),
        (0.66, dict(blade=96, reach=0.29, height=0.82, tilt=-0.38, chest=-62, hips=-32, lean=22, crouch=0.18)),
        # Held finish: the blade overshoots behind on the right, then the figure starts to rise.
        (0.80, dict(blade=110, reach=0.24, height=0.86, tilt=-0.34, roll=-8, chest=-68, hips=-35, lean=18, crouch=0.16, hip_fwd=0.40)),
        (1.00, dict(blade=102, reach=0.20, height=0.96, tilt=-0.25, roll=-4, chest=-58, hips=-30, lean=12, crouch=0.10, hip_fwd=0.40, hip_side=-0.03)),
    ]),
}


def swing_feet(step: int, p: float) -> dict[str, tuple[Vector, float, float, float]]:
    """Where each foot is in the world frame of the swing (start position = origin, metres):
    ball position, yaw, heel lift and lift. Swing 1 steps onto the right foot, swing 2 brings
    the left foot through, the Soul Cleave lunges far onto the right."""
    def at(forward: float, side: float) -> Vector:
        # rig space: forward is -Y, the figure's left is +X (Poser.left)
        return Vector((side, -0.144 - forward, 0.0))

    w = STANCE_WIDTH
    if step == 1:
        right, lift_r = step_arc(p, 0.06, 0.26, at(0.0, -w), at(0.22, -w + 0.03), 0.06)
        left = at(-0.02, w)
        return {"r": (right, -14 * window(p, 0.1, 0.3), 0.0, lift_r),
                "l": (left, -10 * window(p, 0.2, 0.6), 16 * window(p, 0.3, 0.6) * (1 - 0.4 * window(p, 0.8, 1.0)), 0.0)}
    if step == 2:
        right = at(0.06, -w + 0.02)
        left, lift_l = step_arc(p, 0.08, 0.30, at(-0.14, w), at(0.30, w - 0.03), 0.07)
        return {"l": (left, 14 * window(p, 0.12, 0.32), 0.0, lift_l),
                "r": (right, 8 * window(p, 0.2, 0.6), 18 * window(p, 0.3, 0.62) * (1 - 0.4 * window(p, 0.8, 1.0)), 0.0)}
    left = at(0.08, w - 0.02)
    right, lift_r = step_arc(p, 0.16, 0.36, at(-0.16, -w), at(0.62, -w + 0.06), 0.09)
    return {"r": (right, -24 * window(p, 0.2, 0.4), 0.0, lift_r),
            "l": (left, -20 * window(p, 0.3, 0.7), 24 * window(p, 0.36, 0.6), 0.0)}


def scythe_placement(poser: Poser, chest: Vector, blade: float, reach: float, height: float, tilt: float,
                     roll: float, direction: float) -> tuple[Vector, Vector, Vector]:
    """The scythe swung around the chest: shaft heading `blade` (game degrees from the aim), hands
    `reach` metres out from the chest at `height`, the shaft climbing by `tilt` per metre outward;
    the blade leads in the swing's direction and rolls up by `roll` degrees."""
    phi = math.radians(-90.0 - blade)
    radial = Vector((math.cos(phi), math.sin(phi), 0.0))
    tangent = Vector((math.sin(phi), -math.cos(phi), 0.0)) * direction
    centre = Vector((chest.x, chest.y, 0.0)) + radial * reach
    centre.z = height
    axis = (radial + Vector((0.0, 0.0, tilt))).normalized()
    r = math.radians(roll)
    up = tangent * math.cos(r) + Vector((0.0, 0.0, 1.0)) * math.sin(r)
    return centre, axis, up


#: Blade points in the scythe's own frame (build_scythe): tip, middle of the crescent and root.
BLADE_TIP = Vector((0.0, SHAFT_TOP + 0.02 - 0.30, 0.73))
BLADE_MID = Vector((0.0, SHAFT_TOP + 0.02 - 0.075, 0.38))
#: The Death Flame core in the iron collar below the blade: the game's flame ribbon streams from it.
BLADE_CORE = Vector((0.024, SHAFT_TOP - 0.03, 0.0))


def pose_body(poser: Poser, keys: Keys, p: float, drift: float, feet: dict, lean_bone_share: float = 1.0) -> None:
    """Hips, spine and head from the channels; feet planted from `feet` (world frame), all moved
    back by the game's forward drift so the planted feet hold still in the world."""
    back = Vector((0.0, drift, 0.0))  # rig +Y is backward
    planter = Feet(poser)
    for side, (ball, yaw, heel, lift) in feet.items():
        planter.plant(side, ball + back, yaw=yaw, heel=heel, lift=lift, knee_out=0.08)
    hips = keys("hips", p)
    chest = keys("chest", p)
    poser.turn("pelvis", hips)
    move_pelvis(poser, Vector((poser.left * keys("hip_side", p), -keys("hip_fwd", p), -keys("crouch", p))) + back)
    poser.set("spine_01", "forward", keys("lean", p) * 0.55 * lean_bone_share)
    poser.set("spine_02", "forward", keys("lean", p) * 0.45 * lean_bone_share)
    twist(poser, chest - hips)
    # The head keeps looking along the aim: it turns back against most of the chest's twist.
    poser.set("neck_01", "forward", 4 - keys("lean", p) * 0.35)
    poser.turn("neck_01", -chest * 0.55)
    poser.turn("head", -chest * 0.2)
    fit_pelvis(poser)
    planter.settle()


def key_swing(poser: Poser, step: int, frames: int, paths: dict) -> bpy.types.Action:
    """Scythe swing `step` (1-3) over `frames` frames; records the blade's path for the game."""
    action = replace_action(poser.rig, f"swing{step}")
    seconds, impulse, direction = SWINGS[step]
    keys = SWING_KEYS[step]
    path = []
    for index in range(frames):
        p = index / (frames - 1)
        poser.clear()
        pose_body(poser, keys, p, impulse_drift(impulse, p * seconds), swing_feet(step, p))
        chest = chest_point(poser)
        place_weapon(poser, *scythe_placement(poser, chest, keys("blade", p), keys("reach", p), keys("height", p),
                                              keys("tilt", p), keys("roll", p), direction))
        loose_fists(poser, 78)
        poser.key(index + 1)
        key_hands(poser.rig, index + 1)
        key_legs(poser.rig, index + 1, 1.0)
        tip = bone_point(poser, "weapon", BLADE_TIP)
        mid = bone_point(poser, "weapon", BLADE_MID)
        drift = impulse_drift(impulse, p * seconds)
        core = bone_point(poser, "weapon", BLADE_CORE)
        path.append({"p": round(p, 4), "tip": stage_coordinates(poser, tip), "mid": stage_coordinates(poser, mid),
                     "core": stage_coordinates(poser, core), "heading": round(keys("blade", p), 2),
                     "drift": round(drift, 4)})
    paths[f"swing{step}"] = path
    return action


def key_swing_return(poser: Poser, step: int, frames: int) -> bpy.types.Action:
    """After a swing, standing still: the figure gathers itself from the swing's last pose back
    into the guard, the trailing foot stepping in. Played on the combo timer after the swing."""
    action = replace_action(poser.rig, f"swing{step}_return")
    seconds, impulse, direction = SWINGS[step]
    keys = SWING_KEYS[step]
    drift = impulse_drift(impulse, seconds)
    end_feet = swing_feet(step, 1.0)
    # Swing end pose, to read the weapon's placement from it.
    poser.clear()
    pose_body(poser, keys, 1.0, drift, end_feet)
    end_weapon = scythe_placement(poser, chest_point(poser), keys("blade", 1.0), keys("reach", 1.0), keys("height", 1.0),
                                  keys("tilt", 1.0), keys("roll", 1.0), direction)
    rest = {side: (Vector((STANCE_WIDTH if side == "l" else -STANCE_WIDTH, -0.144, 0.0)) * Vector((poser.left, 1, 1)), 0.0, 0.0, 0.0)
            for side in ("l", "r")}
    channels = ("hips", "chest", "lean", "crouch", "hip_fwd", "hip_side")
    for index in range(frames):
        p = index / (frames - 1)
        t = smooth(p)
        # The body settles first, the weapon a little after it (overlap).
        body = smooth(min(1.0, p / 0.8))
        weapon = smooth(max(0.0, (p - 0.1) / 0.9))
        # The swing's channels are in the world frame of the swing; here the rig is the frame, so
        # the hips start where the swing left them (moved back by the drift) and come home.
        end = {c: keys(c, 1.0) - (drift if c == "hip_fwd" else 0.0) for c in channels}
        mixed = Keys([(0.0, {c: end[c] * (1 - body) + (2.0 if c == "lean" else 0.0) * body for c in channels})])
        feet = {}
        for side in ("l", "r"):
            ball, yaw, heel, _ = end_feet[side]
            start = ball + Vector((0.0, drift, 0.0))
            target = rest[side][0]
            # The trailing foot (the one further back) steps in on an arc; the other one shuffles.
            trailing = start.y > target.y + 0.02 or (start - target).length > 0.12
            moved, lift = step_arc(p, 0.05, 0.6, start, target, 0.05 if trailing else 0.015)
            feet[side] = (moved, yaw * (1 - t), heel * (1 - window(p, 0.0, 0.4)), lift)
        poser.clear()
        pose_body(poser, mixed, 0.0, 0.0, feet)
        place_weapon(poser, *blend_placement(end_weapon, weapon_idle(poser.left), weapon))
        loose_fists(poser, 78 - 8 * t)
        poser.key(index + 1)
        key_hands(poser.rig, index + 1)
        key_legs(poser.rig, index + 1, 1.0)
    return action


def key_aim(poser: Poser, frames: int) -> bpy.types.Action:
    """Soul Cannon raised in the right hand toward the aim, scythe trailing in the left."""
    action = bpy.data.actions.new("aim")
    poser.rig.animation_data.action = action
    for frame in range(frames + 1):
        phase = frame / frames * math.tau
        poser.clear()
        poser.set("spine_01", "forward", 4)
        poser.turn("spine_02", 12)  # right shoulder forward
        poser.turn("spine_03", 8)
        poser.set("neck_01", "forward", 4)
        right = -poser.left
        place_bone(poser, "cannon", Vector((right * 0.12, -0.42, 1.30 + 0.006 * math.sin(phase))),
                   Vector((0.0, -1.0, 0.0)), Vector((0.0, 0.0, 1.0)))
        place_weapon(poser, Vector((poser.left * 0.25, -0.13, 1.01)),
                     Vector((0.06 * poser.left, 0.55, -0.83)), Vector((poser.left, 0.0, 0.0)))
        loose_fists(poser, 75)
        poser.key(frame + 1)
        key_hands(poser.rig, frame + 1, cannon=1.0)
    return action


def key_aim_move(poser: Poser, frames: int) -> bpy.types.Action:
    """Walking while the Soul Cannon charges (the game slows the player to 58 %): the upper body
    holds the aim of key_aim, the legs take short, grounded steps under it (no flight phase), the
    hips sway a little with each step. A distance clip; the game plays it backward when the player
    backs away, so the feet stay on the floor whichever way he walks."""
    action = replace_action(poser.rig, "aim_move")
    ground = rest_ground(poser.rig)
    for frame in range(frames + 1):
        phase = frame / frames * math.tau
        poser.clear()
        for side, offset in (("l", 0.0), ("r", math.pi)):
            leg = phase + offset
            poser.set(f"thigh_{side}", "forward", 20 * math.sin(leg) + 4)
            lift = max(0.0, math.cos(leg + 0.25)) ** 1.6
            poser.set(f"calf_{side}", "back", 10 + 44 * lift)
            poser.set(f"foot_{side}", "down", 8 * lift)
        ground_feet(poser, ground, settle=1.0)
        poser.turn("pelvis", 4 * math.sin(phase))
        poser.set("spine_01", "forward", 4)
        poser.turn("spine_02", 12 - 4 * math.sin(phase))  # the chest holds the aim against the hips
        poser.turn("spine_03", 8)
        poser.set("neck_01", "forward", 4)
        right = -poser.left
        bob = (poser.rig.pose.bones["pelvis"].head - poser.rig.data.bones["pelvis"].head_local).z
        place_bone(poser, "cannon", Vector((right * 0.12, -0.42, 1.30 + bob * 0.6)),
                   Vector((0.0, -1.0, 0.0)), Vector((0.0, 0.0, 1.0)))
        place_weapon(poser, Vector((poser.left * 0.25, -0.13, 1.01 + bob)),
                     Vector((0.06 * poser.left, 0.55, -0.83)), Vector((poser.left, 0.0, 0.0)))
        loose_fists(poser, 75)
        poser.key(frame + 1)
        key_hands(poser.rig, frame + 1, cannon=1.0)
        key_legs(poser.rig, frame + 1, 0.0)
    return action


# ---- Soul Cannon, dash, hit and death -------------------------------------------------------
# These clips are sampled by the game's own timers (SoulCannon state progress, dash progress,
# hit flash), so each pose lands on the moment the gameplay already defines.

def weapon_idle(left: float) -> tuple[Vector, Vector, Vector]:
    return Vector((0.0, -0.30, 1.0)), Vector((left, 0.0, 0.04)), Vector((0.0, 0.0, 1.0))


def weapon_trailing(left: float) -> tuple[Vector, Vector, Vector]:
    """The scythe in the left hand alone, blade down behind, while the right hand holds the cannon."""
    return Vector((left * 0.25, -0.13, 1.01)), Vector((0.06 * left, 0.55, -0.83)), Vector((left, 0.0, 0.0))


def cannon_aimed(left: float) -> tuple[Vector, Vector, Vector]:
    return Vector((-left * 0.12, -0.42, 1.30)), Vector((0.0, -1.0, 0.0)), Vector((0.0, 0.0, 1.0))


def torso_aim(poser: Poser, t: float) -> None:
    """Between the combat stance (0) and the aiming stance of key_aim (1): right shoulder forward."""
    poser.set("spine_01", "forward", 4 * t)
    poser.set("spine_02", "forward", 2 * (1 - t))
    poser.set("neck_01", "forward", 3 + t)
    poser.turn("spine_02", 12 * t)
    poser.turn("spine_03", 8 * t)


def key_cannon_draw(poser: Poser, frames: int) -> bpy.types.Action:
    """Drawing (0.16 s): the right hand lets go of the scythe, reaches over the shoulder for the
    cannon on the back and swings it forward in an arc into the aim; the scythe drops to trail."""
    action = bpy.data.actions.new("cannon_draw")
    poser.rig.animation_data.action = action
    left = poser.left
    for index in range(frames):
        p = index / (frames - 1)
        reach = ease(min(1.0, p / 0.25))
        pull = ease(max(0.0, (p - 0.25) / 0.75))
        poser.clear()
        torso_aim(poser, 0.15 * reach + 0.85 * pull)
        poser.turn("spine_03", -14 * reach * (1 - pull))  # right shoulder back while it grabs
        back = placement_of(poser, "cannon")
        centre, axis, up = blend_placement(back, cannon_aimed(left), pull)
        centre = centre + Vector((0.0, 0.0, 0.14 * math.sin(math.pi * pull)))  # over the shoulder
        place_bone(poser, "cannon", centre, axis, up)
        place_weapon(poser, *blend_placement(weapon_idle(left), weapon_trailing(left), ease(min(1.0, p / 0.6))))
        loose_fists(poser, 75)
        poser.key(index + 1)
        key_hands(poser.rig, index + 1, cannon=reach)
    return action


def key_cannon_fire(poser: Poser, frames: int) -> bpy.types.Action:
    """Returning (0.28 s after the shot): the recoil throws the muzzle up and the right shoulder
    back, the right foot is driven back half a step and the knees soak up the kick; the figure
    comes forward again, swings the cannon back over the shoulder and takes the scythe again."""
    action = replace_action(poser.rig, "cannon_fire")
    left = poser.left
    w = STANCE_WIDTH
    for index in range(frames):
        p = index / (frames - 1)
        # A sharp kick (peak in the first frames) that decays with a small rebound.
        kick = math.exp(-p * 7.0) * (1.0 - 0.1 * math.sin(p * 12.0))
        settle = smooth(max(0.0, (p - 0.2) / 0.5))
        stow = smooth(max(0.0, (p - 0.42) / 0.58))
        poser.clear()
        feet = Feet(poser)
        back_r = Vector((-w * left, -0.144 + 0.12 * (1 - settle) * smooth(p / 0.12), 0.0))
        feet.plant("r", back_r, yaw=-8 * (1 - settle), lift=0.035 * math.sin(math.pi * min(1.0, p / 0.14)))
        feet.plant("l", Vector((w * left, -0.15, 0.0)), heel=10 * kick)
        move_pelvis(poser, Vector((0.0, 0.06 * kick, -0.07 * kick)))
        poser.set("spine_01", "back", 10 * kick)
        poser.set("spine_02", "back", 16 * kick)
        poser.set("neck_01", "back", 14 * kick)
        torso_aim(poser, (1 - stow) * (1 - 0.8 * kick))
        poser.turn("spine_03", -22 * kick)
        fit_pelvis(poser)
        feet.settle()
        back = placement_of(poser, "cannon")
        aim_centre, aim_axis, aim_up = cannon_aimed(left)
        recoiled = (aim_centre + Vector((0.0, 0.24 * kick, 0.18 * kick)),
                    Vector((0.0, -1.0, 1.05 * kick)), Vector((0.0, 1.05 * kick, 1.0)))
        centre, axis, up = blend_placement(recoiled, back, stow)
        centre = centre + Vector((0.0, 0.0, 0.12 * math.sin(math.pi * stow)))
        place_bone(poser, "cannon", centre, axis, up)
        place_weapon(poser, *blend_placement(weapon_trailing(left), weapon_idle(left), stow))
        loose_fists(poser, 75)
        poser.key(index + 1)
        # The hand stays on the cannon until it sits on the back, then takes the scythe again.
        key_hands(poser.rig, index + 1, cannon=1.0 - ease(max(0.0, (p - 0.8) / 0.2)))
        key_legs(poser.rig, index + 1, 1.0)
    return action


def key_dash(poser: Poser, frames: int) -> bpy.types.Action:
    """Dash (0.14 s): an explosive push-off from the right foot, a long low glide with the left
    knee driving and the right leg trailing, the scythe pulled in low; at the end the left foot
    catches the body and the knees soak up the stop."""
    action = replace_action(poser.rig, "dash")
    left = poser.left
    ground = rest_ground(poser.rig)
    keys = Keys([
        (0.00, dict(lean=22, thigh_l=30, calf_l=44, thigh_r=-16, calf_r=20, foot_r=16, neck=10, carry=0.35)),
        (0.18, dict(lean=34, thigh_l=52, calf_l=62, thigh_r=-38, calf_r=30, foot_r=26, neck=18, carry=0.7)),
        (0.55, dict(lean=30, thigh_l=48, calf_l=54, thigh_r=-36, calf_r=48, foot_r=22, neck=16, carry=1.0)),
        (0.82, dict(lean=22, thigh_l=34, calf_l=44, thigh_r=-18, calf_r=40, foot_r=12, neck=10, carry=0.9)),
        (1.00, dict(lean=16, thigh_l=26, calf_l=48, thigh_r=-4, calf_r=34, foot_r=6, neck=6, carry=0.6)),
    ])
    for index in range(frames):
        p = index / (frames - 1)
        poser.clear()
        poser.set("spine_01", "forward", keys("lean", p) * 0.65)
        poser.set("spine_02", "forward", keys("lean", p) * 0.35)
        poser.set("neck_01", "back", keys("neck", p))
        poser.set("thigh_l", "forward", keys("thigh_l", p))
        poser.set("calf_l", "back", keys("calf_l", p))
        poser.set("thigh_r", "forward", keys("thigh_r", p))
        poser.set("calf_r", "back", keys("calf_r", p))
        poser.set("foot_r", "down", keys("foot_r", p))
        # In the air the body floats a little; on the catch it settles fully onto the floor.
        ground_feet(poser, ground, settle=0.55 + 0.45 * window(p, 0.75, 1.0))
        bob = (poser.rig.pose.bones["pelvis"].head - poser.rig.data.bones["pelvis"].head_local).z
        carry = (Vector((-0.02 * left, -0.27, 1.12)), Vector((0.8 * left, 0.0, 0.62)), Vector((0.0, 1.0, 0.0)))
        low = (Vector((0.04 * left, -0.16, 0.94)), Vector((0.7 * left, 0.62, 0.05)), Vector((0.0, 0.3, 1.0)))
        centre, axis, up = blend_placement(carry, low, keys("carry", p))
        place_weapon(poser, centre + Vector((0.0, 0.0, bob)), axis, up)
        loose_fists(poser, 80)
        poser.key(index + 1)
        key_hands(poser.rig, index + 1)
        key_legs(poser.rig, index + 1, 0.0)
    return action


def key_hit(poser: Poser, frames: int) -> bpy.types.Action:
    """Hit (0.14 s): the blow lands at once, snapping head and chest back and round, the knees
    buckle and the right foot is knocked back; then the guard comes back up, still hunched."""
    action = replace_action(poser.rig, "hit")
    left = poser.left
    w = STANCE_WIDTH
    keys = Keys([
        (0.00, dict(back=16, neck=26, turn=16, crouch=0.07, step=0.10, lift=0.3)),
        (0.25, dict(back=12, neck=18, turn=12, crouch=0.09, step=0.13, lift=0.2)),
        (0.60, dict(back=4, neck=6, turn=5, crouch=0.06, step=0.12, lift=0.06)),
        (1.00, dict(back=0, neck=0, turn=0, crouch=0.03, step=0.11, lift=0.0)),
    ])
    for index in range(frames):
        p = index / (frames - 1)
        poser.clear()
        feet = Feet(poser)
        feet.plant("r", Vector((-w * left, -0.144 + keys("step", p), 0.0)), yaw=-10 * smooth(p * 3), lift=0.03 * math.sin(math.pi * min(1.0, p / 0.3)))
        feet.plant("l", Vector((w * left, -0.15, 0.0)), heel=6 * (1 - p))
        move_pelvis(poser, Vector((0.0, 0.05 * (1 - p), -keys("crouch", p))))
        poser.set("spine_01", "back", keys("back", p) * 0.5)
        poser.set("spine_02", "back", keys("back", p) * 0.7)
        poser.set("neck_01", "back", keys("neck", p))
        poser.turn("spine_03", keys("turn", p))
        poser.turn("neck_01", -keys("turn", p) * 0.4)
        fit_pelvis(poser)
        feet.settle()
        lift = keys("lift", p)
        centre, axis, up = weapon_idle(left)
        place_weapon(poser, centre + Vector((0.0, 0.10 * lift * 2, 0.10 * lift)), axis + Vector((0.0, 0.0, 0.6 * lift)), up + Vector((0.0, 0.6 * lift, 0.0)))
        loose_fists(poser, 75)
        poser.key(index + 1)
        key_hands(poser.rig, index + 1)
        key_legs(poser.rig, index + 1, 1.0)
    return action


def key_retreat(poser: Poser, frames: int) -> bpy.types.Action:
    """Rückstoßsprung (0.22 s, the ability's timer): the figure keeps facing the aim and leaps
    backward: a quick crouch, a push off both feet, airborne with the knees drawn up and the chest
    leaning forward against the flight, then it lands low and absorbs the stop."""
    action = replace_action(poser.rig, "retreat")
    left = poser.left
    ground = rest_ground(poser.rig)
    keys = Keys([
        (0.00, dict(crouch=16, lean=10, air=0.0, tuck=0.0, guard=0.0)),
        (0.14, dict(crouch=24, lean=14, air=0.0, tuck=0.0, guard=0.3)),
        (0.30, dict(crouch=0, lean=18, air=0.14, tuck=0.6, guard=0.8)),
        (0.62, dict(crouch=0, lean=16, air=0.16, tuck=1.0, guard=1.0)),
        (0.84, dict(crouch=26, lean=12, air=0.0, tuck=0.2, guard=0.7)),
        (1.00, dict(crouch=18, lean=8, air=0.0, tuck=0.0, guard=0.4)),
    ])
    for index in range(frames):
        p = index / (frames - 1)
        poser.clear()
        crouch, tuck = keys("crouch", p), keys("tuck", p)
        poser.set("spine_01", "forward", keys("lean", p) * 0.6)
        poser.set("spine_02", "forward", keys("lean", p) * 0.4)
        poser.set("neck_01", "back", keys("lean", p) * 0.5)
        for side, lead in (("l", 1.0), ("r", 0.7)):
            poser.set(f"thigh_{side}", "forward", crouch * 1.1 + 48 * tuck * lead)
            poser.set(f"calf_{side}", "back", crouch * 1.9 + 70 * tuck * lead)
            poser.set(f"foot_{side}", "down", 18 * tuck)
        ground_feet(poser, ground, settle=1.0 if keys("air", p) <= 0.001 else 0.0)
        move_pelvis(poser, Vector((0.0, 0.0, keys("air", p))))
        guard = keys("guard", p)
        centre, axis, up = weapon_idle(left)
        place_weapon(poser, centre + Vector((0.0, -0.06 * guard, 0.08 * guard)), axis + Vector((0.0, 0.0, 0.25 * guard)), up)
        loose_fists(poser, 80)
        poser.key(index + 1)
        key_hands(poser.rig, index + 1)
        key_legs(poser.rig, index + 1, 0.0)
    return action


def key_death(poser: Poser, frames: int) -> bpy.types.Action:
    """Death: the blow throws him back, the knees buckle, the scythe slips from his hands and
    he falls forward onto the floor. Played once from the moment Health reaches zero."""
    action = bpy.data.actions.new("death")
    poser.rig.animation_data.action = action
    left = poser.left
    ground = rest_ground(poser.rig)
    contact = [("foot_l", "head"), ("foot_r", "head"), ("ball_l", "tail"), ("ball_r", "tail"),
               ("calf_l", "head"), ("calf_r", "head"), ("head", "tail"), ("spine_03", "tail"),
               ("hand_l", "tail"), ("hand_r", "tail"), ("pelvis", "head")]
    drop_start = weapon_idle(left)
    drop_end = (Vector((left * 0.38, -0.55, 0.035)), Vector((left * 0.92, -0.38, 0.0)), Vector((0.0, 0.0, 1.0)))
    for index in range(frames):
        p = index / (frames - 1)
        recoil = max(0.0, 1.0 - p / 0.22) ** 1.2
        kneel = ease((p - 0.12) / 0.33)
        fall = ease((p - 0.45) / 0.4) ** 1.6
        poser.clear()
        poser.set("pelvis", "forward", 86 * fall)
        poser.set("spine_01", "back", 8 * recoil)
        poser.set("spine_01", "forward", 22 * kneel * (1 - fall))
        poser.set("spine_02", "forward", 10 * kneel)
        poser.set("neck_01", "back", 14 * recoil)
        poser.set("neck_01", "forward", 20 * kneel * (1 - fall) - 25 * fall)
        poser.set("thigh_r", "forward", 82 * kneel * (1 - fall) + 6 * fall)
        poser.set("calf_r", "back", 112 * kneel * (1 - fall) + 18 * fall)
        poser.set("thigh_l", "forward", 64 * kneel * (1 - fall) - 4 * fall)
        poser.set("calf_l", "back", 70 * kneel * (1 - fall) + 30 * fall)
        holding = 1.0 - ease((p - 0.32) / 0.12)
        if holding < 1.0:
            # Limp arms: hanging while he kneels, then thrown forward onto the floor.
            hang = poser.world(0.35, 1.0, 0.25, "l"), poser.world(0.35, 1.0, 0.25, "r")
            spread = poser.world(0.9, 0.1, 0.55, "l"), poser.world(0.7, 0.2, 0.3, "r")
            for side, h, s in (("l", hang[0], spread[0]), ("r", hang[1], spread[1])):
                direction = (h.lerp(s, fall)).normalized()
                poser.aim(f"upperarm_{side}", direction)
                poser.aim(f"lowerarm_{side}", (direction + Vector((0, 0, -0.2))).normalized())
        if kneel > 0.0 or fall > 0.0:
            ground_points(poser, ground, contact)
        else:
            ground_feet(poser, ground)
        drop = ease((p - 0.36) / 0.3) ** 2
        centre, axis, up = blend_placement(drop_start, drop_end, drop)
        bounce = 0.05 * math.sin(math.pi * min(1.0, max(0.0, (p - 0.66) / 0.12)))
        place_weapon(poser, centre + Vector((0.0, 0.0, bounce)), axis, up)
        loose_fists(poser, 55 + 20 * holding)
        poser.key(index + 1)
        key_hands(poser.rig, index + 1, holding=holding)
    return action


# ---- Main ---------------------------------------------------------------------------------

def main() -> None:
    args = parse(sys.argv)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    human_service, target_service = enable_mpfb()
    macro = target_service.get_default_macro_info_dict()
    macro.update({"gender": 1.0, "age": 0.5, "muscle": 0.45, "weight": 0.38, "proportions": 0.62, "height": 0.56})
    body = human_service.create_human(macro_detail_dict=macro)
    rig = human_service.add_builtin_rig(body, "game_engine")
    rig.name = "figure"

    materials = {"shirt": toon("shirt", SHIRT), "trousers": toon("trousers", TROUSERS), "boots": toon("boots", BOOTS),
                 "skin": toon("skin", SKIN, brush=0.08), "coat": toon("coat", COAT), "hair": toon("hair", HAIR),
                 "leather": toon("leather", LEATHER), "brass": toon("brass", BRASS, brush=0.05), "carmine": toon("carmine", CARMINE, brush=0.05),
                 "wood": toon("wood", WOOD), "iron": toon("iron", IRON, brush=0.05), "steel": toon("steel", STEEL, brush=0.04),
                 "death_flame": flat("death_flame", DEATH_FLAME), "bone": toon("bone", BONE_WHITE, brush=0.06)}
    outline = outline_material()
    ink = flat("ink", INK)

    per_vertex = paint_body(body, rig, materials)
    upper = cloth_shell(body, per_vertex, {"shirt"}, rig, materials["coat"], "coat_upper")
    pelvis_z = (rig.matrix_world @ rig.data.bones["pelvis"].head_local).z
    knee_z = (rig.matrix_world @ rig.data.bones["calf_l"].head_local).z
    skirt = coat_skirt(rig, materials["coat"], waist=pelvis_z + 0.08, knee=knee_z - 0.06)
    hair = build_hair(rig, materials["hair"])
    collar = build_collar(rig, materials["coat"])
    extras = build_strap_and_compass(rig, materials["leather"], materials["brass"], materials["carmine"])
    extras += build_belt(rig, pelvis_z + 0.08, materials["leather"], materials["brass"])
    extras += build_face(body, rig, ink, materials["hair"])
    for obj in (body, upper, skirt, hair, collar):
        add_outline(obj, outline)

    # MPFB humans face -Y; measure it from the toes rather than assume.
    toe = (rig.matrix_world @ rig.data.bones["ball_l"].tail_local) - (rig.matrix_world @ rig.data.bones["foot_l"].head_local)
    forward = Vector((0, toe.y, 0)).normalized()
    add_weapon_rig(rig)
    scythe = build_scythe(materials, outline)
    weapon_bone = rig.data.bones["weapon"]
    scythe.matrix_world = rig.matrix_world @ weapon_bone.matrix_local
    attach(scythe, rig, "weapon")
    add_cannon_rig(rig)
    cannon = build_cannon(materials, outline)
    cannon.matrix_world = rig.matrix_world @ rig.data.bones["cannon"].matrix_local
    attach(cannon, rig, "cannon")
    poser = Poser(rig, forward)
    poser.measure_rest([f"{bone}_{side}" for bone in FINGERS + THUMB for side in ("l", "r")], "down")
    add_leg_ik(rig)
    paths: dict = {}
    actions = [key_idle(poser, LOOP_FRAMES["idle"]), key_run(poser, LOOP_FRAMES["run"]), key_aim(poser, 4),
               key_cannon_draw(poser, 5), key_death(poser, 16)]
    actions += [combat_action(poser, name, paths) for name in COMBAT_FRAMES]
    for action in actions:
        action.use_fake_user = True
    if args.paths:
        write_paths(args.paths, paths)
    idle = actions[0]
    rig.animation_data.action = idle
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, LOOP_FRAMES["idle"]
    bpy.context.scene.render.fps = 24

    args.out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(args.out.resolve()))
    print(f"BUILD_PLAYER_DONE {args.out} forward={tuple(round(v, 2) for v in forward)} height={rig.dimensions.z:.2f}")


#: Frames of the loops. Both are keyed per frame from a phase, so the count only sets how densely
#: the same motion is sampled: 24 keeps the run smooth at the game's 60 Hz (it advances with the
#: distance walked, one cycle per 180 units) and lets the idle breathe without visible steps.
LOOP_FRAMES = {"idle": 24, "run": 24}

#: Frames of every combat action (key poses are written over progress, so these only set the
#: sampling); the render and pack steps take the same counts.
COMBAT_FRAMES = {
    "swing1": 14, "swing2": 16, "swing3": 26,
    "swing1_return": 8, "swing2_return": 8, "swing3_return": 10,
    "dash": 8, "hit": 6, "cannon_fire": 10, "retreat": 8, "aim_move": 12,
}


def combat_action(poser: Poser, name: str, paths: dict) -> bpy.types.Action:
    frames = COMBAT_FRAMES[name]
    if name.startswith("swing") and name.endswith("_return"):
        return key_swing_return(poser, int(name[5]), frames)
    if name.startswith("swing"):
        return key_swing(poser, int(name[5]), frames, paths)
    return {"dash": key_dash, "hit": key_hit, "cannon_fire": key_cannon_fire, "retreat": key_retreat,
            "aim_move": key_aim_move}[name](poser, frames)


def write_paths(path: Path, paths: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    existing = json.loads(path.read_text()) if path.exists() else {}
    existing.update(paths)
    path.write_text(json.dumps(existing, indent=1) + "\n")


def rekey(args: argparse.Namespace) -> None:
    """Key the named combat actions again on the opened figure; the body, clothes and the other
    actions stay exactly as built."""
    rig = bpy.data.objects["figure"]
    toe = (rig.matrix_world @ rig.data.bones["ball_l"].tail_local) - (rig.matrix_world @ rig.data.bones["foot_l"].head_local)
    poser = Poser(rig, Vector((0, toe.y, 0)).normalized())
    add_leg_ik(rig)
    measure_axes(poser, [(f"{bone}_{side}", "down") for bone in FINGERS + THUMB for side in ("l", "r")]
                 + [(bone, way) for bone in ("pelvis", "spine_01", "spine_02", "spine_03", "neck_01")
                    for way in ("forward", "back")]
                 + [(f"{bone}_{side}", way) for bone in ("thigh", "calf", "foot") for side in ("l", "r")
                    for way in ("forward", "back", "down")])
    paths: dict = {}
    names = [name.strip() for name in args.rekey.split(",") if name.strip()]
    for name in names:
        if name in LOOP_FRAMES:
            {"idle": key_idle, "run": key_run}[name](poser, LOOP_FRAMES[name])
            print(f"REKEYED {name} frames={LOOP_FRAMES[name]}")
            continue
        combat_action(poser, name, paths)
        print(f"REKEYED {name} frames={COMBAT_FRAMES[name]}")
    if args.paths and paths:
        write_paths(args.paths, paths)
    finish_rekey(rig, "idle", args.out.resolve())
    print(f"REKEY_PLAYER_DONE {args.out} actions={','.join(names)}")


if __name__ == "__main__":
    arguments = parse(sys.argv)
    if arguments.rekey:
        rekey(arguments)
    else:
        main()
