---
name: monogame-ui-focus
description: MonoGame UI focus system and gamepad navigation — FocusManager singleton, keyboard input routing to the focused element, D-Pad neighbor-ID navigation, tab order, and grid auto-assignment. Use this skill whenever the user asks about keyboard navigation in menus, gamepad controller UI navigation, focus management, tab order, D-Pad menu navigation, "how do I navigate my menu with a controller", neighbor IDs, or any question about which UI element receives keyboard/gamepad input in MonoGame.
---

# MonoGame UI Focus — Keyboard & Gamepad Navigation

This skill covers how keyboard and gamepad input reaches the correct UI element. For mouse/pointer handling, see `monogame-ui-interaction`. For input polling fundamentals, see `monogame-input`. For complete code templates, read `references/ui-focus.md`.

## Assumed Contract

> **UIElement.Bounds must be resolved (via `monogame-ui-layout` Arrange pass) before gamepad navigation warps the visual cursor** to the focused element's center. If Bounds is empty, navigation still works logically — only the visual cursor position will be wrong.
>
> This skill uses the prev/curr GamePadState pattern from `monogame-input`. Do not call `GamePad.GetState()` inside individual element code.

`UIElement.Focus.cs` is the third partial file of `UIElement`, adding the neighbor-ID fields alongside the structural fields (Core) and layout fields (Layout).

## The Focus Problem

Mouse input is positional — hit testing resolves the target automatically. Keyboard and gamepad input is not positional: you need a global reference that says "this element currently receives key presses."

Without a focus system, every element would poll every key every frame, and Tab navigation would be impossible.

## UIFocusManager

A single static class (or a singleton) owns:
- `FocusedElement` — the currently focused `UIElement` (null = nothing focused)
- `SetFocus(element)` — transitions focus: calls `OnFocusLost` on old, `OnFocusGained` on new
- `MoveFocus(direction)` — reads the focused element's neighbor ID and transfers focus to that element

Keyboard events are routed exclusively to `FocusedElement`:

```csharp
// In your root Update, after input polling:
if (UIFocusManager.FocusedElement != null)
    UIFocusManager.FocusedElement.HandleKeyboardInput(keyboardArgs);
```

## Tab Order

Tab order is a flat list of focusable elements maintained in `UIFocusManager.TabOrder`. When Tab is pressed:

```
FocusNext() → finds current index in TabOrder → focus index+1 (wraps to 0)
FocusPrev() → focus index-1 (wraps to Count-1)
```

Register elements in logical reading order (left→right, top→bottom). Re-register when the screen changes.

## Gamepad Navigation: Neighbor-ID System

Each `UIElement` carries four integer neighbor IDs (stored in `UIElement.Focus.cs`):

| Field | Default | Meaning |
|-------|---------|---------|
| `MyId` | -1 (unset) | Unique ID for this element |
| `UpNeighborId` | -1 | ID of the element reached by pressing D-Pad Up |
| `DownNeighborId` | -1 | ID of element reached by D-Pad Down |
| `LeftNeighborId` | -1 | ID of element reached by D-Pad Left |
| `RightNeighborId` | -1 | ID of element reached by D-Pad Right |

`UIFocusManager` keeps a `Dictionary<int, UIElement>` populated via `Register(element)`. On D-Pad press:

```csharp
int neighborId = FocusedElement.RightNeighborId;  // or Up/Down/Left
if (neighborId >= 0 && _registry.TryGetValue(neighborId, out UIElement neighbor))
    SetFocus(neighbor);
```

The dictionary lookup is O(1) — no linear search, no LINQ.

## Assigning IDs

Use sequential IDs or any integer scheme — they just need to be unique within the menu:

```csharp
// Manual assignment for a simple menu:
_okButton.MyId         = 1;
_cancelButton.MyId     = 2;
_okButton.RightNeighborId    = 2;
_cancelButton.LeftNeighborId = 1;
```

For grids (inventory, skill trees), use `AssignGridNeighbors()` to auto-fill all neighbor IDs — see `references/ui-focus.md`.

## Visual Cursor

When focus changes via D-Pad, move a visual cursor sprite to `FocusedElement.Bounds.Center`. This is the Stardew Valley pattern: the cursor is a texture drawn at the focused component's center, not a real DOM focus ring:

```csharp
// In Draw, if using a cursor sprite:
if (UIFocusManager.FocusedElement != null)
{
    Point center = UIFocusManager.FocusedElement.Bounds.Center;
    spriteBatch.Draw(_cursorTexture,
        new Vector2(center.X - _cursorTexture.Width  / 2f,
                    center.Y - _cursorTexture.Height / 2f),
        Color.White);
}
```

## Pressing A / Enter on the Focused Element

When the player presses A (gamepad) or Enter (keyboard), synthesize a click on the focused element:

```csharp
bool confirmPressed = justPressedA || justPressedEnter;
if (confirmPressed && UIFocusManager.FocusedElement != null)
{
    _eventArgs.Reset(UIFocusManager.FocusedElement.Bounds.Center);
    UIFocusManager.FocusedElement.OnClick(_eventArgs);
}
```

Reuse `UIPointerEventArgs` from `monogame-ui-interaction` — the same event system handles both mouse and gamepad confirms.

## Clearing Focus

Focus should be cleared (`SetFocus(null)`) when:
- The menu is closed
- The scene transitions
- The player moves the mouse (optional: auto-focus on hover)

## Anti-Patterns

- Never iterate all `UIElement` instances to find a neighbor — always use the ID dictionary.
- Never assign negative IDs as real neighbor IDs — `-1` is the sentinel "no neighbor here."
- Do not call `GamePad.GetState()` inside individual element code — poll once in the manager.
- Do not set `MyId` to the same value on two different elements — lookup will return an arbitrary one.

## Reference

Complete `UIElement.Focus.cs`, `UIFocusManager`, and `AssignGridNeighbors`: `references/ui-focus.md`.
