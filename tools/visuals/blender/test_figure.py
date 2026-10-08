"""A simple procedural test figure for checking the render pipeline (task 6.3).

Imported by render_directions.py when it runs with --test-figure; not game art. The figure
stands on the world origin (its foot point), faces -Y (toward the camera, direction "s") and
carries a staff on its right so turning is readable. Its "idle" animation bobs and sways
over --frames frames.
"""
from __future__ import annotations

import math

import bpy


def _material(name: str, colour: tuple[float, float, float], roughness: float = 0.8) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*colour, 1.0)
    shader.inputs["Roughness"].default_value = roughness
    return material


def _add(kind: str, material: bpy.types.Material, **kwargs) -> bpy.types.Object:
    if kind == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(vertices=24, **kwargs)
    elif kind == "sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, **kwargs)
    else:
        bpy.ops.mesh.primitive_cube_add(**kwargs)
    obj = bpy.context.active_object
    obj.data.materials.append(material)
    bpy.ops.object.shade_smooth()
    return obj


def build(frames: int) -> bpy.types.Object:
    """Builds the figure under one empty and returns that empty (rotate it to turn the figure)."""
    cloth = _material("cloth", (0.06, 0.055, 0.07))
    skin = _material("skin", (0.55, 0.42, 0.36), 0.6)
    iron = _material("iron", (0.22, 0.22, 0.25), 0.45)
    accent = _material("accent", (0.45, 0.04, 0.1), 0.5)  # carmine, the one saturated accent

    root = bpy.data.objects.new("figure", None)
    bpy.context.collection.objects.link(root)

    body = _add("cylinder", cloth, radius=0.32, depth=1.15, location=(0, 0, 0.78))
    body.scale = (1.0, 0.75, 1.0)
    head = _add("sphere", skin, radius=0.2, location=(0, -0.02, 1.55))
    legs = [_add("cylinder", cloth, radius=0.09, depth=0.45, location=(side * 0.13, 0, 0.22)) for side in (-1, 1)]
    staff = _add("cylinder", iron, radius=0.035, depth=1.7, location=(0.42, -0.12, 0.9))
    staff.rotation_euler = (0.0, math.radians(-12), 0.0)
    blade = _add("cube", iron, size=1.0, location=(0.52, -0.12, 1.72))
    blade.scale = (0.32, 0.03, 0.08)
    compass = _add("sphere", accent, radius=0.06, location=(-0.12, -0.27, 1.12))

    for part in [body, head, *legs, staff, blade, compass]:
        part.parent = root

    # Idle: a gentle bob and sway, looping over the frame range.
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = frames
    for frame in range(1, frames + 2):
        phase = (frame - 1) / frames * math.tau
        for part, amount in ((body, 0.025), (head, 0.035), (staff, 0.02), (blade, 0.02), (compass, 0.025)):
            base = part.get("base_z", part.location.z)
            part["base_z"] = base
            part.location.z = base + math.sin(phase) * amount
            part.keyframe_insert("location", index=2, frame=frame)
        head.rotation_euler.z = math.sin(phase) * 0.06
        head.keyframe_insert("rotation_euler", index=2, frame=frame)
    return root
