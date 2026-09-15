namespace CityForgeV3.UI
{
    // UI time is supplied by the caller so pausing the simulation never pins a notice.
    public sealed class DistrictNoticeLifetime
    {
        public const double DurationSeconds = 6;
        double expiresAt = double.NegativeInfinity;
        public void Show(double now) => expiresAt = now + DurationSeconds;
        public bool Visible(double now) => now < expiresAt;
        public void Clear() => expiresAt = double.NegativeInfinity;
    }
}
