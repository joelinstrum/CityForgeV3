using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    [Serializable]
    public sealed class PlacedDistrictBridge
    {
        public string Id = "";
        public string StyleId = "covered-wood";
        public string RoadFamily = DistrictRoadPlacementModel.AntiqueBrickFamily;
        public Vector2Int Start;
        public Vector2Int End;
        public float DeckHeight;
        public float StartHeight;
        public float EndHeight;
        public int Cost;
        public bool FixedModel;
        public float NearApproach, FarApproach, EndDeckOffset;
    }

    public sealed class DistrictBridgeStyle
    {
        public readonly string Id, Name, Resource, Description;
        public readonly int BaseCost, CostPerMeter;
        public DistrictBridgeStyle(string id, string name, string resource, string description, int baseCost, int costPerMeter)
        { Id=id; Name=name; Resource=resource; Description=description; BaseCost=baseCost; CostPerMeter=costPerMeter; }
        public int Price(float length) => BaseCost + Mathf.CeilToInt(length) * CostPerMeter;
    }

    public static class DistrictBridgeCatalog
    {
        public static readonly DistrictBridgeStyle[] Styles = {
            new("covered-wood", "Covered Wooden Bridge", "CityForgeV3/Bridges/CoveredWoodenV01",
                "A sheltered timber crossing with repeating roof bays and river piers.", 800, 35),
            new("stone-original", "Stone Arch Bridge", "CityForgeV3/Bridges/StoneOriginalV01",
                "Original complete stone bridge. Fixed 34-meter model; requires dry banks at both ends.", 1600, 60),
            new("stone-long", "Long Stone Arch Bridge", "CityForgeV3/Bridges/StoneLongOriginalV01",
                "Original complete long stone bridge. Fixed 50-meter model; requires dry banks at both ends.", 2400, 60)
        };
        public static DistrictBridgeStyle Find(string id)
        { foreach(var style in Styles) if(style.Id==id) return style;
            return id=="stone"?new DistrictBridgeStyle("stone","Legacy Modular Stone Bridge","CityForgeV3/Bridges/StoneV02","Previously placed modular crossing",1600,60):null; }
    }

    // Pure, bounded placement. Coordinates/elevations are district-local meters.
    // The caller supplies indexed river/occupancy queries; no district collections are enumerated here.
    public static class DistrictBridgePlanner
    {
        public const float HalfWidth = 4.5f;
        public const float ApproachHalfWidth = 7.5f;
        public const float TravelHalfWidth = 2.5f;
        public const float RampLength = 8f;
        public const float MaxLength = 240f;
        public const float MinLength = 50f;
        public readonly struct Surface
        {
            public readonly bool Channel, Water;
            public readonly float Ground, WaterHeight;
            public Surface(bool channel, bool water, float ground, float waterHeight)
            { Channel=channel; Water=water; Ground=ground; WaterHeight=waterHeight; }
        }
        public static Vector2 Center(RegionCityTile d, Vector2Int cell) => new(
            (cell.x+.5f)*10-DistrictScale.SizeMeters(d.Width)*.5f,
            (cell.y+.5f)*10-DistrictScale.SizeMeters(d.Height)*.5f);
        public static Rect Bounds(RegionCityTile d, PlacedDistrictBridge b)
        {
            var a=Center(d,b.Start);var z=Center(d,b.End);
            return Rect.MinMaxRect(Mathf.Min(a.x,z.x)-ApproachHalfWidth,Mathf.Min(a.y,z.y)-ApproachHalfWidth,
                Mathf.Max(a.x,z.x)+ApproachHalfWidth,Mathf.Max(a.y,z.y)+ApproachHalfWidth);
        }
        public static bool Contains(RegionCityTile d, PlacedDistrictBridge b, Vector2 p, float halfWidth, out float along)
        {
            var a=Center(d,b.Start);var delta=Center(d,b.End)-a;var length=delta.magnitude;
            if(length<1){along=0;return false;}
            var axis=delta/length;along=Vector2.Dot(p-a,axis);
            if(halfWidth>=HalfWidth && (along<=(b.FixedModel?b.NearApproach:RampLength) || along>=length-(b.FixedModel?b.FarApproach:RampLength)))halfWidth=Mathf.Max(halfWidth,ApproachHalfWidth);
            return along>=0 && along<=length && Mathf.Abs((p.x-a.x)*axis.y-(p.y-a.y)*axis.x)<=halfWidth;
        }
        public static float Height(RegionCityTile d, PlacedDistrictBridge b, float along)
        {
            var length=Vector2.Distance(Center(d,b.Start),Center(d,b.End));
            if(b.FixedModel)
            {
                if(along<b.NearApproach)return Mathf.Lerp(b.StartHeight,b.DeckHeight,along/b.NearApproach);
                if(along>length-b.FarApproach)return Mathf.Lerp(b.DeckHeight+b.EndDeckOffset,b.EndHeight,(along-length+b.FarApproach)/b.FarApproach);
                return b.DeckHeight;
            }
            if(along<RampLength)return Mathf.Lerp(b.StartHeight,b.DeckHeight,along/RampLength);
            if(along>length-RampLength)return Mathf.Lerp(b.DeckHeight,b.EndHeight,(along-length+RampLength)/RampLength);
            return b.DeckHeight;
        }
        public static bool TryFitFixed(RegionCityTile d,PlacedDistrictBridge crossing,float bodyLength,float endRise,
            Func<Vector2,Surface> sample,Func<Vector2,bool> occupied,out PlacedDistrictBridge fitted,out string reason)
        {
            fitted=null;reason="This bridge cannot reach dry ground on both banks.";
            var a=Center(d,crossing.Start);var z=Center(d,crossing.End);var axis=(z-a).normalized;
            if(axis.sqrMagnitude<.5f)return false;
            var side=new Vector2(-axis.y,axis.x);
            float crossingLength=Vector2.Distance(a,z),firstWater=float.PositiveInfinity,lastWater=float.NegativeInfinity;
            if(crossingLength>MaxLength || bodyLength<=0 || bodyLength>MaxLength)return false;
            int waterStations=Mathf.CeilToInt(crossingLength/2f);
            for(int i=0;i<=waterStations;i++)for(int lane=-2;lane<=2;lane++)
            {
                float along=crossingLength*i/waterStations;
                if(sample(a+axis*along+side*(lane*HalfWidth*.5f)).Water)
                {firstWater=Mathf.Min(firstWater,along);lastWater=Mathf.Max(lastWater,along);}
            }
            if(float.IsPositiveInfinity(firstWater))return false;
            var center=a+axis*((firstWater+lastWater)*.5f);
            var near=center-axis*bodyLength*.5f;var far=center+axis*bodyLength*.5f;
            for(int lane=-2;lane<=2;lane++)
                if(sample(near+side*(lane*HalfWidth*.5f)).Water || sample(far+side*(lane*HalfWidth*.5f)).Water)return false;
            var direction=new Vector2Int(Math.Sign(crossing.End.x-crossing.Start.x),Math.Sign(crossing.End.y-crossing.Start.y));
            var start=crossing.Start;var end=crossing.End;
            bool Inside(Vector2Int cell)=>cell.x>=1&&cell.y>=1&&cell.x<DistrictScale.Columns(d.Width)-1&&cell.y<DistrictScale.Columns(d.Height)-1;
            float nearLength=0,farLength=0,ah=0,zh=0;
            for(int n=0;n<26;n++)
            {
                a=Center(d,start);z=Center(d,end);nearLength=Vector2.Dot(near-a,axis);farLength=Vector2.Dot(z-far,axis);
                ah=sample(a).Ground+.152f;zh=sample(z).Ground+.152f;
                bool nearOk=nearLength>=RampLength&&Mathf.Abs(crossing.DeckHeight-ah)<=nearLength*.2f;
                bool farOk=farLength>=RampLength&&Mathf.Abs(crossing.DeckHeight+endRise-zh)<=farLength*.2f;
                if(nearOk&&farOk)break;
                if(!nearOk)start-=direction;if(!farOk)end+=direction;
                if(!Inside(start)||!Inside(end))return false;
                if(n==25)return false;
            }
            float total=Vector2.Distance(a,z);if(total>MaxLength||!Inside(start)||!Inside(end))return false;
            int stations=Mathf.CeilToInt(total/2f);
            for(int i=0;i<=stations;i++)for(int lane=-2;lane<=2;lane++)
            {
                float along=total*i/stations;bool approach=along<=nearLength||along>=total-farLength;
                var p=a+axis*along+side*(lane*(approach?ApproachHalfWidth:HalfWidth)*.5f);
                if(occupied(p)){reason="Clear the bridge and approach footprint first.";return false;}
                if(approach&&sample(p).Water)return false;
            }
            fitted=JsonUtility.FromJson<PlacedDistrictBridge>(JsonUtility.ToJson(crossing));
            fitted.Start=start;fitted.End=end;fitted.StartHeight=ah;fitted.EndHeight=zh;
            fitted.FixedModel=true;fitted.NearApproach=nearLength;fitted.FarApproach=farLength;fitted.EndDeckOffset=endRise;
            reason="";return true;
        }
        public static bool TryPlan(RegionCityTile d, Vector2Int bank, Vector2Int direction,
            Func<Vector2,Surface> sample, Func<Vector2,bool> occupied,
            out PlacedDistrictBridge bridge, out string reason)
        {
            bridge=null;reason="";
            if(direction==Vector2Int.zero || Mathf.Abs(direction.x)>1 || Mathf.Abs(direction.y)>1)
            {reason="Drag a straight road toward the opposite bank.";return false;}
            int cols=DistrictScale.Columns(d.Width),rows=DistrictScale.Columns(d.Height);
            bool InBounds(Vector2Int c)=>c.x>=1&&c.y>=1&&c.x<cols-1&&c.y<rows-1;
            var axis=((Vector2)direction).normalized;var side=new Vector2(-axis.y,axis.x);
            bool Channel(Vector2Int c)
            {
                var p=Center(d,c);
                return sample(p).Channel || sample(p+side*HalfWidth).Channel || sample(p-side*HalfWidth).Channel;
            }
            bool ApproachDry(Vector2Int cell,bool near)
            {
                var origin=Center(d,cell);
                for(int station=0;station<=4;station++)
                for(int lane=-2;lane<=2;lane++)
                {
                    var point=origin+axis*(near?1:-1)*(RampLength*station/4f)+side*(lane*ApproachHalfWidth*.5f);
                    if(sample(point).Water)return false;
                }
                return true;
            }
            var start=bank;
            // Find bank anchors with a dry approach wide enough for the deck.
            int retreat=0;
            while(Channel(start) && retreat++<8)start-=direction;
            for(int i=0;i<4 && InBounds(start) && !ApproachDry(start,true);i++)start-=direction;
            if(!InBounds(start)||!ApproachDry(start,true)){reason="There is not enough dry ground for the near approach.";return false;}
            var end=bank;bool water=false,channel=false;int steps=0;
            while(steps++<26)
            {
                end+=direction;if(!InBounds(end))break;
                float length=Vector2.Distance(Center(d,start),Center(d,end));
                if(length>MaxLength)break;
                var surface=sample(Center(d,end));water|=surface.Water;channel|=Channel(end);
                if(channel && !Channel(end))
                {
                    for(int i=0;i<4 && InBounds(end) && !ApproachDry(end,false);i++)end+=direction;
                    while(InBounds(end) && Vector2.Distance(Center(d,start),Center(d,end))<MinLength)end+=direction;
                    break;
                }
            }
            var a=Center(d,start);var z=Center(d,end);var span=Vector2.Distance(a,z);
            if(!InBounds(end)||span>MaxLength||Channel(end)||span<MinLength)
            {reason="No suitable opposite bank within 240 meters. Try crossing more directly.";return false;}
            float highWater=float.NegativeInfinity;float highGround=float.NegativeInfinity;
            int samples=Mathf.CeilToInt(span/2);
            for(int i=0;i<=samples;i++)
            {
                float along=span*i/samples;var p=Vector2.Lerp(a,z,(float)i/samples);
                for(int lane=-2;lane<=2;lane++)
                {
                    float corridor=(along<=RampLength||along>=span-RampLength)?ApproachHalfWidth:HalfWidth;
                    var q=p+side*(lane*corridor*.5f);var s=sample(q);
                    if(occupied(q)){reason="Clear buildings or an existing bridge from this crossing.";return false;}
                    if((along<RampLength || along>span-RampLength) && s.Water)
                    {reason="The approach needs more dry land. Try a straighter crossing.";return false;}
                    if(s.Water){water=true;highWater=Mathf.Max(highWater,s.WaterHeight);}
                    if(!s.Channel && along>=RampLength && along<=span-RampLength)highGround=Mathf.Max(highGround,s.Ground+.152f);
                }
            }
            if(!water){reason="The route does not cross river water.";return false;}
            float ah=sample(a).Ground+.152f,zh=sample(z).Ground+.152f;
            float deck=Mathf.Max(Mathf.Max(ah,zh),Mathf.Max(highGround,highWater+1.5f));
            if(deck-ah>RampLength*.2f || deck-zh>RampLength*.2f)
            {reason="The banks are too uneven for safe approaches. Level the banks or try another crossing.";return false;}
            bridge=new PlacedDistrictBridge { Id=Guid.NewGuid().ToString("N"), Start=start,End=end,
                StartHeight=ah,EndHeight=zh,DeckHeight=deck };
            return true;
        }
    }
}
