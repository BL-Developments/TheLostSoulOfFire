using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// The fixed drawing order of a scene, back to front (VISUAL-ART-DIRECTION §4). Actors and
/// high props share one band and are sorted by foot point among themselves.
/// </summary>
public enum SceneLayer
{
    FarBackground,
    Background,
    Ground,
    GroundDetail,
    LowProp,
    /// <summary>Player, enemies and high props, sorted by foot point.</summary>
    Actor,
    HighProp,
    Occluder,
    Foreground,
    Atmosphere
}

/// <summary>Something drawn in the actor band, ordered by where it touches the floor.</summary>
public readonly record struct DepthItem(float FootY, int Order, Action Draw);

public static class DepthSort
{
    /// <summary>
    /// Sorts back to front: a smaller foot y is further away. Equal feet keep their submission
    /// order, so the result never flickers between frames.
    /// </summary>
    public static void Sort(List<DepthItem> items) =>
        items.Sort(static (a, b) => a.FootY != b.FootY ? a.FootY.CompareTo(b.FootY) : a.Order.CompareTo(b.Order));
}

/// <summary>
/// Decides when an occluder must turn see-through: a foreground frame or occluder whenever it
/// overlaps the player, an enemy or a telegraph; a high prop only when that thing stands behind
/// it (its foot point is further up the screen than the prop's).
/// </summary>
public static class OccluderFade
{
    public const float SeeThroughAlpha = 0.32f;
    public const float FadePerSecond = 5f;

    public static bool Hides(RectangleF occluder, float occluderFootY, SceneLayer layer, RectangleF target, float targetFootY)
    {
        if (!occluder.Intersects(target))
        {
            return false;
        }

        return layer is SceneLayer.Occluder or SceneLayer.Foreground || targetFootY < occluderFootY;
    }

    public static float TargetAlpha(RectangleF occluder, float occluderFootY, SceneLayer layer, IEnumerable<(RectangleF Bounds, float FootY)> important)
    {
        foreach ((RectangleF bounds, float footY) in important)
        {
            if (Hides(occluder, occluderFootY, layer, bounds, footY))
            {
                return SeeThroughAlpha;
            }
        }

        return 1f;
    }

    /// <summary>Moves <paramref name="current"/> toward <paramref name="target"/> at a fixed rate, without overshoot.</summary>
    public static float Approach(float current, float target, float deltaTime)
    {
        float step = FadePerSecond * MathF.Max(0f, deltaTime);
        return current < target ? MathF.Min(target, current + step) : MathF.Max(target, current - step);
    }
}

/// <summary>A float rectangle; MonoGame's <see cref="Rectangle"/> would round world positions.</summary>
public readonly record struct RectangleF(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public static RectangleF Around(Vector2 center, Vector2 size) => new(center.X - size.X * 0.5f, center.Y - size.Y * 0.5f, size.X, size.Y);

    /// <summary>A rectangle standing on <paramref name="foot"/>, positioned by a normalised origin.</summary>
    public static RectangleF FromFoot(Vector2 foot, Vector2 size, Vector2 origin) => new(foot.X - origin.X * size.X, foot.Y - origin.Y * size.Y, size.X, size.Y);

    public bool Intersects(RectangleF other) =>
        X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;
}
