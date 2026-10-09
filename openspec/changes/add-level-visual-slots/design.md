## Context

- **Grafikvertrag (`add-visual-vertical-slice`):** Spielcode verweist auf Visual-IDs. `registry.json` ordnet ihnen Bilder zu, und `art/specs/<id>.md` beschreibt jede ID mit Status.
  - Fehlt ein Eintrag, meldet `VisualResolver` einen Dummy.
  - Umgebungen sind gemalte Ebenen in Weltgröße, bei Bedarf in Kacheln.
- **Räume (`add-level-rooms`):** Sie sind vorerst die Arena mit ihren gemalten Ebenen und Props.
- **Ziel (Björn):** Die Level sollen sich am Ende sehr von Hub und Prolog abheben. Das Thema von Biom I ist offen (#10), der Hausstil noch nicht trainiert.

## Goals / Non-Goals

**Goals:**
- Slots, an die später gemalte Grafik ohne Codeänderung angeschlossen wird.
- Eine Graubox, die eindeutig Platzhalter ist und sich klar vom Rest abhebt, ohne ein Thema festzulegen.

**Non-Goals:**
- Gemalte Grafik, Props und Thema von Biom I.
- Raumvarianten mit verschiedenen Böden. Ein Slot je Biom reicht, solange alle Räume dieselbe Geometrie haben.

## Decisions

### Ein Slot je Bauteil und Biom

Die Konstanten in `VisualIds` heißen `Biome1Room`, `Biome1RoomWall`, `Biome1Exit` und `GradeBiome1`.
- Raumboden und Raumwand haben die `worldSize` der Arena, damit eine spätere gemalte Ebene genau passt.
- Der Ausgang ist ein Prop mit Fußpunkt. Er hat die Clips `closed` und `open`; `closed` dient als `fallbackClip`.

*Alternative Slots je Raum:* Alle Räume teilen dieselbe Geometrie, deshalb gibt es keinen Grund für verschiedene IDs. Raumvarianten wären ein eigener Change, sobald Räume unterschiedliche Formen haben.

### Graubox-Renderer

`LevelGreyboxRenderer` zeichnet mit dem Pixel-Texture-Muster aus `ShapeRenderer` (keine Allokationen pro Frame):
1. Wandmasse;
2. Boden mit grobem Raster;
3. Südtor;
4. Ausgänge, geschlossen als Gitter und offen als Lichtspalt.

`ArtAssets` fragt je Slot `HasTexture(id)` ab und zeichnet bei vorhandener Grafik das Bild, sonst ruft es die Graubox für dieses Bauteil auf. So lassen sich die Slots einzeln austauschen.

### Platzhalterpalette

Die Palette steht in `BiomeDefinition`:
- Boden ist ein helles, kühles Kalkgrau.
- Die Wand ist dunkles Schieferblau.
- Das Raster ist eine mittlere Grauabstufung.
- Ein Ausgang leuchtet in kaltem Weiß.

Die Wahl folgt diesen Überlegungen:
- Hub, Prolog und Arena sind dunkel mit violetten und rußigen Tönen. Ein heller, kühler Boden hebt sich am stärksten ab und hält Gegner und Flammenfarben lesbar.
- Death-Flame-Violett und Life-Flame-Orange bleiben den Flammen vorbehalten (`VISUAL-ART-DIRECTION.md`).

Die Werte sind Arbeitswerte. Sie werden an einer Aufnahme neben dem Hub geprüft.

### Grading

`CurrentGradeId` liefert in der Phase `Level` die Grading-ID des Bioms. Ohne LUT fällt sie wie bisher auf `grade.neutral` zurück.

## Risks / Trade-offs

- **Ein heller Boden kann Gegner-Silhouetten anders wirken lassen:** Die Aufnahme aus den Tasks prüft das. Falls nötig, wird der Boden um einige Stufen abgedunkelt; die Palette bleibt dabei ein Balance-Wert.
- **Die Arena verliert im Level ihr Aussehen:** Wer die gemalte Arena sehen will, nutzt `--dev --start arena`.
