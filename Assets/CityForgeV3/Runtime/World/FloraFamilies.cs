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
            "date-palm" or "date-palm-tall" or "date-palm-short" or
            "la-fan-palm-a" or "la-fan-palm-b" or
            "la-fan-palm-a-medium" or "la-fan-palm-b-medium" or
            "camphor-tree" or "eucalyptus-robusta-a" or
            "eucalyptus-robusta-b" or "angel-oak-spanish-moss" => Tropical,
            "evergreen" or "cilician-fir" or "vendor-balsam-fir-classic" or
            "fraser-fir-large" or "fraser-fir-small" or "fraser-fir-snowy" or
            "medium-balsam-fir" or "medium-fraser-fir" or "medium-blue-spruce" => Mountain,
            _ => Deciduous
        };
    }
}
