## ADDED Requirements

### Requirement: Ein begehbares Pre-Level liegt vor der Arena
Das System SHALL nach Bestätigung des Titelbildschirms die Aschenvorhalle laden und SHALL den Arenaablauf erst nach Verlassen dieses Pre-Levels beginnen.

#### Scenario: Neuer Durchlauf wird gestartet
- **WHEN** der Spieler den Titelbildschirm bestätigt
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
Das System SHALL bei aktivem Soul Sense Seelenspuren und eingeschlossene Energie in der Aschenvorhalle hervorheben und MUST NOT diese Wahrnehmung zum Erreichen oder Öffnen des Tores voraussetzen.

#### Scenario: Soul Sense wird in der Vorhalle aktiviert
- **WHEN** der Spieler in der Aschenvorhalle `Q` hält
- **THEN** tritt die gewöhnliche Architektur visuell und akustisch zurück, während Seelenspuren zum Tor und seelenbezogene Details hervortreten

#### Scenario: Spieler ignoriert Soul Sense
- **WHEN** der Spieler ohne Aktivierung von Soul Sense zum Tor geht
- **THEN** bleibt der Weg vollständig erkennbar und der Arena-Eintritt verfügbar

### Requirement: Ein massives Tor kontrolliert den Arena-Eintritt
Das System SHALL das Ziel der Aschenvorhalle als klar lesbares Ofen-/Kathedralentor darstellen und SHALL die Aktivierung nur in dessen Interaktionszone erlauben.

#### Scenario: Spieler nähert sich dem Tor
- **WHEN** der Spieler die Interaktionszone des Tores betritt
- **THEN** zeigt das System eine zurückhaltende Aufforderung zur Interaktion mit `E`

#### Scenario: Spieler drückt E außerhalb der Torzone
- **WHEN** der Spieler `E` außerhalb der Interaktionszone drückt
- **THEN** verbleibt das System in der Aschenvorhalle und startet keinen Übergang

#### Scenario: Spieler aktiviert das Tor
- **WHEN** der Spieler innerhalb der Interaktionszone `E` drückt
- **THEN** sperrt das System weitere Gameplay-Eingaben und startet die inszenierte Eintrittssequenz

### Requirement: Der Toreintritt führt nahtlos in das Arena-Intro
Das System SHALL den Eintritt durch Torbewegung, Kamera-Vorschub, Soulfire-Licht und Audioüberblendung darstellen und SHALL anschließend das bestehende Arena-Intro starten.

#### Scenario: Eintrittssequenz endet
- **WHEN** die kurze Toreintrittssequenz abgeschlossen ist
- **THEN** setzt das System den Spieler am Arena-Spawn zurück, wechselt in die Arena-Phase und beginnt deren Intro vor Welle eins

#### Scenario: Audio ist nicht verfügbar
- **WHEN** der Toreintritt ohne verfügbares Audiogerät oder ohne passenden Cue erfolgt
- **THEN** beendet das System den visuellen Übergang weiterhin und startet die Arena ohne Abbruch

### Requirement: Musikintensität steigt erst beim Arena-Eintritt
Das System SHALL die vorhandene räumliche Ambience in der Aschenvorhalle zurückgenommen wiedergeben und SHALL die eigentliche Arena-Musik erst während des Toreintritts oder Arena-Intros einblenden.

#### Scenario: Spieler verweilt in der Vorhalle
- **WHEN** der Spieler die Aschenvorhalle erkundet
- **THEN** bleibt die Audiomischung ruhig und lässt die Bedrohung hinter dem Tor nur gedämpft anklingen

#### Scenario: Spieler betritt die Arena
- **WHEN** die Eintrittssequenz beginnt
- **THEN** überführt das System die ruhige Vorhallenmischung in die bestehende Arena-Ambience und Musik

### Requirement: Resetpfade respektieren den Einstiegskontext
Das System SHALL einen Retry nach Spielertod direkt in der Arena beginnen und SHALL einen vollständigen Neustart nach erfolgreichem Abschluss wieder über Titel und Aschenvorhalle führen.

#### Scenario: Spieler startet nach Tod neu
- **WHEN** der Spieler im Todeszustand `R` drückt
- **THEN** setzt das System die Arena auf ihr Intro zurück, ohne die Aschenvorhalle erneut zu verlangen

#### Scenario: Spieler startet nach Abschluss neu
- **WHEN** der Spieler im erfolgreichen Abschlusszustand `R` drückt
- **THEN** leert das System den vollständigen Durchlaufzustand und kehrt zum Titel zurück, dessen Bestätigung erneut in die Aschenvorhalle führt
