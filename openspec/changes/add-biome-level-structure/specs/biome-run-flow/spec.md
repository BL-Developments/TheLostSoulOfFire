## Purpose

Ein Biom-Run führt aus der Homebase durch die drei Level eines Bioms bis zum Wächterraum und endet mit Biomabschluss, Extraktion oder Niederlage wieder in der Homebase. Die Run-Zustände sind vom Prolog und von der Arena getrennt.

## ADDED Requirements

### Requirement: Ein Biom besteht aus drei Leveln
Das System SHALL jedes Biom als geordnete Folge von drei Leveln führen. Level 1 und 2 SHALL mit einem Raum vom Typ Levelende enden, Level 3 SHALL mit einem Wächterraum enden.

#### Scenario: Biom I wird geladen
- **WHEN** die Definition von Biom I geprüft wird
- **THEN** hat sie drei Level, Level 1 und 2 enden mit Levelende, und Level 3 endet mit dem Wächterraum

### Requirement: Der Run hat eindeutige Zustände
Das System SHALL einen Biom-Run in genau einem der Zustände Homebase, Level läuft, Levelende, Extrahiert, Niederlage oder Biom abgeschlossen führen. Übergänge SHALL nur zwischen den in diesem Spec beschriebenen Zuständen möglich sein. Prolog-Abschnitte, Arena-Neuversuch und Debug-Sprünge MUST NOT den Zustand eines Biom-Runs verändern.

#### Scenario: Unerlaubter Übergang
- **WHEN** im Zustand Homebase ein Levelende gemeldet wird
- **THEN** bleibt der Run im Zustand Homebase

#### Scenario: Prolog-Neuversuch
- **WHEN** der Spieler im Prolog einen Abschnitt neu versucht
- **THEN** bleibt der Zustand des Biom-Runs unverändert

### Requirement: Ein Run beginnt immer bei Level 1
Das System SHALL beim Start eines Biom-Runs aus der Homebase in Level 1 des Bioms beginnen, unabhängig davon, wie weit ein früherer Versuch gekommen ist.

#### Scenario: Neuer Versuch nach Niederlage in Level 3
- **WHEN** der Spieler in Level 3 unterliegt, in den Hub zurückkehrt und Tür I erneut betritt
- **THEN** beginnt der Run in Level 1 von Biom I

### Requirement: Am Levelende steht ein Reisepunkt
Das System SHALL im Raum Levelende von Level 1 und 2 einen Reisepunkt mit den Entscheidungen Teilsichern und weiter, Weiter ohne Sichern und Extrahieren zeigen. Quote, Rundung, Vorschau, Speichern und die Regel einer einzigen Entscheidung je Reisepunkt SHALL denen des Reisepunkts der Arena entsprechen.

#### Scenario: Spieler erreicht das Ende von Level 1
- **WHEN** der Spieler den Raum Levelende von Level 1 betritt
- **THEN** steht dort ein Reisepunkt, und `E` in seiner Reichweite öffnet das Menü mit den drei Entscheidungen

#### Scenario: Teilsichern am Ende von Level 2
- **WHEN** der Spieler mit 175 Geld und 81 Glut im Run am Ende von Level 2 teilsichert
- **THEN** werden 87 Geld und 40 Glut gesichert, und das Spiel wechselt in Level 3

### Requirement: Weiterreisen führt ins nächste Level
Das System SHALL nach Teilsichern und weiter oder Weiter ohne Sichern das nächste Level im Eingangsraum beginnen. Run-Bestände, Gesundheit und gewählte Fähigkeiten SHALL dabei erhalten bleiben; Gegner, Seelen, Kisten und Effekte des alten Levels SHALL entfernt werden.

#### Scenario: Weiter ohne Sichern
- **WHEN** der Spieler am Ende von Level 1 Weiter ohne Sichern wählt
- **THEN** steht er im Eingangsraum von Level 2 mit unveränderten Run-Beständen und unveränderter Gesundheit

### Requirement: Extrahieren beendet den Biom-Run im Hub
Das System SHALL beim Extrahieren an einem Levelende beide Run-Bestände vollständig sichern, das Profil speichern, in den Hub wechseln und kurz anzeigen, was extrahiert wurde.

#### Scenario: Extraktion nach Level 1
- **WHEN** der Spieler am Ende von Level 1 Extrahieren wählt
- **THEN** steht er im Hub, beide Run-Bestände sind null, und die gesicherten Bestände sind um die vorherigen Run-Bestände gestiegen

### Requirement: Der Wächterraum schließt das Biom ab
Das System SHALL im Wächterraum von Level 3 eine Begegnung starten, die bis zum Bosskampf als verstärkte Begegnung aus den vorhandenen Gegnern besteht. Ist sie geräumt, SHALL das System den Biomabschluss zeigen, beide Run-Bestände sichern und danach in den Hub wechseln.

#### Scenario: Wächterraum wird geräumt
- **WHEN** der letzte Gegner im Wächterraum besiegt ist
- **THEN** zeigt das System den Biomabschluss mit den gesicherten Beträgen und wechselt danach in den Hub

### Requirement: Niederlage im Biom führt in den Hub
Das System SHALL beim Tod des Spielers in einem Biom-Level regulären Kampfinput stoppen, nach dem Todeszustand eine kurze Bergungsblende zeigen und dann in den Hub wechseln. Beide Run-Bestände SHALL verloren gehen; gesicherte Bestände SHALL erhalten bleiben. Ein Neuversuch im selben Level MUST NOT angeboten werden.

#### Scenario: Spieler stirbt in Level 2
- **WHEN** der Spieler in Level 2 stirbt
- **THEN** steht er nach der Bergungsblende steuerbar im Hub, die Run-Bestände sind null, und der nächste Run beginnt bei Level 1

### Requirement: Das HUD zeigt Biom und Level
Das System SHALL im Kampf-HUD eines Biom-Levels Biom und Level als `BIOM <römisch> · LEVEL <n>` anzeigen.

#### Scenario: Spieler ist in Level 2 von Biom I
- **WHEN** das Kampf-HUD in Level 2 von Biom I sichtbar ist
- **THEN** zeigt es `BIOM I · LEVEL 2`
