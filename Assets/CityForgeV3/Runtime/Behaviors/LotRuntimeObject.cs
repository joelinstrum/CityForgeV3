using UnityEngine;
namespace CityForgeV3.Behaviors
{
    // Identity belongs to the behavior instance, surviving presentation rebuilds.
    public sealed class LotRuntimeObject : MonoBehaviour
    {
        public string Id;
        public string FriendlyName;
        public static void Attach(Transform target, string id, string name)
        {
            var item = target.gameObject.AddComponent<LotRuntimeObject>();
            item.Id = id; item.FriendlyName = name;
        }
    }
}
