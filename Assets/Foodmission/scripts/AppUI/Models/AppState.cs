using System;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Global app state that persists between sessions.
    /// </summary>
    [Serializable]
    public record AppState
    {
        // ==================== Persisted fields ====================

        /// <summary>
        /// Código de idioma: "es", "en", "ca", etc.
        /// </summary>
        public string lang { get; init; } = "es";

        /// <summary>
        /// Visual theme: "light", "dark", "system"
        /// </summary>
        public string theme { get; init; } = "system";

        /// <summary>
        /// UI scale: "small", "medium", "large"
        /// </summary>
        public string scale { get; init; } = "medium";

        /// <summary>
        /// OnBoarding completed
        /// </summary>
        public bool hasCompletedOnboarding { get; init; } = false;

        /// <summary>
        /// ID of logged user (empty if no session)
        /// </summary>
        public string userId { get; init; } = "";

        /// <summary>
        /// User email
        /// </summary>
        public string userEmail { get; init; } = "";

        /// <summary>
        /// JWT access Token
        /// </summary>
        public string accessToken { get; init; } = "";

        /// <summary>
        /// Token type (Bearer)
        /// </summary>
        public string tokenType { get; init; } = "";

        /// <summary>
        /// Token expiration timestamp
        /// </summary>
        public long tokenExpiresAt { get; init; } = 0;

        /// <summary>
        /// Last session timestamp
        /// </summary>
        public long lastSessionTimestamp { get; init; } = 0;

        // ==================== Temporal data (not persisted) ====================

        /// <summary>
        /// Is there any authentication operation in progress
        /// </summary>
        public bool isAuthenticating { get; init; } = false;

        /// <summary>        
        /// Authentication error message if any
        /// </summary>
        public string authError { get; init; } = "";

    }

}
