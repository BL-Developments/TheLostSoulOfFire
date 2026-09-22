## ADDED Requirements

### Requirement: Soul Residue füllt eine Resonance-Anzeige
Das System SHALL Resonance durch erfolgreiche Soul Releases und qualifizierte Core-Treffer aufbauen und SHALL den Bereitschaftszustand eindeutig im HUD und am Spieler kommunizieren.

#### Scenario: Resonance erreicht die Schwelle
- **WHEN** die Resonance-Anzeige vollständig gefüllt wird
- **THEN** zeigt das System einen klaren Ready-Zustand und ermöglicht die Aktivierung

### Requirement: Resonance wird manuell aktiviert
Das System SHALL bereitstehende Resonance durch `R` aktivieren und SHALL den Übergang durch eine kurze, markante Transformationssequenz darstellen.

#### Scenario: Spieler aktiviert bereite Resonance
- **WHEN** der lebende Spieler bei voller Resonance `R` drückt
- **THEN** startet eine kurze Freeze-/Silence-Phase, gefolgt von einer sicht- und hörbaren Death-Flame-Eruption

#### Scenario: Resonance ist nicht bereit
- **WHEN** der Spieler vor Erreichen der Schwelle `R` drückt
- **THEN** bleibt der normale Spielerzustand unverändert

### Requirement: Resonance verstärkt das bestehende Arsenal zeitlich begrenzt
Das System SHALL während Resonance Bewegung, Sense, Dash und Cannon verstärken und SHALL keine neue exklusive Angriffsart hinzufügen.

#### Scenario: Spieler kämpft während Resonance
- **WHEN** Resonance aktiv ist
- **THEN** sind Bewegung, Sensenwirkung, Dash und Cannon gegenüber ihrem Normalzustand verstärkt und Soul Sense ist ohne Bewegungspreis aktiv

#### Scenario: Resonance endet
- **WHEN** die Resonance-Dauer abläuft
- **THEN** stellt das System die normalen Werte wieder her und lässt Soul Sense nur aktiv, wenn der Spieler `Q` weiterhin hält
