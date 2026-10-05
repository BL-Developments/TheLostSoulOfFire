# Visual-Spec: environment.hub

Art: environment
Status: im-spiel
Stil: hausstil
Weltgröße: 1500 × 900 (zwei Kacheln zu 1125 × 1350 Pixeln)
Akzentfarbe: Death-Flame-Violett nur an Warden-Flammen, Siegeln und der offenen Tür
Lore: [VISUAL-ART-DIRECTION §11, Symbolsatz](../../docs/current/VISUAL-ART-DIRECTION.md)

## Merkmale
- Die Aschene Vorhalle, ein Warden-Ort: behauene dunkle Basaltplatten mit feiner Asche, eine hohe Nordwand aus Basaltquadern auf y = 340 mit sieben Türen (I, II, III, Schlusstür, IV, V, VI); die Öffnungen liegen genau auf `HubDoor.Bounds`.
- Steinlaibungen mit Eisenrahmen, Sturz und eiserner Ziffernplakette über jeder Biom-Tür; über der Schlusstür einmal der offene Ring, gemeißelt und ungefüllt.
- Pilaster mit Eisenbändern und geschmiedeten Fassungen für ruhige Warden-Flammen; die Flammen zeichnet das Spiel, weil sie nie ganz still stehen.
- Gerendert als 3D-Szene mit der Spielkamera (orthografisch, 35°) in Blender (`tools/visuals/blender/build_hub.py`), Oberflächen aus gemalten Texturen (FLUX.2 [klein] 4B), Kuwahara-Pinselfilter.

## Silhouette
Breiter, ruhiger Boden vor einer monumentalen Türwand.

## Animationen
- `default`: Standbild, gekachelt.
