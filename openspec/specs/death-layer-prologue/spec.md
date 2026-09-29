# death-layer-prologue Specification

## Purpose
Der Prolog führt den Spieler als frisch erwachten Warden durch vier kurze Abschnitte in die Grundmechaniken ein, bevor die Aschenvorhalle und die Arena folgen. Er ist ein authored Handlungsstrang, kein allgemeines Quest- oder Cutscene-System.

## Requirements

### Requirement: Der Prolog liegt zwischen Menü und Aschenvorhalle
Das System SHALL nach `NEUES SPIEL` den Prolog beginnen und SHALL erst nach dessen Abschluss in die Aschenvorhalle wechseln. Die Arena SHALL weiterhin nur über die Aschenvorhalle erreichbar sein.

#### Scenario: Neues Spiel wird gestartet
- **WHEN** der Spieler `NEUES SPIEL` auslöst
- **THEN** beginnt der Prolog im ersten Abschnitt mit einer kurzen Erwachensinszenierung und der Spieler ist noch nicht steuerbar

#### Scenario: Prolog ist abgeschlossen
- **WHEN** der Spieler die Warden-Schwelle betreten hat und danach eine beliebige Eingabe auslöst
- **THEN** wechselt das System in die Aschenvorhalle

### Requirement: Der Prolog besteht aus vier Abschnitten in fester Reihenfolge
Das System SHALL den Prolog in die Abschnitte Ufer, Suchgang, Überfahrt und Schwelle gliedern und SHALL sie in dieser Reihenfolge durchlaufen. Jeder Abschnitt SHALL einen eigenen Abschnittstitel beim Betreten einblenden und eine eigene begehbare Fläche besitzen, die Spieler und Kamera begrenzt.

#### Scenario: Abschnitt wird betreten
- **WHEN** der Spieler einen neuen Abschnitt betritt
- **THEN** blendet das System den Abschnittstitel kurz ein und setzt den Spieler an den Startpunkt dieses Abschnitts

#### Scenario: Spieler bewegt sich im Abschnitt
- **WHEN** der Spieler sich bewegt
- **THEN** bleibt er innerhalb der begehbaren Fläche des aktuellen Abschnitts

### Requirement: Der Prolog führt die Grundmechaniken schrittweise ein
Das System SHALL im Prolog nacheinander Soul Sense (Spur finden), den Hollow (Ausweichen und Verankerung treffen), den Burning (Anlauf mit vollem Soul Cannon brechen), den Devourer mit gehaltener Seele (Seele befreien) und einen längeren Verteidigungsabschnitt auf dem Skiff einführen. Jeder Schritt SHALL mit einem sichtbaren Ziel angeleitet werden.

#### Scenario: Soul Sense wird eingeführt
- **WHEN** der Spieler die Spur im ersten Abschnitt mit aktivem Soul Sense erreicht
- **THEN** wird die Spur bestätigt und erst danach erscheint der erste Gegner

#### Scenario: Gegner werden nur gemeinsam mit ihrem Lehrmoment ausgelöst
- **WHEN** der Spieler ein Kampfgebiet betritt oder einen Auslösepunkt überschreitet
- **THEN** erscheinen die Gegner dieses Lehrmoments und der Weg zum nächsten Schritt öffnet sich erst, wenn Gegner und Seelen des Bereichs beseitigt sind

#### Scenario: Der Devourer trägt bereits eine Seele
- **WHEN** der Devourer-Abschnitt beginnt
- **THEN** hält der Devourer eine Seele, und der Abschnitt gilt erst als abgeschlossen, nachdem die Seele befreit wurde und Gegner sowie Seelen beseitigt sind

### Requirement: Die Überfahrt hält den Spieler 62 Sekunden auf dem Skiff
Das System SHALL nach dem Betreten des Skiffs den Spieler auf ein eng begrenztes Deck setzen und in Wellen Gegner erscheinen lassen. Die Überfahrt SHALL frühestens nach 62 Sekunden enden und erst, wenn keine Gegner oder Seelen mehr übrig sind.

#### Scenario: Skiff wird betreten
- **WHEN** der Spieler den Anlegepunkt des Skiffs erreicht
- **THEN** beginnt die Überfahrt mit begrenzter Bewegungsfläche und die erste Welle erscheint nach kurzer Verzögerung

#### Scenario: Zeit ist um, aber Gegner leben
- **WHEN** 62 Sekunden vergangen sind und noch Gegner oder Seelen existieren
- **THEN** bleibt die Überfahrt aktiv, bis alle beseitigt sind

### Requirement: Scheitern wiederholt den aktuellen Abschnitt
Das System SHALL bei Tod des Spielers eine Wiederholungsaufforderung anzeigen und SHALL auf `R` oder `F8` den aktuellen Abschnitt neu beginnen lassen, ohne bereits abgeschlossene Abschnitte zu wiederholen.

#### Scenario: Spieler stirbt im zweiten Abschnitt
- **WHEN** der Spieler im Suchgang stirbt und `R` drückt
- **THEN** beginnt der Suchgang von vorn mit vollem Leben und ohne Gegner des vorigen Versuchs

#### Scenario: Spieler stirbt im ersten Abschnitt
- **WHEN** der Spieler im ersten Abschnitt stirbt und `R` drückt
- **THEN** beginnt der Prolog von vorn

### Requirement: Die Kampfregeln gelten im Prolog wie in der Arena
Das System SHALL im Prolog dieselbe Bewegung, Sense, Soul Cannon, Ignition Dash, Soul Sense, Resonance und dieselben Gegnerverhalten wie in der Arena verwenden und SHALL das Kampf-HUD nur während aktiver Kampfphasen zeigen.

#### Scenario: Spieler kämpft im Prolog
- **WHEN** der Spieler im Prolog angreift oder Fähigkeiten einsetzt
- **THEN** wirken Schaden, Seelenfreisetzung und Rückmeldungen wie in der Arena

### Requirement: Ziele und Erzählzeilen erscheinen als Bildschirmtext
Das System SHALL für jeden Schritt ein kurzes Ziel und, wo vorgesehen, eine Erzählzeile am unteren Bildschirmrand darstellen.

#### Scenario: Schritt wechselt
- **WHEN** der Prolog in den nächsten Schritt wechselt
- **THEN** aktualisiert das System das angezeigte Ziel entsprechend

### Requirement: Der Prolog ist Einzelspieler und ohne Begleiter
Das System SHALL den Prolog ausschließlich mit einem Spieler und ohne Begleitfigur ausführen.

#### Scenario: Prolog läuft
- **WHEN** der Prolog läuft
- **THEN** existiert nur die vom Spieler gesteuerte Figur; Erzählzeilen nennen keinen Begleiter
