## Why

Björn möchte Kampf, Charakterwerte und Gegner frei ausprobieren können, ohne Wellen, Truhen und Run-Ablauf (#104). Dafür braucht es einen eigenen Bereich, der wie die Arena aussieht, aber keine Wellen startet. Er soll nur Entwicklern zur Verfügung stehen und deshalb ausschließlich über den Developer-Mode erreichbar sein.

## What Changes

- Neuer Dev-Start-Bereich `sandbox` für `--dev --start`. Er ist aus Hauptmenü, Hub und Arena nicht erreichbar.
- Die Sandbox zeigt die Arena-Kulisse (Boden, Atmosphäre, Licht, Kamera) und setzt den Spieler steuerbar in die Mitte, ohne Arena-Intro, Wellen, Nachschub, Truhen, Wellenstart und Abschluss.
- In der Sandbox gibt es keine Währungen: Besiegte Gegner bringen keine Glut, eine Niederlage kostet nichts, und das HUD zeigt keine Run-Bestände. Oben steht `SANDBOX`.
- Nach einer Niederlage setzt `R` (wie `F8`) den Spieler mit vollem Leben in die Mitte zurück und räumt das Feld; der Spieler bleibt in der Sandbox.
- Die Debug-Tasten `F2` bis `F4` spawnen Gegner, damit die Sandbox ab diesem Change nutzbar ist. `--wave` wird mit `sandbox` wie bei anderen Bereichen abgelehnt; `--strength`, `--ability-power` und `--armor` gelten auch hier.
- Die Konsole meldet `DEV_START area=sandbox`.

## Capabilities

### New Capabilities

- `sandbox-mode`: Sandbox-Bereich mit Arena-Kulisse ohne Wellen, Truhen und Währungen.

### Modified Capabilities

- `developer-start-mode`: `sandbox` ist ein gültiger Startbereich.

## Impact

`DeveloperStartOptions` bekommt den Bereich `sandbox`. `GameWorld` erhält einen Sandbox-Zustand (neue Datei `GameWorld.Sandbox.cs`), der die Wellenlogik, Truhen, Glut, Run-Verlust und das Währungs-HUD überspringt und Neustarts in der Sandbox hält. README und Tests werden ergänzt. Keine neuen Assets, Abhängigkeiten oder Speicherdaten; ohne `--start sandbox` ändert sich nichts.
