## 1. Wellendaten und Nachschublogik

- [x] 1.1 `ArenaPush`/`ArenaWave` und die Tabelle `GameBalance.ArenaWaves` mit den zehn Wellen aus dem Design anlegen, dazu `ArenaWaveCount`, `ArenaPushInterval`, `ArenaPushEarlyAlive`, `ArenaMaxAliveEnemies`, `ArenaSpawnTelegraphDuration` und `ArenaSpawnMinPlayerDistance` als kommentierte Arbeitswerte.
- [x] 1.2 `ArenaWaveRun` ohne MonoGame-Abhängigkeit anlegen (nächster Schub, Zeit seit letztem Schub, Fälligkeit nach Wartezeit, Frühauslösung, Rückhalt bei Höchstzahl, alle Schübe ausgelöst, Verwerfen ausstehender Schübe).
- [x] 1.3 Unit-Tests: Wellen 1 bis 4 haben genau einen Schub mit der bisherigen Zusammensetzung, Gesamtzahl je Welle sinkt nie und ist in Welle 10 am höchsten, Fälligkeit nach Wartezeit, Frühauslösung bei höchstens einem lebenden Gegner, Rückhalt bei Höchstzahl, Welle erst nach allen Schüben geleert, Verwerfen.

## 2. Arena-Ablauf

- [x] 2.1 `SpawnWave` auf die Tabelle umstellen: Wellen 1 bis 4 mit den bisherigen Versätzen, ab Welle 5 erster Schub an Spawnpunkten.
- [x] 2.2 Acht Spawnpunkte am Rand von `CombatBounds`, Auswahl mit Mindestabstand zum Spieler und Bevorzugung entfernter Punkte, deterministischer Versatz; Unit-Test für die Auswahl.
- [x] 2.3 Nachschub in `UpdateArenaLoop`: `ArenaWaveRun` pro Frame aktualisieren, fällige Schübe als Ankündigungen anlegen, nach `ArenaSpawnTelegraphDuration` Gegner mit `EmitDeathFlame` und leiserem `WaveStart`-Cue erzeugen; Abschlussbedingung um „alle Schübe erschienen, keine Ankündigung offen“ erweitern.
- [x] 2.4 Ankündigungsmarkierung zeichnen (pulsierende Todesflammenringe wie die Mittelzone).
- [x] 2.5 Alle festen `4` durch `GameBalance.ArenaWaveCount` ersetzen (`LastWave`, Abschluss, `WaveClear`-Lautstärke, `FINAL WAVE`, Debugtitel `Wave n/10`); Burning-Obergrenze ab Welle 4 bleibt 2.
- [x] 2.6 `CinematicPresentation.ToRoman` bis X erweitern; Startaufforderung `E  LETZTE WELLE STARTEN` vor Welle 10.
- [x] 2.7 Kisten nach den Wellen 3, 6 und 9 über `GameBalance.ArenaChestWaves`, eigene Positionen in `ArenaChest.PositionForWave`; Pause mit manuellem Start nach den Wellen 1 bis 9; Unit-Test für die Kistenwellen.
- [x] 2.8 `F6` verwirft zusätzlich ausstehende Schübe und Ankündigungen der laufenden Welle.

## 3. Developer-Start, Tests und Doku

- [x] 3.1 `DeveloperStartOptions.MaxWave` auf `GameBalance.ArenaWaveCount`, Hilfetext und Tests für `--wave 10` (gültig) und `--wave 11` (Exitcode 2) anpassen.
- [x] 3.2 `--audio-gameplay-test` auf zehn Wellen umstellen (Zeitlimit erhöhen, Erfolgsmeldung `waves=10`); `--currency-visual-test` öffnet die Kisten nach den Wellen 3 und 6, lässt die nach Welle 9 zu und prüft zweimal `ChestGeld` im gesicherten Bestand.
- [x] 3.3 README-Tabelle (`--wave 1` bis `--wave 10`) und `docs/current/DECISION-LOG.md` (zehn Wellen, Schübe ab Welle 5, Kisten nach den Wellen 3, 6 und 9) nachziehen.

## 4. Gesamtabnahme

- [x] 4.1 `openspec validate extend-arena-waves --strict` und `dotnet test` erfolgreich ausführen.
- [x] 4.2 Manuell anspielen mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena --wave 5` (Nachschub mit Ankündigung am Rand, Frühauslösung beim schnellen Leeren, Kiste nach Welle 6, keine nach Welle 5) und `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena --wave 10` (`FINAL WAVE`, drei Schübe, Abschluss mit Sicherung).
