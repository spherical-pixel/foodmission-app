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
            var stateWithSession = m_InitialState with
            {
                userId = "user123",
                userEmail = "test@example.com",
                accessToken = "token123",
                tokenType = "Bearer",
                tokenExpiresAt = 1234567890
            };
            var action = AppActions.logout.Invoke();

            // Act
            var newState = AppReducers.LogoutReducer(stateWithSession, action);

            // Assert
            Assert.IsEmpty(newState.userId);
            Assert.IsEmpty(newState.userEmail);
            Assert.IsEmpty(newState.accessToken);
            Assert.IsEmpty(newState.tokenType);
            Assert.AreEqual(0, newState.tokenExpiresAt);
        }

    }
}
