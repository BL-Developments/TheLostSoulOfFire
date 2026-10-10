## Why

Mit `add-level-rooms` und `add-room-wave-scaling` ist ein einzelnes Level spielbar. Die beschlossenen Regeln verlangen aber einen Biom-Run mit drei Leveln, Reisepunkten am Ende von Level 1 und 2, einem Boss am Ende von Level 3 und dem Neustart bei Level 1 nach einer Niederlage (#14, #52, #53, #66, #67, #68). Björn hat am 09.10.2026 entschieden, dass Tür I in diesen Run führt.

## What Changes

- **Biom-Run:**
  - Ein Biom ist eine Folge von drei Leveln. Jedes Level erzeugt seine Raumfolge beim Betreten neu, aus einem Seed des Runs.
  - Der Run hat klare Zustände: Homebase, Level läuft, Levelende, Extrahiert, Niederlage, Biom abgeschlossen.
  - Unerlaubte Übergänge bleiben wirkungslos.
- **Reisepunkt am Levelende von Level 1 und 2:**
  - Er hat dieselben drei Entscheidungen, dieselbe Quote und dieselbe Einmaligkeit wie der Reisepunkt der Arena.
  - Weiterreisen führt in den Startraum des nächsten Levels und behält Gesundheit, Run-Bestände, Fähigkeiten und den Raumfortschritt.
  - Extrahieren sichert alles und führt in den Hub.
- **Wächterraum am Ende von Level 3:**
  - Bis zum Boss (#92) ist das eine verstärkte Begegnung aus den vorhandenen Gegnern, eine Stufe schwerer als der letzte Kampfraum.
  - Ist sie geräumt, folgt der Biomabschluss: alles sichern, Abschluss zeigen, zurück in den Hub.
- **Tür I** startet den Run von Biom I. Die Arena bleibt unverändert über `--dev --start arena` spielbar.
- **Niederlage:** Sie führt wie in `add-level-rooms` in den Hub. Der nächste Run beginnt bei Level 1.
- **HUD:** Es zeigt `BIOM I · LEVEL <n> · RAUM <m>`.
- **Developer-Start:** `--dev --start biome:1` mit optionalem `--level <n>` und `--seed <n>`. Der Bereich `level` bleibt für ein einzelnes Level ohne Biom erhalten.

## Capabilities

### New Capabilities

- `biome-run-flow`: Run-Zustände eines Biom-Runs, drei Level, Reisepunkte am Levelende, Wächterraum, Biomabschluss und Neustart bei Level 1.

### Modified Capabilities

- `level-rooms`: Das vorläufige „Levelende führt in den Hub“ entfällt; im Biom-Run übernehmen Reisepunkt und Wächterraum.
- `hub-biome-doors`: Tür I führt in Biom I statt in die Arena.
- `developer-start-mode`: neuer Bereich `biome:1` mit `--level` und `--seed`.
- `run-currencies`: Der Start eines Biom-Runs beginnt einen Run; der Biomabschluss sichert den ganzen Run-Bestand.

## Impact

- **Neuer Code:** `BiomeRun` (reine Zustandslogik) und `BiomeDefinition` mit dem Eintrag für Biom I unter `Game/Levels/`; `GameWorld.BiomeRun.cs`.
- **Geänderte Stellen:** `GameWorld.Levels.cs` (Levelende je nach Level). `GameWorld.TravelPoint.cs` stellt den Reisepunkt auch im Levelende auf. `EnterArena` am Ende der Eintrittssequenz von Tür I führt in den Biom-Run, und das HUD zeigt die neue Zeile.
- **Nicht enthalten:** Boss und Soul-Release-Phase (#92), Freischaltung von Tür II (#93), Biomwahl (#62), Speichern des Run-Fortschritts (#64), Kisten in Räumen.
- **Abhängig von:** `add-level-rooms` und `add-room-wave-scaling`.
