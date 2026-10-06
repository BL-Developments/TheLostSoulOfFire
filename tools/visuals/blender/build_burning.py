"""The Burning (enemy.burning) as a rigged figure with its game actions.

    blender -b -P tools/visuals/blender/build_burning.py -- --out art/production/candidates/enemy.burning/burning.blend

Follows docs/current/characters/burning.md: compact and crouched, shoulders drawn up, fists close
to the body, always about to break into a run. A charred shell like burnt-out coal; violet Death
Flame light in its cracks and as pointed tongues from shoulders and back, never orange. Two
glowing cracks for eyes; scraps of burnt cloth, a belt and the remains of boots.

The glow of cracks and flames is a custom property "glow" on the bone "flames" (keyed per frame
in every action, read by the materials through drivers); the flames hang from that bone, so its
scale makes them flare or sink. Actions: idle, move, telegraph, charge, recover, hit, death.
"""
from __future__ import annotations

import argparse
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from figure_kit import (  # noqa: E402
    FINGERS, THUMB, Poser, add_outline, attach, cloth_shell, dominant_bone, ease, enable_mpfb, flat, ground_feet,
    ground_points, loose_fists, outline_material, rest_ground, shaped_coordinates, toon)
from combat_kit import (  # noqa: E402
    Feet, Keys, add_leg_ik, finish_rekey, fit_pelvis, key_legs, measure_axes, move_pelvis, replace_action, smooth,
    step_arc, window)

CHAR = (0.030, 0.026, 0.032)
EMBER = (0.55, 0.22, 1.0)
CLOTH = (0.05, 0.045, 0.05)
LEATHER = (0.06, 0.04, 0.035)


