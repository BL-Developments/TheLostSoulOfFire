## Context

`Game1` legt ein `RenderTarget2D` in `GameBalance.BackBufferWidth/Height` (1280×720) an, zeichnet die Welt hinein und skaliert das Ergebnis mit `SamplerState.PointClamp` seitenverhältnistreu ins Fenster (`ResolutionManager`). `GameWorld.Draw` bekommt den Viewport des Render-Ziels und nutzt ihn für Kameragrenzen, Vollbild-Rechtecke und HUD-Layout. `SoulfireRenderer` legt ein Szenenziel in Viewportgröße an und setzt es mit Graduierung und Vignette zusammen. `SpriteBatch.Begin` wird an etwa zehn Stellen aufgerufen (`GameWorld`, `SoulfireRenderer`, `SoulSensePresentation`, `Game1`); fast alle nutzen PointClamp. Alle Sprite-Texturen werden ohne Mipmaps gebaut.

Am 02.10.2026 hat Björn den Grafikstil „gemalt wie Bastion“ gewählt. Damit entfällt die Pixel-Regel „ein gezeichnetes Pixel ist ein Bildschirmpixel“.

## Goals / Non-Goals

**Goals:**

- Scharfes, detailreiches 1920×1080-Bild ohne ungleiche Pixel.
- Sichtfeld, Layout, Gameplay und Eingaben bleiben exakt wie heute.
- Die Skalierung steht an genau einer Stelle, damit später 1440p oder 4K nur eine Zahl sind.

**Non-Goals:**

- Auflösungsauswahl im Einstellungsmenü oder dynamische Zeichenauflösung nach Fenstergröße.
- Größeres Sichtfeld oder andere Seitenverhältnisse (Ultrawide).
- Neue oder neu gemalte Assets.
- Änderungen an der Start-Fenstergröße.

## Decisions

### Logische und Zeichenauflösung trennen

Eine kleine statische Klasse `RenderResolution` (unter `Core/`) hält `LogicalWidth/Height` (1280×720), `Scale` (1,5) und daraus `OutputWidth/Height` (1920×1080) sowie `ScaleMatrix`. `GameBalance.BackBufferWidth/Height` werden durch die logischen Werte ersetzt oder verweisen darauf.

`Game1` legt das Render-Ziel in Ausgabegröße an, übergibt `GameWorld.Draw` aber weiterhin den logischen Viewport. So bleiben Kamera (`Camera2D.Follow`/`GetTransform`), HUD-Positionen und alle `viewport.Width/Height`-Rechnungen unverändert.

Alternative: alle Koordinaten auf 1920×1080 umstellen. Verworfen, weil das jedes Layout, jede Kameragrenze und jede Raumgröße berührt und das Sichtfeld verändern würde.

### Skalierung über die Transformationsmatrix

Jeder `SpriteBatch.Begin` erhält `transformMatrix`:

- Welt: `camera.GetTransform(viewport, offset) * RenderResolution.ScaleMatrix`.
- Bildschirmraum (Feedback, HUD, Overlays, Menü): `RenderResolution.ScaleMatrix`.

`SoulfireRenderer.BeginScene`, `PresentScene` und `DrawVignette` arbeiten mit der Ausgabegröße, weil sie ganze Render-Ziele und Vollbild-Rechtecke zeichnen. `SoulfireLighting` und `SoulSensePresentation` bekommen die bereits skalierte Weltmatrix übergeben und bleiben sonst unverändert.

### Sampling: linear mit Mipmaps für Sprites

Welt- und Effekt-Sprites werden mit `SamplerState.LinearClamp` gezeichnet; in `Content.mgcb` wird `GenerateMipmaps=True` für alle Texturen unter `Textures/` gesetzt. Das passt zum gemalten Stil und verhindert Flimmern bei stark verkleinerten Frames. Die 1×1-Pixeltextur für Primitive braucht keine Mipmaps.

Der finale Schritt in `Game1` passt das 1920×1080-Bild mit `LinearClamp` ins Fenster ein. Bei 1:1 (Full-HD-Vollbild) ist das verlustfrei; bei anderen Fenstergrößen ist weiche Skalierung im gemalten Stil gewollt.

### Pixelschrift auf ganze Zielpixel runden

`PixelText` zeichnet Glyphen aus ganzzahligen Pixelquadraten. Unter Faktor 1,5 würde Größe 1 zu 1,5 Zielpixeln. `PixelText` rundet deshalb Glyphengröße und Position im Zielraum auf ganze Pixel: Größe 2 wird exakt 3 Zielpixel, Größe 1 wird 2 statt 1,5. Das HUD-Layout bleibt in logischen Koordinaten.

Alternative: Schrift durch eine SpriteFont ersetzen. Verworfen für diesen Change, weil es ein eigener Stil-Entscheid ist.

### Screenshots

`ScreenshotCapture` speichert das Render-Ziel und damit künftig 1920×1080. Visual-Tests, die Bildgrößen prüfen, werden angepasst.

## Risks / Trade-offs

- [Eine vergessene `Begin`-Stelle zeichnet in der linken oberen Ecke zu klein] → Alle `Begin`-Aufrufe per Suche erfassen; Screenshots von Titel, Prolog, Hub und Arena vor und nach dem Change vergleichen.
- [Lineare Filterung macht harte Primitive-Kanten leicht weicher] → Für den gemalten Stil gewollt; bei störenden Stellen gezielt PointClamp für reine Primitive-Batches.
- [Mipmaps erhöhen Build-Zeit und Speicher um rund ein Drittel pro Textur] → Für die aktuelle Asset-Menge unkritisch.
- [Kleine Fenster (1280×720 Startgröße) zeigen ein verkleinertes Bild] → Gewollt; der Gewinn zeigt sich im Vollbild und bei großen Fenstern.
