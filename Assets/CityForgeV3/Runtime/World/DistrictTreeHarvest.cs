using UnityEngine;
namespace CityForgeV3.World
{
    public enum DistrictTreeHarvestState { Standing, Fallen, Stump }
    public static class DistrictTreeHarvest
    {
        public const int PrototypeWoodYield = 8;
        public static bool CanFell(PlacedDistrictFlora tree) => tree != null &&
            tree.FloraId == "cilician-fir" && tree.HarvestState == DistrictTreeHarvestState.Standing;
        public static bool Fell(PlacedDistrictFlora tree, int direction)
        {
            if (!CanFell(tree)) return false;
            tree.HarvestDirection = Mathf.Clamp(direction, 0, 3);
            tree.HarvestState = DistrictTreeHarvestState.Fallen;
            tree.RemainingWood = PrototypeWoodYield;
            return true;
        }
        public static int TakeWood(PlacedDistrictFlora tree, int requested)
        {
            if (tree == null || tree.HarvestState != DistrictTreeHarvestState.Fallen || requested <= 0) return 0;
            var taken = Mathf.Min(requested, Mathf.Max(0, tree.RemainingWood));
            tree.RemainingWood -= taken;
            if (tree.RemainingWood == 0) tree.HarvestState = DistrictTreeHarvestState.Stump;
            return taken;
        }
    }
}
