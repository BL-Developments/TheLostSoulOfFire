"""Key art for the title screen: the protagonist at the end of the unfinished shore, at night.

    blender -b art/production/candidates/player/player.blend -P tools/visuals/blender/build_title.py -- \\
        --out art/production/candidates/title/backdrop.png [--samples 64] [--width 1920 --height 1080]

Opens the player figure (player.blend) and builds around it, with a perspective camera behind
him: the broken platform edge he stands on, the causeway's broken pieces leading out over the
water, the split-flap departure board rising tilted from the sea (the shore's landmark), the
roofs of the drowned town, and far away on the horizon the Warden threshold with the vertical
violet flame in its slit. Moonlight from behind rims the figure; fog lies on the water. The top
of the frame stays calm for the title the game sets over it. Kuwahara makes it read as painted.
"""
from __future__ import annotations

import argparse
import math
import random
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import env_kit as kit  # noqa: E402

ROOT = Path(__file__).resolve().parents[3]
PROLOGUE = ROOT / "art" / "production" / "candidates" / "environment.prologue"
WARDEN = (0.55, 0.22, 1.0)


def flame_rim(figure: bpy.types.Object, strength: float = 0.7, power: float = 4.0) -> None:
    """The Warden flame ahead lines his silhouette: every toon material of the figure gets an
    added violet term from the facing ratio, so the edges glow toward the gate while the inner
    shapes keep their painted values. Only in this render (the game's sprites are unchanged)."""
    materials = {slot.material for child in figure.children_recursive if child.type == "MESH"
                 for slot in child.material_slots if slot.material is not None}
    for material in materials:
        if not material.use_nodes or "outline" in material.name:
            continue
        nodes, links = material.node_tree.nodes, material.node_tree.links
        emission = next((node for node in nodes if node.type == "EMISSION"), None)
        if emission is None or not emission.inputs["Color"].is_linked:
            continue
        source = emission.inputs["Color"].links[0].from_socket
        weight = nodes.new("ShaderNodeLayerWeight")
        weight.inputs["Blend"].default_value = 0.5
        curve = nodes.new("ShaderNodeMath")
        curve.operation = "POWER"
        curve.inputs[1].default_value = power
        links.new(weight.outputs["Facing"], curve.inputs[0])
        rim = nodes.new("ShaderNodeVectorMath")
        rim.operation = "SCALE"
        rim.inputs[0].default_value = tuple(c * strength for c in (0.62, 0.36, 1.0))
        links.new(curve.outputs[0], rim.inputs["Scale"])
        add = nodes.new("ShaderNodeVectorMath")
        add.operation = "ADD"
        links.new(source, add.inputs[0])
        links.new(rim.outputs["Vector"], add.inputs[1])
        links.new(add.outputs["Vector"], emission.inputs["Color"])


