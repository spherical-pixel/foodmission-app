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
            var state = new AppState { isAuthenticating = true };
            var payload = new AppActions.LoginPayload(
                userId: "user123",
                userName: "testuser",
                email: "test@example.com",
                accessToken: "eyJhbGciOiJIUzI1NiIs...",
                tokenType: "Bearer",
                expiresAt: 1234567890,
                refreshToken: "refresh-token-value"
            );
            var action = AppActions.loginSuccess.Invoke(payload);

            // Act
            var newState = AppReducers.LoginSuccessReducer(state, action);

            // Assert
            Assert.IsFalse(newState.isAuthenticating);
            Assert.AreEqual("user123", newState.userId);
            Assert.AreEqual("testuser", newState.userName);
            Assert.AreEqual("test@example.com", newState.userEmail);
            Assert.AreEqual("eyJhbGciOiJIUzI1NiIs...", newState.accessToken);
            Assert.AreEqual("Bearer", newState.tokenType);
            Assert.AreEqual(1234567890, newState.tokenExpiresAt);
            Assert.AreEqual("refresh-token-value", newState.refreshToken);
        }

        [Test]
        public void LoginFailureReducer_SetsErrorMessageAndIsAuthenticatingToFalse()
        {
            // Arrange
            var state = new AppState { isAuthenticating = true };
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
            var state = new AppState
            {
                userId = "user123",
                userName = "testuser",
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
            Assert.IsEmpty(newState.userName);
            Assert.IsEmpty(newState.userEmail);
            Assert.IsEmpty(newState.accessToken);
            Assert.IsEmpty(newState.tokenType);
            Assert.AreEqual(0, newState.tokenExpiresAt);
        }

        [Test]
        public void AppState_Copy_IncludesRefreshToken()
        {
            var state = new AppState { refreshToken = "my-refresh-token" };

            var copy = state.Copy();

            Assert.AreEqual("my-refresh-token", copy.refreshToken);
        }

        [Test]
        public void TokenRefreshedReducer_UpdatesTokenFieldsOnly()
        {
            var state = new AppState
            {
                userId = "user123",
                userName = "testuser",
                accessToken = "old-token",
                tokenType = "Bearer",
                tokenExpiresAt = 1000,
                refreshToken = "old-refresh"
            };
            var payload = new AppActions.TokenRefreshPayload(
                accessToken: "new-token",
                tokenType: "Bearer",
                expiresAt: 9999,
                refreshToken: "new-refresh"
            );
            var action = AppActions.tokenRefreshed.Invoke(payload);

            var newState = AppReducers.TokenRefreshedReducer(state, action);

            Assert.AreEqual("new-token", newState.accessToken);
            Assert.AreEqual("Bearer", newState.tokenType);
            Assert.AreEqual(9999, newState.tokenExpiresAt);
            Assert.AreEqual("new-refresh", newState.refreshToken);
            // User identity fields must not change
            Assert.AreEqual("user123", newState.userId);
            Assert.AreEqual("testuser", newState.userName);
        }

        [Test]
        public void TokenRefreshedReducer_WithEmptyRefreshToken_KeepsExistingRefreshToken()
        {
            var state = new AppState { refreshToken = "existing-refresh", accessToken = "old" };
            var payload = new AppActions.TokenRefreshPayload(
                accessToken: "new-token",
                tokenType: "Bearer",
                expiresAt: 9999,
                refreshToken: ""
            );
            var action = AppActions.tokenRefreshed.Invoke(payload);

            var newState = AppReducers.TokenRefreshedReducer(state, action);

            Assert.AreEqual("existing-refresh", newState.refreshToken);
        }

        [Test]
        public void LoginSuccessReducer_StoresRefreshToken()
        {
            var state = new AppState();
            var payload = new AppActions.LoginPayload(
                userId: "user123",
                userName: "testuser",
                email: "test@example.com",
                accessToken: "eyJ...",
                tokenType: "Bearer",
                expiresAt: 1234567890,
                refreshToken: "my-refresh-token"
            );
            var action = AppActions.loginSuccess.Invoke(payload);

            var newState = AppReducers.LoginSuccessReducer(state, action);

            Assert.AreEqual("my-refresh-token", newState.refreshToken);
        }

        [Test]
        public void LogoutReducer_ClearsRefreshToken()
        {
            var state = new AppState { refreshToken = "my-refresh-token" };
            var action = AppActions.logout.Invoke();

            var newState = AppReducers.LogoutReducer(state, action);

            Assert.IsEmpty(newState.refreshToken);
        }

        [Test]
        public void AuthService_RefreshAsync_WithNoRefreshToken_ReturnsFalse()
        {
            var localStorageService = new LocalStorageService();
            var storeService = new StoreService(localStorageService);
            // AppState starts with empty refreshToken (default)
            var authService = new AuthService(storeService);

            var task = authService.RefreshAsync();
            task.Wait();

            Assert.IsFalse(task.Result);
        }
    }
}
