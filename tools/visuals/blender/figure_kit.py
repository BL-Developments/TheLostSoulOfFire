"""Shared parts for building rendered figures in Blender (3D path B of the painted slice).

Imported by build_player.py and build_hollow.py: toon materials with brush noise and an
inverted-hull outline, MPFB2 access, a Poser that rotates bones toward stage directions
measured on the rig, IK helpers and grounding. Nothing here is specific to one figure.
"""
from __future__ import annotations

import importlib
import math

import addon_utils
import bmesh
import bpy
from mathutils import Matrix, Vector

MPFB = "bl_ext.blender_org.mpfb"


def toon(name: str, colour: tuple[float, float, float], brush: float = 0.12) -> bpy.types.Material:
    """Diffuse light through a three-step ramp, multiplied with the colour; noise breaks the
    band edges like brush strokes. Emission output, so only the ramp decides the values."""
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    nodes.clear()
    diffuse = nodes.new("ShaderNodeBsdfDiffuse")
    to_rgb = nodes.new("ShaderNodeShaderToRGB")
    to_value = nodes.new("ShaderNodeRGBToBW")
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 22.0
    noise.inputs["Detail"].default_value = 3.0
    wobble = nodes.new("ShaderNodeMath")
    wobble.operation = "MULTIPLY_ADD"
    wobble.inputs[1].default_value = brush
    wobble.inputs[2].default_value = -brush * 0.5
    add = nodes.new("ShaderNodeMath")
    add.operation = "ADD"
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = "EASE"
    elements = ramp.color_ramp.elements
    elements[0].position, elements[0].color = 0.08, (0.38, 0.38, 0.44, 1)
    elements[1].position, elements[1].color = 0.55, (1.0, 1.0, 1.0, 1)
    middle = elements.new(0.3)
    middle.color = (0.72, 0.70, 0.76, 1)
    tint = nodes.new("ShaderNodeVectorMath")
    tint.operation = "MULTIPLY"
    tint.inputs[1].default_value = colour
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Strength"].default_value = 1.6
    output = nodes.new("ShaderNodeOutputMaterial")
    links.new(diffuse.outputs[0], to_rgb.inputs[0])
    links.new(to_rgb.outputs["Color"], to_value.inputs[0])
    links.new(noise.outputs["Fac"], wobble.inputs[0])
    links.new(to_value.outputs[0], add.inputs[0])
    links.new(wobble.outputs[0], add.inputs[1])
    links.new(add.outputs[0], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], tint.inputs[0])
    links.new(tint.outputs[0], emission.inputs["Color"])
    links.new(emission.outputs[0], output.inputs["Surface"])
    return material


def flat(name: str, colour: tuple[float, float, float]) -> bpy.types.Material:
    """Unlit paint, for ink marks such as the outline, eyes and brows."""
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = (*colour, 1)
    output = nodes.new("ShaderNodeOutputMaterial")
    material.node_tree.links.new(emission.outputs[0], output.inputs["Surface"])
    return material


def outline_material() -> bpy.types.Material:
    material = flat("outline", (0.006, 0.005, 0.009))
    material.use_backface_culling = True
    return material


def add_outline(obj: bpy.types.Object, outline: bpy.types.Material, thickness: float = 0.007) -> None:
    obj.data.materials.append(outline)
    modifier = obj.modifiers.new("Outline", "SOLIDIFY")
    modifier.thickness = thickness
    modifier.offset = 1.0
    modifier.use_flip_normals = True
    modifier.use_rim = False
    modifier.material_offset = len(obj.data.materials) - 1


def enable_mpfb():
    addon_utils.enable(MPFB, default_set=True)
    human = importlib.import_module(MPFB + ".services.humanservice").HumanService
    target = importlib.import_module(MPFB + ".services.targetservice").TargetService
    return human, target


def dominant_bone(body: bpy.types.Object, bones: set[str]) -> list[str]:
    names = {group.index: group.name for group in body.vertex_groups if group.name in bones}
    result = []
    for vertex in body.data.vertices:
        best, weight = "", 0.0
        for element in vertex.groups:
            name = names.get(element.group)
            if name and element.weight > weight:
                best, weight = name, element.weight
        result.append(best)
    return result