def parse(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(prog="build_burning.py")
    parser.add_argument("--rekey", help="comma-separated actions to key again on the opened .blend (no rebuild)")
    parser.add_argument("--out", type=Path, required=True)
    return parser.parse_args(argv[argv.index("--") + 1:] if "--" in argv else [])


# ---- Materials ------------------------------------------------------------------------------

def glow_driver(socket, rig: bpy.types.Object, scale: float = 1.0) -> None:
    """Drive a socket's value from the rig's pose.bones["flames"]["glow"]."""
    driver = socket.driver_add("default_value").driver
    driver.type = "SCRIPTED"
    variable = driver.variables.new()
    variable.name = "glow"
    variable.targets[0].id = rig
    variable.targets[0].data_path = 'pose.bones["flames"]["glow"]'
    driver.expression = f"glow * {scale}"


def charred(rig: bpy.types.Object) -> bpy.types.Material:
    """The toon shell of burnt-out coal plus violet light in a network of cracks (patchy, so some
    areas are whole), its strength driven by the glow property."""
    material = toon("char", CHAR, brush=0.18)
    nodes, links = material.node_tree.nodes, material.node_tree.links
    emission = next(n for n in nodes if n.type == "EMISSION")
    output = next(n for n in nodes if n.type == "OUTPUT_MATERIAL")
    coords = nodes.new("ShaderNodeTexCoord")
    cracks = nodes.new("ShaderNodeTexVoronoi")
    cracks.feature = "DISTANCE_TO_EDGE"
    cracks.inputs["Scale"].default_value = 7.0
    links.new(coords.outputs["Object"], cracks.inputs["Vector"])
    edge = nodes.new("ShaderNodeValToRGB")
    edge.color_ramp.elements[0].position, edge.color_ramp.elements[0].color = 0.0, (1, 1, 1, 1)
    edge.color_ramp.elements[1].position, edge.color_ramp.elements[1].color = 0.022, (0, 0, 0, 1)
    links.new(cracks.outputs["Distance"], edge.inputs["Fac"])
    patches = nodes.new("ShaderNodeTexNoise")
    patches.inputs["Scale"].default_value = 3.0
    links.new(coords.outputs["Object"], patches.inputs["Vector"])
    patch_ramp = nodes.new("ShaderNodeValToRGB")
    patch_ramp.color_ramp.elements[0].position, patch_ramp.color_ramp.elements[0].color = 0.5, (0, 0, 0, 1)
    patch_ramp.color_ramp.elements[1].position, patch_ramp.color_ramp.elements[1].color = 0.64, (1, 1, 1, 1)
    links.new(patches.outputs["Fac"], patch_ramp.inputs["Fac"])
    mask = nodes.new("ShaderNodeMath")
    mask.operation = "MULTIPLY"
    links.new(edge.outputs["Color"], mask.inputs[0])
    links.new(patch_ramp.outputs["Color"], mask.inputs[1])
    strength = nodes.new("ShaderNodeValue")
    glow_driver(strength.outputs[0], rig, 3.0)
    amount = nodes.new("ShaderNodeMath")
    amount.operation = "MULTIPLY"
    links.new(mask.outputs[0], amount.inputs[0])
    links.new(strength.outputs[0], amount.inputs[1])
    light = nodes.new("ShaderNodeEmission")
    light.inputs["Color"].default_value = (*EMBER, 1)
    links.new(amount.outputs[0], light.inputs["Strength"])
    add = nodes.new("ShaderNodeAddShader")
    links.new(emission.outputs[0], add.inputs[0])
    links.new(light.outputs[0], add.inputs[1])
    links.new(add.outputs[0], output.inputs["Surface"])
    return material


def flame_material(rig: bpy.types.Object) -> bpy.types.Material:
    """Death Flame: near-white at the root, bright violet, deep violet at the tip (S1)."""
    material = bpy.data.materials.new("flame")
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    nodes.clear()
    coords = nodes.new("ShaderNodeTexCoord")
    separate = nodes.new("ShaderNodeSeparateXYZ")
    links.new(coords.outputs["Generated"], separate.inputs["Vector"])
    ramp = nodes.new("ShaderNodeValToRGB")
    elements = ramp.color_ramp.elements
    elements[0].position, elements[0].color = 0.0, (0.78, 0.55, 1.0, 1)
    elements[1].position, elements[1].color = 1.0, (0.16, 0.04, 0.34, 1)
    middle = elements.new(0.3)
    middle.color = (*EMBER, 1)
    links.new(separate.outputs["Z"], ramp.inputs["Fac"])
    emission = nodes.new("ShaderNodeEmission")
    links.new(ramp.outputs["Color"], emission.inputs["Color"])
    strength = nodes.new("ShaderNodeValue")
    glow_driver(strength.outputs[0], rig, 0.62)
    links.new(strength.outputs[0], emission.inputs["Strength"])
    output = nodes.new("ShaderNodeOutputMaterial")
    links.new(emission.outputs[0], output.inputs["Surface"])
    return material


# ---- Body -----------------------------------------------------------------------------------

def zone(bone: str) -> str:
    if bone.startswith(("foot_", "ball_")) or bone.startswith("calf_"):
        return "boots"
    if bone in ("pelvis",) or bone.startswith("thigh_"):
        return "hips"
    return "skin"


def paint_body(body, rig, materials) -> list[str]:
    body.data.materials.clear()
    for name in ("char", "boots"):
        body.data.materials.append(materials[name])
    per_vertex = [zone(bone) for bone in dominant_bone(body, {bone.name for bone in rig.data.bones})]
    for polygon in body.data.polygons:
        boots = sum(per_vertex[i] == "boots" for i in polygon.vertices)
        polygon.material_index = 1 if boots * 2 > len(polygon.vertices) else 0
    return per_vertex


def tatter(obj: bpy.types.Object, share: float, seed: int) -> None:
    """Burn holes into a cloth shell: remove a share of its faces, in clumps."""
    rng = random.Random(seed)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    centres = [face.calc_center_median() for face in rng.sample(list(bm.faces), max(1, int(len(bm.faces) * share / 12)))]
    remove = [face for face in bm.faces if any((face.calc_center_median() - c).length < rng.uniform(0.03, 0.07) for c in centres)]
    remove += [face for face in bm.faces if face.calc_center_median().z < 0.62 and rng.random() < 0.5]
    bmesh.ops.delete(bm, geom=list(set(remove)), context="FACES")
    bm.to_mesh(obj.data)
    bm.free()


def add_flames_bone(rig: bpy.types.Object) -> None:
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    bones = rig.data.edit_bones
    spine = bones["spine_03"]
    flames = bones.new("flames")
    flames.head, flames.tail, flames.roll = spine.head.copy(), spine.tail.copy(), spine.roll
    flames.parent = spine
    flames.use_deform = False
    bpy.ops.object.mode_set(mode="OBJECT")
    rig.pose.bones["flames"]["glow"] = 1.0


def build_flames(rig: bpy.types.Object, material: bpy.types.Material) -> list[bpy.types.Object]:
    """Pointed, partly angular tongues from shoulders and upper back, some sideways or backward
    (S3, S4); the figure faces -Y, so +Y is its back."""
    shoulders = {side: rig.matrix_world @ rig.data.bones[f"upperarm_{side}"].head_local for side in ("l", "r")}
    back = rig.matrix_world @ rig.data.bones["spine_03"].tail_local
    rng = random.Random(23)
    tongues = []
    spots = [(shoulders["l"], (0.5, 0.3, 1.0)), (shoulders["r"], (-0.5, 0.3, 1.0)), (shoulders["l"], (0.9, 0.5, 0.6)),
             (shoulders["r"], (-0.9, 0.4, 0.6)), (back + Vector((0.06, 0.12, -0.05)), (0.1, 0.9, 0.8)),
             (back + Vector((-0.08, 0.12, -0.12)), (-0.2, 1.0, 0.5)), (back + Vector((0.0, 0.13, 0.04)), (0.0, 0.5, 1.0)),
             (back + Vector((0.1, 0.1, -0.25)), (0.6, 0.9, 0.2))]
    for index, (root, direction) in enumerate(spots):
        length = rng.uniform(0.2, 0.42)
        axis = Vector(direction).normalized()
        bpy.ops.mesh.primitive_cone_add(vertices=5, radius1=rng.uniform(0.03, 0.055), radius2=0.0, depth=length,
                                        location=root + axis * (length / 2 + 0.02))
        cone = bpy.context.active_object
        cone.name = f"flame_{index}"
        cone.rotation_euler = axis.to_track_quat("Z", "Y").to_euler()
        cone.data.materials.append(material)
        tongues.append(cone)
    return tongues


def build_face(body, rig, ember: bpy.types.Material) -> list[bpy.types.Object]:
    """Two glowing cracks where the eyes were: narrow, slanted slits."""
    shaped = shaped_coordinates(body)
    parts = []
    for side in ("l", "r"):
        group = body.vertex_groups.get(f"helper-{side}-eye")
        points = [body.matrix_world @ shaped[v.index] for v in body.data.vertices
                  if group and any(g.group == group.index and g.weight > 0.5 for g in v.groups)]
        centre = sum(points, Vector()) / len(points)
        front = min(p.y for p in points)
        bpy.ops.mesh.primitive_cube_add(size=1, location=(centre.x, front - 0.004, centre.z))
        slit = bpy.context.active_object
        slit.name = f"eye_{side}"
        slit.scale = (0.024, 0.006, 0.006)
        slit.rotation_euler = (0, math.radians(18 if centre.x > 0 else -18), 0)
        slit.data.materials.append(ember)
        parts.append(slit)
    for part in parts:
        attach(part, rig, "head")
    return parts


def build_belt(rig, leather, iron) -> list[bpy.types.Object]:
    pelvis = rig.matrix_world @ rig.data.bones["pelvis"].head_local
    bpy.ops.mesh.primitive_torus_add(major_radius=0.16, minor_radius=0.022, major_segments=24, minor_segments=6,
                                     location=(0, pelvis.y, pelvis.z + 0.1))
    belt = bpy.context.active_object
    belt.name = "belt"
    belt.scale = (1.12, 0.86, 1.0)
    belt.data.materials.append(leather)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, pelvis.y - 0.14, pelvis.z + 0.1))
    buckle = bpy.context.active_object
    buckle.name = "buckle"
    buckle.scale = (0.05, 0.012, 0.04)
    buckle.data.materials.append(iron)
    for part in (belt, buckle):
        attach(part, rig, "pelvis")
    return [belt, buckle]


