# The Lost Soul of Fire

Ein 2D-Top-down-Roguelike auf Basis von C# und MonoGame.

## Projektwissen und Planung

- [Zentrale Wissensübersicht](docs/current/README.md)
- [Spielregeln](docs/current/GAME-RULES.md) und [offene Entscheidungen](docs/current/OPEN-QUESTIONS.md)
- [Roadmap](docs/planning/roadmap.md) und [Pflegeregeln](docs/planning/WORKFLOW.md)
- [GitHub Project](https://github.com/orgs/BL-Developments/projects/1)

## Voraussetzungen

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Entwicklung

```powershell
dotnet restore
dotnet build
dotnet run --project src/TheLostSoulOfFire
```

Mit `Escape` oder der Zurück-Taste eines Controllers wird das Spiel beendet.

Im Spiel öffnet `Escape` das Pausenmenü und `Tab` das Charaktermenü mit den
Reitern Charakter, Map und Skills. Die Charakterseite zeigt Leben,
Charakterwerte und Währungen; `Tab` oder `Escape` schließt das Menü wieder.

## Developer-Mode

Mit `--dev` und `--start <bereich>` beginnt das Spiel direkt im gewählten
Bereich, ohne Titel, Prolog und Hub durchzuspielen:

```powershell
dotnet run --project src/TheLostSoulOfFire -- --dev --start arena --wave 3
```

| Bereich | Start |
| --- | --- |
| `title` | Hauptmenü (wie ohne `--start`) |
| `prologue` | Anfang des Prologs |
| `prologue:find-trace` | Prolog, Seelenspur finden |
| `prologue:search` | Prolog, Suchabschnitt |
| `prologue:devourer` | Prolog, Devourer-Abschnitt |
| `prologue:transit` | Prolog, Fahrt auf dem Skiff |
| `hub` | Aschenvorhalle mit den Biom-Türen |
| `arena` | Arena hinter Tür I; `--wave 1` bis `--wave 10` wählt die erste Welle |
| `sandbox` | Sandbox: Arena ohne Wellen, Truhen und Währungen, nur über `--dev` erreichbar |
| `level` | Ein neu erzeugtes Level, Raum für Raum; `--seed <n>` legt die Raumfolge fest, der Seed steht in der Konsole (`LEVEL_SEED`) |
| `biome:1` | Ein neuer Run von Biom I, drei Level bis zum Wächterraum; `--level 1` bis `--level 3` wählt das Startlevel, `--seed <n>` den Run-Seed (`BIOME_SEED`) |

Mit `--strength`, `--ability-power` und `--armor` (je 0 bis 99, Standard 10)
lassen sich die Charakterwerte des Spielers setzen, etwa
`-- --dev --start arena --strength 20 --armor 0`. Stärke skaliert die Sense,
Fähigkeitsstärke die Seelenkanone, Rüstung verringert erlittenen Schaden.

In der Sandbox setzt `R` nach einer Niederlage (oder `F8`) den Spieler in der
Mitte zurück und räumt das Feld. `F` öffnet dort das Dev-Menü; `F` oder `Escape`
schließt es wieder. `W`/`S` wählen einen Eintrag, `A`/`D` ändern einen Wert
(mit Umschalt in großen Schritten), `Enter` führt eine Aktion aus. Im
Abschnitt Charakter lassen sich Leben (1 bis 999), Stärke, Fähigkeitsstärke und
Rüstung setzen; `ZURÜCKSETZEN` stellt die Werte vom Sandbox-Start wieder her. Im
Abschnitt Gegner spawnt jeder Eintrag einen Gegner dieses Typs in Sichtweite,
darunter die `TRAININGSPUPPE`: Sie steht still, greift nicht an, zeigt jeden
Treffer und die Schadenssumme und füllt sich nach 2,5 Sekunden ohne Treffer auf;
`ALLE GEGNER ENTFERNEN` räumt das Feld.
`C` öffnet in der Sandbox jederzeit die Fähigkeitsauswahl; `Z`/`X` wirken dort
ohne Glutkosten, die Abklingzeiten gelten wie in der Arena.

Ungültige Angaben beenden das Programm mit einer Meldung und Exitcode 2. Die
Debug-Tasten (`F1` Overlay, `F2`–`F4` Gegner, `F5` Resonance, `F6` Welle
räumen, `F7` Soul Sense, `F8` Encounter neu, `D1`–`D4` Prolog-Abschnitte)
funktionieren unabhängig davon. Jeder umgesetzte OpenSpec-Change nennt in
seiner letzten Aufgabe den passenden Startbefehl.

## Projektstruktur

- `src/TheLostSoulOfFire` – DesktopGL-Spielprojekt
- `src/TheLostSoulOfFire/Content` – MonoGame-Content-Pipeline

DesktopGL erlaubt Builds für Windows, Linux und macOS. Spielinhalte werden in
`Content/Content.mgcb` eingetragen und beim Build durch die Content-Pipeline
verarbeitet.

## Erster Faehigkeitenpool

Die sechs ausgewaehlten Faehigkeiten sind als Solo-Arena-Prototyp spielbar. C oeffnet die Auswahl in der Homebase oder vor/zwischen Wellen; links/rechts waehlt den Slot, 1-6 ruestet aus, Enter schliesst. Z/X setzt die beiden Faehigkeiten im Kampf ein. Werte sind vorlaeufig.

Details und Testbefehle: [Faehigkeiten-Prototyp](docs/current/ABILITY-PROTOTYPE.md).
