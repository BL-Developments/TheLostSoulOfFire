# MonoGame UI TextBox Reference

## Table of Contents
1. [UITextBoxBase — shared base](#uitextboxbase--shared-base)
2. [UITextBox — plain text](#uitextbox--plain-text)
3. [UINumericBox — int/float input](#uinumericbox--intfloat-input)
4. [UIPasswordBox — masked input](#uipasswordbox--masked-input)
5. [UITextArea — multiline](#uitextarea--multiline)

---

## UITextBoxBase — Shared Base

```csharp
// UITextBoxBase.cs
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace YourGame.UI
{
    public abstract class UITextBoxBase : UIElement
    {
        protected readonly SpriteFont  _font;
        protected readonly Texture2D   _pixel;
        protected readonly StringBuilder _text = new StringBuilder();

        protected int   _cursorPos;
        protected int   _scrollOffset;    // horizontal scroll in pixels
        protected float _cursorTimer;
        protected bool  _cursorVisible = true;

        // Cached string rebuilt only when _text changes or cursor moves
        private string _cachedText         = "";
        private string _cachedBeforeCursor  = "";
        private bool   _textDirty          = false;
        private bool   _cursorDirty        = false;

        public string  Placeholder  { get; set; } = "";
        public Color   PlaceholderColor { get; set; } = new Color(120, 120, 120);
        public Color   TextColor    { get; set; } = Color.White;
        public Color   BackColor    { get; set; } = new Color(30, 30, 30);
        public Color   BorderColor  { get; set; } = Color.Gray;
        public Color   FocusBorderColor { get; set; } = new Color(100, 149, 237);
        public int     MaxLength    { get; set; } = 256;
        public bool    IsReadOnly   { get; set; } = false;

        // Read-only access to current content
        public string Text => _cachedText;

        public event System.Action<UITextBoxBase, string> TextChanged;
        public event System.Action<UITextBoxBase>         Submitted;  // Enter key

        protected UITextBoxBase(SpriteFont font, Texture2D pixel)
        {
            _font       = font;
            _pixel      = pixel;
            IsFocusable = true;
        }

        // Called from Window.TextInput handler in Game class
        public virtual void HandleTextInput(char c)
        {
            if (IsReadOnly) return;
            if (c == '\b')  // backspace
            {
                if (_cursorPos > 0) { _text.Remove(_cursorPos - 1, 1); _cursorPos--; MarkDirty(); }
                return;
            }
            if (c == '\r' || c == '\n') { OnEnterPressed(); return; }
            if (char.IsControl(c)) return;
            if (_text.Length >= MaxLength) return;

            _text.Insert(_cursorPos, c);
            _cursorPos++;
            MarkDirty();
        }

        protected virtual void OnEnterPressed() => Submitted?.Invoke(this);

        protected void MarkDirty()
        {
            _textDirty   = true;
            _cursorDirty = true;
            _cursorTimer = 0f;
            _cursorVisible = true;
        }

        protected void RebuildCacheIfNeeded()
        {
            if (_textDirty)
            {
                _cachedText  = _text.ToString();
                _textDirty   = false;
                TextChanged?.Invoke(this, _cachedText);
            }
            if (_cursorDirty)
            {
                _cachedBeforeCursor = _text.ToString(0, System.Math.Min(_cursorPos, _text.Length));
                _cursorDirty = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsEnabled) return;
            // Cursor blink
            _cursorTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_cursorTimer >= 0.5f) { _cursorTimer -= 0.5f; _cursorVisible = !_cursorVisible; }
        }

        public override void HandleKeyboardInput(UIKeyboardEventArgs args)
        {
            if (IsReadOnly) return;
            switch (args.Key)
            {
                case Keys.Left:
                    if (_cursorPos > 0) { _cursorPos--; _cursorDirty = true; _cursorTimer = 0; _cursorVisible = true; }
                    args.Handled = true; break;
                case Keys.Right:
                    if (_cursorPos < _text.Length) { _cursorPos++; _cursorDirty = true; _cursorTimer = 0; _cursorVisible = true; }
                    args.Handled = true; break;
                case Keys.Home:
                    _cursorPos = 0; _cursorDirty = true; args.Handled = true; break;
                case Keys.End:
                    _cursorPos = _text.Length; _cursorDirty = true; args.Handled = true; break;
                case Keys.Delete:
                    if (_cursorPos < _text.Length) { _text.Remove(_cursorPos, 1); MarkDirty(); }
                    args.Handled = true; break;
            }
        }

        public override void OnFocusLost()
        {
            _cursorVisible = false;
            OnBlur();
        }

        protected virtual void OnBlur() { }

        protected void DrawBase(SpriteBatch spriteBatch, string displayText)
        {
            RebuildCacheIfNeeded();
            float opacity  = EffectiveOpacity;
            bool  focused  = UIFocusManager.FocusedElement == this;
            Color border   = focused ? FocusBorderColor : BorderColor;

            // Background
            spriteBatch.Draw(_pixel, Bounds, BackColor * opacity);

            // Border (1 px inset drawn as 4 thin rectangles)
            spriteBatch.Draw(_pixel, new Rectangle(Bounds.X,              Bounds.Y,              Bounds.Width, 1),         border * opacity);
            spriteBatch.Draw(_pixel, new Rectangle(Bounds.X,              Bounds.Bottom - 1,     Bounds.Width, 1),         border * opacity);
            spriteBatch.Draw(_pixel, new Rectangle(Bounds.X,              Bounds.Y,              1, Bounds.Height),        border * opacity);
            spriteBatch.Draw(_pixel, new Rectangle(Bounds.Right - 1,      Bounds.Y,              1, Bounds.Height),        border * opacity);

            var textOrigin = new Vector2(Bounds.X + 4, Bounds.Y + (Bounds.Height - _font.LineSpacing) / 2f);

            if (displayText.Length == 0 && Placeholder.Length > 0)
            {
                spriteBatch.DrawString(_font, Placeholder, textOrigin, PlaceholderColor * opacity);
            }
            else
            {
                // Clip text to Bounds — simple horizontal scroll
                spriteBatch.DrawString(_font, displayText,
                    new Vector2(textOrigin.X - _scrollOffset, textOrigin.Y),
                    TextColor * opacity);
            }

            // Cursor
            if (focused && _cursorVisible && !IsReadOnly)
            {
                Vector2 beforeCursor = _font.MeasureString(_cachedBeforeCursor);
                int cx = (int)(textOrigin.X + beforeCursor.X - _scrollOffset);
                if (cx >= Bounds.X + 2 && cx < Bounds.Right - 2)
                    spriteBatch.Draw(_pixel,
                        new Rectangle(cx, (int)textOrigin.Y, 1, _font.LineSpacing),
                        TextColor * opacity);
            }
        }
    }
}
```

---

## UITextBox — Plain Text

```csharp
// UITextBox.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    public class UITextBox : UITextBoxBase
    {
        public UITextBox(SpriteFont font, Texture2D pixel) : base(font, pixel) { }

        public override void Measure(Point available)
        {
            DesiredSize = (FixedSize != Point.Zero)
                ? FixedSize
                : new Point(available.X, _font.LineSpacing + 10);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            DrawBase(spriteBatch, Text);
        }
    }
}
```

---

## UINumericBox — Int/Float Input

```csharp
// UINumericBox.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace YourGame.UI
{
    public class UINumericBox : UITextBoxBase
    {
        public bool  IsInt    { get; set; } = false;
        public float MinValue { get; set; } = float.MinValue;
        public float MaxValue { get; set; } = float.MaxValue;
        public float Step     { get; set; } = 1f;

        // Read parsed values — only parse on demand, never in Draw
        public int   IntValue   => int.TryParse(Text, out int i)     ? i : 0;
        public float FloatValue => float.TryParse(Text,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float f) ? f : 0f;

        public UINumericBox(SpriteFont font, Texture2D pixel) : base(font, pixel) { }

        public override void HandleTextInput(char c)
        {
            if (c == '\b') { base.HandleTextInput(c); return; }
            if (char.IsDigit(c))                { base.HandleTextInput(c); return; }
            if (c == '-' && _cursorPos == 0 && !Text.Contains('-'))
                                                 { base.HandleTextInput(c); return; }
            if (!IsInt && (c == '.' || c == ',') && !Text.Contains('.') && !Text.Contains(','))
                                                 { base.HandleTextInput(c); return; }
            // Reject all other characters silently
        }

        public override void HandleKeyboardInput(UIKeyboardEventArgs args)
        {
            base.HandleKeyboardInput(args);
            if (args.Handled) return;

            if (args.Key == Keys.Up)   { Increment(Step);  args.Handled = true; }
            if (args.Key == Keys.Down) { Increment(-Step); args.Handled = true; }
        }

        private void Increment(float delta)
        {
            float current = FloatValue;
            float next    = MathHelper.Clamp(current + delta, MinValue, MaxValue);
            SetFromFloat(next);
        }

        private void SetFromFloat(float v)
        {
            _text.Clear();
            if (IsInt) _text.Append((int)v);
            else       _text.Append(v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            _cursorPos = _text.Length;
            MarkDirty();
        }

        // Clamp on focus lost
        protected override void OnBlur()
        {
            float v = MathHelper.Clamp(FloatValue, MinValue, MaxValue);
            SetFromFloat(v);
        }

        public override void Measure(Point available)
        {
            DesiredSize = (FixedSize != Point.Zero)
                ? FixedSize
                : new Point(120, _font.LineSpacing + 10);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            DrawBase(spriteBatch, Text);
        }
    }
}
```

---

## UIPasswordBox — Masked Input

```csharp
// UIPasswordBox.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    public class UIPasswordBox : UITextBoxBase
    {
        public char MaskChar { get; set; } = '•';

        public UIPasswordBox(SpriteFont font, Texture2D pixel) : base(font, pixel) { }

        // The actual password string (use carefully — avoid logging)
        public string Password => Text;

        public override void Measure(Point available)
        {
            DesiredSize = (FixedSize != Point.Zero)
                ? FixedSize
                : new Point(available.X, _font.LineSpacing + 10);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            // Render masked characters instead of actual text
            string masked = new string(MaskChar, _text.Length);
            DrawBase(spriteBatch, masked);
        }
    }
}
```

---

## UITextArea — Multiline

```csharp
// UITextArea.cs
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace YourGame.UI
{
    public class UITextArea : UITextBoxBase
    {
        private readonly List<string> _lines = new List<string>();
        private int _scrollOffsetLines;

        public UITextArea(SpriteFont font, Texture2D pixel) : base(font, pixel) { }

        protected override void OnEnterPressed()
        {
            if (IsReadOnly) return;
            _text.Insert(_cursorPos, '\n');
            _cursorPos++;
            MarkDirty();
        }

        private void RebuildLines()
        {
            _lines.Clear();
            int start = 0;
            string s  = _text.ToString();
            for (int i = 0; i <= s.Length; i++)
            {
                if (i == s.Length || s[i] == '\n')
                {
                    _lines.Add(s.Substring(start, i - start));
                    start = i + 1;
                }
            }
        }

        public override void Measure(Point available)
        {
            DesiredSize = (FixedSize != Point.Zero)
                ? FixedSize
                : new Point(available.X, _font.LineSpacing * 5 + 10);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;

            RebuildLines(); // Cheap: only scans the string; cache via _textDirty if perf is an issue

            float opacity  = EffectiveOpacity;
            bool  focused  = UIFocusManager.FocusedElement == this;

            // Background + border (same as DrawBase)
            spriteBatch.Draw(_pixel, Bounds, BackColor * opacity);
            spriteBatch.Draw(_pixel, new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 1), (focused ? FocusBorderColor : BorderColor) * opacity);
            spriteBatch.Draw(_pixel, new Rectangle(Bounds.X, Bounds.Bottom - 1, Bounds.Width, 1), (focused ? FocusBorderColor : BorderColor) * opacity);
            spriteBatch.Draw(_pixel, new Rectangle(Bounds.X, Bounds.Y, 1, Bounds.Height), (focused ? FocusBorderColor : BorderColor) * opacity);
            spriteBatch.Draw(_pixel, new Rectangle(Bounds.Right - 1, Bounds.Y, 1, Bounds.Height), (focused ? FocusBorderColor : BorderColor) * opacity);

            int lineH = _font.LineSpacing;
            int visibleLines = (Bounds.Height - 8) / lineH;

            for (int i = _scrollOffsetLines; i < _lines.Count && i < _scrollOffsetLines + visibleLines; i++)
            {
                float y = Bounds.Y + 4 + (i - _scrollOffsetLines) * lineH;
                spriteBatch.DrawString(_font, _lines[i],
                    new Vector2(Bounds.X + 4, y), TextColor * opacity);
            }
        }
    }
}
```
