# MonoGame UI Controls Reference

Ready-to-use component templates. All classes use `YourGame.UI` namespace.

## Table of Contents
1. [Label](#label)
2. [Button](#button)
3. [ProgressBar / HealthBar](#progressbar--healthbar)
4. [ScrollView (scissor clipping)](#scrollview-scissor-clipping)
5. [Panel (background container)](#panel-background-container)
6. [UISprite (texture display)](#uisprite-texture-display)
7. [UICheckbox](#uicheckbox)
8. [Scissor RasterizerState setup](#scissor-rasterizerstate-setup)

---

## Label

```csharp
// Label.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    public enum TextAlign { Left, Center, Right }

    public class Label : UIElement
    {
        private SpriteFont _font;
        private string     _text  = "";
        private Vector2    _measuredSize;
        private bool       _textDirty = true;

        public TextAlign HorizontalAlign { get; set; } = TextAlign.Left;
        public Color     TextColor       { get; set; } = Color.White;

        public string Text
        {
            get => _text;
            set
            {
                if (_text == value) return;
                _text      = value;
                _textDirty = true;
                InvalidateLayout();
            }
        }

        public Label(SpriteFont font) { _font = font; IsFocusable = false; }

        public override void Measure(Point available)
        {
            if (_textDirty)
            {
                _measuredSize = _font.MeasureString(_text.Length > 0 ? _text : " ");
                _textDirty    = false;
            }
            DesiredSize = (FixedSize != Point.Zero)
                ? FixedSize
                : new Point((int)_measuredSize.X, (int)_measuredSize.Y);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible || _text.Length == 0) return;

            float x = HorizontalAlign switch
            {
                TextAlign.Center => Bounds.X + (Bounds.Width  - _measuredSize.X) / 2f,
                TextAlign.Right  => Bounds.Right - _measuredSize.X,
                _                => Bounds.X
            };

            spriteBatch.DrawString(_font, _text,
                new Vector2(x, Bounds.Y),
                TextColor * EffectiveOpacity);
        }
    }
}
```

---

## Button

```csharp
// UIButton.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    public class UIButton : UIElement
    {
        private SpriteFont _font;
        private string     _label;
        private Vector2    _labelSize;
        private Vector2    _labelOrigin; // cached half-size for centering

        private Texture2D  _texture;     // optional background texture

        // Scale animation fields — mutated in Update, never allocate structs here.
        private float _currentScale = 1f;
        private float _targetScale  = 1f;

        public Color NormalColor   { get; set; } = Color.White;
        public Color HoveredColor  { get; set; } = Color.LightYellow;
        public Color PressedColor  { get; set; } = new Color(180, 180, 180);
        public Color DisabledColor { get; set; } = Color.Gray;

        public event System.Action<UIButton> Clicked;

        public UIButton(SpriteFont font, string label, Texture2D texture = null)
        {
            _font    = font;
            _texture = texture;
            SetLabel(label);
            IsFocusable = true;
        }

        private void SetLabel(string text)
        {
            _label       = text;
            _labelSize   = _font.MeasureString(text);
            _labelOrigin = _labelSize / 2f;
        }

        public override void Measure(Point available)
        {
            DesiredSize = (FixedSize != Point.Zero)
                ? FixedSize
                : new Point((int)_labelSize.X + 16, (int)_labelSize.Y + 10);
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsEnabled) return;

            _targetScale = !IsEnabled ? 1f
                         : IsHovered && IsPressed() ? 0.97f
                         : IsHovered ? 1.05f
                         : 1f;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _currentScale += (_targetScale - _currentScale) * 12f * dt;
        }

        // IsPressed() reads whether this button is currently being held down.
        // Set this field from UIInteractionManager when OnPointerDown fires.
        public bool _isBeingPressed;
        private bool IsPressed() => _isBeingPressed;

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;

            Color tint = !IsEnabled ? DisabledColor
                       : _isBeingPressed ? PressedColor
                       : IsHovered ? HoveredColor
                       : NormalColor;

            tint *= EffectiveOpacity;

            Vector2 center = Bounds.Center.ToVector2();

            // Background texture (scaled from center)
            if (_texture != null)
            {
                Vector2 texOrigin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);
                spriteBatch.Draw(_texture, center, null, tint,
                    0f, texOrigin, _currentScale, SpriteEffects.None, 0f);
            }

            // Label text (centered in Bounds)
            spriteBatch.DrawString(_font, _label,
                center - _labelOrigin * _currentScale,
                Color.Black * EffectiveOpacity,
                0f, Vector2.Zero, _currentScale, SpriteEffects.None, 0f);
        }

        public override void OnClick(UIPointerEventArgs args)
        {
            if (!IsEnabled) return;
            Clicked?.Invoke(this);
            args.Handled = true;
        }

        public override void OnPointerDown(UIPointerEventArgs args) { _isBeingPressed = true;  }
        public override void OnPointerUp(UIPointerEventArgs args)   { _isBeingPressed = false; }
        public override void OnFocusGained() { IsHovered = true; }
        public override void OnFocusLost()   { IsHovered = false; _isBeingPressed = false; }
    }
}
```

---

## ProgressBar / HealthBar

```csharp
// ProgressBar.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    public class ProgressBar : UIElement
    {
        private Texture2D _pixel; // 1x1 white texture

        private float _fillFraction = 1f; // 0.0 to 1.0

        public float FillFraction
        {
            get => _fillFraction;
            set => _fillFraction = MathHelper.Clamp(value, 0f, 1f);
        }

        public Color BackgroundColor { get; set; } = new Color(40, 40, 40);
        public bool  UseGradient     { get; set; } = true; // green→yellow→red

        public ProgressBar(Texture2D pixel)
        {
            _pixel      = pixel;
            IsFocusable = false;
        }

        public override void Measure(Point available)
        {
            DesiredSize = (FixedSize != Point.Zero) ? FixedSize : new Point(200, 20);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;

            float opacity = EffectiveOpacity;

            // Background
            spriteBatch.Draw(_pixel, Bounds, BackgroundColor * opacity);

            // Fill
            int fillWidth = (int)(_fillFraction * Bounds.Width);
            if (fillWidth > 0)
            {
                Color barColor = UseGradient ? GetGradientColor(_fillFraction) : Color.Green;
                var fillRect = new Rectangle(Bounds.X, Bounds.Y, fillWidth, Bounds.Height);
                spriteBatch.Draw(_pixel, fillRect, barColor * opacity);
            }
        }

        private static Color GetGradientColor(float t)
        {
            if (t > 0.5f)
                return Color.Lerp(Color.Yellow, Color.Green, (t - 0.5f) * 2f);
            return Color.Lerp(Color.Red, Color.Yellow, t * 2f);
        }
    }
}
```

---

## ScrollView (Scissor Clipping)

```csharp
// ScrollView.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    public class ScrollView : UIContainer
    {
        private readonly GraphicsDevice _gd;

        // Created ONCE — never inside Draw().
        private readonly RasterizerState _scissorState;

        // Scroll offset in pixels (Y-axis scroll).
        public int ScrollOffsetY { get; private set; }

        // Total content height (set after measuring children).
        public int ContentHeight { get; private set; }

        public ScrollView(GraphicsDevice gd)
        {
            _gd           = gd;
            _scissorState = new RasterizerState { ScissorTestEnable = true };
        }

        public override void Arrange(Rectangle finalRect)
        {
            Bounds = finalRect;
            // Layout children vertically (StackPanel style), offset by scroll.
            int cursor = finalRect.Y - ScrollOffsetY;
            for (int i = 0; i < Children.Count; i++)
            {
                Point ds = Children[i].DesiredSize;
                Children[i].Arrange(new Rectangle(finalRect.X, cursor, finalRect.Width, ds.Y));
                cursor += ds.Y;
            }
            ContentHeight = cursor - (finalRect.Y - ScrollOffsetY);
        }

        public void ScrollBy(int deltaY)
        {
            int maxOffset = System.Math.Max(0, ContentHeight - Bounds.Height);
            ScrollOffsetY = MathHelper.Clamp(ScrollOffsetY + deltaY, 0, maxOffset);
            LayoutDirty   = true; // re-arrange to shift child Bounds
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;

            // End current batch, save scissor, apply intersection, restart.
            spriteBatch.End();

            Rectangle savedScissor = _gd.ScissorRectangle;

            // ALWAYS intersect — never assign Bounds directly.
            Rectangle clip = Rectangle.Intersect(Bounds, savedScissor);
            _gd.ScissorRectangle = clip;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, null, _scissorState);

            // Draw self (background)
            base.Draw(spriteBatch); // calls UIContainer which draws children

            spriteBatch.End();

            // Restore
            _gd.ScissorRectangle = savedScissor;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, null, null);
        }
    }
}
```

---

## Panel (Background Container)

```csharp
// Panel.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    public class Panel : UIContainer
    {
        private Texture2D _background; // null = transparent
        public Color BackgroundColor { get; set; } = Color.White;

        public Panel(Texture2D background = null)
        {
            _background = background;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;

            // Draw background before children.
            if (_background != null)
                spriteBatch.Draw(_background, Bounds, BackgroundColor * EffectiveOpacity);

            // Draw children (inherited from UIContainer).
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].IsVisible)
                    Children[i].Draw(spriteBatch);
            }
        }
    }
}
```

---

## UISprite (Texture Display)

```csharp
// UISprite.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    public enum SpriteDrawMode { Stretch, Fit, Crop, Tile }

    public class UISprite : UIElement
    {
        private Texture2D    _texture;
        public  Rectangle?   SourceRect  { get; set; } = null; // null = full texture
        public  Color        Tint        { get; set; } = Color.White;
        public  SpriteDrawMode DrawMode  { get; set; } = SpriteDrawMode.Stretch;

        public UISprite(Texture2D texture) { _texture = texture; }

        public void SetTexture(Texture2D texture)
        {
            _texture = texture;
            InvalidateLayout();
        }

        public override void Measure(Point available)
        {
            if (FixedSize != Point.Zero) { DesiredSize = FixedSize; return; }
            if (_texture == null)        { DesiredSize = Point.Zero; return; }

            Rectangle src = SourceRect ?? _texture.Bounds;
            DesiredSize = new Point(src.Width, src.Height);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible || _texture == null) return;

            Rectangle src  = SourceRect ?? _texture.Bounds;
            Color     tint = Tint * EffectiveOpacity;

            switch (DrawMode)
            {
                case SpriteDrawMode.Stretch:
                    spriteBatch.Draw(_texture, Bounds, src, tint);
                    break;

                case SpriteDrawMode.Fit:
                {
                    float scaleX = Bounds.Width  / (float)src.Width;
                    float scaleY = Bounds.Height / (float)src.Height;
                    float scale  = System.Math.Min(scaleX, scaleY);
                    int   w      = (int)(src.Width  * scale);
                    int   h      = (int)(src.Height * scale);
                    var   dest   = new Rectangle(
                        Bounds.X + (Bounds.Width  - w) / 2,
                        Bounds.Y + (Bounds.Height - h) / 2,
                        w, h);
                    spriteBatch.Draw(_texture, dest, src, tint);
                    break;
                }

                case SpriteDrawMode.Crop:
                    // Draw at natural size from top-left; caller must apply scissor for overflow.
                    spriteBatch.Draw(_texture,
                        new Vector2(Bounds.X, Bounds.Y), src, tint);
                    break;

                case SpriteDrawMode.Tile:
                    // Requires SamplerState.LinearWrap in the active SpriteBatch.Begin call.
                    spriteBatch.Draw(_texture, Bounds, src, tint);
                    break;
            }
        }
    }
}
```

---

## UICheckbox

```csharp
// UICheckbox.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace YourGame.UI
{
    public class UICheckbox : UIElement
    {
        private SpriteFont _font;
        private string     _label;
        private Texture2D  _boxTexture;      // unchecked box sprite
        private Texture2D  _checkTexture;    // checkmark sprite (drawn on top when checked)
        private int        _boxSize   = 20;
        private int        _spacing   = 6;   // gap between box and label text
        private Vector2    _labelSize;

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                CheckedChanged?.Invoke(this, _isChecked);
            }
        }

        public Color LabelColor { get; set; } = Color.White;
        public event System.Action<UICheckbox, bool> CheckedChanged;

        public UICheckbox(SpriteFont font, string label,
                          Texture2D boxTexture, Texture2D checkTexture)
        {
            _font         = font;
            _label        = label;
            _boxTexture   = boxTexture;
            _checkTexture = checkTexture;
            _labelSize    = font.MeasureString(label);
            IsFocusable   = true;
        }

        public override void Measure(Point available)
        {
            if (FixedSize != Point.Zero) { DesiredSize = FixedSize; return; }
            int w = _boxSize + _spacing + (int)_labelSize.X;
            int h = System.Math.Max(_boxSize, (int)_labelSize.Y);
            DesiredSize = new Point(w, h);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            float opacity = EffectiveOpacity;

            // Box
            var boxRect = new Rectangle(Bounds.X, Bounds.Y + (Bounds.Height - _boxSize) / 2,
                _boxSize, _boxSize);
            spriteBatch.Draw(_boxTexture, boxRect, Color.White * opacity);

            // Checkmark
            if (_isChecked && _checkTexture != null)
                spriteBatch.Draw(_checkTexture, boxRect, Color.White * opacity);

            // Label
            float labelY = Bounds.Y + (Bounds.Height - _labelSize.Y) / 2f;
            spriteBatch.DrawString(_font, _label,
                new Vector2(Bounds.X + _boxSize + _spacing, labelY),
                LabelColor * opacity);
        }

        // Toggle on click
        public override void OnClick(UIPointerEventArgs args)
        {
            IsChecked    = !IsChecked;
            args.Handled = true;
        }

        // Toggle on Space when focused
        public override void HandleKeyboardInput(UIKeyboardEventArgs args)
        {
            if (args.Key == Keys.Space)
            {
                IsChecked    = !IsChecked;
                args.Handled = true;
            }
        }
    }
}
```

---

## Scissor RasterizerState Setup

```csharp
// Create ONCE in LoadContent or the component constructor.
// Never new RasterizerState() inside Draw().

private RasterizerState _scissorRS;

protected override void LoadContent()
{
    _scissorRS = new RasterizerState { ScissorTestEnable = true };
}

// Usage in Draw when you need a one-off clip region:
void DrawClipped(SpriteBatch sb, Rectangle clipRect, System.Action drawContent)
{
    sb.End();

    Rectangle saved = GraphicsDevice.ScissorRectangle;
    GraphicsDevice.ScissorRectangle = Rectangle.Intersect(clipRect, saved);

    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
        SamplerState.LinearClamp, null, _scissorRS);
    drawContent();
    sb.End();

    GraphicsDevice.ScissorRectangle = saved;
    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
        SamplerState.LinearClamp, null, null);
}
```
