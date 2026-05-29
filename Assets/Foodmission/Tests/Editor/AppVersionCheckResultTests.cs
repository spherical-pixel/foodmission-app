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
}
