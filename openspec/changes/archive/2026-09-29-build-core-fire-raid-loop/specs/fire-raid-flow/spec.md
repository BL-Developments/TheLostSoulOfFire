## ADDED Requirements

### Requirement: Eine Sitzung beginnt im Hub
Das System SHALL eine neue Spielsitzung im Start-Hub beginnen und SHALL dort eine steuerbare Spielfigur sowie einen erkennbaren Raid-Eingang bereitstellen.

#### Scenario: Spielstart
- **WHEN** die Anwendung erfolgreich gestartet wurde
- **THEN** befindet sich die Spielfigur im Hub und es ist keine Fire-Raid aktiv

### Requirement: Eine Fire-Raid kann im Hub gestartet werden
Das System SHALL eine neue Fire-Raid starten, wenn die Spielfigur sich im Interaktionsbereich des Raid-Eingangs befindet und die Interaktionsaktion auslöst.

#### Scenario: Raid am Eingang starten
- **WHEN** die Spielfigur im Interaktionsbereich des Raid-Eingangs steht und die Interaktionsaktion auslöst
- **THEN** erzeugt das System einen neuen Runzustand und wechselt in das erste Raid-Level

#### Scenario: Interaktion außerhalb des Eingangs
- **WHEN** die Spielfigur außerhalb des Interaktionsbereichs die Interaktionsaktion auslöst
- **THEN** bleibt die Spielfigur im Hub und es wird kein Runzustand erzeugt

### Requirement: Eine erfolgreiche Raid kehrt in den Hub zurück
Das System SHALL nach Benutzung des aktivierten Abschlussaltars die aktuelle Raid als erfolgreich beenden, ihren Runzustand verwerfen und in den Hub zurückkehren.

#### Scenario: Aktivierten Abschlussaltar benutzen
- **WHEN** alle Abschlussbedingungen erfüllt sind und die Spielfigur den aktivierten Abschlussaltar benutzt
- **THEN** endet die Raid erfolgreich und die Spielfigur erscheint wieder im Hub

### Requirement: Spielertod kehrt in den Hub zurück
Das System SHALL die aktive Raid beenden, wenn die Gesundheit der Spielfigur null erreicht, eine Niederlagenrückmeldung zeigen, den Runzustand verwerfen und anschließend in den Hub zurückkehren.

#### Scenario: Tod in der Raid
- **WHEN** die Gesundheit der Spielfigur während einer Raid null erreicht
- **THEN** endet die Raid nach einer sichtbaren Niederlagenrückmeldung und die Spielfigur erscheint mit einem frischen Zustand im Hub

### Requirement: Jede Raid startet frisch
Das System SHALL beim Start einer weiteren Fire-Raid eine neue Level- und Begegnungsinstanz ohne übrig gebliebene Gegner, Projektile oder Schadenszustände aus der vorherigen Raid erzeugen.

#### Scenario: Erneuter Start nach Tod
- **WHEN** die Spielfigur nach einer Niederlage im Hub eine weitere Fire-Raid startet
- **THEN** beginnt das Raid-Level in seinem definierten Ausgangszustand
