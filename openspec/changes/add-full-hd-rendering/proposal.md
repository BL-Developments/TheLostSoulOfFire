## Why

Das Spiel zeichnet heute alles in ein festes 1280×720-Bild und skaliert es pixelgenau (PointClamp) auf das Fenster. Auf einem Full-HD-Bildschirm ist das Faktor 1,5. Pixel werden dadurch ungleich breit, und die hochaufgelösten Figuren-Frames (384×384), die auf etwa 0,3–0,7 verkleinert gezeichnet werden, verlieren Details und flimmern ohne Mipmaps. Björn hat am 02.10.2026 entschieden, dass das Spiel gemalt wie Bastion aussehen soll, nicht als Pixel-Art (siehe `docs/current/DECISION-LOG.md`). Für diesen Stil ist ein scharfes Full-HD-Bild mit weicher Skalierung der richtige Weg.

## What Changes

- Das fertige Bild wird in 1920×1080 gezeichnet. Spiellogik, Kamera-Sichtfeld, HUD-Layout und Zeigerumrechnung bleiben in den bisherigen 1280×720 Spielkoordinaten.
- Eine zentrale Render-Skalierung (1,5) wird auf jeden Zeichenaufruf angewendet: Welt-Kameramatrix, Licht, Soul Sense, Vignette, Bildschirm-Feedback, HUD und Overlays.
- Welt-Sprites werden mit linearer Filterung und Mipmaps gezeichnet statt mit PointClamp. Generierte Primitive (Rechtecke, Linien, Kreise) bleiben unverändert.
- Das fertige Bild wird weich (LinearClamp) in das Fenster eingepasst. Im Vollbild auf einem Full-HD-Bildschirm wird es 1:1 ausgegeben.
- Screenshots werden in 1920×1080 aufgenommen.
- Die Pixelschrift (`PixelText`) wird auf ganze Zielpixel gerundet, damit sie bei 1,5 nicht ungleichmäßig wird.

## Capabilities

### New Capabilities

Keine.

### Modified Capabilities

- `adaptive-window-presentation`: Trennung zwischen logischer Auflösung (1280×720) und Zeichenauflösung (1920×1080); weiche statt pixelgenauer Einpassung; Screenshots in Zeichenauflösung.

## Impact

Betrifft `Game1` (Render-Ziel, Einpassung, Übergabe des logischen Viewports), `GameWorld` (alle `SpriteBatch.Begin`), `SoulfireRenderer` (Szenenziel, Compositing, Vignette), `SoulSensePresentation`, `PixelText`, `Content.mgcb` (Mipmaps für Sprite-Texturen) und Screenshot-Tests. Keine neuen Abhängigkeiten. Speicherbedarf steigt durch Mipmaps und das größere Render-Ziel leicht. Die Assets selbst werden nicht neu gezeichnet.
