## Why

In der Sandbox sollen sich laut #104 alle Gegnertypen gezielt spawnen lassen, statt sie nur über die Debug-Tasten `F2` bis `F4` direkt neben den Spieler zu setzen. Der Abschnitt `GEGNER` des Dev-Menüs (`add-sandbox-dev-menu`) ist dafür vorgesehen und bisher leer.

## What Changes

- Der Abschnitt `GEGNER` bekommt je eine Aktion für `HOLLOW`, `BURNING` und `DEVOURER` und darunter `ALLE GEGNER ENTFERNEN`.
- Eine Spawn-Aktion setzt einen Gegner des Typs in Sichtweite des Spielers, mit Abstand zu ihm und innerhalb der Arena-Grenzen. Aufeinanderfolgende Spawns verteilen sich rund um den Spieler. Das Menü bleibt offen, damit mehrere Gegner nacheinander gesetzt werden können; sie greifen an, sobald das Menü geschlossen ist.
- Jede Spawn-Zeile zeigt, wie viele Gegner dieses Typs gerade leben; `ALLE GEGNER ENTFERNEN` zeigt die Gesamtzahl.
- `ALLE GEGNER ENTFERNEN` nimmt alle Gegner und verlorenen Seelen vom Feld, ohne Glut, Seelen oder Todeseffekte.
- Die Gegnereinträge entstehen aus einer zentralen Liste der spawnbaren Typen, damit künftige Gegner dort nur ergänzt werden müssen.

## Capabilities

### New Capabilities

Keine.

### Modified Capabilities

- `sandbox-mode`: Gegner im Dev-Menü spawnen und entfernen.

## Impact

Neu ist `Game/SandboxSpawner` mit `SandboxEnemyKind` (zentrale Typliste), Erzeugung, Bezeichnung und Spawnposition. `SandboxDevMenuEntries` erzeugt daraus die Gegnereinträge; `GameWorld` führt Spawn und Entfernen aus. Keine neuen Assets, Abhängigkeiten oder Speicherdaten; Gegnerverhalten bleibt unverändert.
