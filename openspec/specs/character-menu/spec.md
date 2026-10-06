# character-menu Specification

## Purpose
TBD - created by archiving change add-character-menu. Update Purpose after archive.
## Requirements
### Requirement: Tab öffnet und schließt das Charaktermenü
Das System SHALL in jeder Spielphase außer der Titelphase bei `Tab` das Charaktermenü öffnen, sofern weder Pausenmenü noch Charaktermenü geöffnet sind. Bei geöffnetem Charaktermenü SHALL `Tab` oder `Escape` es schließen und das Spiel an derselben Stelle fortsetzen; die schließende Eingabe SHALL keine Spielaktion auslösen und SHALL NOT das Pausenmenü öffnen. Bei geöffnetem Pausenmenü SHALL `Tab` wirkungslos bleiben.

#### Scenario: Öffnen im Kampf
- **WHEN** der Spieler in der Arena `Tab` drückt
- **THEN** erscheint das Charaktermenü

#### Scenario: Schließen mit Tab
- **WHEN** der Spieler bei geöffnetem Charaktermenü `Tab` drückt
- **THEN** schließt sich das Menü und das Spiel läuft an derselben Stelle weiter

#### Scenario: Schließen mit Escape
- **WHEN** der Spieler bei geöffnetem Charaktermenü `Escape` drückt
- **THEN** schließt sich das Charaktermenü, das Spiel läuft weiter und das Pausenmenü erscheint nicht

#### Scenario: Titelphase
- **WHEN** der Spieler auf der Titelkarte oder im Hauptmenü `Tab` drückt
- **THEN** öffnet sich kein Charaktermenü

#### Scenario: Pausenmenü ist offen
- **WHEN** der Spieler bei geöffnetem Pausenmenü `Tab` drückt
- **THEN** bleibt das Pausenmenü unverändert und das Charaktermenü öffnet sich nicht

#### Scenario: Tab wird lange gehalten
- **WHEN** der Spieler `Tab` drückt und gedrückt hält
- **THEN** öffnet sich das Charaktermenü genau einmal und schließt sich nicht von selbst

### Requirement: Das Charaktermenü hält das Spiel an
Das System SHALL das Spiel anhalten, solange das Charaktermenü geöffnet ist, mit denselben Regeln wie beim Pausenmenü: Kein Spielzustand schreitet fort, Spiel- und Entwicklereingaben bleiben wirkungslos, Musik und Ambience laufen mit verringerter Lautstärke weiter und laufende Soundeffekte werden angehalten. Beim Schließen SHALL die vorherige Lautstärke wiederhergestellt und angehaltene Effekte fortgesetzt werden.

#### Scenario: Gegner stehen still
- **WHEN** das Charaktermenü während eines Kampfes geöffnet ist
- **THEN** bewegen sich Gegner, Geschosse und Spieler nicht weiter und der Spieler erleidet keinen Schaden

#### Scenario: Spieleingaben bei offenem Menü
- **WHEN** der Spieler bei geöffnetem Charaktermenü Angriffs-, Bewegungs-, Interaktions- oder Entwicklertasten betätigt
- **THEN** verändert sich der Spielzustand nicht

#### Scenario: Ton beim Öffnen und Schließen
- **WHEN** der Spieler das Charaktermenü öffnet und wieder schließt
- **THEN** läuft die Musik während des Menüs leiser, Effekte verstummen, und nach dem Schließen ist beides wie vorher

### Requirement: Reiterleiste mit Charakter, Map und Skills
Das System SHALL oben im Charaktermenü eine Reiterleiste mit `CHARAKTER`, `MAP` und `SKILLS` in dieser Reihenfolge darstellen und beim Öffnen `CHARAKTER` auswählen. `Rechts` oder `D` SHALL den nächsten, `Links` oder `A` den vorherigen Reiter wählen, ohne am Rand umzubrechen. Ein Mausklick auf einen Reiter SHALL ihn wählen. Der gewählte Reiter SHALL hervorgehoben sein und seine Seite unter der Leiste anzeigen.

#### Scenario: Menü wird geöffnet
- **WHEN** das Charaktermenü erscheint
- **THEN** zeigt die Leiste `CHARAKTER`, `MAP` und `SKILLS`, und `CHARAKTER` ist gewählt

#### Scenario: Reiter per Tastatur wechseln
- **WHEN** der Spieler bei gewähltem `CHARAKTER` dreimal `Rechts` drückt
- **THEN** ist nacheinander `MAP`, `SKILLS` und weiterhin `SKILLS` gewählt

#### Scenario: Reiter per Maus wählen
- **WHEN** der Spieler auf den Reiter `SKILLS` klickt
- **THEN** ist `SKILLS` gewählt und der Klick löst keine Spielaktion aus

#### Scenario: Erneutes Öffnen
- **WHEN** der Spieler das Menü auf `MAP` schließt und wieder öffnet
- **THEN** ist `CHARAKTER` gewählt

