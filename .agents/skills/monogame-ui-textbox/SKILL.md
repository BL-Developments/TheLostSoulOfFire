---
name: monogame-ui-textbox
description: MonoGame UI TextBox controls — plain text input, numeric (int/float with optional step), password (masked), and multiline textarea. Uses Window.TextInput for correct Unicode entry. Use this skill whenever the user asks about text input, a text field, entering text in a UI, numeric input, number field, password field, textarea, "how do I let the user type text in MonoGame", or any editable text control in MonoGame UI.
---

# MonoGame UI TextBox

Text input controls for MonoGame. Four variants share a common base: `TextBoxBase`. Each handles `Window.TextInput` (for correct Unicode / IME support) and maintains a cursor position. For complete code, read `references/ui-textbox.md`.

## Assumed Contract

Extends `UIElement` from `monogame-ui-core`. Requires focus from `monogame-ui-focus` — keyboard events route here only when `UIFocusManager.FocusedElement == this`. Uses `Window.TextInput` event, not keyboard polling.

## The TextInput Event (Not Polling)

**Never poll character keys manually.** `Keyboard.GetState()` gives you key codes, not characters — it cannot handle Unicode, IME composition, keyboard layout differences, or modifier combos correctly. Use the platform event instead:

```csharp
// In Game.Initialize():
Window.TextInput += OnTextInput;

private void OnTextInput(object sender, TextInputEventArgs e)
{
    if (Core.UIFocus.FocusedElement is TextBoxBase tb)
        tb.HandleTextInput(e.Character);
}
```

`HandleTextInput` is responsible for filtering control characters and dispatching to the correct variant's rules.

## Common Base Architecture: TextBoxBase

All TextBox variants share:
- `_text` (StringBuilder) — the content buffer
- `_cursorIndex` (int) — insertion point (0 = before first char)
- `_scrollOffset` (int) — horizontal scroll in pixels for text wider than the box
- `IsReadOnly` — prevents editing
- `Placeholder` — displayed when `_text` is empty, in a dimmed color
- `MaxLength` — clamps input to N characters (-1 = unlimited)
- `Text` property — read-only string accessor (use `SetText(string)` for programmatic changes)
- `event Action<string>? TextChanged` — fires when text changes; receives new string

**Constructor (protected):** `TextBoxBase(SpriteFont? font, Texture2D? pixel, GameWindow? window)`
— pass `Window` from your game to enable TextInput subscription; pass `null` in unit tests.

## Variant 1: TextBox (Plain Text)

Accepts all printable characters. Handles:
- Backspace → delete char before cursor
- Delete → delete char after cursor  
- Left / Right arrows → move cursor
- Home / End → jump to start / end
- Ctrl+A → select all (optional)

## Variant 2: NumericBox

Filters `HandleTextInput` to allow only digits, one minus sign (at position 0), and for float: one decimal separator (`.` or `,`).

Properties:
- `MinValue` / `MaxValue` — clamped on focus-lost (not while typing, to allow intermediate states like `-` or `3.`)
- `Step` — increment/decrement on Up/Down arrow keys
- `IsInt` — when true, no decimal separator allowed; parses to `int`

```csharp
// Integer field, range 1–99, arrow keys step by 1:
var qty = new NumericBox(font, pixel, Window) { IsInt = true, MinValue = 1, MaxValue = 99, Step = 1 };

// Float field, range 0.0–1.0, step 0.1:
var vol = new NumericBox(font, pixel, Window) { MinValue = 0f, MaxValue = 1f, Step = 0.1f };
```

**Parse timing:** Parse the string to a number only in:
- `OnFocusLost` (clamp and reformat)
- When the user reads `IntValue` / `FloatValue` property

Never parse inside `Draw()` — `float.TryParse` allocates.

## Variant 3: PasswordBox

Same as plain text but `Draw()` renders bullet characters (`•`) instead of actual text. Store the real characters in `_text` as plaintext — only the rendering is masked.

```csharp
string masked = new string('•', _text.Length);
spriteBatch.DrawString(_font, masked, textPos, TextColor * opacity);
```

## Variant 4: TextArea (Multiline)

Extends the base with:
- Line breaks on Enter (`\n`)
- `_lines` list rebuilt whenever `_text` changes (rebuild only on change, cache the result)
- Vertical cursor position: `_cursorLine` (int), `_cursorCol` (int)
- Vertical scroll via `_scrollOffsetLines` (int)
- `WordWrap` (bool) — if true, lines are soft-wrapped at the box width

**No LINQ in line rebuilding** — split by scanning characters with a `for` loop.

## Cursor Drawing

Draw a 1-px wide rectangle at the cursor's screen position, visible only when `_cursorVisible == true` and the box is focused:

```csharp
if (IsFocused && _cursorVisible)
{
    // Measure text up to cursor to find X position
    // _cachedBeforeCursor is rebuilt only when _cursorIndex changes
    Vector2 measured = _font.MeasureString(_cachedBeforeCursor);
    int cx = textOrigin.X + (int)measured.X - _scrollOffset;
    spriteBatch.Draw(_pixel,
        new Rectangle(cx, textOrigin.Y, 1, _font.LineSpacing),
        TextColor * opacity);
}
```

Cache `_cachedBeforeCursor` as a field, rebuilt only when `_cursorIndex` changes — do not allocate it every frame.

## Anti-Patterns

- Never poll `Keyboard.GetState()` for character entry — use `Window.TextInput`.
- Never call `_text.ToString()` inside `Draw()` every frame — cache as a field, rebuild only on change.
- Never call `float.TryParse` or `int.TryParse` inside `Draw()`.
- Never allow `\n` characters in single-line variants.
- Never measure the full string every frame to find cursor position — cache `_measuredBeforeCursor` and invalidate only when cursor moves.

## Reference

Complete `TextBoxBase`, `TextBox`, `NumericBox`, `PasswordBox`, and `TextArea`: `references/ui-textbox.md`.
