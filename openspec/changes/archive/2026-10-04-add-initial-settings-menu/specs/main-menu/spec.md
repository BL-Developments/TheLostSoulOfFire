## RENAMED Requirements

- FROM: `### Requirement: Hauptmenü bietet vier Einträge in fester Reihenfolge`
- TO: `### Requirement: Hauptmenü bietet fünf Einträge in fester Reihenfolge`

## MODIFIED Requirements

### Requirement: Hauptmenü bietet fünf Einträge in fester Reihenfolge
Das System SHALL im Hauptmenü die Einträge `EINZELSPIELER`, `MEHRSPIELER`, `EINSTELLUNGEN`, `ERRUNGENSCHAFTEN UND STATISTIKEN` und `BEENDEN` in dieser Reihenfolge darstellen und SHALL genau einen Eintrag als ausgewählt hervorheben.

#### Scenario: Hauptmenü wird geöffnet
- **WHEN** das Hauptmenü erscheint
- **THEN** zeigt das System die fünf Einträge in fester Reihenfolge und hebt `EINZELSPIELER` als ausgewählten Eintrag hervor

### Requirement: Platzhaltereinträge bleiben wirkungslos
Das System SHALL `MEHRSPIELER`, `SPIEL LADEN`, `ERRUNGENSCHAFTEN UND STATISTIKEN`, `STEUERUNG` und `BARRIEREFREIHEIT` sichtbar und auswählbar darstellen. Eine Bestätigung dieser Platzhalter SHALL keine Unterseite öffnen, keine Einstellung ändern und kein Spiel starten.

#### Scenario: Platzhalter wird ausgelöst
- **WHEN** der Spieler einen der Platzhalter bestätigt oder anklickt
- **THEN** bleibt die aktuelle Menüseite unverändert sichtbar und es ändert sich kein Spiel- oder Einstellungswert

### Requirement: Beenden schließt die Anwendung
Das System SHALL bei Auswahl von `BEENDEN` die Anwendung beenden. `Escape` SHALL innerhalb der Einstellungsseiten zur übergeordneten Menüseite führen; außerhalb der Einstellungsseiten SHALL es die Anwendung wie bisher beenden.

#### Scenario: Beenden wird gewählt
- **WHEN** der Spieler `BEENDEN` auslöst
- **THEN** beendet das System die Anwendung

#### Scenario: Escape wird im Untermenü gedrückt
- **WHEN** der Spieler im Einzelspieler-Untermenü `Escape` drückt
- **THEN** beendet das System die Anwendung wie bisher

#### Scenario: Escape wird in einer Einstellungsseite gedrückt
- **WHEN** der Spieler in `GAMEPLAY`, `GRAFIK` oder `AUDIO` `Escape` drückt
- **THEN** zeigt das System die Einstellungsübersicht, ohne die Anwendung zu beenden

#### Scenario: Escape wird in der Einstellungsübersicht gedrückt
- **WHEN** der Spieler in der Einstellungsübersicht `Escape` drückt
- **THEN** zeigt das System das Hauptmenü, ohne die Anwendung zu beenden

## ADDED Requirements

### Requirement: Einstellungen haben drei bedienbare Bereiche und zwei Platzhalter
Das System SHALL bei Auswahl von `EINSTELLUNGEN` die Einträge `GAMEPLAY`, `GRAFIK`, `AUDIO`, `STEUERUNG`, `BARRIEREFREIHEIT` und `ZURÜCK` darstellen. Die ersten drei Einträge SHALL eigene Seiten mit den zugehörigen Werten öffnen; `ZURÜCK` SHALL jeweils eine Menüebene zurückführen.

#### Scenario: Einstellungsübersicht wird geöffnet
- **WHEN** der Spieler `EINSTELLUNGEN` bestätigt
- **THEN** sieht er die drei bedienbaren Bereiche, die zwei Platzhalter und `ZURÜCK`

#### Scenario: Einstellungsbereich wird geöffnet
- **WHEN** der Spieler `GRAFIK` bestätigt
- **THEN** sieht er `VOLLBILD`, `BILDBEWEGUNG` und `ZURÜCK` mit den aktuell wirksamen Werten

#### Scenario: Rückweg wird gewählt
- **WHEN** der Spieler `ZURÜCK` auf einer Einstellungsseite bestätigt
- **THEN** erscheint die unmittelbar übergeordnete Menüseite

### Requirement: Einstellungswerte sind mit Maus und Tastatur änderbar
Das System SHALL den aktuell ausgewählten Wert sichtbar darstellen und Änderungen über Tastatur sowie Maus zulassen. Menübewegung und Wertänderung SHALL getrennte Eingaben verwenden; eine Wertänderung SHALL keine andere Menüzeile auswählen.

#### Scenario: Audiowert wird per Tastatur geändert
- **WHEN** der Spieler einen Audiowert auswählt und eine Links- oder Rechts-Eingabe auslöst
- **THEN** ändert sich nur dieser Wert innerhalb seines zulässigen Bereichs und die neue Prozentzahl ist sichtbar

#### Scenario: Option wird per Maus geändert
- **WHEN** der Spieler auf die angezeigte Option oder einen ihrer Werte klickt
- **THEN** ändert sich die angeklickte Option und die neue Auswahl ist sichtbar
