# MonoGame UI Slider Reference

## Table of Contents
1. [UISlider — full implementation](#uislider--full-implementation)
2. [Vertical slider variant](#vertical-slider-variant)

---

## UISlider — Full Implementation

```csharp
// UISlider.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace YourGame.UI
{
    public class UISlider : UIElement
    {
        private Texture2D _pixel;        // 1×1 white texture
        private Texture2D _thumbTex;     // thumb handle sprite (null = draw rectangle)

        // Range
        public float Min   { get; set; } = 0f;
        public float Max   { get; set; } = 1f;
        public float Step  { get; set; } = 0f;  // 0 = continuous

        private float _value;
        public float Value
        {
            get => _value;
            set
            {
                float clamped = MathHelper.Clamp(value, Min, Max);
                float snapped = (Step > 0f)
                    ? MathF.Round(clamped / Step) * Step
                    : clamped;
                if (System.MathF.Abs(snapped - _value) < 0.00001f) return;
                _value = snapped;
                ValueChanged?.Invoke(this, _value);
            }
        }

        // Visual
        public int   TrackHeight  { get; set; } = 6;
        public int   ThumbSize    { get; set; } = 18;
        public Color TrackColor   { get; set; } = new Color(60, 60, 60);
        public Color FillColor    { get; set; } = new Color(100, 149, 237);  // CornflowerBlue
        public Color ThumbColor   { get; set; } = Color.White;

        // Events
        public event System.Action<UISlider, float> ValueChanged;
        public event System.Action<UISlider>         DragStarted;
        public event System.Action<UISlider>         DragEnded;

        // Drag state
        private bool _isDragging;

        // Cached rects — updated in Arrange, reused in Draw/Update
        private Rectangle _trackRect;
        private Rectangle _thumbHitRect;  // larger hit area for accessibility

        public UISlider(Texture2D pixel, Texture2D thumbTex = null)
        {
            _pixel    = pixel;
            _thumbTex = thumbTex;
            IsFocusable = true;
            _value    = Min;
        }

        public override void Measure(Point available)
        {
            DesiredSize = (FixedSize != Point.Zero)
                ? FixedSize
                : new Point(available.X, System.Math.Max(ThumbSize, TrackHeight) + 4);
        }

        public override void Arrange(Rectangle finalRect)
        {
            Bounds = finalRect;
            int trackY = finalRect.Y + (finalRect.Height - TrackHeight) / 2;
            _trackRect = new Rectangle(
                finalRect.X + ThumbSize / 2,
                trackY,
                finalRect.Width - ThumbSize,
                TrackHeight);
        }

        // Compute thumb screen X from current Value
        private int ThumbX()
        {
            float t = (Max > Min) ? (_value - Min) / (Max - Min) : 0f;
            return _trackRect.X + (int)(t * _trackRect.Width);
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsEnabled) return;
            if (_isDragging)
            {
                // Position is delivered via pointer args in OnPointerDown/Up,
                // but continuous drag needs the current mouse position.
                // Read from the interaction manager's last known mouse position
                // (passed via a shared field or re-polled here as a one-off).
                // For simplicity: re-poll is acceptable during drag only.
                MouseState ms = Mouse.GetState();
                SetValueFromScreenX(ms.X);
            }
        }

        private void SetValueFromScreenX(int screenX)
        {
            float t = MathHelper.Clamp(
                (screenX - _trackRect.X) / (float)_trackRect.Width, 0f, 1f);
            Value = Min + t * (Max - Min);
        }

        public override void OnPointerDown(UIPointerEventArgs args)
        {
            if (!IsEnabled) return;
            _isDragging = true;
            SetValueFromScreenX(args.Position.X);
            DragStarted?.Invoke(this);
            args.Handled = true;
        }

        public override void OnPointerUp(UIPointerEventArgs args)
        {
            if (_isDragging)
            {
                _isDragging = false;
                DragEnded?.Invoke(this);
            }
            args.Handled = true;
        }

        public override void HandleKeyboardInput(UIKeyboardEventArgs args)
        {
            float step = (Step > 0f) ? Step : (Max - Min) * 0.01f;
            if (args.Key == Keys.Left  || args.Key == Keys.Down)  { Value -= step; args.Handled = true; }
            if (args.Key == Keys.Right || args.Key == Keys.Up)    { Value += step; args.Handled = true; }
            if (args.Key == Keys.Home)                             { Value  = Min;  args.Handled = true; }
            if (args.Key == Keys.End)                             { Value  = Max;  args.Handled = true; }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            float opacity = EffectiveOpacity;

            // Track background
            spriteBatch.Draw(_pixel, _trackRect, TrackColor * opacity);

            // Fill (from left to thumb)
            int thumbX   = ThumbX();
            var fillRect = new Rectangle(_trackRect.X, _trackRect.Y,
                thumbX - _trackRect.X, _trackRect.Height);
            if (fillRect.Width > 0)
                spriteBatch.Draw(_pixel, fillRect, FillColor * opacity);

            // Thumb
            var thumbRect = new Rectangle(
                thumbX - ThumbSize / 2,
                Bounds.Y + (Bounds.Height - ThumbSize) / 2,
                ThumbSize, ThumbSize);

            if (_thumbTex != null)
                spriteBatch.Draw(_thumbTex, thumbRect, ThumbColor * opacity);
            else
                spriteBatch.Draw(_pixel, thumbRect, ThumbColor * opacity);
        }
    }
}
```

---

## Vertical Slider Variant

Override `Arrange` and the value↔position math for a vertical slider:

```csharp
// Subclass or add an Orientation property to UISlider.
// Key differences:

public override void Arrange(Rectangle finalRect)
{
    Bounds = finalRect;
    int trackX = finalRect.X + (finalRect.Width - TrackHeight) / 2;
    _trackRect = new Rectangle(trackX,
        finalRect.Y + ThumbSize / 2,
        TrackHeight,
        finalRect.Height - ThumbSize);
}

private int ThumbY()
{
    // Invert: top = Max, bottom = Min (typical for vertical sliders)
    float t = (Max > Min) ? 1f - (_value - Min) / (Max - Min) : 0f;
    return _trackRect.Y + (int)(t * _trackRect.Height);
}

private void SetValueFromScreenY(int screenY)
{
    float t = 1f - MathHelper.Clamp(
        (screenY - _trackRect.Y) / (float)_trackRect.Height, 0f, 1f);
    Value = Min + t * (Max - Min);
}
```
