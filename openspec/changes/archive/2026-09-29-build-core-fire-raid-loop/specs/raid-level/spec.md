## ADDED Requirements

### Requirement: Handgebautes Raid-Level
Das System SHALL ein deterministisch definiertes Raid-Level mit begehbaren Grenzen, blockierender Geometrie, einem Spieler-Spawn, mindestens einer erforderlichen Kampfbegegnung und einem Abschlussaltar laden.

#### Scenario: Raid-Level wird betreten
- **WHEN** eine neue Fire-Raid gestartet wird
- **THEN** erscheint die Spielfigur am definierten Spawnpunkt und alle definierten Gegner und Levelobjekte befinden sich in ihrem Ausgangszustand

### Requirement: Spielfigur bleibt innerhalb der Arena
Das System SHALL blockierende Levelgrenzen und Wände bei der Spieler- und Gegnerbewegung berücksichtigen.

#### Scenario: Spielfigur erreicht den Arenarand
- **WHEN** die Spielfigur versucht, sich über eine blockierende Levelgrenze hinauszubewegen
- **THEN** verbleibt ihre Hitbox innerhalb des begehbaren Bereichs

### Requirement: Abschlussaltar ist zunächst gesperrt
Das System SHALL den Abschlussaltar deaktiviert darstellen und SHALL einen erfolgreichen Abschluss verhindern, solange mindestens ein erforderlicher Gegner lebt.

#### Scenario: Altar vor Begegnungsende benutzen
- **WHEN** die Spielfigur den Abschlussaltar benutzt, während mindestens ein erforderlicher Gegner lebt
- **THEN** bleibt die Raid aktiv und der Altar signalisiert seinen gesperrten Zustand

### Requirement: Begegnung aktiviert den Abschluss
Das System SHALL den Abschlussaltar aktivieren, sobald alle für die Begegnung erforderlichen Gegner besiegt wurden.

#### Scenario: Letzter erforderlicher Gegner wird besiegt
- **WHEN** der letzte erforderliche Gegner aus der aktiven Begegnung entfernt wird
- **THEN** wechselt der Abschlussaltar sichtbar in seinen aktiven Zustand und kann zum Abschluss der Raid benutzt werden

### Requirement: Levelinhalt wird aus einer Definition erzeugt
Das System SHALL den für Hub und Raid benötigten Laufzeitinhalt reproduzierbar aus Leveldefinitionen erzeugen.

#### Scenario: Dieselbe Raid-Definition wird erneut geladen
- **WHEN** dieselbe Raid-Leveldefinition in einer neuen Raid geladen wird
- **THEN** stimmen Grenzen, Spawnpunkte, Wände, Gegneranzahl und Abschlussposition mit der Definition überein
