Lies vor dem Start `design.md`. Die Map ist reine Darstellung im Charaktermenü: keine neue `GamePhase`, keine Änderung an Raumerzeugung, Begegnungen oder HUD.

## 1. Logik ohne MonoGame

- [ ] 1.1 `Game/Levels/LevelLayout.cs`: Property `public int RoomCount => _roomsById.Count;`. Prüfen: Build.
- [ ] 1.2 `Game/Levels/LevelRun.cs`:
  - Feld `private readonly bool[] _visited;`, im Konstruktor mit `new bool[layout.RoomCount]` anlegen und `_visited[layout.Start.Id] = true` setzen.
  - In `TryTakeExit` nach `Current = next;` den Eintrag `_visited[next.Id] = true` setzen.
  - Methode `public bool WasVisited(int roomId) => _visited[roomId];`.
  - Property `public bool GuardianAtEnd => _guardianAtEnd;` mit `/// <summary>`.

  Prüfen: 1.4.
- [ ] 1.3 Neue Datei `Game/Levels/LevelMap.cs`: `public static class LevelMap` (namespace `TheLostSoulOfFire.Game.Levels`).
  - Konstanten `private const float BottomMargin = 70f;`, `private const float StageSpacing = 56f;`, `private const float ForkOffsetX = 90f;`.
  - `public static Vector2 NodePosition(Rectangle page, LevelLayout layout, LevelRoom room)`:
    - `y = page.Bottom - BottomMargin - room.Progress * StageSpacing`;
    - `IReadOnlyList<LevelRoom> stage = layout.Stages[room.Progress]`; bei `stage.Count == 1` ist `x = page.Center.X`, sonst `page.Center.X - ForkOffsetX` für `stage[0]` und `page.Center.X + ForkOffsetX` für `stage[1]` (Vergleich über `Id`, keine LINQ).
  - `public static bool IsPathRevealed(LevelRun run, LevelRoom from, int toId) => run.WasVisited(from.Id) && run.WasVisited(toId);`

  Prüfen: 1.4.
- [ ] 1.4 Tests (Seeds über die vorhandene Hilfsmethode `ForkSeed()` in `LevelRunTests` bzw. eine Kopie davon in der neuen Testklasse):
  - in `tests/TheLostSoulOfFire.Tests/LevelRunTests.cs`:
    - `WasVisited_AtStart_OnlyStartRoom` (alle Räume außer `layout.Start` sind `false`);
    - `WasVisited_AfterRightExitOfFork_LeftRoomStaysHidden` (Start räumen ist nicht nötig, `TryTakeExit(1, …)` vom Startraum der Fork-Seed-Layouts; `Stages[1][0]` bleibt `false`, `Stages[1][1]` wird `true`);
    - `WasVisited_AfterLeavingRoom_StaysVisited` (zwei Räume weitergehen, mit `MarkCleared()` dazwischen);
    - `GuardianAtEnd_ConstructedWithGuardian_ReturnsTrue`.
  - neue Datei `tests/TheLostSoulOfFire.Tests/LevelMapTests.cs` (`public sealed class LevelMapTests`):
    - `NodePosition_ForkStage_LeftAndRightOfCentre`;
    - `NodePosition_LaterStage_IsHigher` (Stufe 2 hat kleineres `Y` als Stufe 1);
    - `NodePosition_SingleRoomStage_IsCentred`;
    - `IsPathRevealed_ToUnvisitedRoom_ReturnsFalse`.

  Prüfen: `dotnet test`.
- [ ] 1.5 `Menu/CharacterMenu.cs`:
  - `public void OpenOn(CharacterMenuTab tab)` ruft `Open()` und danach `Select(tab)` auf.
  - `IsPlaceholder` liefert nur noch für `CharacterMenuTab.Skills` `true`; den `/// <summary>` anpassen („Skills is a placeholder.“).
  - In `tests/TheLostSoulOfFire.Tests/CharacterMenuTests.cs`: `OpenOn_Map_SelectsMapTab`, `IsPlaceholder_Map_ReturnsFalse`, `IsPlaceholder_Skills_ReturnsTrue`.

  Prüfen: `dotnet test`.

## 2. Anbindung im Spiel

- [ ] 2.1 Überschrift in `GameWorld`:
  - Feld `private string _levelMapHeading = "";` in `Game/GameWorld.Levels.cs`.
  - In `StartLevel` (`GameWorld.Levels.cs`) `_levelMapHeading = "LEVEL";` setzen.
  - In `Game/GameWorld.BiomeRun.cs` eine Methode `private static string LevelMapHeading(BiomeRun biome) => $"BIOM {biome.Biome.Numeral} · LEVEL {biome.Level}";` neben `BiomeRoomLabel` anlegen und in `BeginBiomeLevel` und `TravelToNextBiomeLevel` jeweils dort setzen, wo `_biomeRoomLabel` gesetzt wird.

  Prüfen: Build.
