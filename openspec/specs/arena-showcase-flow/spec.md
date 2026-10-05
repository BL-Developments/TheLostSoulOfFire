# arena-showcase-flow Specification

## Purpose
Der Arena-Showcase führt vom Titelzustand durch definierte Wellen zu reproduzierbaren Endzuständen bei Tod oder Abschluss.
## Requirements
### Requirement: Der Showcase besitzt einen Titelzustand
Das System SHALL vor Kampfbeginn einen Titelzustand darstellen und SHALL erst nach bestätigender Eingabe in den aktiven Arenaablauf wechseln.

#### Scenario: Spiel wird gestartet
- **WHEN** die Anwendung den Showcase initialisiert
- **THEN** zeigt sie Titel und Startaufforderung, ohne bereits aktive Gegnerangriffe auszuführen

### Requirement: Arena fortschreitet durch definierte Wellen
Das System SHALL Gegner in unterscheidbaren Wellen einführen und SHALL die nächste Phase erst nach Erfüllung der aktuellen Abschlussbedingung starten. Nach dem Leeren jeder Welle außer der letzten SHALL das System eine Pause einlegen, in der der Spieler sich frei bewegen kann. Die nächste Welle SHALL erst starten, wenn der Spieler in der markierten Zone in der Arenamitte `E` drückt. Die erste Welle und das Finale nach der letzten Welle SHALL ohne Auslösen beginnen.

#### Scenario: Welle beginnt
- **WHEN** die vorherige Übergangsphase endet
- **THEN** kündigt das System die neue Welle an und erzeugt deren vorgesehene Gegnerzusammenstellung

#### Scenario: Welle wird geleert
- **WHEN** alle für eine der Wellen 1 bis 3 erforderlichen Gegner besiegt sind
- **THEN** spielt das System Abschlussfeedback, markiert die Zone in der Arenamitte und wartet ohne Zeitlimit

#### Scenario: Spieler startet die nächste Welle
- **WHEN** der Spieler während der Pause in der markierten Zone `E` drückt
- **THEN** zeigt das System die Aufforderung nicht mehr, kündigt die nächste Welle an und erzeugt sie nach der Übergangsphase

#### Scenario: Spieler steht außerhalb der Zone
- **WHEN** der Spieler während der Pause außerhalb der markierten Zone `E` drückt
- **THEN** startet keine neue Welle

#### Scenario: Letzte Welle wird geleert
- **WHEN** alle Gegner der vierten Welle besiegt sind
- **THEN** wechselt das System ohne Pause zum Finale

### Requirement: Tod und Abschluss führen zu reproduzierbaren Endzuständen
Das System SHALL Spieler-Tod und erfolgreichen Arenaabschluss getrennt inszenieren und SHALL einen Neustart mit zurückgesetztem Laufzeitzustand ermöglichen.

#### Scenario: Spieler stirbt
- **WHEN** die Spielergesundheit null erreicht
- **THEN** stoppt das System regulären Kampfinput, zeigt den Todeszustand und bietet einen vollständigen Neustart an

#### Scenario: Finale wird abgeschlossen
- **WHEN** die letzte Welle und der vorgesehene Abschlussmoment beendet sind
- **THEN** zeigt das System den erfolgreichen Endzustand und ermöglicht einen neuen Durchlauf

