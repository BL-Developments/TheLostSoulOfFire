## ADDED Requirements

### Requirement: Das Spielfenster ist frei skalierbar und maximierbar
Das System SHALL dem Nutzer erlauben, die Fenstergröße durch Ziehen der Fensterränder zu verändern, und SHALL das Maximieren des Fensters zulassen.

#### Scenario: Nutzer zieht den Fensterrand
- **WHEN** der Nutzer den Rand des Spielfensters zieht
- **THEN** folgt das Fenster der Zeigerbewegung und die Darstellung passt sich fortlaufend der neuen Größe an

#### Scenario: Nutzer maximiert das Fenster
- **WHEN** der Nutzer das Fenster maximiert
- **THEN** füllt das Fenster den Arbeitsbereich des Bildschirms und die Darstellung passt sich der neuen Größe an

#### Scenario: Fenster wird auf eine sehr kleine Größe gezogen
- **WHEN** der Nutzer das Fenster unter die zulässige Mindestgröße verkleinern will
- **THEN** begrenzt das System die Fenstergröße und die Darstellung bleibt sichtbar und unverzerrt

### Requirement: Das Spiel wird in fester virtueller Auflösung gezeichnet und seitenverhältnistreu eingepasst
Das System SHALL Welt und HUD unabhängig von der Fenstergröße in einer festen virtuellen Auflösung von 1280×720 zeichnen und SHALL das Ergebnis ohne Verzerrung größtmöglich mittig in das Fenster einpassen.

#### Scenario: Fenstergröße weicht vom Seitenverhältnis ab
- **WHEN** das Fenster ein anderes Seitenverhältnis als die virtuelle Auflösung besitzt
- **THEN** bleibt das Bild unverzerrt, wird mittig eingepasst und die ungenutzten Randbereiche werden schwarz gefüllt

#### Scenario: Sichtfeld bei vergrößertem Fenster
- **WHEN** das Fenster vergrößert wird
- **THEN** bleibt der sichtbare Weltausschnitt identisch zu dem bei Startgröße und die Darstellung wird lediglich größer skaliert

#### Scenario: HUD bei veränderter Fenstergröße
- **WHEN** die Fenstergröße verändert wird
- **THEN** behalten HUD, Overlays und Menüeinträge ihre Anordnung und ihre relativen Größen innerhalb des Spielbilds

### Requirement: Das Spiel lässt sich randlos im Vollbild darstellen
Das System SHALL über `F11` zwischen Fenster- und randlosem Vollbildmodus umschalten und SHALL beim Verlassen des Vollbilds die zuvor genutzte Fenstergröße derselben Sitzung wiederherstellen.

#### Scenario: Nutzer wechselt in das Vollbild
- **WHEN** der Nutzer im Fenstermodus `F11` drückt
- **THEN** füllt das Spiel den Bildschirm randlos und die Darstellung wird seitenverhältnistreu eingepasst

#### Scenario: Nutzer verlässt das Vollbild
- **WHEN** der Nutzer im Vollbildmodus `F11` drückt
- **THEN** kehrt das Spiel in ein Fenster mit der zuvor genutzten Größe und Darstellung zurück

#### Scenario: Escape behält seine bestehende Bedeutung
- **WHEN** der Nutzer `Escape` drückt
- **THEN** verhält sich das System unverändert zum bisherigen Verhalten und der Vollbildzustand wird dadurch nicht umgeschaltet

#### Scenario: Startzustand des Fensters
- **WHEN** das Spiel gestartet wird
- **THEN** öffnet es als Fenster in der Startgröße, unabhängig von Größe und Vollbildzustand vorangegangener Sitzungen

### Requirement: Zeigereingaben werden auf Spielkoordinaten abgebildet
Das System SHALL Zeigerpositionen aus Fensterkoordinaten in Koordinaten der virtuellen Auflösung umrechnen und SHALL alle spielseitigen Auswertungen von Zeigerpositionen auf dieser umgerechneten Position durchführen.

#### Scenario: Zielen bei skaliertem Fenster
- **WHEN** der Spieler bei vergrößertem oder verkleinertem Fenster auf eine Stelle der Spielwelt zeigt
- **THEN** entspricht das im Spiel getroffene Weltziel der Stelle, auf die der Zeiger sichtbar deutet

#### Scenario: Menübedienung bei skaliertem Fenster
- **WHEN** der Nutzer bei veränderter Fenstergröße einen Hauptmenüeintrag überfährt oder anklickt
- **THEN** reagiert genau der Eintrag, über dem der Zeiger sichtbar steht

#### Scenario: Zeiger liegt im Letterbox-Bereich
- **WHEN** der Zeiger sich außerhalb des eingepassten Spielbilds in einem schwarzen Randbereich befindet
- **THEN** löst er keine Auswahl eines Menüeintrags aus

### Requirement: Screenshots werden in virtueller Auflösung ohne Randbereiche aufgenommen
Das System SHALL Bildschirmaufnahmen aus dem Spielbild in virtueller Auflösung erzeugen und SHALL keine Letterbox-Randbereiche einschließen.

#### Scenario: Aufnahme bei abweichender Fenstergröße
- **WHEN** der Nutzer bei einer von der virtuellen Auflösung abweichenden Fenstergröße eine Aufnahme auslöst
- **THEN** besitzt die erzeugte Bilddatei die virtuelle Auflösung und enthält ausschließlich Spielinhalt

#### Scenario: Aufnahme im Vollbildmodus
- **WHEN** der Nutzer im Vollbildmodus eine Aufnahme auslöst
- **THEN** ist die erzeugte Bilddatei von der Aufnahme im Fenstermodus nicht zu unterscheiden
