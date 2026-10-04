## MODIFIED Requirements

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
