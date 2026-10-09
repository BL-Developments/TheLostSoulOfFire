## Context

Björn hat am 09.10.2026 entschieden:
- Die Räume eines Levels werden zufällig erzeugt, mehr oder weniger linear und mit möglichen parallelen Wegen.
- Man betritt die Räume einzeln wie bei Hades; ein Weg ist die Wahl zwischen den Ausgängen eines geräumten Raums.

`WORLD-GRAMMAR.md` §7 erlaubt prozedurale Kombination von Räumen innerhalb einer handgebauten Grammatik. Verbindungen und Fortschritt bleiben regelbasiert und deterministisch.

## Goals / Non-Goals

**Goals:**
- Eine kleine, reine Datenstruktur für die Raumfolge, die `add-level-rooms` direkt abspielen kann.
- Reproduzierbarkeit über einen Seed, damit Tests, Fehlerberichte und `--seed` im Developer-Start (folgt in `add-level-rooms`) dieselbe Raumfolge liefern.

**Non-Goals:**
- Spielanbindung, Zeichnen, Begegnungen (`add-level-rooms`, `add-room-wave-scaling`).
- Raumtypen außer Start, Kampf und Levelende; Belohnungs-, Elite- oder Wächterräume folgen mit eigenen Changes.
- Räumliche Anordnung auf einer Karte (einzelne Räume, keine Karte).

## Decisions

### Stufenmodell statt freiem Graphen

`LevelLayout` besteht aus Stufen (`IReadOnlyList<IReadOnlyList<LevelRoom>>`). `LevelRoom` hat `Id`, `Kind` (Start, Combat, LevelEnd), `Progress` (Stufenindex: Start 0, erste Kampfstufe 1) und `Exits` (Ids der nächsten Stufe, links vor rechts).
- Jeder Raum verbindet zu allen Räumen der nächsten Stufe. Bei höchstens zwei Räumen je Stufe gibt es damit höchstens zwei Ausgänge.
- Gabeln und Zusammenführen ergeben sich von selbst, Sackgassen und unerreichbare Räume sind ausgeschlossen.

*Alternative zufälliger Graph mit Prüfung:* Das bräuchte Reparatur- oder Wiederholungslogik für Sackgassen und ist schwerer zu lesen. Das Stufenmodell ist eindeutig „mehr oder weniger linear“.

### Zufall aus eigenem, festem Generator

`LevelLayoutGenerator.Generate(int seed, LevelLayoutSettings settings)` nutzt `System.Random(seed)` nur lokal und in fester Reihenfolge: erst die Anzahl der Stufen, dann je Stufe die Gabelung.

Die Einstellungen kommen aus `GameBalance`:
- `LevelCombatStagesMin`, `LevelCombatStagesMax` (Arbeitswerte 4 und 6);
- `LevelForkChance` (Arbeitswert 0,35).

Zwei Gabelungen nacheinander sind erlaubt.

Den Seed eines Runs würfelt erst der aufrufende Code (`add-level-rooms`), etwa aus `Environment.TickCount`. Der Generator selbst bleibt deterministisch.

## Risks / Trade-offs

- **`System.Random(seed)` ist nur innerhalb einer .NET-Version garantiert gleich:** Für Tests und Fehlerberichte reicht das. Gespeicherte Seeds über Versionen hinweg sind kein Ziel, weil Run-Fortschritt nicht gespeichert wird.
- **Parallele Wege unterscheiden sich vorerst nur im Seed ihrer Begegnung:** Unterschiedliche Raumtypen als echte Wahl kommen später.
