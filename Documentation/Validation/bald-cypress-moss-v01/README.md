# Bald cypress with Spanish moss V01

`resources.txt` is a fresh Unity in-editor resource and pivot check. All four 1024×1536 seasonal textures load through the shared Lot/District resource resolver. Alpha-measured trunk feet match pivots: spring 25px, summer 27px, autumn 27px, winter 26px. PPU 82 gives an approximately 18m tree.

`lot-camera-{season}.png` are direct renders of the active Lot camera with a temporary SpriteRenderer using the production flora material. The temporary object and sprites were removed after capture; Lot session JSON was unchanged. Captures use current Lot lighting and do not include the placed-tree shadow presentation. Summer and winter were visually inspected for ortho-like stance, cutout behavior, and ground contact. These are visual previews, not a dense performance measurement.
