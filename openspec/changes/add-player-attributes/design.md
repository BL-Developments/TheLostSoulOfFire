## Context

Schaden entsteht an drei Stellen: `ScytheCombat.BuildStrike` (Sensenschläge), `SoulCannon.Fire` (Kanonenschuss) und `Player.ApplyDamage` (erlittene Treffer von Hollow, Burning und Devourer). Resonance multipliziert Sense und Kanone bereits dort.

## Goals / Non-Goals

**Goals:** Drei Charakterwerte mit nachvollziehbarer, getesteter Wirkung; das heutige Balancing bleibt beim Startwert für ausgeteilten Schaden unverändert.

**Non-Goals:** Anzeige im HUD oder Pausenmenü, Stufenaufstieg, Ausrüstung, Speichern der Werte. Rüstung wirkt nicht auf Rückstoß oder Unverwundbarkeit.

## Decisions

- **Eigener Werttyp `PlayerAttributes`** (readonly record struct, ohne MonoGame) mit den Formeln als Methoden. So sind die Regeln ohne Spielfenster testbar, und Sense und Kanone bekommen die Werte pro Update wie heute schon `resonanceActive`.
- **Lineare Skalierung um den Startwert 10** (`1 + (Wert − 10) × 0,05`). Bei 10 bleibt jede bisherige Schadenszahl gleich, und jeder Punkt ist gleich viel wert. Untergrenze 1 Schaden.
- **Rüstung mit abnehmendem Ertrag** (`Rüstung / (Rüstung + 50)`). Rüstung kann so nie 100 % erreichen; 50 Rüstung halbiert den Schaden. Ein Treffer mit Schaden verursacht mindestens 1.
- **Rüstung startet bei 10 statt 0**, damit der Wert sofort spürbar ist. Dadurch wird der Spieler etwas robuster; über `--armor 0` lässt sich das alte Verhalten im Developer-Mode prüfen.
- **Reihenfolge:** erst Charakterwert, dann Resonance, dann Kern-Multiplikatoren. Die Werte sind die Grundlage, Zustände verstärken sie.
- **Der Audio-Test-Todestreffer** in `GameWorld` übergeht Rüstung, damit er weiterhin sicher tödlich ist.

## Risks / Trade-offs

- Rüstung 10 senkt den erlittenen Schaden in Prolog und Arena um rund 17 %. Falls das zu leicht wirkt, ist der Startwert ein einzelner Konstantenwert.
- Ohne Anzeige sind die Werte im Spiel unsichtbar; das ist bis zu einem Fortschrittssystem bewusst so.
