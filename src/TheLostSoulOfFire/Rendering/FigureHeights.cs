using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Screen heights above the foot point of a rendered figure, in world units. The renders use an
/// orthographic camera 35° above the floor at 66.7 world units per metre
/// (tools/visuals/blender/render_directions.py), so a point h metres up appears
/// h · cos 35° · 66.7 units above the feet. Gameplay positions are foot points.
/// </summary>
public static class FigureHeights
{
    /// <summary>Hands holding the scythe level, about 1.0 m up.</summary>
    public const float Hold = 55f;

    /// <summary>The Death Flame core under the sternum, about 1.3 m up.</summary>
    public const float Core = 71f;

    /// <summary>The eyes, about 1.66 m up.</summary>
    public const float Eyes = 91f;

    /// <summary>The raised Soul Cannon's muzzle, about 1.37 m up ...</summary>
    public const float Muzzle = 75f;

    /// <summary>... and 0.85 m ahead of the feet (level distance, before squashing).</summary>
    public const float MuzzleReach = 57f;

    /// <summary>
    /// Shots, sparks and slashes are drawn this far above their gameplay position, so they fly
    /// and hit at body height (chest to shoulder) instead of along the floor.
    /// </summary>
    public const float Air = 70f;

    /// <summary>How much a level circle is squashed vertically when seen from the camera (sin 35°).</summary>
    public const float LevelSquash = 0.57f;

    /// <summary>
    /// Where the chest of a fallen figure lies (death clip): about 0.75 m ahead of the feet along
    /// the facing, a hand's breadth above the floor.
    /// </summary>
    public static Vector2 FallenChestOf(Vector2 foot, Vector2 facing) =>
        foot + new Vector2(facing.X * 50f, facing.Y * 50f * LevelSquash - 8f);

    /// <summary>Screen position of the raised cannon's muzzle for a figure on <paramref name="foot"/> aiming along <paramref name="facing"/>.</summary>
    public static Vector2 MuzzleOf(Vector2 foot, Vector2 facing) =>
        foot + new Vector2(facing.X * MuzzleReach, facing.Y * MuzzleReach * LevelSquash - Muzzle);
}
