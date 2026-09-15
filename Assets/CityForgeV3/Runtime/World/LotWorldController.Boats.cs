using UnityEngine;
using UnityEngine.Rendering;
namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        public const string WoodenBargePropId = "wooden-lumber-barge-v01";
        public const string WoodenBargeResourcePath = "CityForgeV3/Vehicles/WoodenBargeV01/WoodenBargeV01";
        public bool SelectedPropIsBoat => SelectedPropIndex >= 0 && SelectedPropIndex < PropCount &&
            BoatCatalog.Find(_session.Data.Props[SelectedPropIndex].PropId) != null;
        public string SelectedBoatDisplayName => SelectedPropIsBoat
            ? BoatCatalog.Find(_session.Data.Props[SelectedPropIndex].PropId).displayName : "";
        private Transform CreateBoatPresentation(BoatDefinition boat, string name, float alpha)
        {
            var prefab = Resources.Load<GameObject>(boat.prefabResourcePath);
            if (prefab == null) return null;
            var root = Instantiate(prefab).transform;
            root.name = name;
            if (alpha < 1f)
            {
                var owner = root.gameObject.AddComponent<CharacterShadowMaterialOwner>();
                foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                {
                    var material = new Material(renderer.sharedMaterial);
                    var color = material.color; color.a = alpha; material.color = color;
                    material.SetFloat("_Mode", 3);
                    material.SetInt("_SrcBlend", (int)BlendMode.One);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    renderer.sharedMaterial = material;
                    owner.Add(material);
                }
            }
            return root;
        }
    }
}
