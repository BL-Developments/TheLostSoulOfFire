## 1. Konten und Regeln

- [x] 1.1 `CurrencyWallet.PreviewPartialSecure` und `SecurePartialRun` mit Abrundung je Währung; Arbeitswerte `TravelPointSecurePercent`, `TravelPointWave`, `TravelPointInteractRadius` in `GameBalance`.
- [x] 1.2 `TravelPoint` mit `TryDecide`, das genau eine Entscheidung annimmt; `GameFlowRules.ExtractToHub`.
- [x] 1.3 Unit-Tests: Rechenbeispiele aus `ECONOMY.md`, Bestand null und eins, Vorschau gleich Übertrag, doppelte Entscheidung, Weiter ohne Sichern, Extrahieren, Niederlage nach Teilsicherung, Reichweite, Position abseits von Kisten und Auslösezone.

## 2. Arena

- [x] 2.1 Reisepunkt in der Pause nach Welle 5 erzeugen, nicht in der Sandbox; beim Wellenstart und Run-Ende entfernen.
- [x] 2.2 `E`-Reihenfolge Kiste, Reisepunkt, Auslösezone; Aufforderung `E REISEPUNKT`.
- [x] 2.3 Menü mit drei Entscheidungen, Vorschau und gesicherten Beständen; Welt eingefroren; `Esc` schließt.
- [x] 2.4 Teilsichern speichert und startet Welle 6; Weiter ohne Sichern startet Welle 6; Extrahieren speichert und wechselt in den Hub mit Anzeige des extrahierten Betrags.

## 3. Prüfung

- [x] 3.1 `--travel-visual-test`: Run 1 öffnet die Kiste nach Welle 3, sichert am Reisepunkt die Hälfte und stirbt in Welle 6; Run 2 extrahiert. Prüft Bestände nach jedem Schritt und nimmt Aufforderung, Menü, Teilsicherung und Hub auf. PASS am 06.10.2026.
- [x] 3.2 `--currency-visual-test` läuft weiter grün (Welle 5 wird dort über die Mitte gestartet, also ohne Sichern).
- [ ] 3.3 Lokaler Koop: NOT_RUN; #59 ist offen und Koop gibt es auf dieser Code-Basis nicht.
- [ ] 3.4 Nach dem Merge des Visuals-Branchs auf `main` umstellen und die Platzierung nach Welle 5 in `docs/current/DECISION-LOG.md` und `ECONOMY.md` eintragen.

Developer-Start: `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena --wave 5`, Welle 5 leeren, oben mittig mit `E` den Reisepunkt öffnen.
