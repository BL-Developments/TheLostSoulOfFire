## Why

Die Fähigkeiten aus `add-first-run-abilities` (#105) lassen sich in der Sandbox (#104) nicht ausprobieren. Die Auswahl mit `C` geht nur in der Vorkammer, im Arena-Intro und in der Wellenpause, und die Sandbox hat keins davon. Wirken kostet Run-Glut, die es in der Sandbox nicht gibt. Gerade zum Ausprobieren von Fähigkeiten zusammen mit Charakterwerten und Trainingspuppe ist die Sandbox aber der richtige Ort.

## What Changes

- In der Sandbox öffnet `C` die Fähigkeitsauswahl jederzeit, solange der Spieler lebt und weder Pause-, Charakter- noch Dev-Menü offen ist. Die Welt friert während der Auswahl ein wie in der Arena.
- In der Sandbox kostet Wirken mit `Z`/`X` keine Glut. Abklingzeiten und alle übrigen Bedingungen (volles Leben, blockierter Rückstoßsprung, kampfunfähig) gelten wie in der Arena.
- Die Fähigkeitsleisten zeigen in der Sandbox `FREI` statt der Glutkosten und nie `GLUT FEHLT`.
- `ZWEITER ATEM` heilt bis zum aktuellen Maximalleben des Spielers statt bis zur festen 100. Außerhalb der Sandbox ist das Maximalleben immer 100, dort ändert sich nichts.
- Neustart in der Sandbox (`R`, `F8`, Niederlage) räumt laufende Fähigkeitseffekte und Abklingzeiten ab und behält die ausgerüsteten Fähigkeiten.

## Capabilities

### New Capabilities

Keine.

### Modified Capabilities

- `sandbox-mode`: Fähigkeiten auswählen und ohne Glutkosten wirken.

## Impact

`GameWorld.Abilities.cs` (`CanChooseAbilities`, Wirken, HUD) und `RunAbilities.TryCast` (Kosten abschaltbar, Heilgrenze), `Player.Heal` (Grenze `MaxHealth`). Arena, Vorkammer und Wallet verhalten sich außerhalb der Sandbox unverändert. Keine neuen Assets, Abhängigkeiten oder Speicherdaten.
