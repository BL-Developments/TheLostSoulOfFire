## Purpose

Ein Level wird Raum für Raum gespielt: Jeder Raum ist wie die Arena aufgebaut, Kampfräume müssen geräumt werden, und ein oder zwei Ausgänge führen in den nächsten Raum bis zum Levelende.

## ADDED Requirements

### Requirement: Ein Level beginnt mit einer neuen Raumfolge
Das System SHALL beim Start eines Levels einen Seed festlegen, daraus die Raumfolge erzeugen und den Spieler in den Startraum setzen. Der Seed SHALL in der Konsole als `LEVEL_SEED <n>` erscheinen. Ein Levelstart SHALL einen neuen Run mit den Run-Beständen eines Run-Beginns starten.

#### Scenario: Level wird gestartet
- **WHEN** ein Level ohne vorgegebenen Seed startet
- **THEN** steht der Spieler im Startraum, die Konsole zeigt `LEVEL_SEED` mit dem gewürfelten Seed, und der Run-Bestand Glut ist der Basisvorrat

#### Scenario: Neuer Start, neue Raumfolge
- **WHEN** zwei Level nacheinander ohne vorgegebenen Seed gestartet werden
- **THEN** werden zwei Seeds gewürfelt und in der Konsole genannt

### Requirement: Räume sind wie die Arena aufgebaut
Das System SHALL jeden Raum mit den Grenzen, der Kampffläche und dem Aussehen der Arena darstellen. Der Spieler SHALL einen Raum am Südtor betreten.

#### Scenario: Spieler betritt einen Raum
- **WHEN** der Spieler einen Raum betritt
- **THEN** steht er am Südtor innerhalb der Kampffläche, und die Kamera zeigt den Raum wie die Arena

### Requirement: Kampfräume müssen geräumt werden
Das System SHALL in einem Kampfraum kurz nach dem Betreten eine Begegnung starten. Bis alle Gegner der Begegnung besiegt sind, SHALL kein Ausgang nutzbar sein. Ein geräumter Raum SHALL ein Signal zeigen, dass seine Ausgänge offen sind.

#### Scenario: Begegnung läuft
- **WHEN** der Spieler während einer laufenden Begegnung an einem Ausgang `E` drückt
- **THEN** passiert nichts, und der Spieler bleibt im Raum

#### Scenario: Raum wird geräumt
- **WHEN** der letzte Gegner der Begegnung besiegt ist
- **THEN** öffnen sich die Ausgänge des Raums sichtbar

### Requirement: Start- und Levelende-Raum haben keine Gegner
Das System SHALL im Startraum und im Levelende keine Begegnung starten. Die Ausgänge des Startraums SHALL sofort offen sein.

#### Scenario: Spieler steht im Startraum
- **WHEN** das Level beginnt
- **THEN** erscheinen keine Gegner, und die Ausgänge des Startraums sind offen

### Requirement: Ausgänge führen in den nächsten Raum
Das System SHALL je Verbindung des Raums einen Ausgang in der Nordwand zeigen, bei zwei Ausgängen links und rechts. `E` in Reichweite eines offenen Ausgangs SHALL nach einer kurzen Blende in genau den Raum führen, mit dem dieser Ausgang verbunden ist.

#### Scenario: Wahl zwischen zwei Wegen
- **WHEN** ein geräumter Raum zwei Ausgänge hat und der Spieler am rechten Ausgang `E` drückt
- **THEN** betritt er nach der Blende den Raum, mit dem der rechte Ausgang verbunden ist

### Requirement: Der Raumwechsel erhält den Spielerzustand
Das System SHALL beim Raumwechsel Gesundheit, Run-Bestände, gewählte Fähigkeiten und ihre Abklingzeiten erhalten. Gegner, Seelen, Geschosse und Effekte des alten Raums SHALL entfernt werden.

#### Scenario: Raumwechsel mit wenig Gesundheit
- **WHEN** der Spieler mit halber Gesundheit und 40 Glut im Run den Raum wechselt
- **THEN** hat er im nächsten Raum halbe Gesundheit und 40 Glut im Run

### Requirement: Das Levelende führt vorläufig in den Hub
Das System SHALL im Levelende `LEVEL GESCHAFFT` zeigen. `E` an seinem Ausgang SHALL beide Run-Bestände vollständig sichern, das Profil speichern und in den Hub wechseln. Diese Regel gilt, bis Reisepunkte am Levelende stehen.

#### Scenario: Spieler verlässt das Levelende
- **WHEN** der Spieler im Levelende am Ausgang `E` drückt
- **THEN** steht er im Hub, die Run-Bestände sind null, und die gesicherten Bestände sind um die vorherigen Run-Bestände gestiegen

### Requirement: Niederlage im Level führt in den Hub
Das System SHALL beim Tod des Spielers in einem Raum regulären Kampfinput stoppen, den Todeszustand zeigen und danach in den Hub wechseln. Beide Run-Bestände SHALL verloren gehen. Ein Neuversuch im selben Raum MUST NOT angeboten werden.

#### Scenario: Spieler stirbt in einem Kampfraum
- **WHEN** der Spieler in einem Kampfraum stirbt
- **THEN** steht er nach dem Todeszustand steuerbar im Hub, die Run-Bestände sind null, und die gesicherten Bestände sind unverändert

### Requirement: Das HUD zeigt den Raumfortschritt
Das System SHALL im Kampf-HUD eines Levels `RAUM <n>` mit dem Fortschritt des aktuellen Raums anzeigen.

#### Scenario: Dritter Kampfraum
- **WHEN** der Spieler im Kampfraum der dritten Kampfstufe steht
- **THEN** zeigt das HUD `RAUM 3`
