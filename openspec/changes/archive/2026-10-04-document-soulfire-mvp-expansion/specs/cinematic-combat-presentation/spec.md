## ADDED Requirements

### Requirement: Produktionsnahe Assets besitzen klare Silhouetten
Das System SHALL Spieler, drei Gegnerrollen, Waffen, Seelen und Arena mit authored Assets darstellen und SHALL ihre kampfrelevanten Silhouetten auch bei Effektüberlagerung unterscheidbar halten.

#### Scenario: Mehrere Akteure und Effekte überlagern sich
- **WHEN** Angriffe, Partikel und Gegner im selben Arenabereich dargestellt werden
- **THEN** bleiben Spieler, Gegnerrolle, Telegraph und Treffergefahr durch Form, Kontrast oder Kontur erkennbar

### Requirement: Starke Aktionen erhalten abgestuftes Combat-Feedback
Das System SHALL Trefferfeedback anhand der Aktionsstärke aus Hitstop, Flash, Partikeln, Knockback, Kamera-Kick und Shake zusammensetzen.

#### Scenario: Normaler Treffer erfolgt
- **WHEN** ein normaler Sensen- oder Cannon-Treffer bestätigt wird
- **THEN** zeigt das System kurzes, zurückhaltendes Feedback, ohne den Spielfluss unlesbar zu unterbrechen

#### Scenario: Signaturaktion trifft oder aktiviert
- **WHEN** Soul Cleave, vollgeladene Cannon, Burning-Detonation oder Resonance ausgelöst wird
- **THEN** verwendet das System gegenüber normalen Treffern stärkere, aber zeitlich begrenzte Impact- und Kamerareaktionen

### Requirement: Soulfire-Licht und Zustandsdarstellung priorisieren Spielinformation
Das System SHALL Feuerquellen, Cannon-Ladung, Soul Sense, Resonance, Seelen und gegnerische Telegraphen mit zustandsabhängigen Licht- und Farbeffekten darstellen.

#### Scenario: Soul Sense ist aktiv
- **WHEN** der Wahrnehmungsmodus gezeichnet wird
- **THEN** tritt die gewöhnliche Welt zurück, während Seelen, Cores und Frakturen nahe Weiß beziehungsweise Violett hervorstechen

#### Scenario: Resonance ist aktiv
- **WHEN** der verstärkte Zustand gezeichnet wird
- **THEN** bleiben Spielersilhouette und Core trotz zusätzlicher Death Flame, Risse, Partikel und Afterimages klar lesbar

### Requirement: HUD kommuniziert kritische Kampfzustände
Das System SHALL Gesundheit, Cannon-Ladung, Dash-Verfügbarkeit, Resonance und aktuellen Arenafortschritt in Bildschirmkoordinaten darstellen.

#### Scenario: Ressourcenstatus ändert sich
- **WHEN** Gesundheit, Ladung, Cooldown, Resonance oder Welle ihren Zustand ändern
- **THEN** aktualisiert das HUD die zugehörige Anzeige ohne von Kamera oder Welttransformation verschoben zu werden
