## ADDED Requirements

### Requirement: Die Map zeigt das aktuelle Level
Das System SHALL auf dem Reiter `MAP` des Charaktermenüs die Raumfolge des Levels zeigen, in dem der Spieler gerade steht. Der Startraum SHALL unten stehen, jede weitere Stufe darüber; zwei Räume derselben Stufe SHALL links und rechts nebeneinander stehen, in der Reihenfolge ihrer Ausgänge. Über der Map SHALL im Biom-Run `BIOM <römisch> · LEVEL <n>` stehen, in einem Level aus dem Developer-Start `level` nur `LEVEL`. Räume früherer Level SHALL NOT angezeigt werden.

#### Scenario: Map im zweiten Level
- **WHEN** der Spieler in Level 2 von Biom I steht und den Reiter `MAP` wählt
- **THEN** zeigt die Seite `BIOM I · LEVEL 2` und nur Räume aus Level 2

#### Scenario: Gabelung nebeneinander
- **WHEN** der Spieler den rechten Ausgang einer Gabelung genommen hat und die Map öffnet
- **THEN** steht der betretene Raum rechts der Mitte über dem Raum, aus dem er kam

### Requirement: Räume werden Raum für Raum aufgedeckt
Das System SHALL auf der Map nur Räume zeigen, die der Spieler in diesem Level betreten hat, sowie die Wege zwischen zwei betretenen Räumen. Nicht betretene Räume, darunter der nicht gewählte Raum einer Gabelung, SHALL verdeckt bleiben, auch nachdem die Wege wieder zusammenlaufen. Die Map SHALL NOT erkennen lassen, wie viele Stufen das Level noch hat.

#### Scenario: Levelbeginn
- **WHEN** der Spieler im Startraum eines Levels die Map öffnet
- **THEN** zeigt sie nur den Startraum und die Ansätze seiner Ausgänge

#### Scenario: Nicht gewählter Weg
- **WHEN** der Spieler an einer Gabelung den linken Raum betreten hat und danach in den Raum gelangt, in dem beide Wege zusammenlaufen
- **THEN** zeigt die Map den linken Raum und den Zusammenlauf, der rechte Raum bleibt verdeckt

#### Scenario: Betretener Raum bleibt sichtbar
- **WHEN** der Spieler einen Raum verlassen hat und die Map öffnet
- **THEN** ist der verlassene Raum weiterhin sichtbar

### Requirement: Der aktuelle Raum und seine Ausgänge sind erkennbar
Das System SHALL den aktuellen Raum auf der Map hervorheben. Für jeden Ausgang des aktuellen Raums SHALL ein kurzer Wegansatz in Richtung seines Zielraums stehen, ohne den Zielraum zu zeigen. Ansätze zu Ausgängen, die noch geschlossen sind, SHALL gedämpft dargestellt werden.

#### Scenario: Geräumter Raum vor einer Gabelung
- **WHEN** der Spieler in einem geräumten Raum mit zwei Ausgängen die Map öffnet
- **THEN** ist der Raum hervorgehoben und zeigt einen Wegansatz nach links oben und einen nach rechts oben

#### Scenario: Kampf läuft noch
- **WHEN** der Spieler während des Kampfes in einem Kampfraum die Map öffnet
- **THEN** sind die Wegansätze des Raums gedämpft

### Requirement: Raumarten haben eigene Symbole
Das System SHALL Startraum, Kampfraum, Levelende mit Reisepunkt und Wächterraum mit je einem eigenen Symbol darstellen und unter der Map eine Legende mit diesen Symbolen und den Bezeichnungen `START`, `KAMPF`, `REISEPUNKT` und `WÄCHTER` zeigen.

#### Scenario: Levelende erreicht
- **WHEN** der Spieler in Level 1 das Levelende betreten hat und die Map öffnet
- **THEN** steht der Raum oben mit dem Symbol für den Reisepunkt

#### Scenario: Wächterraum erreicht
- **WHEN** der Spieler in Level 3 den Wächterraum betreten hat und die Map öffnet
- **THEN** steht der Raum oben mit dem Symbol für den Wächter

### Requirement: Jedes Level beginnt mit einer neuen Map
Das System SHALL die aufgedeckten Räume nur für das laufende Level führen. Beim Weiterreisen ins nächste Level, nach einer Niederlage und nach einer Extraktion SHALL die Map beim nächsten Level wieder nur den Startraum zeigen.

#### Scenario: Weiterreisen
- **WHEN** der Spieler am Reisepunkt von Level 1 weiterreist und im Startraum von Level 2 die Map öffnet
- **THEN** zeigt sie nur den Startraum von Level 2

### Requirement: Außerhalb eines Levels gibt es keine Karte
Das System SHALL auf dem Reiter `MAP` den gedämpften Hinweis `KEINE KARTE` zeigen, solange der Spieler in keinem Level steht, etwa im Hub, in der Arena, im Prolog oder in der Sandbox.

#### Scenario: Map im Hub
- **WHEN** der Spieler im Hub den Reiter `MAP` wählt
- **THEN** zeigt die Seite `KEINE KARTE` und das Spiel bleibt angehalten

### Requirement: M öffnet das Charaktermenü auf der Map
Das System SHALL bei `M` das Charaktermenü öffnen und `MAP` wählen, unter denselben Bedingungen, unter denen `Tab` das Menü öffnet. Bei geöffnetem Charaktermenü auf `MAP` SHALL `M` das Menü schließen wie `Tab`; auf einem anderen Reiter SHALL `M` den Reiter `MAP` wählen. `Tab` SHALL das Menü weiterhin auf `CHARAKTER` öffnen.

#### Scenario: M im Kampfraum
- **WHEN** der Spieler in einem Kampfraum `M` drückt
- **THEN** erscheint das Charaktermenü mit gewähltem `MAP`, und das Spiel ist angehalten

#### Scenario: M auf der Map
- **WHEN** das Charaktermenü auf `MAP` geöffnet ist und der Spieler `M` drückt
- **THEN** schließt sich das Menü, und das Spiel läuft an derselben Stelle weiter

#### Scenario: M auf einem anderen Reiter
- **WHEN** das Charaktermenü auf `CHARAKTER` geöffnet ist und der Spieler `M` drückt
- **THEN** ist `MAP` gewählt, und das Menü bleibt offen

#### Scenario: Titelphase und Pausenmenü
- **WHEN** der Spieler auf der Titelkarte, im Hauptmenü oder bei geöffnetem Pausenmenü `M` drückt
- **THEN** öffnet sich kein Charaktermenü
