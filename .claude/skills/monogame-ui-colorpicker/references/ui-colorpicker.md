# MonoGame UI Color Picker Reference

## Table of Contents
1. [HSV ↔ RGB conversion utilities](#hsv--rgb-conversion-utilities)
2. [UIColorPickerRGB — three sliders](#uicolorpickerrgb--three-sliders)
3. [UIHueBar — precomputed gradient texture](#uihuebar--precomputed-gradient-texture)
4. [UIHsvSquare — saturation/value square](#uihsvsquare--saturationvalue-square)
5. [UIColorPickerHSV — composite picker](#uicolorpickerhsv--composite-picker)

---

## HSV ↔ RGB Conversion Utilities

```csharp
// ColorMath.cs — no allocations, pure static methods
using Microsoft.Xna.Framework;

namespace YourGame.UI
{
    public static class ColorMath
    {
        public static Color HsvToRgb(float h, float s, float v, byte alpha = 255)
        {
            if (s == 0f) { byte lum = (byte)(v * 255); return new Color(lum, lum, lum, alpha); }
            h /= 60f;
            int   i = (int)h;
            float f = h - i;
            float p = v * (1f - s);
            float q = v * (1f - s * f);
            float t = v * (1f - s * (1f - f));
            float r, g, b;
            switch (i % 6)
            {
                case 0:  r=v; g=t; b=p; break;
                case 1:  r=q; g=v; b=p; break;
                case 2:  r=p; g=v; b=t; break;
                case 3:  r=p; g=q; b=v; break;
                case 4:  r=t; g=p; b=v; break;
                default: r=v; g=p; b=q; break;
            }
            return new Color((byte)(r*255), (byte)(g*255), (byte)(b*255), alpha);
        }

        public static void RgbToHsv(Color c, out float h, out float s, out float v)
        {
            float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
            float max   = MathF.Max(r, MathF.Max(g, b));
            float min   = MathF.Min(r, MathF.Min(g, b));
            float delta = max - min;
            v = max;
            s = (max == 0f) ? 0f : delta / max;
            if (delta == 0f) { h = 0f; return; }
            if      (max == r) h = 60f * ((g - b) / delta % 6);
            else if (max == g) h = 60f * ((b - r) / delta + 2);
            else               h = 60f * ((r - g) / delta + 4);
            if (h < 0) h += 360f;
        }
    }
}
```

---

## UIColorPickerRGB — Three Sliders

```csharp
// UIColorPickerRGB.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    // Composes three UISlider instances (R, G, B) + preview swatch.
    // Requires monogame-ui-slider skill.
    public class UIColorPickerRGB : UIContainer
    {
        private readonly UISlider _sliderR;
        private readonly UISlider _sliderG;
        private readonly UISlider _sliderB;
        private readonly UISlider _sliderA;  // optional
        private readonly Texture2D _pixel;

        private Color _selectedColor = Color.White;
        public Color SelectedColor => _selectedColor;

        public bool ShowAlpha { get; }
        public event System.Action<UIColorPickerRGB, Color> ColorChanged;

        public UIColorPickerRGB(Texture2D pixel, bool showAlpha = false)
        {
            _pixel     = pixel;
            ShowAlpha  = showAlpha;

            _sliderR = MakeSlider(pixel, Color.Red);
            _sliderG = MakeSlider(pixel, new Color(0, 200, 0));
            _sliderB = MakeSlider(pixel, Color.Blue);

            _sliderR.Value = 255; _sliderG.Value = 255; _sliderB.Value = 255;
            _sliderR.ValueChanged += (_,__) => UpdateColor();
            _sliderG.ValueChanged += (_,__) => UpdateColor();
            _sliderB.ValueChanged += (_,__) => UpdateColor();

            Add(_sliderR); Add(_sliderG); Add(_sliderB);

            if (showAlpha)
            {
                _sliderA = MakeSlider(pixel, Color.Gray);
                _sliderA.Value = 255;
                _sliderA.ValueChanged += (_,__) => UpdateColor();
                Add(_sliderA);
            }
        }

        private static UISlider MakeSlider(Texture2D pixel, Color fillColor)
        {
            var s = new UISlider(pixel) { Min = 0, Max = 255, Step = 1 };
            s.FillColor = fillColor;
            return s;
        }

        private void UpdateColor()
        {
            byte r = (byte)_sliderR.Value;
            byte g = (byte)_sliderG.Value;
            byte b = (byte)_sliderB.Value;
            byte a = ShowAlpha ? (byte)_sliderA.Value : (byte)255;
            _selectedColor = new Color(r, g, b, a);
            ColorChanged?.Invoke(this, _selectedColor);
        }

        // Push an external Color into the sliders without re-firing events.
        public void SetColor(Color c)
        {
            _sliderR.ValueChanged -= OnSliderChanged;
            _sliderG.ValueChanged -= OnSliderChanged;
            _sliderB.ValueChanged -= OnSliderChanged;

            _sliderR.Value = c.R; _sliderG.Value = c.G; _sliderB.Value = c.B;
            if (ShowAlpha && _sliderA != null) _sliderA.Value = c.A;
            _selectedColor = c;

            _sliderR.ValueChanged += OnSliderChanged;
            _sliderG.ValueChanged += OnSliderChanged;
            _sliderB.ValueChanged += OnSliderChanged;
        }

        private void OnSliderChanged(UISlider _, float __) => UpdateColor();

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            base.Draw(spriteBatch);  // draws sliders

            // Preview swatch in the bottom-right of Bounds (8px margin)
            var swatch = new Rectangle(Bounds.Right - 44, Bounds.Bottom - 36, 40, 32);
            spriteBatch.Draw(_pixel, swatch, _selectedColor);
        }
    }
}
```

---

## UIHueBar — Precomputed Gradient Texture

```csharp
// UIHueBar.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    // Horizontal hue selector bar — gradient from 0° to 360°.
    public class UIHueBar : UIElement
    {
        private Texture2D _gradientTex;  // precomputed, created in constructor
        private Texture2D _pixel;
        private int       _barHeight;

        public float Hue { get; private set; }  // 0–360
        public event System.Action<UIHueBar, float> HueChanged;

        public UIHueBar(GraphicsDevice gd, Texture2D pixel, int width = 200, int barHeight = 20)
        {
            _pixel     = pixel;
            _barHeight = barHeight;
            FixedSize  = new Point(width, barHeight);
            BuildTexture(gd, width);
        }

        private void BuildTexture(GraphicsDevice gd, int width)
        {
            _gradientTex = new Texture2D(gd, width, 1);
            Color[] data = new Color[width];
            for (int i = 0; i < width; i++)
                data[i] = ColorMath.HsvToRgb(i / (float)width * 360f, 1f, 1f);
            _gradientTex.SetData(data);
        }

        public override void Measure(Point available) => DesiredSize = FixedSize;

        public override void OnPointerDown(UIPointerEventArgs args) => SetFromMouse(args.Position.X);
        public override void OnClick(UIPointerEventArgs args)       => SetFromMouse(args.Position.X);

        private void SetFromMouse(int mx)
        {
            float t = MathHelper.Clamp((mx - Bounds.X) / (float)Bounds.Width, 0f, 1f);
            Hue = t * 360f;
            HueChanged?.Invoke(this, Hue);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            // Stretch the 1-pixel-tall gradient to the full bar height
            spriteBatch.Draw(_gradientTex, Bounds, Color.White * EffectiveOpacity);

            // Cursor line
            int cx = Bounds.X + (int)(Hue / 360f * Bounds.Width);
            spriteBatch.Draw(_pixel, new Rectangle(cx - 1, Bounds.Y, 2, Bounds.Height), Color.White);
        }
    }
}
```

---

## UIHsvSquare — Saturation/Value Square

```csharp
// UIHsvSquare.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    // 2D square: X = saturation (0→1), Y = value (1→0), at a fixed hue.
    // Call RebuildTexture() when Hue changes.
    public class UIHsvSquare : UIElement
    {
        private Texture2D _squareTex;
        private Texture2D _pixel;
        private float     _hue;
        private int       _size;

        public float Saturation { get; private set; } = 1f;
        public float Value      { get; private set; } = 1f;
        public event System.Action<UIHsvSquare, float, float> SvChanged;

        public UIHsvSquare(GraphicsDevice gd, Texture2D pixel, int size = 180)
        {
            _pixel    = pixel;
            _size     = size;
            FixedSize = new Point(size, size);
            BuildTexture(gd, 0f);
        }

        public void SetHue(GraphicsDevice gd, float hue)
        {
            if (System.MathF.Abs(hue - _hue) < 0.5f) return; // skip tiny changes
            _hue = hue;
            BuildTexture(gd, hue);
        }

        private void BuildTexture(GraphicsDevice gd, float hue)
        {
            if (_squareTex == null)
                _squareTex = new Texture2D(gd, _size, _size);

            Color[] data = new Color[_size * _size];
            for (int y = 0; y < _size; y++)
            for (int x = 0; x < _size; x++)
            {
                float s = x / (float)(_size - 1);
                float v = 1f - y / (float)(_size - 1);
                data[y * _size + x] = ColorMath.HsvToRgb(hue, s, v);
            }
            _squareTex.SetData(data);
        }

        public override void Measure(Point available) => DesiredSize = FixedSize;

        public override void OnPointerDown(UIPointerEventArgs args) => SetFromMouse(args.Position);
        public override void OnClick(UIPointerEventArgs args)       => SetFromMouse(args.Position);

        private void SetFromMouse(Point p)
        {
            Saturation = MathHelper.Clamp((p.X - Bounds.X) / (float)Bounds.Width, 0f, 1f);
            Value      = 1f - MathHelper.Clamp((p.Y - Bounds.Y) / (float)Bounds.Height, 0f, 1f);
            SvChanged?.Invoke(this, Saturation, Value);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible || _squareTex == null) return;
            spriteBatch.Draw(_squareTex, Bounds, Color.White * EffectiveOpacity);

            // Crosshair cursor
            int cx = Bounds.X + (int)(Saturation * Bounds.Width);
            int cy = Bounds.Y + (int)((1f - Value) * Bounds.Height);
            spriteBatch.Draw(_pixel, new Rectangle(cx - 5, cy, 10, 1), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(cx, cy - 5, 1, 10), Color.White);
        }
    }
}
```

---

## UIColorPickerHSV — Composite Picker

```csharp
// UIColorPickerHSV.cs — wires together UIHueBar + UIHsvSquare + preview
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    public class UIColorPickerHSV : UIContainer
    {
        private readonly UIHueBar   _hueBar;
        private readonly UIHsvSquare _square;
        private readonly Texture2D  _pixel;
        private readonly GraphicsDevice _gd;

        private Color _selectedColor = Color.White;
        public  Color  SelectedColor  => _selectedColor;

        public event System.Action<UIColorPickerHSV, Color> ColorChanged;

        public UIColorPickerHSV(GraphicsDevice gd, Texture2D pixel)
        {
            _gd    = gd;
            _pixel = pixel;

            _square = new UIHsvSquare(gd, pixel);
            _hueBar = new UIHueBar(gd, pixel);

            _hueBar.HueChanged += (_, h) =>
            {
                _square.SetHue(_gd, h);
                UpdateColor();
            };
            _square.SvChanged += (_, s, v) => UpdateColor();

            Add(_square);
            Add(_hueBar);
        }

        private void UpdateColor()
        {
            _selectedColor = ColorMath.HsvToRgb(_hueBar.Hue, _square.Saturation, _square.Value);
            ColorChanged?.Invoke(this, _selectedColor);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            base.Draw(spriteBatch);  // draws square + hue bar

            // Preview swatch
            var swatch = new Rectangle(Bounds.Right - 44, Bounds.Bottom - 36, 40, 32);
            spriteBatch.Draw(_pixel, swatch, _selectedColor);
        }
    }
}
```
