## MODIFIED Requirements

### Requirement: Der Developer-Mode startet direkt in einem gewählten Bereich
Das System SHALL beim Start mit `--dev --start <bereich>` ohne Durchlaufen vorheriger Bereiche direkt im gewählten Bereich beginnen. Gültige Bereiche SHALL `title`, `prologue`, `prologue:find-trace`, `prologue:search`, `prologue:devourer`, `prologue:transit`, `hub`, `arena`, `sandbox` und `biome:1` sein; Groß-/Kleinschreibung SHALL ignoriert werden.

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

#### Scenario: Start in Biom I
- **WHEN** das Spiel mit `--dev --start biome:1` gestartet wird
- **THEN** beginnt ein neuer Run von Biom I im Eingangsraum von Level 1, wie nach der Eintrittssequenz durch Tür I

#### Scenario: Start in einem späteren Level
- **WHEN** das Spiel mit `--dev --start biome:1 --level 3` gestartet wird
- **THEN** beginnt ein neuer Run von Biom I im Eingangsraum von Level 3 mit den Run-Beständen eines Run-Beginns, und Abschluss, Extraktion und Niederlage verhalten sich wie im regulären Ablauf

### Requirement: Ungültige Startparameter brechen verständlich ab
Das System SHALL bei unbekanntem Bereich, `--wave` außerhalb von 1 bis 10 oder ohne `--start arena`, `--level` außerhalb von 1 bis 3 oder ohne `--start biome:<n>`, `--start`, `--wave` oder `--level` ohne `--dev` sowie bei `--dev` zusammen mit einem automatisierten Testflag eine Fehlermeldung mit den gültigen Bereichen ausgeben, kein Spielfenster öffnen und mit Exitcode `2` enden.

#### Scenario: Unbekannter Bereich
- **WHEN** das Spiel mit `--dev --start dungeon` gestartet wird
- **THEN** nennt die Meldung den unbekannten Bereich und alle gültigen Bereiche, und der Prozess endet mit Exitcode `2`

#### Scenario: Welle ohne Arena
- **WHEN** das Spiel mit `--dev --start hub --wave 2` gestartet wird
- **THEN** meldet das System, dass `--wave` nur mit `arena` gilt, und endet mit Exitcode `2`

#### Scenario: Welle außerhalb des Bereichs
- **WHEN** das Spiel mit `--dev --start arena --wave 11` gestartet wird
- **THEN** nennt die Meldung den gültigen Bereich 1 bis 10 und der Prozess endet mit Exitcode `2`

#### Scenario: Level ohne Biom
- **WHEN** das Spiel mit `--dev --start arena --level 2` gestartet wird
- **THEN** meldet das System, dass `--level` nur mit `biome:<n>` gilt, und endet mit Exitcode `2`

#### Scenario: Biom, das es nicht gibt
- **WHEN** das Spiel mit `--dev --start biome:2` gestartet wird
- **THEN** nennt die Meldung den unbekannten Bereich und alle gültigen Bereiche, und der Prozess endet mit Exitcode `2`

### Requirement: Der Dev-Start ist in der Konsole nachvollziehbar
Das System SHALL bei einem gültigen Dev-Start eine Zeile ausgeben, die mit `DEV_START` beginnt und den Bereich sowie bei der Arena die Welle und bei einem Biom das Level nennt.

#### Scenario: Konsolenausgabe
- **WHEN** das Spiel mit `--dev --start arena --wave 2` gestartet wird
- **THEN** erscheint in der Konsole `DEV_START area=arena wave=2`

#### Scenario: Konsolenausgabe für ein Biom
- **WHEN** das Spiel mit `--dev --start biome:1 --level 2` gestartet wird
- **THEN** erscheint in der Konsole `DEV_START area=biome:1 level=2`
