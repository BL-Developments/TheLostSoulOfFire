## Purpose

Berechnet die Begegnung eines Kampfraums aus dem Fortschritt im Run und dem Seed des Raums, sodass Wellenzahl, Gegnerzahl und Anteil schwerer Gegner mit dem Fortschritt wachsen.

## ADDED Requirements

### Requirement: Der Fortschritt zählt die Kampfräume des Runs
Das System SHALL jedem betretenen Kampfraum als Fortschritt die Anzahl der im laufenden Run betretenen Kampfräume einschließlich dieses Raums geben. Start- und Levelende-Räume SHALL nicht zählen.

#### Scenario: Erster Kampfraum
- **WHEN** der Spieler nach dem Startraum den ersten Kampfraum betritt
- **THEN** hat dieser Raum den Fortschritt 1

#### Scenario: Gabelung
- **WHEN** der Spieler nach dem dritten Kampfraum einen von zwei parallelen Räumen wählt
- **THEN** hat der gewählte Raum den Fortschritt 4, egal welcher Ausgang genommen wurde

### Requirement: Eine Begegnung besteht aus Wellen
Das System SHALL eine Raum-Begegnung als Folge von Wellen spielen. Die nächste Welle SHALL nach einer kurzen Pause beginnen, sobald alle Gegner der vorherigen besiegt sind. Der Raum SHALL erst nach der letzten Welle geräumt sein.

#### Scenario: Zwei Wellen
- **WHEN** in einem Raum mit zwei Wellen der letzte Gegner der ersten Welle besiegt ist
- **THEN** beginnt nach der Pause die zweite Welle, und die Ausgänge bleiben geschlossen

### Requirement: Wellen und schwere Gegner wachsen mit dem Fortschritt
Das System SHALL Wellenzahl, Gegner je Welle und Anzahl schwerer Gegner aus dem Fortschritt berechnen. Keiner dieser Werte MUST von einem Fortschritt zum nächsten sinken. Wellenzahl und Gegnerzahl SHALL nach oben begrenzt sein.

#### Scenario: Früher und später Raum
- **WHEN** die Begegnungen für Fortschritt 1 und Fortschritt 12 berechnet werden
- **THEN** hat Fortschritt 12 mindestens so viele Wellen, Gegner und schwere Gegner wie Fortschritt 1 und in mindestens einem dieser Werte mehr

#### Scenario: Obergrenze
- **WHEN** die Begegnung für einen sehr hohen Fortschritt berechnet wird
- **THEN** überschreiten Wellenzahl und Gegner je Welle ihre Höchstwerte nicht

### Requirement: Schwere Gegner kommen schrittweise hinzu
Das System SHALL im ersten Kampfraum nur Hollow einsetzen. Burning SHALL ab einem festgelegten Fortschritt erscheinen, Devourer ab einem späteren.

#### Scenario: Erster Raum
- **WHEN** die Begegnung für Fortschritt 1 berechnet wird
- **THEN** enthält sie nur Hollow

#### Scenario: Devourer
- **WHEN** die Begegnung für einen Fortschritt unterhalb der Devourer-Schwelle berechnet wird
- **THEN** enthält sie keinen Devourer

### Requirement: Der Seed bestimmt Mischung und Reihenfolge
Das System SHALL die Verteilung der schweren Gegner auf die Wellen und die Reihenfolge innerhalb einer Welle aus dem Seed des Raums würfeln. Gleicher Seed und gleicher Fortschritt SHALL dieselbe Begegnung ergeben; die Anzahlen je Gegnertyp SHALL nur vom Fortschritt abhängen.

#### Scenario: Gleicher Seed
- **WHEN** zweimal für denselben Seed und Fortschritt gerechnet wird
- **THEN** sind beide Begegnungen gleich

#### Scenario: Paralleler Raum
- **WHEN** zwei parallele Räume mit gleichem Fortschritt verschiedene Seeds haben
- **THEN** enthalten beide gleich viele Gegner je Typ, möglicherweise anders auf die Wellen verteilt

### Requirement: Die Stellschrauben stehen an einer Stelle
Das System SHALL Startwerte, Zuwachs je Fortschritt, Höchstwerte, Schwellen für Burning und Devourer sowie die Pause zwischen Wellen als Balance-Werte an einer Stelle führen.

#### Scenario: Wert wird angepasst
- **WHEN** der Zuwachs der Gegner je Welle in den Balance-Werten erhöht wird
- **THEN** ändern sich die berechneten Begegnungen ohne weitere Codeänderung
