## ADDED Requirements

### Requirement: Der Hub enthält sieben Biom-Türen
Das System SHALL in der Aschenvorhalle, die als Hub dient, sieben Türen darstellen, je eine pro Biom, erkennbar beschriftet mit den Ziffern `I` bis `VII` in fester Reihenfolge.

#### Scenario: Spieler betritt den Hub
- **WHEN** der Spieler steuerbar in der Aschenvorhalle erscheint
- **THEN** sind alle sieben Türen mit ihren Ziffern ohne Soul Sense sichtbar und über die begehbare Fläche erreichbar

### Requirement: Türen unterscheiden offen und versiegelt
Das System SHALL für jede Tür einen Sperrzustand führen. Tür I SHALL offen sein; die Türen II bis VII SHALL versiegelt sein. Versiegelte Türen SHALL sich visuell von der offenen Tür unterscheiden.

#### Scenario: Spieler betrachtet die Türen
- **WHEN** der Spieler die Türreihe sieht
- **THEN** erkennt er Tür I als zugänglich und die Türen II bis VII als versiegelt

### Requirement: Versiegelte Türen zeigen einen Hinweis und bleiben geschlossen
Das System SHALL in der Interaktionszone einer versiegelten Tür den Hinweis `SEALED · DEFEAT THE PREVIOUS GUARDIAN` anzeigen und SHALL bei `E` keinen Übergang starten.

#### Scenario: Spieler nähert sich einer versiegelten Tür
- **WHEN** der Spieler die Interaktionszone einer der Türen II bis VII betritt
- **THEN** zeigt das System den Hinweis `SEALED · DEFEAT THE PREVIOUS GUARDIAN`

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
