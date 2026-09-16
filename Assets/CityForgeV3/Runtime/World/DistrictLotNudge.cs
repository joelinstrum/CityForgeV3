using System;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    [Serializable] public sealed class DistrictLotNudge
    {
        public DistrictSelectionKind Kind;
        public string Id;
        public Rect TileBounds;
        public Vector2 Offset;
        public static Vector2 GetOffset(RegionCityTile district,DistrictSelectionKind kind,string id) =>
            district?.LotNudges?.FirstOrDefault(n=>n.Kind==kind&&n.Id==id)?.Offset ?? Vector2.zero;
        public static Rect EnclosingTiles(Rect bounds,float width,float depth)
        {
            float cell=DistrictScale.CellSizeMeters;
            return Rect.MinMaxRect(
                Mathf.Max(-width/2,Mathf.Floor((bounds.xMin+width/2+.001f)/cell)*cell-width/2),
                Mathf.Max(-depth/2,Mathf.Floor((bounds.yMin+depth/2+.001f)/cell)*cell-depth/2),
                Mathf.Min(width/2,Mathf.Ceil((bounds.xMax+width/2-.001f)/cell)*cell-width/2),
                Mathf.Min(depth/2,Mathf.Ceil((bounds.yMax+depth/2-.001f)/cell)*cell-depth/2));
        }
        public static Vector2 ClampDelta(Rect bounds,Rect tiles,Vector2 delta)
        {
            float minX=tiles.xMin-bounds.xMin,maxX=tiles.xMax-bounds.xMax;
            float minZ=tiles.yMin-bounds.yMin,maxZ=tiles.yMax-bounds.yMax;
            return new Vector2(minX<=maxX?Mathf.Clamp(delta.x,minX,maxX):0,minZ<=maxZ?Mathf.Clamp(delta.y,minZ,maxZ):0);
        }
    }
}
