#if UNITY_EDITOR
using System;
using System.IO;
using CityForgeV3.World;
using UnityEditor;
public static class FarmerFieldQa
{
    [MenuItem("City Forge/QA/Farmer/Check Field Boundaries")]
    static void Check()
    {
        var flora=new[]{new PlacedFlora{FloraId="corn-field",PositionX=20,PositionZ=-10},new PlacedFlora{FloraId="maple",PositionX=0,PositionZ=0}};
        void Assert(bool value,string message){if(!value)throw new Exception(message);}
        Assert(LotWorldController.IsPointOnAgricultureField(flora,20,-10),"Inside field");
        Assert(LotWorldController.IsPointOnAgricultureField(flora,25.9f,-15.9f),"Inside field corner");
        Assert(!LotWorldController.IsPointOnAgricultureField(flora,26.1f,-10),"Outside field east");
        Assert(!LotWorldController.IsPointOnAgricultureField(flora,20,-16.1f),"Outside field south");
        Assert(!LotWorldController.IsPointOnAgricultureField(flora,0,0),"Tree is not a field");
        Assert(!LotWorldController.IsPointOnAgricultureField(Array.Empty<PlacedFlora>(),20,-10),"Removed field");
        Assert(!LotWorldController.IsPointOnAgricultureField(null,20,-10),"Missing flora list");
        Directory.CreateDirectory("QA/Farmer");File.WriteAllText("QA/Farmer/field-checks.txt","Passed: field interior, corner, two outside edges, nonagricultural flora, removed field, missing list.\n");
    }
}
#endif
