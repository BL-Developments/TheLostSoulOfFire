# main-menu

## Purpose

TBD - synced from `add-main-menu` change specs. Update this Purpose statement to describe the capability in one or two sentences.

## Requirements

### Requirement: Titelkarte führt in das Hauptmenü
Das System SHALL die Titelkarte weiterhin als ersten Zustand darstellen und SHALL bei bestätigender Eingabe in das Hauptmenü wechseln, anstatt unmittelbar den Arenaablauf zu beginnen.

#### Scenario: Anwendung wird gestartet
- **WHEN** die Anwendung den Showcase initialisiert
- **THEN** zeigt sie die Titelkarte mit Startaufforderung, ohne den Arenaablauf zu beginnen

#### Scenario: Startaufforderung wird bestätigt
- **WHEN** der Spieler auf der Titelkarte eine Taste drückt oder klickt
- **THEN** ersetzt das System die Startaufforderung durch das Hauptmenü und behält Titel, Zierlinien, Schleier, Letterbox und Kameraführung der Titelkarte bei

### Requirement: Hauptmenü bietet vier Einträge in fester Reihenfolge
Das System SHALL im Hauptmenü die Einträge `EINZELSPIELER`, `MEHRSPIELER`, `EINSTELLUNGEN` und `BEENDEN` in dieser Reihenfolge darstellen und SHALL genau einen Eintrag als ausgewählt hervorheben.

#### Scenario: Hauptmenü wird geöffnet
- **WHEN** das Hauptmenü erscheint
- **THEN** zeigt das System die vier Einträge in fester Reihenfolge und hebt `EINZELSPIELER` als ausgewählten Eintrag hervor

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

### Requirement: Neues Spiel startet den Arenadurchlauf
Das System SHALL bei Auswahl von `NEUES SPIEL` das Menü schließen und den Arenadurchlauf mit zurückgesetztem Laufzeitzustand beginnen.

#### Scenario: Neues Spiel wird gewählt
- **WHEN** der Spieler `NEUES SPIEL` auslöst
- **THEN** beendet das System die Menüdarstellung und startet den Arenaablauf mit seiner regulären Eröffnungsinszenierung

### Requirement: Platzhaltereinträge bleiben wirkungslos
Das System SHALL die Einträge `MEHRSPIELER`, `EINSTELLUNGEN` und `SPIEL LADEN` auswählbar und auslösbar halten und SHALL bei ihrer Auslösung weder den Menüzustand noch den Spielzustand verändern.

#### Scenario: Platzhalter wird ausgelöst
- **WHEN** der Spieler `MEHRSPIELER`, `EINSTELLUNGEN` oder `SPIEL LADEN` auslöst
- **THEN** bleibt die aktuelle Menüseite unverändert sichtbar und es wird kein Spiel gestartet

### Requirement: Beenden schließt die Anwendung
Das System SHALL bei Auswahl von `BEENDEN` die Anwendung beenden. Das System SHALL die bestehende Wirkung der Escape-Taste unverändert beibehalten, sodass diese die Anwendung in jedem Zustand beendet.

#### Scenario: Beenden wird gewählt
- **WHEN** der Spieler `BEENDEN` auslöst
- **THEN** beendet das System die Anwendung

#### Scenario: Escape wird im Untermenü gedrückt
- **WHEN** der Spieler im Einzelspieler-Untermenü die Escape-Taste drückt
- **THEN** beendet das System die Anwendung, da Escape kein Rückweg innerhalb des Menüs ist

### Requirement: Menütexte werden vollständig dargestellt
Das System SHALL alle Menübeschriftungen vollständig darstellen, einschließlich der im Deutschen erforderlichen Umlaute.

#### Scenario: Beschriftung enthält einen Umlaut
- **WHEN** ein Menüeintrag wie `ZURÜCK` dargestellt wird
- **THEN** erscheint jedes Zeichen der Beschriftung, ohne dass Zeichen stillschweigend entfallen

### Requirement: Automatisierte Läufe erreichen den Arenaablauf
Das System SHALL den automatisierten Prüfläufen einen Weg in den Arenaablauf bereitstellen, der nicht von einer Menünavigation abhängt.

#### Scenario: Automatisierter Prüflauf wird gestartet
- **WHEN** die Anwendung in einem automatisierten Prüfmodus startet
- **THEN** erreicht sie den Arenaablauf ohne manuelle Menüauswahl und innerhalb der für den Prüflauf vorgesehenen Zeitgrenze
