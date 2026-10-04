# hub-biome-doors Specification

## Purpose
Der Hub in der Aschenvorhalle zeigt sechs Biom-Türen und eine hervorgehobene Final-Tür; nur offene Türen führen weiter, versiegelte zeigen einen Hinweis.
## Requirements
### Requirement: Der Hub enthält sechs Biom-Türen und eine Final-Tür
Das System SHALL in der Aschenvorhalle, die als Hub dient, sieben Türen in einer Reihe darstellen: sechs Biom-Türen mit den Ziffern `I` bis `VI` und in der Mitte eine Final-Tür. Die Reihenfolge von links nach rechts SHALL `I`, `II`, `III`, Final-Tür, `IV`, `V`, `VI` sein.

#### Scenario: Spieler betritt den Hub
- **WHEN** der Spieler steuerbar in der Aschenvorhalle erscheint
- **THEN** sind alle sieben Türen ohne Soul Sense sichtbar und über die begehbare Fläche erreichbar, die Biom-Türen mit ihren Ziffern

### Requirement: Die Final-Tür ist optisch hervorgehoben
Das System SHALL die Final-Tür größer als jede Biom-Tür und mit einem eigenen Rahmen darstellen, sodass sie als Zugang zu Endboss und End-Level erkennbar ist.

#### Scenario: Spieler betrachtet die Türreihe
- **WHEN** der Spieler die Türreihe sieht
- **THEN** ist die mittlere Tür größer als die übrigen sechs und klar von ihnen unterscheidbar

### Requirement: Türen unterscheiden offen und versiegelt
Das System SHALL für jede Tür einen Sperrzustand führen. Biom-Tür I SHALL offen sein; die Biom-Türen II bis VI und die Final-Tür SHALL versiegelt sein. Versiegelte Türen SHALL sich visuell von der offenen Tür unterscheiden.

#### Scenario: Spieler betrachtet die Sperrzustände
- **WHEN** der Spieler die Türreihe sieht
- **THEN** erkennt er Tür I als zugänglich und alle anderen Türen als versiegelt

### Requirement: Versiegelte Türen zeigen einen Hinweis und bleiben geschlossen
Das System SHALL in der Interaktionszone einer versiegelten Biom-Tür den Hinweis `SEALED · DEFEAT THE PREVIOUS GUARDIAN` und in der Interaktionszone der versiegelten Final-Tür den Hinweis `SEALED · DEFEAT ALL GUARDIANS` anzeigen. Das System SHALL bei `E` an einer versiegelten Tür keinen Übergang starten.

#### Scenario: Spieler nähert sich einer versiegelten Biom-Tür
- **WHEN** der Spieler die Interaktionszone einer der Biom-Türen II bis VI betritt
- **THEN** zeigt das System den Hinweis `SEALED · DEFEAT THE PREVIOUS GUARDIAN`

#### Scenario: Spieler nähert sich der Final-Tür
- **WHEN** der Spieler die Interaktionszone der versiegelten Final-Tür betritt
- **THEN** zeigt das System den Hinweis `SEALED · DEFEAT ALL GUARDIANS`

#### Scenario: Spieler drückt E an einer versiegelten Tür
- **WHEN** der Spieler innerhalb der Interaktionszone einer versiegelten Tür `E` drückt
- **THEN** verbleibt das System steuerbar in der Aschenvorhalle und startet keinen Übergang

### Requirement: Die offene Tür I führt in die Arena
Das System SHALL in der Interaktionszone von Tür I eine Aufforderung zur Interaktion mit `E` anzeigen und SHALL bei `E` weitere Gameplay-Eingaben sperren und die inszenierte Eintrittssequenz in die Arena starten.

#### Scenario: Spieler nähert sich Tür I
- **WHEN** der Spieler die Interaktionszone von Tür I betritt
- **THEN** zeigt das System eine zurückhaltende Aufforderung zur Interaktion mit `E`

#### Scenario: Spieler aktiviert Tür I
- **WHEN** der Spieler innerhalb der Interaktionszone von Tür I `E` drückt
- **THEN** sperrt das System weitere Gameplay-Eingaben und startet die Eintrittssequenz

#### Scenario: Spieler drückt E außerhalb jeder Türzone
- **WHEN** der Spieler `E` außerhalb aller Interaktionszonen drückt
- **THEN** verbleibt das System in der Aschenvorhalle und startet keinen Übergang

