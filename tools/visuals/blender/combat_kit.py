"""Combat animation for rendered figures: key poses with smooth in-betweens, legs on IK, feet
planted on the floor, and the path of a weapon point read back for the game's trails.

Combat clips are sampled by the game's own timers (progress 0..1), so an action is written as a
handful of key poses over that progress. Every channel is interpolated with a monotone cubic:
motion eases out of and into each key and never overshoots on its own; any overshoot or
follow-through is an authored key. Legs stand on IK targets, so the hips can turn, drop and
shift weight while the feet stay where they were planted, and a stepping foot travels on an
arc instead of sliding.

Used by build_player.py, build_hollow.py, build_burning.py and build_devourer.py.
"""
from __future__ import annotations

import math
from typing import Iterable

import bpy
from mathutils import Matrix, Vector

from figure_kit import Poser, best_pole_angle  # noqa: F401  (re-exported for the build scripts)

#: The renders show 66.7 world units per metre (render_directions.py at 1.5 pixels per unit).
UNITS_PER_METRE = 66.7


# ---- Curves ---------------------------------------------------------------------------------

def pchip(xs: list[float], ys: list[float], x: float) -> float:
    """Monotone cubic Hermite interpolation (Fritsch–Carlson): smooth, no overshoot between keys."""
    n = len(xs)
    if n == 1 or x <= xs[0]:
        return ys[0]
    if x >= xs[-1]:
        return ys[-1]
    h = [xs[i + 1] - xs[i] for i in range(n - 1)]
    d = [(ys[i + 1] - ys[i]) / h[i] for i in range(n - 1)]
    m = [0.0] * n
    for i in range(1, n - 1):
        if d[i - 1] * d[i] <= 0.0:
            m[i] = 0.0
        else:
            w1, w2 = 2 * h[i] + h[i - 1], h[i] + 2 * h[i - 1]
            m[i] = (w1 + w2) / (w1 / d[i - 1] + w2 / d[i])
    # Ends: a key at the start or end eases in and out, as a held pose does.
    m[0] = 0.0
    m[-1] = 0.0
    i = 0
    while x > xs[i + 1]:
        i += 1
    t = (x - xs[i]) / h[i]
    t2, t3 = t * t, t * t * t
    return ((2 * t3 - 3 * t2 + 1) * ys[i] + (t3 - 2 * t2 + t) * h[i] * m[i]
            + (-2 * t3 + 3 * t2) * ys[i + 1] + (t3 - t2) * h[i] * m[i + 1])


class Keys:
    """Key poses over progress: [(p, {"channel": value, ...}), ...]. A channel missing from a
    key is interpolated across the keys that set it; a channel set nowhere returns `default`."""

    def __init__(self, keys: Iterable[tuple[float, dict[str, float]]]):
        self.keys = sorted(keys, key=lambda key: key[0])

    def __call__(self, channel: str, p: float, default: float = 0.0) -> float:
        points = [(at, values[channel]) for at, values in self.keys if channel in values]
        if not points:
            return default
        return pchip([at for at, _ in points], [value for _, value in points], p)


def smooth(t: float) -> float:
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def window(p: float, start: float, end: float) -> float:
    """0 before `start`, 1 after `end`, smooth in between."""
    return smooth((p - start) / max(1e-6, end - start))


def impulse_drift(impulse: float, seconds: float, decay_per_second: float = 0.002) -> float:
    """How far (metres) the game moves a figure by a forward impulse that decays like
    Player._attackImpulse (factor `decay_per_second` per second): planted feet stay put in the
    world when the rig moves them back by this much."""
    k = -math.log(decay_per_second)
    return impulse / k * (1.0 - math.exp(-k * seconds)) / UNITS_PER_METRE


# ---- Legs on IK -----------------------------------------------------------------------------

LEG_IK = "IK leg"


def add_leg_ik(rig: bpy.types.Object) -> None:
    """Foot targets (children of Root, at the ankles) and knee poles in front of the knees; an
    IK constraint on each calf, off unless an action keys it on. Safe to call twice."""
    if "ik_foot_l" in rig.data.bones:
        return
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    bones = rig.data.edit_bones
    forward = Vector((0.0, -1.0, 0.0))
    for side in ("l", "r"):
        ankle = bones[f"foot_{side}"].head.copy()
        target = bones.new(f"ik_foot_{side}")
        target.head, target.tail = ankle, ankle + Vector((0.0, 0.0, 0.08))
        target.parent = bones["Root"]
        target.use_deform = False
        knee = bones[f"calf_{side}"].head.copy()
        pole = bones.new(f"ik_knee_{side}")
        pole.head = knee + forward * 0.6
        pole.tail = pole.head + Vector((0.0, 0.0, 0.05))
        pole.parent = bones["Root"]
        pole.use_deform = False
    bpy.ops.object.mode_set(mode="OBJECT")
    for side in ("l", "r"):
        constraint = rig.pose.bones[f"calf_{side}"].constraints.new("IK")
        constraint.name = LEG_IK
        constraint.target, constraint.subtarget = rig, f"ik_foot_{side}"
        constraint.pole_target, constraint.pole_subtarget = rig, f"ik_knee_{side}"
        constraint.chain_count = 2
        constraint.influence = 1.0
        constraint.pole_angle = _knee_pole_angle(rig, side, constraint)
        constraint.influence = 0.0


