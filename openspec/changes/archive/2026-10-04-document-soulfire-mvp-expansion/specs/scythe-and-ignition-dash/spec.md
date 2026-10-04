## ADDED Requirements

### Requirement: Dreistufige Sensenkombo
Das System SHALL auf aufeinanderfolgende primäre Angriffe eine dreistufige Sensenkombo ausführen und SHALL nach Ablauf des Kombofensters wieder mit dem ersten Schlag beginnen.

#### Scenario: Kombo wird fortgesetzt
- **WHEN** der Spieler während des zulässigen Folgeschlagfensters erneut primär angreift
- **THEN** führt das System den nächsten Schlag der Kombo mit eigener Reichweite, Schaden und Trefferpräsentation aus

#### Scenario: Kombo läuft aus
- **WHEN** nach einem Schlag keine rechtzeitige Folgeeingabe erfolgt
- **THEN** setzt das System den nächsten Sensenangriff auf Komboschritt eins zurück

### Requirement: Soul Cleave schließt die Kombo ab
Das System SHALL den dritten Komboschritt als stärkeren Soul Cleave mit größerer Wirkung als die vorherigen Schläge ausführen.

#### Scenario: Dritter Schlag trifft
- **WHEN** Soul Cleave einen Gegner trifft
- **THEN** verursacht der Treffer erhöhten Schaden und verstärktes Knockback sowie deutlich stärkeres visuelles und kamerabasiertes Feedback

### Requirement: Ignition Dash bewegt und schützt den Spieler
Das System SHALL bei einer Dash-Eingabe eine kurze gerichtete Bewegung mit temporärer Unverwundbarkeit ausführen und SHALL einen erneuten Dash bis zum Ende seiner Abklingzeit verhindern.

#### Scenario: Dash mit Bewegungseingabe
- **WHEN** der Spieler bei bereitem Dash eine Bewegungsrichtung hält und `Space` drückt
- **THEN** bewegt sich der Spieler schnell in diese Richtung und ignoriert während des aktiven Dash-Schritts eingehenden Schaden

#### Scenario: Dash ohne Bewegungseingabe
- **WHEN** der Spieler bei bereitem Dash ohne Bewegungsrichtung `Space` drückt
- **THEN** verwendet das System die aktuelle Blickrichtung als Dash-Richtung

#### Scenario: Resonance verstärkt den Dash
- **WHEN** ein Dash während aktiver Resonance beginnt
- **THEN** verwendet er eine größere Distanz und eine kürzere Abklingzeit als im Normalzustand
