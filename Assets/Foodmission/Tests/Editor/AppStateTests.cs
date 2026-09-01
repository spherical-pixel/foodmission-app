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
                isAuthenticating = false,
                authError = ""
            };
            var copy = state.Copy();
            Assert.AreEqual("ca", copy.lang);
            Assert.AreEqual("dark", copy.theme);
            Assert.AreEqual("user-1", copy.userId);
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
            Assert.AreEqual(1990, copy.userYearOfBirth);
            Assert.AreEqual("ES", copy.userCountry);
            Assert.AreEqual("CT", copy.userRegion);
            Assert.AreEqual("08001", copy.userZip);
            Assert.AreEqual("MEDIUM", copy.userAnnualIncome);
        }

        [Test]
        public void Copy_PreservesPilotSurveyCycleStateAndConsent()
        {
            var state = new AppState
            {
                pilotConsentAccepted = true,
                pilotSurveyCycleState = new PilotSurveyCycleState
                {
                    currentCycle = 2,
                    cycleStartDate = "2026-08-01",
                    activeDatesInCycle = new System.Collections.Generic.List<string> { "2026-08-01", "2026-08-02" },
                    completedSlugsInCycle = new System.Collections.Generic.List<string> { "second-use" }
                }
            };

            var copy = state.Copy();
            Assert.IsTrue(copy.pilotConsentAccepted);
            Assert.IsNotNull(copy.pilotSurveyCycleState);
            Assert.AreEqual(2, copy.pilotSurveyCycleState.currentCycle);
            Assert.AreEqual(2, copy.pilotSurveyCycleState.activeDatesInCycle.Count);
            Assert.AreEqual("second-use", copy.pilotSurveyCycleState.completedSlugsInCycle[0]);

            // Independent instance
            copy.pilotSurveyCycleState.currentCycle = 3;
            Assert.AreEqual(2, state.pilotSurveyCycleState.currentCycle);
        }
    }
}
