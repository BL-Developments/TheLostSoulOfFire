## 1. Modell

- [x] 1.1 `DevMenu` mit Abschnitten `Character`/`Enemies`, Einträgen (`Id`, Abschnitt, `Value`/`Action`, Beschriftung), `Open`, `Close`, `Tick`, `MoveSelection`, `Select` ohne MonoGame-Abhängigkeit; Prüfung der Abschnittsreihenfolge.
- [x] 1.2 `SandboxDevMenuEntries.All` als zentrale, vorerst leere Eintragsliste.
- [x] 1.3 Unit-Tests: Abschnittsbeschriftungen, Öffnen/Schließen, Auswahl über Abschnitte ohne Umbruch, Auswahl bleibt beim erneuten Öffnen, leeres Menü, falsche Reihenfolge, Trefferflächen, darstellbare Beschriftungen.

## 2. Steuerung

- [x] 2.1 `F` öffnet in der Sandbox, wenn kein Menü offen ist; `F`/`Escape` schließen ohne Pausen- oder Charaktermenü; `AudioDirector.SetPaused` beim Öffnen und Schließen; `IsGamePaused` umfasst das Dev-Menü.
- [x] 2.2 `W`/`S`, Pfeile, `A`/`D` mit Umschalttaste für große Schritte, `Enter`, Maus-Überfahren und -Klick auf Einträge; Handler `AdjustDevEntry`, `ActivateDevEntry`, `DevEntryValue` als Andockpunkte.

## 3. Darstellung

- [x] 3.1 `DevMenuRenderer`: leichter Schleier, Tafel links unter dem Lebensbalken, `DEV-MENÜ`, Abschnitte mit Linie, `NOCH KEINE EINTRÄGE`, Auswahlmarkierung und -farbe, Werte rechtsbündig, Tastenhilfe; Trefferflächen aus derselben Anordnung.
- [x] 3.2 Sandbox-HUD `SANDBOX · F DEV-MENÜ`; Screenshot-Kontext `sandbox_dev_menu`.

## 4. Abnahme

- [x] 4.1 README um die Taste `F` in der Sandbox ergänzen.
- [x] 4.2 `openspec validate add-sandbox-dev-menu --strict` und `dotnet test` erfolgreich ausführen.
- [x] 4.3 Manuell anspielen mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start sandbox` (`F2` drücken, `F` öffnet das Menü, Hollow steht still, `Escape` schließt ohne Pausenmenü, `F` in `--dev --start arena` bewirkt nichts).
