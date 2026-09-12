namespace CityForgeV3.World
{
    // Visual planting families, not a botanical taxonomy.
    public static class FloraFamilies
    {
        public const string Tropical = "Tropical";
        public const string Deciduous = "Deciduous";
        public const string Mountain = "Fir and Mountain";
        public static readonly string[] Names = { Tropical, Deciduous, Mountain };
        public static string ForTree(string id) => id switch
        {
            "date-palm" or "camphor-tree" or "eucalyptus-robusta-a" or
            "eucalyptus-robusta-b" or "angel-oak-spanish-moss" => Tropical,
            "evergreen" or "cilician-fir" or "vendor-balsam-fir-classic" or
            "fraser-fir-large" or "fraser-fir-small" or "fraser-fir-snowy" => Mountain,
            _ => Deciduous
        };
    }
}
