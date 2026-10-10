## Why

Björn möchte, dass die Räume eines Levels zufällig erzeugt werden (09.10.2026): mehr oder weniger linear, mit gelegentlich parallelen Wegen zum Levelende. Das ist die Grundlage aller weiteren Level-Changes und lässt sich als reine Logik ohne Spielanbindung bauen und testen.

## What Changes

- Ein Generator erzeugt aus einem Seed die Raumfolge eines Levels:
  - Sie besteht aus Stufen. Jede Stufe hat einen oder zwei Räume.
  - Jeder Raum führt zu allen Räumen der nächsten Stufe, also zu höchstens zwei Ausgängen.
  - Die erste Stufe ist ein Startraum, die letzte das Levelende. Dazwischen liegen Kampfräume.
- Gleicher Seed und gleiche Einstellungen ergeben immer dieselbe Raumfolge. Jeder neue Run würfelt einen neuen Seed.
- Jeder Raum kennt seinen Fortschritt, nämlich seine Stufe. Daran hängt später die Skalierung der Wellen.
- Anzahl der Kampfstufen und Wahrscheinlichkeit einer Gabelung sind Balance-Werte in `GameBalance`.
- Noch nichts davon ist im Spiel sichtbar.

## Capabilities

### New Capabilities

- `level-room-generation`: zufällige, reproduzierbare Raumfolge eines Levels mit Stufen, Gabelungen und Fortschritt je Raum.

### Modified Capabilities

Keine.

## Impact

- Neuer Code unter `src/TheLostSoulOfFire/Game/Levels/` (`LevelLayout`, `LevelRoom`, `LevelLayoutGenerator`) und Werte in `GameBalance`.
- Tests unter `tests/TheLostSoulOfFire.Tests/`.
- `docs/current/DECISION-LOG.md`: Eintrag zur zufälligen Raumfolge mit einzelnen Räumen (Björn, 09.10.2026).
- Grundlage für `add-level-rooms`, `add-room-wave-scaling` und `add-biome-run-flow`.
