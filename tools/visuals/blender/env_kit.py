"""Shared helpers for environment plates rendered in 3D (Blender 5.x, EEVEE).

The game draws its world in screen-aligned world units: x to the right, y down, and a figure
stands with its feet on its position. The figures are rendered by an orthographic camera 35°
above the floor at 66.7 world units per metre (render_directions.py). An environment plate uses
the same camera, so a floor point at world (x, y) lands exactly on pixel (x, y) * ppu of the
plate and walls, props and figures share one perspective and one key light:

    ground point (x, y)   ->  Blender (x / U, -y / (U sin 35°), 0)
    h metres of height    ->  h * U * cos 35° world units up on screen

Plates are rendered at `ppu` texture pixels per world unit (the slice: 1.5) and go through a
Kuwahara pass so the 3D render reads as painted, like the FLUX-painted floors of the shore and
the arena. Tall props are rendered on their own (transparent) with their foot point recorded,
so the game can sort them against figures; in the plate they only cast their shadow.
"""
from __future__ import annotations

import math
from pathlib import Path

import bpy
from mathutils import Vector

#: World units per metre (render_directions.py: 100 px/m at 1.5 px per unit).
U = 100.0 / 1.5
ELEVATION = math.radians(35.0)
SIN = math.sin(ELEVATION)
COS = math.cos(ELEVATION)


def reset() -> bpy.types.Scene:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    for identifier in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
        try:
            scene.render.engine = identifier
            break
        except TypeError:
            continue
    scene.eevee.taa_render_samples = 48
    if hasattr(scene.eevee, "use_shadows"):
        scene.eevee.use_shadows = True
    if hasattr(scene.eevee, "use_raytracing"):
        scene.eevee.use_raytracing = True
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    world = bpy.data.worlds.new("world")
    scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.02, 0.02, 0.03, 1.0)
    background.inputs["Strength"].default_value = 0.35
    return scene


def ground(x: float, y: float, z_metres: float = 0.0) -> Vector:
    """Blender point of the world floor position (x, y), optionally z metres up."""
    return Vector((x / U, -y / (U * SIN), z_metres))


def metres(world_units: float) -> float:
    """Horizontal (x) world units to metres."""
    return world_units / U


def depth(world_units: float) -> float:
    """Floor depth (y) world units to metres."""
    return world_units / (U * SIN)


def rise(screen_units: float) -> float:
    """Metres of height that appear `screen_units` world units tall on screen."""
    return screen_units / (U * COS)


