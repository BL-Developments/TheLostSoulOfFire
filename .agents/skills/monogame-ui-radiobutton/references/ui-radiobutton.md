# MonoGame UI RadioButton Reference

## Table of Contents
1. [RadioGroup](#radiogroup)
2. [UIRadioButton](#uiradiobutton)
3. [Layout example — vertical option list](#layout-example--vertical-option-list)

---

## RadioGroup

```csharp
// RadioGroup.cs
using System.Collections.Generic;

namespace YourGame.UI
{
    public class RadioGroup
    {
        private readonly List<UIRadioButton> _buttons = new List<UIRadioButton>();
        public UIRadioButton SelectedButton { get; private set; }

        public event System.Action<RadioGroup, UIRadioButton> SelectionChanged;

        public void Register(UIRadioButton button)
        {
            if (!_buttons.Contains(button))
                _buttons.Add(button);
        }

        public void Unregister(UIRadioButton button)
        {
            _buttons.Remove(button);
            if (SelectedButton == button) SelectedButton = null;
        }

        // Select the given button; deselect all others.
        public void Select(UIRadioButton button)
        {
            if (button == null || !_buttons.Contains(button)) return;
            if (SelectedButton == button) return;  // clicking selected button: no-op

            // Deselect current
            if (SelectedButton != null)
            {
                var old = SelectedButton;
                SelectedButton = null;
                old.IsChecked = false;
                // Unregister old from TabOrder, register new
                UIFocusManager.Unregister(old);
            }

            SelectedButton    = button;
            button.IsChecked  = true;
            UIFocusManager.Register(button);

            SelectionChanged?.Invoke(this, button);
        }

        // Navigation helpers — no LINQ
        public UIRadioButton NextAfter(UIRadioButton current)
        {
            for (int i = 0; i < _buttons.Count; i++)
                if (_buttons[i] == current && i + 1 < _buttons.Count)
                    return _buttons[i + 1];
            return null;
        }

        public UIRadioButton PrevBefore(UIRadioButton current)
        {
            for (int i = 0; i < _buttons.Count; i++)
                if (_buttons[i] == current && i - 1 >= 0)
                    return _buttons[i - 1];
            return null;
        }

        public UIRadioButton First() => _buttons.Count > 0 ? _buttons[0] : null;
    }
}
```

---

## UIRadioButton

```csharp
// UIRadioButton.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace YourGame.UI
{
    public class UIRadioButton : UIElement
    {
        private readonly SpriteFont _font;
        private readonly Texture2D  _emptyTex;    // unchecked circle sprite
        private readonly Texture2D  _filledTex;   // checked circle sprite
        private readonly RadioGroup _group;

        public string Label     { get; }
        public bool   IsChecked { get; internal set; }

        private int     _circleSize;
        private int     _spacing = 6;
        private Vector2 _labelSize;

        public Color LabelColor   { get; set; } = Color.White;
        public Color CheckedColor { get; set; } = new Color(100, 149, 237);

        public UIRadioButton(SpriteFont font, Texture2D emptyTex, Texture2D filledTex,
                             string label, RadioGroup group, int circleSize = 18)
        {
            _font       = font;
            _emptyTex   = emptyTex;
            _filledTex  = filledTex;
            Label       = label;
            _group      = group;
            _circleSize = circleSize;
            _labelSize  = font.MeasureString(label);

            // Only the first-registered (or selected) button gets tab focus.
            // RadioGroup.Select() manages UIFocusManager registration.
            IsFocusable = false;  // managed externally by RadioGroup

            group.Register(this);
        }

        public override void Measure(Point available)
        {
            int w = _circleSize + _spacing + (int)_labelSize.X;
            int h = System.Math.Max(_circleSize, _font.LineSpacing);
            DesiredSize = (FixedSize != Point.Zero) ? FixedSize : new Point(w, h);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            float opacity = EffectiveOpacity;

            // Circle
            var circleRect = new Rectangle(
                Bounds.X,
                Bounds.Y + (Bounds.Height - _circleSize) / 2,
                _circleSize, _circleSize);

            spriteBatch.Draw(_emptyTex, circleRect, Color.White * opacity);
            if (IsChecked)
                spriteBatch.Draw(_filledTex, circleRect, CheckedColor * opacity);

            // Label
            float ly = Bounds.Y + (Bounds.Height - _labelSize.Y) / 2f;
            spriteBatch.DrawString(_font, Label,
                new Vector2(Bounds.X + _circleSize + _spacing, ly),
                LabelColor * opacity);
        }

        // Click: delegate to group
        public override void OnClick(UIPointerEventArgs args)
        {
            if (!IsEnabled) return;
            _group.Select(this);
            args.Handled = true;
        }

        // Keyboard: navigate within group + select
        public override void HandleKeyboardInput(UIKeyboardEventArgs args)
        {
            switch (args.Key)
            {
                case Keys.Down:
                case Keys.Right:
                {
                    var next = _group.NextAfter(this);
                    if (next != null) { _group.Select(next); UIFocusManager.SetFocus(next); }
                    args.Handled = true;
                    break;
                }
                case Keys.Up:
                case Keys.Left:
                {
                    var prev = _group.PrevBefore(this);
                    if (prev != null) { _group.Select(prev); UIFocusManager.SetFocus(prev); }
                    args.Handled = true;
                    break;
                }
                case Keys.Space:
                case Keys.Enter:
                    _group.Select(this);
                    args.Handled = true;
                    break;
            }
        }
    }
}
```

---

## Layout Example — Vertical Option List

```csharp
// Arrange radio buttons in a StackPanel (from monogame-ui-layout skill).

var group = new RadioGroup();

var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };
stack.Add(new UIRadioButton(font, emptyTex, filledTex, "Easy",   group));
stack.Add(new UIRadioButton(font, emptyTex, filledTex, "Normal", group));
stack.Add(new UIRadioButton(font, emptyTex, filledTex, "Hard",   group));

// Pre-select "Normal"
group.Select((UIRadioButton)stack.Children[1]);

group.SelectionChanged += (g, btn) =>
    GameSettings.Difficulty = btn.Label;

panel.Add(stack);
```
