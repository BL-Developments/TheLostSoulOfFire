# main-menu

## Purpose

Das Hauptmenü ist der Einstieg nach der Titelkarte: ein navigierbares Menü mit Einzelspieler-Untermenü, Auswahl per Maus und Tastatur, dem Start eines neuen Spiels und dem Beenden der Anwendung.
## Requirements
### Requirement: Titelkarte führt in das Hauptmenü
Das System SHALL die Titelkarte weiterhin als ersten Zustand darstellen und SHALL bei bestätigender Eingabe in das Hauptmenü wechseln, anstatt unmittelbar den Arenaablauf zu beginnen.

#### Scenario: Anwendung wird gestartet
- **WHEN** die Anwendung den Showcase initialisiert
- **THEN** zeigt sie die Titelkarte mit Startaufforderung, ohne den Arenaablauf zu beginnen

#### Scenario: Startaufforderung wird bestätigt
- **WHEN** der Spieler auf der Titelkarte eine Taste drückt oder klickt
- **THEN** ersetzt das System die Startaufforderung durch das Hauptmenü und behält Titel, Zierlinien, Schleier, Letterbox und Kameraführung der Titelkarte bei

### Requirement: Menüeinträge sind per Maus und per Tastatur bedienbar
Das System SHALL die Auswahl sowohl über Mauszeiger als auch über Tastatur ermöglichen und SHALL dabei stets höchstens einen ausgewählten Eintrag führen. Das System SHALL die Auswahl nur dann anhand der Zeigerposition verändern, wenn sich der Zeiger tatsächlich bewegt hat.

#### Scenario: Zeiger bewegt sich über einen Eintrag
- **WHEN** der Spieler den Mauszeiger auf einen Menüeintrag bewegt
- **THEN** wird dieser Eintrag zum ausgewählten Eintrag

#### Scenario: Eintrag wird angeklickt
- **WHEN** der Spieler einen Menüeintrag anklickt
- **THEN** löst das System die Wirkung genau dieses Eintrags aus

#### Scenario: Auswahl wird per Tastatur verschoben
- **WHEN** der Spieler die Auswahl per Tastatur bewegt und der Mauszeiger dabei unbewegt über einem anderen Eintrag liegt
- **THEN** bleibt die per Tastatur getroffene Auswahl bestehen

#### Scenario: Auswahl wird per Tastatur bestätigt
- **WHEN** der Spieler die Auswahl per Tastatur bestätigt
- **THEN** löst das System die Wirkung des ausgewählten Eintrags aus

#### Scenario: Auswahl läuft über das Listenende hinaus
- **WHEN** der Spieler die Auswahl per Tastatur über den letzten Eintrag hinaus bewegt
- **THEN** führt das System die Auswahl auf den ersten Eintrag derselben Menüseite zurück

### Requirement: Einzelspieler öffnet ein Untermenü mit Rückweg
Das System SHALL bei Auswahl von `EINZELSPIELER` ein Untermenü mit den Einträgen `NEUES SPIEL`, `SPIEL LADEN` und `ZURÜCK` darstellen und SHALL über `ZURÜCK` zum Hauptmenü zurückkehren.

#### Scenario: Einzelspieler wird gewählt
- **WHEN** der Spieler `EINZELSPIELER` auslöst
- **THEN** zeigt das System das Einzelspieler-Untermenü mit `NEUES SPIEL`, `SPIEL LADEN` und `ZURÜCK`

#### Scenario: Rückweg wird gewählt
- **WHEN** der Spieler im Einzelspieler-Untermenü `ZURÜCK` auslöst
- **THEN** zeigt das System wieder das Hauptmenü

### Requirement: Neues Spiel startet einen frischen Durchlauf
Das System SHALL bei Auswahl von `NEUES SPIEL` das Menü schließen und einen neuen Durchlauf mit zurückgesetztem Laufzeitzustand beginnen. Der Durchlauf SHALL mit dem Prolog beginnen.

#### Scenario: Neues Spiel wird gewählt
- **WHEN** der Spieler `NEUES SPIEL` auslöst
- **THEN** beendet das System die Menüdarstellung und startet den Prolog mit seiner Erwachensinszenierung

### Requirement: Platzhaltereinträge bleiben wirkungslos
Das System SHALL `MEHRSPIELER`, `SPIEL LADEN`, `ERRUNGENSCHAFTEN UND STATISTIKEN`, `STEUERUNG` und `BARRIEREFREIHEIT` sichtbar und auswählbar darstellen. Eine Bestätigung dieser Platzhalter SHALL keine Unterseite öffnen, keine Einstellung ändern und kein Spiel starten.

