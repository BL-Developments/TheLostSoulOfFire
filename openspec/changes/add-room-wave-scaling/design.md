## Context

- **Grundlage aus `add-level-rooms`:** Ein Kampfraum spielt eine Platzhalter-Welle über `ArenaWaveRun`. `LevelRun` kennt Layout, Seed und aktuellen Raum.
- **Arena als Vorbild:** Sie skaliert über eine feste Tabelle in `GameBalance.ArenaWaves` (zehn Wellen aus Schüben, Höchstzahl gleichzeitiger Gegner, Mindestabstand).
- **Wunsch Björn:** Wellenmenge und Anzahl schwerer Gegner wachsen mit dem Raumfortschritt.

## Goals / Non-Goals

**Goals:**
- Eine reine, testbare Berechnung `RoomEncounterPlan.For(progress, seed)`.
- Wenige, verständliche Stellschrauben statt einer großen Tabelle, weil die Anzahl der Räume zufällig ist.

**Non-Goals:**
- Elite- und Wächterräume (`add-biome-run-flow`).
- Unterschiedliche Schwierigkeit paralleler Wege.
- Neue Gegnertypen.
- Feinbalance. Die Werte sind Arbeitswerte zum Anspielen.

## Decisions

### Formel mit Obergrenzen

Für Fortschritt `p` (ab 1) gilt:
- `waves = min(RoomWavesMax, 1 + (p - 1) / RoomStagesPerExtraWave)`.
- `perWave = min(RoomEnemiesPerWaveMax, RoomEnemiesPerWaveBase + (p - 1) * RoomEnemiesPerWaveGrowth)`. Gerechnet wird mit `float` und abgerundet.
- `heavy = p < RoomBurningFromProgress ? 0 : min(perWave * waves - waves, (p - RoomBurningFromProgress + 1) * RoomHeavyPerProgress)`. So hat jede Welle mindestens einen Hollow.
- Unter den schweren Gegnern sind Devourer ab `RoomDevourerFromProgress`. Ihr Anteil ist `RoomDevourerShare`, abgerundet und mindestens 1.

Alle Größen steigen schwach monoton, sinken also nie. Ein Test prüft das für `p` von 1 bis 40.

Arbeitswerte:

| Wert | Arbeitswert |
|---|---|
| `RoomWavesMax` | 4 |
| `RoomStagesPerExtraWave` | 3 |
| `RoomEnemiesPerWaveBase` | 3 |
| `RoomEnemiesPerWaveGrowth` | 0,5 |
| `RoomEnemiesPerWaveMax` | 8 |
| `RoomBurningFromProgress` | 2 |
| `RoomHeavyPerProgress` | 1 |
| `RoomDevourerFromProgress` | 4 |
| `RoomDevourerShare` | 0,25 |
| `RoomWavePause` | 1,2 s |

`ArenaMaxAliveEnemies` begrenzt weiterhin, wie viele Gegner gleichzeitig stehen. Eine Welle mit acht Gegnern erscheint deshalb in Schüben.

*Alternative Tabelle wie in der Arena:* Sie passt nicht zu einer zufälligen Anzahl von Räumen und mehreren Leveln. Die Formel deckt jeden Fortschritt ab.

### Der Seed verteilt, die Anzahlen bleiben fest

`RoomEncounterPlan` erzeugt je Welle eine `ArenaPush`-Liste. Die Anzahlen je Typ hängen nur von `p` ab.

Der Raum-Seed ergibt sich aus Level-Seed und Raum-Id. Er verteilt die schweren Gegner mit `System.Random` auf die Wellen; jede Welle erhält mindestens einen Hollow.

So fühlen sich parallele Räume gleich schwer an und spielen sich trotzdem verschieden.

### Wellenfolge im Raum

`RoomEncounter` hält den Plan, den Index der aktuellen Welle und einen `ArenaWaveRun` je Welle.
- Ist eine Welle geräumt, startet nach `RoomWavePause` die nächste.
- Nach der letzten Welle meldet `RoomEncounter` „geräumt“, und `LevelRun.MarkCleared` wird aufgerufen.

### Fortschritt im Run

`LevelRun` zählt `CombatRoomsEntered` und gibt den Wert beim Betreten eines Kampfraums an `RoomEncounterPlan` weiter.

`add-biome-run-flow` übergibt den Zähler beim Levelwechsel. Damit läuft der Fortschritt über alle drei Level weiter.

## Risks / Trade-offs

- **Die Formel kann sich flach oder zu steil anfühlen:** Die Werte sind ausdrücklich Arbeitswerte. Ein Test schreibt die Übersicht für Fortschritt 1 bis 18 in die Testausgabe, und Balance läuft später in #95.
- **Lange Räume:** Bei vier Wellen mit acht Gegnern dauert ein Raum lange. Die Obergrenzen lassen sich ohne Codeänderung senken.
