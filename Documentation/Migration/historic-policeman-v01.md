# Historic Policeman V01

- Canonical intake archive: `Authoring/Props/HistoricPoliceman/v01_source/historic+police+officer+3d+model.zip`.
- Extracted source: `Authoring/Props/HistoricPoliceman/v01_source/raw/`, preserving the supplied FBX and five texture maps.
- Runtime identity: `historic-policeman-animated-v01`.
- Runtime model: `CityForgeV3/Props/Characters/HistoricPolicemanV01/HistoricPolicemanAnimatedV01`.
- Supplied animation families: idle, walk, run, wait, look around, angry, hit to body, and fall.
- Business-as-usual behavior: idle 65%, walk 20%, look around 10%, and wait 5%. Walking uses the established cardinal-heading, destination-driven character movement contract.
- Angry, hit-to-body, run, and fall are preserved for later hooligan/policeman proximity interactions and are not selected by BAU.
- Runtime material uses the supplied navy base color and normal map plus a City Forge metallic/smoothness derivative assembled from supplied metallic and inverted roughness maps.
- Human scale is normalized to 1.78 m at presentation time.
- The stable policeman identity is the interaction-archetype hook; proximity behavior remains intentionally deferred.
