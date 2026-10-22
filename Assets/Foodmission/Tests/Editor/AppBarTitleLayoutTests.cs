using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class AppBarTitleLayoutTests
    {
        private const float Gap = AppBarTitleLayout.Gap;

        [Test]
        public void Insets_ShortTitle_StaysCenteredClearOfTheWiderSide()
        {
            (float left, float right) = AppBarTitleLayout.Insets(1000f, 120f, 260f, 300f);

            Assert.AreEqual(260f + Gap, left);
            Assert.AreEqual(260f + Gap, right);
        }

        [Test]
        public void Insets_LongTitle_TakesAllTheSpaceBetweenTheButtons()
        {
            (float left, float right) = AppBarTitleLayout.Insets(1000f, 120f, 260f, 600f);

            Assert.AreEqual(120f + Gap, left);
            Assert.AreEqual(260f + Gap, right);
        }

        [Test]
        public void Insets_NoButtons_OnlyTheGap()
        {
            (float left, float right) = AppBarTitleLayout.Insets(1000f, 0f, 0f, 300f);

            Assert.AreEqual(Gap, left);
            Assert.AreEqual(Gap, right);
        }
    }
}
