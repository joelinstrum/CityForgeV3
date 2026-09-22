using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    // Forest clusters are one authored composition, not a collection of
    // independently positioned simulation trees. Their ground shadow follows
    // the composition's shared root and bounds so a new cluster illustration
    // cannot invalidate hidden, hand-authored trunk coordinates.
    public static class ForestClusterShadows
    {
        const int CanopySides = 24;
        const int ContactSides = 16;

        public static bool Update(SpriteRenderer source, MeshRenderer shadow, Vector3 ray,
            Func<Vector3, float> groundHeight, Func<Vector3, Vector3> groundAnchor)
        {
            string name = source.sprite.texture.name;
            if (!ForestClusterCatalog.IsTexture(name)) return false;

            var sprite = source.sprite;
            var size = sprite.rect.size / sprite.pixelsPerUnit;
            var scale = source.transform.lossyScale;
            float width = size.x * scale.x;
            float height = size.y * scale.y /
                Mathf.Max(.2f, source.transform.up.y);
            bool winter = name.EndsWith("-winter");
            var groundRay = new Vector3(ray.x, 0, ray.z);
            var direction = groundRay.sqrMagnitude > .0001f
                ? groundRay.normalized : Vector3.forward;
            var side = Vector3.Cross(Vector3.up, direction).normalized;
            var root = groundAnchor(source.transform.position);
            float travel = groundRay.magnitude * height * .46f /
                Mathf.Max(.05f, -ray.y);

            var vertices = new List<Vector3>(CanopySides + ContactSides + 2);
            var colors = new List<Color>(CanopySides + ContactSides + 2);
            var indices = new List<int>((CanopySides + ContactSides) * 3);

            Vector3 Ground(Vector3 point)
            {
                point.y = groundHeight(point) + .031f;
                return shadow.transform.InverseTransformPoint(point);
            }

            void Fan(Vector3 center, float sideRadius, float lengthRadius,
                int sides, float opacity, float phase)
            {
                int start = vertices.Count;
                vertices.Add(Ground(center));
                colors.Add(new Color(opacity, 1, 1, .4f));
                for (int i = 0; i < sides; i++)
                {
                    float angle = i * Mathf.PI * 2 / sides;
                    // Small deterministic radius changes keep the shared
                    // footprint organic without storing coordinates per asset.
                    float irregular = 1f + .07f * Mathf.Sin(i * 2.37f + phase);
                    var point = center +
                        side * (Mathf.Cos(angle) * sideRadius * irregular) +
                        direction * (Mathf.Sin(angle) * lengthRadius * irregular);
                    vertices.Add(Ground(point));
                    colors.Add(new Color(0, 1, 1, .4f));
                }
                for (int i = 0; i < sides; i++)
                    indices.AddRange(new[] { start, start + 1 + i,
                        start + 1 + (i + 1) % sides });
            }

            // A soft directional canopy footprint replaces the former opaque
            // trunk strips. It stays attached to the one real cluster root and
            // scales automatically with compact, large and future artwork.
            float canopyOpacity = winter ? .28f : .72f;
            var canopyCenter = root + direction * (travel * .5f);
            Fan(canopyCenter, width * (winter ? .31f : .45f),
                travel * .5f + width * (winter ? .18f : .29f),
                CanopySides, canopyOpacity, 0f);
            Fan(root + direction * width * .035f, width * .18f,
                width * .14f, ContactSides, winter ? .12f : .34f, 1.7f);

            var mesh = shadow.GetComponent<MeshFilter>().sharedMesh;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, new Vector2[vertices.Count]);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return true;
        }
    }
}
