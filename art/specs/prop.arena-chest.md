# Visual-Spec: prop.arena-chest

Art: prop
Status: im-spiel
Stil: hausstil
Weltgröße: aus dem Render (Bodenmitte der Truhe)
Akzentfarbe: Messing und warmes Gold im Inneren; Seelenglas im Schloss violett
Lore: [VISUAL-ART-DIRECTION §11, Wardens](../../docs/current/VISUAL-ART-DIRECTION.md)

## Merkmale
- Warden-Reliquiar aus dunklem Holz mit Eisenbändern, Messingecken und gewölbtem Deckel; im Schloss ein violettes Seelenglas.
- Beim Öffnen schwingt der Deckel am hinteren Scharnier auf, Gold leuchtet von innen.
- Gebaut mit `tools/visuals/blender/build_chest.py` (Spielkamera, 1,5 px je Welteinheit), gepackt mit `tools/visuals/pack_prop_clip.py`.

## Silhouette
Niedrige, breite Truhe mit gewölbtem Deckel; geöffnet steht der Deckel hinten auf.

## Animationen
- `default`: geschlossen (Standbild).
- `open`: 10 Frames, abgetastet über `ArenaChest.OpenProgress`; danach bleibt die offene Truhe als Bild kurz stehen und verblasst.
