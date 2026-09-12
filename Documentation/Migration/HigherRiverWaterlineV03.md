# Higher river waterline — 2026-09-09

Joel accepted the corrected bank lighting and requested higher water with only a small amount of bank showing.

Changed default Water Height from -0.9 to -0.05. The water mesh now reaches the intersection of its elevation with the existing staged bank profile, minus a 0.10 m inset. Raising the surface therefore expands its footprint over the submerged slopes. The existing independent 88–100% shoreline modulation remains, now bounded by the bank intersection rather than the old fixed 78%-of-dirt-width envelope. Water below the channel floor produces no surface. No terrain/bank vertices, river path, textures, materials or shaders changed. The v02 normal correction remains intact.

Validation in the normal docked Game view: shallow fixture surface elevation 0.1615 versus terrain 0.184. 371 cross-sections; water shore reaches 80.8–91.8% of the full bank half-width, leaving about 8–19% exposed per side across broad variations. Both shores remain independently shaped; zero bank-envelope violations. Runtime and editor compilation passed. Screenshot game-view.jpg shows substantially reduced exposed bank. Existing transient river QA fixture used, no saved region/lot written.

Water Height continues to control elevation; surface width now follows the bank profile at that height. Latest shoreline QA compares against the full bank footprint; old v01 fixed-water-envelope measurements are historical.
