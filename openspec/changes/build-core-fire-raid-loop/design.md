## Context

Das Repository befindet sich fachlich am Anfang: Es gibt noch keine etablierte Gameplay-Architektur und keine bestehenden Hauptspezifikationen. Die erste Vertical Slice soll den vollständigen Weg vom Hub in eine Fire-Raid und zurück beweisen, ohne bereits Meta-Progression oder eine umfangreiche Content-Pipeline aufzubauen.

MonoGame stellt den Game Loop und die Grafik-/Eingabe-APIs bereit, aber keine Szenen-, Entity- oder Levelarchitektur. Diese benötigten Bausteine werden bewusst klein und lokal aufgebaut. Die installierten MonoGame-Skills dienen dabei als Architekturleitfaden; sie sind selbst keine kompilierte Bibliothek.

## Goals / Non-Goals

**Goals:**

- Eine ausführbare DesktopGL-Anwendung mit festem 60-Hz-Update und auflösungsunabhängiger 2D-Darstellung schaffen.
- Hub, Raid und ihre Lebenszyklen sauber voneinander trennen.
- Profil-, Run- und Levelzustand so abgrenzen, dass spätere Meta-Progression ergänzt werden kann.
- Eine kleine handgebaute Raid mit Bewegung, Feuerprojektilen, einem einfachen Gegnertyp, Schaden, Erfolg und Tod spielbar machen.
- Die dunkle Feueridentität mit klaren Rendering-Layern und begrenzten, wiederverwendbaren Partikeleffekten zeigen.
- Kritische Spielfluss- und Domänenlogik automatisiert testbar halten.

**Non-Goals:**

- Permanente Fähigkeiten, Händler, kaufbare Gegenstände, Inventar, Währungen oder Speichern/Laden.
- Prozedurale Levelgenerierung, mehrere Biome, Bosse oder verzweigte Raid-Routen.
- Vollständige Menüs, Pause-System, Controller-Unterstützung oder frei belegbare Eingaben.
- Shader-basiertes dynamisches Licht, Schattenberechnung oder ein allgemeiner Level-Editor.
- Ein vollständiges universelles Game-Engine-Framework.

## Decisions

### Lokale MonoGame-Grundlage statt unbekannter Kernel-Abhängigkeit

Die Anwendung verwendet .NET 9, MonoGame DesktopGL 3.8.5.1 und MonoGame.Extended 6.0.0 für gepoolte Partikeleffekte. Die benötigten Konzepte `Core`, `Scene`, `SceneManager`, `InputManager`, `GameWorld`, `GameEntity` und `GameBehaviour` werden im Projekt implementiert; die Skill-Sammlung liefert dafür Muster, aber kein Runtime-Paket.

Alternativen waren ein monolithisches `Game1` oder ein zusätzliches Game-Framework. `Game1` würde den Ausbau erschweren, während ein weiteres Framework für die kleine Slice unnötige API- und Versionsrisiken erzeugt.

### Hub und Raid sind Szenen; Raid-Level sind Daten innerhalb der Raid-Szene

`HubScene` und `RaidScene` besitzen jeweils einen eigenen `ContentManager` und eine eigene `GameWorld`. Ein Szenenwechsel wird über `SceneManager.RequestChange` angefordert und erst an einer sicheren Frame-Grenze vollzogen. Die alte Szene wird inklusive ihrer Inhalte entsorgt.

Einzelne Raid-Level werden nicht als neue Top-Level-Szenen modelliert. `RaidScene` hält den `RunState` und lädt eine `RaidLevelDefinition` in eine neue Levelinstanz. So kann ein späterer Run mehrere Level durchlaufen, ohne temporäre Run-Daten zu verlieren.

### Drei Lebensdauern für Zustand

- `ProfileState` lebt für die Anwendungssitzung und bleibt zunächst leer; er ist der spätere Anker für dauerhaften Fortschritt.
- `RunState` entsteht beim Betreten der Raid und wird bei Erfolg oder Tod vollständig verworfen.
- `LevelState` enthält die aktuelle Begegnung und wird beim Laden oder Verlassen eines Levels verworfen.

Szenen teilen keine Laufzeit-Entity und keine szenenspezifischen Assets. Sie erhalten nur erforderliche reine Zustandsdaten.

### Leichtgewichtiges komponentenbasiertes Entity-Modell

Eine `GameWorld` besitzt alle `GameEntity`-Instanzen und steuert deren `GameBehaviour`-Lebenszyklen. Spieler, Gegner, Portal und Projektile werden aus fokussierten Behaviours zusammengesetzt, beispielsweise Transform, Sprite, Hitbox, Health, Input, Attack und EnemyChase. Abfragen erfolgen über kleine Domäneninterfaces wie `IDamageable` und `ICollidable`; Komponentenreferenzen werden einmalig in `Awake` beziehungsweise `Start` aufgelöst.

Erzeugung und Zerstörung während eines Frames werden auf eine sichere Update-Grenze verschoben. Feuerprojektile und Partikel verwenden feste Pools, damit der normale Kampf keine wiederkehrenden Heap-Allokationen verursacht.

Einfachere konkrete Klassen für Spieler und Gegner wären kurzfristig kleiner, würden aber gemeinsame Fähigkeiten und zukönftige Gegnerkombinationen früh an Vererbung binden.

### Fester Game Loop und zentrale Eingabe

Das Spiel aktualisiert mit festem Zeitschritt bei 60 Hz. Geschwindigkeiten und Timer verwenden trotzdem `ElapsedGameTime`, wodurch Verhalten explizit und testbar bleibt. `Draw` verändert keinen Spielzustand.

Ein zentraler `InputManager` erfasst zu Beginn jedes Updates vorherigen und aktuellen Tastatur- und Mauszustand. Die erste Belegung ist:

