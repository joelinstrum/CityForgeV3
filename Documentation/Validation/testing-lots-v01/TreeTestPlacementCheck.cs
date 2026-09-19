using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEngine;
public static class TreeTestPlacementCheck
{
 public static void Run()
 {
  var path = Path.GetFullPath("tree-test-fixture.json");
  var lot = JsonUtility.FromJson<LotSaveData>(File.ReadAllText(path));
  var flags = BindingFlags.NonPublic | BindingFlags.Static;
  var catalog = typeof(LotContentCatalog);
  var byId = catalog.GetField("_byId", flags); var all = catalog.GetField("_all", flags);
  var oldById = byId.GetValue(null); var oldAll = all.GetValue(null);
  var go = new GameObject("Tree Test scratch placement"); go.SetActive(false);
  try
  {
   var sourceType = catalog.GetNestedType("Source", BindingFlags.NonPublic);
   var source = Activator.CreateInstance(sourceType, true);
   sourceType.GetField("JsonPath").SetValue(source, path);
   var map = (IDictionary)Activator.CreateInstance(byId.FieldType); map.Add(lot.LotId, source);
   byId.SetValue(null, map); all.SetValue(null, new List<LotSaveSummary>());
   var app = go.AddComponent<CityForgeApp>();
   var instance = BindingFlags.Instance | BindingFlags.NonPublic;
   foreach (var pos in new[] { .1f, .5f, .9f })
   {
    var district = new RegionCityTile { TileId = "scratch", Width = 4, Height = 4, Treasury = 0 };
    var region = new RegionSaveData(); region.Tiles.Add(district);
    typeof(CityForgeApp).GetField("_openRegion", instance).SetValue(app, region);
    typeof(CityForgeApp).GetField("_selectedRegionTileId", instance).SetValue(app, district.TileId);
    typeof(CityForgeApp).GetField("_pendingDistrictLotId", instance).SetValue(app, lot.LotId);
    typeof(CityForgeApp).GetField("_pendingDistrictLotIsTest", instance).SetValue(app, true);
    typeof(CityForgeApp).GetMethod("PlaceDistrictLot", instance).Invoke(app, new object[] { district, pos, pos });
    if (district.Lots.Count != 1 || district.Lots[0].LotId != lot.LotId || district.Treasury != 0)
     throw new Exception("Tree Test model placement failed at " + pos);
   }
   File.WriteAllText("tree-test-placement-check.txt", "PASS: read-only copy of Tree Test placed into three isolated in-memory districts at normalized 0.1, 0.5 and 0.9 with zero treasury and no charge. No world rendering or player saves.\n");
  }
  finally { byId.SetValue(null, oldById); all.SetValue(null, oldAll); UnityEngine.Object.DestroyImmediate(go); }
 }
}
