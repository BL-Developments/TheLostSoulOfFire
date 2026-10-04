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
| `arena` | Arena hinter Tür I; `--wave 1` bis `--wave 4` wählt die erste Welle |

Mit `--strength`, `--ability-power` und `--armor` (je 0 bis 99, Standard 10)
lassen sich die Charakterwerte des Spielers setzen, etwa
`-- --dev --start arena --strength 20 --armor 0`. Stärke skaliert die Sense,
Fähigkeitsstärke die Seelenkanone, Rüstung verringert erlittenen Schaden.

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
