## RENAMED Requirements

- FROM: `### Requirement: Neues Spiel startet den Arenadurchlauf`
- TO: `### Requirement: Neues Spiel startet einen frischen Durchlauf`

## MODIFIED Requirements

### Requirement: Neues Spiel startet einen frischen Durchlauf
Das System SHALL bei Auswahl von `NEUES SPIEL` das Menü schließen und einen neuen Durchlauf mit zurückgesetztem Laufzeitzustand beginnen. Der Durchlauf SHALL mit dem Prolog beginnen.

#### Scenario: Neues Spiel wird gewählt
- **WHEN** der Spieler `NEUES SPIEL` auslöst
- **THEN** beendet das System die Menüdarstellung und startet den Prolog mit seiner Erwachensinszenierung

### Requirement: Automatisierte Läufe erreichen den Arenaablauf
Das System SHALL den automatisierten Prüfläufen einen Weg in den Arenaablauf bereitstellen, der nicht von einer Menünavigation abhängt und den Prolog überspringt.

#### Scenario: Automatisierter Prüflauf wird gestartet
- **WHEN** die Anwendung in einem automatisierten Prüfmodus startet
- **THEN** erreicht sie den Arenaablauf ohne manuelle Menüauswahl und ohne den Prolog zu durchlaufen, innerhalb der für den Prüflauf vorgesehenen Zeitgrenze
