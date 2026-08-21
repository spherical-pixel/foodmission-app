using NUnit.Framework;
using Unity.AppUI.Redux;
using eu.foodmission.platform;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class AppReducersTests
    {
        private AppState m_InitialState;

        [SetUp]
        public void SetUp()
        {
            m_InitialState = new AppState();
        }

        [Test]
        public void SetThemeReducer_UpdatesTheme()
        {
            // Arrange
            var action = AppActions.setTheme.Invoke("dark");

            // Act
            var newState = AppReducers.SetThemeReducer(m_InitialState, action);

            // Assert
            Assert.AreEqual("dark", newState.theme);
        }

        [Test]
        public void SetLanguageReducer_UpdatesLanguage()
        {
            // Arrange
            var action = AppActions.setLanguage.Invoke("en");

            // Act
            var newState = AppReducers.SetLanguageReducer(m_InitialState, action);

            // Assert
            Assert.AreEqual("en", newState.lang);
        }

        [Test]
        public void CompleteOnboardingReducer_SetsFlagToTrue()
        {
            // Arrange
            var action = AppActions.completeOnboarding.Invoke();

            // Act
            var newState = AppReducers.CompleteOnboardingReducer(m_InitialState, action);

            // Assert
            Assert.IsTrue(newState.hasCompletedOnboarding);
        }

        [Test]
        public void LogoutReducer_ClearsAllSessionData()
        {
            // Arrange
            var stateWithSession = new AppState
            {
                userId = "user123",
                userName = "testuser",
                userEmail = "test@example.com",
                accessToken = "token123",
                tokenType = "Bearer",
                tokenExpiresAt = 1234567890,
                refreshToken = "refresh-token",
                hasCompletedOnboarding = true,
                hasCompletedExtendedProfile = true,
                userOnboardingSurvey = new OnboardingSurveyData { weeklyMeatConsumption = "FIVE_TO_NINE" },
                userMotivation = "PLANETARY_IMPACT",
                userSegment = "BEGINNER",
                theme = "dark",
                scale = "large",
                font = "open-sans"
            };
            var action = AppActions.logout.Invoke();

            // Act
            var newState = AppReducers.LogoutReducer(stateWithSession, action);

            // Assert — session & profile data cleared
            Assert.IsEmpty(newState.userId);
            Assert.IsEmpty(newState.userName);
            Assert.IsEmpty(newState.userEmail);
            Assert.IsEmpty(newState.accessToken);
            Assert.IsEmpty(newState.tokenType);
            Assert.AreEqual(0, newState.tokenExpiresAt);
            Assert.IsEmpty(newState.refreshToken);
            Assert.IsFalse(newState.hasCompletedOnboarding);
            Assert.IsFalse(newState.hasCompletedExtendedProfile);
            Assert.IsFalse(newState.hasSkippedExtendedProfile);
            Assert.IsFalse(newState.userOnboardingSurvey.HasAnswers());
            Assert.IsEmpty(newState.userMotivation);
            Assert.IsEmpty(newState.userSegment);
            // Preferences reset to defaults
            Assert.AreEqual("system", newState.theme);
            Assert.AreEqual("medium", newState.scale);
            Assert.AreEqual("roboto", newState.font);
        }

        [Test]
        public void SetScaleReducer_UpdatesScale()
        {
            var action = AppActions.setScale.Invoke("large");

            var newState = AppReducers.SetScaleReducer(m_InitialState, action);

            Assert.AreEqual("large", newState.scale);
        }

        [Test]
        public void SetSoundReducer_UpdatesSoundVolume()
        {
            var action = AppActions.setSound.Invoke(50);

            var newState = AppReducers.SetSoundReducer(m_InitialState, action);

            Assert.AreEqual(50, newState.soundVolume);
        }

        [Test]
        public void SetMusicReducer_UpdatesMusicVolume()
        {
            var action = AppActions.setMusic.Invoke(75);

            var newState = AppReducers.SetMusicReducer(m_InitialState, action);

            Assert.AreEqual(75, newState.musicVolume);
        }

        [Test]
        public void SetPushNotificationsReducer_UpdatesFlag()
        {
            var action = AppActions.setPushNotifications.Invoke(true);

            var newState = AppReducers.SetPushNotificationsReducer(m_InitialState, action);

            Assert.IsTrue(newState.pushNotificationsEnabled);
        }

        [Test]
        public void SetNotificationPreferredTimeReducer_UpdatesTime()
        {
            var action = AppActions.setNotificationPreferredTime.Invoke("14:30");

            var newState = AppReducers.SetNotificationPreferredTimeReducer(m_InitialState, action);

            Assert.AreEqual("14:30", newState.notificationPreferredTime);
        }

        [Test]
        public void SetBackgroundPatternReducer_UpdatesFlag()
        {
            var state = new AppState { backgroundPattern = true };
            var action = AppActions.setBackgroundPattern.Invoke(false);

            var newState = AppReducers.SetBackgroundPatternReducer(state, action);

            Assert.IsFalse(newState.backgroundPattern);
        }

        [Test]
        public void SetUserReducer_UpdatesUserId()
        {
            var action = AppActions.setUser.Invoke("user456");

            var newState = AppReducers.SetUserReducer(m_InitialState, action);

            Assert.AreEqual("user456", newState.userId);
        }

        [Test]
        public void UpdateSessionTimestampReducer_UpdatesTimestamp()
        {
            var action = AppActions.updateSessionTimestamp.Invoke(9999999);
            var newState = AppReducers.UpdateSessionTimestampReducer(m_InitialState, action);
            Assert.AreEqual(9999999, newState.lastSessionTimestamp);
        }

        [Test]
        public void RestoreStateReducer_ReturnsPayloadDirectly()
        {
            var restoredState = new AppState
            {
                userId = "restored-user",
                theme = "dark",
                lang = "en"
            };
            var action = AppActions.restoreState.Invoke(restoredState);

            var newState = AppReducers.RestoreStateReducer(m_InitialState, action);

            Assert.AreEqual("restored-user", newState.userId);
            Assert.AreEqual("dark", newState.theme);
            Assert.AreEqual("en", newState.lang);
        }

    }
}
