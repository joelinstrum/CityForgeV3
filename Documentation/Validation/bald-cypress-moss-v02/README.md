# Cypress pair validation

Fresh Unity resource check verified both planting IDs and all eight 1024×1536 seasonal assets. Measured alpha>128 central trunk feet match runtime pivots: A spring/summer/autumn/winter 25/27/27/26px, B 17/17/17/18px. Both use PPU82.

`lot-camera-{season}.png` shows the two cypresses side by side through the active Lot camera, using temporary SpriteRenderers with the production flora material. The preview objects were removed and Lot JSON was unchanged. Summer and autumn captures were visually inspected; B's deep green summer and golden autumn are distinguishable from A's softer green and copper autumn. The preview uses current Lot lighting and lacks placed-tree shadows. No dense performance measurement.
