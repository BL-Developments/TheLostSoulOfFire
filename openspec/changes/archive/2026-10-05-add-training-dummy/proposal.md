## Why

Laut #104 sollen sich in der Sandbox Dummy-Gegner spawnen lassen. Echte Gegner bewegen sich, greifen an und sterben nach wenigen Treffern; um Schaden von Sense, Kanone und Charakterwerten zu vergleichen, braucht es ein stillstehendes Ziel, das jeden Treffer lesbar anzeigt und stehen bleibt.

## What Changes

- Neuer Gegnertyp **Trainingspuppe**: Holzpfahl mit Strohkörper, bewegt sich nicht, greift nicht an und wird von Rückstoß nicht verschoben.
- Treffer wirken wie bei anderen Gegnern (Trefferblitz, Kontakteffekte, Kerntreffer bei Soul Sense über die üblichen Regeln), der Lebensbalken sinkt aber höchstens bis 1: Die Puppe stirbt nie, gibt keine Glut und setzt keine Seele frei.
- Jeder Treffer steigt als Zahl über der Puppe auf (Kerntreffer in Todesflammen-Farbe); darüber steht `SUMME <n>` mit dem Schaden seit dem letzten Auffüllen.
- Nach 2,5 Sekunden ohne Treffer füllt die Puppe ihr Leben wieder auf und die Summe beginnt neu.
- Die Puppe erscheint im Dev-Menü der Sandbox als `TRAININGSPUPPE` im Abschnitt `GEGNER` und lässt sich mit `ALLE GEGNER ENTFERNEN` wieder entfernen. In Arena-Wellen und im Prolog kommt sie nicht vor.

## Capabilities

### New Capabilities

Keine.

### Modified Capabilities

- `soulfire-enemy-roster`: Trainingspuppe als Sandbox-Gegner.
- `sandbox-mode`: Trainingspuppe im Dev-Menü spawnen.

## Impact

Neu ist `Entities/TrainingDummy`. `SandboxEnemyKind` und `SandboxSpawner` bekommen den Typ, wodurch der Menüeintrag automatisch entsteht. `GameBalance` erhält die Werte der Puppe (Leben 1000, Radius, Auffüllpause, Anzeigedauer der Zahlen) anstelle der ungenutzten Konstante `DummyMaxHealth`. Keine neuen Assets, Abhängigkeiten oder Speicherdaten; Arena-Wellen bleiben unverändert.
