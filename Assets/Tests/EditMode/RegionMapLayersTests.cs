using CityForgeV3.UI;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

public class RegionMapLayersTests
{
    [Test] public void LayerChoicesAreIndependentAndPreserveExistingVisuals()
    {
        var root=new VisualElement();
        foreach(var id in new[]{"region-place-labels","region-district-labels","region-river-map-layer","region-topography-map-layer","region-transportation-map-layer","region-map-plane"})root.Add(new VisualElement{name=id});
        var tile=new Button();tile.AddToClassList("region-city-tile");root.Add(tile);
        var river=root.Q("region-river-map-layer");var settings=new RegionMapLayers();
        settings.Rivers=false;settings.TownsAndCities=false;CityForgeApp.ApplyRegionMapLayers(root,settings);
        Assert.AreEqual(DisplayStyle.None,river.style.display.value);
        Assert.AreEqual(DisplayStyle.Flex,root.Q("region-topography-map-layer").style.display.value);
        Assert.True(tile.ClassListContains("region-city-tile--map-hidden"));
        settings.DistrictNamesAndBorders=true;CityForgeApp.ApplyRegionMapLayers(root,settings);
        Assert.AreEqual(DisplayStyle.Flex,root.Q("region-district-labels").style.display.value);
        Assert.AreEqual(DisplayStyle.None,root.Q("region-place-labels").style.display.value);
        Assert.False(tile.ClassListContains("region-city-tile--map-hidden"));
        settings.Topography=false;settings.Rivers=true;CityForgeApp.ApplyRegionMapLayers(root,settings);
        Assert.AreSame(river,root.Q("region-river-map-layer"));
        Assert.AreEqual(DisplayStyle.Flex,river.style.display.value);
        Assert.AreEqual(DisplayStyle.None,root.Q("region-topography-map-layer").style.display.value);
        Assert.True(root.Q("region-map-plane").ClassListContains("region-map--neutral"));
    }
    [Test] public void OldRegionsDefaultToVisibleAndChoicesAndTransportSurviveReload()
    {
        var old=JsonUtility.FromJson<RegionSaveData>("{\"Name\":\"Older save\"}");
        Assert.True(old.MapLayers.TownsAndCities&&old.MapLayers.Rivers&&old.MapLayers.Topography&&old.MapLayers.Transportation);
        Assert.False(old.MapLayers.DistrictNamesAndBorders);
        old.MapLayers.DistrictNamesAndBorders=true;
        old.MapLayers.Rivers=false;old.TransportRoutes.Add(new(){Kind=RegionTransportKind.Rail,Points=new(){new Vector2(1,2),new Vector2(3,4)}});
        old.Tiles.Add(new(){Biome=RegionBiome.Desert});
        var loaded=JsonUtility.FromJson<RegionSaveData>(JsonUtility.ToJson(old));
        Assert.True(loaded.MapLayers.DistrictNamesAndBorders);
        Assert.False(loaded.MapLayers.Rivers);Assert.True(loaded.MapLayers.Transportation);
        Assert.AreEqual(RegionTransportKind.Rail,loaded.TransportRoutes[0].Kind);
        Assert.AreEqual(new Vector2(3,4),loaded.TransportRoutes[0].Points[1]);Assert.AreEqual(RegionBiome.Desert,loaded.Tiles[0].Biome);
    }
    [Test] public void ReliefPreviewIsBoundedAndUsesDistinctLandCoverColors()
    {
        var region=new RegionSaveData{Width=80,Height=64};var texture=RegionTopographyMapLayer.BuildTexture(region);
        try { Assert.LessOrEqual(texture.width,1024);Assert.LessOrEqual(texture.height,1024); }
        finally { Object.DestroyImmediate(texture); }
        Assert.AreNotEqual(RegionTopographyMapLayer.LandColor(RegionBiome.Desert),RegionTopographyMapLayer.LandColor(RegionBiome.Grassland));
        var tile=new RegionCityTile{Width=1,Height=1,Hills=new(){Mountains=true,HeightMeters=180}};
        var preview=new DistrictElevation(tile,20);var full=new DistrictElevation(tile);
        Assert.Less(preview.Heights.Length,full.Heights.Length);
        Assert.AreEqual(full.Sample(0,0),preview.Sample(0,0),.001f);
    }
}
