## 1. Werte

- [x] 1.1 `Player.MaxHealth` und `SetMaxHealth` (auffüllen, mindestens 1); `Reset` füllt auf `MaxHealth`; HUD, Charakterseite und Debug-Overlay lesen das Maximalleben vom Spieler.
- [x] 1.2 `DevValueRange` mit Bereichen für Leben (1–999, 10/100) und Charakterwerte (0–99, 1/10); Unit-Tests für Schritte und Grenzen.

## 2. Dev-Menü

- [x] 2.1 Einträge `LEBEN`, `STÄRKE`, `FÄHIGKEITSSTÄRKE`, `RÜSTUNG` (Werte) und `ZURÜCKSETZEN` (Aktion) in `SandboxDevMenuEntries`; Anzeige der aktuellen Werte, Ändern in `AdjustDevEntry`.
- [x] 2.2 Startwerte beim Betreten der Sandbox merken; `ZURÜCKSETZEN` und Verlassen der Sandbox stellen sie und Leben 100 wieder her; `ClearRunState(stayInSandbox)` für den Neustart in der Sandbox.
- [x] 2.3 Unit-Tests für Maximalleben über `Reset`, Reihenfolge und Art der Charaktereinträge und Breite der längsten Zeile.

## 3. Abnahme

- [x] 3.1 README um die Charakterwerte im Dev-Menü ergänzen.
- [x] 3.2 `openspec validate add-sandbox-attribute-editor --strict` und `dotnet test` erfolgreich ausführen.
- [x] 3.3 Manuell anspielen mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start sandbox --armor 0` (`F`, Leben und Stärke ändern, HUD und `Tab` prüfen, `F2` und Treffer vergleichen, `ZURÜCKSETZEN`, Neustart mit `R` behält Werte).
