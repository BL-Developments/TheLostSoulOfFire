## Why

Seit dem Beschluss vom 02.10.2026 soll das Spiel wie gemalte, hochaufgelöste 2D-Grafik im Sinne von Bastion aussehen: düster, mit einem Hauch Diablo, aber nicht so drastisch. Auf `main` liegt dagegen Folgendes:
- die Ludo-Grafik vom 29.08.2026 im Pixel-Look; jede der 96 Figurenanimationen ist pro Richtung getrennt generiert, entgegen `VISUAL-ART-DIRECTION.md` §5b („acht Richtungen entstehen aus einem Rig“);
- der Prolog, ganz aus Formen gezeichnet;
- Lore, die für die Orte der Scheibe weder ein menschliches Ereignis noch ein Aussehen der Figuren festlegt.

Später soll ein Visual-Agent dauerhaft auf `prototype` Grafik nachliefern, während Gameplay auf `main` weiterläuft. Dafür braucht es drei Dinge:
- eine Stilbasis aus Lore, Hausstil und Stil-Bibel, aus der Agenten ohne Rückfrage weiterarbeiten;
- eine kleine Scheibe, an der der Owner Qualität, Kosten und Ablauf abnimmt;
- einen Vertrag, über den Gameplay nie auf Grafik warten muss.

## What Changes

- **Lore-Grundlage (Freigabe 0):** Region-Verträge für den Prolog und die Industrial Cathedral (Arena) mit allen 18 Feldern aus `WORLD-GRAMMAR.md`. Dazu Figurenblätter für Protagonist, Hollow, Burning und Devourer, kurz auch Bruder und Vaelor, sowie eine Düsternis-Charta (das Diablo-Maß), ein Symbolsatz und eine Epochenregel. Ein Agent entwirft, der Owner gibt frei.
- **Stilbasis breit:** Stil-Frames, ein trainiertes Hausstil-Modell und eine Stil-Bibel mit Farbskript und gesperrten Key-Arts für alle vier Prolog-Abschnitte (Ufer, Suchgang, Überfahrt, Schwelle), die Arena und alle Figuren der Figurenblätter.
- **Visueller Vertrag:**
  - Spielcode verweist auf Visual-IDs statt auf Texturpfade. Eine Registry-Datei ordnet jeder ID ihre Clips, Ebenen und Effekte zu.
  - Jede ID hat eine Visual-Spec unter `art/specs/<visual-id>.md` mit Verweis auf ihr Lore-Blatt, Merkmalen, Silhouette, Größe, Akzentfarbe, Animationen mit Spielzeiten, Effekten und Status (`dummy`, `konzept`, `freigegeben`, `im-spiel`).
  - Fehlt Grafik, zeichnet das Spiel einen Dummy und läuft weiter.
  - Die vorhandenen Ludo-Assets ziehen in die Registry um.
- **Gemalte Szene:**
  - Shader werden auf Windows, macOS und Linux ohne Wine gebaut.
  - Farbgrading pro Bereich und Normal-Map-Beleuchtung der Figuren durch Schlüssellicht und Death-Flame-Lichter.
  - Besiegte Gegner lösen sich auf; Vordergrund-Occluder verdecken Spieler, Gegner und Telegraphe nicht.
  - Figuren drehen sich ruhig, die Laufanimation folgt der Strecke.
- **Fertig im Spiel:**
  - Arena hinter Tür I (Welle 1) und das Ufer, der erste Prolog-Abschnitt, neu gemalt: Boden, Props, Hintergrund- und Vordergrundebenen, Atmosphäre.
  - Spieler (Ruhe, Laufen, drei Sensenhiebe, Dash) und Hollow (Ruhe, Bewegung, Swipe, Treffer, Tod) in acht Richtungen aus je einem Rig, mit Normal-Maps.
  - Effekte: drei Sensenhiebe, Dash-Zündung, Core-Treffer, Soul Release, Gegnerauflösung.
- **Für den Agenten vorbereitet:** Suchgang, Überfahrt mit Skiff, Schwelle, Burning, Devourer und Bruder bekommen Key-Art und Visual-Spec mit Status `konzept`. Bis dahin behalten sie ihre Ludo-Grafik oder ihre Formen.
- **Produktionsweg als Skripte:**
  - Bilder und Hausstil laufen lokal auf dem Mac.
  - Figuren entstehen über Konzept, 3D-Modell, Rig und Blender-Rendering; Effekte als Shader, Partikel und Flipbooks.
  - Jeder Schritt läuft über ein Skript unter `tools/visuals/` und landet im Produktionsmanifest. So ist die Scheibe zugleich das Rezept für den Visual-Agenten.
