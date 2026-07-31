using System.Threading.Tasks;

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
            var state = new AppState { userId = "stale-user", authError = "previous error" };
            var action = AppActions.loginRequest.Invoke("testuser");

            // Act
            var newState = AppReducers.LoginRequestReducer(state, action);

            // Assert
            Assert.IsTrue(newState.isAuthenticating);
            Assert.IsEmpty(newState.authError);
            Assert.IsEmpty(newState.userId); // must clear userId so navigation guard doesn't fire prematurely
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
                tokenExpiresAt = 1234567890,
                refreshToken = "refresh-token",
                theme = "dark",
                scale = "large",
                font = "open-sans"
            };
            var action = AppActions.logout.Invoke();

            // Act
            var newState = AppReducers.LogoutReducer(state, action);

            // Assert — session data cleared
            Assert.IsEmpty(newState.userId);
            Assert.IsEmpty(newState.userName);
            Assert.IsEmpty(newState.userEmail);
            Assert.IsEmpty(newState.accessToken);
            Assert.IsEmpty(newState.tokenType);
            Assert.AreEqual(0, newState.tokenExpiresAt);
            Assert.IsEmpty(newState.refreshToken);
            // Preferences reset to defaults
            Assert.AreEqual("system", newState.theme);
            Assert.AreEqual("medium", newState.scale);
            Assert.AreEqual("roboto", newState.font);
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
        public async Task AuthService_RefreshAsync_WithNoRefreshToken_ReturnsFalse()
        {
            UnityEngine.PlayerPrefs.DeleteAll();
            var localStorageService = new LocalStorageService();
            var storeService = new StoreService(localStorageService);
            // AppState starts with empty refreshToken (default)
            var authService = new AuthService(storeService);

            var result = await authService.RefreshAsync();

            storeService.Dispose();
            Assert.IsFalse(result);
        }

        [Test]
        public async Task AuthService_HandleUnauthorizedAsync_WithNoRefreshToken_LogsOutAndFiresOnSessionExpired()
        {
            UnityEngine.PlayerPrefs.DeleteAll();
            var localStorageService = new LocalStorageService();
            var storeService = new StoreService(localStorageService);
            var authService = new AuthService(storeService);

            bool sessionExpiredFired = false;
            authService.OnSessionExpired += () => sessionExpiredFired = true;

            var result = await authService.HandleUnauthorizedAsync();

            storeService.Dispose();
            Assert.IsFalse(result);
            Assert.IsTrue(sessionExpiredFired);
        }

        [Test]
        public void AppState_Copy_IncludesFont()
        {
            var state = new AppState { font = "open-sans" };

            var copy = state.Copy();

            Assert.AreEqual("open-sans", copy.font);
        }

        [Test]
        public void SetFontReducer_UpdatesFontField()
        {
            var state = new AppState { font = "roboto" };
            var action = AppActions.setFont.Invoke("open-dyslexic");

            var newState = AppReducers.SetFontReducer(state, action);

            Assert.AreEqual("open-dyslexic", newState.font);
        }

        [Test]
        public void RegisterRequestReducer_SetsIsAuthenticatingToTrue()
        {
            var state = new AppState { authError = "previous error" };
            var action = AppActions.registerRequest.Invoke();

            var newState = AppReducers.RegisterRequestReducer(state, action);

            Assert.IsTrue(newState.isAuthenticating);
            Assert.IsEmpty(newState.authError);
        }

        [Test]
        public void RegisterSuccessReducer_SetsUserIdAndIsAuthenticatingToFalse()
        {
            var state = new AppState { isAuthenticating = true };
            var action = AppActions.registerSuccess.Invoke("new-user-id");

            var newState = AppReducers.RegisterSuccessReducer(state, action);

            Assert.IsFalse(newState.isAuthenticating);
            Assert.AreEqual("new-user-id", newState.userId);
            Assert.IsEmpty(newState.authError);
        }

        [Test]
        public void RegisterFailureReducer_SetsErrorAndIsAuthenticatingToFalse()
        {
            var state = new AppState { isAuthenticating = true };
            var action = AppActions.registerFailure.Invoke("Email already in use");

            var newState = AppReducers.RegisterFailureReducer(state, action);

            Assert.IsFalse(newState.isAuthenticating);
            Assert.AreEqual("Email already in use", newState.authError);
        }

        [Test]
        public void SetExtendedProfileReducer_SetsHasCompletedExtendedProfileToTrue()
        {
            var state = new AppState { hasCompletedExtendedProfile = false };
            var action = AppActions.setExtendedProfile.Invoke();

            var newState = AppReducers.SetExtendedProfileReducer(state, action);

            Assert.IsTrue(newState.hasCompletedExtendedProfile);
        }

        // ── ProfileSyncedReducer ──────────────────────────────────────────────

        [Test]
        public void ProfileSyncedReducer_AppliesProfileFields()
        {
            var state = new AppState();
            var payload = new AppActions.ProfilePayload(
                yearOfBirth: 1990,
                country: "ES", region: "ES-VC", zip: "03450",
                gender: "MALE", annualIncome: "FROM_20000_TO_34999",
                educationLevel: "UNIVERSITY", activityLevel: "MODERATE"
            );
            var action = AppActions.profileSynced.Invoke(payload);

            var newState = AppReducers.ProfileSyncedReducer(state, action);

            Assert.AreEqual(1990, newState.userYearOfBirth);
            Assert.AreEqual("ES", newState.userCountry);
            Assert.AreEqual("ES-VC", newState.userRegion);
            Assert.AreEqual("03450", newState.userZip);
            Assert.AreEqual("MALE", newState.userGender);
            Assert.AreEqual("FROM_20000_TO_34999", newState.userAnnualIncome);
            Assert.AreEqual("UNIVERSITY", newState.userEducationLevel);
            Assert.AreEqual("MODERATE", newState.userActivityLevel);
        }

        [Test]
        public void ProfileSyncedReducer_AppliesPreferences()
        {
            var state = new AppState();
            var payload = new AppActions.ProfilePayload(
                yearOfBirth: 0,
                country: "", region: "", zip: "",
                gender: "", annualIncome: "", educationLevel: "", activityLevel: "",
                dietaryPreference: new[] { "VEGETARIAN", "GLUTEN_FREE" }, shoppingResponsibility: "PRIMARY"
            );
            var action = AppActions.profileSynced.Invoke(payload);

            var newState = AppReducers.ProfileSyncedReducer(state, action);

            CollectionAssert.AreEqual(new[] { "VEGETARIAN", "GLUTEN_FREE" }, newState.userDietaryPreference);
            Assert.AreEqual("PRIMARY", newState.userShoppingResponsibility);
        }

        [Test]
        public void ProfileSyncedReducer_WithNullPreferences_KeepsEmptyDefaults()
        {
            var state = new AppState { userDietaryPreference = new[] { "VEGAN" } };
            var payload = new AppActions.ProfilePayload(
                yearOfBirth: 0,
                country: "", region: "", zip: "",
                gender: "", annualIncome: "", educationLevel: "", activityLevel: ""
            );
            var action = AppActions.profileSynced.Invoke(payload);

            var newState = AppReducers.ProfileSyncedReducer(state, action);

            // Null preferences (default "") overwrite any stale local value
            Assert.IsEmpty(newState.userDietaryPreference);
            Assert.IsEmpty(newState.userShoppingResponsibility);
        }

        [Test]
        public void ProfileSyncedReducer_WithValidSettings_AppliesSettings()
        {
            var state = new AppState { theme = "system", scale = "medium", soundVolume = 100 };
            var settings = new UserSettingsDto
            {
                theme = "dark", scale = "large", font = "open-sans",
                soundVolume = 80, musicVolume = 60,
                pushNotificationsEnabled = true, backgroundPattern = false
            };
            var payload = new AppActions.ProfilePayload(
                yearOfBirth: 0,
                country: "", region: "", zip: "",
                gender: "", annualIncome: "", educationLevel: "", activityLevel: "",
                settings: settings
            );
            var action = AppActions.profileSynced.Invoke(payload);

            var newState = AppReducers.ProfileSyncedReducer(state, action);

            Assert.AreEqual("dark", newState.theme);
            Assert.AreEqual("large", newState.scale);
            Assert.AreEqual("open-sans", newState.font);
            Assert.AreEqual(80, newState.soundVolume);
            Assert.AreEqual(60, newState.musicVolume);
            Assert.IsTrue(newState.pushNotificationsEnabled);
            Assert.IsFalse(newState.backgroundPattern);
        }

        [Test]
        public void ProfileSyncedReducer_WithEmptySettings_KeepsLocalSettings()
        {
            // Server returns settings:{} — theme is empty — local settings must not be overwritten
            var state = new AppState { theme = "dark", scale = "large", soundVolume = 50 };
            var emptySettings = new UserSettingsDto(); // theme == null
            var payload = new AppActions.ProfilePayload(
                yearOfBirth: 0,
                country: "", region: "", zip: "",
                gender: "", annualIncome: "", educationLevel: "", activityLevel: "",
                settings: emptySettings
            );
            var action = AppActions.profileSynced.Invoke(payload);

            var newState = AppReducers.ProfileSyncedReducer(state, action);

            Assert.AreEqual("dark", newState.theme);
            Assert.AreEqual("large", newState.scale);
            Assert.AreEqual(50, newState.soundVolume);
        }

        [Test]
        public void ProfileSyncedReducer_WithNullSettings_KeepsLocalSettings()
        {
            var state = new AppState { theme = "dark" };
            var payload = new AppActions.ProfilePayload(
                yearOfBirth: 0,
                country: "", region: "", zip: "",
                gender: "", annualIncome: "", educationLevel: "", activityLevel: "",
                settings: null
            );
            var action = AppActions.profileSynced.Invoke(payload);

            var newState = AppReducers.ProfileSyncedReducer(state, action);

            Assert.AreEqual("dark", newState.theme);
        }

        [Test]
        public void ProfileSyncedReducer_WithLanguage_UpdatesLang()
        {
            var state = new AppState { lang = "en" };
            var payload = new AppActions.ProfilePayload(
                yearOfBirth: 0,
                country: "", region: "", zip: "",
                gender: "", annualIncome: "", educationLevel: "", activityLevel: "",
                language: "es"
            );
            var action = AppActions.profileSynced.Invoke(payload);

            var newState = AppReducers.ProfileSyncedReducer(state, action);

            Assert.AreEqual("es", newState.lang);
        }

        [Test]
        public void ProfileSyncedReducer_WithNullLanguage_KeepsExistingLang()
        {
            var state = new AppState { lang = "en" };
            var payload = new AppActions.ProfilePayload(
                yearOfBirth: 0,
                country: "", region: "", zip: "",
                gender: "", annualIncome: "", educationLevel: "", activityLevel: "",
                language: null
            );
            var action = AppActions.profileSynced.Invoke(payload);

            var newState = AppReducers.ProfileSyncedReducer(state, action);

            Assert.AreEqual("en", newState.lang);
        }

        [Test]
        public void LogoutReducer_ClearsProfileFields()
        {
            var state = new AppState
            {
                userYearOfBirth = 1990, userCountry = "ES",
                userGender = "MALE", userActivityLevel = "MODERATE",
                userDietaryPreference = new[] { "VEGETARIAN", "GLUTEN_FREE" }, userShoppingResponsibility = "PRIMARY"
            };
            var action = AppActions.logout.Invoke();

            var newState = AppReducers.LogoutReducer(state, action);

            Assert.AreEqual(0, newState.userYearOfBirth);
            Assert.IsEmpty(newState.userCountry);
            Assert.IsEmpty(newState.userGender);
            Assert.IsEmpty(newState.userActivityLevel);
            Assert.IsEmpty(newState.userDietaryPreference);
            Assert.IsEmpty(newState.userShoppingResponsibility);
        }

        [Test]
        public void AppState_Copy_IncludesProfileFields()
        {
            var state = new AppState
            {
                userYearOfBirth = 1990, userCountry = "ES", userRegion = "ES-VC",
                userZip = "03450", userGender = "MALE",
                userAnnualIncome = "FROM_20000_TO_34999",
                userEducationLevel = "UNIVERSITY", userActivityLevel = "MODERATE",
                userDietaryPreference = new[] { "VEGETARIAN" }, userShoppingResponsibility = "PRIMARY"
            };

            var copy = state.Copy();

            Assert.AreEqual(1990, copy.userYearOfBirth);
            Assert.AreEqual("ES", copy.userCountry);
            Assert.AreEqual("ES-VC", copy.userRegion);
            Assert.AreEqual("03450", copy.userZip);
            Assert.AreEqual("MALE", copy.userGender);
            Assert.AreEqual("FROM_20000_TO_34999", copy.userAnnualIncome);
            Assert.AreEqual("UNIVERSITY", copy.userEducationLevel);
            Assert.AreEqual("MODERATE", copy.userActivityLevel);
            CollectionAssert.AreEqual(new[] { "VEGETARIAN" }, copy.userDietaryPreference);
            Assert.AreEqual("PRIMARY", copy.userShoppingResponsibility);
        }
    }
}