- [ ] 2.2 Taste `M` in `Game/GameWorld.cs`:
  - Direkt nach dem `Tab`-Block in `Update` (der Block mit `_characterMenu.Open()`) einen gleich gebauten Block für `Keys.M` einfügen, der `_characterMenu.OpenOn(CharacterMenuTab.Map)` aufruft.
  - `OpenSkillsMenu` in `GameWorld.Abilities.cs` auf `_characterMenu.OpenOn(CharacterMenuTab.Abilities)` umstellen.
  - In `UpdateCharacterMenu` die Schließbedingung um `_characterMenu.SelectedTab == CharacterMenuTab.Map && input.WasKeyPressed(Keys.M)` erweitern. Danach, vor der Links/Rechts-Auswertung: `M` auf einem anderen Reiter ruft `_characterMenu.Select(CharacterMenuTab.Map)` auf (gleicher Ton `UiMove` wie beim Reiterwechsel, über den vorhandenen Vergleich mit `tabBefore`).

  Prüfen: `--dev --start hub`, `M` öffnet auf `MAP`, `M` schließt, `Tab` öffnet weiter auf `CHARAKTER`, `Escape` öffnet bei offenem Menü kein Pausenmenü.
- [ ] 2.3 Neue Datei `Rendering/LevelMapRenderer.cs`: `public static class LevelMapRenderer` mit
  `public static void Draw(SpriteBatch batch, Texture2D pixel, Rectangle page, LevelRun run, string heading, float pulse, float reveal)`.
  - Überschrift mit `PixelText.DrawCentered` oben in der Seite.
  - Schleife über `run.Layout.Stages` und deren Räume (verschachtelte `for`-Schleifen, kein `foreach` über LINQ, keine Listen):
    1. Wege: für jeden betretenen Raum je Ausgang `toId` mit `LevelMap.IsPathRevealed` eine Linie `batch.DrawLine` zwischen den `NodePosition`s.
    2. Wegansätze: für `run.Current` je Ausgang eine Linie auf 40 % der Strecke zum Zielraum; Farbe gedämpft, wenn `!run.IsCleared`.
    3. Räume: nur bei `run.WasVisited(room.Id)` das Symbol nach `design.md` (Start Umriss mit `DrawRectangle`, Kampf `FillRectangle`, Levelende `UiKit.FillDiamond`, Wächter größere Raute in `GameBalance.DeathFlame`; Wächter, wenn `room.Kind == LevelRoomKind.LevelEnd && run.GuardianAtEnd`).
    4. Aktueller Raum in `GameBalance.DeathFlameBright` mit Ring `batch.DrawCircle`, Radius im Takt von `pulse`.
  - Legende in einer Zeile unter der Map mit den vier Symbolen und `START`, `KAMPF`, `REISEPUNKT`, `WÄCHTER`.
  - Alle Farben mit `reveal` multipliziert, wie auf der Charakterseite.

  Prüfen: Build.
- [ ] 2.4 `Rendering/CinematicPresentation.cs`, `DrawCharacterMenu`:
  - Zwei neue Parameter `LevelRun? levelRun, string levelMapHeading` am Ende.
  - Neuer Zweig `menu.SelectedTab == CharacterMenuTab.Map`: Panel wie auf der Charakterseite (`UiKit.Panel` mit derselben Seitengröße), darin bei `levelRun is not null` `LevelMapRenderer.Draw(…, pulse: menu.OpenTimer, reveal)`, sonst `KEINE KARTE` mittig in derselben gedämpften Farbe wie `NOCH NICHT VERFÜGBAR`.
  - Der Aufruf in `GameWorld.Draw` (Zeile mit `_presentation.DrawCharacterMenu`) übergibt `_levelRun` und `_levelMapHeading`.

  Prüfen: Build ohne neue Warnungen.

## 3. Abnahme

- [ ] 3.1 `openspec/specs`-Abgleich: `openspec validate add-level-map --strict` läuft ohne Fehler.
- [ ] 3.2 Manuell anspielen mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start biome:1`:
  - im Startraum `M`: nur der Startraum mit Wegansätzen, Überschrift `BIOM I · LEVEL 1`;
  - im Kampf `M`: Ansätze gedämpft, Spiel steht;
  - an einer Gabelung einen Weg wählen: der andere Raum bleibt verdeckt;
  - am Reisepunkt weiterreisen: Map in Level 2 zeigt nur den Startraum;
  - im Hub `M`: `KEINE KARTE`.

  Für eine Gabelung direkt am Start mit `--start level --seed <n>` einen Seed aus der Konsolenausgabe `LEVEL_SEED` wiederverwenden. Screenshot der Map nach `/mnt/project-files/level-map/` legen.
