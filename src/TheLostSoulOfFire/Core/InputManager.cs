using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TheLostSoulOfFire.Core;

public sealed class InputManager
{
    private KeyboardState _previousKeyboard;
    private KeyboardState _currentKeyboard;
    private MouseState _previousMouse;
    private MouseState _currentMouse;

    public Vector2 MouseVirtualPosition { get; private set; }
    public bool IsLeftMouseHeld => _currentMouse.LeftButton == ButtonState.Pressed;

    public void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _previousMouse = _currentMouse;
        _currentKeyboard = Keyboard.GetState();
        _currentMouse = Mouse.GetState();
        MouseVirtualPosition = GameCore.Resolution.WindowToVirtual(new Point(_currentMouse.X, _currentMouse.Y));
    }

    public bool IsKeyHeld(Keys key) => _currentKeyboard.IsKeyDown(key);
    public bool IsKeyPressed(Keys key) => _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    public bool IsKeyReleased(Keys key) => _currentKeyboard.IsKeyUp(key) && _previousKeyboard.IsKeyDown(key);

    public Vector2 GetMovement()
    {
        float x = 0f;
        float y = 0f;
        if (IsKeyHeld(Keys.A) || IsKeyHeld(Keys.Left)) x -= 1f;
        if (IsKeyHeld(Keys.D) || IsKeyHeld(Keys.Right)) x += 1f;
        if (IsKeyHeld(Keys.W) || IsKeyHeld(Keys.Up)) y -= 1f;
        if (IsKeyHeld(Keys.S) || IsKeyHeld(Keys.Down)) y += 1f;
        Vector2 movement;
        movement.X = x;
        movement.Y = y;
        if (movement.LengthSquared() > 1f) movement.Normalize();
        return movement;
    }
}
