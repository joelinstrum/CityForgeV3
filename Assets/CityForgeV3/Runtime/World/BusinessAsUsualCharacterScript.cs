using UnityEngine;

namespace CityForgeV3.World
{
    public enum BusinessAsUsualAction
    {
        Walk,
        Wait,
        FoldArms,
        Idle,
        LookAround
    }

    /// <summary>
    /// The default autonomous behavior contract for a character who has no
    /// explicit authored task. Selection is intentionally separated from the
    /// runtime controller so the probability contract is stable and testable.
    /// </summary>
    public static class BusinessAsUsualCharacterScript
    {
        public static BusinessAsUsualAction SelectForCharacter(string propId,
            float roll)
        {
            if (string.Equals(propId, LotWorldController.HooliganCharacterId,
                    System.StringComparison.OrdinalIgnoreCase))
                return Mathf.Clamp01(roll) < 0.15f
                    ? BusinessAsUsualAction.Walk
                    : BusinessAsUsualAction.Idle;
            if (string.Equals(propId, LotWorldController.HistoricPolicemanCharacterId,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                roll = Mathf.Clamp01(roll);
                if (roll < 0.20f) return BusinessAsUsualAction.Walk;
                if (roll < 0.25f) return BusinessAsUsualAction.Wait;
                if (roll < 0.35f) return BusinessAsUsualAction.LookAround;
                return BusinessAsUsualAction.Idle;
            }
            return Select(roll);
        }

        public static BusinessAsUsualAction Select(float roll)
        {
            roll = Mathf.Clamp01(roll);
            if (roll < 0.75f) return BusinessAsUsualAction.Walk;
            if (roll < 0.80f) return BusinessAsUsualAction.Wait;
            if (roll < 0.85f) return BusinessAsUsualAction.FoldArms;
            if (roll < 0.95f) return BusinessAsUsualAction.Idle;
            return BusinessAsUsualAction.LookAround;
        }

        public static string AnimationState(BusinessAsUsualAction action) =>
            action switch
            {
                BusinessAsUsualAction.Walk => "walk",
                BusinessAsUsualAction.Wait => "wait",
                BusinessAsUsualAction.FoldArms => "fold_arms",
                BusinessAsUsualAction.LookAround => "look_around",
                _ => "idle"
            };

        public static Vector2 WalkingDirection(float roll)
        {
            // Match Documentation/Architecture/LOT_EDITOR_COORDINATES.md:
            // autonomous travel uses the same exact cardinal headings as
            // authored arrow-key travel, rather than arbitrary 360° headings.
            var directionIndex = Mathf.Min(
                Mathf.FloorToInt(Mathf.Clamp01(roll) * 4f), 3);
            return directionIndex switch
            {
                0 => Vector2.up,    // North, 0°
                1 => Vector2.right, // East, 90°
                2 => Vector2.down,  // South, 180°
                _ => Vector2.left   // West, 270°
            };
        }

        public static float Duration(BusinessAsUsualAction action, float roll)
        {
            roll = Mathf.Clamp01(roll);
            return action switch
            {
                // A walk is destination-driven by the controller and ends at
                // the lot boundary; this duration is only a defensive fallback.
                BusinessAsUsualAction.Walk => 60f,
                BusinessAsUsualAction.Wait => Mathf.Lerp(1.5f, 3.5f, roll),
                BusinessAsUsualAction.FoldArms => Mathf.Lerp(3f, 7f, roll),
                BusinessAsUsualAction.LookAround => Mathf.Lerp(2.5f, 5.5f, roll),
                _ => Mathf.Lerp(2f, 5f, roll)
            };
        }
    }
}
