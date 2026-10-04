## Context

Die Arena (`GameWorld.SpawnWave`) kennt vier fest verdrahtete Wellen. Jede Welle legt alle Gegner beim Start auf feste Versätze zur Arenamitte; die Welle ist geleert, wenn `_enemies` und `_souls` leer sind. Nach den Wellen 1 bis 3 folgt seit `add-run-currencies` die Pause `ArenaLoopState.Intermission` mit Kiste und manuellem Start per `E` in der Arenamitte; nach Welle 4 beginnt direkt der Abschluss, der beide Run-Bestände sichert. Die Zahl 4 steckt an mehreren Stellen: `GameWorld.Currency.LastWave`, `waveNumber >= 4` in `UpdateArenaLoop` und `ConfigureBurningAggression`, `nextWave >= 4` für `FINAL WAVE`, `ToRoman` (nur bis IV), `DeveloperStartOptions.MaxWave`, der Debugtitel `Wave n/4` und der `--audio-gameplay-test` (35 s Zeitlimit, `waves=4` in der Erfolgsmeldung).

Björn möchte zehn Wellen; spätere Wellen sollen mehr Gegner bringen und sie über einen längeren Zeitraum erscheinen lassen.

## Goals / Non-Goals

**Goals:**

- Zehn spielbare Wellen mit moderat steigender Gegnerzahl; Wellen 1 bis 4 fühlen sich an wie heute.
- Gestaffelter Nachschub ab Welle 5, der Druck über Zeit aufbaut, ohne den Spieler zu überrollen oder unfair hinter ihm zu spawnen.
- Alle Stellschrauben (Zusammensetzung, Wartezeit, Ankündigung, Obergrenze, Abstand) als benannte Werte in `GameBalance`.
- Die Wellenlogik (wann kommt welcher Schub, wann ist die Welle geleert) ohne MonoGame testbar.

**Non-Goals:**

- Neue Gegnertypen, stärkere Gegnerwerte je Welle oder Elite-Varianten.
- Änderungen an Glut je Gegner, Kistenbetrag, Kistenzahl, Sicherungsregel oder HUD.
- Eine Wellenanzeige im HUD (die Ankündigung und die Startaufforderung nennen die Welle bereits).
- Endlosmodus oder zufällige Wellen.

## Decisions

**Wellentabelle in `GameBalance`.** Eine Welle ist eine Liste von Schüben, ein Schub zählt Hollow, Burning und Devourer. Die Tabelle steht als `GameBalance.ArenaWaves` (z. B. `ArenaWave[]` mit `ArenaPush(int Hollow, int Burning, int Devourer)`), damit Balance an einer Stelle bleibt. `GameBalance.ArenaWaveCount` leitet sich aus der Tabelle ab und ersetzt alle verstreuten `4`.

| Welle | Schub 1 (Start) | Schub 2 | Schub 3 | Gesamt |
|---|---|---|---|---|
| 1 | 3 H | – | – | 3 |
| 2 | 2 H, 2 B | – | – | 4 |
| 3 | 2 H, 2 B, 1 D | – | – | 5 |
| 4 | 2 H, 3 B, 1 D | – | – | 6 |
| 5 | 3 H, 2 B | 2 H, 1 B | – | 8 |
| 6 | 3 H, 2 B | 2 H, 1 B, 1 D | – | 9 |
| 7 | 2 H, 2 B, 1 D | 3 H, 2 B | – | 10 |
| 8 | 3 H, 2 B | 2 H, 1 B, 1 D | 2 H, 1 B | 12 |
| 9 | 3 H, 2 B, 1 D | 2 H, 2 B | 2 H, 1 B, 1 D | 14 |
| 10 | 2 H, 2 B, 1 D | 3 H, 2 B | 2 H, 1 B, 2 D | 15 |

H = Hollow, B = Burning, D = Devourer. Alternative: Gegnerzahl per Formel aus der Wellennummer. Verworfen, weil die Zusammensetzung (wann der erste und der zweite Devourer kommt) eine Designentscheidung ist und eine Tabelle leichter zu lesen und nachzujustieren ist.

**Wellen 1 bis 4 bleiben unverändert.** Ihre bisherigen festen Versätze bleiben als Positionen des ersten Schubs erhalten, damit sich der bekannte Einstieg nicht verschiebt. Ab Welle 5 nutzen alle Schübe die Spawnpunkte.

**Nachschub-Regel.** Ein `ArenaWaveRun` (reine Klasse, kein MonoGame) führt den Zustand einer laufenden Welle: Index des nächsten Schubs und Zeit seit dem letzten Schub. Pro Frame bekommt er `deltaTime` und die Zahl lebender Gegner und meldet, ob der nächste Schub jetzt fällig ist:

- fällig, wenn seit dem letzten Schub `GameBalance.ArenaPushInterval` (10 s) vergangen ist **oder** höchstens `GameBalance.ArenaPushEarlyAlive` (1) Gegner leben;
- zurückgehalten, solange `GameBalance.ArenaMaxAliveEnemies` (9) oder mehr Gegner leben (Gegner in Ankündigung zählen mit).

