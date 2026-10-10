# player-attributes Specification

## Purpose
TBD - created by archiving change add-player-attributes. Update Purpose after archive.
## Requirements
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

### Requirement: Der Spieler hat zehn Charakterwerte
Das System SHALL für den Spieler die Charakterwerte `Stärke`, `Fähigkeitsstärke`, `Rüstung`, `Tempo`, `Glück`, `Kernschärfe`, `Einklang`, `Gewandtheit`, `Fokus` und `Standfestigkeit` als ganze Zahlen von 0 bis 99 führen. Jeder Wert SHALL mit 10 beginnen. Ein Neustart nach dem Tod SHALL die Werte nicht zurücksetzen. Wo eine Regel einen Faktor nennt, SHALL er `1 + (Wert − 10) × 0,05` betragen; beim Startwert 10 SHALL jede Wirkung der neuen Werte das bisherige Verhalten unverändert lassen.

#### Scenario: Neuer Lauf
- **WHEN** ein neuer Lauf beginnt
- **THEN** hat der Spieler in allen zehn Charakterwerten 10

#### Scenario: Startwerte ändern nichts
- **WHEN** der Spieler mit allen Werten bei 10 einen Hollow besiegt, eine Seele erlöst und ausweicht
- **THEN** erhält er 3 Glut und 18 Resonance, und das Ausweichen hat 0,62 s Abklingzeit

### Requirement: Tempo beschleunigt Sense und Seelenkanone
Das System SHALL die Schwungzeit jedes Sensenschlags durch den Tempo-Faktor teilen, sodass Treffermoment und Ende des Schlags entsprechend früher kommen. Die Zeit bis zur vollen Ladung der Seelenkanone SHALL ebenfalls durch den Tempo-Faktor geteilt werden; Resonance SHALL zusätzlich wirken. Die Zeit, nach der eine unterbrochene Kombo von vorn beginnt, SHALL unverändert bleiben.

#### Scenario: Schnellere Kombo
- **WHEN** der Spieler mit Tempo 20 den ersten Kombo-Schlag ausführt
- **THEN** ist der Schlag nach rund 0,14 s statt 0,205 s beendet

#### Scenario: Schnellere Ladung
- **WHEN** der Spieler mit Tempo 20 die Seelenkanone lädt
- **THEN** ist sie nach 0,8 s statt 1,2 s voll geladen

### Requirement: Glück erhöht Glut und Geld
Das System SHALL die Glut, die ein besiegter Gegner gutschreibt, und das Geld aus einer Kiste mit dem Glücks-Faktor multiplizieren. Der ganzzahlige Teil SHALL gutgeschrieben werden, der Bruchteil SHALL mit seiner Wahrscheinlichkeit eine weitere Einheit ergeben. Der Glut-Basisvorrat eines Runs SHALL unverändert bleiben.

#### Scenario: Hohes Glück
- **WHEN** der Spieler mit Glück 30 einen Hollow besiegt und eine Kiste öffnet
- **THEN** erhält er 6 Glut und 50 Geld

#### Scenario: Bruchteil
- **WHEN** der Spieler mit Glück 14 einen Hollow besiegt
- **THEN** erhält er 3 Glut und mit 60 % Wahrscheinlichkeit eine weitere

### Requirement: Kernschärfe erhöht den Schaden von Kerntreffern
Das System SHALL den Schaden jedes Kerntreffers von Sense und Seelenkanone nach dem Kernbonus der Waffe mit dem Kernschärfe-Faktor multiplizieren, auf ganze Zahlen runden und mindestens 1 Schaden verursachen. Treffer ohne Kern SHALL unverändert bleiben.

#### Scenario: Kerntreffer mit hoher Kernschärfe
- **WHEN** der Spieler mit Kernschärfe 20 und aktivem Soul Sense den dritten Kombo-Schlag auf einen Kern setzt
- **THEN** verursacht der Schlag 87 statt 58 Schaden

### Requirement: Einklang erhöht den Resonance-Aufbau
Das System SHALL jeden Resonance-Zuwachs mit dem Einklang-Faktor multiplizieren, bevor die Anzeige auf ihr Maximum begrenzt wird.

#### Scenario: Seelenerlösung mit hohem Einklang
- **WHEN** der Spieler mit Einklang 20 eine Seele erlöst
- **THEN** steigt die Resonance um 27 statt 18

### Requirement: Gewandtheit verbessert Ausweichen und Laufen
Das System SHALL die Abklingzeit des Ausweichens durch den Gewandtheits-Faktor teilen; der Resonance-Multiplikator SHALL zusätzlich wirken. Die Laufgeschwindigkeit SHALL sich um 1 % pro Punkt über oder unter 10 ändern. Weite, Dauer und Unverwundbarkeit des Ausweichens SHALL unverändert bleiben.

#### Scenario: Hohe Gewandtheit
- **WHEN** der Spieler mit Gewandtheit 20 ausweicht und danach läuft
- **THEN** hat das Ausweichen rund 0,41 s Abklingzeit und der Spieler läuft 10 % schneller

### Requirement: Fokus verkürzt die Abklingzeiten der Run-Fähigkeiten
Das System SHALL die Abklingzeiten der Run-Fähigkeiten mit dem Fokus-Faktor schneller herunterzählen. Kosten und Wirkung der Fähigkeiten SHALL unverändert bleiben.

#### Scenario: Sog mit hohem Fokus
- **WHEN** der Spieler mit Fokus 20 `SOG` wirkt
- **THEN** ist die Fähigkeit nach rund 2,7 s statt 4 s wieder bereit

### Requirement: Standfestigkeit verringert erlittenen Rückstoß
Das System SHALL den Rückstoß jedes Treffers, den der Spieler erleidet, durch den Standfestigkeits-Faktor teilen. Schaden und Unverwundbarkeit nach dem Treffer SHALL unverändert bleiben.

#### Scenario: Hohe Standfestigkeit
- **WHEN** ein Hollow-Hieb den Spieler mit Standfestigkeit 20 trifft
- **THEN** wird der Spieler nur zwei Drittel so weit zurückgestoßen wie mit Standfestigkeit 10

