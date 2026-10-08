## Purpose

Die gemalte Szenendarstellung legt fest, wie hochaufgelöste, gemalte Grafik im Spiel räumlich und lebendig wirkt: Shader, Farbgrading, Beleuchtung der Figuren, Ebenen mit Vordergrund sowie ruhige Drehung und Gangart der Figuren.

## ADDED Requirements

### Requirement: Shader-Effekte bauen auf allen Entwicklungsplattformen
Das System SHALL alle Shader-Effekte des Spiels mit dem normalen Build auf Windows, macOS und Linux erzeugen, ohne Wine, Windows SDK oder vorkompilierte Shader-Dateien im Repository.

#### Scenario: Build auf macOS
- **WHEN** auf einem Mac ohne Wine `dotnet build` ausgeführt wird
- **THEN** werden alle Shader-Effekte gebaut und das Spiel startet mit ihnen

#### Scenario: Build in der CI
- **WHEN** die CI auf Ubuntu und Windows baut
- **THEN** gelingt der Build einschließlich der Shader-Effekte auf beiden Systemen

### Requirement: Das Szenenbild erhält ein Farbgrading
Das System SHALL das fertige Szenenbild über eine Farb-Lookup-Tabelle des jeweiligen Bereichs graden. Bei aktivem Soul Sense SHALL das System stufenlos auf das Soul-Sense-Grading überblenden, wobei Seelen, Cores und Frakturen hervorstechen wie bisher. HUD und Menüs SHALL nicht gegradet werden.

#### Scenario: Arena wird gegradet
- **WHEN** die Arena gezeichnet wird
- **THEN** erscheint das Szenenbild mit dem Grading der Arena, HUD und Menüs dagegen in ihren unveränderten Farben

#### Scenario: Ufer und Arena unterscheiden sich
- **WHEN** der Spieler vom Ufer über den Prolog in die Arena gelangt
- **THEN** zeigt jeder Bereich sein eigenes Grading, ohne dass sich die Farben von Death Flame und Life Flame ändern

#### Scenario: Soul Sense blendet über
- **WHEN** der Spieler Soul Sense aktiviert
- **THEN** geht das Szenenbild innerhalb der bisherigen Einblendzeit in das Soul-Sense-Grading über und zurück, sobald er es beendet

### Requirement: Figuren werden räumlich beleuchtet
Das System SHALL Figuren, für die eine Normal-Map vorliegt, durch ein Schlüssellicht von oben links und durch die Soulfire-Lichter der Szene (Death Flame, Seelen, Effekte) räumlich beleuchten, sodass die einer Lichtquelle zugewandte Seite heller wird. Figuren ohne Normal-Map SHALL wie bisher gezeichnet werden. Die Beleuchtung SHALL Gesicht, Hände und Waffenführung nicht überstrahlen.

#### Scenario: Death Flame neben dem Spieler
- **WHEN** links neben dem Spieler eine Death-Flame-Lichtquelle leuchtet
- **THEN** ist die linke Seite der Spielerfigur sichtbar violett aufgehellt und die abgewandte Seite nicht

#### Scenario: Figur ohne Normal-Map
- **WHEN** ein Burning ohne Normal-Map gezeichnet wird
- **THEN** erscheint er wie vor der Änderung

### Requirement: Besiegte Gegner lösen sich auf
Das System SHALL einen besiegten Gegner, für den in der Registry eine Auflösung hinterlegt ist, nicht schlagartig entfernen, sondern ihn über eine Maske von den Rändern her in violette Death-Flame-Fragmente auflösen. Die Auflösung SHALL keine Kollision, keinen Schaden und keine Spielzeit beeinflussen.

#### Scenario: Hollow wird besiegt
- **WHEN** ein Hollow besiegt wird
- **THEN** löst sich seine letzte Pose sichtbar in violette Fragmente auf, während die Welle und Seelenmechanik genau wie bisher weiterlaufen

### Requirement: Vordergrund verdeckt keine Spielinformation
Das System SHALL Umgebungsebenen in einer festen Reihenfolge von fernem Hintergrund über Boden, niedrige Props, Akteure und hohe Props bis zu Vordergrund und Atmosphäre zeichnen und Akteure gegenüber Props nach ihrem Fußpunkt sortieren. Ein Vordergrund- oder Occluder-Element SHALL durchscheinend werden, solange es Spieler, Gegner oder einen Telegraph verdeckt.

#### Scenario: Spieler läuft hinter eine Säule
- **WHEN** der Spieler hinter eine hohe Säule im Vordergrund läuft
- **THEN** wird die Säule durchscheinend und der Spieler bleibt erkennbar

#### Scenario: Spieler steht vor einem Prop
- **WHEN** der Fußpunkt des Spielers vor dem Fußpunkt eines Props liegt
- **THEN** wird der Spieler vor dem Prop gezeichnet

### Requirement: Figuren drehen sich ruhig und laufen nach Strecke
Das System SHALL die sichtbare Blickrichtung einer Figur mit begrenzter Drehrate und mit Hysterese zwischen den acht Richtungen der Grafik nachführen. Zielen, Angriffsrichtung und Treffer SHALL weiterhin die sofortige Spielrichtung verwenden. Die Laufanimation SHALL nach zurückgelegter Strecke voranschreiten, nicht nach Zeit.

#### Scenario: Maus kreist schnell um den Spieler
- **WHEN** der Spieler die Maus schnell im Kreis um die Figur bewegt
- **THEN** dreht sich die Figur gleichmäßig durch die Richtungen, statt zwischen weit auseinanderliegenden Richtungen zu springen, und Angriffe gehen trotzdem genau in Mausrichtung

#### Scenario: Maus an einer Richtungsgrenze
- **WHEN** die Maus genau auf der Grenze zwischen zwei Richtungen leicht hin und her bewegt wird
- **THEN** wechselt die sichtbare Richtung nicht ständig hin und her

#### Scenario: Langsames Gehen
- **WHEN** der Spieler mit halber Geschwindigkeit läuft
- **THEN** läuft die Laufanimation halb so schnell, sodass die Füße nicht über den Boden rutschen
