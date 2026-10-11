# developer-start-mode Specification

## Purpose
Der Developer-Mode startet das Spiel per Kommandozeile direkt in einem gewählten Bereich oder einer Arena-Welle, ohne den normalen Start zu verändern.
## Requirements
### Requirement: Der Developer-Mode startet direkt in einem gewählten Bereich
Das System SHALL beim Start mit `--dev --start <bereich>` ohne Durchlaufen vorheriger Bereiche direkt im gewählten Bereich beginnen. Gültige Bereiche SHALL `title`, `prologue`, `prologue:find-trace`, `prologue:search`, `prologue:devourer`, `prologue:transit`, `hub`, `arena`, `sandbox`, `level` und `biome:1` sein; Groß-/Kleinschreibung SHALL ignoriert werden.

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

#### Scenario: Start in einem Level
- **WHEN** das Spiel mit `--dev --start level` gestartet wird
- **THEN** steht der Spieler im Startraum eines neu erzeugten Levels

#### Scenario: Start mit festem Seed
- **WHEN** das Spiel zweimal mit `--dev --start level --seed 4711` gestartet wird
- **THEN** ist die Raumfolge beide Male gleich, und die Konsole zeigt `LEVEL_SEED 4711`

#### Scenario: Start in Biom I
- **WHEN** das Spiel mit `--dev --start biome:1` gestartet wird
- **THEN** beginnt ein neuer Run von Biom I im Startraum von Level 1, wie nach der Eintrittssequenz durch Tür I

#### Scenario: Start in einem späteren Level
- **WHEN** das Spiel mit `--dev --start biome:1 --level 3` gestartet wird
- **THEN** beginnt ein neuer Run von Biom I im Startraum von Level 3 mit den Run-Beständen eines Run-Beginns, und Reisepunkt, Wächterraum und Niederlage verhalten sich wie im regulären Ablauf

### Requirement: Die Arena kann bei einer gewählten Welle beginnen
Das System SHALL beim Start mit `--dev --start arena` das Arena-Intro zeigen und danach Welle 1 starten. Mit zusätzlich `--wave <n>` für `n` von 1 bis 10 SHALL das Intro direkt in Welle `n` übergehen.

#### Scenario: Start in Welle 3
- **WHEN** das Spiel mit `--dev --start arena --wave 3` gestartet wird
- **THEN** folgt auf das Arena-Intro Welle 3 mit ihrer regulären Gegnerzusammensetzung

#### Scenario: Start in einer späten Welle
- **WHEN** das Spiel mit `--dev --start arena --wave 9` gestartet wird
- **THEN** folgt auf das Arena-Intro Welle 9 mit ihrem ersten Schub, und die weiteren Schübe rücken wie im regulären Ablauf nach

#### Scenario: Ablauf nach dem Einstieg
- **WHEN** der Spieler nach einem Dev-Start in der Arena stirbt oder die letzte Welle abschließt
- **THEN** verhält sich das System wie im regulären Ablauf

### Requirement: Ungültige Startparameter brechen verständlich ab
Das System SHALL bei unbekanntem Bereich, `--wave` außerhalb von 1 bis 10 oder ohne `--start arena`, `--seed` ohne ganze Zahl oder ohne `--start level` oder `biome:<n>`, `--level` außerhalb von 1 bis 3 oder ohne `--start biome:<n>`, `--start`, `--wave`, `--seed` oder `--level` ohne `--dev` sowie bei `--dev` zusammen mit einem automatisierten Testflag eine Fehlermeldung mit den gültigen Bereichen ausgeben, kein Spielfenster öffnen und mit Exitcode `2` enden.

#### Scenario: Unbekannter Bereich
- **WHEN** das Spiel mit `--dev --start dungeon` gestartet wird
- **THEN** nennt die Meldung den unbekannten Bereich und alle gültigen Bereiche, und der Prozess endet mit Exitcode `2`

#### Scenario: Welle ohne Arena
- **WHEN** das Spiel mit `--dev --start hub --wave 2` gestartet wird
- **THEN** meldet das System, dass `--wave` nur mit `arena` gilt, und endet mit Exitcode `2`

#### Scenario: Welle außerhalb des Bereichs
- **WHEN** das Spiel mit `--dev --start arena --wave 11` gestartet wird
- **THEN** nennt die Meldung den gültigen Bereich 1 bis 10 und der Prozess endet mit Exitcode `2`

#### Scenario: Seed ohne Level
- **WHEN** das Spiel mit `--dev --start arena --seed 3` gestartet wird
- **THEN** meldet das System, dass `--seed` nur mit `level` oder `biome:<n>` gilt, und endet mit Exitcode `2`

