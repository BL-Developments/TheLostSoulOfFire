# MonoGame UI Interaction Reference

Hit testing, event bubbling, and hover state templates. All classes use `YourGame.UI` namespace.

## Table of Contents
1. [UIPointerEventArgs](#uipointereventargs)
2. [HitTest — DFS reverse traversal](#hittest--dfs-reverse-traversal)
3. [BubbleEvent — propagation up the tree](#bubbleevent--propagation-up-the-tree)
4. [UIInteractionManager — root input coordinator](#uiinteractionmanager--root-input-coordinator)
5. [UIElement virtual pointer event hooks](#uielement-virtual-pointer-event-hooks)

---

## UIPointerEventArgs

```csharp
// UIPointerEventArgs.cs
using Microsoft.Xna.Framework;

namespace YourGame.UI
{
    public class UIPointerEventArgs
    {
        public Point Position;
        public bool  Handled;

        // Reuse this instance per frame — do NOT allocate new every frame.
        public void Reset(Point position)
        {
            Position = position;
            Handled  = false;
        }
    }
}
```

---

## HitTest — DFS Reverse Traversal

```csharp
// Returns the deepest visible, enabled UIElement whose Bounds contains 'point'.
// Returns null if no element is hit.
// Indexed reverse for-loop — no allocations.
public static UIElement HitTest(UIElement root, Point point)
{
    if (!root.IsVisible || !root.IsEnabled) return null;
    if (!root.Bounds.Contains(point))       return null;

    // Check children last-to-first (topmost drawn = tested first).
    for (int i = root.Children.Count - 1; i >= 0; i--)
    {
        UIElement hit = HitTest(root.Children[i], point);
        if (hit != null) return hit;
    }

    // No child was hit — this node is the target.
    return root;
}
```

---

## BubbleEvent — Propagation Up the Tree

```csharp
// Fires 'action' on 'target', then walks up Parent chain until Handled == true or root reached.
public static void BubbleEvent(UIElement target, UIPointerEventArgs args,
    System.Action<UIElement, UIPointerEventArgs> action)
{
    UIElement current = target;
    while (current != null && !args.Handled)
    {
        action(current, args);
        current = current.Parent;
    }
}

// Usage:
// BubbleEvent(hitTarget, args, (el, a) => el.OnPointerDown(a));
```

---

## UIInteractionManager — Root Input Coordinator

```csharp
// UIInteractionManager.cs
// Attach one of these to your root UIContainer.
// Requires the prev/curr MouseState pattern from monogame-input.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace YourGame.UI
{
    public class UIInteractionManager
    {
        private readonly UIElement _root;

        private MouseState _prevMouse;
        private MouseState _currMouse;

        private UIElement _hoveredElement;
        private UIElement _pressedElement;

        // Reused every frame — no allocation.
        private readonly UIPointerEventArgs _eventArgs = new UIPointerEventArgs();

        public UIInteractionManager(UIElement root)
        {
            _root = root;
            _prevMouse = Mouse.GetState();
            _currMouse = _prevMouse;
        }

        public void Update()
        {
            _prevMouse = _currMouse;
            _currMouse = Mouse.GetState();

            Point mousePos = new Point(_currMouse.X, _currMouse.Y);

            // --- Hover ---
            UIElement newHover = HitTestHelper.HitTest(_root, mousePos);

            if (newHover != _hoveredElement)
            {
                if (_hoveredElement != null)
                {
                    _eventArgs.Reset(mousePos);
                    _hoveredElement.OnPointerLeave(_eventArgs);
                    _hoveredElement.IsHovered = false;
                }
                if (newHover != null)
                {
                    _eventArgs.Reset(mousePos);
                    newHover.OnPointerEnter(_eventArgs);
                    newHover.IsHovered = true;
                }
                _hoveredElement = newHover;
            }

            bool leftJustPressed  = _currMouse.LeftButton == ButtonState.Pressed
                                 && _prevMouse.LeftButton == ButtonState.Released;
            bool leftJustReleased = _currMouse.LeftButton == ButtonState.Released
                                 && _prevMouse.LeftButton == ButtonState.Pressed;

            // --- Press ---
            if (leftJustPressed && newHover != null)
            {
                _pressedElement = newHover;
                _eventArgs.Reset(mousePos);
                BubbleHelper.BubbleEvent(newHover, _eventArgs, (el, a) => el.OnPointerDown(a));
            }

            // --- Release + Click ---
            if (leftJustReleased)
            {
                if (newHover != null)
                {
                    _eventArgs.Reset(mousePos);
                    BubbleHelper.BubbleEvent(newHover, _eventArgs, (el, a) => el.OnPointerUp(a));

                    if (_pressedElement == newHover)
                    {
                        _eventArgs.Reset(mousePos);
                        BubbleHelper.BubbleEvent(newHover, _eventArgs, (el, a) => el.OnClick(a));
                    }
                }
                _pressedElement = null;
            }
        }
    }

    // Thin static wrappers so the logic above is readable.
    internal static class HitTestHelper
    {
        public static UIElement HitTest(UIElement root, Point point)
        {
            if (!root.IsVisible || !root.IsEnabled) return null;
            if (!root.Bounds.Contains(point))       return null;
            for (int i = root.Children.Count - 1; i >= 0; i--)
            {
                UIElement hit = HitTest(root.Children[i], point);
                if (hit != null) return hit;
            }
            return root;
        }
    }

    internal static class BubbleHelper
    {
        public static void BubbleEvent(UIElement target, UIPointerEventArgs args,
            System.Action<UIElement, UIPointerEventArgs> action)
        {
            UIElement current = target;
            while (current != null && !args.Handled)
            {
                action(current, args);
                current = current.Parent;
            }
        }
    }
}
```

---

## UIElement Virtual Pointer Event Hooks

Add these virtual methods to `UIElement.Core.cs` (or a separate partial file):

```csharp
// In UIElement (partial class — add to UIElement.Core.cs or a new UIElement.Interaction.cs)

// Whether the cursor is currently over this element.
public bool IsHovered { get; internal set; }

public virtual void OnPointerEnter(UIPointerEventArgs args) { }
public virtual void OnPointerLeave(UIPointerEventArgs args) { }
public virtual void OnPointerDown(UIPointerEventArgs args)  { }
public virtual void OnPointerUp(UIPointerEventArgs args)    { }
public virtual void OnClick(UIPointerEventArgs args)        { }
```

Override in concrete components:

```csharp
// Example: Button click handler
public override void OnClick(UIPointerEventArgs args)
{
    Clicked?.Invoke(this);
    args.Handled = true;   // stop bubbling
}

public event System.Action<UIButton> Clicked;
```
