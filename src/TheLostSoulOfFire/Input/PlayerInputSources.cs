using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Input;

/// <summary>
/// Where one Warden's <see cref="PlayerCommand"/> comes from. Three concrete
/// implementations cover every case the slice needs: mouse and keyboard, a
/// gamepad, and a second keyboard layout so the owner can review co-op without
/// controller hardware. Nothing else should be added here without a real device.
/// </summary>
public interface IPlayerInputSource
{
    string ControlSummary { get; }

    PlayerCommand Read(InputState input, Camera2D camera, Viewport viewport, Vector2 wardenPosition, Vector2 lastAimPoint);
}

/// <summary>
/// Player 1's default: WASD, mouse aim, LMB Scythe, RMB Soul Cannon, Space dash,
/// Q Soul Sense, R Resonance, E stabilise. Unchanged from solo so the approved
/// feel of the Golden Slice is preserved exactly.
/// </summary>
public sealed class KeyboardMouseInput : IPlayerInputSource
{
    public string ControlSummary => "WASD MOVE / MOUSE AIM / LMB SCYTHE / RMB CANNON / SPACE DASH / Q SENSE / R RESONANCE / E STABILISE";

    public PlayerCommand Read(InputState input, Camera2D camera, Viewport viewport, Vector2 wardenPosition, Vector2 lastAimPoint)
    {
        Vector2 move = Vector2.Zero;
        if (input.IsKeyDown(Keys.W)) move.Y -= 1f;
        if (input.IsKeyDown(Keys.S)) move.Y += 1f;
        if (input.IsKeyDown(Keys.A)) move.X -= 1f;
        if (input.IsKeyDown(Keys.D)) move.X += 1f;
        if (move.LengthSquared() > 1f)
        {
            move = Vector2.Normalize(move);
        }

        // ScreenToWorld inverts the live camera transform, so aim stays correct
        // while the group camera zooms between the two brothers.
        Vector2 aim = camera.ScreenToWorld(input.MousePosition, viewport);

        return new PlayerCommand(
            move,
            aim,
            input.WasKeyPressed(Keys.Space),
            input.WasLeftMousePressed,
            input.WasRightMousePressed,
            input.IsRightMouseDown,
            input.IsKeyDown(Keys.Q),
            input.WasKeyPressed(Keys.R),
            input.IsKeyDown(Keys.E));
    }
}

/// <summary>
/// Player 2's default. Right stick aims; with no right-stick input the Warden
/// keeps aiming where he was last pointed, which stops the brother's facing from
/// snapping back every time the stick is released.
/// </summary>
public sealed class GamePadInput : IPlayerInputSource
{
    private const float AimDistance = 260f;
    private readonly PlayerIndex _index;

    public GamePadInput(PlayerIndex index) => _index = index;

    public string ControlSummary => "LEFT STICK MOVE / RIGHT STICK AIM / X SCYTHE / RT CANNON / A DASH / LT SENSE / Y RESONANCE / B STABILISE";

    public bool IsConnected => GamePad.GetState(_index).IsConnected;

    public PlayerCommand Read(InputState input, Camera2D camera, Viewport viewport, Vector2 wardenPosition, Vector2 lastAimPoint)
    {
        GamePadState pad = input.GetGamePad(_index);
        GamePadState previous = input.GetPreviousGamePad(_index);

        Vector2 move = new(pad.ThumbSticks.Left.X, -pad.ThumbSticks.Left.Y);
        move = ApplyDeadzone(move, 0.22f);

        Vector2 look = new(pad.ThumbSticks.Right.X, -pad.ThumbSticks.Right.Y);
        look = ApplyDeadzone(look, 0.28f);

        Vector2 aim = lastAimPoint;
        if (look.LengthSquared() > 0.0001f)
        {
            aim = wardenPosition + Vector2.Normalize(look) * AimDistance;
        }
        else if (move.LengthSquared() > 0.0001f)
        {
            aim = wardenPosition + Vector2.Normalize(move) * AimDistance;
        }

        bool cannonHeld = pad.Triggers.Right > 0.42f;
        bool cannonWasHeld = previous.Triggers.Right > 0.42f;

        return new PlayerCommand(
            move,
            aim,
            WasPressed(pad, previous, Buttons.A),
            WasPressed(pad, previous, Buttons.X),
            cannonHeld && !cannonWasHeld,
            cannonHeld,
            pad.Triggers.Left > 0.42f,
            WasPressed(pad, previous, Buttons.Y),
            pad.IsButtonDown(Buttons.B));
    }

