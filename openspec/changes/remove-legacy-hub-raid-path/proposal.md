## Why

Das Repository enthält zwei Spielgerüste. `Program.cs` startet ausschließlich `Game1` (bzw. `AudioRuntimeTestGame` für Audio-Laufzeittests). Das zweite Gerüst aus dem Change `build-core-fire-raid-loop` – `TheLostSoulOfFireGame`, `GameCore`/`SceneManager`, `HubScene`/`RaidScene`, das eigene `Ecs/`-Modell und ein zweites `Camera2D` – wird nirgends instanziiert. Es ist zur Laufzeit zudem nicht mehr lauffähig: `Sprites/FireAtlas`, `Fonts/UIFont` und die Level-JSONs stehen weder in `Content.mgcb` noch werden sie ins Ausgabeverzeichnis kopiert. Der tote Pfad erschwert die Orientierung (zwei `GameWorld`-, zwei `Camera2D`-Typen) und bindet Tests an Code, den kein Spieler erreicht.

## What Changes

- Den ungenutzten Spielpfad entfernen: `TheLostSoulOfFireGame`, `Core/GameCore`, `Core/Scene`, `Core/SceneManager`, `Core/InputManager`, `Core/GameAssets`, `Core/Palette`, `Scenes/`, `Ecs/`, `Gameplay/` (inklusive `Gameplay/Camera2D`), `Levels/` und `Presentation/`.
- Die nur von diesem Pfad genutzten Inhalte entfernen: `Content/Sprites/FireAtlas.png`, `Content/Fonts/UIFont.spritefont`, `Content/Levels/Hub.json` und `Content/Levels/FirstRaid.json`.
- Die Tests entfernen, die ausschließlich diesen Pfad prüfen (`AttackTests`, `HealthAndEcsTests`, `ProjectilePoolTests`, `SessionAndLevelTests` sowie die Kollisions- und Diagonal-Tests aus `CollisionAndResolutionTests`).
- `Core/ResolutionManager` bleibt, weil `Game1` und `InputState` ihn nutzen; seine Tests bleiben als `ResolutionManagerTests` erhalten.
- Den offenen Change `build-core-fire-raid-loop` ohne Spec-Sync archivieren, weil er durch den `Game1`-Pfad (Hauptmenü, Prolog, Aschenvorhalle, Arena) überholt ist und seine Specs nie in `openspec/specs/` übernommen wurden.

## Capabilities

### New Capabilities

Keine.

### Modified Capabilities

Keine. Die Capabilities `fire-raid-flow`, `top-down-combat`, `raid-level` und `dark-fire-presentation` existieren nur im überholten Change und nicht in `openspec/specs/`. Eine spätere Homebase (#11) und Biom-Runs (#14) werden als eigene Changes auf dem `Game1`-Pfad spezifiziert.

## Impact

- Reine Entfernung; das gestartete Spiel, das Hauptmenü, der Prolog, die Aschenvorhalle und die Arena sind nicht betroffen.
- Die Testzahl sinkt von 46 auf 34, weil nur Tests für den toten Pfad entfallen.
- Keine neuen Abhängigkeiten, keine Änderungen an `Content.mgcb` oder CI.
