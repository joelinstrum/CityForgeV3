using System;

namespace CityForgeV3.World
{
    /// <summary>
    /// Stable, language-neutral script identifiers stored in lot saves. Native
    /// state machines use them today; a future sandboxed Python adapter can
    /// target the same command contract without storing executable code here.
    /// </summary>
    public static class CharacterBehaviorScript
    {
        public const string BusinessAsUsual = "business-as-usual";
        public const string HarassPedestrian = "harass-pedestrian";
        public const string FightHooligan = "fight-hooligan";
        public const string EvadePolice = "evade-police";

        public static string Normalize(string value) => value switch
        {
            HarassPedestrian => HarassPedestrian,
            FightHooligan => FightHooligan,
            EvadePolice => EvadePolice,
            _ => BusinessAsUsual
        };

        public static string DisplayName(string value) => Normalize(value) switch
        {
            HarassPedestrian => "Harass Pedestrian",
            FightHooligan => "Fight Hooligan",
            EvadePolice => "Evade Police",
            _ => "Business as Usual"
        };

        public static bool IsAvailableFor(string propId, string value)
        {
            value = Normalize(value);
            if (value == BusinessAsUsual) return true;
            return LotWorldController.IsHooligan(propId) &&
                value is HarassPedestrian or EvadePolice;
        }
    }
}
