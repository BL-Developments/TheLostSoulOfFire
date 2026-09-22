## ADDED Requirements

### Requirement: Der Showcase besitzt einen Titelzustand
Das System SHALL vor Kampfbeginn einen Titelzustand darstellen und SHALL erst nach bestätigender Eingabe in den aktiven Arenaablauf wechseln.

#### Scenario: Spiel wird gestartet
- **WHEN** die Anwendung den Showcase initialisiert
- **THEN** zeigt sie Titel und Startaufforderung, ohne bereits aktive Gegnerangriffe auszuführen

### Requirement: Arena fortschreitet durch definierte Wellen
Das System SHALL Gegner in unterscheidbaren Wellen einführen und SHALL die nächste Phase erst nach Erfüllung der aktuellen Abschlussbedingung starten.

#### Scenario: Welle beginnt
- **WHEN** die vorherige Übergangsphase endet
- **THEN** kündigt das System die neue Welle an und erzeugt deren vorgesehene Gegnerzusammenstellung

#### Scenario: Welle wird geleert
- **WHEN** alle für die aktuelle Welle erforderlichen Gegner besiegt sind
- **THEN** spielt das System Abschlussfeedback und wechselt nach der vorgesehenen Pause zur nächsten Welle oder zum Finale

### Requirement: Tod und Abschluss führen zu reproduzierbaren Endzuständen
Das System SHALL Spieler-Tod und erfolgreichen Arenaabschluss getrennt inszenieren und SHALL einen Neustart mit zurückgesetztem Laufzeitzustand ermöglichen.

#### Scenario: Spieler stirbt
- **WHEN** die Spielergesundheit null erreicht
- **THEN** stoppt das System regulären Kampfinput, zeigt den Todeszustand und bietet einen vollständigen Neustart an

#### Scenario: Finale wird abgeschlossen
- **WHEN** die letzte Welle und der vorgesehene Abschlussmoment beendet sind
- **THEN** zeigt das System den erfolgreichen Endzustand und ermöglicht einen neuen Durchlauf