# ---- Poses ----------------------------------------------------------------------------------

def stance(poser: Poser, crouch: float = 0.0, lean: float = 0.0) -> None:
    """Hunched and coiled: back bent, head pushed forward, shoulders up, knees bent, on the balls of the feet."""
    poser.set("spine_01", "forward", 12 + lean * 0.5)
    poser.set("spine_02", "forward", 10 + lean * 0.3)
    poser.set("spine_03", "forward", 6 + lean * 0.2)
    poser.set("neck_01", "forward", 14)
    poser.set("head", "back", 20)
    for side in ("l", "r"):
        poser.set(f"clavicle_{side}", "up", 14)
        poser.set(f"thigh_{side}", "forward", 24 + crouch)
        poser.set(f"calf_{side}", "back", 40 + crouch * 1.5)
        poser.set(f"foot_{side}", "down", 8)


def fists_close(poser: Poser, forward: float = 0.35, out: float = 0.25, bend: float = 0.9) -> None:
    for side in ("l", "r"):
        poser.aim(f"upperarm_{side}", poser.world(forward * 0.4, 1.0, out, side))
        poser.aim(f"lowerarm_{side}", poser.world(bend, 0.35, out * 0.3, side))
    loose_fists(poser, 95)


def key_glow(poser: Poser, frame: int, glow: float, flame_scale: float = 1.0) -> None:
    bone = poser.rig.pose.bones["flames"]
    bone["glow"] = glow
    bone.keyframe_insert('["glow"]', frame=frame)
    bone.scale = (flame_scale, flame_scale, flame_scale)
    bone.keyframe_insert("scale", frame=frame)


