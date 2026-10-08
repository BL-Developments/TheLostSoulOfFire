## Purpose

Die visuellen Qualitätsprüfungen fangen technische Grafikfehler automatisch ab und erzeugen eine feste Bildreihe von Ufer und Arena, an der Mensch und Agent die Qualität beurteilen können, ohne selbst spielen zu müssen.

## ADDED Requirements

### Requirement: Der Testlauf prüft jede Registry-Grafik
Der Testlauf SHALL für jeden Eintrag der Registry prüfen: Die Quelldatei existiert; die Bildmaße passen ohne Rest zu Framegröße und Frameanzahl; eine Figur mit Richtungen hat alle acht Richtungen für jede Animation; Normal-Maps haben dieselben Maße wie ihre Farbbilder; transparente Grafik hat einen transparenten Rand und kein eingebackenes Schachbrett- oder Hintergrundmuster. Jeder Verstoß SHALL den Testlauf mit Visual-ID, Datei und Grund fehlschlagen lassen.

#### Scenario: Eingebackenes Schachbrettmuster
- **WHEN** ein Effekt-Sheet statt Transparenz ein graues Schachbrettmuster enthält
- **THEN** schlägt der Testlauf fehl und nennt Visual-ID, Datei und „Hintergrundmuster“

#### Scenario: Fehlende Richtung
- **WHEN** für `player` die Animation `run` in Richtung `sw` fehlt
- **THEN** schlägt der Testlauf fehl und nennt `player/run/sw`

#### Scenario: Raster passt nicht
- **WHEN** ein Sheet mit 9 Frames zu je 256 Pixeln 700 Pixel breit ist
- **THEN** schlägt der Testlauf fehl und nennt die erwarteten und tatsächlichen Maße

### Requirement: Death-Flame-Grafik hält das Farbbudget
Der Testlauf SHALL für jede Grafik, deren Registry-Eintrag die Palette `death-flame` trägt, fehlschlagen, wenn mehr als ein kleiner, in den Tests festgelegter Anteil ihrer sichtbaren Pixel im Orange-, Grün- oder Blaubereich liegt. Orange SHALL der Palette `life-flame` vorbehalten bleiben.

#### Scenario: Orangefarbener Sensenhieb
- **WHEN** ein Sensenhieb mit Palette `death-flame` deutlich orange Flammen enthält
- **THEN** schlägt der Testlauf fehl und nennt Visual-ID und gemessenen Farbanteil

#### Scenario: Life Flame
- **WHEN** die Life Flame mit Palette `life-flame` orange ist
- **THEN** besteht sie die Prüfung

### Requirement: Die Scheibe nimmt eine feste Bildreihe auf
Das System SHALL beim Start mit `--slice-visual-test` ohne Eingaben nacheinander den Prolog-Abschnitt Ufer und die Arena bei Welle 1 betreten, eine feste, benannte Reihe von Aufnahmen erstellen und sich danach mit Exitcode 0 beenden. Die Reihe SHALL mindestens enthalten: Überblick des Ufers ohne HUD, Spieler mit Soul Sense an der Spur, den ersten Hollow im Ufer, Überblick der Arena ohne HUD, Spieler in Ruhe in allen acht Richtungen, Laufen, jeden der drei Sensenhiebe am Treffermoment, Dash-Zündung, Core-Treffer, Hollow-Swipe im Telegraph, Hollow-Auflösung, Soul Release, Soul Sense und den Spieler hinter einem Vordergrund-Occluder. Jede Aufnahme SHALL ihren Namen im Dateinamen tragen. Kann eine Aufnahme nicht erstellt werden, SHALL das System mit einem Exitcode ungleich 0 und dem Namen der Aufnahme enden.

#### Scenario: Vollständige Aufnahmereihe
- **WHEN** das Spiel mit `--slice-visual-test` gestartet wird
- **THEN** liegen danach unter `artifacts/screenshots/` alle benannten Aufnahmen der Reihe und das Spiel hat sich mit Exitcode 0 beendet

#### Scenario: Aufnahme scheitert
- **WHEN** während `--slice-visual-test` kein Hollow für die Auflösung erscheint
- **THEN** endet das Spiel mit einem Exitcode ungleich 0 und nennt die fehlende Aufnahme
