## MODIFIED Requirements

### Requirement: Der Developer-Mode startet direkt in einem gewählten Bereich
Das System SHALL beim Start mit `--dev --start <bereich>` ohne Durchlaufen vorheriger Bereiche direkt im gewählten Bereich beginnen. Gültige Bereiche SHALL `title`, `prologue`, `prologue:find-trace`, `prologue:search`, `prologue:devourer`, `prologue:transit`, `hub`, `arena` und `sandbox` sein; Groß-/Kleinschreibung SHALL ignoriert werden.

#### Scenario: Start im Hub
- **WHEN** das Spiel mit `--dev --start hub` gestartet wird
- **THEN** erscheint der Spieler steuerbar in der Aschenvorhalle, ohne Titelmenü und Prolog

#### Scenario: Start in einem Prolog-Abschnitt
- **WHEN** das Spiel mit `--dev --start prologue:search` gestartet wird
- **THEN** beginnt der Prolog im Such-Abschnitt, wie nach `D2` im laufenden Prolog

#### Scenario: Start im Titel
- **WHEN** das Spiel mit `--dev` oder `--dev --start title` gestartet wird
- **THEN** erscheint das Hauptmenü wie bei einem normalen Start

#### Scenario: Start in der Sandbox
- **WHEN** das Spiel mit `--dev --start sandbox` gestartet wird
- **THEN** erscheint der Spieler steuerbar in der Sandbox, und die Konsole zeigt `DEV_START area=sandbox`
