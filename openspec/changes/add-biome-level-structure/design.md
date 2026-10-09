## Context

- **Phasen:** `GameWorld` kennt die Phasen `Title`, `Prologue`, `Antechamber`, `EnteringArena` und `Arena` (`Game/GameWorld.cs`).
- **Arena:** Sie ist ein festes Rechteck (`GameBalance.ArenaBounds`, `CombatBounds`).
  - Ihre Wellen laufen über `ArenaWaves` und `ArenaWaveRun`.
  - Kisten, Glutgutschrift und Reisepunkt fragen direkt `_phase == GamePhase.Arena` ab.
- **Bewegung:** Spieler und Gegner werden nur in ein Rechteck geklemmt (`Player.Update(..., Rectangle movementBounds)`, `Enemy.UpdateCommon`). Wände gibt es nirgends.
- **Grafik:** Sie hängt seit `add-visual-vertical-slice` an Visual-IDs.
  - Eine Umgebungs-ID ist eine gemalte Ebene in Weltgröße, bei Bedarf in Kacheln bis 2048 Pixel.
  - Fehlt ein Eintrag, meldet die Registry einen Dummy (`VisualResolver`).
  - Das Grading wählt je Bereich eine LUT, sonst neutral (`CurrentGradeId`).
- **Beschlüsse:**
  - Ein Biom hat drei Level, der Boss steht am Ende von Level 3 (#52).
  - Reisepunkte stehen am Ende von Level 1 und 2 (#53).
  - Nach einer Niederlage geht es in die Homebase, und der nächste Versuch beginnt bei Level 1 (#14).
  - Handgebaut und deterministisch (`WORLD-GRAMMAR.md` §7).
  - Tür I führt in die neuen Level (Björn, 09.10.2026).

## Goals / Non-Goals

**Goals:**
- Ein Level-Modell, mit dem der Biom-Brief später nur Daten füllt (Räume, Raumtypen, Begegnungen) und kein neues System braucht.
- Ein Biom-Run, der spielbar die beschlossenen Regeln zeigt: drei Level, Reisepunkte, Wächterraum, Niederlage zum Hub.
- Grafik-Slots, über die gemalte Böden ohne Codeänderung die Graubox ersetzen.
- Reine Logik für Begehbarkeit, Begegnungen und Run-Zustände, damit Tests sie ohne `GraphicsDevice` prüfen.

**Non-Goals:**
- Boss, Soul-Release-Phase und Freischaltung von Tür II (#92, #93); Spielstand des Run-Fortschritts.
- Biomwahl im Hub (#62), NPCs (#85), Collectibles (#72), Koop (#97).
- Zufällige oder prozedurale Raumfolgen, Hindernisse innerhalb von Räumen, Gefahren.
- Gemalte Grafik und Thema von Biom I (#10).
- Umbau der Arena: Sie bleibt unverändert, auch ihr Reisepunkt nach Welle 5.

## Decisions

### Leveldaten als C#-Definitionen

`Game/Levels/` enthält diese Typen:
- `RoomKind`: Entrance, Combat, LargeCombat, Traversal, Reward, Elite, LevelEnd, Guardian. Das ist eine Auswahl aus `WORLD-GRAMMAR.md` §6. Weitere Typen wie Environmental Story oder Soul Sense kommen mit ihren Inhalten.
- `RoomDefinition`: Id, Rechteck, Typ und Begegnung als Liste von `ArenaPush`.
- `DoorwayDefinition`: Rechteck, die beiden verbundenen Räume und die Seite.
- `LevelDefinition`: Räume, Durchgänge, Eingangspunkt, Visual-ID der Bodenebene; die Grenzen werden aus den Räumen berechnet.
- `BiomeDefinition`: Nummer, drei Level, Visual-ID des Gradings, Platzhalterpalette.

Die Daten für Biom I liegen in `BiomeCatalog`. Begegnungsgrößen und Kistenbeträge stehen als benannte Werte in `GameBalance`.

*Alternative JSON oder Tiled:* Ein Editor-Workflow lohnt erst, wenn es Inhalt und Grafik gibt. Ein zweiter Ladeweg jetzt wäre Spekulation. Die Definitionen sind so geschnitten, dass ein späterer Loader sie nur befüllt.

### Begehbarkeit: Rechtecke, durch die ein Körper ganz passt

`LevelWalkArea` hält alle Raum- und Durchgangsrechtecke, wobei Durchgänge um einen Körperradius in die Nachbarräume hineinreichen.
- Eine Position ist gültig, wenn der Kreis des Körpers ganz in mindestens einem Rechteck liegt.
- `Resolve(previous, candidate, radius)` versucht zuerst die volle Bewegung, dann nur X, dann nur Y, und hält sonst die alte Position. Daraus entsteht das Gleiten an Wänden, und ein Dash endet an der Wand.
- Geschlossene Durchgänge fallen aus der Menge heraus.

Gegner werden weiter in ein Rechteck geklemmt, nämlich in ihren Raum. Dafür braucht `Enemy` keine Änderung.

*Alternative Tile-Kollision:* Sie würde ein Kachelraster voraussetzen, das die gemalten Böden nicht haben. Rechtecke reichen für die Graubox und für die gemalte Ebene, die später aus demselben Kollisionslayout gemalt wird (Vorgehen wie Slice-Task 8.1).

### Raum-Begegnung wiederverwendet `ArenaWaveRun`

`RoomEncounter` verbindet Raum, `ArenaWaveRun` und Zustand (wartet, läuft, geräumt).
- Der Start erfolgt, wenn der Kreis des Spielers ganz im Raumrechteck liegt.
- Spawnpunkte kommen aus `ArenaWaves.ChooseSpawnPositions` mit den Raumgrenzen statt der Arena. Ankündigung, Nachschub und Höchstzahl bleiben damit gleich.
- Geräumt ist der Raum, wenn alle Schübe erschienen und alle Gegner der Begegnung besiegt sind.

### Run-Zustand als reine Logik

`BiomeRun` kennt die Zustände Homebase, InLevel, LevelEnd, Extracted, Defeated und BiomeComplete sowie Biom und Levelindex.
- Methoden: `Start(biome, level = 1)`, `ReachLevelEnd()`, `TravelOn()`, `Extract()`, `Defeat()`, `CompleteBiome()`, `ReturnHome()`.
- Jede Methode prüft den Ausgangszustand und lässt unerlaubte Übergänge ohne Wirkung. Das deckt die Abnahme von #66 und #67 ab.

`GameFlowRules` bekommt passende Regeln für die Phasen. In `GamePhase` kommt `BiomeLevel` dazu, und `EnteringArena` wird zu `EnteringBiome`. Der Arena-Pfad bleibt über den Developer-Start erhalten.

### Anbindung in `GameWorld`

Neues Teilstück `GameWorld.BiomeRun.cs`. Es übernimmt Levelaufbau, Raumwechsel, Begegnungen, Reisepunkt am Levelende, Wächterraum, Abschluss und Niederlage.

Stellen, die heute fest `GamePhase.Arena` voraussetzen, fragen eine kleine Eigenschaft ab, etwa `IsRunCombatPhase`:
- Glutgutschrift, Kisten, Fähigkeitskosten und HUD gelten in Arena und Biom-Level.
- `ActiveCombatBounds` und `ActiveWorldBounds` liefern im Level den aktuellen Raum bzw. die Levelgrenzen.

Der Reisepunkt verwendet `TravelPoint` und das vorhandene Menü. Nur Position und Folge („nächstes Level“ statt „nächste Welle“) unterscheiden sich.

Niederlage: Nach dem vorhandenen Todeszustand folgt eine Bergungsblende von `GameBalance.RecoveryFadeDuration`. Danach geht es in den Hub, wie bei der Extraktion. `R` startet im Biom keinen Neuversuch.

### Graubox und Grafik-Slots

`Rendering/LevelGreyboxRenderer` zeichnet nur, und zwar in dieser Reihenfolge:
1. Wandmasse;
2. Raumböden;
3. Durchgänge;
4. geschlossene Durchgänge als Gitter;
5. eine Raumtyp-Markierung am Boden (Symbol und Farbstreifen).

Dazu kommen Visual-IDs, die Konstanten in `VisualIds` sind:
- je Level eine Bodenebene, zum Beispiel `environment.biome1-level1`, mit `worldSize` gleich den Levelgrenzen;
- je Biom das Grading `grade.biome1`.

Liefert die Registry eine Bodenebene, zeichnet `ArtAssets` sie anstelle der Graubox. Wände und Raumtyp-Markierungen entfallen dann; die Debug-Ansicht zeigt weiterhin die Kollisionsrechtecke.

Für jede neue ID gibt es eine Visual-Spec unter `art/specs/` mit `Status: dummy`. Sie verweist auf diesen Change, weil es noch kein Lore-Blatt gibt.

Platzhalterpalette für Biom I:
- kühles, helleres Schiefergrau für Böden und fast schwarzes Blau für Wände;
- je Raumtyp eine Signalfarbe in kleinen Flächen.

Damit ist die Graubox gegenüber Hub und Prolog eindeutig als Platzhalter erkennbar. Sie legt kein Thema fest. Die Werte stehen in `BiomeDefinition`.

### Biom I als Graubox

Drei Level mit 5 bis 6 Räumen, damit jeder Raumtyp einmal vorkommt:
- **Level 1:** Eingang → Kampf → Durchgangsraum → Kampf → Levelende.
- **Level 2:** Eingang → Kampf → Belohnung → Großer Kampf → Levelende.
- **Level 3:** Eingang → Kampf → Elite → Durchgangsraum → Wächter.

Die Begegnungen nutzen die Zusammensetzungen der Arena-Wellen 1 bis 4 und steigern sich. Der Wächterraum ist ein großer Raum mit drei Schüben, angelehnt an Arena-Welle 8. Die endgültige Raumfolge kommt aus dem Biom-Brief.

### Developer-Start und Tests

- `DeveloperStartOptions` bekommt den Bereich `biome:1` und `--level 1..3`.
- Tests prüfen:
  - die Leveldefinitionen: jeder Raum erreichbar, genau ein Eingang und ein Ende, Begegnungen nicht leer, Durchgänge breit genug;
  - Begehbarkeit und Gleiten;
  - die Zustandsübergänge von `BiomeRun`;
  - Start, Räumen und Durchgänge von `RoomEncounter`;
  - das Parsen der Startoptionen.
- Ein automatischer Lauf `--biome-visual-test` nimmt je Level und Raumtyp ein Bild auf und beendet sich.

## Risks / Trade-offs

- **`GameWorld` wird noch größer:** Gegenmittel sind ein eigenes Teilstück und reine Logiktypen; nur die Phasenabfragen ändern sich in `GameWorld.cs`.
- **Doppelter Code für Arena und Levelende** (Reisepunkt, Abschluss): Es werden dieselben Methoden verwendet, nur der Aufrufer unterscheidet sich. Die Arena-Pfade bleiben, bis über ihre Zukunft entschieden ist.
- **Graubox statt Arena hinter Tür I:** Der normale Ablauf sieht vorerst schlichter aus als die gemalte Arena. Das ist bewusst gewählt (Björn, 09.10.2026); die Arena bleibt per Developer-Start erreichbar.
- **Rechteck-Kollision begrenzt Raumformen:** Schräge oder runde Räume gehen nur als Zusammensetzung von Rechtecken. Für Graubox und Brief reicht das; Hindernisse im Raum wären ein eigener Change.
- **Run-Fortschritt wird nicht gespeichert:** Wer das Spiel mitten im Run schließt, startet wieder im Hub. Gesicherte Bestände bleiben erhalten wie heute (#64, #65).
