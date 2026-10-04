## MODIFIED Requirements

### Requirement: Arena fortschreitet durch definierte Wellen
Das System SHALL Gegner in zehn unterscheidbaren Wellen einführen und SHALL die nächste Phase erst nach Erfüllung der aktuellen Abschlussbedingung starten. Eine Welle SHALL erst als geleert gelten, wenn alle ihre Schübe erschienen und alle Gegner und Seelen verschwunden sind. Nach dem Leeren jeder Welle außer der zehnten SHALL das System eine Pause einlegen, in der der Spieler sich frei bewegen kann. Die nächste Welle SHALL erst starten, wenn der Spieler in der markierten Zone in der Arenamitte `E` drückt. Die erste Welle und das Finale nach der zehnten Welle SHALL ohne Auslösen beginnen. Die Ankündigung SHALL die Wellen 1 bis 9 mit ihrer römischen Nummer und die zehnte Welle als `FINAL WAVE` nennen.

#### Scenario: Welle beginnt
- **WHEN** die vorherige Übergangsphase endet
- **THEN** kündigt das System die neue Welle an und erzeugt den ersten Schub ihrer vorgesehenen Gegnerzusammenstellung

#### Scenario: Welle wird geleert
- **WHEN** alle Schübe einer der Wellen 1 bis 9 erschienen und alle ihre Gegner besiegt sind
- **THEN** spielt das System Abschlussfeedback, markiert die Zone in der Arenamitte und wartet ohne Zeitlimit

#### Scenario: Gegner einer Welle sind besiegt, ein Schub steht noch aus
- **WHEN** alle lebenden Gegner besiegt sind, die Welle aber noch einen ausstehenden Schub hat
- **THEN** gilt die Welle nicht als geleert, und der nächste Schub wird angekündigt

#### Scenario: Spieler startet die nächste Welle
- **WHEN** der Spieler während der Pause in der markierten Zone `E` drückt
- **THEN** zeigt das System die Aufforderung nicht mehr, kündigt die nächste Welle an und erzeugt sie nach der Übergangsphase

#### Scenario: Spieler steht außerhalb der Zone
- **WHEN** der Spieler während der Pause außerhalb der markierten Zone `E` drückt
- **THEN** startet keine neue Welle

#### Scenario: Letzte Welle wird angekündigt
- **WHEN** der Spieler die Pause nach der neunten Welle beendet
- **THEN** lautet die Aufforderung `LETZTE WELLE STARTEN` und die Ankündigung `FINAL WAVE`

#### Scenario: Letzte Welle wird geleert
- **WHEN** alle Schübe der zehnten Welle erschienen und alle ihre Gegner besiegt sind
- **THEN** wechselt das System ohne Pause zum Finale

## ADDED Requirements

### Requirement: Spätere Wellen bringen mehr Gegner in Schüben
Das System SHALL die Wellen 1 bis 4 mit ihrer bisherigen Zusammensetzung als einen einzigen Schub beim Wellenstart erzeugen. Ab Welle 5 SHALL eine Welle aus mehreren Schüben bestehen, und die Gesamtzahl der Gegner SHALL von Welle zu Welle nicht sinken und in Welle 10 am höchsten sein. Zusammensetzung je Schub, Wartezeit zwischen Schüben, Ankündigungsdauer, Höchstzahl gleichzeitiger Gegner und Mindestabstand zum Spieler SHALL als Balance-Werte an einer Stelle festgelegt sein.

#### Scenario: Frühe Welle
- **WHEN** Welle 3 beginnt
- **THEN** erscheinen zwei Hollow, zwei Burning und ein Devourer gleichzeitig, und es folgt kein weiterer Schub

#### Scenario: Späte Welle
- **WHEN** Welle 8 beginnt
- **THEN** erscheint zunächst nur ihr erster Schub, und die übrigen Gegner der Welle rücken in weiteren Schüben nach

### Requirement: Nachschub rückt über die Zeit nach
Das System SHALL den nächsten Schub einer Welle auslösen, sobald seit dem vorigen Schub die festgelegte Wartezeit vergangen ist oder höchstens ein Gegner der Welle noch lebt. Würde der Schub die Höchstzahl gleichzeitiger Gegner überschreiten, wobei angekündigte Gegner mitzählen, SHALL er warten. Nachrückende Gegner SHALL an festen Spawnpunkten am Rand des Kampfbereichs erscheinen, die mindestens den festgelegten Abstand zum Spieler haben, und SHALL sich dort vor dem Erscheinen für die festgelegte Ankündigungsdauer durch eine sichtbare Markierung ankündigen. Während der Ankündigung SHALL der Gegner weder Schaden nehmen noch Schaden verursachen.

#### Scenario: Wartezeit läuft ab
- **WHEN** seit dem vorigen Schub die Wartezeit vergangen ist und der Schub die Höchstzahl nicht überschreitet
- **THEN** erscheinen Markierungen an Spawnpunkten am Arenarand und nach der Ankündigungsdauer die Gegner des nächsten Schubs

#### Scenario: Spieler leert das Feld schnell
- **WHEN** vor Ablauf der Wartezeit nur noch ein Gegner der Welle lebt
- **THEN** wird der nächste Schub sofort angekündigt

#### Scenario: Höchstzahl würde überschritten
- **WHEN** die Wartezeit abgelaufen ist, aber lebende Gegner und der nächste Schub zusammen die Höchstzahl überschreiten würden
- **THEN** wartet der Schub, bis genug Gegner besiegt sind

#### Scenario: Spieler steht am Spawnpunkt
- **WHEN** ein Schub fällig wird und der Spieler näher als den Mindestabstand an einem Spawnpunkt steht
- **THEN** wählt das System für diesen Schub nur Spawnpunkte, die mindestens den Mindestabstand entfernt sind

### Requirement: Das HUD zeigt die aktuelle Welle
Das System SHALL im Kampf-HUD der Arena ab der ersten Welle die aktuelle Welle und die Gesamtzahl als `WELLE <n>/10` anzeigen. Hat die Welle mehrere Schübe, SHALL das HUD je Schub eine Markierung zeigen, die hervorgehoben ist, sobald der Schub ausgelöst wurde. Die zehnte Welle SHALL im HUD hervorgehoben sein.

#### Scenario: Spieler kämpft in Welle 3
- **WHEN** das Kampf-HUD während Welle 3 sichtbar ist
- **THEN** zeigt es `WELLE 3/10` ohne Schubmarkierungen

#### Scenario: Nachschub rückt nach
- **WHEN** in Welle 8 der zweite von drei Schüben ausgelöst wird
- **THEN** zeigt das HUD `WELLE 8/10` mit zwei hervorgehobenen und einer leeren Schubmarkierung

#### Scenario: Pause nach einer Welle
- **WHEN** die Arena nach Welle 5 pausiert
- **THEN** zeigt das HUD weiter `WELLE 5/10`, bis die nächste Welle beginnt
