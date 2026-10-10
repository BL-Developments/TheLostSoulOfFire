# level-room-generation Specification

## Purpose
TBD - created by archiving change add-level-room-generation. Update Purpose after archive.
## Requirements
### Requirement: Eine Raumfolge besteht aus Stufen
Das System SHALL die Raumfolge eines Levels als Folge von Stufen erzeugen. Die erste Stufe SHALL genau einen Startraum, die letzte genau ein Levelende enthalten. Jede Stufe dazwischen SHALL einen oder zwei Kampfräume enthalten.

#### Scenario: Aufbau einer erzeugten Raumfolge
- **WHEN** eine Raumfolge mit beliebigem Seed erzeugt wird
- **THEN** beginnt sie mit einem Startraum, endet mit einem Levelende, und jede Stufe dazwischen hat einen oder zwei Kampfräume

### Requirement: Jeder Weg führt zum Levelende
Das System SHALL jeden Raum mit jedem Raum der nächsten Stufe verbinden. Kein Raum MUST mehr als zwei Ausgänge haben, und jeder Raum SHALL vom Startraum aus erreichbar sein und zum Levelende führen.

#### Scenario: Gabelung
- **WHEN** auf eine Stufe mit einem Raum eine Stufe mit zwei Räumen folgt
- **THEN** hat der Raum zwei Ausgänge, und beide Räume der nächsten Stufe führen weiter in Richtung Levelende

#### Scenario: Wege laufen wieder zusammen
- **WHEN** auf eine Stufe mit zwei Räumen eine Stufe mit einem Raum folgt
- **THEN** führen beide Räume in denselben Raum

### Requirement: Gleicher Seed ergibt dieselbe Raumfolge
Das System SHALL aus demselben Seed und denselben Balance-Werten immer dieselbe Raumfolge erzeugen. Die Erzeugung MUST NOT von Uhrzeit, Bildrate oder Spielereingaben abhängen.

#### Scenario: Zwei Erzeugungen mit demselben Seed
- **WHEN** zweimal mit Seed 4711 erzeugt wird
- **THEN** sind Stufen, Räume und Verbindungen beider Raumfolgen gleich

#### Scenario: Verschiedene Seeds
- **WHEN** mit 100 verschiedenen Seeds erzeugt wird
- **THEN** kommen sowohl Raumfolgen mit als auch ohne Gabelung vor

### Requirement: Länge und Gabelungen sind Balance-Werte
Das System SHALL die kleinste und größte Anzahl an Kampfstufen und die Wahrscheinlichkeit einer Stufe mit zwei Räumen als Balance-Werte an einer Stelle führen. Jede erzeugte Raumfolge SHALL innerhalb dieser Grenzen liegen.

#### Scenario: Grenzen werden eingehalten
- **WHEN** mit 1000 verschiedenen Seeds erzeugt wird
- **THEN** liegt die Anzahl der Kampfstufen jeder Raumfolge zwischen dem kleinsten und dem größten Balance-Wert

### Requirement: Jeder Raum kennt seinen Fortschritt
Das System SHALL jedem Raum seinen Fortschritt im Level als Stufennummer mitgeben. Parallele Räume derselben Stufe SHALL denselben Fortschritt haben.

#### Scenario: Parallele Räume
- **WHEN** eine Stufe zwei Räume enthält
- **THEN** haben beide Räume denselben Fortschritt, und er ist um eins höher als der der vorherigen Stufe

