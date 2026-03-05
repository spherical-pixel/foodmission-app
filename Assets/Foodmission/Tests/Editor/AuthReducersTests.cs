using NUnit.Framework;
using Unity.AppUI.Redux;
using eu.foodmission.platform;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class AuthReducersTests
    {
        private AppState m_InitialState;

        [SetUp]
        public void SetUp()
        {
            m_InitialState = new AppState();
        }

        [Test]
        public void LoginRequestReducer_SetsIsAuthenticatingToTrue()
        {
            // Arrange
            var action = AppActions.loginRequest.Invoke("testuser");

            // Act
            var newState = AppReducers.LoginRequestReducer(m_InitialState, action);

            // Assert
            Assert.IsTrue(newState.isAuthenticating);
            Assert.IsEmpty(newState.authError);
        }

        [Test]
        public void LoginSuccessReducer_SetsUserIdAndIsAuthenticatingToFalse()
        {
            // Arrange
            var state = m_InitialState with { isAuthenticating = true };
            var payload = new AppActions.LoginPayload(
                userId: "user123",
                email: "test@example.com",
                accessToken: "eyJhbGciOiJIUzI1NiIs...",
                tokenType: "Bearer",
                expiresAt: 1234567890
            );
            var action = AppActions.loginSuccess.Invoke(payload);

            // Act
            var newState = AppReducers.LoginSuccessReducer(state, action);

            // Assert
            Assert.IsFalse(newState.isAuthenticating);
            Assert.AreEqual("user123", newState.userId);
            Assert.AreEqual("test@example.com", newState.userEmail);
            Assert.AreEqual("eyJhbGciOiJIUzI1NiIs...", newState.accessToken);
            Assert.AreEqual("Bearer", newState.tokenType);
            Assert.AreEqual(1234567890, newState.tokenExpiresAt);
        }

        [Test]
        public void LoginFailureReducer_SetsErrorMessageAndIsAuthenticatingToFalse()
        {
            // Arrange
            var state = m_InitialState with { isAuthenticating = true };
            var action = AppActions.loginFailure.Invoke("Error message");

            // Act
            var newState = AppReducers.LoginFailureReducer(state, action);

            // Assert
            Assert.IsFalse(newState.isAuthenticating);
            Assert.AreEqual("Error message", newState.authError);
        }

        [Test]
        public void LogoutReducer_ClearsAllUserData()
        {
            // Arrange
            var state = m_InitialState with
            {
                userId = "user123",
                userEmail = "test@example.com",
                accessToken = "eyJ...",
                tokenType = "Bearer",
                tokenExpiresAt = 1234567890
            };
            var action = AppActions.logout.Invoke();

            // Act
            var newState = AppReducers.LogoutReducer(state, action);

            // Assert
            Assert.IsEmpty(newState.userId);
            Assert.IsEmpty(newState.userEmail);
            Assert.IsEmpty(newState.accessToken);
            Assert.IsEmpty(newState.tokenType);
            Assert.AreEqual(0, newState.tokenExpiresAt);
        }
    }
}
