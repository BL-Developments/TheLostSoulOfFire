# MonoGame UI Focus Reference

Keyboard and gamepad navigation templates. All classes use `YourGame.UI` namespace.

## Table of Contents
1. [UIElement.Focus.cs — partial class extension](#uielementfocuscs)
2. [UIFocusManager — focus and navigation](#uifocusmanager)
3. [UIKeyboardEventArgs](#uikeyboardeventargs)
4. [AssignGridNeighbors — auto-fill grid IDs](#assigngridneighbors)
5. [Gamepad navigation update loop](#gamepad-navigation-update-loop)

---

## UIElement.Focus.cs

```csharp
// UIElement.Focus.cs
// Extends UIElement defined in UIElement.Core.cs (monogame-ui-core skill).

using Microsoft.Xna.Framework;

namespace YourGame.UI
{
    public abstract partial class UIElement
    {
        // Unique ID for this element within the current menu/screen.
        // -1 = not registered for gamepad navigation.
        public int MyId            { get; set; } = -1;

        // Neighbor IDs for D-Pad navigation. -1 = no neighbor in that direction.
        public int UpNeighborId    { get; set; } = -1;
        public int DownNeighborId  { get; set; } = -1;
        public int LeftNeighborId  { get; set; } = -1;
        public int RightNeighborId { get; set; } = -1;

        // Whether this element can receive focus.
        public bool IsFocusable { get; set; } = false;

        // Called when this element gains keyboard/gamepad focus.
        public virtual void OnFocusGained() { }

        // Called when this element loses focus.
        public virtual void OnFocusLost() { }

        // Called when focused and a keyboard event fires.
        public virtual void HandleKeyboardInput(UIKeyboardEventArgs args) { }
    }
}
```

---

## UIFocusManager

```csharp
// UIFocusManager.cs
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace YourGame.UI
{
    public static class UIFocusManager
    {
        public static UIElement FocusedElement { get; private set; }

        // O(1) lookup by MyId.
        private static readonly Dictionary<int, UIElement> _registry
            = new Dictionary<int, UIElement>();

        // Ordered list for Tab navigation.
        private static readonly List<UIElement> _tabOrder
            = new List<UIElement>();

        // --- Registration ---

        public static void Register(UIElement element)
        {
            if (element.MyId >= 0)
                _registry[element.MyId] = element;

            if (element.IsFocusable && !_tabOrder.Contains(element))
                _tabOrder.Add(element);
        }

        public static void Unregister(UIElement element)
        {
            if (element.MyId >= 0)
                _registry.Remove(element.MyId);
            _tabOrder.Remove(element);
        }

        // Call this when changing screens to wipe state.
        public static void Clear()
        {
            SetFocus(null);
            _registry.Clear();
            _tabOrder.Clear();
        }

        // --- Focus transitions ---

        public static void SetFocus(UIElement element)
        {
            if (FocusedElement == element) return;

            if (FocusedElement != null)
                FocusedElement.OnFocusLost();

            FocusedElement = element;

            if (FocusedElement != null)
                FocusedElement.OnFocusGained();
        }

        // --- Tab order navigation ---

        public static void FocusNext()
        {
            if (_tabOrder.Count == 0) return;
            int index = FocusedElement != null ? _tabOrder.IndexOf(FocusedElement) : -1;
            index = (index + 1) % _tabOrder.Count;
            SetFocus(_tabOrder[index]);
        }

        public static void FocusPrev()
        {
            if (_tabOrder.Count == 0) return;
            int index = FocusedElement != null ? _tabOrder.IndexOf(FocusedElement) : 0;
            index = (index - 1 + _tabOrder.Count) % _tabOrder.Count;
            SetFocus(_tabOrder[index]);
        }

        // --- D-Pad navigation ---

        public static void MoveFocus(NavDirection direction)
        {
            if (FocusedElement == null) return;

            int neighborId = direction switch
            {
                NavDirection.Up    => FocusedElement.UpNeighborId,
                NavDirection.Down  => FocusedElement.DownNeighborId,
                NavDirection.Left  => FocusedElement.LeftNeighborId,
                NavDirection.Right => FocusedElement.RightNeighborId,
                _                  => -1
            };

            if (neighborId >= 0 && _registry.TryGetValue(neighborId, out UIElement neighbor))
                SetFocus(neighbor);
        }
    }

    public enum NavDirection { Up, Down, Left, Right }
}
```

---

## UIKeyboardEventArgs

```csharp
// UIKeyboardEventArgs.cs
using Microsoft.Xna.Framework.Input;

namespace YourGame.UI
{
    public class UIKeyboardEventArgs
    {
        public Keys Key;
        public bool Handled;

        public void Reset(Keys key)
        {
            Key     = key;
            Handled = false;
        }
    }
}
```

---

## AssignGridNeighbors

Auto-fills all neighbor IDs for a 2D grid (inventory slots, skill tree, button grid). IDs are assigned row-major starting from `startId`.

```csharp
// UIFocusGridHelper.cs
namespace YourGame.UI
{
    public static class UIFocusGridHelper
    {
        // elements[row, col] — row 0 is top, col 0 is left.
        // startId: first ID assigned; IDs are sequential row-major.
        // wrapH: wrap left/right edges horizontally.
        // wrapV: wrap top/bottom edges vertically.
        public static void AssignGridNeighbors(UIElement[,] elements,
            int startId = 0, bool wrapH = false, bool wrapV = false)
        {
            int rows = elements.GetLength(0);
            int cols = elements.GetLength(1);

            // Assign IDs first.
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                elements[r, c].MyId = startId + r * cols + c;

            // Assign neighbors.
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                UIElement el = elements[r, c];

                // Up
                if (r > 0)
                    el.UpNeighborId = elements[r - 1, c].MyId;
                else if (wrapV)
                    el.UpNeighborId = elements[rows - 1, c].MyId;

                // Down
                if (r < rows - 1)
                    el.DownNeighborId = elements[r + 1, c].MyId;
                else if (wrapV)
                    el.DownNeighborId = elements[0, c].MyId;

                // Left
                if (c > 0)
                    el.LeftNeighborId = elements[r, c - 1].MyId;
                else if (wrapH)
                    el.LeftNeighborId = elements[r, cols - 1].MyId;

                // Right
                if (c < cols - 1)
                    el.RightNeighborId = elements[r, c + 1].MyId;
                else if (wrapH)
                    el.RightNeighborId = elements[r, 0].MyId;

                // Register with focus manager.
                UIFocusManager.Register(el);
            }
        }
    }
}
```

---

## Gamepad Navigation Update Loop

```csharp
// In UIManager or Game.Update — after polling prev/curr GamePadState:

private GamePadState _prevPad;
private GamePadState _currPad;
private readonly UIKeyboardEventArgs _kbArgs = new UIKeyboardEventArgs();
private readonly UIPointerEventArgs  _ptrArgs = new UIPointerEventArgs();

private void UpdateFocusInput()
{
    _prevPad = _currPad;
    _currPad = GamePad.GetState(PlayerIndex.One);

    if (!_currPad.IsConnected) return;

    bool up    = _currPad.DPad.Up    == ButtonState.Pressed && _prevPad.DPad.Up    == ButtonState.Released;
    bool down  = _currPad.DPad.Down  == ButtonState.Pressed && _prevPad.DPad.Down  == ButtonState.Released;
    bool left  = _currPad.DPad.Left  == ButtonState.Pressed && _prevPad.DPad.Left  == ButtonState.Released;
    bool right = _currPad.DPad.Right == ButtonState.Pressed && _prevPad.DPad.Right == ButtonState.Released;

    if (up)    UIFocusManager.MoveFocus(NavDirection.Up);
    if (down)  UIFocusManager.MoveFocus(NavDirection.Down);
    if (left)  UIFocusManager.MoveFocus(NavDirection.Left);
    if (right) UIFocusManager.MoveFocus(NavDirection.Right);

    // A button = confirm (synthesize a click on focused element)
    bool confirm = _currPad.Buttons.A == ButtonState.Pressed
                && _prevPad.Buttons.A == ButtonState.Released;

    if (confirm && UIFocusManager.FocusedElement != null)
    {
        _ptrArgs.Reset(UIFocusManager.FocusedElement.Bounds.Center);
        UIFocusManager.FocusedElement.OnClick(_ptrArgs);
    }

    // Keyboard Tab navigation
    var kb     = Keyboard.GetState();
    var prevKb = _prevKeyboard;  // cached prev frame keyboard state
    bool tab      = kb.IsKeyDown(Keys.Tab)   && !prevKb.IsKeyDown(Keys.Tab);
    bool shiftTab = tab && (kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift));

    if (tab && !shiftTab) UIFocusManager.FocusNext();
    if (shiftTab)         UIFocusManager.FocusPrev();

    // Route other keys to focused element
    if (UIFocusManager.FocusedElement != null)
    {
        Keys[] pressed = kb.GetPressedKeys();
        for (int i = 0; i < pressed.Length; i++)
        {
            if (!prevKb.IsKeyDown(pressed[i]))
            {
                _kbArgs.Reset(pressed[i]);
                UIFocusManager.FocusedElement.HandleKeyboardInput(_kbArgs);
            }
        }
    }
}
```
