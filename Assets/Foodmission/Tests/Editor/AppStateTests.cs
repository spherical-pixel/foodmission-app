using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class AppStateTests
    {
        [Test]
        public void Copy_ReturnsDeepCloneWithEqualValues()
        {
            var state = new AppState
            {
                lang = "ca",
                theme = "dark",
                scale = "large",
                font = "open-dyslexic",
                soundVolume = 50,
                musicVolume = 75,
                pushNotificationsEnabled = true,
                backgroundPattern = false,
                hasCompletedOnboarding = true,
                userId = "user-1",
                userName = "testuser",
                accessToken = "tok-123",
                tokenType = "Bearer",
                tokenExpiresAt = 999999,
                refreshToken = "ref-123",
                userFirstName = "Test",
                userWeightKg = 70.5f,
                userHeightCm = 175f,
                isAuthenticating = false,
                authError = ""
            };
            var copy = state.Copy();
            Assert.AreEqual("ca", copy.lang);
            Assert.AreEqual("dark", copy.theme);
            Assert.AreEqual("user-1", copy.userId);
            Assert.AreEqual(70.5f, copy.userWeightKg, 0.001f);
            Assert.AreEqual(175f, copy.userHeightCm, 0.001f);
        }

        [Test]
        public void Copy_ReturnsIndependentClone()
        {
            var state = new AppState { userId = "original", lang = "en" };
            var copy = state.Copy();
            copy.userId = "modified";
            copy.lang = "es";
            Assert.AreEqual("original", state.userId);
            Assert.AreEqual("en", state.lang);
        }

        [Test]
        public void Copy_MaintainsDefaultValues()
        {
            var state = new AppState();
            var copy = state.Copy();
            Assert.AreEqual("none", copy.lang);
            Assert.AreEqual("system", copy.theme);
            Assert.AreEqual("medium", copy.scale);
            Assert.AreEqual("roboto", copy.font);
            Assert.IsFalse(copy.hasCompletedOnboarding);
            Assert.IsEmpty(copy.userId);
            Assert.AreEqual(100, copy.soundVolume);
            Assert.IsTrue(copy.backgroundPattern);
        }

        [Test]
        public void Roundtrips_Via_JsonUtility()
        {
            var state = new AppState
            {
                lang = "es",
                theme = "light",
                userId = "user-1",
                accessToken = "tok-456",
                tokenExpiresAt = 12345,
                hasCompletedOnboarding = true
            };
            string json = JsonUtility.ToJson(state);
            var result = JsonUtility.FromJson<AppState>(json);
            Assert.AreEqual("es", result.lang);
            Assert.AreEqual("light", result.theme);
            Assert.AreEqual("user-1", result.userId);
            Assert.AreEqual("tok-456", result.accessToken);
            Assert.AreEqual(12345, result.tokenExpiresAt);
            Assert.IsTrue(result.hasCompletedOnboarding);
        }

        [Test]
        public void Copy_PreservesAllProfileFields()
        {
            var state = new AppState
            {
                userFirstName = "John",
                userLastName = "Doe",
                userYearOfBirth = 1990,
                userCountry = "ES",
                userRegion = "CT",
                userZip = "08001",
                userGender = "M",
                userAnnualIncome = "MEDIUM",
                userEducationLevel = "BACHELOR",
                userActivityLevel = "ACTIVE"
            };
            var copy = state.Copy();
            Assert.AreEqual("John", copy.userFirstName);
            Assert.AreEqual("Doe", copy.userLastName);
            Assert.AreEqual(1990, copy.userYearOfBirth);
            Assert.AreEqual("ES", copy.userCountry);
            Assert.AreEqual("CT", copy.userRegion);
            Assert.AreEqual("08001", copy.userZip);
            Assert.AreEqual("MEDIUM", copy.userAnnualIncome);
        }
    }
}
