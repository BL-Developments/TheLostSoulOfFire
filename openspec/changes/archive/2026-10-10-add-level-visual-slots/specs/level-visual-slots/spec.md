## Purpose

Gibt jedem Biom austauschbare Grafik-Slots für seine Räume. Solange keine gemalte Grafik vorliegt, erscheinen die Räume als Graubox in einer eigenen Platzhalterpalette, die sich von Hub, Prolog und Arena abhebt.

## ADDED Requirements

### Requirement: Jedes Biom hat Grafik-Slots für seine Räume
Das System SHALL für jedes Biom je eine Visual-ID für Raumboden, Raumwand mit Südtor, Ausgang und Grading führen. Jede dieser Visual-IDs SHALL eine Visual-Spec mit Status haben.

#### Scenario: Slots von Biom I
- **WHEN** die Visual-Specs im Testlauf geprüft werden
- **THEN** haben Raumboden, Raumwand, Ausgang und Grading von Biom I je eine gültige Visual-Spec

### Requirement: Fehlende Grafik erscheint als Graubox
Das System SHALL einen Raum, für dessen Slots die Registry keine Grafik liefert, als Graubox in der Platzhalterpalette des Bioms zeichnen. Boden, Wand, Südtor sowie offene und geschlossene Ausgänge SHALL ohne Text unterscheidbar sein. Das Fehlen MUST NOT als Fehler gemeldet werden.

#### Scenario: Raum ohne Grafik
- **WHEN** der Spieler einen Raum von Biom I betritt und die Registry keine Grafik für dessen Slots hat
- **THEN** sieht er Boden, Wand, Südtor und Ausgänge in der Platzhalterpalette, und keine gemalte Ebene der Arena

### Requirement: Die Platzhalterpalette hebt sich ab
Das System SHALL die Platzhalterpalette eines Bioms so wählen, dass sich Räume in Bodenhelligkeit und Grundton deutlich von Hub, Prolog und Arena unterscheiden. Death-Flame-Violett und Life-Flame-Orange MUST NOT als Grundfarbe der Palette dienen.

#### Scenario: Vergleich mit dem Hub
- **WHEN** eine Aufnahme eines Graubox-Raums neben eine Aufnahme des Hubs gelegt wird
- **THEN** ist der Graubox-Raum auf den ersten Blick als anderer Ort erkennbar

### Requirement: Gelieferte Grafik ersetzt die Graubox ohne Codeänderung
Das System SHALL für jeden Slot, für den die Registry Grafik liefert, diese Grafik statt der Graubox zeichnen. Dafür MUST NOT Code geändert werden; ein Bild und ein Registry-Eintrag SHALL genügen.

#### Scenario: Raumboden wird nachgeliefert
- **WHEN** Bild und Registry-Eintrag für `environment.biome1-room` hinzukommen und das Spiel neu startet
- **THEN** zeigen alle Räume von Biom I diesen Boden, während Wand und Ausgang weiter als Graubox erscheinen, solange sie keine Grafik haben

### Requirement: Ohne LUT ist das Grading neutral
Das System SHALL in einem Biom-Raum das Grading des Bioms verwenden. Liefert die Registry dafür keine LUT, SHALL das Grading neutral sein.

#### Scenario: Grading fehlt
- **WHEN** ein Raum von Biom I gezeichnet wird und `grade.biome1` keine Grafik hat
- **THEN** wird die Szene ohne Farbverschiebung gezeichnet
