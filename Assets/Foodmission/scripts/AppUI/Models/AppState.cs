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
        public string lang = "es";

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
        /// Last session timestamp (Unix seconds as int)
        /// </summary>
        public int lastSessionTimestamp = 0;

        // ==================== Temporal data (not persisted) ====================

        /// <summary>
        /// Is there any authentication operation in progress
        /// </summary>
        public bool isAuthenticating = false;

        /// <summary>
        /// Authentication error message if any
        /// </summary>
        public string authError = "";

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
                hasCompletedOnboarding = this.hasCompletedOnboarding,
                hasCompletedExtendedProfile = this.hasCompletedExtendedProfile,
                userId = this.userId,
                userName = this.userName,
                userEmail = this.userEmail,
                accessToken = this.accessToken,
                tokenType = this.tokenType,
                tokenExpiresAt = this.tokenExpiresAt,
                refreshToken = this.refreshToken,
                lastSessionTimestamp = this.lastSessionTimestamp,
                isAuthenticating = this.isAuthenticating,
                authError = this.authError
            };
        }
    }
}
