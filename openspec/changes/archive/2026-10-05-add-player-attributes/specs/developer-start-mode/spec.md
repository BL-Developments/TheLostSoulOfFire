## ADDED Requirements

### Requirement: Charakterwerte können beim Dev-Start gesetzt werden
Das System SHALL mit `--dev` die Parameter `--strength <n>`, `--ability-power <n>` und `--armor <n>` für `n` von 0 bis 99 annehmen und die genannten Charakterwerte des Spielers beim Start setzen; nicht genannte Werte SHALL beim Startwert 10 bleiben. Werte außerhalb des Bereichs, fehlende Zahlen oder diese Parameter ohne `--dev` SHALL wie andere ungültige Startparameter mit Meldung und Exitcode `2` abbrechen. Die `DEV_START`-Zeile SHALL bei gesetzten Werten alle drei Werte nennen.

#### Scenario: Stärke für die Arena setzen
- **WHEN** das Spiel mit `--dev --start arena --strength 20 --armor 0` gestartet wird
- **THEN** beginnt die Arena mit Stärke 20, Fähigkeitsstärke 10 und Rüstung 0, und die Konsole zeigt `DEV_START area=arena wave=1 strength=20 ability-power=10 armor=0`

#### Scenario: Wert außerhalb des Bereichs
- **WHEN** das Spiel mit `--dev --ability-power 100` gestartet wird
- **THEN** meldet das System den gültigen Bereich und endet mit Exitcode `2`
