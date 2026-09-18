using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    // Authored against the realistic cutouts. Each trunk has a separate ground
    // anchor; hidden rear trunks are estimated from the visible tree's axis.
    // Values are normalized image coordinates (top-left origin), width and height.
    public static class ForestClusterShadows
    {
        // Both palettes depict the same arrangement. Left, rear, right, fir, front.
        static readonly Vector4[] Trees =
        {
            new(.267f,.843f,.49f,.64f), new(.537f,.72f,.48f,.70f),
            new(.782f,.834f,.42f,.56f), new(.663f,.850f,.23f,.54f),
            new(.480f,.975f,.43f,.48f)
        };

        public static bool Update(SpriteRenderer source, MeshRenderer shadow, Vector3 ray,
            Func<Vector3, float> groundHeight, Func<Vector3, Vector3> groundAnchor)
        {
            string name = source.sprite.texture.name;
            if (!ForestClusterCatalog.IsTexture(name)) return false;
            bool winter = name.EndsWith("-winter");
            var vertices = new List<Vector3>(200);
            var colors = new List<Color>(200);
            var indices = new List<int>(600);
            var sprite = source.sprite;
            var size = sprite.rect.size / sprite.pixelsPerUnit;
            var pivot = sprite.pivot / sprite.pixelsPerUnit;
            var right = source.transform.right; right.y = 0; right.Normalize();
            var sunGround = new Vector3(ray.x, 0, ray.z);
            var sunDirection = sunGround.sqrMagnitude > .0001f ? sunGround.normalized : Vector3.forward;
            var canopySide = Vector3.Cross(Vector3.up, sunDirection);
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
                var t = Trees[tree];
                // Intersect the camera ray through the pictured trunk foot with
                // its ground plane, so the shadow touches that foot on screen.
                var foot = source.transform.TransformPoint(new Vector3(t.x * size.x - pivot.x,
                    (1 - t.y) * size.y - pivot.y, 0));
                foot = groundAnchor(foot);
                float height = t.w * size.y * scale.y / verticalProjection;
                float width = t.z * size.x * scale.x;
                bool fir = tree == 3;
                bool bare = winter && !fir;
                // Soft canopy/contact shade reads beneath the grove even when
                // the directional projection is hidden behind its billboard.
                int contact = vertices.Count;
                Vertex(foot, 0, bare ? .10f : .65f);
                var across = Vector3.Cross(Vector3.up, right);
                const int contactSides = 16;
                for (int i = 0; i < contactSides; i++)
                {
                    float angle = i * Mathf.PI * 2 / contactSides;
                    Vertex(foot + (right * Mathf.Cos(angle) + across * Mathf.Sin(angle)) * width * .46f, 0, 0);
                }
                for (int i = 0; i < contactSides; i++)
                    indices.AddRange(new[] { contact, contact + 1 + i, contact + 1 + (i + 1) % contactSides });
                int start = vertices.Count;
                // Continuous trunk joins the contact point to the canopy.
                float trunk = width * .035f;
                Vertex(foot - right * trunk, 0, .85f); Vertex(foot + right * trunk, 0, .85f);
                Vertex(foot + right * trunk, height * .65f, .7f); Vertex(foot - right * trunk, height * .65f, .7f);
                indices.AddRange(new[] { start, start+1, start+2, start, start+2, start+3 });
                if (bare)
                {
                    // Open branch projections, not a solid summer canopy.
                    for (int branch = 0; branch < 9; branch++)
                    {
                        float level = .30f + branch * .072f;
                        float side = branch % 2 == 0 ? -1 : 1;
                        float reach = width * .45f * (1 - (level - .55f) * (level - .55f));
                        int b = vertices.Count;
                        Vertex(foot - canopySide * trunk * .5f, height * level, .38f);
                        Vertex(foot + canopySide * trunk * .5f, height * level, .38f);
                        Vertex(foot + canopySide * side * reach, height * Mathf.Min(1, level + .22f), .10f);
                        indices.AddRange(new[] { b, b+1, b+2 });
                    }
                    continue;
                }
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
                    // Round canopy volume supplies ground depth even at noon.
                    // A flat camera-facing source collapses when sun rays run
                    // parallel to its width axis (morning/afternoon presets).
                    Vertex(foot + canopySide * x + sunDirection * (y * width * .30f * radius), h, ring == 0 ? .8f : 0);
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
