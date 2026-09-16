using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    // Temporary presentation only. Completion is driven by the lot's existing
    // building sequences, with no district scans or per-frame polling.
    public sealed class LotConstructionSite : MonoBehaviour
    {
        readonly HashSet<BuildingConstructionSequence> pending = new();
        GameObject surface;
        Material material;
        static Material sharedMaterial;
        static int materialUsers;
        public bool SurfaceVisible => surface != null && surface.activeSelf;
        public int PendingBuildings => pending.Count;

        public void Track(BuildingConstructionSequence sequence, float width, float depth)
        {
            if (sequence == null || !pending.Add(sequence)) return;
            if (surface != null) return;
            surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = "Temporary Lot Construction Dirt";
            surface.transform.SetParent(transform, false);
            surface.transform.localPosition = new Vector3(0f, .18f, 0f);
            surface.transform.localScale = new Vector3(width, .02f, depth);
            surface.GetComponent<Collider>().enabled = false;
            if (sharedMaterial == null)
            {
                sharedMaterial = new Material(Shader.Find("Standard"))
                {
                    name = "CF Lot Construction Dirt",
                    color = new Color(.30f, .19f, .10f),
                    renderQueue = 2440,
                    enableInstancing = true
                };
                sharedMaterial.SetFloat("_Glossiness", .05f);
            }
            material = sharedMaterial;
            materialUsers++;
            var renderer = surface.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        public void SequenceChanged(BuildingConstructionSequence sequence)
        {
            if (sequence == null || !sequence.IsComplete || !pending.Remove(sequence) || pending.Count != 0) return;
            if (surface != null) { surface.SetActive(false); Dispose(surface); }
            surface = null;
            ReleaseMaterial();
        }

        void OnDestroy() => ReleaseMaterial();
        void ReleaseMaterial()
        {
            if (material == null) return;
            material = null;
            if (--materialUsers > 0) return;
            Dispose(sharedMaterial);
            sharedMaterial = null;
            materialUsers = 0;
        }
        static void Dispose(Object item)
        {
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }
    }
}
