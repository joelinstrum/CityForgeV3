# Hooligan V01

- Canonical intake archive: `Authoring/Props/Hooligan/v01_source/hooligan.zip`.
- Extracted source: `Authoring/Props/Hooligan/v01_source/raw/`, preserving the supplied FBX and five texture maps.
- Runtime identity: `hooligan-animated-v01`.
- Runtime model: `CityForgeV3/Props/Characters/HooliganV01/HooliganAnimatedV01`.
- Supplied animation families: idle, walk, and run. Run is retained for later authored interactions but is not part of business-as-usual behavior.
- Business-as-usual behavior: idle 85%, walk 15%. Walking uses the established cardinal-heading, destination-driven character movement contract.
- Runtime material preserves the supplied base color as canonical source and uses the UV-registered `base-color-brown.png` derivative in game. The derivative restores the intended brown outfit because the supplied base map is unusually low-chroma and reads gray under neutral outdoor lighting. Skin and light trim remain substantially protected. Normal mapping is supplied unchanged; metallic/smoothness is assembled from supplied metallic and inverted roughness maps.
- Human scale is normalized to 1.78 m at presentation time, matching the existing 3D character contract.
- Hooligan/hooligan and hooligan/policeman proximity interactions are intentionally deferred. The stable `hooligan-animated-v01` identity provides the archetype hook for those scripts.
