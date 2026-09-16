using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    // One bounded preview texture per open map. Visibility changes never regenerate it.
    public sealed class RegionTopographyMapLayer : VisualElement
    {
        Texture2D texture;
        public RegionTopographyMapLayer(RegionSaveData region, float unit)
        {
            name = "region-topography-map-layer"; pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute; style.left = 0; style.top = 0;
            style.width = region.Width * unit; style.height = region.Height * unit;
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (texture != null) return;
                texture = BuildTexture(region); style.backgroundImage = new StyleBackground(texture);
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                style.backgroundImage = StyleKeyword.None;
                if (texture != null) { if (Application.isPlaying) Object.Destroy(texture); else Object.DestroyImmediate(texture); }
                texture = null;
            });
        }

        // Mean sRGB of DistrictWorldController.DefaultGrassResource (73, 89, 36).
        // Use its authored color without adding repeating ground detail to the map.
        public static readonly Color DefaultGrassColor = new Color32(73, 89, 36, 255);

        public static Color LandColor(RegionBiome biome) => biome switch
        {
            RegionBiome.Desert => new Color(.76f,.65f,.42f),
            RegionBiome.Forest => new Color(.26f,.40f,.29f),
            RegionBiome.Snow => new Color(.84f,.88f,.85f),
            _ => DefaultGrassColor
        };

        public static Texture2D BuildTexture(RegionSaveData region)
        {
            float density = Mathf.Min(32, 1024f / Mathf.Max(1, region.Width, region.Height));
            int width = Mathf.Max(1, Mathf.CeilToInt(region.Width * density));
            int height = Mathf.Max(1, Mathf.CeilToInt(region.Height * density));
            var colors = new Color32[width * height];
            for (int i=0; i<colors.Length; i++) colors[i] = LandColor(RegionBiome.Grassland);
            foreach (var tile in region.Tiles)
            {
                int x0 = Mathf.Clamp(Mathf.RoundToInt(tile.X*density),0,width), x1 = Mathf.Clamp(Mathf.RoundToInt((tile.X+tile.Width)*density),0,width);
                int z0 = Mathf.Clamp(Mathf.RoundToInt(tile.Y*density),0,height), z1 = Mathf.Clamp(Mathf.RoundToInt((tile.Y+tile.Height)*density),0,height);
                bool relief = tile.Hills != null && tile.Hills.HeightMeters > 0;
                var elevation = relief ? new DistrictElevation(tile, 20) : null;
                float metersX=DistrictScale.SizeMeters(tile.Width), metersZ=DistrictScale.SizeMeters(tile.Height);
                for(int z=z0;z<z1;z++)for(int x=x0;x<x1;x++)
                {
                    var color=LandColor(tile.Biome);
                    if (elevation != null)
                    {
                        float px=((x+.5f-x0)/Mathf.Max(1,x1-x0)-.5f)*metersX;
                        float pz=((z+.5f-z0)/Mathf.Max(1,z1-z0)-.5f)*metersZ;
                        float h=elevation.Sample(px,pz), dx=elevation.Sample(px+20,pz)-elevation.Sample(px-20,pz), dz=elevation.Sample(px,pz+20)-elevation.Sample(px,pz-20);
                        var normal=new Vector3(-dx,40,-dz).normalized;
                        float light=Mathf.Clamp(.65f+Vector3.Dot(normal,new Vector3(-.6f,.8f,-.4f).normalized)*.45f,.4f,1.15f);
                        color=Color.Lerp(color,new Color(.63f,.59f,.47f),Mathf.InverseLerp(20,110,h));
                        color=Color.Lerp(color,new Color(.85f,.85f,.78f),Mathf.InverseLerp(120,240,h));
                        color*=light; color.a=1;
                    }
                    colors[(height-1-z)*width+x]=color;
                }
            }
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false) { name="Region relief preview", filterMode=FilterMode.Bilinear, wrapMode=TextureWrapMode.Clamp };
            texture.SetPixels32(colors); texture.Apply(false,true); return texture;
        }
    }
}
