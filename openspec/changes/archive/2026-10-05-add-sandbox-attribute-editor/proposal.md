## Why

Laut #104 soll man in der Sandbox die Charakterwerte von Hand setzen können, um ihre Wirkung im Kampf direkt auszuprobieren, ohne das Spiel mit neuen Startparametern neu zu starten. Das Dev-Menü (`add-sandbox-dev-menu`) hat dafür den Abschnitt `CHARAKTER`, der bisher leer ist.

## What Changes

- Der Abschnitt `CHARAKTER` des Dev-Menüs bekommt die Werteinträge `LEBEN`, `STÄRKE`, `FÄHIGKEITSSTÄRKE` und `RÜSTUNG` sowie die Aktion `ZURÜCKSETZEN`.
- `A`/`D` (oder Klick) ändern Stärke, Fähigkeitsstärke und Rüstung um 1, mit Umschalttaste um 10, im Bereich 0 bis 99. Die Werte wirken sofort auf Schaden und Schadensverringerung.
- `LEBEN` ist das maximale Leben: Schritte von 10, mit Umschalttaste 100, im Bereich 1 bis 999. Jede Änderung setzt das aktuelle Leben auf das neue Maximum. HUD, Charaktermenü und Debug-Overlay zeigen das geänderte Maximum.
- `ZURÜCKSETZEN` stellt die Werte vom Sandbox-Start wieder her: Leben 100 und die Charakterwerte aus `--strength`, `--ability-power` und `--armor` oder deren Startwert 10.
- Ein Neustart in der Sandbox (`R`, `F8`) behält die gesetzten Werte. Verlässt der Spieler die Sandbox über das Pausenmenü, gelten wieder die Startwerte.

## Capabilities

### New Capabilities

Keine.

### Modified Capabilities

- `sandbox-mode`: Charakterwerte im Dev-Menü setzen und zurücksetzen.

## Impact

`Player` bekommt ein änderbares Maximalleben (`MaxHealth`, `SetMaxHealth`) statt überall `GameBalance.PlayerMaxHealth` zu lesen; außerhalb der Sandbox bleibt es bei 100. Neu sind `Menu/DevValueRange` (Grenzen und Schritte) und die Einträge in `SandboxDevMenuEntries`. `HudRenderer`, Charakterseite und Debug-Overlay lesen das Maximalleben vom Spieler. Keine neuen Assets, Abhängigkeiten oder Speicherdaten; die Regeln in `PlayerAttributes` bleiben unverändert.