### Requirement: Die Charakterseite zeigt die Eigenschaften des Charakters
Das System SHALL auf der Seite `CHARAKTER` die aktuellen Werte des Spielers anzeigen: `LEBEN` als aktuelles und maximales Leben, `STÄRKE`, `FÄHIGKEITSSTÄRKE` und `RÜSTUNG` als ganze Zahlen. Zu jedem dieser drei Werte SHALL eine Wirkungszeile stehen: `WAFFENSCHADEN` und `FÄHIGKEITSSCHADEN` als Abweichung vom Grundschaden in ganzen Prozent mit Vorzeichen, `SCHADENSVERRINGERUNG` als Anteil, den Rüstung von erlittenem Schaden abzieht, in ganzen Prozent ohne Vorzeichen. Die Prozentwerte SHALL aus denselben Regeln berechnet werden, mit denen `player-attributes` den Schaden bestimmt.

#### Scenario: Startwerte
- **WHEN** ein neuer Lauf beginnt und der Spieler das Charaktermenü öffnet
- **THEN** zeigt die Seite `LEBEN 100 / 100`, `STÄRKE 10` mit `WAFFENSCHADEN +0 %`, `FÄHIGKEITSSTÄRKE 10` mit `FÄHIGKEITSSCHADEN +0 %` und `RÜSTUNG 10` mit `SCHADENSVERRINGERUNG 17 %`

#### Scenario: Gesetzte Werte im Developer-Mode
- **WHEN** das Spiel mit `--dev --start arena --strength 20 --ability-power 6 --armor 0` gestartet wird und der Spieler das Charaktermenü öffnet
- **THEN** zeigt die Seite `STÄRKE 20` mit `WAFFENSCHADEN +50 %`, `FÄHIGKEITSSTÄRKE 6` mit `FÄHIGKEITSSCHADEN -20 %` und `RÜSTUNG 0` mit `SCHADENSVERRINGERUNG 0 %`

#### Scenario: Leben nach einem Treffer
- **WHEN** der Spieler mit Rüstung 10 einen Hollow-Hieb erlitten hat und das Charaktermenü öffnet
- **THEN** zeigt die Seite `LEBEN 87 / 100`

### Requirement: Die Charakterseite zeigt die Währungen
Das System SHALL auf der Seite `CHARAKTER` unter den Charakterwerten einen Abschnitt `WÄHRUNGEN` mit je einer Zeile für `GELD` und `GLUT` anzeigen. In der Arena SHALL jede Zeile den Run-Bestand und den gesicherten Bestand nennen (`IM LAUF <n>` und `GESICHERT <n>`); außerhalb der Arena SHALL sie nur den gesicherten Bestand nennen. Die Beträge SHALL dieselben sein, die `run-currencies` führt.

#### Scenario: Währungen im Run
- **WHEN** der Spieler in der Arena 25 Geld und 19 Glut im Run hat, 50 Geld und 30 Glut gesichert sind und er das Charaktermenü öffnet
- **THEN** zeigt die Seite `GELD` mit `IM LAUF 25` und `GESICHERT 50` sowie `GLUT` mit `IM LAUF 19` und `GESICHERT 30`

#### Scenario: Währungen im Hub
- **WHEN** der Spieler im Hub mit 50 gesichertem Geld und 30 gesicherter Glut das Charaktermenü öffnet
- **THEN** zeigt die Seite `GELD` mit `GESICHERT 50` und `GLUT` mit `GESICHERT 30` und keinen Run-Bestand

### Requirement: Map und Skills sind Platzhalter
Das System SHALL die Reiter `MAP` und `SKILLS` wählbar machen, sie in der gedämpften Farbe der Platzhaltereinträge des Hauptmenüs darstellen und auf ihrer Seite nur den Hinweis `NOCH NICHT VERFÜGBAR` zeigen.

#### Scenario: Map wird gewählt
- **WHEN** der Spieler den Reiter `MAP` wählt
- **THEN** zeigt die Seite `NOCH NICHT VERFÜGBAR` und das Spiel bleibt angehalten

### Requirement: Charaktermenü im Stil des Pausenmenüs
Das System SHALL das angehaltene Spielbild unter demselben halbtransparenten dunklen Schleier wie beim Pausenmenü sichtbar lassen und darüber Letterbox, Zierlinie, Reiterleiste und Seiteninhalt in Schrift, Farben und Auswahlmarkierung des Hauptmenüs darstellen. Story-, Hinweis- und Interaktionstexte der Spielphase SHALL ausgeblendet sein, solange das Menü offen ist.

#### Scenario: Menübild wird dargestellt
- **WHEN** das Charaktermenü geöffnet ist
- **THEN** ist das angehaltene Spielbild gedämpft erkennbar und darüber stehen Reiterleiste und Charakterwerte im Stil des Hauptmenüs

