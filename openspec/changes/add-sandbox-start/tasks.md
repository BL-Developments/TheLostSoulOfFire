## 1. Startparameter

- [x] 1.1 Bereich `sandbox` in `DeveloperStartOptions` ergänzen; Tests für Zuordnung, Groß-/Kleinschreibung, `--wave` mit `sandbox` (Exitcode-Fall), Attribut-Flags, `DEV_START area=sandbox` und Nennung in der Hilfe.

## 2. Sandbox in GameWorld

- [x] 2.1 `GameWorld.Sandbox.cs` mit Kennzeichen `_sandboxActive`, `BeginSandbox` (Feld leeren, Phase `Arena`, Zustand `Combat`, Spieler in der Mitte, Kamera, Audio) und `ResetSandbox`; `ApplyDeveloperStart` ruft `BeginSandbox` auf.
- [x] 2.2 `UpdateArenaLoop` überspringt in der Sandbox Wellen, Nachschub und Abschluss; `ClearRunState` setzt das Kennzeichen zurück.
- [x] 2.3 Glut, Run-Verlust, Truhen und `E`-Wellenstart in der Sandbox abschalten; Charakterseite zeigt dort nur gesicherte Bestände.
- [x] 2.4 `R` nach Niederlage und `F8` setzen in der Sandbox per `ResetSandbox` zurück.
- [x] 2.5 HUD in der Sandbox: Leben und Resonanz wie in der Arena, kein Währungs- und Wellen-HUD, `SANDBOX` oben; Screenshot-Kontext `sandbox` und `sandbox_player_down`.

## 3. Abnahme

- [x] 3.1 README: Bereich `sandbox` und Neustart in der Sandbox.
- [x] 3.2 `openspec validate add-sandbox-start --strict` und `dotnet test` erfolgreich ausführen.
- [x] 3.3 Manuell anspielen mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start sandbox` (keine Wellen, `F2` bis `F4` spawnen Gegner, keine Glut, Niederlage und `R` setzen zurück, `Zum Hauptmenü` führt in den normalen Ablauf).
