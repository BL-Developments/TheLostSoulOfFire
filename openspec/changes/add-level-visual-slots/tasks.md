Voraussetzungen: `add-level-rooms` und `add-biome-run-flow` sind gemergt. Ein Level läuft in der Phase `Arena` mit `InLevel == true`.

## 1. Slots

- [x] 1.1 In `Rendering/Visuals/VisualIds.cs` die Konstanten `Biome1Room = "environment.biome1-room"`, `Biome1RoomWall = "environment.biome1-room-wall"`, `Biome1Exit = "prop.biome1-exit"` und `GradeBiome1 = "grade.biome1"`.
  - Visual-Specs `art/specs/<id>.md` nach `art/specs/README.md` mit `Status: dummy` und `Stil: hausstil`.
  - Weltgrößen: Boden und Wand wie `environment.arena` bzw. `environment.arena-wall` (aus `registry.json` ablesen), Ausgang 160 × 200.
  - `Lore:` verweist auf `../../openspec/changes/add-level-visual-slots/design.md`.
  - Der Ausgang hat die Clips `closed` und `open`.

  Prüfen: `VisualSpecTests` grün.
- [x] 1.2 `BiomeDefinition` (`Game/Levels/BiomeDefinition.cs`) um `LevelPalette Palette` erweitern: neuer `public sealed record LevelPalette(Color Floor, Color Grid, Color Wall, Color ExitLight)` und Visual-IDs (`string RoomId`, `string WallId`, `string ExitId`, `string GradeId`). Die Werte für Biom I kommen aus design.md, Abschnitt Platzhalterpalette, zum Beispiel Floor `new(178, 184, 190)`, Grid `new(132, 138, 146)`, Wall `new(26, 31, 44)`, ExitLight `new(236, 244, 255)`. Prüfen: Build.

## 2. Zeichnen

- [x] 2.1 Neue Datei `Rendering/LevelGreyboxRenderer.cs`: `public static class LevelGreyboxRenderer` mit `DrawRoom(SpriteBatch batch, Texture2D pixel, Rectangle bounds, Rectangle combat, LevelPalette palette)`, `DrawWall(…)` und `DrawExit(SpriteBatch batch, Texture2D pixel, Vector2 foot, bool open, LevelPalette palette)`.
  - Nur `batch.FillRectangle`/`DrawLine` aus `ShapeRenderer`; keine Listen oder Strings pro Frame.
  - Das Südtor ist eine Lücke in der Südwand, 184 breit, wie `DrawArenaLoop` es zeichnet.

  Prüfen: Aufnahme aus 3.1.
- [x] 2.2 In `GameWorld.cs`, `DrawScene`, im `else`-Zweig mit `_art.DrawEnvironment(batch, VisualIds.ArenaWall, …)`:
  - Bei `InLevel` zeichnet er je Slot `_art.HasArt(id) ? _art.DrawEnvironment(…) : LevelGreyboxRenderer.…`. `DrawArenaShading` und `_arenaAtmosphere.DrawBackground` entfallen im Level.
  - In der Prop-Auswahl (`GamePhase.Arena => _arenaProps.Concat(_sceneProps)`) liefert `InLevel` nur `_sceneProps`.
  - Ausgänge aus `add-level-rooms` werden über `Biome1Exit` gezeichnet, ohne Grafik über `LevelGreyboxRenderer.DrawExit`.

  Prüfen: Die Arena per `--dev --start arena` sieht unverändert aus (Aufnahme aus `--tour-visual-test` bzw. `--slice-visual-test` unverändert grün).
- [x] 2.3 `CurrentGradeId` (`GameWorld.cs`): Bei `InLevel` liefert sie die Grading-ID des Bioms; fehlt dafür die LUT, greift wie bisher `grade.neutral` in der Zeile mit `_art.GetSpriteTexture(CurrentGradeId) ?? …`. Prüfen: Unit-Test, falls die Logik in eine reine Funktion ausgelagert wird, sonst Aufnahme.
- [x] 2.4 Test in `tests/TheLostSoulOfFire.Tests/Visuals/` mit einer Test-Registry, die nur `environment.biome1-room` enthält: `VisualResolver.Resolve` liefert den Boden, Wand und Ausgang sind Dummy. Prüfen: `dotnet test`.

## 3. Prüfung

- [ ] 3.1 `--level-visual-test` nimmt einen Graubox-Raum mit geschlossenen und offenen Ausgängen auf, dazu eine Hub-Aufnahme zum Vergleich. Prüfen: Raum und Hub sind auf den ersten Blick verschieden, Gegner und Flammenfarben bleiben lesbar.
- [ ] 3.2 `openspec validate add-level-visual-slots --strict`, `dotnet build` und `dotnet test` ohne Fehler und neue Warnungen.
- [ ] 3.3 Anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start level --seed 4711`
