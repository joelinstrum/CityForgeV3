# Medium conifers validation

`resources.txt` is a fresh Unity in-editor check: all three species resolve to snow-free art in Spring/Summer/Autumn and snowy art in Winter, six textures load, each is 1024×1536, each is in the Fir and Mountain library, and measured alpha>128 trunk feet match runtime pivots. Balsam PPU128, Fraser PPU128, Blue Spruce PPU120, giving roughly 12m visible height.

`lot-camera-summer.png` and `lot-camera-winter.png` are direct renders from the active Lot camera using temporary SpriteRenderers with the production flora material. All preview objects were removed; Lot JSON was unchanged. Both captures were visually inspected for silhouette, color, snow coverage and terrain contact. They use current Lot lighting and omit actual placed-tree shadows. No dense performance measurement was made.
