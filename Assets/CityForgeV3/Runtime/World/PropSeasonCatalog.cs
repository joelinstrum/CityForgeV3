using System;
using System.Collections.Generic;

namespace CityForgeV3.World
{
    [Flags]
    public enum PropSeason
    {
        None = 0,
        Spring = 1 << 0,
        Summer = 1 << 1,
        Autumn = 1 << 2,
        Winter = 1 << 3,
        All = Spring | Summer | Autumn | Winter
    }

    /// <summary>
    /// Seasonal availability is catalog metadata, not placed-instance state.
    /// This lets a saved decoration disappear out of season and return when
    /// its season becomes active without deleting or rewriting the lot.
    /// Unregistered props remain available all year for save compatibility.
    /// </summary>
    public static class PropSeasonCatalog
    {
        private static readonly Dictionary<string, PropSeason> Seasons = new(
            StringComparer.OrdinalIgnoreCase)
        {
            { LotWorldController.PumpkinJackOLanternPropId, PropSeason.Autumn }
        };

        public static PropSeason SeasonFor(string propId) =>
            !string.IsNullOrWhiteSpace(propId) &&
            Seasons.TryGetValue(propId, out var season)
                ? season
                : PropSeason.All;

        public static bool IsAvailable(string propId, SeasonPreset season) =>
            (SeasonFor(propId) & ToMask(season)) != 0;

        private static PropSeason ToMask(SeasonPreset season) => season switch
        {
            SeasonPreset.Spring => PropSeason.Spring,
            SeasonPreset.Summer => PropSeason.Summer,
            SeasonPreset.Autumn => PropSeason.Autumn,
            SeasonPreset.Winter => PropSeason.Winter,
            _ => PropSeason.None
        };
    }
}
