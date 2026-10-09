## MODIFIED Requirements

### Requirement: Ein Run beginnt mit einem Glut-Basisvorrat
Das System SHALL beim Start eines Biom-Runs durch Tür I oder per Developer-Start, beim Developer-Start in die Arena und bei einem Neuversuch nach dem Tod in der Arena einen neuen Run beginnen. Zu Run-Beginn SHALL der Run-Bestand Geld null und der Run-Bestand Glut gleich dem Basisvorrat sein. Gesicherte Glut MUST NOT in den Run-Bestand übernommen werden.

#### Scenario: Spieler betritt die Arena
- **WHEN** das Arena-Intro nach `--dev --start arena` beginnt
- **THEN** zeigt der Run-Bestand Geld null und der Run-Bestand Glut den Basisvorrat, unabhängig vom gesicherten Bestand

#### Scenario: Spieler betritt Biom I
- **WHEN** die Eintrittssequenz durch Tür I endet
- **THEN** zeigt der Run-Bestand Geld null und der Run-Bestand Glut den Basisvorrat, unabhängig vom gesicherten Bestand

#### Scenario: Levelwechsel ist kein neuer Run
- **WHEN** der Spieler am Ende von Level 1 weiterreist
- **THEN** bleiben beide Run-Bestände unverändert und werden nicht auf den Basisvorrat zurückgesetzt

## ADDED Requirements

### Requirement: Der Biomabschluss sichert den ganzen Run-Bestand
Das System SHALL beim Biomabschluss beide Run-Bestände vollständig zu den gesicherten Beständen addieren, die Run-Bestände danach auf null setzen und das Profil speichern.

#### Scenario: Wächterraum wird geräumt
- **WHEN** der Wächterraum von Level 3 geräumt ist und der Biomabschluss beginnt
- **THEN** steigen die gesicherten Bestände um die bisherigen Run-Bestände, die Run-Bestände sind null, und der Biomabschluss nennt die gesicherten Beträge

### Requirement: Belohnungsräume enthalten eine Kiste
Das System SHALL in jedem Raum vom Typ Belohnung eines Biom-Levels beim Betreten des Levels eine geschlossene Kiste aufstellen, die sich wie die Kisten der Arena mit `E` öffnen lässt und ihren Geldbetrag genau einmal gutschreibt. Eine ungeöffnete Kiste SHALL beim Verlassen des Levels verfallen.

#### Scenario: Kiste im Belohnungsraum
- **WHEN** der Spieler die Kiste im Belohnungsraum öffnet
- **THEN** steigt der Run-Bestand Geld um den festgelegten Betrag, und die Kiste ist danach entfernt

#### Scenario: Kiste bleibt zurück
- **WHEN** der Spieler das Level verlässt, ohne die Kiste zu öffnen
- **THEN** steht die Kiste im nächsten Level nicht mehr, und der Run-Bestand Geld ist unverändert