def plate_camera(scene: bpy.types.Scene, width: float, height: float, ppu: float = 1.5,
                 left: float = 0.0, top: float = 0.0) -> bpy.types.Object:
    """Orthographic game camera framing the world rectangle (left, top, width, height)."""
    data = bpy.data.cameras.new("plate-camera")
    data.type = "ORTHO"
    data.ortho_scale = max(width, height) / U
    camera = bpy.data.objects.new("plate-camera", data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    scene.render.resolution_x = round(width * ppu)
    scene.render.resolution_y = round(height * ppu)
    scene.render.resolution_percentage = 100
    data.sensor_fit = "HORIZONTAL" if width >= height else "VERTICAL"
    target = ground(left + width / 2, top + height / 2)
    direction = Vector((0.0, COS, -SIN))
    camera.location = target - direction * 60.0
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    data.clip_end = 200.0
    return camera


def key_light(scene: bpy.types.Scene, energy: float = 2.2, colour=(0.82, 0.84, 1.0), angle_deg: float = 6.0) -> bpy.types.Object:
    """Sun from the upper left of the screen, slightly toward the camera (as for the figures)."""
    data = bpy.data.lights.new("key", type="SUN")
    data.energy = energy
    data.color = colour
    data.angle = math.radians(angle_deg)
    key = bpy.data.objects.new("key", data)
    scene.collection.objects.link(key)
    camera = scene.camera
    rotation = camera.matrix_world.to_quaternion()
    right, up, toward = rotation @ Vector((1, 0, 0)), rotation @ Vector((0, 1, 0)), rotation @ Vector((0, 0, 1))
    to_light = (-right * 0.55 + up * 0.55 + toward * 0.63).normalized()
    key.rotation_euler = (-to_light).to_track_quat("-Z", "Y").to_euler()
    return key


def point_light(name: str, at: Vector, energy: float, colour, radius: float = 0.1) -> bpy.types.Object:
    data = bpy.data.lights.new(name, type="POINT")
    data.energy = energy
    data.color = colour
    data.shadow_soft_size = radius
    light = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(light)
    light.location = at
    return light


# ---- Materials ------------------------------------------------------------------------------

def _principled(name: str):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    return material, nodes, material.node_tree.links, bsdf


def _coordinates(nodes, links, scale: float):
    coords = nodes.new("ShaderNodeTexCoord")
    mapping = nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (scale, scale, scale)
    links.new(coords.outputs["Object"], mapping.inputs["Vector"])
    return mapping


def _ramp(nodes, stops: list[tuple[float, tuple[float, float, float]]]):
    ramp = nodes.new("ShaderNodeValToRGB")
    elements = ramp.color_ramp.elements
    while len(elements) > 2:
        elements.remove(elements[-1])
    for index, (position, colour) in enumerate(stops):
        element = elements[index] if index < 2 else elements.new(position)
        element.position = position
        element.color = (*colour, 1.0)
    return ramp


def painted(name: str, dark, light, scale: float = 1.0, roughness: float = 0.85, detail: float = 6.0,
            bump: float = 0.25, image: str | None = None, image_scale: float = 1.0) -> bpy.types.Material:
    """A surface between two colours, broken up by noise (or an image texture, box-projected),
    with a little bump: the base of every painted material."""
    material, nodes, links, bsdf = _principled(name)
    mapping = _coordinates(nodes, links, scale)
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 3.0
    noise.inputs["Detail"].default_value = detail
    noise.inputs["Roughness"].default_value = 0.6
    links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
    ramp = _ramp(nodes, [(0.3, dark), (0.72, light)])
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    colour_out = ramp.outputs["Color"]
    if image:
        texture = nodes.new("ShaderNodeTexImage")
        texture.image = bpy.data.images.load(image, check_existing=True)
        texture.projection = "BOX"
        texture.projection_blend = 0.3
        tex_map = _coordinates(nodes, links, image_scale)
        links.new(tex_map.outputs["Vector"], texture.inputs["Vector"])
        mix = nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        mix.blend_type = "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(texture.outputs["Color"], mix.inputs["A"])
        links.new(ramp.outputs["Color"], mix.inputs["B"])
        colour_out = mix.outputs["Result"]
    links.new(colour_out, bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = roughness
    if bump > 0:
        bump_node = nodes.new("ShaderNodeBump")
        bump_node.inputs["Strength"].default_value = bump
        links.new(noise.outputs["Fac"], bump_node.inputs["Height"])
        links.new(bump_node.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def flagstones(name: str, dark, light, mortar, size: float = 1.1, ash=None, ash_amount: float = 0.0) -> bpy.types.Material:
    """Irregular dressed flagstones (Voronoi cells), dark mortar joints, optional ash in the joints
    and low places."""
    material, nodes, links, bsdf = _principled(name)
    mapping = _coordinates(nodes, links, 1.0 / size)
    cells = nodes.new("ShaderNodeTexVoronoi")
    cells.feature = "DISTANCE_TO_EDGE"
    cells.inputs["Scale"].default_value = 1.0
    cells.inputs["Randomness"].default_value = 0.55
    links.new(mapping.outputs["Vector"], cells.inputs["Vector"])
    colour_cells = nodes.new("ShaderNodeTexVoronoi")
    colour_cells.inputs["Scale"].default_value = 1.0
    colour_cells.inputs["Randomness"].default_value = 0.55
    links.new(mapping.outputs["Vector"], colour_cells.inputs["Vector"])
    joint = _ramp(nodes, [(0.015, (0.0, 0.0, 0.0)), (0.045, (1.0, 1.0, 1.0))])
    links.new(cells.outputs["Distance"], joint.inputs["Fac"])
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 9.0
    noise.inputs["Detail"].default_value = 8.0
    links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
    tone = nodes.new("ShaderNodeMath")
    tone.operation = "MULTIPLY_ADD"
    tone.inputs[1].default_value = 0.5
    links.new(colour_cells.outputs["Color"], tone.inputs[0])
    links.new(noise.outputs["Fac"], tone.inputs[2])
    stone = _ramp(nodes, [(0.35, dark), (0.85, light)])
    links.new(tone.outputs["Value"], stone.inputs["Fac"])
    mix = nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    links.new(joint.outputs["Color"], mix.inputs["Factor"])
    mix.inputs["A"].default_value = (*mortar, 1.0)
    links.new(stone.outputs["Color"], mix.inputs["B"])
    colour_out = mix.outputs["Result"]
    if ash is not None and ash_amount > 0:
        ash_noise = nodes.new("ShaderNodeTexNoise")
        ash_noise.inputs["Scale"].default_value = 1.6
        ash_noise.inputs["Detail"].default_value = 6.0
        links.new(mapping.outputs["Vector"], ash_noise.inputs["Vector"])
        ash_mask = _ramp(nodes, [(0.62 - ash_amount * 0.3, (0, 0, 0)), (0.75 - ash_amount * 0.2, (1, 1, 1))])
        links.new(ash_noise.outputs["Fac"], ash_mask.inputs["Fac"])
        ash_mix = nodes.new("ShaderNodeMix")
        ash_mix.data_type = "RGBA"
        links.new(ash_mask.outputs["Color"], ash_mix.inputs["Factor"])
        links.new(colour_out, ash_mix.inputs["A"])
        ash_mix.inputs["B"].default_value = (*ash, 1.0)
        colour_out = ash_mix.outputs["Result"]
    links.new(colour_out, bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.9
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.35
    links.new(joint.outputs["Color"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def blocks(name: str, dark, light, mortar, width: float = 0.9, height: float = 0.45) -> bpy.types.Material:
    """Dressed stone blocks in courses (brick texture scaled to ashlar)."""
    material, nodes, links, bsdf = _principled(name)
    coords = nodes.new("ShaderNodeTexCoord")
    brick = nodes.new("ShaderNodeTexBrick")
    brick.inputs["Scale"].default_value = 1.0
    brick.inputs["Brick Width"].default_value = width
    brick.inputs["Row Height"].default_value = height
    brick.inputs["Mortar Size"].default_value = 0.018
    brick.inputs["Color1"].default_value = (*dark, 1.0)
    brick.inputs["Color2"].default_value = (*light, 1.0)
    brick.inputs["Mortar"].default_value = (*mortar, 1.0)
    brick.offset = 0.5
    links.new(coords.outputs["Object"], brick.inputs["Vector"])
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 7.0
    noise.inputs["Detail"].default_value = 8.0
    links.new(coords.outputs["Object"], noise.inputs["Vector"])
    mix = nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "OVERLAY"
    mix.inputs["Factor"].default_value = 0.35
    links.new(brick.outputs["Color"], mix.inputs["A"])
    links.new(noise.outputs["Color"], mix.inputs["B"])
    links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.88
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.4
    links.new(brick.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def textured(name: str, image: str, size: float, tint=(1.0, 1.0, 1.0), roughness: float = 0.85,
             bump: float = 0.3, axes: str = "XY", variation: float = 0.15, wet: float = 0.0) -> bpy.types.Material:
    """A painted texture (FLUX) repeated every `size` metres, projected along two axes ("XY" for
    floors, "XZ" for walls facing the camera, "BOX" for objects), tinted, with a bump from its
    own brightness and a slow large-scale value variation so repeats do not read."""
    material, nodes, links, bsdf = _principled(name)
    coords = nodes.new("ShaderNodeTexCoord")
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = bpy.data.images.load(image, check_existing=True)
    texture.extension = "MIRROR"
    if axes == "BOX":
        mapping = nodes.new("ShaderNodeMapping")
        mapping.inputs["Scale"].default_value = (1 / size, 1 / size, 1 / size)
        links.new(coords.outputs["Object"], mapping.inputs["Vector"])
        texture.projection = "BOX"
        texture.projection_blend = 0.25
        links.new(mapping.outputs["Vector"], texture.inputs["Vector"])
    else:
        separate = nodes.new("ShaderNodeSeparateXYZ")
        links.new(coords.outputs["Object"], separate.inputs["Vector"])
        combine = nodes.new("ShaderNodeCombineXYZ")
        links.new(separate.outputs[axes[0]], combine.inputs["X"])
        links.new(separate.outputs[axes[1]], combine.inputs["Y"])
        mapping = nodes.new("ShaderNodeMapping")
        mapping.inputs["Scale"].default_value = (1 / size, 1 / size, 1)
        links.new(combine.outputs["Vector"], mapping.inputs["Vector"])
        links.new(mapping.outputs["Vector"], texture.inputs["Vector"])
    tint_node = nodes.new("ShaderNodeMix")
    tint_node.data_type = "RGBA"
    tint_node.blend_type = "MULTIPLY"
    tint_node.inputs["Factor"].default_value = 1.0
    tint_node.inputs["B"].default_value = (*tint, 1.0)
    links.new(texture.outputs["Color"], tint_node.inputs["A"])
    colour_out = tint_node.outputs["Result"]
    if variation > 0:
        large = nodes.new("ShaderNodeTexNoise")
        large.inputs["Scale"].default_value = 0.08
        large.inputs["Detail"].default_value = 2.0
        links.new(coords.outputs["Object"], large.inputs["Vector"])
        shade = _ramp(nodes, [(0.3, (1 - variation,) * 3), (0.7, (1 + variation * 0.5,) * 3)])
        links.new(large.outputs["Fac"], shade.inputs["Fac"])
        vary = nodes.new("ShaderNodeMix")
        vary.data_type = "RGBA"
        vary.blend_type = "MULTIPLY"
        vary.inputs["Factor"].default_value = 1.0
        links.new(colour_out, vary.inputs["A"])
        links.new(shade.outputs["Color"], vary.inputs["B"])
        colour_out = vary.outputs["Result"]
    if wet > 0:
        # Puddles: soft world-space patches that are darker and nearly mirror-smooth.
        geometry = nodes.new("ShaderNodeNewGeometry")
        puddle = nodes.new("ShaderNodeTexNoise")
        puddle.inputs["Scale"].default_value = 0.45
        puddle.inputs["Detail"].default_value = 4.0
        puddle.inputs["Roughness"].default_value = 0.55
        links.new(geometry.outputs["Position"], puddle.inputs["Vector"])
        mask = _ramp(nodes, [(0.66 - wet * 0.12, (0, 0, 0)), (0.7 - wet * 0.1, (1, 1, 1))])
        links.new(puddle.outputs["Fac"], mask.inputs["Fac"])
        darker = nodes.new("ShaderNodeMix")
        darker.data_type = "RGBA"
        darker.blend_type = "MULTIPLY"
        links.new(mask.outputs["Color"], darker.inputs["Factor"])
        links.new(colour_out, darker.inputs["A"])
        darker.inputs["B"].default_value = (0.45, 0.48, 0.55, 1.0)
        colour_out = darker.outputs["Result"]
        rough = nodes.new("ShaderNodeMix")
        rough.data_type = "FLOAT"
        links.new(mask.outputs["Color"], rough.inputs["Factor"])
        rough.inputs["A"].default_value = roughness
        rough.inputs["B"].default_value = 0.05
        links.new(rough.outputs["Result"], bsdf.inputs["Roughness"])
    else:
        bsdf.inputs["Roughness"].default_value = roughness
    links.new(colour_out, bsdf.inputs["Base Color"])
    if bump > 0:
        bump_node = nodes.new("ShaderNodeBump")
        bump_node.inputs["Strength"].default_value = bump
        links.new(texture.outputs["Color"], bump_node.inputs["Height"])
        links.new(bump_node.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def roof(name: str, centre: Vector, length: float, width: float, rise: float, material: bpy.types.Material,
         yaw: float = 0.0) -> bpy.types.Object:
    """A pitched roof (ridge along x before `yaw`): a triangular prism, its eaves at centre.z."""
    import bmesh
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    half_l, half_w = length / 2, width / 2
    verts = [bm.verts.new(v) for v in ((-half_l, -half_w, 0), (half_l, -half_w, 0), (half_l, half_w, 0), (-half_l, half_w, 0),
                                       (-half_l, 0, rise), (half_l, 0, rise))]
    for face in ((0, 1, 5, 4), (2, 3, 4, 5), (0, 4, 3), (1, 2, 5), (0, 3, 2, 1)):
        bm.faces.new([verts[i] for i in face])
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = centre
    obj.rotation_euler = (0, 0, yaw)
    mesh.materials.append(material)
    return obj


def stone(name: str, image: str, size: float, tint=(1.0, 1.0, 1.0), spread: float = 0.25,
          ash_image: str | None = None, ash_amount: float = 0.0, ash_tint=(0.55, 0.53, 0.56),
          roughness: float = 0.85, bump: float = 0.35, axes: str = "XY", random_attribute: str | None = None,
          wet: float = 0.0) -> bpy.types.Material:
    """Material for single stones (paving, ashlar): each object samples its own part of a
    jointless painted surface (Object Info > Random) and gets its own value, so hundreds of
    stones never repeat; ash settles across them in world space. With `random_attribute` the
    value comes from that point attribute instead (many stones in one mesh, see `setts`);
    `wet` adds dark, nearly mirror-smooth puddles in world space as in `textured`."""
    material, nodes, links, bsdf = _principled(name)
    coords = nodes.new("ShaderNodeTexCoord")
    if random_attribute:
        info = nodes.new("ShaderNodeAttribute")
        info.attribute_name = random_attribute
        random_out = info.outputs["Fac"]
    else:
        info = nodes.new("ShaderNodeObjectInfo")
        random_out = info.outputs["Random"]
    separate = nodes.new("ShaderNodeSeparateXYZ")
    links.new(coords.outputs["Object"], separate.inputs["Vector"])

    def math_node(operation, a, b=None):
        node = nodes.new("ShaderNodeMath")
        node.operation = operation
        if isinstance(a, float):
            node.inputs[0].default_value = a
        else:
            links.new(a, node.inputs[0])
        if b is not None:
            if isinstance(b, float):
                node.inputs[1].default_value = b
            else:
                links.new(b, node.inputs[1])
        return node.outputs[0]

    random_u = math_node("FRACT", math_node("MULTIPLY", random_out, 1.0))
    random_v = math_node("FRACT", math_node("MULTIPLY", random_out, 7.31))
    random_w = math_node("FRACT", math_node("MULTIPLY", random_out, 13.7))
    u = math_node("ADD", math_node("MULTIPLY", separate.outputs[axes[0]], 1.0 / size), math_node("ADD", math_node("MULTIPLY", random_u, 0.5), 0.25))
    v = math_node("ADD", math_node("MULTIPLY", separate.outputs[axes[1]], 1.0 / size), math_node("ADD", math_node("MULTIPLY", random_v, 0.5), 0.25))
    combine = nodes.new("ShaderNodeCombineXYZ")
    links.new(u, combine.inputs["X"])
    links.new(v, combine.inputs["Y"])
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = bpy.data.images.load(image, check_existing=True)
    texture.extension = "EXTEND"
    links.new(combine.outputs["Vector"], texture.inputs["Vector"])
    value = math_node("ADD", math_node("MULTIPLY", random_w, spread), 1.0 - spread * 0.5)
    tint_rgb = nodes.new("ShaderNodeMix")
    tint_rgb.data_type = "RGBA"
    tint_rgb.blend_type = "MULTIPLY"
    tint_rgb.inputs["Factor"].default_value = 1.0
    tint_rgb.inputs["B"].default_value = (*tint, 1.0)
    links.new(texture.outputs["Color"], tint_rgb.inputs["A"])
    shade = nodes.new("ShaderNodeVectorMath")
    shade.operation = "SCALE"
    links.new(tint_rgb.outputs["Result"], shade.inputs[0])
    links.new(value, shade.inputs["Scale"])
    colour_out = shade.outputs["Vector"]
    if ash_amount > 0 and not ash_image:
        # Flat ash in soft world-space drifts, independent of the stone each point lies on.
        geometry = nodes.new("ShaderNodeNewGeometry")
        drift = nodes.new("ShaderNodeTexNoise")
        drift.inputs["Scale"].default_value = 0.22
        drift.inputs["Detail"].default_value = 6.0
        drift.inputs["Roughness"].default_value = 0.62
        links.new(geometry.outputs["Position"], drift.inputs["Vector"])
        mask = _ramp(nodes, [(0.6 - ash_amount * 0.2, (0, 0, 0)), (0.74 - ash_amount * 0.1, (1, 1, 1))])
        links.new(drift.outputs["Fac"], mask.inputs["Fac"])
        grain = nodes.new("ShaderNodeTexNoise")
        grain.inputs["Scale"].default_value = 40.0
        links.new(geometry.outputs["Position"], grain.inputs["Vector"])
        ash_rgb = nodes.new("ShaderNodeMix")
        ash_rgb.data_type = "RGBA"
        ash_rgb.blend_type = "MULTIPLY"
        ash_rgb.inputs["A"].default_value = (*ash_tint, 1.0)
        links.new(grain.outputs["Color"], ash_rgb.inputs["B"])
        ash_rgb.inputs["Factor"].default_value = 0.35
        mix = nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        links.new(mask.outputs["Color"], mix.inputs["Factor"])
        links.new(colour_out, mix.inputs["A"])
        links.new(ash_rgb.outputs["Result"], mix.inputs["B"])
        colour_out = mix.outputs["Result"]
    if ash_image and ash_amount > 0:
        geometry = nodes.new("ShaderNodeNewGeometry")
        ash_map = nodes.new("ShaderNodeMapping")
        ash_map.inputs["Scale"].default_value = (1 / 7.0, 1 / 7.0, 1 / 7.0)
        links.new(geometry.outputs["Position"], ash_map.inputs["Vector"])
        ash_tex = nodes.new("ShaderNodeTexImage")
        ash_tex.image = bpy.data.images.load(ash_image, check_existing=True)
        ash_tex.extension = "MIRROR"
        links.new(ash_map.outputs["Vector"], ash_tex.inputs["Vector"])
        ash_colour = nodes.new("ShaderNodeMix")
        ash_colour.data_type = "RGBA"
        ash_colour.blend_type = "MULTIPLY"
        ash_colour.inputs["Factor"].default_value = 1.0
        ash_colour.inputs["B"].default_value = (*ash_tint, 1.0)
        links.new(ash_tex.outputs["Color"], ash_colour.inputs["A"])
        drift = nodes.new("ShaderNodeTexNoise")
        drift.inputs["Scale"].default_value = 0.35
        drift.inputs["Detail"].default_value = 5.0
        drift.inputs["Roughness"].default_value = 0.65
        links.new(geometry.outputs["Position"], drift.inputs["Vector"])
        mask = _ramp(nodes, [(0.62 - ash_amount * 0.25, (0, 0, 0)), (0.72 - ash_amount * 0.15, (1, 1, 1))])
        links.new(drift.outputs["Fac"], mask.inputs["Fac"])
        mix = nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        links.new(mask.outputs["Color"], mix.inputs["Factor"])
        links.new(colour_out, mix.inputs["A"])
        links.new(ash_colour.outputs["Result"], mix.inputs["B"])
        colour_out = mix.outputs["Result"]
    bsdf.inputs["Roughness"].default_value = roughness
    if wet > 0:
        colour_out = _puddles(nodes, links, bsdf, colour_out, roughness, wet)
    links.new(colour_out, bsdf.inputs["Base Color"])
    if bump > 0:
        bump_node = nodes.new("ShaderNodeBump")
        bump_node.inputs["Strength"].default_value = bump
        links.new(texture.outputs["Color"], bump_node.inputs["Height"])
        links.new(bump_node.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def _puddles(nodes, links, bsdf, colour_out, roughness: float, wet: float):
    """Puddles: soft world-space patches that are darker and nearly mirror-smooth."""
    geometry = nodes.new("ShaderNodeNewGeometry")
    puddle = nodes.new("ShaderNodeTexNoise")
    puddle.inputs["Scale"].default_value = 0.45
    puddle.inputs["Detail"].default_value = 4.0
    puddle.inputs["Roughness"].default_value = 0.55
    links.new(geometry.outputs["Position"], puddle.inputs["Vector"])
    mask = _ramp(nodes, [(0.66 - wet * 0.12, (0, 0, 0)), (0.7 - wet * 0.1, (1, 1, 1))])
    links.new(puddle.outputs["Fac"], mask.inputs["Fac"])
    darker = nodes.new("ShaderNodeMix")
    darker.data_type = "RGBA"
    darker.blend_type = "MULTIPLY"
    links.new(mask.outputs["Color"], darker.inputs["Factor"])
    links.new(colour_out, darker.inputs["A"])
    darker.inputs["B"].default_value = (0.45, 0.48, 0.55, 1.0)
    rough = nodes.new("ShaderNodeMix")
    rough.data_type = "FLOAT"
    links.new(mask.outputs["Color"], rough.inputs["Factor"])
    rough.inputs["A"].default_value = roughness
    rough.inputs["B"].default_value = 0.05
    links.new(rough.outputs["Result"], bsdf.inputs["Roughness"])
    return darker.outputs["Result"]


def setts(prefix: str, x0: float, x1: float, y0: float, y1: float, material: bpy.types.Material, seed: int = 1,
          row: float = 0.2, lengths=(0.24, 0.38), gap: float = 0.026, height: float = 0.1, jitter: float = 0.012,
          tilt: float = 0.06, bevel: float = 0.018, sunken: float = 0.04, missing: float = 0.012,
          skip=None) -> bpy.types.Object:
    """Small stones (setts) in running rows between Blender x0..x1 and y0..y1 (metres), built as
    ONE mesh: thousands of objects would be slow. Each stone is a box, slightly turned and with
    a slightly sloping top, and carries its own random value in the point attribute
    `stone_random`, which `stone(..., random_attribute="stone_random")` reads. A share of the
    stones has sunk (`sunken`, they hold water) and a few are gone (`missing`)."""
    import random

    import bmesh
    rng = random.Random(seed)
    mesh_builder = bmesh.new()
    layer = mesh_builder.verts.layers.float.new("stone_random")
    corners = [(-1, -1, 0), (1, -1, 0), (1, 1, 0), (-1, 1, 0), (-1, -1, 1), (1, -1, 1), (1, 1, 1), (-1, 1, 1)]
    sides = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    y = y0
    while y < y1:
        x = x0 - rng.uniform(0, lengths[1])
        while x < x1:
            length = rng.uniform(*lengths)
            # Stones at the ends of a row are cut to the edge, so the paving ends straight.
            left, right = max(x, x0), min(x + length, x1)
            depth = min(row, y1 - y)
            cx, cy = (left + right) / 2, y + depth / 2
            if (skip is None or not skip(cx, cy)) and rng.random() >= missing and right - left > 0.08 and depth > 0.08:
                hx, hy = (right - left - gap) / 2, (depth - gap) / 2
                top = rng.uniform(-jitter, jitter) - (rng.uniform(0.015, 0.03) if rng.random() < sunken else 0.0)
                yaw = rng.uniform(-0.03, 0.03)
                tx, ty = rng.uniform(-tilt, tilt), rng.uniform(-tilt, tilt)
                value = rng.random()
                c, s = math.cos(yaw), math.sin(yaw)
                verts = []
                for sx, sy, sz in corners:
                    lx, ly = sx * hx, sy * hy
                    vert = mesh_builder.verts.new((cx + lx * c - ly * s, cy + lx * s + ly * c,
                                                   (top + lx * tx + ly * ty) if sz else top - height))
                    vert[layer] = value
                    verts.append(vert)
                for side in sides:
                    mesh_builder.faces.new([verts[index] for index in side])
            x += length
        y += row
    bmesh.ops.recalc_face_normals(mesh_builder, faces=mesh_builder.faces)
    mesh = bpy.data.meshes.new(prefix)
    mesh_builder.to_mesh(mesh)
    mesh_builder.free()
    obj = bpy.data.objects.new(prefix, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    modifier = obj.modifiers.new("Bevel", "BEVEL")
    modifier.width = bevel
    modifier.segments = 2
    return obj


def paving(prefix: str, x0: float, x1: float, y0: float, y1: float, material: bpy.types.Material, seed: int = 1,
           row: float = 0.62, lengths=(0.7, 1.5), gap: float = 0.035, height: float = 0.12,
           jitter: float = 0.012, skip=None) -> list[bpy.types.Object]:
    """Paving stones in running rows between Blender x0..x1 and y0..y1 (metres), each its own
    bevelled object, slightly uneven. `skip(x, y)` leaves a stone out (e.g. under a wall)."""
    import random
    rng = random.Random(seed)
    stones = []
    y = y0
    while y < y1:
        x = x0 - rng.uniform(0, lengths[1])
        while x < x1:
            length = rng.uniform(*lengths)
            centre = Vector((x + length / 2, y + row / 2, -height / 2 + rng.uniform(-jitter, jitter)))
            if skip is None or not skip(centre.x, centre.y):
                bpy.ops.mesh.primitive_cube_add(size=1.0, location=centre,
                                                rotation=(rng.uniform(-0.01, 0.01), rng.uniform(-0.01, 0.01), rng.uniform(-0.012, 0.012)))
                obj = bpy.context.active_object
                obj.name = f"{prefix}_{len(stones)}"
                obj.scale = (length - gap, row - gap, height)
                bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
                obj.data.materials.append(material)
                bevel = obj.modifiers.new("Bevel", "BEVEL")
                bevel.width = 0.022
                bevel.segments = 2
                stones.append(obj)
            x += length
        y += row
    return stones


def ashlar(prefix: str, x0: float, x1: float, z0: float, z1: float, front_y: float, material: bpy.types.Material,
           seed: int = 2, course: float = 0.45, lengths=(0.8, 1.45), gap: float = 0.028, depth: float = 0.6,
           openings=()) -> list[bpy.types.Object]:
    """A wall face of dressed blocks in courses, front face on y = front_y (metres), leaving out
    `openings` given as (x_min, x_max, z_top) rectangles from the floor."""
    import random
    rng = random.Random(seed)
    blocks = []
    z = z0
    while z < z1:
        blocked = sorted((a, b) for a, b, top in openings if z < top)
        free, start = [], x0
        for a, b in blocked:
            if a > start:
                free.append((start, a))
            start = max(start, b)
        if start < x1:
            free.append((start, x1))
        for a, b in free:
            x = a - (rng.uniform(0, lengths[0]) if a == x0 else 0.0)
            while x < b - 0.05:
                length = min(rng.uniform(*lengths), b - x)
                if b - (x + length) < 0.3:
                    length = b - x
                protrude = rng.uniform(-0.015, 0.02)
                centre = Vector((x + length / 2, front_y + depth / 2 - protrude, z + course / 2))
                bpy.ops.mesh.primitive_cube_add(size=1.0, location=centre)
                obj = bpy.context.active_object
                obj.name = f"{prefix}_{len(blocks)}"
                obj.scale = (length - gap, depth, course - gap)
                bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
                obj.data.materials.append(material)
                bevel = obj.modifiers.new("Bevel", "BEVEL")
                bevel.width = 0.02
                bevel.segments = 2
                blocks.append(obj)
                x += length
        z += course
    return blocks


def no_shadows(objects: list[bpy.types.Object]) -> None:
    """Architecture that should not throw the key light's shadow across the play floor."""
    for obj in objects:
        obj.visible_shadow = False


def area_light(name: str, at: Vector, size: float, energy: float, colour) -> bpy.types.Object:
    """A large soft light pointing straight down (overhead fill)."""
    data = bpy.data.lights.new(name, type="AREA")
    data.energy = energy
    data.color = colour
    data.shape = "DISK"
    data.size = size
    light = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(light)
    light.location = at
    return light


def emissive(name: str, colour, strength: float) -> bpy.types.Material:
    material, nodes, links, bsdf = _principled(name)
    bsdf.inputs["Base Color"].default_value = (*colour, 1.0)
    bsdf.inputs["Emission Color"].default_value = (*colour, 1.0)
    bsdf.inputs["Emission Strength"].default_value = strength
    return material


# ---- Geometry -------------------------------------------------------------------------------

def box(name: str, centre: Vector, size: tuple[float, float, float], material: bpy.types.Material,
        bevel: float = 0.0, rotation=(0.0, 0.0, 0.0)) -> bpy.types.Object:
    """A box in metres around `centre`."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=centre, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    if bevel > 0:
        modifier = obj.modifiers.new("Bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    return obj


def cylinder(name: str, centre: Vector, radius: float, height: float, material: bpy.types.Material,
             vertices: int = 24, rotation=(0.0, 0.0, 0.0)) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=height, location=centre, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.data.materials.append(material)
    bpy.ops.object.shade_smooth()
    return obj


def plane(name: str, corner_a: Vector, corner_b: Vector, material: bpy.types.Material) -> bpy.types.Object:
    """A floor rectangle between two Blender corners (z of corner_a)."""
    centre = (corner_a + corner_b) / 2
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(centre.x, centre.y, corner_a.z))
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (abs(corner_b.x - corner_a.x), abs(corner_b.y - corner_a.y), 1.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    return obj


def scatter(name: str, source: bpy.types.Object, points: list[Vector], seed: int = 1,
            scale_range=(0.8, 1.2), yaw_range=(0.0, math.tau)) -> list[bpy.types.Object]:
    """Linked copies of `source` at points with random yaw and scale."""
    import random
    rng = random.Random(seed)
    copies = []
    for index, point in enumerate(points):
        copy = source.copy()
        copy.name = f"{name}_{index}"
        bpy.context.collection.objects.link(copy)
        copy.location = point
        copy.rotation_euler = (0.0, 0.0, rng.uniform(*yaw_range))
        factor = rng.uniform(*scale_range)
        copy.scale = (factor, factor, factor)
        copies.append(copy)
    source.hide_render = True
    return copies


# ---- Rendering ------------------------------------------------------------------------------

def painterly(scene: bpy.types.Scene, size: int = 5, sharpness: float = 0.6) -> None:
    """Kuwahara (anisotropic) over the render, so the 3D read as brushwork."""
    tree = bpy.data.node_groups.new("painterly", "CompositorNodeTree")
    scene.compositing_node_group = tree
    nodes, links = tree.nodes, tree.links
    layers = nodes.new("CompositorNodeRLayers")
    kuwahara = nodes.new("CompositorNodeKuwahara")
    for prop, value in (("variation", "ANISOTROPIC"),):
        if hasattr(kuwahara, prop):
            setattr(kuwahara, prop, value)
    for socket_name, value in (("Size", size), ("Sharpness", sharpness)):
        if socket_name in kuwahara.inputs:
            kuwahara.inputs[socket_name].default_value = value
    if hasattr(kuwahara, "size"):
        kuwahara.size = size
    output = nodes.new("NodeGroupOutput")
    tree.interface.new_socket("Image", in_out="OUTPUT", socket_type="NodeSocketColor")
    links.new(layers.outputs["Image"], kuwahara.inputs["Image"])
    # Keep the alpha of isolated renders.
    set_alpha = nodes.new("CompositorNodeSetAlpha")
    links.new(kuwahara.outputs["Image"], set_alpha.inputs["Image"])
    links.new(layers.outputs["Alpha"], set_alpha.inputs["Alpha"])
    links.new(set_alpha.outputs["Image"], output.inputs[0])


def render(scene: bpy.types.Scene, path: Path, transparent: bool = False) -> None:
    scene.render.film_transparent = transparent
    path.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def isolate(objects: list[bpy.types.Object], keep_lights: bool = True):
    """Context: only `objects` (and lights) render; everything else is hidden. Returns a restore function."""
    keep = set(objects)
    hidden = []
    for obj in bpy.data.objects:
        if obj in keep or obj.type == "CAMERA" or (keep_lights and obj.type == "LIGHT"):
            continue
        if not obj.hide_render:
            obj.hide_render = True
            hidden.append(obj)

    def restore() -> None:
        for obj in hidden:
            obj.hide_render = False
    return restore


def camera_only_hidden(objects: list[bpy.types.Object], hidden: bool = True) -> None:
    """Objects invisible to the camera but still casting shadows (props rendered separately)."""
    for obj in objects:
        obj.visible_camera = not hidden