def key_idle(poser: Poser, frames: int) -> bpy.types.Action:
    action = bpy.data.actions.new("idle")
    poser.rig.animation_data_create().action = action
    ground = rest_ground(poser.rig)
    for frame in range(frames + 1):
        phase = frame / frames * math.tau
        poser.clear()
        stance(poser, crouch=5 * math.sin(phase * 2))
        poser.turn("spine_02", 4 * math.sin(phase))
        ground_feet(poser, ground)
        fists_close(poser, forward=0.35 + 0.05 * math.sin(phase * 2), bend=0.9)
        poser.key(frame + 1)
        key_glow(poser, frame + 1, 1.0 + 0.3 * math.sin(phase * 2), 1.0 + 0.08 * math.sin(phase * 3))
    return action


def key_move(poser: Poser, frames: int) -> bpy.types.Action:
    """A low, jerky stalk: short quick strides, shoulders working, head steady and forward."""
    action = bpy.data.actions.new("move")
    poser.rig.animation_data.action = action
    ground = rest_ground(poser.rig)
    for frame in range(frames + 1):
        phase = frame / frames * math.tau
        poser.clear()
        stance(poser, crouch=6, lean=8)
        for side, offset in (("l", 0.0), ("r", math.pi)):
            leg = phase + offset
            swing = math.sin(leg) + 0.25 * math.sin(2 * leg)
            poser.set(f"thigh_{side}", "forward", 22 * swing + 6)
            lift = max(0.0, math.cos(leg + 0.3)) ** 1.6
            poser.set(f"calf_{side}", "back", 18 + 55 * lift)
        poser.turn("spine_02", 7 * math.sin(phase))
        ground_feet(poser, ground, settle=0.6)
        for side, offset in (("l", math.pi), ("r", 0.0)):
            pump = math.sin(phase + offset)
            poser.aim(f"upperarm_{side}", poser.world(0.2 + 0.35 * pump, 1.0, 0.25, side))
            poser.aim(f"lowerarm_{side}", poser.world(1.0, 0.25 - 0.2 * pump, 0.05, side))
        loose_fists(poser, 95)
        poser.key(frame + 1)
        key_glow(poser, frame + 1, 1.2, 1.0)
    return action


TELEGRAPH_KEYS = Keys([
    # A sharp breath: it rears up a little, chest open, and every flame flares.
    (0.00, dict(crouch=4, lean=0, rear=0.0, head=0, fists=0.0, glow=1.2, flame=1.0, sink=0.0, scrape=0.0)),
    (0.14, dict(crouch=-6, lean=-8, rear=0.0, head=-8, fists=0.15, glow=2.6, flame=1.35, sink=-0.03, scrape=0.0)),
    # It drops into a sprinter's crouch, the right foot pawing back once, fists drawn back.
    (0.40, dict(crouch=14, lean=12, rear=0.6, head=6, fists=0.6, glow=2.4, flame=1.2, sink=0.05, scrape=1.0)),
    (0.62, dict(crouch=20, lean=18, rear=0.8, head=10, fists=0.85, glow=3.0, flame=1.35, sink=0.08, scrape=0.0)),
    # Loaded: the lowest point, trembling, flames streaming back.
    (1.00, dict(crouch=26, lean=24, rear=1.0, head=12, fists=1.0, glow=3.8, flame=1.5, sink=0.11, scrape=0.0)),
])


