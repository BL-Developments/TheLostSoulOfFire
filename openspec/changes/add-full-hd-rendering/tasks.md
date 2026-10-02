## 1. Auflösung trennen

- [ ] 1.1 `RenderResolution` mit logischer Auflösung 1280×720, Skalierung 1,5, Ausgabeauflösung 1920×1080 und `ScaleMatrix` anlegen; `GameBalance.BackBufferWidth/Height` darauf umstellen
- [ ] 1.2 Render-Ziel in `Game1` in Ausgabegröße anlegen und `GameWorld.Draw` weiterhin den logischen Viewport übergeben
- [ ] 1.3 Unit-Tests für `RenderResolution` (Ausgabegröße, Matrix) und für die unveränderte Zeigerumrechnung in `ResolutionManager`

## 2. Zeichnen skalieren

- [ ] 2.1 Alle `SpriteBatch.Begin` in `GameWorld` mit Welt- bzw. Bildschirm-Skalierung versehen
- [ ] 2.2 `SoulfireRenderer` (Szenenziel, `PresentScene`, `DrawVignette`) auf Ausgabegröße umstellen; `SoulfireLighting` und `SoulSensePresentation` erhalten die skalierte Weltmatrix
- [ ] 2.3 `PixelText` auf ganze Zielpixel runden

## 3. Filterung

- [ ] 3.1 Welt- und Effekt-Sprites mit `LinearClamp` zeichnen; `GenerateMipmaps=True` für alle Texturen in `Content.mgcb`
- [ ] 3.2 Finale Einpassung in `Game1` mit `LinearClamp`

## 4. Doku

- [ ] 4.1 Kommentar „World art remains PointClamp“ in `SoulfireRenderSettings` und betroffene Code-Kommentare an den gemalten Stil anpassen

## 5. Verifikation

- [ ] 5.1 Build und alle Tests grün; `openspec validate add-full-hd-rendering --strict`
- [ ] 5.2 Vorher/Nachher-Screenshots im Vollbild 1920×1080 von Titel, Prolog, Hub und Arena vergleichen; Zielen und Menübedienung bei kleinem und großem Fenster prüfen
- [ ] 5.3 Startbefehle zum Anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start hub` und `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena --wave 3`, jeweils mit `F11` ins Vollbild
