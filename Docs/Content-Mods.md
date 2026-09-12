# CityForge content manifests

CityForge discovers mod packages below `Application.persistentDataPath/Mods`.
Content IDs are permanent save-file identities and must be globally unique.
Invalid manifests, duplicate IDs, and paths that escape a mod directory are
logged and skipped without disabling built-in content.

## Buildings

Each mod may provide `building-content.json` using schema
`cityforge-building-content-v1`. Runtime models and thumbnails are loaded from
a Unity AssetBundle; built-in content uses the same entry contract with the
`resources` provider.

```json
{
  "schema": "cityforge-building-content-v1",
  "buildings": [{
    "id": "author.mod.example-house.v1",
    "displayName": "Example House",
    "category": "Residential",
    "subcategory": "Low Wealth",
    "description": "A compact example residence",
    "provider": "asset-bundle",
    "bundlePath": "content.bundle",
    "modelAssetName": "Assets/ExampleHouse.prefab",
    "thumbnailAssetName": "Assets/ExampleHouse.png",
    "pitchDegrees": 0,
    "baseYawDegrees": 90,
    "normalizeAxis": "height",
    "normalizeMeters": 8,
    "materialMode": "embedded",
    "plopCost": 125,
    "constructionRequirements": [
      { "resourceId": "lumber", "amount": 8 }
    ],
    "operatingInputs": [
      { "resourceId": "food", "amount": 1, "interval": "year" }
    ],
    "operatingOutputs": [],
    "effects": [
      { "effectId": "housing-capacity", "magnitude": 6, "target": "district" }
    ],
    "construction": {
      "mode": "mesh-height",
      "secondsPerStage": 1,
      "stageNames": ["foundation", "structure", "finish"]
    }
  }]
}
```

Economy and construction fields are optional. Missing values mean zero cost,
no resource flow, no effects, and the default mesh-height construction reveal.
The catalog stores these contracts now; resource inventory and simulation rules
may consume them later without changing content IDs or save-file references.
`rendererGroups` can optionally name authored prefab groups for unusually shaped
assets. It never implies windows, floors, or another architectural feature.

Buildings are visible in both consumers unless `hideFromLotEditor` or
`hideFromDistrictBuilder` is explicitly true. A composed lot records only the
stable building ID; the provider resolves its model when the lot is opened in
the Lot Editor or instantiated in a district.

A lot save may set `BasePlopCost`. Its displayed and charged plop cost is that
base plus the `plopCost` of every placed 3D building. Construction requirements
are aggregated the same way, but are not enforced until district resource
inventories are implemented.

## Lots

Each mod may provide `lot-content.json`. The referenced lot file uses the
ordinary versioned CityForge lot-save schema and declares its building/prop
package dependencies.

```json
{
  "schema": "cityforge-lot-content-v1",
  "lots": [{
    "id": "author.mod.example-farm.v1",
    "lotFile": "lots/example-farm.json",
    "previewFile": "lots/example-farm.png"
  }]
}
```

The District Builder merges these packages with locally saved Lot Editor lots.
It does not special-case filenames, categories, buildings, or mod authors.