def burning_arms(poser: Poser, fists: float) -> None:
    """Fists from the ready guard to drawn back past the hips."""
    for side in ("l", "r"):
        poser.aim(f"upperarm_{side}", poser.world(0.14 - 0.75 * fists, 1.0, 0.28 + 0.12 * fists, side))
        poser.aim(f"lowerarm_{side}", poser.world(0.9 - 0.6 * fists, 0.35 + 0.4 * fists, 0.05, side))
    loose_fists(poser, 100)


def key_telegraph(poser: Poser, frames: int) -> bpy.types.Action:
    """Standstill before the run (0.62 s): a sharp breath that flares every flame, then it drops
    into a sprinter's crouch, paws the floor back once with the right foot and coils, fists
    drawn, trembling at the lowest point; the cracks burn brighter all the while."""
    action = replace_action(poser.rig, "telegraph")
    keys = TELEGRAPH_KEYS
    w = 0.179
    for frame in range(frames):
        p = frame / (frames - 1)
        poser.clear()
        stance(poser, crouch=keys("crouch", p), lean=keys("lean", p))
        poser.set("head", "forward", keys("head", p))
        planter = Feet(poser)
        # The rear foot paws back across the floor, then sets far back like a starting block.
        rear_y = -0.128 + 0.10 * keys("rear", p)
        paw = 0.04 * math.sin(math.pi * min(1.0, max(0.0, (p - 0.22) / 0.28)))
        planter.plant("r", Vector((-w * poser.left, rear_y, 0.0)), yaw=6 * keys("rear", p), heel=34 * keys("rear", p), lift=paw)
        planter.plant("l", Vector((w * poser.left, -0.16, 0.0)), yaw=-6 * keys("rear", p), heel=0.0)
        move_pelvis(poser, Vector((0.0, -0.03 * keys("rear", p), -keys("sink", p))))
        fit_pelvis(poser, reach=0.95)
        planter.settle()
        burning_arms(poser, keys("fists", p))
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 1.0)
        tremble = 0.25 * window(p, 0.6, 1.0) * math.sin(frame * 2.7)
        key_glow(poser, frame + 1, keys("glow", p) + tremble, keys("flame", p) + 0.05 * tremble)
    return action


def key_charge(poser: Poser, frames: int) -> bpy.types.Action:
    """The explosive sprint (loop): leaning hard into it, long strides with a real flight phase,
    fists pumping low, head down like a ram, the flames streaming back."""
    action = replace_action(poser.rig, "charge")
    ground = rest_ground(poser.rig)
    for frame in range(frames + 1):
        phase = frame / frames * math.tau
        poser.clear()
        stance(poser, crouch=0, lean=30 + 3 * math.sin(phase * 2))
        poser.set("spine_01", "forward", 16)
        poser.set("head", "forward", 8)
        poser.turn("spine_02", 6 * math.sin(phase))
        for side, offset in (("l", 0.0), ("r", math.pi)):
            leg = phase + offset
            poser.set(f"thigh_{side}", "forward", 46 * math.sin(leg) + 12)
            lift = max(0.0, math.cos(leg + 0.25)) ** 1.4
            poser.set(f"calf_{side}", "back", 26 + 88 * lift)
        ground_feet(poser, ground, settle=0.3)
        for side, offset in (("l", math.pi), ("r", 0.0)):
            pump = math.sin(phase + offset)
            poser.aim(f"upperarm_{side}", poser.world(-0.55 + 0.45 * pump, 0.8, 0.25, side))
            poser.aim(f"lowerarm_{side}", poser.world(-0.3 + 0.8 * pump, 0.6, 0.12, side))
        loose_fists(poser, 100)
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 0.0)
        key_glow(poser, frame + 1, 3.6 + 0.4 * math.sin(phase * 2), 1.4 + 0.05 * math.sin(phase * 2))
    return action


