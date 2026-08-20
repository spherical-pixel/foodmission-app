using System;
using System.Collections.Generic;
using eu.foodmission.platform;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform.Tests
{
    public class TestStoreService : IStoreService, IDisposable
    {
        public IStore<AppState> store { get; private set; }
        public List<string> DispatchedActionTypes { get; } = new List<string>();

        public TestStoreService()
        {
            store = BuildStore(new AppState());
        }

        public AppState GetAppState() => store.GetState();

        public void SetAppState(AppState state)
        {
            store?.Dispose();
            store = BuildStore(state);
        }

        public void SaveAppState() { }

        public void RestoreAppState() { }

        public void SetAppStateFromStorage() { }

        public void ClearSessionData()
        {
            DispatchedActionTypes.Clear();
            store?.Dispose();
            store = BuildStore(new AppState());
        }

        public void Dispose()
        {
            store?.Dispose();
            store = null;
        }

        private IStore<AppState> BuildStore(AppState initialState)
        {
            var reducerBuilder = new SliceReducerSwitchBuilder<AppState>("app")
                // Preferences
                .AddCase(AppActions.setTheme, AppReducers.SetThemeReducer)
                .AddCase(AppActions.setLanguage, AppReducers.SetLanguageReducer)
                .AddCase(AppActions.setScale, AppReducers.SetScaleReducer)
                .AddCase(AppActions.setFont, AppReducers.SetFontReducer)
                .AddCase(AppActions.setSound, AppReducers.SetSoundReducer)
                .AddCase(AppActions.setMusic, AppReducers.SetMusicReducer)
                .AddCase(AppActions.setPushNotifications, AppReducers.SetPushNotificationsReducer)
                .AddCase(AppActions.setDevicePushRegistration, AppReducers.SetDevicePushRegistrationReducer)
                .AddCase(AppActions.setBackgroundPattern, AppReducers.SetBackgroundPatternReducer)
                .AddCase(AppActions.completeOnboarding, AppReducers.CompleteOnboardingReducer)
                .AddCase(AppActions.setUser, AppReducers.SetUserReducer)
                .AddCase(AppActions.logout, AppReducers.LogoutReducer)
                .AddCase(AppActions.updateSessionTimestamp, AppReducers.UpdateSessionTimestampReducer)
                .AddCase(AppActions.restoreState, AppReducers.RestoreStateReducer)
                // Auth
                .AddCase(AppActions.loginRequest, AppReducers.LoginRequestReducer)
                .AddCase(AppActions.loginSuccess, AppReducers.LoginSuccessReducer)
                .AddCase(AppActions.loginFailure, AppReducers.LoginFailureReducer)
                .AddCase(AppActions.tokenRefreshed, AppReducers.TokenRefreshedReducer)
                .AddCase(AppActions.registerRequest, AppReducers.RegisterRequestReducer)
                .AddCase(AppActions.registerSuccess, AppReducers.RegisterSuccessReducer)
                .AddCase(AppActions.registerFailure, AppReducers.RegisterFailureReducer)
                // Extended profile
                .AddCase(AppActions.setExtendedProfile, AppReducers.SetExtendedProfileReducer)
                .AddCase(AppActions.setSkippedExtendedProfile, AppReducers.SetSkippedExtendedProfileReducer)
                .AddCase(AppActions.setOnboardingSurvey, AppReducers.SetOnboardingSurveyReducer)
                .AddCase(AppActions.setAvatar, AppReducers.SetAvatarReducer)
                // Profile sync
                .AddCase(AppActions.profileSynced, AppReducers.ProfileSyncedReducer)
                // Food Info
                .AddCase(AppActions.foodInfoAddRequested, AppReducers.FoodInfoAddRequestedReducer)
                .AddCase(AppActions.foodInfoAddRequestConsumed, AppReducers.FoodInfoAddRequestConsumedReducer);

            var realReducer = reducerBuilder.GetReducer();

            Reducer<AppState> trackingReducer = (state, action) =>
            {
                DispatchedActionTypes.Add(action.type);
                return realReducer(state, action);
            };

            return StoreFactory.CreateStore(trackingReducer, initialState);
        }
    }
}
