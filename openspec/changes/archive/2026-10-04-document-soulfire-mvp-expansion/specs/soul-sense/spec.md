## ADDED Requirements

### Requirement: Soul Sense ist ein gehaltener Wahrnehmungsmodus
Das System SHALL Soul Sense aktivieren, solange der Spieler `Q` hält, und SHALL beim Loslassen in die normale Darstellung zurückkehren.

#### Scenario: Soul Sense wird gehalten
- **WHEN** der lebende Spieler `Q` hält
- **THEN** dämpft und entsättigt das System die gewöhnliche Welt und hebt seelenbezogene Elemente hervor

#### Scenario: Soul Sense wird losgelassen
- **WHEN** der Spieler `Q` loslässt und Resonance nicht aktiv ist
- **THEN** beendet das System Soul Sense und stellt normale Bewegung, Darstellung und Audiomischung wieder her

### Requirement: Gegner zeigen unterschiedliche Seelenanatomie
Das System SHALL in Soul Sense gegnerspezifische Schwachpunkte darstellen, statt allen Gegnern denselben generischen Core zu geben.

#### Scenario: Gegner werden untersucht
- **WHEN** Soul Sense aktiv ist und Hollow, Burning oder Devourer sichtbar sind
- **THEN** zeigt das System beim Hollow einen stabilen Brust-Core, beim Burning mehrere instabile Frakturen und beim Devourer die Seelenmasse im Torso

### Requirement: Manueller Soul Sense hat einen Bewegungspreis
Das System SHALL die Bewegung während manuell aktivem Soul Sense reduzieren, ohne Angriffe oder Dash zu sperren.

#### Scenario: Spieler kämpft in Soul Sense
- **WHEN** der Spieler Soul Sense manuell hält
- **THEN** bewegt er sich langsamer, kann aber Sense, Cannon und Dash weiterhin verwenden

#### Scenario: Resonance erzwingt Soul Sense
- **WHEN** Resonance aktiv ist
- **THEN** bleibt Soul Sense automatisch aktiv und die manuelle Bewegungsreduktion entfällt
