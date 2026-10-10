## Why

Der Reiter `MAP` im Charaktermenü ist seit `add-character-menu` ein Platzhalter. Seit `add-level-rooms` und `add-biome-run-flow` besteht ein Level aus einer Folge einzelner Räume mit Gabelungen, aber der Spieler sieht nirgends, welchen Weg er genommen hat. Björn möchte die Map als Nächstes bauen (Thread vom 10.10.2026).

## What Changes

- **Map des aktuellen Levels.** Der Reiter `MAP` zeigt die Raumfolge des Levels, in dem der Spieler gerade steht, als Stufenbild von unten (Startraum) nach oben.
- **Raum für Raum aufgedeckt.** Sichtbar sind nur Räume, die der Spieler in diesem Level betreten hat, und die Wege zwischen ihnen. Der Gegenweg einer Gabelung und alle noch nicht betretenen Räume bleiben verdeckt; auch die Länge des Levels verrät die Map nicht.
- **Ausgänge des aktuellen Raums** erscheinen als kurze Wegansätze ohne Zielraum, damit man an einer Gabelung links und rechts erkennt.
- **Aktueller Raum** ist hervorgehoben. Die vier Raumarten (Start, Kampf, Levelende mit Reisepunkt, Wächterraum) haben je ein einfaches Symbol und eine Legende.
- **Überschrift** `BIOM I · LEVEL 2` im Biom-Run, `LEVEL` im Developer-Start `level`.
- **Taste `M`** öffnet das Charaktermenü direkt auf `MAP`; auf `MAP` schließt `M` es wieder. `Tab` öffnet weiterhin auf `CHARAKTER`.
- **Außerhalb eines Levels** (Hub, Arena, Prolog, Sandbox) zeigt `MAP` den Hinweis `KEINE KARTE`.
- **Neues Level, neue Map.** Weiterreisen ins nächste Level, Niederlage oder Extraktion beginnen die Map von vorn.
- `MAP` ist kein Platzhalter mehr; `SKILLS` bleibt einer.

## Capabilities

### New Capabilities

- `level-map`: Map des aktuellen Levels im Charaktermenü mit Raum-für-Raum-Aufdeckung, Symbolen je Raumart und Taste `M`.

### Modified Capabilities

- `character-menu`: `MAP` ist kein Platzhalter mehr.

## Impact

- **Neuer Code:** `Game/Levels/LevelMap.cs` (Lage der Räume auf der Seite, ohne MonoGame-Grafik), `Rendering/LevelMapRenderer.cs`.
- **Geändert:** `LevelRun` merkt sich betretene Räume, `LevelLayout` nennt die Raumanzahl, `CharacterMenu` bekommt `OpenOn` und `IsPlaceholder` ohne `Map`, `GameWorld` (Taste `M`, Überschrift, Übergabe an das Zeichnen), `CinematicPresentation.DrawCharacterMenu`.
- **Nicht betroffen:** Raumerzeugung, Begegnungen, Grafik-Slots, HUD.
- **Abhängig von:** `add-level-rooms`, `add-biome-run-flow` (beide archiviert).
