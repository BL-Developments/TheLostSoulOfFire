using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TheLostSoulOfFire.Input;

public sealed class InputState
{
    private KeyboardState _previousKeyboard;
    private KeyboardState _keyboard;
    private MouseState _previousMouse;
    private MouseState _mouse;
    private readonly HashSet<Keys> _injectedPresses = [];
    private readonly HashSet<Keys> _injectedDown = [];
    private bool _injectedLeftMouse;
    private bool _injectedRightMouse;

    public Point MousePosition => _mouse.Position;
    public bool AnyInputPressed
    {
        get
        {
            if (_injectedPresses.Count > 0)
            {
                return true;
            }
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
        _injectedPresses.Clear();
        _injectedDown.Clear();
        _injectedLeftMouse = false;
        _injectedRightMouse = false;
        _previousKeyboard = _keyboard;
        _previousMouse = _mouse;
        _keyboard = Keyboard.GetState();
        _mouse = Mouse.GetState();
    }

    public bool IsKeyDown(Keys key) => _injectedDown.Contains(key) || _keyboard.IsKeyDown(key);

    public bool WasKeyPressed(Keys key) =>
        _injectedPresses.Contains(key) || _keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

    public bool WasKeyReleased(Keys key) =>
        _keyboard.IsKeyUp(key) && _previousKeyboard.IsKeyDown(key);

    public bool IsLeftMouseDown => _injectedLeftMouse || _mouse.LeftButton == ButtonState.Pressed;
    public bool WasLeftMousePressed =>
        _injectedLeftMouse || _mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
    public bool WasLeftMouseReleased =>
        _mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed;
    public bool IsRightMouseDown => _injectedRightMouse || _mouse.RightButton == ButtonState.Pressed;
    public bool WasRightMousePressed =>
        _injectedRightMouse || _mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;
    public bool WasRightMouseReleased =>
        _mouse.RightButton == ButtonState.Released && _previousMouse.RightButton == ButtonState.Pressed;

    internal void InjectKeyPress(Keys key) => _injectedPresses.Add(key);

    internal void InjectKeyDown(Keys key) => _injectedDown.Add(key);

    internal void InjectMousePresses(bool left, bool right)
    {
        _injectedLeftMouse = left;
        _injectedRightMouse = right;
    }
}
