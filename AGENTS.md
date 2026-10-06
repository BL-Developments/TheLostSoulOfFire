# Hinweise für Agenten

Diese Datei ist die gemeinsame Anleitung für alle Coding-Agents (Claude, Codex,
Copilot, Cursor). `CLAUDE.md` bindet sie nur ein; Regeln werden hier gepflegt.
Ausführliches Wissen zu MonoGame steht in den Skills unter `.claude/skills/`
(gespiegelt in `.agents/skills/` und `.codex/skills/`), Spielregeln und
Entscheidungen in `docs/current/`.

## Projekt

- 2D-Top-down-Roguelike in C# 13 / .NET 9 mit MonoGame 3.8 (DesktopGL) und
  MonoGame.Extended 6.
- `src/TheLostSoulOfFire` ist das Spiel, `tests/TheLostSoulOfFire.Tests` die
  MSTest-Tests, `openspec/` die Spezifikationen und Changes.
- Code, Bezeichner und Code-Kommentare sind Englisch. Dokumentation,
  OpenSpec-Artefakte, Spieltexte und PR-Beschreibungen sind Deutsch.
  Spielbegriffe wie `Glut`, `Geld` oder `SoulSense` bleiben als Namen erhalten.

## Befehle

```powershell
dotnet build tests/TheLostSoulOfFire.Tests/TheLostSoulOfFire.Tests.csproj
dotnet test tests/TheLostSoulOfFire.Tests/TheLostSoulOfFire.Tests.csproj
dotnet run --project src/TheLostSoulOfFire -- --dev --start arena --wave 3
```

Vor jedem Commit laufen Build und Tests ohne Fehler und ohne neue Warnungen.
Mit `--dev --start <bereich>` lässt sich eine Änderung direkt anspielen; die
Bereiche stehen in der README.

## Code-Stil

Die `.editorconfig` gilt (4 Leerzeichen, geschweifte Klammern auf eigener
Zeile, `System`-Usings zuerst). Darüber hinaus folgt neuer Code dem
bestehenden Stil:

- File-scoped Namespaces, die dem Ordner entsprechen
  (`namespace TheLostSoulOfFire.Combat;`).
- Klassen sind `sealed`, solange niemand von ihnen erbt. Felder sind `private`
  und heißen `_camelCase`; was sich nach dem Konstruktor nicht ändert, ist
  `readonly`. Öffentlicher Zustand ist eine Property mit `private set`.
- Explizite Typen statt `var`, mit Target-typed `new`:
  `CurrencyWallet wallet = new();`.
- Nullable ist aktiv. Keine `!`-Unterdrückung, wenn ein Null-Check oder ein
  besserer Typ das Problem löst.
- Balance-Werte (Schaden, Reichweiten, Zeiten, Kosten) stehen als benannte
  Konstante in `Game/GameBalance.cs`, nicht als Zahl im Code.
- Wird eine Klasse zu groß, wird sie nach Verantwortung geteilt, wie
  `GameWorld` in `GameWorld.Abilities.cs`, `GameWorld.Currency.cs` usw.
  Neue Logik bekommt eigene Typen statt weiterer Zeilen in `GameWorld`.

## Sauberer Code

- **Namen erklären die Absicht.** Ein Name sagt, was etwas ist oder tut
  (`TrySpendRun`, `GuardRemaining`), nicht wie es gebaut ist. Keine neuen
  Abkürzungen; `dt` für die Frame-Zeit ist üblich.
- **Eine Aufgabe pro Methode.** Eine Methode, die man nur mit „und" beschreiben
  kann, wird geteilt. Früh zurückkehren statt tief zu verschachteln.
- **Logik von MonoGame trennen.** Regeln, Zustände und Berechnungen kommen ohne
  `GraphicsDevice`, `SpriteBatch` oder `Keyboard` aus, damit Tests sie direkt
  prüfen können. `Draw` zeichnet nur, `Update` ändert Zustand.
- **Keine Allokationen pro Frame.** In `Update` und `Draw` keine neuen Listen,
  LINQ-Ketten, String-Verkettungen oder Lambdas mit Captures; Puffer werden
  wiederverwendet. Inhalte werden einmal in `LoadContent` geladen.
- **Kommentare erklären das Warum.** Was der Code tut, sagt der Code. Ein
  Kommentar begründet eine nicht offensichtliche Entscheidung oder verweist
  auf das Issue (`(#104)`). `/// <summary>` an öffentlichen Typen und Members,
  deren Zweck der Name nicht trägt. Kein auskommentierter Code.
- **Kein toter Code, keine Spekulation.** Nichts für einen „später
  vielleicht"-Fall bauen. Unbenutzte Members, Parameter und Usings entfernen.
- **Fehler bewusst behandeln.** Ein `catch` ohne Typ ist nur an Grenzen zu
  Datei und Hardware erlaubt (Spielstand, Einstellungen, Audio) und hat dann
  einen klaren Fallback und einen Kommentar, warum das Spiel weiterläuft.
  Ungültige Eingaben wie Startoptionen werden geprüft und klar gemeldet.

## Tests

- Jede Verhaltensänderung bekommt einen Test, jeder behobene Fehler einen
  Test, der ohne die Korrektur fehlschlägt.
- Testklassen sind `public sealed` mit `[TestClass]`; Testnamen folgen
  `Methode_Situation_Erwartung`
  (`TrySpendRun_RefusesShortFundsAndInvalidAmounts_WithoutChange`).
- Aufbau: vorbereiten, eine Aktion, prüfen, jeweils durch eine Leerzeile
  getrennt. Erwartete Werte kommen aus `GameBalance`, nicht als Kopie.
- Tests nie abschwächen, auskommentieren oder löschen, um Grün zu bekommen.

## Änderungen

- Klein und fokussiert: nur ändern, was die Aufgabe braucht. Kein Umformatieren
  oder Umbenennen nebenbei.
- Vor dem Schreiben nachsehen, wie es das Projekt schon löst, und das
  wiederverwenden statt eine zweite Variante zu bauen.
- Commits im Stil `feat: …`, `fix: …`, `docs: …`, `chore: …`, `ci: …` mit
  kurzer Betreffzeile.
- Neue Pakete, geänderte Spielregeln und Änderungen an CI oder Workflows nur
  nach Rückfrage. Spielregeln richten sich nach `docs/current/GAME-RULES.md`
  und `docs/current/DECISION-LOG.md`.

## Pull Requests mit OpenSpec-Change

Setzt ein PR einen OpenSpec-Change vollständig um, steht in seiner
Beschreibung eine eigene Zeile:

```
OpenSpec-Archive: <change-name>
```

Mehrere Changes werden durch Leerzeichen getrennt, in der Reihenfolge, in der
sie aufeinander aufbauen. Nach dem Merge archiviert der Workflow
`.github/workflows/openspec-archive.yml` genau diese Changes nacheinander und
öffnet dafür einen gemeinsamen Folge-PR `chore/archive-pr-<nummer>`. PRs, die einen
Change nur vorschlagen (Proposal, Design, Tasks) oder nur teilweise umsetzen,
bekommen die Zeile nicht. Archiviere Changes nach einem Merge nicht von Hand.