- `WASD` oder Pfeiltasten: Bewegung
- Mausposition: Zielrichtung in Weltkoordinaten
- Linke Maustaste halten: Feuerprojektile mit Abklingzeit abgeben
- `E`: Portal beziehungsweise Abschlussaltar benutzen
- `Escape`: Anwendung verlassen

Bewegungsvektoren werden normalisiert, damit diagonale Bewegung nicht schneller ist. Eine inverse, einmal pro Update berechnete Kameramatrix übersetzt die Mausposition in Weltkoordinaten.

### Handgebautes, datengetriebenes erstes Level

Hub und erste Raid werden als einfache Leveldefinitionen mit Grenzen, Wänden, Spawnpunkten und Triggern modelliert. Die Definitionen werden als Content-Daten geladen; Laufzeitobjekte entstehen daraus beim Szenenstart. Das erste Raid-Level ist deterministisch und enthält mindestens eine Kampfbegegnung sowie einen zunächst inaktiven Abschlussaltar.

Der Altar wird aktiv, sobald alle erforderlichen Gegner besiegt sind. Die Interaktion mit dem aktiven Altar beendet die Raid erfolgreich. Beim erneuten Start wird eine frische Levelinstanz erzeugt.

Handgebaute Daten wurden einer prozeduralen Generierung vorgezogen, damit Kampftempo, Raumgröße und Lesbarkeit zuerst validiert werden können.

### Einfache deterministische Kampfregeln

Kollisionen verwenden getrennte AABB-Hitboxen auf Basis von `Rectangle`; Spritegrößen definieren keine Hitboxen. Wandkollisionen werden achsenweise aufgelöst. Gegner verfolgen den Spieler innerhalb der Arena und verursachen bei Kontakt Schaden mit einer kurzen Unverwundbarkeitszeit. Feuerprojektile fliegen bis zum ersten Treffer oder bis ihre Lebensdauer endet, verursachen Schaden und kehren anschließend in ihren Pool zurück.

Sinkt die Spielergesundheit auf null, akzeptiert die Raid keinen weiteren Gameplay-Input, zeigt kurz eine Niederlagenrückmeldung und fordert danach den Wechsel in den Hub an.

### Dunkle Darstellung mit klaren Renderpässen

Die Welt verwendet eine feste virtuelle Auflösung und wird proportional mit Letterboxing skaliert. Welt und UI werden getrennt gezeichnet:

1. Terrain und Architektur mit dunkler Kohle-/Braunpalette
2. Entities und Projektile mit eindeutigen Silhouetten
3. additive Feuer-, Glut- und Trefferpartikel
4. HUD in Bildschirmkoordinaten ohne Kameratransformation

Feuer verwendet `MonoGame.Extended.Particles` mit beim Laden festgelegten Kapazitäten. Kontinuierliche Effekte markieren Portal und Altar; manuelle Bursts geben Schuss und Treffer Feedback. Es gibt zunächst keinen Shader und keine echte Lichtsimulation. Gegner und Gefahren erhalten Farben beziehungsweise Konturen, die nicht mit dem Spielerfeuer verschmelzen.

## Risks / Trade-offs

- **[Zu viel Grundarchitektur vor dem ersten Spielgefühl]** → Nur die von Hub und Raid tatsächlich verwendeten Core-, Scene- und ECS-Funktionen implementieren; keine allgemeine Engine-API vorwegnehmen.
- **[MonoGame.Extended-Version kollidiert mit MonoGame]** → Versionen explizit festschreiben und Restore sowie DesktopGL-Build als erste Aufgabe validieren.
- **[Dunkle Darstellung verdeckt Gegner oder Gefahren]** → Lesbarkeit als Abnahmekriterium behandeln und auf Shader-Licht verzichten, bis die Grundpalette funktioniert.
- **[Partikel oder Projektile erzeugen Frame-Spitzen]** → Feste Kapazitäten, Projectile Pool und keine Erzeugung von Grafikressourcen in `Update` oder `Draw`.
- **[ECS erschwert kleine Tests]** → Spielfluss und Schadensregeln in kleinen zustandsorientierten Diensten beziehungsweise Behaviours halten und MonoGame-Gerätezugriffe an den Rändern kapseln.
- **[Fehlende finale Grafik beeinflusst das Urteil über Atmosphäre]** → Platzhalter bewusst als kohärentes Mini-Artset gestalten und die finale Art Direction als Folgeentscheidung behandeln.

## Migration Plan

1. Den fehlenden DesktopGL-Quellscaffold wiederherstellen beziehungsweise erzeugen und Abhängigkeiten festschreiben.
2. Core-, Eingabe-, Szenen- und Entity-Grundlagen mit Tests einführen.
3. Hub und Raid-Leveldaten samt Szenen integrieren.
4. Kampf, Abschluss- und Todespfade verbinden.
5. Rendering, Partikel, HUD und Lesbarkeitsprüfung ergänzen.
6. Gesamten Loop in Debug-Build und automatisierten Tests validieren.

Da noch kein produktiver Spielstand und keine bestehende Gameplay-API migriert werden, ist kein Datenmigrationspfad erforderlich. Ein Rollback besteht aus dem Entfernen der neuen Gameplaymodule und der MonoGame.Extended-Abhängigkeit.

## Open Questions

- Ob die finale Darstellung reine Pixel Art oder hochauflösende handgemalte Sprites verwendet, bleibt nach der Vertical Slice zu entscheiden.
- Controller-Unterstützung und frei belegbare Aktionen werden erst nach Validierung des Maus-/Tastatur-Kampfs festgelegt.
- Anzahl, Länge und Verzweigung späterer Raid-Level sind noch keine Anforderungen dieses Changes.
