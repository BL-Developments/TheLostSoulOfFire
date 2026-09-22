# MonoGame UI Core Reference

Templates for the structural layer of the UI system. All classes use the `YourGame.UI` namespace — replace with your project namespace.

## Table of Contents
1. [UIElement.Core.cs — base partial class](#uielementcorecs)
2. [UIContainer — recursive traversal](#uicontainer)
3. [Frame rendering order](#frame-rendering-order)
4. [Stardew-style IClickableMenu skeleton](#stardew-style-iclickablemenu-skeleton)

---

## UIElement.Core.cs

```csharp
// UIElement.Core.cs
// This is a partial class. See also:
//   UIElement.Layout.cs  (monogame-ui-layout skill)
//   UIElement.Focus.cs   (monogame-ui-focus skill)

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    public abstract partial class UIElement
    {
        public UIElement Parent { get; internal set; }
        public List<UIElement> Children { get; } = new List<UIElement>();

        // Absolute screen rectangle — set by the layout Arrange pass.
        // Do NOT write to this directly; let the layout engine manage it.
        public Rectangle Bounds { get; internal set; }

        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;

        // 0.0 = fully transparent, 1.0 = fully opaque.
        // Multiply with parent opacity when drawing.
        public float Opacity { get; set; } = 1f;

        // Called every frame when IsEnabled == true.
        public virtual void Update(GameTime gameTime) { }

        // Called every frame when IsVisible == true.
        // Draw ONLY this element here; children are drawn by UIContainer.
        public virtual void Draw(SpriteBatch spriteBatch) { }

        // Override to react when a child is added/removed (e.g., invalidate layout).
        protected virtual void OnChildAdded(UIElement child) { }
        protected virtual void OnChildRemoved(UIElement child) { }

        // Convenience: effective opacity considering parent chain.
        public float EffectiveOpacity
        {
            get
            {
                float o = Opacity;
                UIElement p = Parent;
                while (p != null) { o *= p.Opacity; p = p.Parent; }
                return o;
            }
        }

        // Convenience: effective visibility considering parent chain.
        public bool EffectiveVisible
        {
            get
            {
                if (!IsVisible) return false;
                UIElement p = Parent;
                while (p != null)
                {
                    if (!p.IsVisible) return false;
                    p = p.Parent;
                }
                return true;
            }
        }
    }
}
```

---

## UIContainer

```csharp
// UIContainer.cs
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YourGame.UI
{
    // UIContainer owns the recursive Update/Draw traversal.
    // Extend this for panels, StackPanel, ScrollView, etc.
    public class UIContainer : UIElement
    {
        // Set to true whenever children are added/removed or content changes size.
        // The layout engine checks this flag before running Measure/Arrange.
        internal bool LayoutDirty = true;

        public void Add(UIElement child)
        {
            if (child.Parent != null)
                ((UIContainer)child.Parent).Remove(child);

            child.Parent = this;
            Children.Add(child);
            LayoutDirty = true;
            OnChildAdded(child);
        }

        public void Remove(UIElement child)
        {
            if (Children.Remove(child))
            {
                child.Parent = null;
                LayoutDirty = true;
                OnChildRemoved(child);
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsEnabled) return;
            base.Update(gameTime);
            // Indexed for-loop — no allocations.
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].IsEnabled)
                    Children[i].Update(gameTime);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            base.Draw(spriteBatch);          // draw self (background, border, etc.)
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

## Frame Rendering Order

```csharp
// Game1.cs (or your root Game class)

private SpriteBatch _worldBatch;
private SpriteBatch _uiBatch;
private UIContainer _uiRoot;

protected override void LoadContent()
{
    _worldBatch = new SpriteBatch(GraphicsDevice);
    _uiBatch    = new SpriteBatch(GraphicsDevice);
    _uiRoot     = new UIContainer();
    // add children to _uiRoot here or in screen/state setup
}

protected override void Update(GameTime gameTime)
{
    // 1. World update
    UpdateWorld(gameTime);

    // 2. UI update (hit testing happens inside)
    if (_uiRoot.IsEnabled)
        _uiRoot.Update(gameTime);

    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.Black);

    // 1. World pass — uses camera transform
    _worldBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend,
        SamplerState.PointClamp, null, null, null, _cameraMatrix);
    DrawWorld(_worldBatch);
    _worldBatch.End();

    // 2. UI pass — NO transformMatrix (absolute screen coords)
    _uiBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
        SamplerState.LinearClamp, null, null);
    _uiRoot.Draw(_uiBatch);
    _uiBatch.End();

    base.Draw(gameTime);
}
```

---

## Stardew-style IClickableMenu skeleton

Use this when building a single, static menu screen without the full tree architecture.

```csharp
// MyMenu.cs — Stardew Valley–style menu skeleton
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace YourGame.UI
{
    public class MyMenu
    {
        // The menu's bounding rectangle on screen.
        public Rectangle Bounds;

        // Typed component fields — one per interactive element.
        private SimpleButton _okButton;
        private SimpleButton _cancelButton;

        // Layout is calculated once in the constructor from current viewport size.
        public MyMenu(GraphicsDevice gd)
        {
            int width  = 400;
            int height = 300;
            int x = gd.Viewport.Width  / 2 - width  / 2;
            int y = gd.Viewport.Height / 2 - height / 2;
            Bounds = new Rectangle(x, y, width, height);

            _okButton     = new SimpleButton(new Rectangle(x + width - 120, y + height - 50, 100, 36), "OK");
            _cancelButton = new SimpleButton(new Rectangle(x + 20,          y + height - 50, 100, 36), "Cancel");
        }

        // Called from Game.Update after polling mouse state.
        public void ReceiveLeftClick(int mouseX, int mouseY)
        {
            if (_okButton.ContainsPoint(mouseX, mouseY))
            {
                // handle OK
            }
            else if (_cancelButton.ContainsPoint(mouseX, mouseY))
            {
                // handle Cancel
            }
        }

        public void Draw(SpriteBatch sb, SpriteFont font)
        {
            // Draw background
            sb.Draw(/* panel texture */, Bounds, Color.White);
            _okButton.Draw(sb, font);
            _cancelButton.Draw(sb, font);
        }
    }

    // Minimal component — just bounds + label.
    public class SimpleButton
    {
        public Rectangle Bounds;
        public string Label;
        public SimpleButton(Rectangle bounds, string label) { Bounds = bounds; Label = label; }
        public bool ContainsPoint(int x, int y) => Bounds.Contains(x, y);
        public void Draw(SpriteBatch sb, SpriteFont font)
        {
            sb.Draw(/* button texture */, Bounds, Color.White);
            sb.DrawString(font, Label, new Vector2(Bounds.X + 8, Bounds.Y + 8), Color.Black);
        }
    }
}
```
