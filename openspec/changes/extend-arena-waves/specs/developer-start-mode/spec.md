## MODIFIED Requirements

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
Das System SHALL bei unbekanntem Bereich, `--wave` außerhalb von 1 bis 10 oder ohne `--start arena`, `--start` oder `--wave` ohne `--dev` sowie bei `--dev` zusammen mit einem automatisierten Testflag eine Fehlermeldung mit den gültigen Bereichen ausgeben, kein Spielfenster öffnen und mit Exitcode `2` enden.

#### Scenario: Unbekannter Bereich
- **WHEN** das Spiel mit `--dev --start dungeon` gestartet wird
- **THEN** nennt die Meldung den unbekannten Bereich und alle gültigen Bereiche, und der Prozess endet mit Exitcode `2`

#### Scenario: Welle ohne Arena
- **WHEN** das Spiel mit `--dev --start hub --wave 2` gestartet wird
- **THEN** meldet das System, dass `--wave` nur mit `arena` gilt, und endet mit Exitcode `2`

#### Scenario: Welle außerhalb des Bereichs
- **WHEN** das Spiel mit `--dev --start arena --wave 11` gestartet wird
- **THEN** nennt die Meldung den gültigen Bereich 1 bis 10 und der Prozess endet mit Exitcode `2`