- **Visuelle Prüfung:** `dotnet test` prüft jede Registry-Grafik. `--slice-visual-test` nimmt eine feste Bildreihe in Ufer und Arena auf und beendet sich.
- **Vier Freigaben des Owners:** Lore-Grundlage, Stil-Frames mit Basismodell, 3D-Modell des Spielers vor der Animation, Abnahme der Scheibe im Spiel.
- **Kostenlos zuerst:** freie, lokal ausführbare Modelle und Werkzeuge mit Lizenzen für ein kommerzielles Spiel; bezahlte Dienste sind ein Notweg, den nur der Owner öffnet.

## Capabilities

### New Capabilities

- `visual-registry`: Visual-IDs, Registry, Visual-Specs mit Status und Dummy-Darstellung fehlender Grafik.
- `painted-scene-rendering`: Shader-Build ohne Wine, Farbgrading pro Bereich, Normal-Map-Beleuchtung, Auflösung besiegter Gegner, Occluder sowie Drehen und Gangart von Figuren.
- `visual-quality-checks`: Asset-Prüfungen im Testlauf und die automatische Bildaufnahme der Scheibe in Ufer und Arena.

### Modified Capabilities

Keine. Kampfregeln, Prologablauf, Spielzeiten und HUD bleiben unverändert. `cinematic-combat-presentation` und `death-layer-prologue` gelten weiter und werden durch die neue Grafik erfüllt, nicht geändert.

## Impact

- **Lore und Art Direction:**
  - neu: `docs/current/regions/` (Region-Verträge) und `docs/current/characters/` (Figurenblätter);
  - `VISUAL-ART-DIRECTION.md` bekommt Düsternis-Charta, Symbolsatz und Epochenregel; §5 „wie Diablo“ wird um die Stimmung erweitert;
  - `REGION-SEEDS.md` verlinkt die Verträge, `CANON-STATUS.md` und `DECISION-LOG.md` halten die Freigaben fest.
- **Code:**
  - `Rendering/ArtAssets` liest die Registry.
  - `SoulfireRenderer`: Grading beim Darstellen der Szene, Beleuchtungsdurchgang.
  - `GameWorld`: Zeichenreihenfolge der Ebenen, Occluder, Auflösung.
  - `Rendering/PrologueEnvironment`: Die Formenkomposition des Ufers wird durch Registry-Ebenen ersetzt.
  - `Entities/Player` und die Gegner: sichtbare Blickrichtung, Gangart nach Strecke.
  - `Game1`/`Program`: `--slice-visual-test`.
  - `Rendering/ShapeRenderer`: Dummys.
  - Die getrennte Sensenzeichnung entfällt für Animationen, in denen die Sense Teil der Frames ist.
- **Content:**
  - neue `.fx`-Dateien und das Build-Plugin `ShadowDusk.MgcbPlugin` in `Content.mgcb`;
  - neue Texturen für Arena, Ufer, Spieler, Hollow und Effekte;
  - die ersetzten Ludo-Texturen werden gelöscht; ihre Quellen bleiben in der Git-Historie.
- **Neue Ordner:** `art/specs/` (Visual-Specs), `art/production/` (Quellen, Manifest, Stil-Bibel, Key-Arts), `tools/visuals/` (Python- und Blender-Skripte).
- **Abhängigkeiten:**
  - NuGet `ShadowDusk.MgcbPlugin`;
  - lokal Blender 5.x mit MPFB2, Python 3, mflux, ComfyUI und rembg;
  - ein kostenloses Konto bei Hugging Face; Tokens stehen nur in der Umgebung, nie im Repository;
  - nur Modelle mit Lizenzen, die ein kommerzielles Spiel erlauben.
- **Kosten:** Ziel ist 0 €. Bezahlte Dienste sind ein Notweg, den nur der Owner öffnet (siehe `design.md`).
- **Nicht betroffen:** Spielregeln, Balancewerte, Speicherdaten, Audio.
