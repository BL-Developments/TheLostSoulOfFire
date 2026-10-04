## 1. Menümodell und Escape-Regeln

- [x] 1.1 `MenuController.Open` mit Wurzelseite, Seiten `Pause`, `PauseQuit`, `QuitConfirm`, `MenuPage.Prompt`, Ergebnisse `Resume`/`QuitToMainMenu` und `IsValueEntry` ergänzen; per Unit-Test prüfen, dass alle Einträge die vorgesehenen Seiten oder Ergebnisse liefern und `ERRUNGENSCHAFTEN UND STATISTIKEN` wirkungslos bleibt.
- [x] 1.2 `HandleEscape` und `OpenQuitConfirmation` ergänzen; per Unit-Test prüfen: Unterseiten führen eine Ebene zurück, Pausenwurzel liefert `Resume`, Hauptmenüwurzel öffnet die Abfrage, `NEIN`/Escape in der Abfrage kehren zurück, `JA` liefert `Quit`.

## 2. Pause im Spielablauf

- [x] 2.1 Menüeingabe aus `GameWorld.UpdateMenu` in eine gemeinsame Methode für beide Menüs ziehen und Escape aus `Game1` entfernen; bestehende Menütests bleiben grün.
- [x] 2.2 Pausenweiche in `GameWorld.Update` einbauen (Öffnen per `WasKeyPressed(Escape)` außer in der Titelphase, Fortsetzen, Hauptmenü, Desktop) und Titelkarte/Hauptmenü auf die Beenden-Abfrage umstellen; per `dotnet build` und den automatisierten Audio-Läufen prüfen, dass der Arenaablauf ohne Escape unverändert durchläuft.
- [x] 2.3 `AudioDirector.SetPaused` und `StopEffects` ergänzen und an Öffnen, Fortsetzen und Rückkehr ins Hauptmenü anbinden; per Build und manueller Prüfung (Musik leiser, Effekte stumm) abnehmen.

## 3. Darstellung

- [x] 3.1 Pausen-Overlay (Schleier, Letterbox, Zierlinie, `PAUSIERT`, Liste) und Abfragetext zeichnen; per Unit-Test prüfen, dass die Hit-Boxen von Pausen-, Beenden- und Abfrageseite im virtuellen Bild liegen.

## 4. Gesamtabnahme

- [x] 4.1 `openspec validate add-pause-menu --strict` und `dotnet test` erfolgreich ausführen.
- [ ] 4.2 Manuell anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena` (Pause im Kampf, Einstellungen, beide Beenden-Wege) und `-- --dev --start prologue` (Pause während der Inszenierung); `Escape` auf Titelkarte und Hauptmenü mit `JA`/`NEIN` prüfen.
