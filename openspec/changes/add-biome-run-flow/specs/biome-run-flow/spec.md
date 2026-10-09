## Purpose

Ein Biom-Run führt aus der Homebase durch drei Level eines Bioms bis zum Wächterraum und endet mit Biomabschluss, Extraktion oder Niederlage wieder in der Homebase. Seine Zustände sind von Prolog und Arena getrennt.

## ADDED Requirements

### Requirement: Ein Biom besteht aus drei Leveln
Das System SHALL einen Biom-Run als Folge von drei Leveln spielen. Jedes Level SHALL beim Betreten eine neue Raumfolge aus einem Seed erhalten, der aus dem Seed des Runs abgeleitet ist.

#### Scenario: Gleicher Run-Seed
- **WHEN** zwei Biom-Runs mit demselben Run-Seed gestartet werden
- **THEN** haben alle drei Level in beiden Runs dieselben Raumfolgen

### Requirement: Der Run hat eindeutige Zustände
Das System SHALL einen Biom-Run in genau einem der Zustände Homebase, Level läuft, Levelende, Extrahiert, Niederlage oder Biom abgeschlossen führen. Unerlaubte Übergänge SHALL wirkungslos bleiben. Prolog-Abschnitte, Arena und Debug-Sprünge MUST NOT den Zustand eines Biom-Runs verändern.

#### Scenario: Unerlaubter Übergang
- **WHEN** im Zustand Homebase ein Levelende gemeldet wird
- **THEN** bleibt der Run im Zustand Homebase

#### Scenario: Prolog-Neuversuch
- **WHEN** der Spieler im Prolog einen Abschnitt neu versucht
- **THEN** bleibt der Zustand des Biom-Runs unverändert

### Requirement: Ein Run beginnt immer bei Level 1
Das System SHALL einen Biom-Run aus der Homebase in Level 1 beginnen, unabhängig davon, wie weit ein früherer Versuch gekommen ist.

#### Scenario: Neuer Versuch nach Niederlage in Level 3
- **WHEN** der Spieler in Level 3 unterliegt, in den Hub zurückkehrt und Tür I erneut nutzt
- **THEN** beginnt der Run im Startraum von Level 1 mit neuer Raumfolge

### Requirement: Am Levelende von Level 1 und 2 steht ein Reisepunkt
Das System SHALL im Levelende von Level 1 und 2 einen Reisepunkt mit den Entscheidungen Teilsichern und weiter, Weiter ohne Sichern und Extrahieren zeigen. Quote, Rundung, Vorschau, Speichern und die Regel einer einzigen Entscheidung SHALL denen des Reisepunkts der Arena entsprechen.

#### Scenario: Teilsichern am Ende von Level 2
- **WHEN** der Spieler mit 175 Geld und 81 Glut im Run am Ende von Level 2 teilsichert
- **THEN** werden 87 Geld und 40 Glut gesichert, und der Spieler betritt den Startraum von Level 3

### Requirement: Weiterreisen führt ins nächste Level
Das System SHALL nach Teilsichern und weiter oder Weiter ohne Sichern den Startraum des nächsten Levels betreten lassen. Gesundheit, Run-Bestände, gewählte Fähigkeiten und der Raumfortschritt des Runs SHALL erhalten bleiben.

#### Scenario: Fortschritt läuft weiter
- **WHEN** der Spieler Level 1 nach fünf Kampfräumen ohne Sichern verlässt und in Level 2 den ersten Kampfraum betritt
- **THEN** hat dieser Raum den Fortschritt 6

### Requirement: Extrahieren beendet den Biom-Run im Hub
Das System SHALL beim Extrahieren an einem Levelende beide Run-Bestände vollständig sichern, das Profil speichern, in den Hub wechseln und kurz anzeigen, was extrahiert wurde.

#### Scenario: Extraktion nach Level 1
- **WHEN** der Spieler am Ende von Level 1 Extrahieren wählt
- **THEN** steht er im Hub, beide Run-Bestände sind null, und die gesicherten Bestände sind um die vorherigen Run-Bestände gestiegen

### Requirement: Level 3 endet im Wächterraum
Das System SHALL das Levelende von Level 3 als Wächterraum spielen. Bis zum Bosskampf SHALL dort eine Begegnung aus den vorhandenen Gegnern starten, die schwerer ist als die des vorherigen Kampfraums.

#### Scenario: Wächterraum betreten
- **WHEN** der Spieler den Wächterraum betritt
- **THEN** startet eine Begegnung mit mindestens so vielen Wellen und schweren Gegnern wie im vorherigen Kampfraum und mindestens einem davon mehr

### Requirement: Der geräumte Wächterraum schließt das Biom ab
Das System SHALL nach der Räumung des Wächterraums den Biomabschluss zeigen und danach in den Hub wechseln.

#### Scenario: Wächterraum wird geräumt
- **WHEN** der letzte Gegner im Wächterraum besiegt ist
- **THEN** zeigt das System den Biomabschluss mit den gesicherten Beträgen und wechselt danach in den Hub

### Requirement: Das HUD zeigt Biom, Level und Raum
Das System SHALL im Kampf-HUD eines Biom-Runs `BIOM <römisch> · LEVEL <n> · RAUM <m>` anzeigen, wobei `m` der Raumfortschritt des Runs ist.

#### Scenario: Spieler ist in Level 2
- **WHEN** der Spieler in Level 2 von Biom I im Kampfraum mit Fortschritt 7 steht
- **THEN** zeigt das HUD `BIOM I · LEVEL 2 · RAUM 7`
