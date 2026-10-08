using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// A placed prop or environment piece: a Visual-ID standing on a foot point. Its drawing band
/// comes from the registry; occluders fade while they hide the player, an enemy or a telegraph.
/// </summary>
public sealed class SceneProp(string visualId, Vector2 foot, Vector2 fallbackSize, SceneLayer fallbackLayer)
{
    public string VisualId { get; } = visualId;
    public Vector2 Foot { get; } = foot;
    public Vector2 FallbackSize { get; } = fallbackSize;
    public SceneLayer FallbackLayer { get; } = fallbackLayer;
    public float Alpha { get; set; } = 1f;
}
