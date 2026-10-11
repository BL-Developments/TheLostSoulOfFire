## ADDED Requirements

### Requirement: Weitere Charakterwerte können beim Dev-Start gesetzt werden
Das System SHALL mit `--dev` die Parameter `--attack-speed`, `--luck`, `--core-sharpness`, `--attunement`, `--agility`, `--focus` und `--steadiness` (je 0 bis 99) annehmen und damit die gleichnamigen Charakterwerte beim Start setzen; ungültige Werte SHALL wie bei `--strength` abbrechen. Die `DEV_START`-Zeile SHALL zusätzlich jeden dieser Werte nennen, der vom Startwert abweicht.

#### Scenario: Glück für die Arena setzen
- **WHEN** das Spiel mit `--dev --start arena --luck 30` gestartet wird
- **THEN** beginnt die Arena mit Glück 30 und die Konsole zeigt `DEV_START area=arena wave=1 strength=10 ability-power=10 armor=10 luck=30`

#### Scenario: Wert außerhalb des Bereichs
- **WHEN** das Spiel mit `--dev --focus 100` gestartet wird
- **THEN** meldet das System den gültigen Bereich und endet mit Exitcode `2`
