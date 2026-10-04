using System;
using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// A money chest left after a cleared wave. It opens once with <c>E</c>, credits its Geld at
/// that moment and disappears after a short opening animation.
/// </summary>
public sealed class ArenaChest
{
    private float _openTimer;

    public ArenaChest(Vector2 position) => Position = position;

    public Vector2 Position { get; }
    public bool IsOpened { get; private set; }
    public bool IsGone => IsOpened && _openTimer >= GameBalance.ChestOpenDuration;
    public float OpenProgress => IsOpened ? MathHelper.Clamp(_openTimer / GameBalance.ChestOpenDuration, 0f, 1f) : 0f;

    public bool IsInReach(Vector2 position) =>
        !IsOpened && Vector2.DistanceSquared(position, Position) <= GameBalance.ChestInteractRadius * GameBalance.ChestInteractRadius;

    /// <summary>Opens the chest; true exactly once.</summary>
    public bool TryOpen()
    {
        if (IsOpened)
        {
            return false;
        }

        IsOpened = true;
        _openTimer = 0f;
        return true;
    }

    public void Update(float deltaTime)
    {
        if (IsOpened)
        {
            _openTimer = MathF.Min(GameBalance.ChestOpenDuration, _openTimer + deltaTime);
        }
    }

    /// <summary>Fixed spot per wave inside the combat area so unopened chests never overlap.</summary>
    public static Vector2 PositionForWave(int waveNumber, Rectangle combatBounds) => waveNumber switch
    {
        3 => new Vector2(combatBounds.Left + combatBounds.Width * 0.22f, combatBounds.Top + combatBounds.Height * 0.3f),
        6 => new Vector2(combatBounds.Left + combatBounds.Width * 0.78f, combatBounds.Top + combatBounds.Height * 0.3f),
        _ => new Vector2(combatBounds.Left + combatBounds.Width * 0.5f, combatBounds.Top + combatBounds.Height * 0.78f)
    };
}
