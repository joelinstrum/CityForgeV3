# Coal at hill feet v01

Automatically seeds one or two coal deposits when a hilly district is first built, including existing saved districts. Little River Bend generated two at normalized coordinates (.0546875,.8671875) and (.5703125,.3515625). Positions and IDs persist in RegionCityTile.ResourceDeposits; NaturalResourceGenerationKey records the terrain recipe. Changing the Hills settings reseeds; ordinary flora edits and reload retain positions. A flat district has no deposits. If no suitable clear pocket exists, placement returns fewer rather than occupying water or infrastructure.

Candidate centers use 4–18% of configured hill height, with a rise of at least12% within80m. Twelve-meter footprint samples avoid level buffers; initial flora clearance14m; deposits separated by at least25% of the shorter district dimension. Existing elevation corridors exclude roads/lots/rivers during initial generation. No existing flora removed. Later infrastructure may suppress a deposit flattened or submerged by terrain changes; this prototype does not yet reserve buildable space around deposits.

Coal is a16m-wide camera-facing sprite using the existing shared flora material, neutral time-of-day tint, depth testing and sorting conventions. Source image is preserved. Versioned transparent image derivative is installed at Resources/CityForgeV3/NaturalResources/CoalV01/coal-cutout.png. Actual RGBA,38.03% alpha0; first two imagegen attempts had baked checkerboards and were rejected. Final source-alpha retained. Prompt in prompt.txt. No new per-tree brightness overrides; accepted hills/overlay shader unchanged.

These are natural deposits, separate from resource inventory. No mining, depletion, worker assignment or coal credit is implemented yet.

Validation: runtime compilation passes. Real Little River Bend saved/reloaded through RegionSaveStore; entire district JSON matches, two rendered deposits, grounded and camera-aligned; flat fixture empty; flora-clear fixture retains saved positions; resource inventory unchanged. Existing hills QA verifies770flora, peak37.44m,1river1lot and collider. Both outcrops inspected in normal docked Unity Game view; broad context shows placement on lower hill shoulders. No broad EditMode suite/performance run claimed. User visual acceptance pending.

Editor menu City Forge / QA / Coal: Open Little River Bend, Close View, Second Deposit, District View, Check Saved Reload. These use the normal runtime camera. Main Unity left at close coal review, simulation paused. The second DryGoods review editor is untouched.
