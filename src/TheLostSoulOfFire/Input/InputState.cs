using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Core;

namespace TheLostSoulOfFire.Input;

public sealed class InputState
{
    private KeyboardState _previousKeyboard;
    private KeyboardState _keyboard;
    private MouseState _previousMouse;
    private MouseState _mouse;
    private readonly HashSet<Keys> _injectedPresses = [];

    public Point MousePosition => _mouse.Position;

    /// <summary>
    /// The pointer position mapped from window coordinates into the game's virtual
    /// resolution. All gameplay- and menu-facing hit tests should use this instead of
    /// <see cref="MousePosition"/> so that aiming and clicking stay accurate at any
    /// window size.
    /// </summary>
    public Vector2 MouseVirtualPosition { get; private set; }

    public bool MouseMoved => _mouse.Position != _previousMouse.Position;
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

    public void Update(ResolutionManager resolution)
    {
        _injectedPresses.Clear();
        _previousKeyboard = _keyboard;
        _previousMouse = _mouse;
        _keyboard = Keyboard.GetState();
        _mouse = Mouse.GetState();
        MouseVirtualPosition = resolution.WindowToVirtual(_mouse.Position);
    }

    public bool IsKeyDown(Keys key) => _keyboard.IsKeyDown(key);

    public bool WasKeyPressed(Keys key) =>
        _injectedPresses.Contains(key) || _keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

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

    internal void InjectKeyPress(Keys key) => _injectedPresses.Add(key);
}