def key_recover(poser: Poser, frames: int) -> bpy.types.Action:
    """After a missed or broken run (1.0 s): it brakes, skidding on its heels and leaning back,
    doubles over heaving with its hands on its knees, then straightens; the glow sinks."""
    action = replace_action(poser.rig, "recover")
    keys = Keys([
        (0.00, dict(crouch=18, lean=-16, back=10, hands=0.0, glow=3.0, flame=1.3, skid=1.0)),
        (0.15, dict(crouch=22, lean=-10, back=6, hands=0.3, glow=2.6, flame=1.25, skid=0.6)),
        (0.40, dict(crouch=20, lean=26, back=0, hands=1.0, glow=1.9, flame=1.1, skid=0.0)),
        (0.70, dict(crouch=14, lean=22, back=0, hands=1.0, glow=1.4, flame=1.05, skid=0.0)),
        (1.00, dict(crouch=4, lean=2, back=0, hands=0.0, glow=1.0, flame=1.0, skid=0.0)),
    ])
    w = 0.179
    for frame in range(frames):
        p = frame / (frames - 1)
        heave = math.sin(p * math.tau * 2.5) * window(p, 0.3, 0.45) * (1 - window(p, 0.75, 1.0))
        poser.clear()
        stance(poser, crouch=keys("crouch", p), lean=keys("lean", p) + 5 * heave)
        poser.set("spine_02", "back", keys("back", p))
        planter = Feet(poser)
        skid = keys("skid", p)
        # Braking: the front foot ahead on its heel, the rear foot dragging; then feet together.
        planter.plant("l", Vector((w * poser.left, -0.128 - 0.22 * skid, 0.0)), heel=-18 * skid)
        planter.plant("r", Vector((-w * poser.left, -0.128 + 0.06 * skid, 0.0)), heel=10 * skid)
        move_pelvis(poser, Vector((0.0, 0.04 * skid, -0.03 * keys("hands", p))))
        fit_pelvis(poser, reach=0.95)
        planter.settle()
        hands = keys("hands", p)
        for side in ("l", "r"):
            # Arms thrown back for balance while braking, then hands braced on the knees.
            braced = poser.world(0.55, 1.0, 0.1, side)
            thrown = poser.world(-0.4, 0.7, 0.55, side)
            ready = poser.world(0.14, 1.0, 0.28, side)
            upper = thrown.lerp(braced, hands).lerp(ready, window(p, 0.75, 1.0)).normalized()
            poser.aim(f"upperarm_{side}", upper)
            poser.aim(f"lowerarm_{side}", (upper + poser.world(0.6, 0.2, -0.2, side) * (1 - hands * 0.6)).normalized())
        loose_fists(poser, 90)
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 1.0)
        key_glow(poser, frame + 1, keys("glow", p) + 0.2 * heave, keys("flame", p))
    return action


def key_hit(poser: Poser, frames: int) -> bpy.types.Action:
    """Hit: knocked back off its coil, embers jumping out of the cracks, then it hunches again."""
    action = replace_action(poser.rig, "hit")
    ground = rest_ground(poser.rig)
    for frame in range(frames):
        p = frame / (frames - 1)
        k = math.exp(-p * 3.0) * (1.0 - p) ** 0.6
        poser.clear()
        stance(poser, crouch=8 * k, lean=-22 * k)
        poser.set("head", "back", 18 * k)
        poser.turn("spine_03", 14 * k)
        ground_feet(poser, ground)
        fists_close(poser, forward=0.35 - 0.4 * k, out=0.25 + 0.35 * k)
        poser.key(frame + 1)
        key_legs(poser.rig, frame + 1, 0.0)
        key_glow(poser, frame + 1, 1.0 + 2.2 * k, 1.0 + 0.3 * k)
    return action


def key_death(poser: Poser, frames: int) -> bpy.types.Action:
    """Dying (0.58 s): it drops to its knees, the flames sink, the shell slumps forward; the game
    dissolves the last pose and the soul appears."""
    action = bpy.data.actions.new("death")
    poser.rig.animation_data.action = action
    ground = rest_ground(poser.rig)
    contact = [("foot_l", "head"), ("foot_r", "head"), ("ball_l", "tail"), ("ball_r", "tail"),
               ("calf_l", "head"), ("calf_r", "head"), ("hand_l", "tail"), ("hand_r", "tail")]
    for frame in range(frames):
        t = frame / (frames - 1)
        kneel = ease(t / 0.6)
        slump = ease((t - 0.35) / 0.65)
        poser.clear()
        stance(poser, crouch=0, lean=-10 * (1 - kneel) + 30 * slump)
        poser.set("head", "forward", 30 * slump)
        for side in ("l", "r"):
            poser.set(f"thigh_{side}", "forward", 60 * kneel)
            poser.set(f"calf_{side}", "back", 70 * kneel)
        if kneel > 0:
            ground_points(poser, ground, contact)
        else:
            ground_feet(poser, ground)
        for side in ("l", "r"):
            poser.aim(f"upperarm_{side}", poser.world(0.3 * slump, 1.0, 0.25, side))
            poser.aim(f"lowerarm_{side}", poser.world(0.4 * slump + 0.1, 1.0, 0.1, side))
        loose_fists(poser, 60)
        poser.key(frame + 1)
        key_glow(poser, frame + 1, 1.6 * (1 - t) + 0.15, 1.0 - 0.85 * t)
    return action


