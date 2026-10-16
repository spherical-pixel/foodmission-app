using System;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class SupportContactTests
    {
        private const string BodySeparator = "&body=";

        // The body carries device data (OS, model, network), so tests check the subject exactly and the body by content
        private static (string head, string body) Split(string url)
        {
            int index = url.IndexOf(BodySeparator, StringComparison.Ordinal);
            Assert.GreaterOrEqual(index, 0, "The mail link should have a body: " + url);
            return (url.Substring(0, index), Uri.UnescapeDataString(url.Substring(index + BodySeparator.Length)));
        }

        [Test]
        public void BuildMailtoUrl_EscapesSubjectWithVersionAndPlatform()
        {
            (string head, string body) = Split(SupportContact.BuildMailtoUrl(" Soporte FOODMISSION ", "1.2.3", "Android"));

            Assert.AreEqual("mailto:feedback@foodmission.eu?subject=Soporte%20FOODMISSION%20%281.2.3%2C%20Android%29", head);
            StringAssert.StartsWith("Hello Foodmission Team,", body);
            StringAssert.Contains("- App Version & Build Number: 1.2.3\n", body);
            StringAssert.Contains("- Network Connection: ", body);
        }

        [Test]
        public void BuildMailtoUrl_MissingParts_AreSkipped()
        {
            Assert.AreEqual("mailto:feedback@foodmission.eu?subject=FOODMISSION", Split(SupportContact.BuildMailtoUrl(null, "", null)).head);
            Assert.AreEqual("mailto:feedback@foodmission.eu?subject=Support%20%28IPhonePlayer%29", Split(SupportContact.BuildMailtoUrl("Support", null, "IPhonePlayer")).head);
        }

        [Test]
        public void BuildMailtoUrl_EscapesTheBody()
        {
            string url = SupportContact.BuildMailtoUrl("Support", "1.0", "Android");
            string encodedBody = url.Substring(url.IndexOf(BodySeparator, StringComparison.Ordinal) + BodySeparator.Length);

            Assert.IsFalse(encodedBody.Contains("\n") || encodedBody.Contains(" ") || encodedBody.Contains("&"), encodedBody);
        }
    }
}
