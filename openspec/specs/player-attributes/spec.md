# player-attributes Specification

## Purpose
TBD - created by archiving change add-player-attributes. Update Purpose after archive.
## Requirements
### Requirement: Der Spieler hat drei Charakterwerte
Das System SHALL für den Spieler die Charakterwerte `Stärke`, `Fähigkeitsstärke` und `Rüstung` als ganze Zahlen von 0 bis 99 führen. Jeder Wert SHALL mit 10 beginnen. Ein Neustart nach dem Tod SHALL die Werte nicht zurücksetzen.

#### Scenario: Neuer Lauf
- **WHEN** ein neuer Lauf beginnt
- **THEN** hat der Spieler Stärke 10, Fähigkeitsstärke 10 und Rüstung 10

### Requirement: Stärke skaliert den Waffenschaden
Das System SHALL den Grundschaden jedes Sensenschlags mit `1 + (Stärke − 10) × 0,05` multiplizieren, auf ganze Zahlen runden und mindestens 1 Schaden verursachen. Resonance und Soul-Sense-Kerntreffer SHALL auf diesen skalierten Schaden wirken.

#### Scenario: Startwert
- **WHEN** der Spieler mit Stärke 10 den dritten Kombo-Schlag ausführt
- **THEN** verursacht der Schlag 40 Schaden

#### Scenario: Hohe Stärke
- **WHEN** der Spieler mit Stärke 20 den dritten Kombo-Schlag ausführt
- **THEN** verursacht der Schlag 60 Schaden

### Requirement: Fähigkeitsstärke skaliert den Fähigkeitsschaden
Das System SHALL den Grundschaden der Seelenkanone mit `1 + (Fähigkeitsstärke − 10) × 0,05` multiplizieren, auf ganze Zahlen runden und mindestens 1 Schaden verursachen. Stärke SHALL den Kanonenschaden nicht verändern, Fähigkeitsstärke SHALL den Sensenschaden nicht verändern.

#### Scenario: Voll geladener Schuss mit erhöhter Fähigkeitsstärke
- **WHEN** der Spieler mit Fähigkeitsstärke 14 einen voll geladenen Kanonenschuss abgibt
- **THEN** verursacht das Projektil 82 statt 68 Grundschaden

### Requirement: Rüstung verringert erlittenen Schaden
Das System SHALL jeden Treffer, den der Spieler erleidet, um den Anteil `Rüstung / (Rüstung + 50)` verringern, auf ganze Zahlen runden und bei einem Treffer mit Schaden mindestens 1 Schaden abziehen. Rückstoß und Unverwundbarkeit nach dem Treffer SHALL unverändert bleiben.

#### Scenario: Startwert
- **WHEN** ein Hollow-Hieb mit 16 Schaden den Spieler mit Rüstung 10 trifft
- **THEN** verliert der Spieler 13 Leben

#### Scenario: Keine Rüstung
- **WHEN** ein Hollow-Hieb mit 16 Schaden den Spieler mit Rüstung 0 trifft
- **THEN** verliert der Spieler 16 Leben

