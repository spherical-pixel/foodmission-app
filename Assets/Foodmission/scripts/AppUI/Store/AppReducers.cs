using Unity.AppUI.Redux;

namespace eu.foodmission.platform
{
    // ==================== App Actions ====================

    /// <summary>
    /// Actions for application state
    /// </summary>
    public static class AppActions
    {
        // Preferences
        public static readonly ActionCreator<string> setTheme = "app/setTheme";
        public static readonly ActionCreator<string> setLanguage = "app/setLanguage";
        public static readonly ActionCreator<string> setScale = "app/setScale";
        public static readonly ActionCreator completeOnboarding = "app/completeOnboarding";
        public static readonly ActionCreator<string> setUser = "app/setUser";
        public static readonly ActionCreator logout = "app/logout";
        public static readonly ActionCreator<long> updateSessionTimestamp = "app/updateSessionTimestamp";
        public static readonly ActionCreator<AppState> restoreState = "app/restoreState";

        
        // Auth
        public static readonly ActionCreator<string> loginRequest = "app/loginRequest";
        public static readonly ActionCreator<LoginPayload> loginSuccess = "app/loginSuccess";
        public static readonly ActionCreator<string> loginFailure = "app/loginFailure";

        /// <summary>
        /// Payload for successful login with all session data
        /// </summary>
        public readonly struct LoginPayload
        {
            public readonly string userId;
            public readonly string email;
            public readonly string accessToken;
            public readonly string tokenType;
            public readonly long expiresAt;

            public LoginPayload(string userId, string email, string accessToken, string tokenType, long expiresAt)
            {
                this.userId = userId;
                this.email = email;
                this.accessToken = accessToken;
                this.tokenType = tokenType;
                this.expiresAt = expiresAt;
            }
        }
        public static readonly ActionCreator registerRequest = "app/registerRequest";
        public static readonly ActionCreator<string> registerSuccess = "app/registerSuccess";
        public static readonly ActionCreator<string> registerFailure = "app/registerFailure";
        
    }

    // ==================== App Reducers ====================

    /// <summary>
    /// Reducers for application state
    /// </summary>
    public static class AppReducers
    {
        // Preferences

        public static AppState SetThemeReducer(AppState state, IAction<string> action)
        {
            return state with { theme = action.payload };
        }

        public static AppState SetLanguageReducer(AppState state, IAction<string> action)
        {
            return state with { lang = action.payload };
        }

        public static AppState SetScaleReducer(AppState state, IAction<string> action)
        {
            return state with { scale = action.payload };
        }

        public static AppState CompleteOnboardingReducer(AppState state, IAction action)
        {
            return state with { hasCompletedOnboarding = true };
        }

        public static AppState SetUserReducer(AppState state, IAction<string> action)
        {
            return state with { userId = action.payload };
        }

        public static AppState LogoutReducer(AppState state, IAction action)
        {
            return state with
            {
                userId = "",
                userEmail = "",
                accessToken = "",
                tokenType = "",
                tokenExpiresAt = 0
            };
        }

        public static AppState UpdateSessionTimestampReducer(AppState state, IAction<long> action)
        {
            return state with { lastSessionTimestamp = action.payload };
        }

        public static AppState RestoreStateReducer(AppState state, IAction<AppState> action)
        {
            return action.payload;
        }


        // Auth

        public static AppState LoginRequestReducer(AppState state, IAction<string> action)
        {
            return state with { isAuthenticating = true, authError = "" };
        }

        public static AppState LoginSuccessReducer(AppState state, IAction<AppActions.LoginPayload> action)
        {
            return state with
            {
                isAuthenticating = false,
                authError = "",
                userId = action.payload.userId,
                userEmail = action.payload.email,
                accessToken = action.payload.accessToken,
                tokenType = action.payload.tokenType,
                tokenExpiresAt = action.payload.expiresAt
            };
        }

        public static AppState LoginFailureReducer(AppState state, IAction<string> action)
        {
            return state with { isAuthenticating = false, authError = action.payload };
        }

        public static AppState RegisterRequestReducer(AppState state, IAction action)
        {
            return state with { isAuthenticating = true, authError = "" };
        }

        public static AppState RegisterSuccessReducer(AppState state, IAction<string> action)
        {
            return state with { isAuthenticating = false, userId = action.payload, authError = "" };
        }

        public static AppState RegisterFailureReducer(AppState state, IAction<string> action)
        {
            return state with { isAuthenticating = false, authError = action.payload };
        }

        
    }
}