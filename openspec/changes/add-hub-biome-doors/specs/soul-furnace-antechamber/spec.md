## REMOVED Requirements

### Requirement: Ein massives Tor kontrolliert den Arena-Eintritt
**Reason**: Das Tor entfällt. Die Aschenvorhalle wird zum Hub mit sechs Biom-Türen und einer Final-Tür; der Arena-Eintritt liegt hinter Tür I.
**Migration**: Siehe `hub-biome-doors`, Requirement „Die offene Tür I führt in die Arena“.

## RENAMED Requirements

- FROM: `### Requirement: Der Toreintritt führt nahtlos in das Arena-Intro`
- TO: `### Requirement: Der Eintritt durch Tür I führt nahtlos in das Arena-Intro`

## MODIFIED Requirements

### Requirement: Der Eintritt durch Tür I führt nahtlos in das Arena-Intro
Das System SHALL den Eintritt durch Öffnen von Tür I, Kamera-Vorschub, Soulfire-Licht und Audioüberblendung darstellen und SHALL anschließend das bestehende Arena-Intro starten.

#### Scenario: Eintrittssequenz endet
- **WHEN** die kurze Eintrittssequenz an Tür I abgeschlossen ist
- **THEN** setzt das System den Spieler am Arena-Spawn zurück, wechselt in die Arena-Phase und beginnt deren Intro vor Welle eins

#### Scenario: Audio ist nicht verfügbar
- **WHEN** der Eintritt ohne verfügbares Audiogerät oder ohne passenden Cue erfolgt
- **THEN** beendet das System den visuellen Übergang weiterhin und startet die Arena ohne Abbruch

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