def attach(obj: bpy.types.Object, rig: bpy.types.Object, bone: str) -> None:
    bpy.context.view_layer.update()  # scale and rotation set just before must reach matrix_world
    world = obj.matrix_world.copy()
    obj.parent = rig
    obj.parent_type = "BONE"
    obj.parent_bone = bone
    obj.matrix_world = world


def bone_world(rig: bpy.types.Object, name: str) -> tuple[Vector, Vector]:
    bone = rig.data.bones[name]
    return rig.matrix_world @ bone.head_local, rig.matrix_world @ bone.tail_local


def shaped_coordinates(body: bpy.types.Object) -> list[Vector]:
    """Vertex positions with MPFB's macro shape keys applied; the basis mesh is the neutral human."""
    keys = body.data.shape_keys
    if keys is None:
        return [v.co.copy() for v in body.data.vertices]
    basis = keys.reference_key
    result = [point.co.copy() for point in basis.data]
    for block in keys.key_blocks:
        if block == basis or block.mute or block.value == 0.0:
            continue
        relative = block.relative_key
        for index, point in enumerate(block.data):
            result[index] += (point.co - relative.data[index].co) * block.value
    return result


def axis_for(rig: bpy.types.Object, bone: str, direction: Vector, angle: float = 0.35) -> tuple[int, float]:
    """The local rotation axis and sign that move the bone's tail most along `direction`."""
    pose = rig.pose.bones[bone]
    pose.rotation_mode = "XYZ"
    best = (0, 1.0, -1e9)
    for axis in range(3):
        for sign in (1.0, -1.0):
            pose.rotation_euler = (0, 0, 0)
            bpy.context.view_layer.update()
            before = (rig.matrix_world @ pose.tail).copy()
            rotation = [0.0, 0.0, 0.0]
            rotation[axis] = angle * sign
            pose.rotation_euler = rotation
            bpy.context.view_layer.update()
            moved = (rig.matrix_world @ pose.tail) - before
            score = moved.dot(direction)
            if score > best[2]:
                best = (axis, sign, score)
    pose.rotation_euler = (0, 0, 0)
    bpy.context.view_layer.update()
    return best[0], best[1]


class Poser:
    """Rotations in degrees toward world directions, so poses read like stage directions."""

    def __init__(self, rig: bpy.types.Object, forward: Vector):
        self.rig = rig
        self.forward = forward
        self.down = Vector((0, 0, -1))
        self.cache: dict[tuple[str, str], tuple[int, float]] = {}
        self.left = 1.0 if rig.data.bones["upperarm_l"].head_local.x > 0 else -1.0
        for pose in rig.pose.bones:
            pose.rotation_mode = "XYZ"

    def _axis(self, bone: str, toward: str) -> tuple[int, float]:
        key = (bone, toward)
        if key not in self.cache:
            vector = {"forward": self.forward, "down": self.down, "up": -self.down, "back": -self.forward,
                      "left": Vector((self.left, 0, 0)), "right": Vector((-self.left, 0, 0)),
                      "out": Vector((1 if "_l" in bone and bone.endswith("_l") else -1, 0, 0)) * self._side()}[toward]
            self.cache[key] = axis_for(self.rig, bone, vector)
        return self.cache[key]

    def _side(self) -> float:
        left = self.rig.data.bones["upperarm_l"].head_local.x
        return 1.0 if left > 0 else -1.0

    def set(self, bone: str, toward: str, degrees: float) -> None:
        axis, sign = self._axis(bone, toward)
        pose = self.rig.pose.bones[bone]
        rotation = list(pose.rotation_euler)
        rotation[axis] += math.radians(degrees) * sign
        pose.rotation_euler = rotation

    def aim(self, bone: str, direction: Vector) -> None:
        """Turn a bone so it points along a world direction (its parent chain already posed)."""
        bpy.context.view_layer.update()
        pose = self.rig.pose.bones[bone]
        target = (self.rig.matrix_world.inverted().to_3x3() @ direction).normalized()
        current = pose.matrix.to_3x3()
        rotation = current.col[1].normalized().rotation_difference(target).to_matrix()
        pose.matrix = Matrix.Translation(pose.matrix.translation) @ (rotation @ current).to_4x4()
        bpy.context.view_layer.update()

    def world(self, forward: float, down: float, out: float, side: str) -> Vector:
        """A direction from stage terms: forward, down and outward from the figure's `side` (l/r)."""
        outward = Vector((self.left if side == "l" else -self.left, 0, 0))
        return (self.forward * forward + self.down * down + outward * out).normalized()

    def turn(self, bone: str, degrees: float) -> None:
        """Turn a bone about the world's vertical axis through its head (torso twist)."""
        bpy.context.view_layer.update()
        pose = self.rig.pose.bones[bone]
        head = pose.matrix.translation.copy()
        up = (self.rig.matrix_world.inverted().to_3x3() @ Vector((0, 0, 1))).normalized()
        rotation = Matrix.Rotation(math.radians(degrees), 4, up)
        pose.matrix = Matrix.Translation(head) @ rotation @ Matrix.Translation(-head) @ pose.matrix
        bpy.context.view_layer.update()

    def measure_rest(self, bones: list[str], toward: str) -> None:
        """Measure rotation axes now, in the rest pose (MPFB: T-pose, palms down), before posing."""
        for bone in bones:
            self._axis(bone, toward)

    def clear(self) -> None:
        for pose in self.rig.pose.bones:
            pose.rotation_euler = (0, 0, 0)
            pose.location = (0, 0, 0)

    def key(self, frame: int) -> None:
        for pose in self.rig.pose.bones:
            pose.keyframe_insert("rotation_euler", frame=frame)
            pose.keyframe_insert("location", frame=frame)