# ---- Main -----------------------------------------------------------------------------------

def main() -> None:
    args = parse(sys.argv)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    human_service, target_service = enable_mpfb()
    macro = target_service.get_default_macro_info_dict()
    macro.update({"gender": 0.65, "age": 0.45, "muscle": 0.78, "weight": 0.55, "proportions": 0.42, "height": 0.32})
    body = human_service.create_human(macro_detail_dict=macro)
    rig = human_service.add_builtin_rig(body, "game_engine")
    rig.name = "figure"
    add_flames_bone(rig)

    materials = {"char": charred(rig), "boots": toon("boots", LEATHER), "cloth": toon("cloth", CLOTH, brush=0.2),
                 "leather": toon("leather", LEATHER), "iron": toon("iron", (0.07, 0.07, 0.08), brush=0.05)}
    flame = flame_material(rig)
    ember = flat("ember", (0.85, 0.7, 1.0))
    outline = outline_material()

    per_vertex = paint_body(body, rig, materials)
    scraps = cloth_shell(body, per_vertex, {"hips"}, rig, materials["cloth"], "scraps", push=0.014)
    tatter(scraps, 0.45, seed=5)
    parts = build_belt(rig, materials["leather"], materials["iron"])
    parts += build_face(body, rig, ember)
    tongues = build_flames(rig, flame)
    for tongue in tongues:
        attach(tongue, rig, "flames")
    for obj in (body, scraps):
        add_outline(obj, outline)

    toe = (rig.matrix_world @ rig.data.bones["ball_l"].tail_local) - (rig.matrix_world @ rig.data.bones["foot_l"].head_local)
    poser = Poser(rig, Vector((0, toe.y, 0)).normalized())
    poser.measure_rest([f"{bone}_{side}" for bone in FINGERS + THUMB for side in ("l", "r")], "down")
    add_leg_ik(rig)
    actions = [key_idle(poser, 8), key_move(poser, 10), key_death(poser, 8)]
    actions += [combat_action(poser, name) for name in COMBAT_FRAMES]
    for action in actions:
        action.use_fake_user = True
    rig.animation_data.action = actions[0]
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, 8
    bpy.context.scene.render.fps = 12
    args.out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(args.out.resolve()))
    print(f"BUILD_BURNING_DONE {args.out} height={rig.dimensions.z:.2f}")


#: Frames of the combat actions; render and pack take the same counts.
COMBAT_FRAMES = {"telegraph": 14, "charge": 10, "recover": 12, "hit": 6}


def combat_action(poser: Poser, name: str) -> bpy.types.Action:
    return {"telegraph": key_telegraph, "charge": key_charge, "recover": key_recover, "hit": key_hit}[name](poser, COMBAT_FRAMES[name])


def rekey(args: argparse.Namespace) -> None:
    rig = bpy.data.objects["figure"]
    toe = (rig.matrix_world @ rig.data.bones["ball_l"].tail_local) - (rig.matrix_world @ rig.data.bones["foot_l"].head_local)
    poser = Poser(rig, Vector((0, toe.y, 0)).normalized())
    add_leg_ik(rig)
    measure_axes(poser, [(f"{bone}_{side}", "down") for bone in FINGERS + THUMB for side in ("l", "r")]
                 + [(bone, way) for bone in ("pelvis", "spine_01", "spine_02", "spine_03", "neck_01", "head")
                    for way in ("forward", "back")]
                 + [(f"{bone}_{side}", way) for bone in ("thigh", "calf", "foot", "clavicle") for side in ("l", "r")
                    for way in ("forward", "back", "down", "up")])
    names = [name.strip() for name in args.rekey.split(",") if name.strip()]
    for name in names:
        combat_action(poser, name)
        print(f"REKEYED {name} frames={COMBAT_FRAMES[name]}")
    finish_rekey(rig, "idle", args.out.resolve())
    print(f"REKEY_BURNING_DONE {args.out} actions={','.join(names)}")


if __name__ == "__main__":
    arguments = parse(sys.argv)
    if arguments.rekey:
        rekey(arguments)
    else:
        main()
