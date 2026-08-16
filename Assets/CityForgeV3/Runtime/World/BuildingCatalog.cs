using System.Collections.Generic;

namespace CityForgeV3.World
{
    public enum BuildingUseCategory
    {
        Residential,
        Commercial,
        Industrial,
        Mixed
    }

    public readonly struct BuildingCatalogEntry
    {
        public BuildingCatalogEntry(
            string id,
            string name,
            string category,
            string sizeClass,
            int occupancyWidth,
            int occupancyDepth,
            string packageResourcePath,
            string shortcut,
            string reviewStatus,
            string shortName,
            string thumbnailResourcePath)
        {
            Id = id;
            Name = name;
            Category = category;
            SizeClass = sizeClass;
            OccupancyWidth = occupancyWidth;
            OccupancyDepth = occupancyDepth;
            PackageResourcePath = packageResourcePath;
            Shortcut = shortcut;
            ReviewStatus = reviewStatus;
            ShortName = shortName;
            ThumbnailResourcePath = thumbnailResourcePath;
        }

        public string Id { get; }
        public string Name { get; }
        public string Category { get; }
        public string SizeClass { get; }
        public int OccupancyWidth { get; }
        public int OccupancyDepth { get; }
        public string PackageResourcePath { get; }
        public string Shortcut { get; }
        public string ReviewStatus { get; }
        public string ShortName { get; }
        public string ThumbnailResourcePath { get; }
    }

    public static class BuildingCatalog
    {
        public const string ColonialGovernmentHouseId =
            "cityforge.v3.civic.colonial_government_house_01";
        public const string NewEnglandHouseId =
            "cityforge.v3.residential.new_england_house_1720_01";
        public const string ColonialCornerPorticoCommercialId =
            "cityforge.base.building.commercial.colonial_corner_portico_commercial_01";

        private static BuildingCatalogEntry[] _entries;

        private static BuildingCatalogEntry[] Entries
        {
            get
            {
                if (_entries != null) return _entries;
                var packages = HybridBuildingPackageRegistry.All;
                _entries = new BuildingCatalogEntry[packages.Count];
                for (var index = 0; index < packages.Count; index++)
                {
                    var package = packages[index];
                    _entries[index] = new BuildingCatalogEntry(
                        package.Id, package.DisplayName, package.Category,
                        package.SizeClass, package.OccupancyWidth,
                        package.OccupancyDepth, package.ResourcePath,
                        package.LibraryShortcut, package.ReviewStatus,
                        package.ShortDisplayName,
                        package.CatalogThumbnailResourcePath);
                }
                return _entries;
            }
        }

        public static IReadOnlyList<BuildingCatalogEntry> All => Entries;

        public static BuildingUseCategory UseCategoryFor(BuildingCatalogEntry entry)
        {
            if (System.Enum.TryParse(entry.Category, true, out BuildingUseCategory category))
                return category;

            // The compact Lot Editor picker intentionally has four broad land-use
            // families. Civic and institutional packages live under Mixed until
            // a dedicated civic library is introduced.
            return BuildingUseCategory.Mixed;
        }

        public static IReadOnlyList<BuildingCatalogEntry> ForUseCategory(
            BuildingUseCategory category)
        {
            var matches = new List<BuildingCatalogEntry>();
            foreach (var entry in Entries)
                if (UseCategoryFor(entry) == category) matches.Add(entry);
            return matches;
        }

        public static BuildingCatalogEntry GovernmentHouse => Entries[0];
        public static BuildingCatalogEntry NewEnglandHouse => Entries[1];

        public static BuildingCatalogEntry Find(string id)
        {
            foreach (var entry in Entries)
            {
                if (entry.Id == id) return entry;
            }

            throw new KeyNotFoundException($"Unknown building catalog id: {id}");
        }
    }
}