FINGERS = [f"{finger}_{joint:02d}" for finger in ("index", "middle", "ring", "pinky") for joint in (1, 2, 3)]
THUMB = ["thumb_01", "thumb_02", "thumb_03"]


def loose_fists(poser: Poser, curl: float) -> None:
    """Fingers curled toward the palm; MPFB's flat open hands read as reaching."""
    for side in ("l", "r"):
        for bone in FINGERS:
            poser.set(f"{bone}_{side}", "down", curl * (0.8 if bone.endswith("01") else 1.0))
        for bone in THUMB[1:]:
            poser.set(f"{bone}_{side}", "down", curl * 0.35)


def ground_feet(poser: Poser, ground: float, settle: float = 1.0) -> None:
    """Shift the pelvis so no foot sinks below the ground height of the rest pose. A foot above
    the ground is pulled down only by `settle` (0..1); below 1 the run keeps a flight phase
    instead of dropping into a lunge when the leading leg reaches forward."""
    bpy.context.view_layer.update()
    rig = poser.rig
    lowest = min((rig.matrix_world @ rig.pose.bones[name].head).z for name in ("foot_l", "foot_r", "ball_l", "ball_r"))
    lowest = min(lowest, *((rig.matrix_world @ rig.pose.bones[name].tail).z for name in ("ball_l", "ball_r")))
    pelvis = rig.pose.bones["pelvis"]
    needed = ground - lowest
    world_shift = Vector((0, 0, needed if needed > 0 else needed * settle))
    local = pelvis.bone.matrix_local.to_3x3().inverted() @ (rig.matrix_world.to_3x3().inverted() @ world_shift)
    pelvis.location = pelvis.location + local
    bpy.context.view_layer.update()


def ground_points(poser: Poser, ground: float, points: list[tuple[str, str]]) -> None:
    """Move the pelvis up or down so the lowest of the given bone ends (bone, "head"|"tail")
    rests on the ground: for kneeling and lying poses, where knees, hands or the chest touch it."""
    bpy.context.view_layer.update()
    rig = poser.rig
    lowest = min((rig.matrix_world @ getattr(rig.pose.bones[name], end)).z for name, end in points)
    pelvis = rig.pose.bones["pelvis"]
    world_shift = Vector((0, 0, ground - lowest))
    local = pelvis.bone.matrix_local.to_3x3().inverted() @ (rig.matrix_world.to_3x3().inverted() @ world_shift)
    pelvis.location = pelvis.location + local
    bpy.context.view_layer.update()


