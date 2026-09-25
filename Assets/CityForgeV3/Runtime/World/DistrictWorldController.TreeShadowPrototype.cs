using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        const string SoftTreeShadowPath =
            "CityForgeV3/Flora/ShadowsV01/broad-canopy";
        const int ShadowAcross = 8;
        const int ShadowAlong = 8;
        Texture2D _softTreeShadowTexture;

        // The hillside study uses one soft mask and a terrain-following grid
        // per scenery handle. A grouped tree gets one full-footprint shadow,
        // while the existing flora batch combines handles into shared draws.
        void UpdateSoftTreeShadowPrototype(SpriteRenderer tree,
            MeshRenderer shadow, Vector3 sunRay)
        {
            var texture = _softTreeShadowTexture != null
                ? _softTreeShadowTexture
                : _softTreeShadowTexture = Resources.Load<Texture2D>(
                    SoftTreeShadowPath);
            if (texture == null)
            {
                shadow.enabled = false;
                return;
            }
            var root = tree.transform.position;
            var localRoot = _content.InverseTransformPoint(root);
            var horizontal = Vector3.ProjectOnPlane(sunRay, Vector3.up);
            var direction = horizontal.sqrMagnitude > .0001f
                ? horizontal.normalized : Vector3.forward;
            var size = tree.sprite.bounds.size;
            var scale = tree.transform.lossyScale;
            var atlas = tree.GetComponent<ForestTrueAngleCluster>();
            var grouped = atlas != null && atlas.PieceCount > 1;
            var otherCluster = atlas == null &&
                ForestClusterCatalog.IsTexture(tree.sprite.texture.name);
            // True-angle roots show only one source tree, so their sprite
            // bounds cannot describe the whole cluster. Other cluster art is
            // already composed in one sprite but needs its full width kept.
            var canopyWidth = grouped
                ? Mathf.Clamp(atlas.EnvelopeWidth * scale.x, 15f, 64f)
                : otherCluster
                    ? Mathf.Clamp(size.x * scale.x * .9f, 12f, 64f)
                    : Mathf.Clamp(size.x * scale.x * .72f, 6f, 24f);
            var treeHeight = Mathf.Clamp((grouped ? atlas.TreeHeight : size.y) *
                scale.y, 5f, 30f);
            var travel = treeHeight * horizontal.magnitude /
                Mathf.Max(.22f, -sunRay.y);
            var length = Mathf.Clamp(canopyWidth * .45f + travel * .62f,
                canopyWidth * .75f, canopyWidth * 2.1f);
            var side = Vector3.Cross(Vector3.up, direction).normalized;

            var vertexCount = (ShadowAcross + 1) * (ShadowAlong + 1);
            var vertices = new Vector3[vertexCount];
            var uv = new Vector2[vertexCount];
            var colors = new Color[vertexCount];
            var triangles = new int[ShadowAcross * ShadowAlong * 6];
            for (var row = 0; row <= ShadowAlong; row++)
            for (var col = 0; col <= ShadowAcross; col++)
            {
                var across = (float)col / ShadowAcross;
                var along = (float)row / ShadowAlong;
                var point = root + side * ((across - .5f) * canopyWidth) +
                    direction * ((along - .08f) * length);
                var local = _content.InverseTransformPoint(point);
                point.y = root.y + TerrainElevation(local.x, local.z) -
                    TerrainElevation(localRoot.x, localRoot.z) + .035f;
                var index = row * (ShadowAcross + 1) + col;
                vertices[index] = shadow.transform.InverseTransformPoint(point);
                // The existing mask occupies the lower 195 pixels of its
                // 512-pixel source; trim its empty upper half in UV space.
                uv[index] = new Vector2(Mathf.Lerp(.129f, .873f, across),
                    along * .381f);
                colors[index] = Color.white;
            }
            var next = 0;
            for (var row = 0; row < ShadowAlong; row++)
            for (var col = 0; col < ShadowAcross; col++)
            {
                var corner = row * (ShadowAcross + 1) + col;
                triangles[next++] = corner;
                triangles[next++] = corner + ShadowAcross + 1;
                triangles[next++] = corner + 1;
                triangles[next++] = corner + 1;
                triangles[next++] = corner + ShadowAcross + 1;
                triangles[next++] = corner + ShadowAcross + 2;
            }
            var mesh = shadow.GetComponent<MeshFilter>().sharedMesh;
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            var properties = new MaterialPropertyBlock();
            shadow.GetPropertyBlock(properties);
            properties.SetTexture("_MainTex", texture);
            properties.SetFloat("_Cutoff", .005f);
            properties.SetColor("_Color", new Color(.018f, .022f, .026f,
                TimeOfDay == TimeOfDayPreset.Noon ? 1.65f : 1.45f));
            shadow.SetPropertyBlock(properties);
            shadow.sortingOrder = tree.sortingOrder - 1;
        }
    }
}
