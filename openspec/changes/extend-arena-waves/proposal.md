## Why

Die Arena endet nach vier kurzen Wellen, bei denen jeweils alle Gegner auf einmal erscheinen. Für einen Run, in dem Glut, Kisten und später Fähigkeiten spürbar werden sollen, ist das zu kurz und zu gleichförmig. Björn wünscht zehn Wellen, in denen spätere Wellen mehr Gegner bringen und diese über einen längeren Zeitraum nachrücken.

## What Changes

- Die Arena hat **zehn Wellen** statt vier. Das Finale beginnt direkt nach dem Leeren der zehnten Welle.
- Die Wellen 1 bis 4 bleiben in Zusammensetzung und Ablauf unverändert.
- Ab Welle 5 besteht eine Welle aus mehreren **Schüben**: Der erste erscheint beim Wellenstart, weitere rücken nach einer Wartezeit nach. Ein Schub kommt früher, wenn das Feld bis auf höchstens einen Gegner geleert ist, und wartet, solange schon die Höchstzahl gleichzeitiger Gegner lebt.
- Nachrückende Gegner erscheinen an festen Spawnpunkten am Arenarand, nie direkt neben dem Spieler, und kündigen sich vorher durch eine kurze Todesflammen-Markierung an.
- Eine Welle gilt erst als geleert, wenn alle ihre Schübe erschienen und alle Gegner und Seelen verschwunden sind.
- Gegnerzahl je Welle steigt moderat von 3 (Welle 1) auf 15 (Welle 10). Zusammensetzung, Wartezeit, Ankündigungsdauer, Höchstzahl gleichzeitiger Gegner und Mindestabstand zum Spieler liegen als einzelne Balance-Werte in `GameBalance`.
- Nach jeder Welle außer der zehnten pausiert die Arena wie bisher, und der Spieler startet die nächste Welle mit `E` in der Arenamitte.
- **Kisten erscheinen nach den Wellen 3, 6 und 9** statt nach den Wellen 1 bis 3. Es bleibt bei drei Kisten und 75 Geld pro Run, verteilt über den ganzen Run. Die bisherige Formulierung „nach jeder Welle außer der letzten“ wird entsprechend ersetzt.
- Die Wellenankündigung zeigt `WAVE I` bis `WAVE IX` und für Welle 10 `FINAL WAVE`; die Startaufforderung nennt die zehnte Welle `LETZTE WELLE`.
- Der Developer-Start nimmt `--wave 1` bis `--wave 10` an.

## Capabilities

### New Capabilities

Keine.

### Modified Capabilities

- `arena-showcase-flow`: zehn Wellen, Schübe mit Nachschub über die Zeit, Abschluss erst nach allen Schüben.
- `run-currencies`: Kisten nach den Wellen 3, 6 und 9; Abschluss und Sicherung nach der zehnten Welle.
- `developer-start-mode`: `--wave` gilt für 1 bis 10.

## Impact

Betroffen sind `GameWorld` (`SpawnWave`, `UpdateArenaLoop`, Debug-Taste `F6`, Debugtitel), `GameWorld.Currency` (`LastWave`, Kistenwellen, Startaufforderung), `ArenaChest.PositionForWave`, `GameBalance` (Wellentabelle und Nachschubwerte), `CinematicPresentation` (`ToRoman` bis X, `FINAL WAVE`), `DeveloperStartOptions` (`MaxWave`), die automatisierten Läufe in `Game1` (`--audio-gameplay-test` mit längerem Zeitlimit und zehn Wellen, `--currency-visual-test` mit den neuen Kistenwellen) sowie README und `docs/current/DECISION-LOG.md`. Gegnerverhalten, Glut je Gegner, Kistenbetrag und Arenagröße bleiben unverändert. Der Change baut auf `add-run-currencies` auf und setzt voraus, dass dieser vorher archiviert wird.