    private static bool WasPressed(GamePadState current, GamePadState previous, Buttons button) =>
        current.IsButtonDown(button) && previous.IsButtonUp(button);

    private static Vector2 ApplyDeadzone(Vector2 value, float deadzone)
    {
        float length = value.Length();
        if (length <= deadzone)
        {
            return Vector2.Zero;
        }

        // Rescale past the deadzone so slow movement is still available.
        float scaled = MathHelper.Clamp((length - deadzone) / (1f - deadzone), 0f, 1f);
        return value / length * scaled;
    }
}

/// <summary>
/// Player 2 without a controller: arrow keys move, the number pad acts, and the
/// Warden aims where he is moving. It exists so the owner can review the co-op
/// design on a single keyboard; the gamepad remains the intended way to play.
/// </summary>
public sealed class SecondaryKeyboardInput : IPlayerInputSource
{
    private const float AimDistance = 260f;

    public string ControlSummary => "ARROWS MOVE+AIM / NUM1 SCYTHE / NUM2 CANNON / NUM0 DASH / NUM3 SENSE / NUM5 RESONANCE / NUM. STABILISE";

    public PlayerCommand Read(InputState input, Camera2D camera, Viewport viewport, Vector2 wardenPosition, Vector2 lastAimPoint)
    {
        Vector2 move = Vector2.Zero;
        if (input.IsKeyDown(Keys.Up)) move.Y -= 1f;
        if (input.IsKeyDown(Keys.Down)) move.Y += 1f;
        if (input.IsKeyDown(Keys.Left)) move.X -= 1f;
        if (input.IsKeyDown(Keys.Right)) move.X += 1f;
        if (move.LengthSquared() > 1f)
        {
            move = Vector2.Normalize(move);
        }

        Vector2 aim = move.LengthSquared() > 0.0001f
            ? wardenPosition + Vector2.Normalize(move) * AimDistance
            : lastAimPoint;

        return new PlayerCommand(
            move,
            aim,
            input.WasKeyPressed(Keys.NumPad0),
            input.WasKeyPressed(Keys.NumPad1),
            input.WasKeyPressed(Keys.NumPad2),
            input.IsKeyDown(Keys.NumPad2),
            input.IsKeyDown(Keys.NumPad3),
            input.WasKeyPressed(Keys.NumPad5),
            input.IsKeyDown(Keys.Decimal));
    }
}

/// <summary>
/// A command written directly by a deterministic scenario. Keeps capture fixtures
/// out of the device layer entirely.
/// </summary>
public sealed class ScriptedInput : IPlayerInputSource
{
    private PlayerCommand _command;
    private bool _hasCommand;

    public string ControlSummary => "SCRIPTED";

    public void Set(PlayerCommand command)
    {
        _command = command;
        _hasCommand = true;
    }

    public PlayerCommand Read(InputState input, Camera2D camera, Viewport viewport, Vector2 wardenPosition, Vector2 lastAimPoint)
    {
        if (!_hasCommand)
        {
            return PlayerCommand.Idle(lastAimPoint);
        }

        PlayerCommand command = _command;
        // One-shot edges are consumed so a scripted press cannot repeat forever.
        _command = command with
        {
            DashPressed = false,
            ScythePressed = false,
            CannonPressed = false,
            ResonancePressed = false
        };
        return command;
    }
}
