## Purpose

Spieler können die erste Auswahl an Spiel-, Bild- und Tonoptionen im Titelmenü ändern und dieselben Werte nach einem Neustart weiterverwenden.

## ADDED Requirements

### Requirement: Optionale Spielhinweise sind einstellbar
Das System SHALL optionale Tutorial- und Steuerungshinweise ein- oder ausblenden können. Hinweise, die für eine notwendige Interaktion oder das Verstehen des aktuellen Ziels erforderlich sind, SHALL unabhängig von dieser Einstellung sichtbar bleiben.

#### Scenario: Optionale Hinweise werden ausgeschaltet
- **WHEN** der Spieler `OPTIONALE HINWEISE` ausschaltet und einen Tutorialabschnitt beginnt
- **THEN** verschwinden optionale Steuerungstipps, während notwendige Interaktionsaufforderungen sichtbar bleiben

### Requirement: Bildbewegung ist einstellbar
Das System SHALL für Kameraerschütterung und Kamera-Kick die Stufen `NORMAL`, `REDUZIERT` und `AUS` anbieten. Der Spielzustand, Treffer und die Steuerung SHALL durch die Auswahl unverändert bleiben.

#### Scenario: Bildbewegung wird ausgeschaltet
- **WHEN** der Spieler `BILDBEWEGUNG` auf `AUS` stellt und ein Treffer gewöhnlich Kameraerschütterung oder Kamera-Kick auslösen würde
- **THEN** bleibt die Kamera von diesen Bewegungen frei und der Treffer wirkt spielmechanisch unverändert

#### Scenario: Bildbewegung wird reduziert
- **WHEN** der Spieler `BILDBEWEGUNG` auf `REDUZIERT` stellt
- **THEN** sind Kameraerschütterung und Kamera-Kick schwächer als bei `NORMAL`

### Requirement: Tonlautstärken sind getrennt einstellbar
Das System SHALL Werte von 0 bis 100 Prozent für `GESAMTLAUTSTÄRKE`, `MUSIK` und `EFFEKTE` anbieten. Die Gesamtlautstärke SHALL Musik, Effekte und Ambience beeinflussen; `MUSIK` SHALL nur Musik und `EFFEKTE` SHALL Soundeffekte einschließlich Ambience beeinflussen. Bestehende zustandsabhängige Mischungen SHALL relativ zu diesen Werten erhalten bleiben.

#### Scenario: Musik wird stummgeschaltet
- **WHEN** der Spieler `MUSIK` auf 0 Prozent stellt
- **THEN** bleibt Musik stumm, während Effekte mit ihren eingestellten Lautstärken hörbar bleiben

#### Scenario: Gesamtlautstärke wird geändert
- **WHEN** der Spieler `GESAMTLAUTSTÄRKE` ändert
- **THEN** ändert sich die Lautstärke aller laufenden und später gestarteten Tonarten entsprechend

#### Scenario: Audio ist nicht verfügbar
- **WHEN** kein Audiogerät oder ein Audio-Asset verfügbar ist
- **THEN** bleiben Menü und Gameplay bedienbar und die gewählten Werte erhalten

### Requirement: Einstellungen bleiben nach einem Neustart erhalten
Das System SHALL Gameplay-, Grafik- und Audiowerte lokal speichern und bei einem späteren Start anwenden, einschließlich des Vollbildzustands. Fehlende, beschädigte oder außerhalb des zulässigen Bereichs liegende Werte SHALL durch Standardwerte ersetzt werden, ohne den Spielstart zu verhindern.

#### Scenario: Gespeicherte Werte werden geladen
- **WHEN** der Spieler Einstellungen ändert, das Spiel beendet und erneut startet
- **THEN** zeigen die Menüs dieselben Werte und das Spiel wendet sie vor der ersten Interaktion an

#### Scenario: Einstellungsdatei ist ungültig
- **WHEN** die lokale Einstellungsdatei nicht lesbar oder ungültig ist
- **THEN** startet das Spiel mit Standardwerten und die Einstellungen bleiben bedienbar

#### Scenario: Speichern ist nicht möglich
- **WHEN** der lokale Speicherort nicht beschreibbar ist
- **THEN** bleiben neue Werte für die laufende Sitzung wirksam und das Spiel läuft weiter