#### Scenario: Seed ist keine Zahl
- **WHEN** das Spiel mit `--dev --start level --seed abc` gestartet wird
- **THEN** meldet das System, dass `--seed` eine ganze Zahl erwartet, und endet mit Exitcode `2`

#### Scenario: Level ohne Biom
- **WHEN** das Spiel mit `--dev --start level --level 2` gestartet wird
- **THEN** meldet das System, dass `--level` nur mit `biome:<n>` gilt, und endet mit Exitcode `2`

#### Scenario: Biom, das es nicht gibt
- **WHEN** das Spiel mit `--dev --start biome:2` gestartet wird
- **THEN** nennt die Meldung den unbekannten Bereich und alle gültigen Bereiche, und der Prozess endet mit Exitcode `2`

### Requirement: Ohne Developer-Mode bleibt der Start unverändert
Das System SHALL ohne `--dev` genau wie bisher starten, einschließlich der bestehenden automatisierten Testflags.

#### Scenario: Normaler Start
- **WHEN** das Spiel ohne Argumente gestartet wird
- **THEN** erscheint das Hauptmenü und `Neues Spiel` beginnt den Prolog

### Requirement: Der Dev-Start ist in der Konsole nachvollziehbar
Das System SHALL bei einem gültigen Dev-Start eine Zeile ausgeben, die mit `DEV_START` beginnt und den Bereich sowie bei der Arena die Welle, bei einem Level oder Biom einen vorgegebenen Seed und bei einem Biom das Level nennt.

#### Scenario: Konsolenausgabe
- **WHEN** das Spiel mit `--dev --start arena --wave 2` gestartet wird
- **THEN** erscheint in der Konsole `DEV_START area=arena wave=2`

#### Scenario: Konsolenausgabe für ein Level mit Seed
- **WHEN** das Spiel mit `--dev --start level --seed 12` gestartet wird
- **THEN** erscheint in der Konsole `DEV_START area=level seed=12`

#### Scenario: Konsolenausgabe für ein Biom
- **WHEN** das Spiel mit `--dev --start biome:1 --level 2` gestartet wird
- **THEN** erscheint in der Konsole `DEV_START area=biome:1 level=2`

### Requirement: Charakterwerte können beim Dev-Start gesetzt werden
Das System SHALL mit `--dev` die Parameter `--strength <n>`, `--ability-power <n>` und `--armor <n>` für `n` von 0 bis 99 annehmen und die genannten Charakterwerte des Spielers beim Start setzen; nicht genannte Werte SHALL beim Startwert 10 bleiben. Werte außerhalb des Bereichs, fehlende Zahlen oder diese Parameter ohne `--dev` SHALL wie andere ungültige Startparameter mit Meldung und Exitcode `2` abbrechen. Die `DEV_START`-Zeile SHALL bei gesetzten Werten alle drei Werte nennen.

#### Scenario: Stärke für die Arena setzen
- **WHEN** das Spiel mit `--dev --start arena --strength 20 --armor 0` gestartet wird
- **THEN** beginnt die Arena mit Stärke 20, Fähigkeitsstärke 10 und Rüstung 0, und die Konsole zeigt `DEV_START area=arena wave=1 strength=20 ability-power=10 armor=0`

#### Scenario: Wert außerhalb des Bereichs
- **WHEN** das Spiel mit `--dev --ability-power 100` gestartet wird
- **THEN** meldet das System den gültigen Bereich und endet mit Exitcode `2`

### Requirement: Weitere Charakterwerte können beim Dev-Start gesetzt werden
Das System SHALL mit `--dev` die Parameter `--attack-speed`, `--luck`, `--core-sharpness`, `--attunement`, `--agility`, `--focus` und `--steadiness` (je 0 bis 99) annehmen und damit die gleichnamigen Charakterwerte beim Start setzen; ungültige Werte SHALL wie bei `--strength` abbrechen. Die `DEV_START`-Zeile SHALL zusätzlich jeden dieser Werte nennen, der vom Startwert abweicht.

#### Scenario: Glück für die Arena setzen
- **WHEN** das Spiel mit `--dev --start arena --luck 30` gestartet wird
- **THEN** beginnt die Arena mit Glück 30 und die Konsole zeigt `DEV_START area=arena wave=1 strength=10 ability-power=10 armor=10 luck=30`

#### Scenario: Wert außerhalb des Bereichs
- **WHEN** das Spiel mit `--dev --focus 100` gestartet wird
- **THEN** meldet das System den gültigen Bereich und endet mit Exitcode `2`

