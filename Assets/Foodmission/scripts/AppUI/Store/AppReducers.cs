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
        public static readonly ActionCreator<string> setFont = "app/setFont";
        public static readonly ActionCreator completeOnboarding = "app/completeOnboarding";
        public static readonly ActionCreator<string> setUser = "app/setUser";
        public static readonly ActionCreator logout = "app/logout";
        public static readonly ActionCreator<int> updateSessionTimestamp = "app/updateSessionTimestamp";
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
            public readonly string userName;
            public readonly string email;
            public readonly string accessToken;
            public readonly string tokenType;
            public readonly int expiresAt;
            public readonly string refreshToken;

            public LoginPayload(string userId, string userName, string email,
                string accessToken, string tokenType, int expiresAt, string refreshToken)
            {
                this.userId = userId;
                this.userName = userName;
                this.email = email;
                this.accessToken = accessToken;
                this.tokenType = tokenType;
                this.expiresAt = expiresAt;
                this.refreshToken = refreshToken;
            }
        }

        public static readonly ActionCreator<TokenRefreshPayload> tokenRefreshed = "app/tokenRefreshed";

        /// <summary>
        /// Payload for token refresh — accessToken is always updated; refreshToken is optional (rotation)
        /// </summary>
        public readonly struct TokenRefreshPayload
        {
            public readonly string accessToken;
            public readonly string tokenType;
            public readonly int expiresAt;
            public readonly string refreshToken;

            public TokenRefreshPayload(string accessToken, string tokenType, int expiresAt, string refreshToken)
            {
                this.accessToken = accessToken;
                this.tokenType = tokenType;
                this.expiresAt = expiresAt;
                this.refreshToken = refreshToken;
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
            var newState = state.Copy();
            newState.theme = action.payload;
            return newState;
        }

        public static AppState SetLanguageReducer(AppState state, IAction<string> action)
        {
            var newState = state.Copy();
            newState.lang = action.payload;
            return newState;
        }

        public static AppState SetScaleReducer(AppState state, IAction<string> action)
        {
            var newState = state.Copy();
            newState.scale = action.payload;
            return newState;
        }

        public static AppState SetFontReducer(AppState state, IAction<string> action)
        {
            var newState = state.Copy();
            newState.font = action.payload;
            return newState;
        }

        public static AppState CompleteOnboardingReducer(AppState state, IAction action)
        {
            var newState = state.Copy();
            newState.hasCompletedOnboarding = true;
            return newState;
        }

        public static AppState SetUserReducer(AppState state, IAction<string> action)
        {
            var newState = state.Copy();
            newState.userId = action.payload;
            return newState;
        }

        public static AppState LogoutReducer(AppState state, IAction action)
        {
            var newState = state.Copy();
            newState.userId = "";
            newState.userName = "";
            newState.userEmail = "";
            newState.accessToken = "";
            newState.tokenType = "";
            newState.tokenExpiresAt = 0;
            newState.refreshToken = "";
            return newState;
        }

        public static AppState UpdateSessionTimestampReducer(AppState state, IAction<int> action)
        {
            var newState = state.Copy();
            newState.lastSessionTimestamp = action.payload;
            return newState;
        }

        public static AppState RestoreStateReducer(AppState state, IAction<AppState> action)
        {
            return action.payload;
        }


        // Auth

        public static AppState LoginRequestReducer(AppState state, IAction<string> action)
        {
            var newState = state.Copy();
            newState.isAuthenticating = true;
            newState.authError = "";
            newState.userId = "";
            return newState;
        }

        public static AppState LoginSuccessReducer(AppState state, IAction<AppActions.LoginPayload> action)
        {
            var newState = state.Copy();
            newState.isAuthenticating = false;
            newState.authError = "";
            newState.userId = action.payload.userId;
            newState.userName = action.payload.userName;
            newState.userEmail = action.payload.email;
            newState.accessToken = action.payload.accessToken;
            newState.tokenType = action.payload.tokenType;
            newState.tokenExpiresAt = action.payload.expiresAt;
            newState.refreshToken = action.payload.refreshToken;
            return newState;
        }

        public static AppState LoginFailureReducer(AppState state, IAction<string> action)
        {
            var newState = state.Copy();
            newState.isAuthenticating = false;
            newState.authError = action.payload;
            return newState;
        }

        public static AppState TokenRefreshedReducer(AppState state, IAction<AppActions.TokenRefreshPayload> action)
        {
            var newState = state.Copy();
            newState.accessToken = action.payload.accessToken;
            newState.tokenType = action.payload.tokenType;
            newState.tokenExpiresAt = action.payload.expiresAt;
            if (!string.IsNullOrEmpty(action.payload.refreshToken))
            {
                newState.refreshToken = action.payload.refreshToken;
            }
            return newState;
        }

        public static AppState RegisterRequestReducer(AppState state, IAction action)
        {
            var newState = state.Copy();
            newState.isAuthenticating = true;
            newState.authError = "";
            return newState;
        }

        public static AppState RegisterSuccessReducer(AppState state, IAction<string> action)
        {
            var newState = state.Copy();
            newState.isAuthenticating = false;
            newState.userId = action.payload;
            newState.authError = "";
            return newState;
        }

        public static AppState RegisterFailureReducer(AppState state, IAction<string> action)
        {
            var newState = state.Copy();
            newState.isAuthenticating = false;
            newState.authError = action.payload;
            return newState;
        }


    }
}
