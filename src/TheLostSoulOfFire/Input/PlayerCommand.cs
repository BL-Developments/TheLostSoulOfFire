using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Input;

/// <summary>
/// One Warden's intent for one frame, already resolved out of whatever device
/// produced it.
///
/// Everything downstream — movement, dash, Scythe, Cannon, Soul Sense, Resonance,
/// stabilisation — reads this and nothing else, so a keyboard, a gamepad or a
/// deterministic capture fixture are interchangeable without any of them knowing
/// about each other. This is deliberately a value type with no device state: it
/// is not a networking abstraction and should not grow into one.
/// </summary>
public readonly record struct PlayerCommand(
    Vector2 Move,
    Vector2 AimPoint,
    bool DashPressed,
    bool ScythePressed,
    bool CannonPressed,
    bool CannonHeld,
    bool SenseHeld,
    bool ResonancePressed,
    bool StabilizeHeld)
{
    public static PlayerCommand Idle(Vector2 aimPoint) => new(
        Vector2.Zero, aimPoint, false, false, false, false, false, false, false);

    public bool CannonReleased => !CannonHeld;
}