Die Welle ist geleert, wenn alle Schübe ausgelöst, alle angekündigten Gegner erschienen und `_enemies` sowie `_souls` leer sind. Alternative: feste Zeitpunkte ohne Frühauslösung. Verworfen, weil schnelle Spieler sonst in einer leeren Arena warten.

**Spawnpunkte und Ankündigung.** Acht feste Spawnpunkte am Rand von `CombatBounds` (Ecken und Seitenmitten, leicht nach innen versetzt). Für jeden Gegner eines Schubs wählt `GameWorld` die Punkte mit mindestens `GameBalance.ArenaSpawnMinPlayerDistance` (380 px) Abstand zum Spieler, bevorzugt die am weitesten entfernten, und verteilt die Gegner reihum mit kleinem deterministischem Versatz, damit sie nicht aufeinanderliegen. An jedem gewählten Punkt erscheint zuerst eine pulsierende Todesflammen-Markierung (gleiche Formsprache wie die Mittelzone) für `GameBalance.ArenaSpawnTelegraphDuration` (0,9 s); erst danach entsteht der Gegner mit einem kleinen `EmitDeathFlame`. Der erste Schub ab Welle 5 nutzt dieselben Punkte, aber ohne zusätzliche Ankündigung, weil die Wellenankündigung vorausgeht. Ein Nachschub spielt einen leiseren `WaveStart`-Cue, damit er hörbar, aber nicht mit einem Wellenstart verwechselbar ist.

**Burning-Aggression.** Die Obergrenze gleichzeitig angreifender Burning bleibt ab Welle 4 bei 2. Mehr Burning in späten Wellen erhöhen den Druck über die Gesamtzahl, nicht über gleichzeitige Charges.

**Pause und Kisten.** Die Pause mit manuellem Start gilt weiter nach jeder Welle außer der letzten, also nach den Wellen 1 bis 9. Kisten erscheinen nach den Wellen 3, 6 und 9 (Björn, 04.10.), festgelegt als `GameBalance.ArenaChestWaves`. Es bleibt bei drei Kisten mit zusammen 75 Geld, sie verteilen sich aber über den ganzen Run, und das Risiko, mit ungesichertem Geld in späte Wellen zu gehen, wird spürbar. `ArenaChest.PositionForWave` bekommt für die Wellen 3, 6 und 9 je eine eigene feste Position außerhalb der Mittelzone. Nach Welle 10 folgt direkt der Abschluss mit Sicherung.

**Ankündigung und Texte.** `ToRoman` reicht bis X. Die Ankündigung zeigt `FINAL WAVE`, wenn die nächste Welle `ArenaWaveCount` ist, sonst `WAVE <römisch>`. Die Startaufforderung lautet für die letzte Welle `E  LETZTE WELLE STARTEN`. Der Debugtitel zeigt `Wave n/10`.

**Debug und automatisierte Läufe.** `F6` besiegt wie bisher alle lebenden Gegner und verwirft zusätzlich ausstehende Schübe und Ankündigungen der laufenden Welle, damit automatisierte Läufe eine Welle mit einem Tastendruck leeren können; verworfene Gegner bringen keine Glut. `--audio-gameplay-test` läuft durch alle zehn Wellen, das Zeitlimit wächst entsprechend, und die Erfolgsmeldung nennt `waves=10`. `--currency-visual-test` öffnet die Kisten nach den Wellen 3 und 6, lässt die nach Welle 9 ungeöffnet und prüft weiter zweimal `ChestGeld` im gesicherten Bestand.

**Developer-Start.** `DeveloperStartOptions.MaxWave` wird `GameBalance.ArenaWaveCount` (10). Hilfetext und README nennen `--wave 1` bis `--wave 10`.

## Risks / Trade-offs

- [Zehn Wellen ohne Fortschritt im Charakter könnten zäh werden] → Werte sind moderat und alle in `GameBalance`; nach dem ersten Anspielen nachjustieren. Skilltrees und Fähigkeiten kommen in eigenen Changes.
- [Nachschub hinter dem Spieler wirkt unfair] → Mindestabstand, Bevorzugung entfernter Punkte und sichtbare Ankündigung vor dem Erscheinen.
- [Zu viele Gegner gleichzeitig kosten Lesbarkeit und Leistung] → Obergrenze `ArenaMaxAliveEnemies`; Schübe warten, statt sich zu stapeln.
- [Devourer fressen in langen Wellen mehr Seelen und werden zu stark] → Höchstens zwei Devourer je Welle, der zweite erst im letzten Schub der zehnten Welle; die bestehende Stapelgrenze `DevourerMaxSoulStacks` gilt weiter.
- [Archiv-Reihenfolge] → Die Deltas für `arena-showcase-flow` und `run-currencies` setzen den Stand von `add-run-currencies` voraus. Dieser Change wird erst nach dessen Archivierung archiviert.

## Open Questions

Keine.
