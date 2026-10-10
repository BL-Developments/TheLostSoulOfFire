# travel-points Specification

## Purpose
TBD - created by archiving change add-arena-travel-point. Update Purpose after archive.
## Requirements
### Requirement: Reisepunkt nach Welle 5
Das System SHALL in der Pause nach Welle 5 der Arena einen Reisepunkt zeigen, solange es keine Levels gibt. In der Sandbox MUST kein Reisepunkt erscheinen. Startet die nächste Welle, verschwindet der Reisepunkt.

#### Scenario: Welle 5 wird geleert
- **WHEN** alle Gegner der fünften Welle besiegt sind
- **THEN** erscheint der Reisepunkt in der Pause, getrennt von Kisten und Auslösezone

#### Scenario: Spieler startet die Welle ohne Reisepunkt
- **WHEN** der Spieler in der Pause nach Welle 5 in der Auslösezone `E` drückt
- **THEN** startet Welle 6, beide Run-Bestände bleiben vollständig im Run, und der Reisepunkt verschwindet

### Requirement: Menü mit drei Entscheidungen und Vorschau
Das System SHALL beim Drücken von `E` in Reichweite des Reisepunkts ein Menü mit den Entscheidungen Teilsichern und weiter, Weiter ohne Sichern und Extrahieren öffnen. Das Menü SHALL je Entscheidung die zu sichernden und die im Run verbleibenden Beträge beider Währungen zeigen. Solange das Menü offen ist, SHALL die Welt stillstehen. `Esc` SHALL das Menü ohne Entscheidung schließen.

#### Scenario: Menü öffnen und schließen
- **WHEN** der Spieler das Menü öffnet und `Esc` drückt
- **THEN** schließt das Menü, alle Bestände sind unverändert, und der Reisepunkt bleibt nutzbar

### Requirement: Teilsichern überträgt die Hälfte abgerundet
Das System SHALL beim Teilsichern je Währung 50 % des Run-Bestands, abgerundet, in den gesicherten Bestand übertragen, das Profil speichern und die nächste Welle starten. Der Rest SHALL im Run bleiben.

#### Scenario: Ungerade Bestände
- **WHEN** der Spieler mit 175 Geld und 81 Glut im Run teilsichert
- **THEN** werden 87 Geld und 40 Glut gesichert, und 88 Geld und 41 Glut bleiben im Run

#### Scenario: Niederlage nach Teilsicherung
- **WHEN** der Spieler nach einer Teilsicherung stirbt
- **THEN** geht nur der verbliebene Run-Bestand verloren, und die gesicherten Beträge bleiben erhalten

### Requirement: Extrahieren sichert alles und beendet den Run
Das System SHALL beim Extrahieren beide Run-Bestände vollständig sichern, das Profil speichern und in den Hub wechseln. Der Hub SHALL kurz anzeigen, was extrahiert wurde. Der nächste Arena-Run SHALL bei Welle 1 beginnen.

#### Scenario: Spieler extrahiert
- **WHEN** der Spieler am Reisepunkt Extrahieren wählt
- **THEN** steht er im Hub, beide Run-Bestände sind null, und die gesicherten Bestände sind um die vorherigen Run-Bestände gestiegen

### Requirement: Genau eine Entscheidung je Reisepunkt
Das System SHALL je Reisepunkt nur die erste Entscheidung ausführen. Weitere Eingaben MUST NOT erneut sichern.

#### Scenario: Doppelte Bestätigung
- **WHEN** im selben oder im nächsten Frame eine zweite Entscheidung eingeht
- **THEN** bleiben alle Bestände so, wie die erste Entscheidung sie hinterlassen hat

