## RENAMED Requirements

- FROM: `### Requirement: Die offene Tür I führt in die Arena`
- TO: `### Requirement: Die offene Tür I führt in Biom I`

## MODIFIED Requirements

### Requirement: Die offene Tür I führt in Biom I
Das System SHALL in der Interaktionszone von Tür I eine Aufforderung zur Interaktion mit `E` anzeigen und SHALL bei `E` weitere Gameplay-Eingaben sperren und die inszenierte Eintrittssequenz starten. Nach der Eintrittssequenz SHALL ein neuer Run von Biom I in Level 1 beginnen. Die Arena SHALL nicht mehr über Tür I erreichbar sein.

#### Scenario: Spieler nähert sich Tür I
- **WHEN** der Spieler die Interaktionszone von Tür I betritt
- **THEN** zeigt das System eine zurückhaltende Aufforderung zur Interaktion mit `E`

#### Scenario: Spieler aktiviert Tür I
- **WHEN** der Spieler innerhalb der Interaktionszone von Tür I `E` drückt
- **THEN** sperrt das System weitere Gameplay-Eingaben und startet die Eintrittssequenz

#### Scenario: Eintrittssequenz endet
- **WHEN** die Eintrittssequenz durch Tür I endet
- **THEN** steht der Spieler im Startraum von Level 1 in Biom I

#### Scenario: Spieler drückt E außerhalb jeder Türzone
- **WHEN** der Spieler `E` außerhalb aller Interaktionszonen drückt
- **THEN** verbleibt das System in der Aschenvorhalle und startet keinen Übergang
