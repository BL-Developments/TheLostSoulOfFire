# MonoGame.Extended Tweening Reference

API signatures and ready-to-paste C# code for the `Tweener` system.

## Table of Contents
1. [Namespaces](#namespaces)
2. [Global Tweener setup](#global-tweener-setup)
3. [TweenTo signature](#tweento-signature)
4. [Easing Functions list](#easing-functions-list)
5. [Example: elastic button scale on hover](#example-elastic-button-scale-on-hover)
6. [Example: panel slide-in with simultaneous fade](#example-panel-slide-in-with-simultaneous-fade)
7. [Example: ordered animation sequence](#example-ordered-animation-sequence)

---

## Namespaces

```csharp
using MonoGame.Extended.Tweening;
```

---

## Global Tweener setup

Declare one `Tweener` for the whole game (or per scene) and update it every frame:

```csharp
// Field
private readonly Tweener _tweener = new Tweener();

// Update
protected override void Update(GameTime gameTime)
{
    _tweener.Update(gameTime.GetElapsedSeconds());
    base.Update(gameTime);
}
```

`Tweener` is not thread-safe. One instance per thread is sufficient for all tweens.

---

## TweenTo signature

```csharp
ITween TweenTo<TTarget, TValue>(
    TTarget target,                        // object that owns the property
    Expression<Func<TTarget, TValue>> expression, // lambda: obj => obj.Property
    TValue toValue,                        // destination value
    float  duration,                       // seconds
    float  delay = 0f                      // seconds before starting
)
```

Returns `ITween` — chainable with `.Easing()`, `.RepeatForever()`, `.AutoReverse()`.

Supported value types out of the box: `float`, `Vector2`, `Vector3`, `Vector4`, `Color`.

---

## Easing Functions list

All members of the static class `EasingFunctions`:

```
Linear
QuadraticIn       QuadraticOut       QuadraticInOut
CubicIn           CubicOut           CubicInOut
ElasticIn         ElasticOut         ElasticInOut
BackIn            BackOut            BackInOut
BounceIn          BounceOut          BounceInOut
```

**Important (v6):** Quartic, Quintic, Sine, Exponential, and Circular families
**do not exist** in MonoGame.Extended v6's `EasingFunctions`. The `InOut` suffix is
also `InOut`, never `EaseInOut`.

---

## Example: elastic button scale on hover

```csharp
// A UI button class with a tweenable Scale property
class MyButton
{
    public float Scale { get; set; } = 1f;
    public Vector2 Position { get; set; }
    // ... draw using Scale applied to SpriteBatch origin
}

// Fields in game/scene class
private readonly Tweener _tweener = new Tweener();
private readonly MyButton _button = new MyButton { Position = new Vector2(200, 150) };
private bool _isHovered;

// In Update — detect hover state change:
protected override void Update(GameTime gameTime)
{
    _tweener.Update(gameTime.GetElapsedSeconds());

    bool hoveredNow = _buttonRect.Contains(Mouse.GetState().Position);
    if (hoveredNow != _isHovered)
    {
        _isHovered = hoveredNow;
        float targetScale = _isHovered ? 1.12f : 1.0f;
        float duration    = _isHovered ? 0.18f : 0.10f;
        Func<float, float> easing = _isHovered
            ? EasingFunctions.ElasticOut
            : EasingFunctions.CubicOut;

        _tweener.TweenTo(_button, b => b.Scale, targetScale, duration)
                .Easing(easing);
    }

    base.Update(gameTime);
}

// In Draw — use _button.Scale as the scale parameter:
protected override void Draw(GameTime gameTime)
{
    _spriteBatch.Begin();
    Vector2 origin = new Vector2(_buttonTexture.Width / 2f, _buttonTexture.Height / 2f);
    _spriteBatch.Draw(_buttonTexture, _button.Position, null, Color.White,
        0f, origin, _button.Scale, SpriteEffects.None, 0f);
    _spriteBatch.End();
}
```

---

## Example: panel slide-in with simultaneous fade

```csharp
// A UI panel with tweenable Position and Alpha
class InventoryPanel
{
    public Vector2 Position { get; set; }
    public float Alpha { get; set; } = 0f;
    // ... visibility flag, draw method
}

// Fields
private readonly Tweener _tweener = new Tweener();
private readonly InventoryPanel _panel;
private readonly Vector2 _panelOpenPos  = new Vector2(50, 100);
private readonly Vector2 _panelClosedPos; // off bottom of screen

// Open the panel (call from input handler or event)
private void OpenPanel()
{
    _tweener.TweenTo(_panel, p => p.Position, _panelOpenPos, 0.35f)
            .Easing(EasingFunctions.CubicOut);
    _tweener.TweenTo(_panel, p => p.Alpha, 1.0f, 0.25f)
            .Easing(EasingFunctions.Linear);
}

// Close the panel
private void ClosePanel()
{
    _tweener.TweenTo(_panel, p => p.Position, _panelClosedPos, 0.25f)
            .Easing(EasingFunctions.CubicIn);
    _tweener.TweenTo(_panel, p => p.Alpha, 0.0f, 0.20f)
            .Easing(EasingFunctions.Linear);
}

// In Draw — use _panel.Alpha as the color tint:
protected override void Draw(GameTime gameTime)
{
    _spriteBatch.Begin();
    _spriteBatch.Draw(_panelTexture, _panel.Position,
        Color.White * _panel.Alpha);
    _spriteBatch.End();
}
```

---

## Example: ordered animation sequence

Use `delay` to chain steps in order without callbacks:

```csharp
// Sequence: move to target → rotate 180° → fade out
float d = 0.3f;

_tweener.TweenTo(_obj, o => o.Position, targetPos, duration: d, delay: 0f)
        .Easing(EasingFunctions.QuadraticOut);

_tweener.TweenTo(_obj, o => o.Rotation, MathF.PI, duration: d, delay: d)
        .Easing(EasingFunctions.CubicInOut);

_tweener.TweenTo(_obj, o => o.Alpha, 0f, duration: d, delay: d * 2)
        .Easing(EasingFunctions.Linear);
```

All three tweens are registered at the same time; the `delay` parameter staggers them.
