---
name: monogame-localization
description: MonoGame localization and multilingual font guide covering JSON-based i18n (runtime file loading, no Content Pipeline), IStringLocalizer pattern with System.Text.Json, culture switching, SpriteFont CharacterRegions for non-ASCII languages (Spanish accents, Cyrillic, CJK), and TTF font setup. Use this skill whenever the user asks about translating a game, adding multiple languages, showing accented or non-Latin characters, ñ not rendering, missing glyphs, font charset, UTF-8 text issues, culture/locale switching, or loading text from JSON files — even if they just say "my Spanish text breaks" or "how do I add French support".
---

# MonoGame Localization Implementation Guide

This skill covers JSON-based localization loaded at runtime (bypassing the Content Pipeline) and SpriteFont configuration for multilingual text rendering. For API signatures and ready-to-copy patterns, read `references/localization.md`.

## Architecture Overview

Modern MonoGame localization avoids `.resx` files and the Content Pipeline for translation strings. Instead:

1. **Translation files** are plain JSON files shipped alongside the game binary (e.g., `Content/Localization/es.json`).
2. **Runtime loading** uses `System.Text.Json` — any player can open and edit the file with a text editor.
3. **A central `Localizer` service** handles culture switching and string lookup.
4. **SpriteFonts** must declare the Unicode ranges for every character the active language uses — this is the most common source of runtime crashes in multilingual games.

## JSON File Format

Use simple flat key/value objects per language file:

```json
{
  "menu.play":    "Jugar",
  "menu.options": "Opciones",
  "hud.score":    "Puntuación: {0}",
  "hud.lives":    "Vidas: {0}"
}
```

Place files in `Content/Localization/<culture>.json` (e.g., `en.json`, `es.json`, `de.json`). These are **not** added to the `.mgcb` file — they are copied raw to the output directory.

To copy them automatically, add to your `.csproj`:

```xml
<ItemGroup>
  <Content Include="Content\Localization\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

## IStringLocalizer Pattern

Implement a minimal `IStringLocalizer` backed by `System.Text.Json`. The interface keeps the rest of your codebase decoupled from the loading mechanism:

```csharp
public interface IStringLocalizer
{
    string this[string key] { get; }
    string Format(string key, params object[] args);
    void SetCulture(string cultureName);
}
```

The concrete `JsonLocalizer` class:
- Loads the JSON file for the requested culture on `SetCulture()`.
- Falls back to the `en.json` default if a key is missing in the target language.
- Caches the parsed dictionary — never re-reads disk unless the culture changes.

See `references/localization.md` for the full implementation.

## Culture Detection and Switching

Detect the system language at startup and allow the player to override it in settings:

```csharp
// Auto-detect:
string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName; // "es", "de", "fr"

// Initialize localizer:
_localizer = new JsonLocalizer("Content/Localization");
_localizer.SetCulture(culture);

// Player override (save to user settings, apply on restart or live):
_localizer.SetCulture("ja");
```

## SpriteFont CharacterRegions — The Critical Step

**This is the most common crash source in multilingual MonoGame games.**

By default, a `.spritefont` only covers Basic Latin (U+0020–U+007E). Switching to Spanish, French, Russian, or Japanese will throw a `KeyNotFoundException` the moment you try to render a character outside that range.

### Rule: one SpriteFont per language group

Group languages by their Unicode requirements and create one `.spritefont` per group (or use a union font if the game supports mixed scripts).

### Spanish / French / German / Portuguese (Latin Extended)

Add the Latin-1 Supplement block:

```xml
<CharacterRegions>
  <CharacterRegion>
    <Start>&#32;</Start>   <!-- space -->
    <End>&#126;</End>      <!-- ~ Basic Latin -->
  </CharacterRegion>
  <CharacterRegion>
    <Start>&#160;</Start>  <!-- NBSP -->
    <End>&#255;</End>      <!-- ÿ  Latin-1 Supplement -->
  </CharacterRegion>
</CharacterRegions>
```

This single addition covers: á é í ó ú ü ñ ¿ ¡ à â ç è ê ë î ï ô œ ù û ä ö ß and more.

### Russian / Bulgarian (Cyrillic)

```xml
<CharacterRegions>
  <CharacterRegion><Start>&#32;</Start><End>&#126;</End></CharacterRegion>
  <CharacterRegion>
    <Start>&#1024;</Start>  <!-- Ѐ Cyrillic block start -->
    <End>&#1279;</End>      <!-- ӿ Cyrillic Supplement end -->
  </CharacterRegion>
</CharacterRegions>
```

### Japanese (Hiragana + Katakana)

CJK ideograms require thousands of glyphs — use a `.ttf` with CJK support (e.g., Noto Sans JP) and keep font size small (12–14pt) to limit atlas size. Generating the full Kanji block (U+4E00–U+9FFF) produces a very large texture; consider using `DefaultCharacter` fallback aggressively.

```xml
<CharacterRegions>
  <CharacterRegion><Start>&#32;</Start><End>&#126;</End></CharacterRegion>
  <CharacterRegion>
    <Start>&#12288;</Start>  <!-- Ideographic space -->
    <End>&#12351;</End>      <!-- CJK Symbols and Punctuation -->
  </CharacterRegion>
  <CharacterRegion>
    <Start>&#12352;</Start>  <!-- Hiragana -->
    <End>&#12447;</End>
  </CharacterRegion>
  <CharacterRegion>
    <Start>&#12448;</Start>  <!-- Katakana -->
    <End>&#12543;</End>
  </CharacterRegion>
</CharacterRegions>
```

## TTF Font Setup

To use a `.ttf` file instead of a system font:

1. Copy the `.ttf` file into the same directory as the `.spritefont` file (or a `Fonts/` subdirectory in Content).
2. In the `.spritefont`, set `<FontName>` to the exact filename (including extension):

```xml
<FontName>MyFont.ttf</FontName>
```

3. Add the `.spritefont` to the `.mgcb` file. The `.ttf` itself is **not** added to MGCB — the pipeline reads it automatically when building the `.spritefont`.

## Loading Localized Non-Text Assets

For language-specific textures, audio, or sprite sheets, use `ContentManager.LoadLocalized<T>()`. It automatically looks for a culture-suffixed file before falling back to the default:

```csharp
// Looks for MyCharacter.es.xnb → MyCharacter.es-ES.xnb → MyCharacter.xnb
Texture2D portrait = Content.LoadLocalized<Texture2D>("Characters/MyCharacter");
```

The culture suffix comes from `CultureInfo.CurrentCulture`. Set it before calling `LoadLocalized`:

```csharp
CultureInfo.CurrentCulture = new CultureInfo("es-ES");
```

## Anti-Patterns to Avoid

- **Never use `.resx` for new projects** — the generated Designer.cs couples string access to compiled code and makes player-editable translations impossible.
- **Never hard-code localized strings** in source code — use the `IStringLocalizer` index everywhere: `_loc["menu.play"]`.
- **Never add JSON translation files to the MGCB** — they must remain as raw files, not compiled `.xnb`.
- **Never assume Basic Latin covers a language** — even English UK uses `£` (U+00A3), which is outside the default range.
- **Never create `SpriteFont` instances per frame** — load them in `LoadContent()` and keep them for the scene lifetime.

## Reference

For `JsonLocalizer` full source, `IStringLocalizer` registration, `SpriteFont` schema templates per language, and format-string helpers, see `references/localization.md`.
