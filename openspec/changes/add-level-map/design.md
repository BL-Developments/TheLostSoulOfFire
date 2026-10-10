## Context

- `LevelLayout` (`Game/Levels/LevelLayout.cs`) hält die Stufen eines Levels. Jeder `LevelRoom` hat `Id`, `Kind`, `Progress` (Stufenindex) und `Exits` (links vor rechts). Ids sind in Stufenreihenfolge ab 0 vergeben.
- `LevelRun` (`Game/Levels/LevelRun.cs`) kennt den aktuellen Raum, aber nicht, welche Räume schon betreten wurden. Ein neues Level erzeugt einen neuen `LevelRun` (`BeginLevel`, `TravelToNextBiomeLevel`); außerhalb eines Levels ist `_levelRun` null.
- Das Charaktermenü (`Menu/CharacterMenu.cs`) hat die Reiter `CHARAKTER`, `MAP`, `SKILLS`, `FÄHIGKEITEN`. `MAP` und `SKILLS` zeichnen `NOCH NICHT VERFÜGBAR` (`CinematicPresentation.DrawCharacterMenu`). `C` öffnet das Menü direkt auf `FÄHIGKEITEN` (`GameWorld.OpenSkillsMenu`).
- Entscheidungen von Björn (10.10.2026): nur das aktuelle Level; Raum für Raum aufdecken; Reiter `MAP` und Taste `M`. Für Orte ohne Level und für Raumsymbole gelten die Vorschläge aus dem Thread (Hinweis `KEINE KARTE`, vier einfache Symbole), weil Björn ihnen nicht widersprochen hat.

## Goals / Non-Goals

**Goals:**
- Der Spieler sieht im Menü, wo er im Level steht und welchen Weg er genommen hat.
- Aufdecken und Lage der Räume sind ohne `GraphicsDevice` testbar.
- Keine Allokationen pro Frame beim Zeichnen.

**Non-Goals:**
- Minimap im HUD; das HUD zeigt schon `RAUM <n>`.
- Map über mehrere Level oder das ganze Biom.
- Vorschau unbetretener Räume, Raumtypen außer den vier vorhandenen, Collectible- oder NPC-Markierungen.
- Speichern der Map über einen Run hinaus.

## Decisions

### Betretene Räume merkt sich `LevelRun`

`LevelRun` bekommt ein `bool[]` in der Größe von `LevelLayout.RoomCount`. Der Konstruktor markiert den Startraum, `TryTakeExit` markiert den betretenen Raum. `WasVisited(int roomId)` liest es.

*Alternative eigene `LevelMap`-Instanz mit eigenem Zustand:* Sie müsste bei jedem Raumwechsel und Levelwechsel mitgeführt werden. Im `LevelRun` setzt sich die Map mit jedem neuen Level von selbst zurück.

### Aufgedeckt heißt betreten

Gezeichnet werden nur betretene Räume und Verbindungen zwischen zwei betretenen Räumen. Damit bleibt der Gegenweg einer Gabelung verdeckt, auch nachdem die Wege wieder zusammenlaufen, und die Gesamtzahl der Stufen ist nicht ablesbar.

Für den aktuellen Raum zeichnet die Map zusätzlich je Ausgang einen kurzen Wegansatz in Richtung seines Zielraums, ohne den Zielraum selbst. Die Ausgänge sieht der Spieler ohnehin in der Nordwand; der Ansatz hilft nur, links und rechts auf der Map zuzuordnen. Ein Ausgang, der noch zu ist (Raum nicht geräumt), wird gedämpft gezeichnet.

### Feste Lage statt Neuverteilung

`LevelMap.NodePosition(Rectangle page, LevelLayout layout, LevelRoom room)` rechnet:
- `y`: Unterkante der Seite minus `BottomMargin` minus `Progress × StageSpacing`. Die Map wächst also von unten nach oben, und betretene Räume verschieben sich nicht, wenn neue dazukommen.
- `x`: Seitenmitte bei einer Stufe mit einem Raum; bei zwei Räumen links bzw. rechts um `ForkOffsetX` versetzt, in der Reihenfolge der Stufe (Index 0 links).

Bei höchstens acht Stufen (`LevelCombatStagesMax` plus Start und Levelende) passt das mit `StageSpacing = 56` in die Seite des Charaktermenüs bei 1080p. Die Werte sind Layout, keine Balance, und stehen deshalb als Konstanten in `LevelMap`, nicht in `GameBalance`.

### Raumsymbole

Alle Symbole sind Primitive aus `ShapeRenderer` und `UiKit`, keine neuen Texturen:

| Raumart | Symbol |
|---|---|
| Startraum | Quadrat, nur Umriss |
| Kampfraum | gefülltes Quadrat |
| Levelende mit Reisepunkt | Raute (`UiKit.FillDiamond`) |
| Wächterraum | größere Raute in `DeathFlame` |

Der aktuelle Raum steht in `DeathFlameBright` und bekommt einen atmenden Ring (`ShapeRenderer.DrawCircle`), der im Takt der Reitermarkierung pulsiert. Die Rauten-Markierung des Menüs (`DrawSelectionMarker`) wird bewusst nicht genutzt, weil sie mit dem Levelende-Symbol verwechselt würde. Betretene Räume stehen in `SoulWhite` gedämpft. Unter der Map steht eine Legende mit denselben Symbolen. Der Wächterraum braucht dafür `LevelRun.GuardianAtEnd` als öffentliche Eigenschaft.

### Taste `M`

`CharacterMenu.OpenOn(CharacterMenuTab tab)` öffnet das Menü wie `Open()` und wählt dann den Reiter. `OpenSkillsMenu` nutzt es für `C`, die neue Weiche für `M`.
- Geschlossenes Menü: `M` öffnet in jeder Phase außer der Titelphase auf `MAP`, unter denselben Bedingungen wie `Tab` (kein Pausenmenü, kein Dev-Menü offen).
- Offenes Menü auf `MAP`: `M` schließt es wie `Tab`.
- Offenes Menü auf einem anderen Reiter: `M` wählt `MAP`.

`M` ist bisher an keine Aktion gebunden.

### Überschrift ohne Allokation pro Frame

`GameWorld` hält `_levelMapHeading` und setzt es dort, wo ein `LevelRun` entsteht: `BIOM <römisch> · LEVEL <n>` in `BeginBiomeLevel` und `TravelToNextBiomeLevel`, `LEVEL` in `StartLevel`. Das Zeichnen bekommt den fertigen String.

## Risks / Trade-offs

- **Verdeckter Gegenweg auch nach dem Zusammenlaufen:** Der Spieler sieht nicht, dass es dort einen zweiten Raum gab. Das ist gewollt („Raum für Raum“); wer mehr sehen will, braucht eine eigene Entscheidung.
- **Feste Stufenhöhe:** Werden die Level einmal länger als acht Stufen, läuft die Map oben aus der Seite. Dann muss `StageSpacing` aus der Stufenanzahl berechnet werden; das verrät aber die Levellänge. Bis dahin reicht der feste Wert.
- **Spec-Abweichung `FÄHIGKEITEN`:** Der Reiter `FÄHIGKEITEN` steht im Code, aber nicht in `openspec/specs/character-menu/spec.md`. Dieser Change ändert daran nichts.
