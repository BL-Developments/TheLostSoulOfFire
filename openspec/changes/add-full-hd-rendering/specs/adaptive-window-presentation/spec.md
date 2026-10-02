## MODIFIED Requirements

### Requirement: Das Spiel wird in fester virtueller Auflösung gezeichnet und seitenverhältnistreu eingepasst
Das System SHALL Spiellogik, Sichtfeld, HUD-Layout und Zeigerauswertung in einer festen logischen Auflösung von 1280×720 berechnen, SHALL Welt und HUD in einer Zeichenauflösung von 1920×1080 darstellen und SHALL das Ergebnis ohne Verzerrung größtmöglich mittig und weich gefiltert in das Fenster einpassen.

#### Scenario: Fenstergröße weicht vom Seitenverhältnis ab
- **WHEN** das Fenster ein anderes Seitenverhältnis als die logische Auflösung besitzt
- **THEN** bleibt das Bild unverzerrt, wird mittig eingepasst und die ungenutzten Randbereiche werden schwarz gefüllt

#### Scenario: Sichtfeld bei vergrößertem Fenster
- **WHEN** das Fenster vergrößert wird
- **THEN** bleibt der sichtbare Weltausschnitt identisch zu dem bei Startgröße und die Darstellung wird lediglich größer skaliert

#### Scenario: HUD bei veränderter Fenstergröße
- **WHEN** die Fenstergröße verändert wird
- **THEN** behalten HUD, Overlays und Menüeinträge ihre Anordnung und ihre relativen Größen innerhalb des Spielbilds

#### Scenario: Vollbild auf einem Full-HD-Bildschirm
- **WHEN** das Spiel im Vollbild auf einem 1920×1080-Bildschirm läuft
- **THEN** wird das Spielbild ohne Skalierung pixelgenau ausgegeben

#### Scenario: Verkleinerte Figuren bleiben ruhig
- **WHEN** hochaufgelöste Figuren- oder Effekt-Frames verkleinert gezeichnet werden
- **THEN** erscheinen sie weich gefiltert ohne Treppenkanten oder Flimmern bei Bewegung

#### Scenario: Pixelschrift bleibt gleichmäßig
- **WHEN** HUD- oder Menütext in der Pixelschrift gezeichnet wird
- **THEN** sind alle Glyphenpixel gleich groß und auf ganze Bildschirmpixel ausgerichtet

### Requirement: Screenshots werden in virtueller Auflösung ohne Randbereiche aufgenommen
Das System SHALL Bildschirmaufnahmen aus dem Spielbild in Zeichenauflösung (1920×1080) erzeugen und SHALL keine Letterbox-Randbereiche einschließen.

#### Scenario: Aufnahme bei abweichender Fenstergröße
- **WHEN** der Nutzer bei einer von der Zeichenauflösung abweichenden Fenstergröße eine Aufnahme auslöst
- **THEN** besitzt die erzeugte Bilddatei die Zeichenauflösung 1920×1080 und enthält ausschließlich Spielinhalt

#### Scenario: Aufnahme im Vollbildmodus
- **WHEN** der Nutzer im Vollbildmodus eine Aufnahme auslöst
- **THEN** ist die erzeugte Bilddatei von der Aufnahme im Fenstermodus nicht zu unterscheiden