def parse(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(prog="build_title.py")
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--samples", type=int, default=64)
    parser.add_argument("--width", type=int, default=1920)
    parser.add_argument("--height", type=int, default=1080)
    return parser.parse_args(argv[argv.index("--") + 1:] if "--" in argv else [])


def tex(name: str) -> str:
    return str(sorted(PROLOGUE.glob(f"tex-{name}-*.png"))[-1])


def sky(scene: bpy.types.Scene) -> None:
    """Night sky: a dark blue-violet gradient, brighter toward the horizon, a veiled moon high
    on the right; volumetric haze over everything."""
    world = scene.world or bpy.data.worlds.new("world")
    scene.world = world
    world.use_nodes = True
    nodes, links = world.node_tree.nodes, world.node_tree.links
    nodes.clear()
    coords = nodes.new("ShaderNodeTexCoord")
    separate = nodes.new("ShaderNodeSeparateXYZ")
    links.new(coords.outputs["Generated"], separate.inputs["Vector"])
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.48
    ramp.color_ramp.elements[0].color = (0.085, 0.09, 0.13, 1)
    ramp.color_ramp.elements[1].position = 0.75
    ramp.color_ramp.elements[1].color = (0.016, 0.016, 0.03, 1)
    links.new(separate.outputs["Z"], ramp.inputs["Fac"])
    clouds = nodes.new("ShaderNodeTexNoise")
    clouds.inputs["Scale"].default_value = 3.5
    clouds.inputs["Detail"].default_value = 8.0
    clouds.inputs["Roughness"].default_value = 0.6
    links.new(coords.outputs["Generated"], clouds.inputs["Vector"])
    cloud_ramp = nodes.new("ShaderNodeValToRGB")
    cloud_ramp.color_ramp.elements[0].position = 0.45
    cloud_ramp.color_ramp.elements[0].color = (0.8, 0.8, 0.85, 1)
    cloud_ramp.color_ramp.elements[1].position = 0.75
    cloud_ramp.color_ramp.elements[1].color = (1.25, 1.25, 1.35, 1)
    links.new(clouds.outputs["Fac"], cloud_ramp.inputs["Fac"])
    mix = nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 1.0
    links.new(ramp.outputs["Color"], mix.inputs["A"])
    links.new(cloud_ramp.outputs["Color"], mix.inputs["B"])
    background = nodes.new("ShaderNodeBackground")
    background.inputs["Strength"].default_value = 1.0
    links.new(mix.outputs["Result"], background.inputs["Color"])
    haze = nodes.new("ShaderNodeVolumePrincipled")
    haze.inputs["Density"].default_value = 0.0045
    haze.inputs["Color"].default_value = (0.55, 0.6, 0.75, 1)
    output = nodes.new("ShaderNodeOutputWorld")
    links.new(background.outputs["Background"], output.inputs["Surface"])
    links.new(haze.outputs["Volume"], output.inputs["Volume"])


def board(m, base: Vector) -> None:
    """The split-flap departure board rising tilted from the sea: an enamel frame on two posts,
    rows of dark flaps with pale strips, one side sunk lower, its axis pointing out to sea."""
    tilt = (0.0, math.radians(-14), math.radians(24))
    frame = kit.box("board_frame", base + Vector((0, 0, 3.4)), (5.4, 0.35, 2.6), m["green"], bevel=0.06, rotation=tilt)
    for side in (-1, 1):
        kit.box(f"board_post_{side}", base + Vector((side * 2.2, 0.1, 1.3)), (0.22, 0.22, 4.2), m["iron"], rotation=tilt)
    rng = random.Random(3)
    for row in range(5):
        for col in range(12):
            flap = kit.box(f"flap_{row}_{col}", base + Vector((-2.35 + col * 0.42, 0.2, 4.35 - row * 0.42)), (0.36, 0.06, 0.34), m["dark"], rotation=tilt)
            if rng.random() < 0.55:
                kit.box(f"glyph_{row}_{col}", flap.location + Vector((0, 0.04, 0)), (0.06 + rng.random() * 0.16, 0.02, 0.2), m["paper"], rotation=tilt)
    # Re-anchor everything around the tilt pivot: rotate as a group about the base.
    for obj in [o for o in bpy.data.objects if o.name.startswith(("board_", "flap_", "glyph_"))]:
        offset = obj.location - base
        from mathutils import Euler
        obj.location = base + Euler(tilt).to_matrix() @ offset


def build(args) -> None:
    scene = bpy.context.scene
    for obj in [o for o in bpy.data.objects if o.type in ("CAMERA", "LIGHT")]:
        bpy.data.objects.remove(obj)
    for identifier in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
        try:
            scene.render.engine = identifier
            break
        except TypeError:
            continue
    scene.eevee.taa_render_samples = args.samples
    if hasattr(scene.eevee, "use_volumetric_shadows"):
        scene.eevee.use_volumetric_shadows = True
    if hasattr(scene.eevee, "volumetric_end"):
        scene.eevee.volumetric_end = 260.0
    scene.view_settings.view_transform = "Standard"
    scene.render.resolution_x, scene.render.resolution_y = args.width, args.height
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    sky(scene)

    figure = bpy.data.objects["figure"]
    figure.animation_data.action = bpy.data.actions["idle"]
    scene.frame_set(1)
    figure.location = (0.0, 0.0, 0.0)
    figure.rotation_euler = (0, 0, math.radians(14))  # he faces -Y, out to sea, a little toward the gate (screen left)
    flame_rim(figure)

    m = {
        "slab": kit.stone("slab", tex("concrete"), 6.0, tint=(0.42, 0.45, 0.5), spread=0.3),
        "kerb": kit.stone("kerb", tex("granite"), 3.0, tint=(0.36, 0.37, 0.4), spread=0.25),
        "kerb_box": kit.textured("kerb_box", tex("granite"), 3.0, tint=(0.3, 0.31, 0.34), axes="BOX", bump=0.3),
        "water": kit.textured("water", tex("water"), 14.0, tint=(0.7, 0.75, 0.9), roughness=0.14, bump=0.2, variation=0.3),
        "iron": kit.painted("iron", (0.03, 0.03, 0.035), (0.08, 0.08, 0.09), scale=3.0, roughness=0.5),
        "green": kit.painted("green", (0.08, 0.13, 0.11), (0.16, 0.22, 0.19), scale=3.0, roughness=0.6),
        "dark": kit.painted("dark", (0.006, 0.006, 0.008), (0.02, 0.02, 0.025), bump=0.0),
        "paper": kit.painted("paper", (0.45, 0.44, 0.40), (0.6, 0.58, 0.52), scale=8.0, bump=0.0),
        "slate": kit.painted("slate", (0.03, 0.034, 0.045), (0.07, 0.075, 0.09), scale=4.0, roughness=0.5),
        "wall": kit.painted("wall", (0.015, 0.014, 0.02), (0.035, 0.032, 0.045), scale=0.2, bump=0.0),
        "flame": kit.emissive("flame", WARDEN, 40.0),
        "rail": kit.painted("rail", (0.1, 0.1, 0.11), (0.24, 0.24, 0.26), scale=8.0, roughness=0.3),
    }
    # The sea.
    kit.plane("sea", Vector((-400, -400, -0.9)), Vector((400, 60, -0.9)), m["water"])
    # The platform under him, its edge broken a metre in front of his feet.
    kit.box("platform", Vector((0.5, 6.0, -0.5)), (14.0, 12.0, 1.0), m["kerb_box"])
    kit.paving("pslab", -6.5, 7.5, 0.6, 12.0, m["slab"], seed=4, row=1.4, lengths=(1.6, 2.8), gap=0.05, height=0.12)
    rng = random.Random(9)
    for k in range(10):
        kit.box(f"edge_{k}", Vector((-6.0 + k * 1.4 + rng.uniform(-0.2, 0.2), 0.3 + rng.uniform(-0.25, 0.15), -0.15 + rng.uniform(-0.15, 0.05))),
                (1.3, 0.6, 0.4), m["kerb"], bevel=0.04, rotation=(rng.uniform(-0.1, 0.1), rng.uniform(-0.12, 0.12), rng.uniform(-0.1, 0.1)))
    # The causeway's broken pieces leading out to sea, with rails, toward the threshold.
    for k in range(9):
        y = -6 - k * 9.0
        x = 3.0 + k * 0.7
        length = rng.uniform(4.5, 7.0)
        kit.box(f"dam_{k}", Vector((x, y, -0.6 - k * 0.06)), (4.2, length, 1.4), m["kerb_box"], bevel=0.08,
                rotation=(rng.uniform(-0.04, 0.04), rng.uniform(-0.05, 0.05), rng.uniform(-0.05, 0.05)))
        for side in (-0.7, 0.7):
            kit.box(f"damrail_{k}_{side}", Vector((x + side, y, 0.13 - k * 0.06)), (0.08, length - 0.3, 0.08), m["rail"])
    # Drowned roofs and chimneys.
    for k in range(14):
        base = Vector((rng.uniform(-60, 45), rng.uniform(-90, -14), -1.6 + rng.uniform(-0.4, 0.2)))
        if abs(base.x - 3 - 0.07 * -base.y) < 4.5:
            continue
        kit.roof(f"roof_{k}", base, rng.uniform(5, 9), rng.uniform(3.5, 5), rng.uniform(1.8, 2.8), m["slate"], yaw=rng.uniform(-0.4, 0.4))
        if rng.random() < 0.6:
            kit.box(f"chim_{k}", base + Vector((rng.uniform(-1.5, 1.5), 0.3, 2.4)), (0.6, 0.6, 1.4), m["slate"])
    board(m, Vector((11.0, -19.0, -1.2)))
    # The Warden threshold on the horizon: a long dark wall, a gate tower, the flame in its slit.
    kit.box("far_wall", Vector((8, -185, 9)), (170, 6, 22), m["wall"])
    kit.box("far_gate", Vector((10, -180, 20)), (14, 8, 44), m["wall"])
    kit.box("far_slit", Vector((10, -175.8, 22)), (0.9, 0.4, 7.0), m["flame"])
    for k in range(7):
        x = -80 + k * 25 + rng.uniform(-6, 6)
        kit.box(f"far_tower_{k}", Vector((x, -192, 18)), (rng.uniform(6, 10), 6, rng.uniform(30, 48)), m["wall"])
    kit.point_light("gate_glow", Vector((10, -172, 22)), 60000.0, WARDEN, radius=2.0)
    # Moonlight from behind and above, a little to the right: a cold rim on the figure.
    moon = bpy.data.lights.new("moon", type="SUN")
    moon.energy = 4.5
    moon.color = (0.72, 0.8, 1.0)
    moon.angle = math.radians(1.5)
    moon_obj = bpy.data.objects.new("moon", moon)
    scene.collection.objects.link(moon_obj)
    travel = Vector((0.35, 0.75, -0.55)).normalized()  # from the moon (far, high, right) toward the camera
    moon_obj.rotation_euler = travel.to_track_quat("-Z", "Y").to_euler()
    # The moon itself, veiled, high on the right.
    bpy.ops.mesh.primitive_uv_sphere_add(radius=6.0, location=(-95, -330, 120))
    disc = bpy.context.active_object
    disc.data.materials.append(kit.emissive("moon_disc", (0.75, 0.8, 0.95), 3.0))
    # A cold platform lamp somewhere behind the camera, left: soft fill on his back.
    kit.point_light("platform_lamp", Vector((-4.0, 9.0, 4.5)), 2600.0, (0.7, 0.78, 0.95), radius=0.6)
    kit.point_light("board_lamp", Vector((7.0, -14.0, 6.0)), 1800.0, (0.66, 0.74, 0.95), radius=0.8)
    # The camera: behind him, a little above, looking out to sea along the causeway.
    data = bpy.data.cameras.new("title")
    data.lens = 28
    data.clip_end = 600
    camera = bpy.data.objects.new("title", data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.location = (-2.4, 8.8, 2.7)
    target = Vector((5.5, -40.0, 3.0))
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    kit.painterly(scene, size=5, sharpness=0.55)
    kit.render(scene, args.out.resolve())


build(parse(sys.argv))
