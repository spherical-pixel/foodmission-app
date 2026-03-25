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
        public string theme = "system";

        /// <summary>
        /// UI scale: "small", "medium", "large"
        /// </summary>
        public string scale = "medium";

        /// <summary>
        /// OnBoarding completed
        /// </summary>
        public bool hasCompletedOnboarding = false;

        /// <summary>
        /// ID of logged user (empty if no session)
        /// </summary>
        public string userId = "";

        /// <summary>
        /// User email
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
                hasCompletedOnboarding = this.hasCompletedOnboarding,
                userId = this.userId,
                userEmail = this.userEmail,
                accessToken = this.accessToken,
                tokenType = this.tokenType,
                tokenExpiresAt = this.tokenExpiresAt,
                lastSessionTimestamp = this.lastSessionTimestamp,
                isAuthenticating = this.isAuthenticating,
                authError = this.authError
            };
        }
    }
}
