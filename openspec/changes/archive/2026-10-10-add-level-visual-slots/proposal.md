## Why

Die Räume aus `add-level-rooms` sind Kopien der Arena. Die Level sollen sich am Ende aber deutlich von Hub, Prolog und Arena abheben, und die gemalte Grafik kommt erst mit dem Hausstil (`add-visual-vertical-slice`). Bis dahin braucht es Platzhalter, die man ohne Codeänderung gegen gemalte Grafik tauschen kann.

## What Changes

- **Grafik-Slots je Biom:** Jedes Biom bekommt Visual-IDs für
  - den Raumboden, zum Beispiel `environment.biome1-room`,
  - die Raumwand mit dem Südtor (`environment.biome1-room-wall`),
  - den Ausgang (`prop.biome1-exit`),
  - das Grading (`grade.biome1`).
- **Visual-Specs:** Jede dieser IDs hat eine Visual-Spec mit `Status: dummy`.
- **Graubox:** Solange die Registry für einen Slot keine Grafik liefert, zeichnet das Spiel den Raum in der Platzhalterpalette des Bioms. Die Palette hebt sich klar von Hub, Prolog und Arena ab und ersetzt im Level das Aussehen der Arena.
- **Austausch:** Bild und Registry-Eintrag für einen Slot ersetzen die Graubox beim nächsten Start. Am Code ändert sich dabei nichts.
- **Grading:** Ohne LUT ist das Grading eines Bioms neutral.

## Capabilities

### New Capabilities

- `level-visual-slots`: Visual-IDs je Biom für Raumboden, Raumwand, Ausgang und Grading, Graubox in Platzhalterpalette und Austausch ohne Codeänderung.

### Modified Capabilities

- `level-rooms`: Räume behalten Grenzen und Kampffläche der Arena, aber nicht mehr ihr Aussehen.

## Impact

- **Neuer Code:** `Rendering/LevelGreyboxRenderer`, Platzhalterpalette in `BiomeDefinition`, Konstanten in `VisualIds`.
- **Geänderte Stellen:** `ArtAssets` (Slot statt Arena-Ebenen im Level), `CurrentGradeId` und `GameWorld.Levels.cs` (Zeichnen).
- **Art-Specs:** `art/specs/environment.biome1-room.md`, `environment.biome1-room-wall.md`, `prop.biome1-exit.md` und `grade.biome1.md`.
- **Abhängig von:** `add-level-rooms`, für `BiomeDefinition` auch `add-biome-run-flow`. Unabhängig von `add-room-wave-scaling`.
