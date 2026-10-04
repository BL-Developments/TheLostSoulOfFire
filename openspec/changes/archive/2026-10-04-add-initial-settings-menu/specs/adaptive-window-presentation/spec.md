## MODIFIED Requirements

### Requirement: Das Spiel lässt sich randlos im Vollbild darstellen
Das System SHALL über `F11` und den Menüpunkt `VOLLBILD` zwischen Fenster- und randlosem Vollbildmodus umschalten. Beide Eingabewege SHALL denselben angezeigten und gespeicherten Vollbildzustand ändern. Beim Verlassen des Vollbilds während einer Sitzung SHALL die zuvor verwendete Fenstergröße wiederhergestellt werden.

#### Scenario: Nutzer wechselt in das Vollbild
- **WHEN** der Nutzer im Fenstermodus `F11` drückt oder `VOLLBILD` im Menü einschaltet
- **THEN** füllt das Spiel den Bildschirm randlos und die Darstellung wird seitenverhältnistreu eingepasst

#### Scenario: Nutzer verlässt das Vollbild
- **WHEN** der Nutzer im Vollbildmodus `F11` drückt oder `VOLLBILD` im Menü ausschaltet
- **THEN** kehrt das Spiel in ein Fenster mit der zuvor in dieser Sitzung genutzten Größe und Darstellung zurück

#### Scenario: Escape behält seine bestehende Bedeutung
- **WHEN** der Nutzer `Escape` drückt
- **THEN** bleibt der Vollbildzustand unverändert

#### Scenario: Startzustand des Fensters
- **WHEN** das Spiel mit zuvor gespeichertem Vollbildzustand gestartet wird
- **THEN** öffnet es randlos im Vollbild und zeigt `VOLLBILD` im Menü als eingeschaltet an

#### Scenario: Kein gespeicherter Vollbildzustand ist vorhanden
- **WHEN** das Spiel ohne gültige gespeicherte Vollbildeinstellung gestartet wird
- **THEN** öffnet es wie bisher als Fenster in der Startgröße
