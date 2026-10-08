"""Render a rigged figure in eight directions with a colour and a normal pass.

    blender -b [scene.blend] -P tools/visuals/blender/render_directions.py -- \\
        --out art/production/candidates/<visual-id>/render --animation idle \\
        [--object figure] [--frames 8] [--elevation 35] [--resolution 256] \\
        [--ortho-scale 2.6] [--foot 0.86] [--engine eevee|cycles] [--test-figure]

The camera is orthographic and fixed; the figure turns (its yaw), so all eight directions
come from one rig (VISUAL-ART-DIRECTION §5b). The key light is fixed to the camera, from the
upper left. The world origin is the foot point and always lands at the same pixel of the
frame (--foot is its height as a share of the frame from the top).

Colour pass: <out>/<animation>/<dir>/0001.png (RGBA, transparent background).
Normal pass: <out>/<animation>_normal/<dir>/0001.png, camera-space normals encoded as
n * 0.5 + 0.5 with +X right, +Y up, +Z toward the camera — the convention of SpriteLit.fx.
A small JSON file <out>/<animation>.render.json records camera, engine and frame count for
pack_sheets.py and the production manifest.

EEVEE runs on the Mac; Cycles on the CPU is the path for a machine without a GPU.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
import time
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))

#: Screen angle (y down, degrees) of each direction, as in VisualDirections.
DIRECTIONS = {"e": 0, "se": 45, "s": 90, "sw": 135, "w": 180, "nw": 225, "n": 270, "ne": 315}


def parse(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(prog="render_directions.py")
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--animation", default="idle")
    parser.add_argument("--object", default="figure", help="object (or empty) that turns the whole figure")
    parser.add_argument("--frames", type=int, default=8)
    parser.add_argument("--elevation", type=float, default=35.0, help="camera angle above the floor, degrees (style bible)")
    parser.add_argument("--resolution", type=int, default=256)
    parser.add_argument("--ortho-scale", type=float, default=2.6, help="world units shown across the frame")
    parser.add_argument("--foot", type=float, default=0.86, help="foot point height as share of the frame from the top")
    parser.add_argument("--engine", choices=["eevee", "cycles"], default="eevee")
    parser.add_argument("--samples", type=int, default=16)
    parser.add_argument("--test-figure", action="store_true", help="build the procedural test figure first")
    parser.add_argument("--action", help="action to play on --object (or its armature), e.g. run")
    parser.add_argument("--frame-start", type=int, help="first frame of the animation (default: scene start)")
    parser.add_argument("--directions", help="comma-separated subset of e,se,s,sw,w,nw,n,ne (previews); default all")
    parser.add_argument("--no-normals", action="store_true", help="skip the normal pass (previews)")
    return parser.parse_args(argv[argv.index("--") + 1:] if "--" in argv else [])


def setup_engine(scene: bpy.types.Scene, engine: str, samples: int) -> str:
    if engine == "cycles":
        scene.render.engine = "CYCLES"
        scene.cycles.device = "CPU"
        scene.cycles.samples = samples
        return "CYCLES (CPU)"
    for identifier in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
        try:
            scene.render.engine = identifier
            break
        except TypeError:
            continue
    if hasattr(scene, "eevee"):
        scene.eevee.taa_render_samples = samples
    return scene.render.engine


def setup_camera(scene: bpy.types.Scene, elevation: float, ortho_scale: float, foot: float) -> bpy.types.Object:
    data = bpy.data.cameras.new("slice-camera")
    data.type = "ORTHO"
    data.ortho_scale = ortho_scale
    camera = bpy.data.objects.new("slice-camera", data)
    scene.collection.objects.link(camera)
    scene.camera = camera

    theta = math.radians(elevation)
    # Look at a point above the origin chosen so the origin lands at `foot` of the frame height.
    target = Vector((0.0, 0.0, (foot - 0.5) * ortho_scale / math.cos(theta)))
    direction = Vector((0.0, math.cos(theta), -math.sin(theta)))  # from the camera toward the target
    camera.location = target - direction * 20.0
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    return camera


def setup_key_light(scene: bpy.types.Scene, camera: bpy.types.Object) -> None:
    data = bpy.data.lights.new("key", type="SUN")
    data.energy = 3.2
    key = bpy.data.objects.new("key", data)
    scene.collection.objects.link(key)
    # From the upper left of the screen, slightly toward the camera.
    right = camera.matrix_world.to_quaternion() @ Vector((1, 0, 0))
    up = camera.matrix_world.to_quaternion() @ Vector((0, 1, 0))
    toward_camera = camera.matrix_world.to_quaternion() @ Vector((0, 0, 1))
    to_light = (-right * 0.55 + up * 0.55 + toward_camera * 0.63).normalized()
    key.rotation_euler = (-to_light).to_track_quat("-Z", "Y").to_euler()

    world = scene.world or bpy.data.worlds.new("world")
    scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.05, 0.05, 0.06, 1.0)
    background.inputs["Strength"].default_value = 0.6


def normal_material() -> bpy.types.Material:
    """Emission of the camera-space normal, mapped to 0..1, for the normal pass."""
    material = bpy.data.materials.new("camera-space-normal")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    geometry = nodes.new("ShaderNodeNewGeometry")
    transform = nodes.new("ShaderNodeVectorTransform")
    transform.vector_type = "NORMAL"
    transform.convert_from = "WORLD"
    transform.convert_to = "CAMERA"
    scale = nodes.new("ShaderNodeVectorMath")
    scale.operation = "MULTIPLY_ADD"
    # Blender's camera space has +Z pointing into the scene; flip it so a normal facing the
    # camera encodes as +Z (blue near 255), as SpriteLit.fx expects. X right and Y up stay.
    scale.inputs[1].default_value = (0.5, 0.5, -0.5)
    scale.inputs[2].default_value = (0.5, 0.5, 0.5)
    emission = nodes.new("ShaderNodeEmission")
    output = nodes.new("ShaderNodeOutputMaterial")
    links.new(geometry.outputs["Normal"], transform.inputs["Vector"])
    links.new(transform.outputs["Vector"], scale.inputs[0])
    links.new(scale.outputs["Vector"], emission.inputs["Color"])
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


def render_pass(scene: bpy.types.Scene, figure: bpy.types.Object, out: Path, frames: int, view_transform: str,
                directions: dict[str, int] = DIRECTIONS) -> None:
    scene.view_settings.view_transform = view_transform
    for name, angle in directions.items():
        figure.rotation_euler = (0.0, 0.0, math.radians(90 - angle))
        directory = out / name
        directory.mkdir(parents=True, exist_ok=True)
        for index in range(frames):
            scene.frame_set(scene.frame_start + index)
            scene.render.filepath = str(directory / f"{index + 1:04d}.png")
            bpy.ops.render.render(write_still=True)


def main() -> None:
    args = parse(sys.argv)
    started = time.monotonic()
    if args.test_figure:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        import test_figure
        test_figure.build(args.frames)
    scene = bpy.context.scene
    figure = bpy.data.objects.get(args.object)
    if figure is None:
        raise SystemExit(f"Objekt '{args.object}' nicht gefunden.")

    if args.action:
        action = bpy.data.actions.get(args.action)
        if action is None:
            raise SystemExit(f"Aktion '{args.action}' nicht gefunden.")
        figure.animation_data_create().action = action
    if args.frame_start is not None:
        scene.frame_start = args.frame_start
    engine = setup_engine(scene, args.engine, args.samples)
    camera = setup_camera(scene, args.elevation, args.ortho_scale, args.foot)
    setup_key_light(scene, camera)
    scene.render.resolution_x = scene.render.resolution_y = args.resolution
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"

    out = args.out.resolve()
    directions = DIRECTIONS
    if args.directions:
        directions = {name: DIRECTIONS[name] for name in args.directions.split(",")}
    render_pass(scene, figure, out / args.animation, args.frames, "Standard", directions)
    if args.no_normals:
        print("RENDER_DIRECTIONS_DONE preview " + args.animation)
        return
    # Outlines are an inverted hull; in the normal pass they would cover the figure.
    outlines = [m for o in bpy.data.objects for m in getattr(o, "modifiers", []) if m.name == "Outline"]
    for modifier in outlines:
        modifier.show_render = False
    scene.view_layers[0].material_override = normal_material()
    render_pass(scene, figure, out / f"{args.animation}_normal", args.frames, "Raw", directions)
    scene.view_layers[0].material_override = None
    for modifier in outlines:
        modifier.show_render = True

    info = {
        "animation": args.animation,
        "frames": args.frames,
        "fps": scene.render.fps,
        "resolution": args.resolution,
        "elevation_deg": args.elevation,
        "ortho_scale": args.ortho_scale,
        "foot": [0.5, args.foot],
        "engine": engine,
        "blender": bpy.app.version_string,
        "test_figure": args.test_figure,
        "seconds": round(time.monotonic() - started, 1),
    }
    (out / f"{args.animation}.render.json").write_text(json.dumps(info, indent=2) + "\n")
    print("RENDER_DIRECTIONS_DONE " + json.dumps(info))


main()
