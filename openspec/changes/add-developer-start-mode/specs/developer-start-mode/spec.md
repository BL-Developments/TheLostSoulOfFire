## ADDED Requirements

### Requirement: Der Developer-Mode startet direkt in einem gewählten Bereich
Das System SHALL beim Start mit `--dev --start <bereich>` ohne Durchlaufen vorheriger Bereiche direkt im gewählten Bereich beginnen. Gültige Bereiche SHALL `title`, `prologue`, `prologue:find-trace`, `prologue:search`, `prologue:devourer`, `prologue:transit`, `hub` und `arena` sein; Groß-/Kleinschreibung SHALL ignoriert werden.

#### Scenario: Start im Hub
- **WHEN** das Spiel mit `--dev --start hub` gestartet wird
- **THEN** erscheint der Spieler steuerbar in der Aschenvorhalle, ohne Titelmenü und Prolog

#### Scenario: Start in einem Prolog-Abschnitt
- **WHEN** das Spiel mit `--dev --start prologue:search` gestartet wird
- **THEN** beginnt der Prolog im Such-Abschnitt, wie nach `D2` im laufenden Prolog

#### Scenario: Start im Titel
- **WHEN** das Spiel mit `--dev` oder `--dev --start title` gestartet wird
- **THEN** erscheint das Hauptmenü wie bei einem normalen Start

### Requirement: Die Arena kann bei einer gewählten Welle beginnen
Das System SHALL beim Start mit `--dev --start arena` das Arena-Intro zeigen und danach Welle 1 starten. Mit zusätzlich `--wave <n>` für `n` von 1 bis 4 SHALL das Intro direkt in Welle `n` übergehen.

#### Scenario: Start in Welle 3
- **WHEN** das Spiel mit `--dev --start arena --wave 3` gestartet wird
- **THEN** folgt auf das Arena-Intro Welle 3 mit ihrer regulären Gegnerzusammensetzung

#### Scenario: Ablauf nach dem Einstieg
- **WHEN** der Spieler nach einem Dev-Start in der Arena stirbt oder die letzte Welle abschließt
- **THEN** verhält sich das System wie im regulären Ablauf

### Requirement: Ungültige Startparameter brechen verständlich ab
Das System SHALL bei unbekanntem Bereich, `--wave` außerhalb von 1 bis 4 oder ohne `--start arena`, `--start` oder `--wave` ohne `--dev` sowie bei `--dev` zusammen mit einem automatisierten Testflag eine Fehlermeldung mit den gültigen Bereichen ausgeben, kein Spielfenster öffnen und mit Exitcode `2` enden.

#### Scenario: Unbekannter Bereich
- **WHEN** das Spiel mit `--dev --start dungeon` gestartet wird
- **THEN** nennt die Meldung den unbekannten Bereich und alle gültigen Bereiche, und der Prozess endet mit Exitcode `2`

#### Scenario: Welle ohne Arena
- **WHEN** das Spiel mit `--dev --start hub --wave 2` gestartet wird
- **THEN** meldet das System, dass `--wave` nur mit `arena` gilt, und endet mit Exitcode `2`

### Requirement: Ohne Developer-Mode bleibt der Start unverändert
Das System SHALL ohne `--dev` genau wie bisher starten, einschließlich der bestehenden automatisierten Testflags.

#### Scenario: Normaler Start
- **WHEN** das Spiel ohne Argumente gestartet wird
- **THEN** erscheint das Hauptmenü und `Neues Spiel` beginnt den Prolog

### Requirement: Der Dev-Start ist in der Konsole nachvollziehbar
Das System SHALL bei einem gültigen Dev-Start eine Zeile ausgeben, die mit `DEV_START` beginnt und den Bereich sowie bei der Arena die Welle nennt.

#### Scenario: Konsolenausgabe
- **WHEN** das Spiel mit `--dev --start arena --wave 2` gestartet wird
- **THEN** erscheint in der Konsole `DEV_START area=arena wave=2`
