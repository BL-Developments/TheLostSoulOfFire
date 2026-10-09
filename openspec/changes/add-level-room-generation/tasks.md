## 1. Raumfolge

- [ ] 1.1 Neue Datei `src/TheLostSoulOfFire/Game/Levels/LevelLayout.cs` (Namespace `TheLostSoulOfFire.Game.Levels`):
  - `public enum LevelRoomKind { Start, Combat, LevelEnd }`
  - `public sealed record LevelRoom(int Id, LevelRoomKind Kind, int Progress, IReadOnlyList<int> Exits)`. Dabei ist `Progress` der Stufenindex (Start = 0, erste Kampfstufe = 1); `Exits` sind die Ids der nächsten Stufe, links vor rechts.
  - `public sealed class LevelLayout` mit `IReadOnlyList<IReadOnlyList<LevelRoom>> Stages`, `int Seed`, `LevelRoom Start`, `LevelRoom Room(int id)` und `int CombatStageCount`.
  - `public sealed record LevelLayoutSettings(int CombatStagesMin, int CombatStagesMax, float ForkChance)` mit `static LevelLayoutSettings Default` aus `GameBalance`.

  Prüfen: `dotnet build` ohne neue Warnungen.
- [ ] 1.2 In `Game/GameBalance.cs` in einem eigenen Abschnitt `// Levels (add-level-room-generation)` die Konstanten `LevelCombatStagesMin = 4`, `LevelCombatStagesMax = 6` und `LevelForkChance = 0.35f`. Prüfen: Build.
- [ ] 1.3 Neue Datei `Game/Levels/LevelLayoutGenerator.cs`: `public static class LevelLayoutGenerator` mit `public static LevelLayout Generate(int seed, LevelLayoutSettings settings)`.
  - Ein `Random random = new(seed)` würfelt zuerst `random.Next(min, max + 1)` Kampfstufen und dann je Kampfstufe `random.NextDouble() < ForkChance` für zwei Räume.
  - Room-Ids werden fortlaufend ab 0 in Stufenreihenfolge vergeben.
  - Jeder Raum bekommt als `Exits` alle Ids der nächsten Stufe; das Levelende bekommt keine.
  - Kein LINQ in Schleifen nötig; Allokationen sind hier erlaubt, weil der Generator nur beim Levelstart läuft.

  Prüfen: 1.4.
- [ ] 1.4 Neue Testdatei `tests/TheLostSoulOfFire.Tests/LevelLayoutGeneratorTests.cs` (`public sealed class`, `[TestClass]`) mit diesen Tests:
  - `Generate_AnySeed_StartsWithStartAndEndsWithLevelEnd`
  - `Generate_AnySeed_EachCombatStageHasOneOrTwoRooms`
  - `Generate_AnySeed_EveryRoomReachableAndLeadsToLevelEnd` (Breitensuche vom Start)
  - `Generate_AnySeed_NoRoomHasMoreThanTwoExits`
  - `Generate_SameSeed_ReturnsSameLayout`
  - `Generate_ThousandSeeds_StaysWithinStageLimits` (Grenzen aus `GameBalance`)
  - `Generate_HundredSeeds_ProducesForksAndStraightLevels`
  - `Generate_ParallelRooms_ShareProgress`

  Prüfen: `dotnet test` grün.

## 2. Abschluss

- [ ] 2.1 In `docs/current/DECISION-LOG.md` oben einen Eintrag „2026-10-09 — Zufällige Raumfolge der Level“ einfügen: grob linear mit parallelen Wegen, einzelne Räume wie bei Hades, Räume vorerst wie die Arena, Wellen wachsen mit dem Raumfortschritt, Tür I führt in die neuen Level. Nachweis: Entscheidungen des Owners im Projekt-Thread am 09.10.2026. Prüfen: Eintrag steht über dem Eintrag vom 06.10.
- [ ] 2.2 `openspec validate add-level-room-generation --strict`, `dotnet build tests/TheLostSoulOfFire.Tests/TheLostSoulOfFire.Tests.csproj` und `dotnet test tests/TheLostSoulOfFire.Tests/TheLostSoulOfFire.Tests.csproj` ohne Fehler und neue Warnungen.
- [ ] 2.3 Der Change ist noch nicht spielbar; den Startbefehl `--dev --start level` bringt erst `add-level-rooms`. Bis dahin prüft `dotnet test`.
