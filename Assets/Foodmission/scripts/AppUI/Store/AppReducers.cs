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
        public static readonly ActionCreator<int> setSound = "app/setSound";
        public static readonly ActionCreator<int> setMusic = "app/setMusic";
        public static readonly ActionCreator<bool> setPushNotifications = "app/setPushNotifications";
        public static readonly ActionCreator<bool> setBackgroundPattern = "app/setBackgroundPattern";
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
            public readonly int refreshTokenExpiresAt;

            public LoginPayload(string userId, string userName, string email,
                string accessToken, string tokenType, int expiresAt, string refreshToken,
                int refreshTokenExpiresAt = 0)
            {
                this.userId = userId;
                this.userName = userName;
                this.email = email;
                this.accessToken = accessToken;
                this.tokenType = tokenType;
                this.expiresAt = expiresAt;
                this.refreshToken = refreshToken;
                this.refreshTokenExpiresAt = refreshTokenExpiresAt;
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
            public readonly int refreshTokenExpiresAt;

            public TokenRefreshPayload(string accessToken, string tokenType, int expiresAt, string refreshToken,
                int refreshTokenExpiresAt = 0)
            {
                this.accessToken = accessToken;
                this.tokenType = tokenType;
                this.expiresAt = expiresAt;
                this.refreshToken = refreshToken;
                this.refreshTokenExpiresAt = refreshTokenExpiresAt;
            }
        }

        public static readonly ActionCreator registerRequest = "app/registerRequest";
        public static readonly ActionCreator<string> registerSuccess = "app/registerSuccess";
        public static readonly ActionCreator<string> registerFailure = "app/registerFailure";

        // Extended profile
        public static readonly ActionCreator setExtendedProfile = "app/setExtendedProfile";

        // Profile sync
        public static readonly ActionCreator<ProfilePayload> profileSynced = "app/profileSynced";

        public readonly struct ProfilePayload
        {
            public readonly string firstName;
            public readonly string lastName;
            public readonly int yearOfBirth;
            public readonly string country;
            public readonly string region;
            public readonly string zip;
            public readonly string gender;
            public readonly string annualIncome;
            public readonly string educationLevel;
            public readonly string activityLevel;
            public readonly float weightKg;
            public readonly float heightCm;
            public readonly string language;
            public readonly UserSettingsDto settings;

            public ProfilePayload(string firstName, string lastName, int yearOfBirth,
                string country, string region, string zip, string gender,
                string annualIncome, string educationLevel, string activityLevel,
                float weightKg, float heightCm, string language = null,
                UserSettingsDto settings = null)
            {
                this.firstName = firstName;
                this.lastName = lastName;
                this.yearOfBirth = yearOfBirth;
                this.country = country;
                this.region = region;
                this.zip = zip;
                this.gender = gender;
                this.annualIncome = annualIncome;
                this.educationLevel = educationLevel;
                this.activityLevel = activityLevel;
                this.weightKg = weightKg;
                this.heightCm = heightCm;
                this.language = language;
                this.settings = settings;
            }
        }
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

        public static AppState SetSoundReducer(AppState state, IAction<int> action)
        {
            var newState = state.Copy();
            newState.soundVolume = action.payload;
            return newState;
        }

        public static AppState SetMusicReducer(AppState state, IAction<int> action)
        {
            var newState = state.Copy();
            newState.musicVolume = action.payload;
            return newState;
        }

        public static AppState SetPushNotificationsReducer(AppState state, IAction<bool> action)
        {
            var newState = state.Copy();
            newState.pushNotificationsEnabled = action.payload;
            return newState;
        }

        public static AppState SetBackgroundPatternReducer(AppState state, IAction<bool> action)
        {
            var newState = state.Copy();
            newState.backgroundPattern = action.payload;
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
            // Clear session data
            newState.lang = "none";
            newState.userId = "";
            newState.userName = "";
            newState.userEmail = "";
            newState.accessToken = "";
            newState.tokenType = "";
            newState.tokenExpiresAt = 0;
            newState.refreshToken = "";
            // Clear profile data
            newState.userFirstName = "";
            newState.userLastName = "";
            newState.userYearOfBirth = 0;
            newState.userCountry = "";
            newState.userRegion = "";
            newState.userZip = "";
            newState.userGender = "";
            newState.userAnnualIncome = "";
            newState.userEducationLevel = "";
            newState.userActivityLevel = "";
            newState.userWeightKg = 0f;
            newState.userHeightCm = 0f;
            // Reset preferences to defaults
            newState.theme = "system";
            newState.scale = "medium";
            newState.font = "roboto";
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
            newState.refreshTokenExpiresAt = action.payload.refreshTokenExpiresAt;
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
            if (action.payload.refreshTokenExpiresAt > 0)
            {
                newState.refreshTokenExpiresAt = action.payload.refreshTokenExpiresAt;
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

        public static AppState SetExtendedProfileReducer(AppState state, IAction action)
        {
            var newState = state.Copy();
            newState.hasCompletedExtendedProfile = true;
            return newState;
        }

        public static AppState ProfileSyncedReducer(AppState state, IAction<AppActions.ProfilePayload> action)
        {
            var newState = state.Copy();
            newState.userFirstName = action.payload.firstName ?? "";
            newState.userLastName = action.payload.lastName ?? "";
            newState.userYearOfBirth = action.payload.yearOfBirth;
            newState.userCountry = action.payload.country ?? "";
            newState.userRegion = action.payload.region ?? "";
            newState.userZip = action.payload.zip ?? "";
            newState.userGender = action.payload.gender ?? "";
            newState.userAnnualIncome = action.payload.annualIncome ?? "";
            newState.userEducationLevel = action.payload.educationLevel ?? "";
            newState.userActivityLevel = action.payload.activityLevel ?? "";
            newState.userWeightKg = action.payload.weightKg;
            newState.userHeightCm = action.payload.heightCm;

            if (!string.IsNullOrEmpty(action.payload.language))
            {
                newState.lang = action.payload.language;
            }

            // Only apply server settings if the server has stored them (theme is our sentinel)
            var s = action.payload.settings;
            if (s != null && !string.IsNullOrEmpty(s.theme))
            {
                newState.theme = s.theme;
                newState.scale = s.scale ?? newState.scale;
                newState.font = s.font ?? newState.font;
                newState.soundVolume = s.soundVolume > 0 ? s.soundVolume : newState.soundVolume;
                newState.musicVolume = s.musicVolume > 0 ? s.musicVolume : newState.musicVolume;
                newState.pushNotificationsEnabled = s.pushNotificationsEnabled;
                newState.backgroundPattern = s.backgroundPattern;
            }

            return newState;
        }
    }
}
