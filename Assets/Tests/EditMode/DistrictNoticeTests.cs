using CityForgeV3.UI;
using NUnit.Framework;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class DistrictNoticeTests
    {
        [Test] public void ExpiresAfterSixRealSecondsWithoutSimulationTicks()
        {
            var notice = new DistrictNoticeLifetime();
            Assert.False(notice.Visible(10));
            notice.Show(10);
            Assert.True(notice.Visible(15.9));
            Assert.False(notice.Visible(16));
            Assert.False(notice.Visible(100));
        }
        [Test] public void NewMessageGetsItsOwnLifetimeAndNavigationCanClearIt()
        {
            var notice = new DistrictNoticeLifetime();
            notice.Show(10); notice.Show(15);
            Assert.True(notice.Visible(20));
            Assert.False(notice.Visible(21));
            notice.Show(30); notice.Clear();
            Assert.False(notice.Visible(30));
        }
    }
}
