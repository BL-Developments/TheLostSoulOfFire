# MonoGame UI Layout Reference

Templates for the layout layer. All classes use the `YourGame.UI` namespace.

## Table of Contents
1. [UIElement.Layout.cs — partial class extension](#uielementlayoutcs)
2. [AnchorPoint enum](#anchorpoint-enum)
3. [StackPanel](#stackpanel)
4. [Canvas](#canvas)
5. [FlowLayoutPanel](#flowlayoutpanel)
6. [ListView](#listview)
7. [Resolution-independent UI scaling](#resolution-independent-ui-scaling)

---

## UIElement.Layout.cs

```csharp
// UIElement.Layout.cs
// Extends UIElement defined in UIElement.Core.cs (monogame-ui-core skill).

using Microsoft.Xna.Framework;

namespace YourGame.UI
{
    public abstract partial class UIElement
    {
        // The size this element wants, computed during Measure().
        public Point DesiredSize { get; protected set; }

        // Optional fixed size override. Point.Zero = auto-size.
        public Point FixedSize { get; set; } = Point.Zero;

        // Anchor relative to parent. Used by parent containers during Arrange().
        public AnchorPoint Anchor { get; set; } = AnchorPoint.TopLeft;

        // Pixel offset applied after anchor calculation.
        public Point Offset { get; set; } = Point.Zero;

        // Bottom-up pass: calculate how much space this element needs.
        // 'available' is the space the parent is offering (may be zero if unconstrained).
        public virtual void Measure(Point available)
        {
            if (FixedSize != Point.Zero)
            {
                DesiredSize = FixedSize;
                return;
            }
            // Default: measure children and take their combined size.
            // Subclasses (StackPanel, Canvas) override this.
            DesiredSize = available;
        }

        // Top-down pass: assign the absolute screen rectangle for this element.
        public virtual void Arrange(Rectangle finalRect)
        {
            Bounds = finalRect;
        }

        // Walk up the tree and mark all UIContainer ancestors as dirty.
        protected void InvalidateLayout()
        {
            UIElement current = Parent;
            while (current != null)
            {
                if (current is UIContainer c)
                    c.LayoutDirty = true;
                current = current.Parent;
            }
        }
    }
}
```

---

## AnchorPoint enum

```csharp
// AnchorPoint.cs
namespace YourGame.UI
{
    public enum AnchorPoint
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        Center,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
}
```

Helper method — put this as a static method in a `UILayout` utility class or inside `UIContainer`:

```csharp
// Compute child's top-left position given parent bounds, anchor, child size, and offset.
// Call this inside Arrange() when positioning anchored children.
public static Point ResolveAnchor(Rectangle parent, AnchorPoint anchor, Point childSize, Point offset)
{
    int x, y;
    switch (anchor)
    {
        case AnchorPoint.TopLeft:      x = parent.X;                           y = parent.Y;                            break;
        case AnchorPoint.TopCenter:    x = parent.X + (parent.Width  - childSize.X) / 2; y = parent.Y;                 break;
        case AnchorPoint.TopRight:     x = parent.Right  - childSize.X;        y = parent.Y;                            break;
        case AnchorPoint.MiddleLeft:   x = parent.X;                           y = parent.Y + (parent.Height - childSize.Y) / 2; break;
        case AnchorPoint.Center:       x = parent.X + (parent.Width  - childSize.X) / 2; y = parent.Y + (parent.Height - childSize.Y) / 2; break;
        case AnchorPoint.MiddleRight:  x = parent.Right  - childSize.X;        y = parent.Y + (parent.Height - childSize.Y) / 2; break;
        case AnchorPoint.BottomLeft:   x = parent.X;                           y = parent.Bottom - childSize.Y;         break;
        case AnchorPoint.BottomCenter: x = parent.X + (parent.Width  - childSize.X) / 2; y = parent.Bottom - childSize.Y; break;
        case AnchorPoint.BottomRight:  x = parent.Right  - childSize.X;        y = parent.Bottom - childSize.Y;         break;
        default:                       x = parent.X;                           y = parent.Y;                            break;
    }
    return new Point(x + offset.X, y + offset.Y);
}
```

---

## StackPanel

```csharp
// StackPanel.cs
using Microsoft.Xna.Framework;

namespace YourGame.UI
{
    public enum Orientation { Horizontal, Vertical }

    public class StackPanel : UIContainer
    {
        public Orientation Orientation { get; set; } = Orientation.Vertical;
        public int Spacing { get; set; } = 4;

        // Bottom-up: sum children sizes along the axis, max on the cross-axis.
        // NO LINQ — indexed for-loop only.
        public override void Measure(Point available)
        {
            int totalMain  = 0;
            int maxCross   = 0;
            int childCount = Children.Count;

            for (int i = 0; i < childCount; i++)
            {
                Children[i].Measure(available);
                Point ds = Children[i].DesiredSize;

                if (Orientation == Orientation.Vertical)
                {
                    totalMain += ds.Y;
                    if (ds.X > maxCross) maxCross = ds.X;
                }
                else
                {
                    totalMain += ds.X;
                    if (ds.Y > maxCross) maxCross = ds.Y;
                }

                if (i < childCount - 1)
                    totalMain += Spacing;
            }

            DesiredSize = (Orientation == Orientation.Vertical)
                ? new Point(maxCross, totalMain)
                : new Point(totalMain, maxCross);
        }

        // Top-down: assign each child a Rectangle, advancing the cursor.
        public override void Arrange(Rectangle finalRect)
        {
            Bounds = finalRect;
            int cursor = (Orientation == Orientation.Vertical) ? finalRect.Y : finalRect.X;

            for (int i = 0; i < Children.Count; i++)
            {
                Point ds = Children[i].DesiredSize;
                Rectangle childRect;

                if (Orientation == Orientation.Vertical)
                {
                    childRect = new Rectangle(finalRect.X, cursor, finalRect.Width, ds.Y);
                    cursor += ds.Y + Spacing;
                }
                else
                {
                    childRect = new Rectangle(cursor, finalRect.Y, ds.X, finalRect.Height);
                    cursor += ds.X + Spacing;
                }

                Children[i].Arrange(childRect);
            }
        }
    }
}
```

---

## Canvas

```csharp
// Canvas.cs
using Microsoft.Xna.Framework;

namespace YourGame.UI
{
    // Canvas positions children at explicit Offset positions relative to Canvas.Bounds.Location.
    // Each child must set its own DesiredSize in Measure().
    public class Canvas : UIContainer
    {
        public override void Measure(Point available)
        {
            // Measure children so they compute their DesiredSize.
            // NO LINQ — indexed for-loop only.
            for (int i = 0; i < Children.Count; i++)
                Children[i].Measure(available);

            // Canvas DesiredSize = fixed size if set, otherwise fill available.
            DesiredSize = (FixedSize != Point.Zero) ? FixedSize : available;
        }

        public override void Arrange(Rectangle finalRect)
        {
            Bounds = finalRect;

            for (int i = 0; i < Children.Count; i++)
            {
                UIElement child = Children[i];
                Point origin = ResolveAnchor(finalRect, child.Anchor, child.DesiredSize, child.Offset);
                child.Arrange(new Rectangle(origin.X, origin.Y, child.DesiredSize.X, child.DesiredSize.Y));
            }
        }

        // Imported from UILayout helper — same method as in anchor section above.
        private static Point ResolveAnchor(Rectangle parent, AnchorPoint anchor, Point childSize, Point offset)
        {
            int x, y;
            switch (anchor)
            {
                case AnchorPoint.TopLeft:      x = parent.X;                                       y = parent.Y;                                       break;
                case AnchorPoint.TopCenter:    x = parent.X + (parent.Width  - childSize.X) / 2;   y = parent.Y;                                       break;
                case AnchorPoint.TopRight:     x = parent.Right - childSize.X;                     y = parent.Y;                                       break;
                case AnchorPoint.MiddleLeft:   x = parent.X;                                       y = parent.Y + (parent.Height - childSize.Y) / 2;   break;
                case AnchorPoint.Center:       x = parent.X + (parent.Width  - childSize.X) / 2;   y = parent.Y + (parent.Height - childSize.Y) / 2;   break;
                case AnchorPoint.MiddleRight:  x = parent.Right - childSize.X;                     y = parent.Y + (parent.Height - childSize.Y) / 2;   break;
                case AnchorPoint.BottomLeft:   x = parent.X;                                       y = parent.Bottom - childSize.Y;                     break;
                case AnchorPoint.BottomCenter: x = parent.X + (parent.Width  - childSize.X) / 2;   y = parent.Bottom - childSize.Y;                     break;
                case AnchorPoint.BottomRight:  x = parent.Right - childSize.X;                     y = parent.Bottom - childSize.Y;                     break;
                default:                       x = parent.X;                                       y = parent.Y;                                       break;
            }
            return new Point(x + offset.X, y + offset.Y);
        }
    }
}
```

---

## FlowLayoutPanel

```csharp
// FlowLayoutPanel.cs
using Microsoft.Xna.Framework;

namespace YourGame.UI
{
    // Wraps children to a new row when they exceed the container width.
    // Equivalent to CSS flex-wrap: wrap with flex-direction: row.
    public class FlowLayoutPanel : UIContainer
    {
        public int ItemSpacing { get; set; } = 4;  // horizontal gap between items
        public int LineSpacing { get; set; } = 4;  // vertical gap between rows

        public override void Measure(Point available)
        {
            // Simulate wrapping to compute total height needed.
            int cursorX    = 0;
            int rowHeight  = 0;
            int totalH     = 0;
            int maxW       = 0;
            int w          = available.X;

            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].Measure(available);
                Point ds = Children[i].DesiredSize;

                if (cursorX > 0 && cursorX + ds.X > w)
                {
                    // Wrap to next row
                    if (cursorX - ItemSpacing > maxW) maxW = cursorX - ItemSpacing;
                    totalH  += rowHeight + LineSpacing;
                    cursorX  = 0;
                    rowHeight = 0;
                }

                cursorX += ds.X + ItemSpacing;
                if (ds.Y > rowHeight) rowHeight = ds.Y;
            }

            // Last row
            if (cursorX - ItemSpacing > maxW) maxW = cursorX - ItemSpacing;
            totalH += rowHeight;

            DesiredSize = (FixedSize != Point.Zero)
                ? FixedSize
                : new Point(maxW > 0 ? maxW : available.X, totalH);
        }

        public override void Arrange(Rectangle finalRect)
        {
            Bounds = finalRect;
            int cursorX   = finalRect.X;
            int cursorY   = finalRect.Y;
            int rowHeight = 0;
            int rowStart  = 0; // index of first child in current row

            for (int i = 0; i < Children.Count; i++)
            {
                Point ds = Children[i].DesiredSize;

                if (cursorX > finalRect.X && cursorX + ds.X > finalRect.Right)
                {
                    // Flush current row — baseline-align items within the row
                    AlignRow(rowStart, i, cursorY, rowHeight, finalRect.X);
                    cursorY   += rowHeight + LineSpacing;
                    cursorX    = finalRect.X;
                    rowHeight  = 0;
                    rowStart   = i;
                }

                // Temporarily place at cursorX — will be finalised in AlignRow
                Children[i].Arrange(new Rectangle(cursorX, cursorY, ds.X, ds.Y));
                cursorX += ds.X + ItemSpacing;
                if (ds.Y > rowHeight) rowHeight = ds.Y;
            }

            // Final row
            AlignRow(rowStart, Children.Count, cursorY, rowHeight, finalRect.X);
        }

        // Re-arrange a row of children to the correct Y baseline within the row.
        private void AlignRow(int from, int to, int rowY, int rowH, int startX)
        {
            int cx = startX;
            for (int i = from; i < to; i++)
            {
                Point ds = Children[i].DesiredSize;
                int   iy = rowY + (rowH - ds.Y) / 2; // vertically center within row
                Children[i].Arrange(new Rectangle(cx, iy, ds.X, ds.Y));
                cx += ds.X + ItemSpacing;
            }
        }
    }
}
```

---

## ListView

```csharp
// ListView.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace YourGame.UI
{
    // Vertical selectable list. Wraps a ScrollView for overflow handling.
    public class ListView : UIContainer
    {
        private readonly SpriteFont  _font;
        private readonly GraphicsDevice _gd;
        private readonly RasterizerState _scissorRS;  // created once in constructor
        private Texture2D _pixel;

        public  int     SelectedIndex { get; private set; } = -1;
        public  int     ItemHeight    { get; set; } = 28;
        public  Color   HighlightColor { get; set; } = new Color(100, 149, 237, 100); // CornflowerBlue 40%
        public  Color   ItemColor      { get; set; } = Color.White;
        public  int     ScrollOffsetY  { get; private set; }

        public event System.Action<ListView, int, string> SelectionChanged;

        private readonly System.Collections.Generic.List<string> _items
            = new System.Collections.Generic.List<string>();

        public ListView(GraphicsDevice gd, SpriteFont font, Texture2D pixel)
        {
            _gd        = gd;
            _font      = font;
            _pixel     = pixel;
            _scissorRS = new RasterizerState { ScissorTestEnable = true };
            IsFocusable = true;
        }

        public void AddItem(string text)
        {
            _items.Add(text);
            LayoutDirty = true;
        }

        public void ClearItems()
        {
            _items.Clear();
            SelectedIndex = -1;
            ScrollOffsetY = 0;
            LayoutDirty   = true;
        }

        public override void Measure(Point available)
        {
            int h = _items.Count * ItemHeight;
            DesiredSize = (FixedSize != Point.Zero)
                ? FixedSize
                : new Point(available.X, System.Math.Min(h, available.Y));
        }

        // Select by click
        public override void OnClick(UIPointerEventArgs args)
        {
            int relY  = args.Position.Y - Bounds.Y + ScrollOffsetY;
            int index = relY / ItemHeight;
            if (index >= 0 && index < _items.Count)
                Select(index);
            args.Handled = true;
        }

        // Navigate with arrow keys / D-Pad when focused
        public override void HandleKeyboardInput(UIKeyboardEventArgs args)
        {
            if (args.Key == Keys.Up   && SelectedIndex > 0)                  { Select(SelectedIndex - 1); args.Handled = true; }
            if (args.Key == Keys.Down && SelectedIndex < _items.Count - 1)   { Select(SelectedIndex + 1); args.Handled = true; }
        }

        private void Select(int index)
        {
            SelectedIndex = index;
            // Auto-scroll to keep selection visible
            int itemTop    = index * ItemHeight;
            int itemBottom = itemTop + ItemHeight;
            if (itemTop    < ScrollOffsetY)              ScrollOffsetY = itemTop;
            if (itemBottom > ScrollOffsetY + Bounds.Height) ScrollOffsetY = itemBottom - Bounds.Height;
            SelectionChanged?.Invoke(this, index, _items[index]);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;

            // Apply scissor clipping so items don't draw outside Bounds
            spriteBatch.End();
            Rectangle savedScissor = _gd.ScissorRectangle;
            _gd.ScissorRectangle = Rectangle.Intersect(Bounds, savedScissor);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, null, _scissorRS);

            for (int i = 0; i < _items.Count; i++)
            {
                int itemY = Bounds.Y + i * ItemHeight - ScrollOffsetY;
                if (itemY + ItemHeight < Bounds.Y || itemY > Bounds.Bottom) continue; // cull

                var rowRect = new Rectangle(Bounds.X, itemY, Bounds.Width, ItemHeight);

                // Highlight selected row
                if (i == SelectedIndex)
                    spriteBatch.Draw(_pixel, rowRect, HighlightColor);

                float textY = itemY + (ItemHeight - _font.LineSpacing) / 2f;
                spriteBatch.DrawString(_font, _items[i],
                    new Vector2(Bounds.X + 6, textY),
                    ItemColor * EffectiveOpacity);
            }

            spriteBatch.End();
            _gd.ScissorRectangle = savedScissor;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, null, null);
        }
    }
}
```

---

## Resolution-Independent UI Scaling

```csharp
// In your Game class or UI manager:

private const int VirtualWidth  = 1920;
private const int VirtualHeight = 1080;

private Matrix _uiScaleMatrix;

// Call this in LoadContent() and whenever the window is resized.
private void RebuildUIScale()
{
    float scaleX = GraphicsDevice.Viewport.Width  / (float)VirtualWidth;
    float scaleY = GraphicsDevice.Viewport.Height / (float)VirtualHeight;
    _uiScaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1f);

    // Invalidate layout so all Bounds are recomputed in virtual coords.
    _uiRoot.LayoutDirty = true;
}

// Wire to window resize event in Initialize():
//   Window.ClientSizeChanged += (s, e) => RebuildUIScale();

// In Draw():
_uiBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
    SamplerState.LinearClamp, null, null, null, _uiScaleMatrix);
_uiRoot.Draw(_uiBatch);
_uiBatch.End();

// In Update(), before _uiRoot.Update():
if (_uiRoot.LayoutDirty)
{
    _uiRoot.Measure(new Point(VirtualWidth, VirtualHeight));
    _uiRoot.Arrange(new Rectangle(0, 0, VirtualWidth, VirtualHeight));
    _uiRoot.LayoutDirty = false;
}
```
