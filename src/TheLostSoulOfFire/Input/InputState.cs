using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TheLostSoulOfFire.Input;

public sealed class InputState
{
    private KeyboardState _previousKeyboard;
    private KeyboardState _keyboard;
    private MouseState _previousMouse;
    private MouseState _mouse;

    public Point MousePosition => _mouse.Position;
    public bool AnyInputPressed
    {
        get
        {
            foreach (Keys key in _keyboard.GetPressedKeys())
            {
                if (_previousKeyboard.IsKeyUp(key))
                {
                    return true;
                }
            }

            return WasLeftMousePressed || WasRightMousePressed;
        }
    }

    public void Update()
    {
        _previousKeyboard = _keyboard;
        _previousMouse = _mouse;
        _keyboard = Keyboard.GetState();
        _mouse = Mouse.GetState();
    }

    public bool IsKeyDown(Keys key) => _keyboard.IsKeyDown(key);

    public bool WasKeyPressed(Keys key) =>
        _keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

    public bool WasKeyReleased(Keys key) =>
        _keyboard.IsKeyUp(key) && _previousKeyboard.IsKeyDown(key);

    public bool IsLeftMouseDown => _mouse.LeftButton == ButtonState.Pressed;
    public bool WasLeftMousePressed =>
        _mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
    public bool WasLeftMouseReleased =>
        _mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed;
    public bool IsRightMouseDown => _mouse.RightButton == ButtonState.Pressed;
    public bool WasRightMousePressed =>
        _mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;
    public bool WasRightMouseReleased =>
        _mouse.RightButton == ButtonState.Released && _previousMouse.RightButton == ButtonState.Pressed;
}
