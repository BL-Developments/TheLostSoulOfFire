## 1. Fähigkeiten

- [ ] 1.1 `RunAbilities.TryCast` um `chargeCost` erweitern (Standard `true`); ohne Kosten keine Buchung, alle übrigen Prüfungen unverändert.
- [ ] 1.2 Vollleben-Prüfung von `ZWEITER ATEM` und `Player.Heal` auf `player.MaxHealth` umstellen.
- [ ] 1.3 Unit-Tests: Wirken ohne Kosten lässt das Wallet unverändert und setzt die Abklingzeit; Abklingzeit lehnt weiterhin ab; Heilung über 100 bei höherem Maximalleben, Ablehnung bei vollem Leben unter 100.

## 2. Sandbox

- [ ] 2.1 `CanChooseAbilities` erlaubt in der Sandbox die Auswahl im Kampf (Spieler lebt, kein anderes Menü offen); `F` öffnet das Dev-Menü nicht, solange die Auswahl offen ist.
- [ ] 2.2 `Z`/`X` wirken in der Sandbox mit `chargeCost: false`.
- [ ] 2.3 Fähigkeitsleisten zeigen in der Sandbox `FREI` statt Kosten und nie `GLUT FEHLT`; der Hinweis `C  FAEHIGKEITEN WAEHLEN` erscheint.
- [ ] 2.4 `ResetSandbox` räumt Fähigkeitseffekte und Abklingzeiten ab und behält die Slots.

## 3. Abnahme

- [ ] 3.1 README-Abschnitt zur Sandbox um Fähigkeiten ergänzen.
- [ ] 3.2 `openspec validate add-sandbox-abilities --strict` und `dotnet test` erfolgreich ausführen.
- [ ] 3.3 Manuell anspielen mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start sandbox` (`C`, Fähigkeiten wählen, Trainingspuppe spawnen, `Z`/`X` wirken, Abklingzeit prüfen, Leben im Dev-Menü auf 200 setzen, Schaden nehmen und `ZWEITER ATEM` heilt über 100, Neustart mit `R` behält die Slots).
