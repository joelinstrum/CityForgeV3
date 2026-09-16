#if UNITY_EDITOR
using System.IO;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
public static class NationalPikeArtReview
{
    public static void Capture()
    {
        var root=new GameObject("Pike art review");
        var target=new RenderTexture(1024,1024,24);
        var readback=new Texture2D(1024,1024,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        var materials=new System.Collections.Generic.List<Material>();
        try
        {
            var camera=root.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=12;
            camera.transform.position=new Vector3(0,30,0);camera.transform.rotation=Quaternion.Euler(90,0,0);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.20f,.29f,.17f);
            camera.cullingMask=1<<31;camera.targetTexture=target;
            var package=RoadPiecePackageCatalog.Resolve(RoadPiecePackageCatalog.NationalPikeDirtId);
            for(int i=0;i<4;i++)
            {
                var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.layer=31;quad.transform.SetParent(root.transform,false);
                // Camera is parent only for cleanup; set world positions explicitly.
                quad.transform.position=new Vector3(i%2==0?-5.5f:5.5f,0,i<2?5.5f:-5.5f);
                quad.transform.rotation=Quaternion.Euler(90,0,0);quad.transform.localScale=new Vector3(10,10,1);
                var m=new Material(Shader.Find("CityForgeV3/ShadowReceivingRoadOverlay"));materials.Add(m);
                m.mainTexture=Resources.Load<Texture2D>(package.Piece((RoadPieceTopology)i).ResourcePath);
                m.SetTexture("_DirtStraightTex",Resources.Load<Texture2D>(package.Piece(RoadPieceTopology.Straight).ResourcePath));
                m.SetFloat("_DirtTopology",i);m.SetFloat("_ReceiveSunShadow",0);quad.GetComponent<Renderer>().sharedMaterial=m;
            }
            camera.Render();RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,1024,1024),0,0);readback.Apply();
            Directory.CreateDirectory("QA/NationalPike");File.WriteAllBytes("QA/NationalPike/dirt-pieces.png",readback.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous;Object.DestroyImmediate(root);Object.DestroyImmediate(target);Object.DestroyImmediate(readback);
            foreach(var material in materials)Object.DestroyImmediate(material);
        }
    }
}
#endif
