## Context

`Player.Attributes` (`PlayerAttributes`: Stärke, Fähigkeitsstärke, Rüstung, je 0 bis 99, Start 10) ist schon setzbar; Schaden und Schadensverringerung werden bei jedem Treffer daraus berechnet. Das Leben dagegen hängt fest an `GameBalance.PlayerMaxHealth` (100): `Player.Reset`, HUD, Charakterseite und Debug-Overlay lesen die Konstante. Das Dev-Menü ruft für Werteinträge `AdjustDevEntry`, für Aktionen `ActivateDevEntry` und für die Anzeige `DevEntryValue` auf.

## Goals / Non-Goals

**Goals:**

- Leben, Stärke, Fähigkeitsstärke und Rüstung in der Sandbox ändern, mit sofortiger Wirkung.
- Nichts davon wirkt außerhalb der Sandbox weiter.

**Non-Goals:**

- Freie Zahleneingabe, Speichern von Werten, weitere Werte (kommen dazu, sobald es sie gibt).

## Decisions

**Maximalleben am Spieler.** `Player` erhält `MaxHealth` (Start `GameBalance.PlayerMaxHealth`) und `SetMaxHealth`, das das Maximum setzt und den Spieler auffüllt. `Reset` füllt auf `MaxHealth`. HUD, Charakterseite und Debug-Overlay lesen `player.MaxHealth`. Ein Wert, der das aktuelle Leben stehen lässt, wäre beim Testen verwirrend (z. B. 100/500); deshalb füllt jede Änderung auf.

**Grenzen und Schritte als kleiner Wertetyp.** `DevValueRange(Min, Max, Step, LargeStep)` mit `Adjust(value, direction, largeStep)` ist ohne Spiel testbar. Charakterwerte nutzen die Grenzen aus `PlayerAttributes` mit Schritten 1/10, Leben 1 bis 999 mit Schritten 10/100. 999 hält die HUD-Zahl dreistellig.

**Startwerte.** Beim Betreten der Sandbox merkt sich `GameWorld` die Charakterwerte (nach den Dev-Flags). `ZURÜCKSETZEN` und das Verlassen der Sandbox stellen sie und Leben 100 wieder her. `ClearRunState` bekommt dafür den Parameter `stayInSandbox`, den nur der Neustart in der Sandbox setzt; jeder andere Aufruf verlässt die Sandbox und stellt die Startwerte her.

**Kein Öffnen nach der Niederlage.** Das Dev-Menü öffnet nur, solange der Spieler lebt (in `add-sandbox-dev-menu` festgelegt). Sonst könnte `LEBEN` einen besiegten Spieler mitten im Todesbildschirm auffüllen.
