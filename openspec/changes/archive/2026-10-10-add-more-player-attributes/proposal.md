## Why

Der Charakter hat bisher drei Charakterwerte: Stärke, Fähigkeitsstärke und Rüstung. Björn möchte weitere Werte, mit denen sich der Charakter ausprägen lässt: einen Wert für die Angriffsgeschwindigkeit und einen Glückswert, der die Mengen von Geld und Glut beeinflusst. Dazu kommen die fünf Vorschläge aus dem Thread: Kernschärfe, Einklang, Gewandtheit, Fokus und Standfestigkeit. Sie knüpfen an Mechaniken an, die es schon gibt (Kerntreffer, Resonance, Ausweichen, Fähigkeiten, Rückstoß).

## What Changes

- Sieben neue Charakterwerte, ganzzahlig von 0 bis 99 mit Startwert 10, nach derselben Regel wie Stärke: ±5 % pro Punkt über oder unter 10. Beim Startwert bleibt jedes heutige Verhalten gleich.
  - `Tempo`: Sensenschläge laufen schneller ab, die Seelenkanone lädt schneller voll.
  - `Glück`: Glut aus besiegten Gegnern und Geld aus Kisten werden mit dem Glücksfaktor multipliziert. Der Bruchteil wird mit seiner Wahrscheinlichkeit zu einer weiteren Einheit (3,6 Glut ergeben 3, mit 60 % Chance 4). Der Glut-Basisvorrat bleibt unverändert.
  - `Kernschärfe`: Kerntreffer von Sense und Seelenkanone verursachen mehr Schaden.
  - `Einklang`: Jeder Resonance-Zuwachs (Seelenerlösung, Kerntreffer) steigt.
  - `Gewandtheit`: Die Abklingzeit des Ausweichens läuft schneller ab (±5 % pro Punkt), die Laufgeschwindigkeit steigt um ±1 % pro Punkt.
  - `Fokus`: Abklingzeiten der Run-Fähigkeiten laufen schneller ab.
  - `Standfestigkeit`: Rückstoß aus erlittenen Treffern wird durch den Faktor geteilt; er erreicht nie null.
- Die Charakterseite zeigt alle zehn Werte in zwei Spalten mit je einer Wirkungszeile, die Währungen in einer Zeile darunter.
- Der Developer-Mode erhält `--attack-speed`, `--luck`, `--core-sharpness`, `--attunement`, `--agility`, `--focus` und `--steadiness`. Die `DEV_START`-Zeile nennt neue Werte nur, wenn sie vom Startwert abweichen.
- Das Dev-Menü der Sandbox kann alle neuen Werte setzen; `ZURÜCKSETZEN` stellt auch sie wieder her. Damit alle Einträge auf den Bildschirm passen, rücken die Zeilen des Dev-Menüs enger zusammen.

## Capabilities

### New Capabilities

Keine.

### Modified Capabilities

- `player-attributes`: sieben weitere Charakterwerte und ihre Wirkung.
- `character-menu`: Die Charakterseite zeigt alle Werte.
- `sandbox-mode`: Das Dev-Menü setzt alle Werte und setzt sie zurück.
- `developer-start-mode`: Startparameter für die neuen Werte.

## Impact

Geändert werden `Combat/PlayerAttributes` (neue Werte, Faktoren, `ScaleCoreDamage`, `ScaleReward`), `ScytheCombat` und `SoulCannon` (Tempo), `Player` (Einklang, Gewandtheit, Standfestigkeit), `RunAbilities` (Fokus), `GameWorld` (Kernschärfe bei Kerntreffern), `GameWorld.Currency` (Glück), `CharacterSheet` und `CinematicPresentation` (Charakterseite), `SandboxDevMenuEntries`, `GameWorld.Sandbox` und `DevMenuRenderer` (Dev-Menü), `DeveloperStartOptions` und die README. Es gibt weiterhin keinen Weg, die Werte im normalen Spiel zu steigern; das folgt mit dem Waffen- und Fähigkeitsbaum. Keine neuen Assets, Abhängigkeiten oder Speicherdaten.
