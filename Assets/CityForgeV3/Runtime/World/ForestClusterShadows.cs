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

            // Only the leafed canopy gains an inner ring. It keeps most of the
            // footprint defined, then fades over a short outer band.
            var vertices = new List<Vector3>(CanopySides * 2 + ContactSides + 2);
            var colors = new List<Color>(CanopySides * 2 + ContactSides + 2);
            var indices = new List<int>((CanopySides * 2 + ContactSides) * 6);

            Vector3 Ground(Vector3 point)
            {
                point.y = groundHeight(point) + .031f;
                return shadow.transform.InverseTransformPoint(point);
            }

            void Fan(Vector3 center, float sideRadius, float lengthRadius,
                int sides, float opacity, float phase, bool definedInterior)
            {
                int start = vertices.Count;
                vertices.Add(Ground(center));
                colors.Add(new Color(opacity, 1, 1, .4f));
                void Ring(float radius, float ringOpacity)
                {
                    for (int i = 0; i < sides; i++)
                    {
                        float angle = i * Mathf.PI * 2 / sides;
                        // Small deterministic radius changes keep the shared
                        // footprint organic without per-tree coordinates.
                        float irregular = 1f +
                            (definedInterior ? .09f : .07f) *
                            Mathf.Sin(i * 2.37f + phase);
                        var point = center +
                            side * (Mathf.Cos(angle) * sideRadius * radius * irregular) +
                            direction * (Mathf.Sin(angle) * lengthRadius * radius * irregular);
                        vertices.Add(Ground(point));
                        colors.Add(new Color(ringOpacity, 1, 1, .4f));
                    }
                }
                if (definedInterior) Ring(.76f, opacity);
                int outerStart = vertices.Count;
                Ring(1f, 0f);
                int innerStart = definedInterior ? start + 1 : outerStart;
                for (int i = 0; i < sides; i++)
                {
                    int next = (i + 1) % sides;
                    indices.Add(start); indices.Add(innerStart + i);
                    indices.Add(innerStart + next);
                    if (!definedInterior) continue;
                    indices.Add(innerStart + i); indices.Add(outerStart + i);
                    indices.Add(outerStart + next);
                    indices.Add(innerStart + i); indices.Add(outerStart + next);
                    indices.Add(innerStart + next);
                }
            }

            // A directional canopy footprint replaces the former opaque trunk
            // strips. Leafed clusters have a defined interior and short soft
            // edge; all versions stay attached to the one real cluster root.
            float canopyOpacity = winter ? .28f : .78f;
            var canopyCenter = root + direction * (travel * .5f);
            Fan(canopyCenter, width * (winter ? .31f : .45f),
                travel * .5f + width * (winter ? .18f : .29f),
                CanopySides, canopyOpacity, 0f, !winter);
            Fan(root + direction * width * .035f, width * .18f,
                width * .14f, ContactSides, winter ? .12f : .38f,
                1.7f, false);

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
