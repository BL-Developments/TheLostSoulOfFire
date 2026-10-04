# soul-furnace-antechamber Specification

## Purpose
Die Aschenvorhalle ist ein kampffreies, begehbares Pre-Level zwischen Prolog und Arena. Sie führt räumlich und atmosphärisch in den Abandoned Soul Furnace ein und endet mit einem inszenierten Toreintritt in das Arena-Intro.
## Requirements
### Requirement: Ein begehbares Pre-Level liegt vor der Arena
Das System SHALL nach dem Prolog die Aschenvorhalle laden und SHALL den Arenaablauf erst nach Verlassen dieses Pre-Levels beginnen. Automatisierte Prüfläufe, die den Prolog überspringen, SHALL direkt nach dem Titel in die Aschenvorhalle wechseln.

#### Scenario: Neuer Durchlauf wird gestartet
- **WHEN** der Spieler den Prolog abgeschlossen hat oder ein automatisierter Prüflauf den Prolog überspringt
- **THEN** erscheint der Spieler steuerbar am Spawnpunkt der Aschenvorhalle und die Arena-Wellen bleiben inaktiv

#### Scenario: Spieler erkundet die Vorhalle
- **WHEN** der Spieler sich vor dem Toreintritt in der Aschenvorhalle bewegt
- **THEN** begrenzt das System Spieler und Kamera auf den vorgesehenen begehbaren Bereich und erzeugt keine Gegner oder Kampfziele

### Requirement: Die Aschenvorhalle verwendet die Soulfire-Präsentation
Das System SHALL Pre-Level, Spieler und Atmosphäre über denselben pixelklaren World-, Licht-, Soul-Sense- und Vignette-Renderpfad wie die Arena darstellen.

#### Scenario: Pre-Level wird normal dargestellt
- **WHEN** Soul Sense nicht aktiv ist
- **THEN** zeigt das System eine dunkle gotisch-industrielle Vorhalle mit schwarzem Stein, Metall, Rohren, Ketten, erloschenen Öfen und zurückhaltender violetter Energie

#### Scenario: Spieler bewegt sich durch das Pre-Level
- **WHEN** der Spieler läuft oder das Ignition Dash verwendet
- **THEN** verwendet das System dieselben Spieler-Sprites, Bewegungsanimationen, Death-Flame-Lichter und passenden Bewegungseffekte wie im Arenaspiel

### Requirement: Das Pre-Level bleibt kampffrei
Das System SHALL im Pre-Level Bewegung, Blickrichtung, Ignition Dash und Soul Sense zulassen und SHALL aktive Sensen- und Cannon-Angriffe bis zum Arena-Eintritt unterdrücken.

#### Scenario: Spieler verwendet Erkundungsaktionen
- **WHEN** der Spieler läuft, zielt, das Ignition Dash verwendet oder `Q` hält
- **THEN** verarbeitet das System die jeweilige Bewegung beziehungsweise Wahrnehmungsdarstellung ohne eine Kampfbegegnung zu starten

#### Scenario: Spieler versucht anzugreifen
- **WHEN** der Spieler in der Aschenvorhalle die primäre oder sekundäre Angriffseingabe verwendet
- **THEN** erzeugt das System weder Sensenangriff noch Cannon-Ladung oder Projektil

### Requirement: Soul Sense enthüllt optionale Umgebungsinformationen
Das System SHALL bei aktivem Soul Sense Seelenspuren und eingeschlossene Energie in der Aschenvorhalle hervorheben und MUST NOT diese Wahrnehmung zum Erreichen oder Öffnen von Tür I voraussetzen.

#### Scenario: Soul Sense wird in der Vorhalle aktiviert
- **WHEN** der Spieler in der Aschenvorhalle `Q` hält
- **THEN** tritt die gewöhnliche Architektur visuell und akustisch zurück, während Seelenspuren zu Tür I und seelenbezogene Details hervortreten

#### Scenario: Spieler ignoriert Soul Sense
- **WHEN** der Spieler ohne Aktivierung von Soul Sense zu Tür I geht
- **THEN** bleibt der Weg vollständig erkennbar und der Arena-Eintritt verfügbar

### Requirement: Musikintensität steigt erst beim Arena-Eintritt
Das System SHALL die vorhandene räumliche Ambience in der Aschenvorhalle zurückgenommen wiedergeben und SHALL die eigentliche Arena-Musik erst während des Eintritts durch Tür I oder des Arena-Intros einblenden.

#### Scenario: Spieler verweilt in der Vorhalle
- **WHEN** der Spieler die Aschenvorhalle erkundet
- **THEN** bleibt die Audiomischung ruhig und lässt die Bedrohung hinter Tür I nur gedämpft anklingen

#### Scenario: Spieler betritt die Arena
- **WHEN** die Eintrittssequenz beginnt
- **THEN** überführt das System die ruhige Vorhallenmischung in die bestehende Arena-Ambience und Musik

### Requirement: Resetpfade respektieren den Einstiegskontext
Das System SHALL einen Retry nach Spielertod direkt in der Arena beginnen und SHALL einen vollständigen Neustart nach erfolgreichem Abschluss wieder über Titel, Prolog und Aschenvorhalle führen.

#### Scenario: Spieler startet nach Tod neu
- **WHEN** der Spieler im Todeszustand `R` drückt
- **THEN** setzt das System die Arena auf ihr Intro zurück, ohne die Aschenvorhalle erneut zu verlangen

#### Scenario: Spieler startet nach Abschluss neu
- **WHEN** der Spieler im erfolgreichen Abschlusszustand `R` drückt
- **THEN** leert das System den vollständigen Durchlaufzustand und kehrt zum Titel zurück, dessen Bestätigung erneut über den Prolog in die Aschenvorhalle führt

### Requirement: Der Eintritt durch Tür I führt nahtlos in das Arena-Intro
Das System SHALL den Eintritt durch Öffnen von Tür I, Kamera-Vorschub, Soulfire-Licht und Audioüberblendung darstellen und SHALL anschließend das bestehende Arena-Intro starten.

#### Scenario: Eintrittssequenz endet
- **WHEN** die kurze Eintrittssequenz an Tür I abgeschlossen ist
- **THEN** setzt das System den Spieler am Arena-Spawn zurück, wechselt in die Arena-Phase und beginnt deren Intro vor Welle eins

#### Scenario: Audio ist nicht verfügbar
- **WHEN** der Eintritt ohne verfügbares Audiogerät oder ohne passenden Cue erfolgt
- **THEN** beendet das System den visuellen Übergang weiterhin und startet die Arena ohne Abbruch

