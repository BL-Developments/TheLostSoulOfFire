## 1. Spawner

- [x] 1.1 `SandboxEnemyKind` (Hollow, Burning, Devourer) und `SandboxSpawner` mit Bezeichnung, Radius, Typprüfung und Erzeugung.
- [x] 1.2 `SandboxSpawner.ChoosePosition`: 260 Pixel vom Spieler, Drehung um den goldenen Winkel pro Spawn, Klemmen in die Kampfgrenzen, Ausweichrichtungen bei weniger als 160 Pixel Abstand.
- [x] 1.3 Unit-Tests: Eintrag je Typ vor `ALLE GEGNER ENTFERNEN`, passender Typ und Radius, Positionen innerhalb der Grenzen und mit Abstand (Mitte, Ecken, Rand), keine Überlappung aufeinanderfolgender Spawns.

## 2. Dev-Menü

- [x] 2.1 Gegnereinträge aus `SandboxEnemyKind` in `SandboxDevMenuEntries` erzeugen, dazu `ALLE GEGNER ENTFERNEN`.
- [x] 2.2 `ActivateDevEntry` spawnt (mit kleiner Todesflamme) oder räumt Gegner und Seelen; `DevEntryValue` zeigt die Zahl lebender Gegner je Typ und gesamt.

## 3. Abnahme

- [x] 3.1 README um das Spawnen im Dev-Menü ergänzen.
- [x] 3.2 `openspec validate add-sandbox-enemy-spawner --strict` und `dotnet test` erfolgreich ausführen.
- [x] 3.3 Manuell anspielen mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start sandbox --armor 99` (`F`, Gegner jeden Typs spawnen, Zähler prüfen, Menü schließen, Kampf, `ALLE GEGNER ENTFERNEN`).
