using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    // Authored against the five summer cutouts. Each trunk has a separate ground
    // anchor; hidden rear trunks are estimated from the visible tree's axis.
    // Values are normalized image coordinates (top-left origin), width and height.
    public static class ForestClusterShadows
    {
        static readonly Vector4[][] Trees =
        {
            new[] { new Vector4(.38f,.66f,.31f,.64f), new Vector4(.65f,.68f,.27f,.53f), new Vector4(.30f,.83f,.45f,.53f), new Vector4(.71f,.81f,.40f,.46f), new Vector4(.52f,.95f,.30f,.31f) },
            new[] { new Vector4(.36f,.70f,.36f,.67f), new Vector4(.67f,.67f,.42f,.59f), new Vector4(.20f,.84f,.34f,.39f), new Vector4(.70f,.83f,.44f,.48f), new Vector4(.42f,.94f,.36f,.44f) },
            new[] { new Vector4(.53f,.65f,.45f,.63f), new Vector4(.82f,.75f,.31f,.67f), new Vector4(.26f,.75f,.47f,.57f), new Vector4(.42f,.93f,.29f,.47f), new Vector4(.66f,.90f,.32f,.41f) },
            new[] { new Vector4(.33f,.65f,.40f,.64f), new Vector4(.56f,.69f,.29f,.58f), new Vector4(.78f,.76f,.40f,.51f), new Vector4(.29f,.91f,.42f,.48f), new Vector4(.63f,.95f,.36f,.46f) },
            new[] { new Vector4(.28f,.65f,.32f,.61f), new Vector4(.62f,.63f,.49f,.56f), new Vector4(.22f,.89f,.33f,.54f), new Vector4(.77f,.86f,.40f,.44f), new Vector4(.47f,.91f,.33f,.48f) }
        };
        static readonly int[] FirMasks = { 3, 1, 10, 2, 5 };

        public static bool Update(SpriteRenderer source, MeshRenderer shadow, Vector3 ray,
            Func<Vector3, float> groundHeight, Func<Vector3, Vector3> groundAnchor)
        {
            string name = source.sprite.texture.name;
            if (!ForestClusterCatalog.IsTexture(name)) return false;
            int variant = name[16] - '1'; // forest-cluster-01-summer
            if (variant < 0 || variant >= Trees.Length) return false;
            var vertices = new List<Vector3>(200);
            var colors = new List<Color>(200);
            var indices = new List<int>(600);
            var sprite = source.sprite;
            var size = sprite.rect.size / sprite.pixelsPerUnit;
            var pivot = sprite.pivot / sprite.pixelsPerUnit;
            var right = source.transform.right; right.y = 0; right.Normalize();
            var scale = source.transform.lossyScale;
            float verticalProjection = Mathf.Max(.2f, source.transform.up.y);
            Vector3 Cast(Vector3 point, float height)
            {
                point += new Vector3(ray.x, 0, ray.z) * (height / Mathf.Max(.05f, -ray.y));
                point.y = groundHeight(point) + .031f;
                return shadow.transform.InverseTransformPoint(point);
            }
            void Vertex(Vector3 point, float height, float opacity)
            {
                vertices.Add(Cast(point, height)); colors.Add(new Color(opacity, 1, 1, .4f));
            }
            for (int tree = 0; tree < 5; tree++)
            {
                var t = Trees[variant][tree];
                // Intersect the camera ray through the pictured trunk foot with
                // its ground plane, so the shadow touches that foot on screen.
                var foot = source.transform.TransformPoint(new Vector3(t.x * size.x - pivot.x,
                    (1 - t.y) * size.y - pivot.y, 0));
                foot = groundAnchor(foot);
                float height = t.w * size.y * scale.y / verticalProjection;
                float width = t.z * size.x * scale.x;
                bool fir = (FirMasks[variant] & (1 << tree)) != 0;
                int start = vertices.Count;
                // Continuous trunk joins the contact point to the canopy.
                float trunk = width * .035f;
                Vertex(foot - right * trunk, 0, .85f); Vertex(foot + right * trunk, 0, .85f);
                Vertex(foot + right * trunk, height * .65f, .7f); Vertex(foot - right * trunk, height * .65f, .7f);
                indices.AddRange(new[] { start, start+1, start+2, start, start+2, start+3 });
                const int sides = 16;
                start = vertices.Count;
                float centerHeight = height * (fir ? .49f : .61f);
                Vertex(foot, centerHeight, .8f);
                for (int ring=0; ring<2; ring++)
                for (int i=0; i<sides; i++)
                {
                    float a = i * Mathf.PI * 2 / sides;
                    float y = Mathf.Sin(a);
                    float crownWidth = fir ? (1f-y)*.48f : 1f;
                    float irregular = 1 + .12f * Mathf.Sin(i * 2.7f + tree);
                    float radius = ring == 0 ? .84f : 1f;
                    float x = Mathf.Cos(a) * width * .5f * crownWidth * irregular * radius;
                    float h = centerHeight + y * height * (fir ? .49f : .36f) * radius;
                    Vertex(foot + right * x, h, ring == 0 ? .8f : 0);
                }
                for (int i=0; i<sides; i++)
                {
                    int a=start+1+i, b=start+1+(i+1)%sides, c=a+sides, d=b+sides;
                    indices.AddRange(new[] {start,a,b,a,c,d,a,d,b});
                }
            }
            var mesh = shadow.GetComponent<MeshFilter>().sharedMesh;
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetColors(colors);
            mesh.SetUVs(0, new Vector2[vertices.Count]); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
            return true;
        }
    }
}