def _knee_pole_angle(rig: bpy.types.Object, side: str, constraint: bpy.types.Constraint) -> float:
    """Bend the leg by lifting its target, then pick the pole angle that puts the knee nearest
    its pole (in front), measured rather than assumed."""
    target = rig.pose.bones[f"ik_foot_{side}"]
    target.location = (0.0, 0.0, 0.0)
    rest = target.matrix.copy()
    target.matrix = Matrix.Translation(Vector((0.0, 0.05, 0.25))) @ rest
    pole = rig.pose.bones[f"ik_knee_{side}"]
    best, distance = 0.0, float("inf")
    for degrees in range(-180, 180, 5):
        constraint.pole_angle = math.radians(degrees)
        bpy.context.view_layer.update()
        knee = rig.pose.bones[f"calf_{side}"].head
        gap = (knee - pole.head).length
        if gap < distance:
            best, distance = math.radians(degrees), gap
    target.matrix = rest
    bpy.context.view_layer.update()
    return best


def key_legs(rig: bpy.types.Object, frame: int, weight: float) -> None:
    for side in ("l", "r"):
        constraint = rig.pose.bones[f"calf_{side}"].constraints[LEG_IK]
        constraint.influence = weight
        constraint.keyframe_insert("influence", frame=frame)


class Feet:
    """Plants both feet for one frame. Positions are the ball of each foot on the floor in rig
    space (figure faces -Y, its left along +X); yaw turns a foot about the vertical (degrees,
    counter-clockwise from above), heel lifts it about the ball, lift raises the whole foot."""

    def __init__(self, poser: Poser):
        self.poser = poser
        rig = poser.rig
        self._rest = {}
        for side in ("l", "r"):
            foot = rig.data.bones[f"foot_{side}"]
            ball = rig.data.bones[f"ball_{side}"]
            self._rest[side] = (foot.matrix_local.copy(), ball.head_local.copy())
        self._wanted: dict[str, Matrix] = {}

    def rest_ball(self, side: str) -> Vector:
        return self._rest[side][1].copy()

    def plant(self, side: str, ball: Vector, yaw: float = 0.0, heel: float = 0.0, lift: float = 0.0, knee_out: float = 0.0) -> None:
        rig = self.poser.rig
        foot_rest, ball_rest = self._rest[side]
        turn = Matrix.Rotation(math.radians(yaw), 4, "Z")
        # Lifting the heel turns the foot about the ball's lateral axis.
        lateral = (turn.to_3x3() @ Vector((1.0, 0.0, 0.0))).normalized()
        tilt = Matrix.Rotation(math.radians(heel), 4, lateral)
        rotation = tilt @ turn
        ankle_offset = foot_rest.translation - ball_rest
        ball_at = Vector((ball.x, ball.y, ball_rest.z + lift))
        ankle = ball_at + (rotation.to_3x3() @ ankle_offset)
        foot_world = Matrix.Translation(ankle) @ rotation @ Matrix.Translation(-foot_rest.translation) @ foot_rest
        self._wanted[side] = foot_world
        target = rig.pose.bones[f"ik_foot_{side}"]
        target_rest = target.bone.matrix_local
        target.matrix = Matrix.Translation(ankle) @ target_rest.to_3x3().to_4x4()
        # The knee pole sits in front of the knee, turned with the foot and pushed outward.
        pole = rig.pose.bones[f"ik_knee_{side}"]
        outward = Vector((1.0 if (side == "l") == (self.poser.left > 0) else -1.0, 0.0, 0.0))
        front = turn.to_3x3() @ Vector((0.0, -1.0, 0.0))
        knee_at = ankle + Vector((0.0, 0.0, 0.45)) + front * 0.6 + (turn.to_3x3() @ outward) * knee_out
        pole.matrix = Matrix.Translation(knee_at) @ pole.bone.matrix_local.to_3x3().to_4x4()

    def settle(self) -> None:
        """After the IK has solved: turn each foot to its planted orientation."""
        bpy.context.view_layer.update()
        for side, matrix in self._wanted.items():
            pose = self.poser.rig.pose.bones[f"foot_{side}"]
            pose.matrix = Matrix.Translation(pose.matrix.translation) @ matrix.to_3x3().to_4x4()
        bpy.context.view_layer.update()