def rest_ground(rig: bpy.types.Object) -> float:
    points = [rig.matrix_world @ rig.data.bones[name].head_local for name in ("foot_l", "foot_r", "ball_l", "ball_r")]
    points += [rig.matrix_world @ rig.data.bones[name].tail_local for name in ("ball_l", "ball_r")]
    return min(point.z for point in points)


def best_pole_angle(rig: bpy.types.Object, side: str, constraint: bpy.types.Constraint) -> float:
    """The pole angle that puts the elbow nearest its pole bone, measured rather than guessed."""
    pole = rig.matrix_world @ rig.pose.bones[f"pole_{side}"].head
    best, distance = 0.0, float("inf")
    for degrees in range(-180, 180, 15):
        constraint.pole_angle = math.radians(degrees)
        bpy.context.view_layer.update()
        elbow = rig.matrix_world @ rig.pose.bones[f"lowerarm_{side}"].head
        if (elbow - pole).length < distance:
            best, distance = math.radians(degrees), (elbow - pole).length
    return best


Placement = tuple[Vector, Vector, Vector]


def placement_of(poser: Poser, bone: str) -> Placement:
    """Where a carrier bone is now, in rig space: (centre, axis, up), as place_bone takes it."""
    bpy.context.view_layer.update()
    matrix = poser.rig.pose.bones[bone].matrix
    return matrix.translation.copy(), matrix.col[1].xyz.normalized(), matrix.col[2].xyz.normalized()


def blend_placement(a: Placement, b: Placement, t: float) -> Placement:
    """In-between of two placements: straight line for the centre, shortest turn for the rest."""
    def rotation(placement: Placement):
        y = placement[1].normalized()
        z = (placement[2] - y * placement[2].dot(y)).normalized()
        return Matrix((y.cross(z), y, z)).transposed().to_quaternion()
    turned = rotation(a).slerp(rotation(b), t).to_matrix()
    return a[0].lerp(b[0], t), turned.col[1], turned.col[2]


def ease(t: float) -> float:
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def place_bone(poser: Poser, bone: str, centre: Vector, axis: Vector, up: Vector) -> None:
    """Put a non-deforming carrier bone at `centre` in rig space, its Y along `axis`, Z toward `up`."""
    y = axis.normalized()
    z = (up - y * up.dot(y)).normalized()
    matrix = Matrix((y.cross(z), y, z)).transposed().to_4x4()
    matrix.translation = centre
    bpy.context.view_layer.update()
    poser.rig.pose.bones[bone].matrix = matrix
    bpy.context.view_layer.update()


def cloth_shell(body: bpy.types.Object, per_vertex: list[str], zones: set[str], rig: bpy.types.Object,
                material: bpy.types.Material, name: str, push: float = 0.016) -> bpy.types.Object:
    """Clothing cut from the body: the faces of the given zones, pushed outward along their
    normals and skinned with the body's weights, so it deforms with the rig."""
    mesh = body.data.copy()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.matrix_world = body.matrix_world
    for group in body.vertex_groups:
        obj.vertex_groups.new(name=group.name)
    # Copy weights.
    for vertex in body.data.vertices:
        for element in vertex.groups:
            obj.vertex_groups[body.vertex_groups[element.group].name].add([vertex.index], element.weight, "REPLACE")
    # Only real skin may become cloth: MPFB keeps hidden helper geometry (skirt, tights, teeth)
    # in the same mesh, outside the vertex group "body".
    body_group = body.vertex_groups["body"].index
    skin = {v.index for v in body.data.vertices if any(g.group == body_group and g.weight > 0.5 for g in v.groups)}
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()
    remove = [face for face in bm.faces if any(per_vertex[v.index] not in zones or v.index not in skin for v in face.verts)]
    bmesh.ops.delete(bm, geom=remove, context="FACES")
    for vertex in bm.verts:
        vertex.co += vertex.normal * push
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.clear()
    mesh.materials.append(material)
    obj.modifiers.clear()
    armature = obj.modifiers.new("Armature", "ARMATURE")
    armature.object = rig
    obj.parent = rig
    return obj
