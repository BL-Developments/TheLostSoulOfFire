# MonoGame UI Dropdown Reference

## Table of Contents
1. [UIOverlayManager — root overlay pass](#uioverlaymanager--root-overlay-pass)
2. [UIDropdown — full implementation](#uidropdown--full-implementation)
3. [Wiring into Game.Draw](#wiring-into-gamedraw)

---

## UIOverlayManager — Root Overlay Pass

```csharp
// UIOverlayManager.cs
// Draws UI elements that must appear on top of everything (dropdowns, tooltips, context menus).

using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    public static class UIOverlayManager
    {
        private static readonly List<IOverlayDrawable> _overlays = new List<IOverlayDrawable>();

        public static void Register(IOverlayDrawable overlay)
        {
            if (!_overlays.Contains(overlay))
                _overlays.Add(overlay);
        }

        public static void Unregister(IOverlayDrawable overlay) => _overlays.Remove(overlay);

        // Call AFTER _uiRoot.Draw() in Game.Draw()
        public static void Draw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < _overlays.Count; i++)
                _overlays[i].DrawOverlay(spriteBatch);
        }

        public static void CloseAll()
        {
            for (int i = _overlays.Count - 1; i >= 0; i--)
                _overlays[i].CloseOverlay();
        }
    }

    public interface IOverlayDrawable
    {
        void DrawOverlay(SpriteBatch spriteBatch);
        void CloseOverlay();
    }
}
```

---

## UIDropdown — Full Implementation

```csharp
// UIDropdown.cs
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace YourGame.UI
{
    public class UIDropdown : UIElement, IOverlayDrawable
    {
        private readonly SpriteFont _font;
        private readonly Texture2D  _pixel;
        private readonly int        _screenHeight;

        private readonly List<string> _items = new List<string>();
        private int  _selectedIndex   = -1;
        private int  _highlightIndex  = -1;
        private bool _isOpen          = false;

        public int  ItemHeight    { get; set; } = 28;
        public int  MaxVisibleItems { get; set; } = 6;
        public Color HeaderColor  { get; set; } = new Color(50, 50, 50);
        public Color ListColor    { get; set; } = new Color(40, 40, 40);
        public Color HighlightColor { get; set; } = new Color(100, 149, 237, 160);
        public Color TextColor    { get; set; } = Color.White;

        // Computed once on open
        private Rectangle _listBounds;
        private int       _scrollOffset;

        public string SelectedText  => (_selectedIndex >= 0 && _selectedIndex < _items.Count)
                                       ? _items[_selectedIndex] : "";
        public int    SelectedIndex => _selectedIndex;

        public event System.Action<UIDropdown, int, string> SelectionChanged;

        public UIDropdown(SpriteFont font, Texture2D pixel, int screenHeight)
        {
            _font         = font;
            _pixel        = pixel;
            _screenHeight = screenHeight;
            IsFocusable   = true;
        }

        public void AddItem(string text) => _items.Add(text);

        public void ClearItems() { _items.Clear(); _selectedIndex = -1; Close(); }

        public void SelectIndex(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            _selectedIndex = index;
            SelectionChanged?.Invoke(this, index, _items[index]);
        }

        // ---- Open / Close ----

        public void Open()
        {
            if (_isOpen || _items.Count == 0) return;
            _isOpen        = true;
            _highlightIndex = _selectedIndex;
            _scrollOffset  = 0;

            int visible    = System.Math.Min(_items.Count, MaxVisibleItems);
            int listHeight = visible * ItemHeight;
            bool flipUp    = (Bounds.Bottom + listHeight) > _screenHeight;

            _listBounds = flipUp
                ? new Rectangle(Bounds.X, Bounds.Y - listHeight, Bounds.Width, listHeight)
                : new Rectangle(Bounds.X, Bounds.Bottom,          Bounds.Width, listHeight);

            UIOverlayManager.Register(this);
        }

        public void Close()
        {
            _isOpen = false;
            UIOverlayManager.Unregister(this);
        }

        public void CloseOverlay() => Close();

        // ---- Input ----

        public override void OnClick(UIPointerEventArgs args)
        {
            if (!IsEnabled) return;
            if (_isOpen) Close(); else Open();
            args.Handled = true;
        }

        public override void HandleKeyboardInput(UIKeyboardEventArgs args)
        {
            if (!_isOpen)
            {
                if (args.Key == Keys.Enter || args.Key == Keys.Space) { Open(); args.Handled = true; }
                return;
            }

            switch (args.Key)
            {
                case Keys.Up:
                    if (_highlightIndex > 0) _highlightIndex--;
                    args.Handled = true; break;
                case Keys.Down:
                    if (_highlightIndex < _items.Count - 1) _highlightIndex++;
                    args.Handled = true; break;
                case Keys.Enter:
                    if (_highlightIndex >= 0) SelectIndex(_highlightIndex);
                    Close();
                    args.Handled = true; break;
                case Keys.Escape:
                    Close();
                    args.Handled = true; break;
            }
        }

        // ---- Draw ----

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            float opacity = EffectiveOpacity;

            // Header background
            spriteBatch.Draw(_pixel, Bounds, HeaderColor * opacity);
            // Selected text
            string label = _selectedIndex >= 0 ? _items[_selectedIndex] : "";
            float  ty    = Bounds.Y + (Bounds.Height - _font.LineSpacing) / 2f;
            spriteBatch.DrawString(_font, label, new Vector2(Bounds.X + 6, ty), TextColor * opacity);
            // Arrow indicator
            string arrow = _isOpen ? "▲" : "▼";
            Vector2 arrowSize = _font.MeasureString(arrow);
            spriteBatch.DrawString(_font, arrow,
                new Vector2(Bounds.Right - arrowSize.X - 6, ty), TextColor * opacity);
        }

        // Called by UIOverlayManager.Draw — draws the expanded list on top of everything
        public void DrawOverlay(SpriteBatch spriteBatch)
        {
            if (!_isOpen) return;
            float opacity = EffectiveOpacity;

            // List background
            spriteBatch.Draw(_pixel, _listBounds, ListColor * opacity);

            int visible = System.Math.Min(_items.Count, MaxVisibleItems);
            for (int i = 0; i < visible; i++)
            {
                int itemIndex = i + _scrollOffset;
                if (itemIndex >= _items.Count) break;

                int   iy       = _listBounds.Y + i * ItemHeight;
                var   rowRect  = new Rectangle(_listBounds.X, iy, _listBounds.Width, ItemHeight);

                // Highlight hovered/keyboard-selected item
                if (itemIndex == _highlightIndex)
                    spriteBatch.Draw(_pixel, rowRect, HighlightColor);

                float ty = iy + (ItemHeight - _font.LineSpacing) / 2f;
                spriteBatch.DrawString(_font, _items[itemIndex],
                    new Vector2(_listBounds.X + 6, ty), TextColor * opacity);
            }
        }

        // Hit-test the overlay list on pointer down (called by UIInteractionManager)
        public bool OverlayContainsPoint(Point p) => _isOpen && _listBounds.Contains(p);

        public void HandleOverlayClick(Point p)
        {
            if (!_isOpen) return;
            int relY  = p.Y - _listBounds.Y;
            int index = relY / ItemHeight + _scrollOffset;
            if (index >= 0 && index < _items.Count)
            {
                SelectIndex(index);
                Close();
            }
        }
    }
}
```

---

## Wiring into Game.Draw

```csharp
// In Game.Draw():
_uiBatch.Begin();
_uiRoot.Draw(_uiBatch);        // normal UI tree
UIOverlayManager.Draw(_uiBatch); // overlays drawn on top
_uiBatch.End();

// In UIInteractionManager.Update(), handle overlay clicks first:
// if active dropdown overlaps click position, route to dropdown, not normal hit test.
```