def step_arc(p: float, start: float, end: float, a: Vector, b: Vector, height: float = 0.07) -> tuple[Vector, float]:
    """A foot travelling from `a` to `b` between progress `start` and `end`: the position on the
    floor and how high it is lifted (a smooth arc, highest in the middle)."""
    t = window(p, start, end)
    raw = max(0.0, min(1.0, (p - start) / max(1e-6, end - start)))
    return a.lerp(b, t), height * math.sin(math.pi * raw)


# ---- Body -------------------------------------------------------------------------------------

def move_pelvis(poser: Poser, offset: Vector) -> None:
    """Shift the pelvis by a rig-space vector (on top of what is already set)."""
    bpy.context.view_layer.update()
    pelvis = poser.rig.pose.bones["pelvis"]
    local = pelvis.bone.matrix_local.to_3x3().inverted() @ offset
    pelvis.location = pelvis.location + local
    bpy.context.view_layer.update()


def fit_pelvis(poser: Poser, reach: float = 0.965) -> float:
    """Lower the pelvis until both ankles are within `reach` of the full leg length, so planted
    feet are reached with a little knee bend instead of a locked, floating leg. Returns the drop."""
    rig = poser.rig
    lengths = {side: rig.data.bones[f"thigh_{side}"].length + rig.data.bones[f"calf_{side}"].length for side in ("l", "r")}
    dropped = 0.0
    for _ in range(12):
        bpy.context.view_layer.update()
        worst = 0.0
        for side in ("l", "r"):
            hip = rig.pose.bones[f"thigh_{side}"].head
            ankle = rig.pose.bones[f"ik_foot_{side}"].head
            over = (ankle - hip).length - lengths[side] * reach
            worst = max(worst, over)
        if worst <= 0.002:
            break
        move_pelvis(poser, Vector((0.0, 0.0, -worst)))
        dropped += worst
    return dropped


def twist(poser: Poser, degrees: float, shares: tuple[tuple[str, float], ...] = (("spine_01", 0.3), ("spine_02", 0.35), ("spine_03", 0.35))) -> None:
    for bone, share in shares:
        poser.turn(bone, degrees * share)


def chest_point(poser: Poser) -> Vector:
    bpy.context.view_layer.update()
    return poser.rig.pose.bones["spine_03"].head.copy()


# ---- Weapon paths -----------------------------------------------------------------------------

def bone_point(poser: Poser, bone: str, local: Vector) -> Vector:
    """A point given in a bone's own frame (head at the origin), in rig space as posed now."""
    bpy.context.view_layer.update()
    return poser.rig.pose.bones[bone].matrix @ local


def stage_coordinates(poser: Poser, point: Vector) -> tuple[float, float, float]:
    """Rig space to the figure's stage frame in metres: forward, left, up."""
    forward = poser.forward.normalized()
    left = Vector((poser.left, 0.0, 0.0))
    return (round(point.dot(forward), 4), round(point.dot(left), 4), round(point.z, 4))


# ---- Re-keying an existing figure -----------------------------------------------------------

def measure_axes(poser: Poser, pairs: Iterable[tuple[str, str]]) -> None:
    """Measure rotation axes on the rest pose with every constraint off, before any action poses
    the rig: the axes then match the ones the original build measured."""
    rig = poser.rig
    saved = []
    for pose in rig.pose.bones:
        for constraint in pose.constraints:
            saved.append((constraint, constraint.influence))
            constraint.influence = 0.0
    if rig.animation_data:
        rig.animation_data.action = None
    poser.clear()
    bpy.context.view_layer.update()
    for bone, toward in pairs:
        poser._axis(bone, toward)
    for constraint, influence in saved:
        constraint.influence = influence


def replace_action(rig: bpy.types.Object, name: str) -> bpy.types.Action:
    """A fresh, empty action under `name` (an older one of that name is removed), made active."""
    old = bpy.data.actions.get(name)
    if old is not None:
        bpy.data.actions.remove(old)
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    rig.animation_data_create().action = action
    return action


def finish_rekey(rig: bpy.types.Object, rest_action: str, out) -> None:
    """Leave the file as the build left it: the rest action active, leg IK off outside the
    actions that key it, then save."""
    rig.animation_data.action = bpy.data.actions[rest_action]
    for side in ("l", "r"):
        constraint = rig.pose.bones[f"calf_{side}"].constraints.get(LEG_IK)
        if constraint is not None:
            constraint.influence = 0.0
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(out))
