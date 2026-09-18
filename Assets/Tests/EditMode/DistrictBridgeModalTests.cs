using System;
using System.Linq;
using System.Reflection;
using CityForgeV3.UI;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

public sealed class DistrictBridgeModalTests
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    [Test]
    public void RoadCatalogOffersBothBridgeStylesAndCancelLeavesStateUntouched()
    {
        var host=new GameObject("Bridge modal test");host.SetActive(false);
        try
        {
            var app=host.AddComponent<CityForgeApp>();var root=new VisualElement();
            void Field(string name,object value)=>typeof(CityForgeApp).GetField(name,Flags).SetValue(app,value);
            void Call(string name,params object[] args)=>typeof(CityForgeApp).GetMethod(name,Flags).Invoke(app,args);
            var d=new RegionCityTile{TileId="bridge-modal"};var region=new RegionSaveData();region.Tiles.Add(d);
            Field("_root",root);Field("_openRegion",region);Field("_selectedRegionTileId",d.TileId);
            string before=JsonUtility.ToJson(d);
            Call("ComposeDistrictRoadFamilyModal");
            Assert.That(root.Query<Button>().ToList().Any(b=>b.text=="BRIDGES"),Is.True);
            Call("ComposeDistrictBridgeModal",new object[]{null});
            foreach(var style in DistrictBridgeCatalog.Styles)
                Assert.That(root.Query<Label>().ToList().Any(l=>l.text==style.Name.ToUpperInvariant()),Is.True);
            Assert.That(root.Query<Button>().ToList().Any(b=>b.text=="DRAW CROSSING"),Is.True);
            Call("RemoveDocumentModal");Assert.That(root.Q("document-modal"),Is.Null);
            Assert.That(JsonUtility.ToJson(d),Is.EqualTo(before));
        }
        finally{Object.DestroyImmediate(host);}
    }
}
