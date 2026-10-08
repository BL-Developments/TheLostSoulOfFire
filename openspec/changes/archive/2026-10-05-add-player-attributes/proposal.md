## Why

Der Spielercharakter hat bisher nur feste Kampfwerte: Leben, Tempo und die Schadenszahlen von Sense und Seelenkanone stehen in `GameBalance`. Es gibt keine Charakterwerte, über die sich der Charakter später entwickeln lässt. Björn möchte Stärke, Fähigkeitsstärke und Rüstung: Die Waffe skaliert mit Stärke, die Fähigkeiten skalieren mit Fähigkeitsstärke, und Rüstung verringert erlittenen Schaden.

## What Changes

- Neue Charakterwerte des Spielers: `Stärke`, `Fähigkeitsstärke` und `Rüstung`, jeweils ganzzahlig von 0 bis 99. Startwert ist für alle drei 10.
- Stärke skaliert den Schaden aller drei Sensenschläge: ±5 % pro Punkt über oder unter 10. Bei 10 bleiben 20 / 25 / 40.
- Fähigkeitsstärke skaliert den Schaden der Seelenkanone nach derselben Regel. Bei 10 bleiben 24 bis 68.
- Rüstung verringert jeden erlittenen Treffer um `Rüstung / (Rüstung + 50)`, gerundet, mindestens 1 Schaden. Mit dem Startwert 10 sinkt erlittener Schaden um rund 17 % (Hollow 16 → 13, Burning-Ansturm 20 → 17, Devourer-Schlag 24 → 20).
- Resonance, Soul-Sense-Kerntreffer und Kanonen-Kernbonus wirken weiterhin als Multiplikatoren auf den skalierten Schaden.
- Der Developer-Mode erhält `--strength`, `--ability-power` und `--armor`, um die Werte beim Start zu setzen.

## Capabilities

### New Capabilities

- `player-attributes`: Charakterwerte des Spielers und ihre Wirkung auf ausgeteilten und erlittenen Schaden.

### Modified Capabilities

- `developer-start-mode`: Zusätzliche Startparameter für die Charakterwerte.

## Impact

Neu ist `Combat/PlayerAttributes`. Geändert werden `ScytheCombat` und `SoulCannon` (Schaden aus den Werten), `Player` (Werte halten, Rüstung beim Treffer anwenden), `GameWorld` (Audio-Test-Todestreffer ignoriert Rüstung), `DeveloperStartOptions`, `GameWorld.DeveloperStart` und die README. Es gibt noch keine Anzeige und keinen Weg, die Werte im Spiel zu erhöhen; beides folgt mit Fortschrittssystemen. Keine neuen Assets, Abhängigkeiten oder Speicherdaten.
