using System;
using UnityEngine;
namespace CityForgeV3.World
{
    // Model measurements live here; all variants share the same driving controller.
    [Serializable]
    public sealed class HorseWagonDefinition
    {
        public string DisplayName="Horse and carriage";
        public string ResourcePath="CityForgeV3/Vehicles/CarriageV02/Carriage_Articulated_v02";
        public string TexturePath="CityForgeV3/Vehicles/CarriageV01/base-color";
        public float Height=2.644676f, HorseOffset=2.05f, FrontAxleOffset=.176f, RearAxleOffset=2.0295f;
        public float TurningRadius=3.25f, Width=1.8f, TeamLength=6.5f, Tint=.7f;
        public Vector3 ShaftAnchor=new Vector3(.37f,.98f,2.50f);
        public Vector3 DriverHands=new Vector3(.16f,1.87f,.10f);
        public Vector2 ShadowSize=new Vector2(2.05f,3.55f);
        public float ShadowCenterZ=-.75f;
        public int HorseCount=1;
        public float HorseSpacing=1f;
        public float ShaftLength=>HorseOffset-FrontAxleOffset;
        public float Wheelbase=>RearAxleOffset+FrontAxleOffset;
        public static readonly HorseWagonDefinition Carriage=new();
        public static readonly HorseWagonDefinition Lumber=new()
        {
            DisplayName="Horse and lumber wagon",
            ResourcePath="CityForgeV3/Vehicles/LumberWagonV01/Lumber_Wagon_Articulated_v01",
            TexturePath="CityForgeV3/Vehicles/LumberWagonV01/base-color",
            Height=2.4001557f,HorseOffset=2.12f,FrontAxleOffset=.0702f,RearAxleOffset=1.8468f,
            Width=2f,TeamLength=6.6f,Tint=.8f,
            ShaftAnchor=new Vector3(.44f,.81f,2.54f),DriverHands=new Vector3(.17f,1.63f,.57f),
            ShadowSize=new Vector2(2.1f,3.3f),ShadowCenterZ=-.95f
        };
        public static readonly HorseWagonDefinition Covered=new()
        {
            DisplayName="Two horses and covered wagon",HorseCount=2,HorseSpacing=1f,
            ResourcePath="CityForgeV3/Vehicles/CoveredWagonV01/Covered_Wagon_Articulated_v01",
            TexturePath="CityForgeV3/Vehicles/CoveredWagonV01/base-color",
            Height=2.7773728f,HorseOffset=2.45f,FrontAxleOffset=-.16775f,RearAxleOffset=2.29665f,
            Width=2.15f,TeamLength=7.3f,Tint=.8f,TurningRadius=3.75f,
            ShaftAnchor=new Vector3(.35f,.92f,2.87f),DriverHands=new Vector3(.16f,1.85f,.15f),
            ShadowSize=new Vector2(2.25f,3.8f),ShadowCenterZ=-1.4f
        };
        public static readonly HorseWagonDefinition Food=new()
        {
            DisplayName="Horse and food wagon",
            ResourcePath="CityForgeV3/Vehicles/FoodWagonV01/Food_Wagon_Articulated_v01",
            TexturePath="CityForgeV3/Vehicles/FoodWagonV01/base-color",
            Height=2.301941f,HorseOffset=2.02f,FrontAxleOffset=.04f,RearAxleOffset=1.69f,
            Width=2.05f,TeamLength=6.5f,Tint=.8f,
            ShaftAnchor=new Vector3(.40f,.88f,2.36f),DriverHands=new Vector3(.16f,1.68f,.52f),
            ShadowSize=new Vector2(2.15f,3.2f),ShadowCenterZ=-.95f
        };
        public static HorseWagonDefinition For(string propId)=>propId switch
        {
            LotWorldController.HorseFoodWagonPropId=>Food,
            LotWorldController.HorseCoveredWagonPropId=>Covered,
            LotWorldController.HorseLumberWagonPropId=>Lumber,
            _=>Carriage
        };
    }
}
