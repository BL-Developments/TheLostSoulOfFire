## 1. Modell

- [ ] 1.1 `CharacterMenu` (Reiter `Character`, `Map`, `Skills`; `IsOpen`, `SelectedTab`, `OpenTimer`; `Open`, `Close`, `SelectNext`, `SelectPrevious`, `Select`, `Update`) ohne MonoGame-Abhängigkeit anlegen; Unit-Tests für Öffnen auf `CHARAKTER`, Wechsel ohne Umbruch am Rand und Zurücksetzen beim erneuten Öffnen.
- [ ] 1.2 Anzeigewerte der Charakterseite aus `PlayerAttributes` ableiten (Waffen- und Fähigkeitsschaden in ganzen Prozent mit Vorzeichen, Schadensverringerung in ganzen Prozent); Unit-Tests für Startwerte, `Stärke 20`, `Fähigkeitsstärke 6` und `Rüstung 0`.

## 2. Steuerung und Pause

- [ ] 2.1 In `GameWorld` eine gemeinsame Pausenbedingung (Pausen- oder Charaktermenü offen) einführen und an allen Stellen nutzen, die heute `_pauseMenu.IsOpen` für Update-Stopp, ausgeblendete Texte und Screenshot-Kontext abfragen.
- [ ] 2.2 `Tab` öffnet das Charaktermenü außerhalb der Titelphase, wenn kein Menü offen ist; `Tab` und `Escape` schließen es ohne Spielaktion und ohne Pausenmenü; `Tab` im Pausenmenü bleibt wirkungslos. `AudioDirector.SetPaused` beim Öffnen und Schließen.
- [ ] 2.3 Reiterwechsel per `Links`/`Rechts`, `A`/`D` und Mausklick auf einen Reiter.

## 3. Darstellung

- [ ] 3.1 `PixelText` um das Zeichen `+` ergänzen.
- [ ] 3.2 `CinematicPresentation.DrawCharacterMenu`: Schleier, Letterbox, Zierlinie und Einblendung wie beim Pausenmenü, Reiterleiste mit Auswahlfarbe, Atmen, Auswahlmarkierung und Unterstreichung, gedämpfte Platzhalterreiter; Reiter-Trefferflächen für die Maus aus derselben Anordnung.
- [ ] 3.3 Charakterseite mit `LEBEN`, `STÄRKE`, `FÄHIGKEITSSTÄRKE`, `RÜSTUNG` und Wirkungszeilen; Platzhalterseiten mit `NOCH NICHT VERFÜGBAR`.
- [ ] 3.4 Screenshot-Kontext `character_<reiter>` ergänzen und native Aufnahmen der drei Seiten in Arena und Hub prüfen.

## 4. Gesamtabnahme

- [ ] 4.1 README um die Taste `Tab` ergänzen.
- [ ] 4.2 `openspec validate add-character-menu --strict` und `dotnet test` erfolgreich ausführen.
- [ ] 4.3 Manuell anspielen mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena --strength 20 --ability-power 6 --armor 0` (Menü mit `Tab` öffnen, Werte und Prozente prüfen, Reiter wechseln, Gegner stehen still, Musik leiser, Schließen mit `Tab` und `Escape` ohne Pausenmenü) und mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start hub` (Menü im Hub, Hub-Hinweise ausgeblendet).
