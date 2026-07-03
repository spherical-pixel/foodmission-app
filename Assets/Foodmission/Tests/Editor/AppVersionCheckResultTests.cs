using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class AppVersionCheckResultTests
    {
        [Test]
        public void PlatformVersionInfo_Roundtrips_Via_JsonUtility()
        {
            var info = new PlatformVersionInfo
            {
                latestVersion = "1.2.3",
                isForced = true,
                storeUrl = "https://testflight.apple.com/join/xxx",
                releaseNotes = "Bug fixes"
            };
            string json = JsonUtility.ToJson(info);
            var result = JsonUtility.FromJson<PlatformVersionInfo>(json);
            Assert.AreEqual("1.2.3", result.latestVersion);
            Assert.IsTrue(result.isForced);
            Assert.AreEqual("https://testflight.apple.com/join/xxx", result.storeUrl);
            Assert.AreEqual("Bug fixes", result.releaseNotes);
        }

        [Test]
        public void AppVersionCheckResponse_Roundtrips_Via_JsonUtility()
        {
            var resp = new AppVersionCheckResponse
            {
                ios = new PlatformVersionInfo { latestVersion = "2.0.0", isForced = false },
                android = new PlatformVersionInfo { latestVersion = "2.0.0", isForced = true }
            };
            string json = JsonUtility.ToJson(resp);
            var result = JsonUtility.FromJson<AppVersionCheckResponse>(json);
            Assert.AreEqual("2.0.0", result.ios.latestVersion);
            Assert.IsFalse(result.ios.isForced);
            Assert.IsTrue(result.android.isForced);
        }
    }

    [TestFixture]
    public class PlatformVersionInfoTests
    {
        private PlatformVersionInfo CreateInfoWithNotes(string englishNotes = "English notes")
        {
            return new PlatformVersionInfo
            {
                latestVersion = "1.0.0",
                releaseNotes = englishNotes,
                releaseNotes_nl = "Dutch notes",
                releaseNotes_de = "German notes",
                releaseNotes_el = "Greek notes",
                releaseNotes_it = "Italian notes",
                releaseNotes_no = "Norwegian notes",
                releaseNotes_pl = "Polish notes",
                releaseNotes_sl = "Slovenian notes",
                releaseNotes_es = "Spanish notes"
            };
        }

        [Test]
        public void GetLocalizedReleaseNotes_DefaultLocale_ReturnsEnglish()
        {
            var info = CreateInfoWithNotes();
            Assert.AreEqual("English notes", info.GetLocalizedReleaseNotes("en"));
        }

        [Test]
        public void GetLocalizedReleaseNotes_UnknownLocale_ReturnsEnglish()
        {
            var info = CreateInfoWithNotes();
            Assert.AreEqual("English notes", info.GetLocalizedReleaseNotes("fr"));
        }

        [Test]
        public void GetLocalizedReleaseNotes_SpanishLocale_ReturnsSpanish()
        {
            var info = CreateInfoWithNotes();
            Assert.AreEqual("Spanish notes", info.GetLocalizedReleaseNotes("es"));
        }

        [Test]
        public void GetLocalizedReleaseNotes_LocalizedFieldNull_FallsBackToEnglish()
        {
            var info = new PlatformVersionInfo
            {
                releaseNotes = "English notes",
                releaseNotes_es = null
            };
            Assert.AreEqual("English notes", info.GetLocalizedReleaseNotes("es"));
        }

        [TestCase("nl", "Dutch notes")]
        [TestCase("de", "German notes")]
        [TestCase("el", "Greek notes")]
        [TestCase("it", "Italian notes")]
        [TestCase("no", "Norwegian notes")]
        [TestCase("pl", "Polish notes")]
        [TestCase("sl", "Slovenian notes")]
        [TestCase("es", "Spanish notes")]
        public void GetLocalizedReleaseNotes_SupportedLocales_ReturnsCorrectValue(string locale, string expected)
        {
            var info = CreateInfoWithNotes();
            Assert.AreEqual(expected, info.GetLocalizedReleaseNotes(locale));
        }
    }
}
