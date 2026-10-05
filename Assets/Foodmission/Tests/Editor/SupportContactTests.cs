using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class SupportContactTests
    {
        [Test]
        public void BuildMailtoUrl_EscapesSubjectWithVersionAndPlatform()
        {
            string url = SupportContact.BuildMailtoUrl(" Soporte FOODMISSION ", "1.2.3", "Android");

            Assert.AreEqual("mailto:foodmission@devilishgames.com?subject=Soporte%20FOODMISSION%20%281.2.3%2C%20Android%29", url);
        }

        [Test]
        public void BuildMailtoUrl_MissingParts_AreSkipped()
        {
            Assert.AreEqual("mailto:foodmission@devilishgames.com?subject=FOODMISSION", SupportContact.BuildMailtoUrl(null, "", null));
            Assert.AreEqual("mailto:foodmission@devilishgames.com?subject=Support%20%28IPhonePlayer%29", SupportContact.BuildMailtoUrl("Support", null, "IPhonePlayer"));
        }
    }
}