#### Scenario: Platzhalter wird ausgelöst
- **WHEN** der Spieler einen der Platzhalter bestätigt oder anklickt
- **THEN** bleibt die aktuelle Menüseite unverändert sichtbar und es ändert sich kein Spiel- oder Einstellungswert

### Requirement: Beenden schließt die Anwendung
Das System SHALL die Anwendung über das Hauptmenü erst nach einer Bestätigung beenden. Bei Auswahl von `BEENDEN` sowie bei `Escape` auf der Titelkarte oder der Hauptmenüseite SHALL das System die Frage `SOLL DAS SPIEL WIRKLICH BEENDET WERDEN?` mit den Einträgen `JA` und `NEIN` darstellen. `Escape` SHALL in jeder anderen Menüseite, einschließlich Einzelspieler-Untermenü, Einstellungsseiten und Abfrage, eine Menüebene zurückführen.

#### Scenario: Beenden wird gewählt
- **WHEN** der Spieler im Hauptmenü `BEENDEN` auslöst
- **THEN** zeigt das System die Beenden-Abfrage mit `JA` und `NEIN`, ohne die Anwendung zu beenden

#### Scenario: Beenden wird bestätigt
- **WHEN** der Spieler in der Beenden-Abfrage `JA` auslöst
- **THEN** beendet das System die Anwendung

#### Scenario: Beenden wird abgelehnt
- **WHEN** der Spieler in der Beenden-Abfrage `NEIN` auslöst oder `Escape` drückt
- **THEN** zeigt das System wieder das Hauptmenü und die Anwendung läuft weiter

#### Scenario: Escape wird auf der Hauptmenüseite gedrückt
- **WHEN** der Spieler auf der Hauptmenüseite `Escape` drückt
- **THEN** zeigt das System die Beenden-Abfrage

#### Scenario: Escape wird auf der Titelkarte gedrückt
- **WHEN** der Spieler auf der Titelkarte mit Startaufforderung `Escape` drückt
- **THEN** öffnet das System das Hauptmenü mit der Beenden-Abfrage

#### Scenario: Escape wird im Untermenü gedrückt
- **WHEN** der Spieler im Einzelspieler-Untermenü `Escape` drückt
- **THEN** zeigt das System wieder das Hauptmenü, ohne die Anwendung zu beenden

#### Scenario: Escape wird in einer Einstellungsseite gedrückt
- **WHEN** der Spieler in `GAMEPLAY`, `GRAFIK` oder `AUDIO` `Escape` drückt
- **THEN** zeigt das System die Einstellungsübersicht, ohne die Anwendung zu beenden

### Requirement: Menütexte werden vollständig dargestellt
Das System SHALL alle Menübeschriftungen vollständig darstellen, einschließlich der im Deutschen erforderlichen Umlaute.

#### Scenario: Beschriftung enthält einen Umlaut
- **WHEN** ein Menüeintrag wie `ZURÜCK` dargestellt wird
- **THEN** erscheint jedes Zeichen der Beschriftung, ohne dass Zeichen stillschweigend entfallen

### Requirement: Automatisierte Läufe erreichen den Arenaablauf
Das System SHALL den automatisierten Prüfläufen einen Weg in den Arenaablauf bereitstellen, der nicht von einer Menünavigation abhängt und den Prolog überspringt.

#### Scenario: Automatisierter Prüflauf wird gestartet
- **WHEN** die Anwendung in einem automatisierten Prüfmodus startet
- **THEN** erreicht sie den Arenaablauf ohne manuelle Menüauswahl und ohne den Prolog zu durchlaufen, innerhalb der für den Prüflauf vorgesehenen Zeitgrenze

### Requirement: Hauptmenü bietet fünf Einträge in fester Reihenfolge
Das System SHALL im Hauptmenü die Einträge `EINZELSPIELER`, `MEHRSPIELER`, `EINSTELLUNGEN`, `ERRUNGENSCHAFTEN UND STATISTIKEN` und `BEENDEN` in dieser Reihenfolge darstellen und SHALL genau einen Eintrag als ausgewählt hervorheben.

#### Scenario: Hauptmenü wird geöffnet
- **WHEN** das Hauptmenü erscheint
- **THEN** zeigt das System die fünf Einträge in fester Reihenfolge und hebt `EINZELSPIELER` als ausgewählten Eintrag hervor

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

