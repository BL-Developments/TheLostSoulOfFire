## ADDED Requirements

### Requirement: Alle Gegnertypen lassen sich im Dev-Menü spawnen
Das System SHALL im Abschnitt `GEGNER` des Dev-Menüs für jeden spawnbaren Gegnertyp eine Aktion anzeigen, mindestens `HOLLOW`, `BURNING` und `DEVOURER`, jeweils mit der Zahl der lebenden Gegner dieses Typs. Die Aktion SHALL einen Gegner des Typs 260 Pixel vom Spieler entfernt, nie näher als 160 Pixel und vollständig innerhalb der Kampfgrenzen erscheinen lassen; aufeinanderfolgende Spawns SHALL sich um den Spieler verteilen. Das Dev-Menü SHALL dabei geöffnet bleiben. Die Einträge SHALL aus einer zentralen Liste der spawnbaren Typen entstehen.

#### Scenario: Mehrere Gegner spawnen
- **WHEN** der Spieler im Dev-Menü zweimal `HOLLOW` und einmal `DEVOURER` auslöst und das Menü schließt
- **THEN** stehen zwei Hollows und ein Devourer verteilt um den Spieler, die Zeilen zeigen 2 und 1, und die Gegner greifen nach dem Schließen an

#### Scenario: Spieler an der Wand
- **WHEN** der Spieler am Rand der Arena steht und `BURNING` auslöst
- **THEN** erscheint der Burning innerhalb der Arena und mindestens 160 Pixel vom Spieler entfernt

### Requirement: Alle Gegner lassen sich entfernen
Das System SHALL im Abschnitt `GEGNER` unter den Spawn-Aktionen die Aktion `ALLE GEGNER ENTFERNEN` mit der Zahl aller lebenden Gegner anzeigen. Sie SHALL alle Gegner und verlorenen Seelen vom Feld nehmen, ohne Seelen freizusetzen oder Todesanimationen abzuspielen.

#### Scenario: Feld räumen
- **WHEN** vier Gegner auf dem Feld stehen und der Spieler `ALLE GEGNER ENTFERNEN` auslöst
- **THEN** zeigt die Zeile 0 und nach dem Schließen ist das Feld leer
