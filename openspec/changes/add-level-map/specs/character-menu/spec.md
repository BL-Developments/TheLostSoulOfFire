## RENAMED Requirements

- FROM: `### Requirement: Map und Skills sind Platzhalter`
- TO: `### Requirement: Skills ist ein Platzhalter`

## MODIFIED Requirements

### Requirement: Skills ist ein Platzhalter
Das System SHALL den Reiter `SKILLS` wählbar machen, ihn in der gedämpften Farbe der Platzhaltereinträge des Hauptmenüs darstellen und auf seiner Seite nur den Hinweis `NOCH NICHT VERFÜGBAR` zeigen. Der Reiter `MAP` SHALL in der Farbe der übrigen Reiter stehen und die Map nach `level-map` zeigen.

#### Scenario: Skills wird gewählt
- **WHEN** der Spieler den Reiter `SKILLS` wählt
- **THEN** zeigt die Seite `NOCH NICHT VERFÜGBAR` und das Spiel bleibt angehalten

#### Scenario: Map wird gewählt
- **WHEN** der Spieler den Reiter `MAP` wählt
- **THEN** steht `MAP` in derselben Farbe wie `CHARAKTER`, seine Seite zeigt nicht `NOCH NICHT VERFÜGBAR`, und das Spiel bleibt angehalten
