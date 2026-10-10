## MODIFIED Requirements

### Requirement: Charakterwerte lassen sich im Dev-Menü setzen
Das System SHALL im Abschnitt `CHARAKTER` des Dev-Menüs die Werteinträge `LEBEN`, `STÄRKE`, `FÄHIGKEITSSTÄRKE`, `RÜSTUNG`, `TEMPO`, `KERNSCHÄRFE`, `FOKUS`, `STANDFESTIGKEIT`, `GEWANDTHEIT`, `EINKLANG` und `GLÜCK` in dieser Reihenfolge mit ihrem aktuellen Wert anzeigen. Alle Charakterwerte außer `LEBEN` SHALL sich in Schritten von 1, mit Umschalttaste von 10, im Bereich 0 bis 99 ändern lassen. `LEBEN` SHALL das maximale Leben in Schritten von 10, mit Umschalttaste von 100, im Bereich 1 bis 999 ändern und das aktuelle Leben auf das neue Maximum setzen. Jede Änderung SHALL sofort wirken; HUD und Charaktermenü SHALL die neuen Werte zeigen. Alle Einträge des Dev-Menüs SHALL ohne Scrollen auf den Bildschirm passen.

#### Scenario: Stärke erhöhen
- **WHEN** der Spieler im Dev-Menü `STÄRKE` wählt, mit gedrückter Umschalttaste `D` drückt und das Menü schließt
- **THEN** ist die Stärke 20, das Charaktermenü zeigt `WAFFENSCHADEN +50 %` und die Sense trifft entsprechend härter

#### Scenario: Tempo erhöhen
- **WHEN** der Spieler im Dev-Menü `TEMPO` wählt, mit gedrückter Umschalttaste `D` drückt und das Menü schließt
- **THEN** ist das Tempo 20 und die Sense schlägt sichtbar schneller

#### Scenario: Leben erhöhen
- **WHEN** der Spieler im Dev-Menü bei `LEBEN` zweimal `D` drückt
- **THEN** zeigen Dev-Menü und HUD 120, und der Spieler hat 120 von 120 Leben

#### Scenario: Grenze
- **WHEN** die Rüstung 0 ist und der Spieler bei `RÜSTUNG` `A` drückt
- **THEN** bleibt die Rüstung 0

### Requirement: Zurücksetzen stellt die Startwerte wieder her
Das System SHALL mit der Aktion `ZURÜCKSETZEN` im Abschnitt `CHARAKTER` das maximale Leben auf 100 und den Spieler auf volles Leben setzen sowie alle übrigen Charakterwerte auf die Werte beim Sandbox-Start zurücksetzen (die Werte aus den Dev-Start-Parametern wie `--strength` oder `--luck` oder sonst 10). Ein Neustart in der Sandbox SHALL gesetzte Werte behalten. Beim Verlassen der Sandbox SHALL das System die Startwerte wiederherstellen.

#### Scenario: Zurücksetzen nach Änderungen
- **WHEN** die Sandbox mit `--armor 0` gestartet wurde, der Spieler Leben, Rüstung und Tempo ändert und `ZURÜCKSETZEN` ausführt
- **THEN** hat der Spieler 100 von 100 Leben, Rüstung 0 und Tempo 10

#### Scenario: Neustart behält Werte
- **WHEN** der Spieler Stärke 30 setzt, besiegt wird und `R` drückt
- **THEN** ist die Stärke weiterhin 30

#### Scenario: Sandbox verlassen
- **WHEN** der Spieler Leben 500 setzt und über das Pausenmenü zum Hauptmenü zurückkehrt und ein neues Spiel beginnt
- **THEN** hat der Spieler wieder 100 Leben
