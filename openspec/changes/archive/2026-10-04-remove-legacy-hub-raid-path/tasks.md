## 1. Nutzung prüfen

- [x] 1.1 Bestätigen, dass `Program.cs` nur `Game1` und `AudioRuntimeTestGame` startet und keine Live-Klasse Typen aus `Scenes`, `Ecs`, `Gameplay`, `Levels`, `Presentation` oder den entfernten `Core`-Typen referenziert
- [x] 1.2 Bestätigen, dass `FireAtlas`, `UIFont` und die Level-JSONs nur vom toten Pfad geladen werden und nicht in `Content.mgcb` stehen
- [x] 1.3 Bestätigen, dass kein Spec in `openspec/specs/` von diesem Pfad abhängt

## 2. Entfernen

- [x] 2.1 Den zweiten Spielpfad samt Inhalten löschen und `Core/ResolutionManager` behalten
- [x] 2.2 Tests für den toten Pfad löschen und `CollisionAndResolutionTests` auf `ResolutionManagerTests` reduzieren
- [x] 2.3 `build-core-fire-raid-loop` ohne Spec-Sync archivieren

## 3. Validierung

- [x] 3.1 Release-Build des Testprojekts ohne neue Warnungen oder Fehler ausführen
- [x] 3.2 Gesamten Testlauf ohne Fehler ausführen
