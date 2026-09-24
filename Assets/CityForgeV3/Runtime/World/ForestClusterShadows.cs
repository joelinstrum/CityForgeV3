using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    // A cluster has one ground anchor. Its few crown/contact lobes are a
    // deterministic approximation within the artwork bounds, not separate
    // simulation trees or additional renderers.
    public static class ForestClusterShadows
    {
        // Art-directed district shadows retain the sun's elevation/length, but
        // their horizontal footprint stays behind upright camera-facing trees.
        // A small sun-side component keeps morning and afternoon distinct.
        public static Vector3 BehindCameraRay(Vector3 sunRay,
            Vector3 cameraForward)
        {
            var horizontal = Vector3.ProjectOnPlane(sunRay, Vector3.up);
            var behind = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
            if (horizontal.sqrMagnitude < .0001f || behind.sqrMagnitude < .0001f)
                return sunRay;
            behind.Normalize();
            var lateral = horizontal - Vector3.Dot(horizontal, behind) * behind;
            var direction = (behind + lateral.normalized * .3f).normalized;
            return direction * horizontal.magnitude + Vector3.up * sunRay.y;
        }

        const int CanopySides = 20;
        const int ContactSides = 8;
        static readonly Vector2[] CompactCrowns =
        {
            new(-.28f, .08f), new(.02f, -.08f), new(.28f, .10f)
        };
        static readonly Vector2[] StandardCrowns =
        {
            new(-.32f, .10f), new(-.11f, -.08f),
            new(.12f, .12f), new(.33f, -.06f)
        };
        static readonly Vector2[] LargeCrowns =
        {
            new(-.36f, .08f), new(-.18f, -.10f), new(0f, .12f),
            new(.20f, -.07f), new(.37f, .10f)
        };
        static readonly Vector2[] BroadTreeCrowns =
        {
            new(-.25f, 0f), new(0f, 0f), new(.25f, 0f)
        };

        public static bool Update(SpriteRenderer source, MeshRenderer shadow, Vector3 ray,
            Func<Vector3, float> groundHeight, Func<Vector3, Vector3> groundAnchor,
            float atlasProjectionScale = 1f,
            Vector3 atlasDirectionOverride = default)
        {
            var atlas = source.GetComponent<ForestTrueAngleCluster>();
            string name = source.sprite.texture.name;
            bool broadTree = name.StartsWith("american-elm-") ||
                name.StartsWith("american-sycamore-") ||
                name == "angel-oak-spanish-moss";
            if (atlas == null && !ForestClusterCatalog.IsTexture(name) &&
                !broadTree) return false;

            var sprite = source.sprite;
            var size = sprite.rect.size / sprite.pixelsPerUnit;
            var scale = source.transform.lossyScale;
            float width = (atlas != null ? atlas.EnvelopeWidth : size.x) * scale.x;
            float height = (atlas != null ? atlas.TreeHeight : size.y) * scale.y /
                (broadTree ? 1f : Mathf.Max(.2f, source.transform.up.y));
            bool winter = atlas != null ?
                atlas.Season == SeasonPreset.Winter && !atlas.IsFir :
                name.EndsWith("-winter");
            var groundRay = new Vector3(ray.x, 0, ray.z);
            var direction = atlas != null &&
                    atlasDirectionOverride.sqrMagnitude > .0001f
                ? atlasDirectionOverride.normalized : groundRay.sqrMagnitude > .0001f
                ? groundRay.normalized : Vector3.forward;
            var side = Vector3.Cross(Vector3.up, direction).normalized;
            var artSide = Vector3.ProjectOnPlane(source.transform.right,
                Vector3.up).normalized;
            if (artSide.sqrMagnitude < .0001f) artSide = side;
            var artDepth = Vector3.Cross(artSide, Vector3.up).normalized;
            var root = broadTree ? source.transform.position :
                groundAnchor(source.transform.position);
            if (broadTree) root.y = groundHeight(root);
            float travel = groundRay.magnitude * height * .46f /
                Mathf.Max(.05f, -ray.y);
            if (atlas != null) travel *= atlasProjectionScale;
            if (broadTree) travel = Mathf.Min(travel * .7f, width * .4f);
            float cameraBehindBias = broadTree ? width * .12f :
                atlas != null && atlasDirectionOverride.sqrMagnitude > .0001f
                    ? atlas.TreeWidth * .16f : 0f;
            var crowns = broadTree ? BroadTreeCrowns :
                name.Contains("-large-") ? LargeCrowns :
                name.Contains("-compact-") ? CompactCrowns : StandardCrowns;

            int crownCount = atlas != null ? atlas.PieceCount : crowns.Length;
            int capacity = winter ? 42 : crownCount *
                (CanopySides * 2 + ContactSides + 2);
            var vertices = new List<Vector3>(capacity);
            var colors = new List<Color>(capacity);
            var indices = new List<int>(capacity * 6);

            Vector3 Ground(Vector3 point)
            {
                point.y = groundHeight(point) + .031f;
                return shadow.transform.InverseTransformPoint(point);
            }

            void Fan(Vector3 center, float sideRadius, float lengthRadius,
                int sides, float opacity, float phase, bool definedInterior,
                float edgeRadius = .76f)
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
                            .07f * Mathf.Sin(i * 2.37f + phase) +
                            (definedInterior ? .035f : 0f) *
                            Mathf.Sin(i * .9f + phase * 1.3f);
                        var point = center +
                            side * (Mathf.Cos(angle) * sideRadius * radius * irregular) +
                            direction * (Mathf.Sin(angle) * lengthRadius * radius * irregular);
                        vertices.Add(Ground(point));
                        colors.Add(new Color(ringOpacity, 1, 1, .4f));
                    }
                }
                if (definedInterior) Ring(edgeRadius, opacity);
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

            if (winter)
            {
                // Bare deciduous branches retain the lighter shared footprint.
                Fan(root + direction * (travel * (atlas != null ? .2f : .5f) +
                        cameraBehindBias),
                    width * (atlas != null ? .20f : .31f),
                    travel * (atlas != null ? .22f : .5f) +
                    width * (atlas != null ? .12f : .18f),
                    24, .28f, 0f, false);
                Fan(root + direction * width * .035f,
                    width * (atlas != null ? .12f : .18f),
                    width * (atlas != null ? .10f : .14f),
                    16, .12f, 1.7f, false);
            }
            else
            {
                // Separate but overlapping silhouettes suggest the visible
                // crowns. Only the outermost 16% feathers, so their edges
                // remain readable at a district zoom without hard pixels.
                for (int i = 0; i < crownCount; i++)
                {
                    var basePoint = atlas != null ? root + atlas.WorldOffset(i) :
                        root + artSide * (crowns[i].x * width) +
                        artDepth * (crowns[i].y * width);
                    float lobeWidth = broadTree ? width * .22f : atlas != null ?
                        atlas.TreeWidth * atlas.PieceScale(i) * .28f :
                        width * (crowns.Length == 5 ? .165f :
                        crowns.Length == 4 ? .19f : .225f);
                    Fan(basePoint + direction * (travel *
                            (broadTree ? .35f : atlas != null ? .25f : .55f) +
                            cameraBehindBias), lobeWidth,
                        broadTree ? (width * .14f + travel * .25f) *
                            (i == 1 ? 1.5f : 1f) : atlas != null ? atlas.TreeWidth *
                            atlas.PieceScale(i) * .24f + travel * .28f :
                            width * .16f + travel * .40f,
                        CanopySides,
                        .62f, i * 1.9f, true,
                        atlas != null ? .94f : .84f);
                    Fan(basePoint + direction * (atlas != null ? 0f :
                            width * .025f),
                        width * .065f, width * .055f, ContactSides,
                        .24f, i * 1.4f, false);
                }
            }

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
