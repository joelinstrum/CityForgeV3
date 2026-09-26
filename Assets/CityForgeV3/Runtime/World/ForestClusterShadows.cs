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

        // The district's upright tree art reads best with shadows on the
        // visible ground plane. The noon and afternoon marks favor screen right;
        // keep a small away-from-camera component so the silhouettes remain
        // attached to the tree feet rather than crossing their billboards.
        public static Vector3 DistrictTreeRay(Vector3 sunRay,
            Vector3 cameraForward, Vector3 cameraRight, TimeOfDayPreset preset)
        {
            if (preset != TimeOfDayPreset.Noon &&
                preset != TimeOfDayPreset.Afternoon)
                return BehindCameraRay(sunRay, cameraForward);
            var horizontal = Vector3.ProjectOnPlane(sunRay, Vector3.up);
            var behind = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
            var right = Vector3.ProjectOnPlane(cameraRight, Vector3.up);
            if (horizontal.sqrMagnitude < .0001f ||
                behind.sqrMagnitude < .0001f || right.sqrMagnitude < .0001f)
                return sunRay;
            var behindWeight = preset == TimeOfDayPreset.Noon ? .4f : .1f;
            var direction = (right.normalized +
                behind.normalized * behindWeight).normalized;
            return direction * horizontal.magnitude + Vector3.up * sunRay.y;
        }

        // Project each authored member's alpha cutout onto the terrain. One
        // tessellated quad per member follows hills without introducing child
        // renderers; UV3 chooses the second atlas in mixed fir/deciduous stands.
        public static bool UpdateArtwork(SpriteRenderer source,
            MeshRenderer shadow, Vector3 ray,
            Func<Vector3, float> groundHeight,
            Func<Vector3, Vector3> groundAnchor,
            TimeOfDayPreset preset, float lengthScale,
            out Texture2D alternateAtlas)
        {
            alternateAtlas = null;
            var atlas = source.GetComponent<ForestTrueAngleCluster>();
            if ((atlas == null || atlas.PieceCount <= 1) &&
                !ForestClusterCatalog.IsTexture(source.sprite.texture.name))
                return false;
            var horizontal = Vector3.ProjectOnPlane(ray, Vector3.up);
            var horizontalMagnitude = horizontal.magnitude;
            var direction = horizontalMagnitude > .0001f
                ? horizontal / horizontalMagnitude : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, direction).normalized;
            var root = groundAnchor(source.transform.position);
            var sourceTexture = source.sprite.texture;
            var scale = source.transform.lossyScale;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var selectors = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var pieceCount = atlas?.PieceCount ?? 1;
            for (var pieceIndex = 0; pieceIndex < pieceCount; pieceIndex++)
            {
                var piece = atlas != null ? atlas.Piece(pieceIndex) :
                    source.sprite;
                bool alternate = piece.texture != sourceTexture;
                if (alternate)
                {
                    if (alternateAtlas != null && alternateAtlas != piece.texture)
                        throw new InvalidOperationException(
                            "A clump shadow supports two artwork atlases.");
                    alternateAtlas = piece.texture;
                }
                var pieceScale = atlas != null ? atlas.PieceScale(pieceIndex) : 1f;
                var widthScale = scale.x * pieceScale;
                var heightScale = scale.y * pieceScale;
                var referenceHeight = Mathf.Max(.01f,
                    piece.bounds.size.y * heightScale);
                var travel = Mathf.Min(referenceHeight * horizontalMagnitude /
                        Mathf.Max(.05f, -ray.y) *
                        (preset == TimeOfDayPreset.Noon ? .8f : .55f),
                    piece.bounds.size.x * widthScale * .65f) * lengthScale;
                var foot = root + (atlas != null ? atlas.WorldOffset(pieceIndex) :
                    Vector3.zero);
                var pieceUV = piece.uv;
                float minU = 1f, maxU = 0f, minV = 1f, maxV = 0f;
                foreach (var point in pieceUV)
                {
                    minU = Mathf.Min(minU, point.x);
                    maxU = Mathf.Max(maxU, point.x);
                    minV = Mathf.Min(minV, point.y);
                    maxV = Mathf.Max(maxV, point.y);
                }
                // A wider legacy composition needs more terrain samples than
                // each narrow member sprite in a true-angle cluster.
                int divisions = atlas != null ? 4 : 8;
                int start = vertices.Count;
                for (int row = 0; row <= divisions; row++)
                for (int col = 0; col <= divisions; col++)
                {
                    float u = (float)col / divisions;
                    float v = (float)row / divisions;
                    float x = Mathf.Lerp(piece.bounds.min.x,
                        piece.bounds.max.x, u) * widthScale;
                    float height = Mathf.Max(0f, Mathf.Lerp(
                        piece.bounds.min.y, piece.bounds.max.y, v) * heightScale);
                    var point = foot + right * x + direction *
                        (travel * Mathf.Clamp01(height / referenceHeight));
                    point.y = groundHeight(point) + .031f;
                    vertices.Add(shadow.transform.InverseTransformPoint(point));
                    uv.Add(new Vector2(Mathf.Lerp(minU, maxU, u),
                        Mathf.Lerp(minV, maxV, v)));
                    selectors.Add(alternate ? Vector2.right : Vector2.zero);
                    colors.Add(new Color(1f, 1f, 1f,
                        Mathf.Clamp01(height / referenceHeight)));
                }
                for (int row = 0; row < divisions; row++)
                for (int col = 0; col < divisions; col++)
                {
                    int corner = start + row * (divisions + 1) + col;
                    triangles.Add(corner);
                    triangles.Add(corner + divisions + 1);
                    triangles.Add(corner + 1);
                    triangles.Add(corner + 1);
                    triangles.Add(corner + divisions + 1);
                    triangles.Add(corner + divisions + 2);
                }
            }
            var mesh = shadow.GetComponent<MeshFilter>().sharedMesh;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetUVs(3, selectors);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return true;
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
        public static bool Update(SpriteRenderer source, MeshRenderer shadow, Vector3 ray,
            Func<Vector3, float> groundHeight, Func<Vector3, Vector3> groundAnchor,
            float atlasProjectionScale = 1f,
            Vector3 atlasDirectionOverride = default)
        {
            var atlas = source.GetComponent<ForestTrueAngleCluster>();
            string name = source.sprite.texture.name;
            // Single firs can project their real atlas cutout. The shared
            // crown footprint is only needed for multi-tree compositions.
            if (atlas != null && atlas.IsFir && atlas.PieceCount == 1)
                return false;
            if (atlas == null && !ForestClusterCatalog.IsTexture(name)) return false;

            var sprite = source.sprite;
            var size = sprite.rect.size / sprite.pixelsPerUnit;
            var scale = source.transform.lossyScale;
            float width = (atlas != null ? atlas.EnvelopeWidth : size.x) * scale.x;
            float height = (atlas != null ? atlas.TreeHeight : size.y) * scale.y /
                Mathf.Max(.2f, source.transform.up.y);
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
            var root = groundAnchor(source.transform.position);
            float travel = groundRay.magnitude * height * .46f /
                Mathf.Max(.05f, -ray.y);
            if (atlas != null) travel *= atlasProjectionScale;
            float cameraBehindBias = atlas != null &&
                atlasDirectionOverride.sqrMagnitude > .0001f
                    ? atlas.TreeWidth * .16f : 0f;
            var crowns = name.Contains("-large-") ? LargeCrowns :
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
                    float lobeWidth = atlas != null ?
                        atlas.TreeWidth * atlas.PieceScale(i) * .28f :
                        width * (crowns.Length == 5 ? .165f :
                        crowns.Length == 4 ? .19f : .225f);
                    Fan(basePoint + direction * (travel *
                            (atlas != null ? .25f : .55f) +
                            cameraBehindBias), lobeWidth,
                        atlas != null ? atlas.TreeWidth *
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
