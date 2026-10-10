## Context

- **Aus `add-level-rooms`:** Ein Level lässt sich spielen (`LevelRun`, `GameWorld.Levels.cs`). Sein Levelende führt vorläufig in den Hub.
- **Aus `add-room-wave-scaling`:** Begegnungen hängen am Fortschritt des Runs (`CombatRoomsEntered`).
- **Reisepunkt der Arena:** `TravelPoint`, Menü und Sicherungslogik gibt es aus `add-arena-travel-point` (#74).
- **Beschlüsse:**
  - drei Level je Biom, Boss am Ende von Level 3 (#52);
  - Reisepunkte am Ende von Level 1 und 2 (#53);
  - Niederlage führt in die Homebase, Neustart bei Level 1 (#14);
  - Tür I führt in Biom I (Björn, 09.10.2026).

## Goals / Non-Goals

**Goals:**
- `BiomeRun` als reine, getestete Zustandsmaschine für #66 und #67.
- Wiederverwendung von Reisepunkt und Sicherungslogik ohne zweite Variante.
- Ein durchgehender Run von Tür I bis zum Biomabschluss.

**Non-Goals:**
- Boss mit Soul-Release-Phase (#92).
- Dauerhafte Freischaltung von Tür II (#93).
- Biomwahl (#62).
- Speichern des Run-Fortschritts (#64).
- Eigene Grafik (`add-level-visual-slots`).

## Decisions

### `BiomeRun` hält Biom, Level, Seed und Zustand

`BiomeRun` hat folgende Methoden:
- `Start(biome, runSeed, level = 1)`;
- `ReachLevelEnd()`, `TravelOn()`, `Extract()`, `Defeat()`, `CompleteBiome()`, `ReturnHome()`.

Jede prüft den Ausgangszustand; ein unerlaubter Aufruf ändert nichts und liefert `false`.

Der Level-Seed ist `unchecked(runSeed * 7919 + level)`. Er kommt ohne `HashCode` aus, damit er über Programmstarts hinweg gleich bleibt.

`BiomeDefinition` beschreibt Biom I: Nummer, Name `I`, drei Level und Layout-Einstellungen je Level. Die Einstellungen sind vorerst gleich und können später je Level steigen.

### Levelende je nach Level

`GameWorld.Levels.cs` fragt beim Betreten des Levelendes `BiomeRun` ab:
- **Level 1 und 2:** Im Levelende-Raum entsteht ein `TravelPoint` an der Position, an der in `add-level-rooms` der Ausgang stand.
  - Teilsichern und Weiter rufen `TravelOn` auf und starten das nächste Level.
  - Extrahieren nutzt den bestehenden Extraktionsweg.
- **Level 3:** Der Levelende-Raum wird zum Wächterraum. Seine Begegnung ist `RoomEncounterPlan.For(progress + GameBalance.GuardianProgressBonus, seed)` mit einer zusätzlichen Welle (`GuardianExtraWaves`).
  - Nach der Räumung wird `CompleteBiome` aufgerufen, und `SecureRunCurrencies` zeigt den Biomabschluss.
  - Danach führt `ReturnHome` nach `GameBalance.BiomeCompleteDuration` in den Hub.

Der Bereich `level` (einzelnes Level ohne Biom) behält das vorläufige Levelende nicht; dort steht ebenfalls ein Reisepunkt, nach dem es in den Hub geht. Damit entfällt die Regel aus `add-level-rooms` überall.

### Tür I und Phasen

Die Eintrittssequenz von Tür I endet in `StartBiomeRun(seed: null, level: 1)` statt in `EnterArena`. `GamePhase.EnteringArena` behält seinen Namen, weil ein Level weiter in der Phase `Arena` läuft (siehe `add-level-rooms`); eine Umbenennung wäre nur Rauschen.

Die Arena wird nur noch über den Developer-Start (`DeveloperStartArea.Arena`) erreicht. Ihr Ablauf, auch ihr Reisepunkt nach Welle 5, bleibt unverändert.

### HUD

Die Zeile `RAUM <n>` aus `add-level-rooms` wird im Biom-Run zu `BIOM I · LEVEL <n> · RAUM <m>`. Die Breite wird mit `PixelText.Measure` ermittelt.

## Risks / Trade-offs

- **Der Hub führt nicht mehr in die gemalte Arena:** Der normale Ablauf sieht bis `add-level-visual-slots` wie die Arena aus und danach wie eine Graubox. Das ist bewusst so (Björn, 09.10.2026).
- **Wächterraum ohne Boss:** Er ist ein Platzhalter. #92 ersetzt die Begegnung, nicht den Ablauf.
- **Spec `travel-points` ist noch nicht archiviert:** `add-arena-travel-point` muss vor diesem Change archiviert werden. Die Anforderungen hier verweisen deshalb auf „wie der Reisepunkt der Arena“ statt ihn zu ändern.
