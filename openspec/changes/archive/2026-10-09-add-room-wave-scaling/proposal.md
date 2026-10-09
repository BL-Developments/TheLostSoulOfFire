## Why

Mit `add-level-rooms` hat jeder Kampfraum dieselbe Platzhalter-Welle. Björn möchte, dass die Wellen mit dem Raumfortschritt wachsen: mehr Wellen je Raum und mehr schwere Gegner, je weiter der Spieler kommt (09.10.2026).

## What Changes

- Die Begegnung eines Kampfraums wird aus seinem Fortschritt berechnet und ersetzt die Platzhalter-Welle.
- **Fortschritt:** Er zählt die Kampfräume, die der Spieler im laufenden Run schon betreten hat, einschließlich des aktuellen. Er läuft über Levelgrenzen weiter, sobald es mehrere Level gibt.
- **Mit dem Fortschritt steigen:**
  - die Anzahl der Wellen je Raum,
  - die Gegner je Welle,
  - die Anzahl schwerer Gegner (Burning, später Devourer).

  Keiner dieser Werte sinkt von einem Raum zum nächsten.
- **Wellen im Raum:** Eine Welle startet nach einer kurzen Pause, sobald die vorherige geräumt ist. Ankündigung, Mindestabstand und Höchstzahl gleichzeitiger Gegner gelten wie in der Arena.
- **Zufall:** Die genaue Mischung und die Reihenfolge der Gegner würfelt der Seed des Raums. Gleicher Seed und gleicher Fortschritt ergeben dieselbe Begegnung.
- **Balance:** Alle Stellschrauben stehen in `GameBalance`. Ein Test schreibt die Tabelle für die Fortschritte 1 bis 18 als Übersicht.

## Capabilities

### New Capabilities

- `room-encounter-scaling`: Zusammensetzung der Begegnung eines Kampfraums aus Fortschritt und Seed.

### Modified Capabilities

Keine. `level-rooms` verlangt nur, dass ein Kampfraum eine Begegnung hat; die Platzhalter-Welle steht im Design von `add-level-rooms`, nicht im Spec.

## Impact

- **Neuer Code:** `RoomEncounterPlan` (reine Berechnung) und `RoomEncounter` (Wellenfolge mit Pausen auf Basis von `ArenaWaveRun`) unter `Game/Levels/`.
- **Anbindung:** in `GameWorld.Levels.cs`. `LevelRun` zählt den Fortschritt im Run.
- **Werte:** in `GameBalance`, und `LevelPlaceholderPush` entfällt.
- **Abhängig von:** `add-level-rooms`.
