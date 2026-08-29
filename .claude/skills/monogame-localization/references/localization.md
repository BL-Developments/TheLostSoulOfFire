# MonoGame Localization Reference

## Table of Contents
1. [IStringLocalizer interface](#istringlocalizer-interface)
2. [JsonLocalizer implementation](#jsonlocalizer-implementation)
3. [Registering the localizer as a game service](#registering-the-localizer-as-a-game-service)
4. [Using the localizer in game code](#using-the-localizer-in-game-code)
5. [JSON translation file structure](#json-translation-file-structure)
6. [.csproj setup for raw JSON copy](#csproj-setup-for-raw-json-copy)
7. [SpriteFont CharacterRegion templates](#spritefont-characterregion-templates)
8. [Full .spritefont template (Latin + Spanish)](#full-spritefont-template-latin--spanish)
9. [Full .spritefont template (Cyrillic)](#full-spritefont-template-cyrillic)
10. [TTF font reference in .spritefont](#ttf-font-reference-in-spritefont)
11. [LoadLocalized for non-text assets](#loadlocalized-for-non-text-assets)
12. [Culture detection helpers](#culture-detection-helpers)

---

## IStringLocalizer interface

```csharp
using System.Globalization;

public interface IStringLocalizer
{
    /// <summary>Returns the localized string for key, or the key itself if not found.</summary>
    string this[string key] { get; }

    /// <summary>Returns a formatted localized string. Equivalent to string.Format(this[key], args).</summary>
    string Format(string key, params object[] args);

    /// <summary>Loads the translation file for cultureName (e.g., "es", "de", "ja").</summary>
    void SetCulture(string cultureName);

    /// <summary>Currently active culture name.</summary>
    string CurrentCulture { get; }
}
```

---

## JsonLocalizer implementation

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public sealed class JsonLocalizer : IStringLocalizer
{
    private readonly string _basePath;       // e.g., "Content/Localization"
    private readonly string _fallbackCulture; // e.g., "en"

    private Dictionary<string, string> _current  = new();
    private Dictionary<string, string> _fallback = new();

    public string CurrentCulture { get; private set; } = string.Empty;

    /// <param name="basePath">Directory that contains the *.json language files.</param>
    /// <param name="fallbackCulture">Culture used when a key is missing in the active language.</param>
    public JsonLocalizer(string basePath, string fallbackCulture = "en")
    {
        _basePath        = basePath;
        _fallbackCulture = fallbackCulture;

        // Always pre-load the fallback so we never return null:
        _fallback = Load(fallbackCulture);
    }

    public string this[string key]
    {
        get
        {
            if (_current.TryGetValue(key, out var value)) return value;
            if (_fallback.TryGetValue(key, out value))    return value;
            return key; // Return the key itself so missing strings are obvious
        }
    }

    public string Format(string key, params object[] args)
        => string.Format(this[key], args);

    public void SetCulture(string cultureName)
    {
        if (cultureName == CurrentCulture) return;

        CurrentCulture = cultureName;

        if (cultureName == _fallbackCulture)
        {
            _current = _fallback; // Reuse; no double-load
            return;
        }

        _current = Load(cultureName);
    }

    private Dictionary<string, string> Load(string cultureName)
    {
        var path = Path.Combine(_basePath, $"{cultureName}.json");

        if (!File.Exists(path))
        {
            // Try two-letter code if full culture ("es-ES" → "es"):
            var twoLetter = cultureName.Length >= 2 ? cultureName[..2] : cultureName;
            path = Path.Combine(_basePath, $"{twoLetter}.json");
        }

        if (!File.Exists(path))
        {
            // Nothing found — return empty dict, fallback will cover
            return new Dictionary<string, string>();
        }

        var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
               ?? new Dictionary<string, string>();
    }
}
```

---

## Registering the localizer as a game service

```csharp
// In Game.Initialize():
var localizer = new JsonLocalizer("Content/Localization", fallbackCulture: "en");

// Auto-detect system language, clamp to supported cultures:
var systemCulture = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
var supported = new HashSet<string> { "en", "es", "de", "fr", "ja" };
localizer.SetCulture(supported.Contains(systemCulture) ? systemCulture : "en");

// Register so any component can resolve it:
Services.AddService(typeof(IStringLocalizer), localizer);

// Keep a shortcut reference:
_loc = localizer;
```

Resolving from a component:

```csharp
var loc = (IStringLocalizer)Game.Services.GetService(typeof(IStringLocalizer));
```

---

## Using the localizer in game code

```csharp
// Simple lookup:
string label = _loc["menu.play"];

// Formatted string with a placeholder:
string score = _loc.Format("hud.score", _currentScore);    // "Score: 1500"

// Switch language at runtime (e.g., options menu):
_loc.SetCulture("es");

// Render with SpriteBatch:
_spriteBatch.DrawString(_font, _loc["menu.play"], position, Color.White);
```

---

## JSON translation file structure

`Content/Localization/en.json`
```json
{
  "menu.play":       "Play",
  "menu.options":    "Options",
  "menu.quit":       "Quit",
  "hud.score":       "Score: {0}",
  "hud.lives":       "Lives: {0}",
  "dialog.confirm":  "Are you sure?"
}
```

`Content/Localization/es.json`
```json
{
  "menu.play":       "Jugar",
  "menu.options":    "Opciones",
  "menu.quit":       "Salir",
  "hud.score":       "Puntuación: {0}",
  "hud.lives":       "Vidas: {0}",
  "dialog.confirm":  "¿Estás seguro?"
}
```

`Content/Localization/de.json`
```json
{
  "menu.play":       "Spielen",
  "menu.options":    "Optionen",
  "menu.quit":       "Beenden",
  "hud.score":       "Punkte: {0}",
  "hud.lives":       "Leben: {0}",
  "dialog.confirm":  "Bist du sicher?"
}
```

Naming convention: use **IETF two-letter language codes** (`en`, `es`, `de`, `fr`, `ja`, `ru`, `zh`). The `JsonLocalizer` also accepts full codes like `es-ES` and trims to two letters automatically.

---

## .csproj setup for raw JSON copy

```xml
<!-- Copy all localization JSON files to output — NOT through MGCB: -->
<ItemGroup>
  <Content Include="Content\Localization\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

Do **not** add these files to `Content.mgcb` — they must remain as editable plain text, not compiled `.xnb`.

---

## SpriteFont CharacterRegion templates

### Basic Latin only (English)
```xml
<CharacterRegions>
  <CharacterRegion>
    <Start>&#32;</Start>    <!-- space (U+0020) -->
    <End>&#126;</End>       <!-- tilde ~ (U+007E) -->
  </CharacterRegion>
</CharacterRegions>
```

### Latin + Latin-1 Supplement (Spanish, French, German, Portuguese, Italian)
Covers: á é í ó ú ü ñ ¿ ¡ à â ç è ê ë î ï ô œ ù û ä ö ß ø å æ and more.
```xml
<CharacterRegions>
  <CharacterRegion>
    <Start>&#32;</Start>
    <End>&#126;</End>
  </CharacterRegion>
  <CharacterRegion>
    <Start>&#160;</Start>   <!-- NBSP (U+00A0) -->
    <End>&#255;</End>       <!-- ÿ (U+00FF) -->
  </CharacterRegion>
</CharacterRegions>
```

### Cyrillic (Russian, Bulgarian, Serbian, Ukrainian)
```xml
<CharacterRegions>
  <CharacterRegion>
    <Start>&#32;</Start>
    <End>&#126;</End>
  </CharacterRegion>
  <CharacterRegion>
    <Start>&#1024;</Start>  <!-- Ѐ (U+0400) -->
    <End>&#1279;</End>      <!-- ӿ (U+04FF) -->
  </CharacterRegion>
</CharacterRegions>
```

### Greek
```xml
<CharacterRegions>
  <CharacterRegion>
    <Start>&#32;</Start>
    <End>&#126;</End>
  </CharacterRegion>
  <CharacterRegion>
    <Start>&#880;</Start>   <!-- Ͱ (U+0370) -->
    <End>&#1023;</End>      <!-- Ͽ (U+03FF) -->
  </CharacterRegion>
</CharacterRegions>
```

### Japanese (Hiragana + Katakana + CJK punctuation)
Use a CJK-capable TTF (e.g., Noto Sans JP, Source Han Sans). Keep font size ≤14pt.
```xml
<CharacterRegions>
  <CharacterRegion>
    <Start>&#32;</Start>
    <End>&#126;</End>
  </CharacterRegion>
  <CharacterRegion>
    <Start>&#12288;</Start> <!-- Ideographic space (U+3000) -->
    <End>&#12351;</End>     <!-- CJK Symbols and Punctuation (U+303F) -->
  </CharacterRegion>
  <CharacterRegion>
    <Start>&#12352;</Start> <!-- Hiragana start (U+3040) -->
    <End>&#12447;</End>     <!-- Hiragana end (U+309F) -->
  </CharacterRegion>
  <CharacterRegion>
    <Start>&#12448;</Start> <!-- Katakana start (U+30A0) -->
    <End>&#12543;</End>     <!-- Katakana end (U+30FF) -->
  </CharacterRegion>
</CharacterRegions>
```

### Chinese Simplified (common Hanzi subset — large atlas!)
Only include if your game ships specifically in Chinese. This generates a ~4096×4096 atlas.
```xml
<CharacterRegions>
  <CharacterRegion>
    <Start>&#32;</Start>
    <End>&#126;</End>
  </CharacterRegion>
  <CharacterRegion>
    <Start>&#19968;</Start> <!-- 一 (U+4E00) CJK Unified Ideographs start -->
    <End>&#40959;</End>     <!-- 龿 (U+9FFF) end of common block -->
  </CharacterRegion>
</CharacterRegions>
```

---

## Full .spritefont template (Latin + Spanish)

```xml
<?xml version="1.0" encoding="utf-8"?>
<XnaContent xmlns:Graphics="Microsoft.Xna.Framework.Content.Pipeline.Graphics">
  <Asset Type="Graphics:FontDescription">
    <FontName>Arial</FontName>     <!-- System font name, or filename.ttf for embedded font -->
    <Size>16</Size>
    <Spacing>0</Spacing>
    <UseKerning>true</UseKerning>
    <Style>Regular</Style>
    <DefaultCharacter>?</DefaultCharacter>   <!-- Renders '?' for any unregistered glyph -->
    <CharacterRegions>
      <CharacterRegion>
        <Start>&#32;</Start>
        <End>&#126;</End>
      </CharacterRegion>
      <CharacterRegion>
        <Start>&#160;</Start>
        <End>&#255;</End>
      </CharacterRegion>
    </CharacterRegions>
  </Asset>
</XnaContent>
```

---

## Full .spritefont template (Cyrillic)

```xml
<?xml version="1.0" encoding="utf-8"?>
<XnaContent xmlns:Graphics="Microsoft.Xna.Framework.Content.Pipeline.Graphics">
  <Asset Type="Graphics:FontDescription">
    <FontName>Arial</FontName>
    <Size>16</Size>
    <Spacing>0</Spacing>
    <UseKerning>true</UseKerning>
    <Style>Regular</Style>
    <DefaultCharacter>?</DefaultCharacter>
    <CharacterRegions>
      <CharacterRegion>
        <Start>&#32;</Start>
        <End>&#126;</End>
      </CharacterRegion>
      <CharacterRegion>
        <Start>&#1024;</Start>
        <End>&#1279;</End>
      </CharacterRegion>
    </CharacterRegions>
  </Asset>
</XnaContent>
```

---

## TTF font reference in .spritefont

Option A — Font installed on the build machine (system font):
```xml
<FontName>Segoe UI</FontName>
```

Option B — Font file in the same directory as the `.spritefont`:
```xml
<FontName>MyCustomFont.ttf</FontName>
```

Option C — Font file in a subdirectory:
```xml
<FontName>../Fonts/MyCustomFont.ttf</FontName>
```

The `.ttf` file is **not** added to the MGCB — only the `.spritefont` is. The Content Pipeline reads the TTF during the build and embeds the rasterized glyphs in the compiled `.xnb`.

---

## LoadLocalized for non-text assets

`ContentManager.LoadLocalized<T>()` looks for culture-specific variants before the default:

```
MyCharacter.es-ES.xnb  →  MyCharacter.es.xnb  →  MyCharacter.xnb
```

```csharp
// Set the culture before loading:
System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("es-ES");

// Load — automatically picks the Spanish version if it exists:
Texture2D portrait  = Content.LoadLocalized<Texture2D>("Characters/Hero");
Song      voiceover = Content.LoadLocalized<Song>("Audio/Intro");
SpriteFont localFont = Content.LoadLocalized<SpriteFont>("Fonts/UI");
```

Name your localized XNB source files in MGCB as `Hero.es-ES` (or `Hero.es`) to produce the right output filename.

---

## Culture detection helpers

```csharp
using System.Globalization;

// Full culture tag, e.g., "es-ES", "en-US", "ja-JP":
string fullCulture = CultureInfo.CurrentCulture.Name;

// Two-letter ISO code, e.g., "es", "en", "ja":
string shortCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

// Override the thread culture (affects LoadLocalized and number formatting):
CultureInfo.CurrentCulture = new CultureInfo("es-ES");
CultureInfo.CurrentUICulture = new CultureInfo("es-ES");

// Safe culture resolution with fallback:
private static string ResolveCulture(IEnumerable<string> supported, string fallback = "en")
{
    var twoLetter = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
    return supported.Contains(twoLetter) ? twoLetter : fallback;
}
```
