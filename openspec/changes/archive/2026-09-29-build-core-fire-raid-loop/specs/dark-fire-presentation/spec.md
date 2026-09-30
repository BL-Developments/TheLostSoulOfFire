## ADDED Requirements

### Requirement: Düstere Feueridentität
Das System SHALL Hub und Raid mit einer dunklen Kohle-, Braun- und Rotpalette darstellen und SHALL aktives Feuer durch deutlich hellere Orange-, Gelb- und Gluttöne hervorheben.

#### Scenario: Hub wird dargestellt
- **WHEN** der Hub gezeichnet wird
- **THEN** ist der Raid-Eingang durch Feuer- oder Glutelemente klar von der dunklen Umgebung unterscheidbar

#### Scenario: Raid wird dargestellt
- **WHEN** das Raid-Level gezeichnet wird
- **THEN** vermitteln Architektur und Hintergrund eine dunkle Atmosphäre, ohne begehbare Flächen und Wände unkenntlich zu machen

### Requirement: Kampfrelevante Elemente bleiben lesbar
Das System SHALL Spieler, Gegner, Feuerprojektile, blockierende Geometrie und aktive Gefahren durch Silhouette, Helligkeit oder Farbkontrast eindeutig unterscheidbar halten.

#### Scenario: Mehrere Effekte überlappen im Kampf
- **WHEN** Spieler, Gegner und Feuerpartikel im selben Bildschirmbereich dargestellt werden
- **THEN** bleiben die Hitbox-relevanten Figuren und die Flugrichtung des Projektils visuell erkennbar

### Requirement: Feuer besitzt dynamisches Feedback
Das System SHALL kontinuierliche gepoolte Feuer- oder Glutpartikel an wichtigen Feuerquellen und kurze Partikelimpulse bei Schuss oder Treffer darstellen.

#### Scenario: Feuerangriff wird abgegeben
- **WHEN** die Spielfigur ein Feuerprojektil erzeugt
- **THEN** erscheint ein kurzer Feuerimpuls am Ursprung des Angriffs

#### Scenario: Feuerprojektil trifft
- **WHEN** ein Feuerprojektil einen Gegner trifft
- **THEN** erscheint ein kurzer Trefferimpuls am Kollisionspunkt

### Requirement: HUD ist von der Welt getrennt
Das System SHALL das HUD in Bildschirmkoordinaten über der Spielwelt darstellen, sodass Kamerabewegungen seine Position und Größe nicht verändern.

#### Scenario: Kamera folgt der Spielfigur
- **WHEN** sich die Weltkamera mit der Spielfigur bewegt
- **THEN** bleibt die Gesundheitsanzeige an ihrer festen Bildschirmposition

### Requirement: Darstellung skaliert proportional
Das System SHALL eine feste virtuelle Auflösung proportional auf das Anwendungsfenster skalieren und SHALL das Seitenverhältnis ohne Verzerrung erhalten.

#### Scenario: Fenster besitzt abweichendes Seitenverhältnis
- **WHEN** die Fenstermaße nicht dem virtuellen Seitenverhältnis entsprechen
- **THEN** wird die Spielfläche proportional skaliert und der ungenutzte Bereich durch Letterboxing gefüllt
