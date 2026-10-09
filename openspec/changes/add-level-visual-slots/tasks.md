## 1. Slots

- [ ] 1.1 Visual-IDs `environment.biome1-room`, `environment.biome1-room-wall`, `prop.biome1-exit` und `grade.biome1` in `VisualIds`; Visual-Specs unter `art/specs/` mit `Status: dummy`, Verweis auf diesen Change und Weltgrößen der Arena. Prüfen: Spec-Prüfung der Registry-Tests grün.
- [ ] 1.2 Platzhalterpalette in `BiomeDefinition` für Biom I. Prüfen: Build ohne neue Warnungen.

## 2. Zeichnen

- [ ] 2.1 `LevelGreyboxRenderer` für Wand, Boden, Südtor und Ausgänge (geschlossen und offen) ohne Allokationen pro Frame. Prüfen: Aufnahme aus 3.1.
- [ ] 2.2 `ArtAssets`: je Slot Grafik aus der Registry oder Graubox; im Level keine gemalten Ebenen und Props der Arena mehr. Prüfen: Test mit einer Test-Registry, die nur den Raumboden liefert; der Boden kommt aus der Registry, Wand und Ausgang aus der Graubox.
- [ ] 2.3 `CurrentGradeId` liefert im Level `grade.biome1` und fällt ohne LUT auf neutral zurück. Prüfen: Unit-Test.

## 3. Prüfung

- [ ] 3.1 `--level-visual-test` nimmt einen Graubox-Raum mit geschlossenen und offenen Ausgängen auf, dazu eine Hub-Aufnahme zum Vergleich. Prüfen: Raum und Hub sind auf den ersten Blick verschieden, Gegner und Flammenfarben bleiben lesbar.
- [ ] 3.2 `openspec validate add-level-visual-slots --strict`, `dotnet build` und `dotnet test` ohne Fehler und neue Warnungen.
- [ ] 3.3 Anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start level --seed 4711`
