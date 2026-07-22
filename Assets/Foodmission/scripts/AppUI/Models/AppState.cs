using System;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Global app state that persists between sessions.
    /// Uses regular properties instead of init-only to support JsonUtility serialization.
    /// </summary>
    [Serializable]
    public class AppState
    {
        // ==================== Persisted fields ====================

        /// <summary>
        /// Código de idioma: "es", "en", "ca", etc.
        /// </summary>
        public string lang = "none";

        /// <summary>
        /// Visual theme: "light", "dark", "system"
        /// </summary>
        public string theme = "system";// "system";

        /// <summary>
        /// UI scale: "small", "medium", "large"
        /// </summary>
        public string scale = "medium";

        /// <summary>
        /// Font family preference: "roboto", "open-sans", or "open-dyslexic"
        /// </summary>
        public string font = "roboto";

        /// <summary>
        /// Sound effects volume: 0–100 in steps of 5
        /// </summary>
        public int soundVolume = 100;

        /// <summary>
        /// Music volume: 0–100 in steps of 5
        /// </summary>
        public int musicVolume = 100;

        /// <summary>
        /// Whether push notifications are enabled
        /// </summary>
        public bool pushNotificationsEnabled = false;

        /// <summary>
        /// Whether the tile background pattern is shown (false = plain color)
        /// </summary>
        public bool backgroundPattern = true;

        /// <summary>
        /// OnBoarding completed
        /// </summary>
        public bool hasCompletedOnboarding = false;

        /// <summary>
        /// Whether the user has completed the extended profile (onboarding)
        /// </summary>
        public bool hasCompletedExtendedProfile = false;

        /// <summary>
        /// ID of logged user (empty if no session)
        /// </summary>
        public string userId = "";

        /// <summary>
        /// Username (display name, from profile API)
        /// </summary>
        public string userName = "";

        /// <summary>
        /// User email (from profile API, not the login input)
        /// </summary>
        public string userEmail = "";

        /// <summary>
        /// JWT access Token
        /// </summary>
        public string accessToken = "";

        /// <summary>
        /// Token type (Bearer)
        /// </summary>
        public string tokenType = "";

        /// <summary>
        /// Token expiration timestamp (Unix seconds as int)
        /// </summary>
        public int tokenExpiresAt = 0;

        /// <summary>
        /// OAuth2 refresh token (persisted — used to obtain new access tokens)
        /// </summary>
        public string refreshToken = "";

        /// <summary>
        /// Refresh token expiration timestamp (Unix seconds). 0 = unknown.
        /// </summary>
        public int refreshTokenExpiresAt = 0;

        /// <summary>
        /// Last session timestamp (Unix seconds as int)
        /// </summary>
        public int lastSessionTimestamp = 0;

        // ==================== User profile (synced from server) ====================

        public int userYearOfBirth = 0;
        public string userCountry = "";
        public string userRegion = "";
        public string userZip = "";
        public string userGender = "";
        public string userAnnualIncome = "";
        public string userEducationLevel = "";
        public string userActivityLevel = "";
        public string[] userDietaryPreference = new string[0];
        public string userShoppingResponsibility = "";
        public OnboardingSurveyData userOnboardingSurvey = new OnboardingSurveyData();
        public string userLastShoppingListId = "";

        // ==================== Temporal data (not persisted) ====================

        /// <summary>
        /// Is there any authentication operation in progress
        /// </summary>
        public bool isAuthenticating = false;

        /// <summary>
        /// Authentication error message if any
        /// </summary>
        public string authError = "";

        public AddToContextRequestedAction foodInfoAddRequest;

        /// <summary>
        /// Default constructor for JsonUtility
        /// </summary>
        public AppState() { }

        /// <summary>
        /// Creates a copy of this state
        /// </summary>
        public AppState Copy()
        {
            return new AppState
            {
                lang = this.lang,
                theme = this.theme,
                scale = this.scale,
                font = this.font,
                soundVolume = this.soundVolume,
                musicVolume = this.musicVolume,
                pushNotificationsEnabled = this.pushNotificationsEnabled,
                backgroundPattern = this.backgroundPattern,
                hasCompletedOnboarding = this.hasCompletedOnboarding,
                hasCompletedExtendedProfile = this.hasCompletedExtendedProfile,
                userId = this.userId,
                userName = this.userName,
                userEmail = this.userEmail,
                accessToken = this.accessToken,
                tokenType = this.tokenType,
                tokenExpiresAt = this.tokenExpiresAt,
                refreshToken = this.refreshToken,
                refreshTokenExpiresAt = this.refreshTokenExpiresAt,
                lastSessionTimestamp = this.lastSessionTimestamp,
                userYearOfBirth = this.userYearOfBirth,
                userCountry = this.userCountry,
                userRegion = this.userRegion,
                userZip = this.userZip,
                userGender = this.userGender,
                userAnnualIncome = this.userAnnualIncome,
                userEducationLevel = this.userEducationLevel,
                userActivityLevel = this.userActivityLevel,
                userDietaryPreference = this.userDietaryPreference != null ? (string[])this.userDietaryPreference.Clone() : new string[0],
                userShoppingResponsibility = this.userShoppingResponsibility,
                userOnboardingSurvey = this.userOnboardingSurvey,
                userLastShoppingListId = this.userLastShoppingListId,
                isAuthenticating = this.isAuthenticating,
                authError = this.authError,
                foodInfoAddRequest = this.foodInfoAddRequest
            };
        }
    }
}
