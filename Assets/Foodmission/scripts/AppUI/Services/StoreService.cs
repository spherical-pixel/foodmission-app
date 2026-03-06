using System;
using Unity.AppUI.Redux;
using UnityEngine;

#if UNITY_EDITOR
using Unity.AppUI.Redux.DevTools;
#endif



namespace eu.foodmission.platform
{
    /// <summary>
    /// Store service implementation with local persistence integration.
    /// Manages the global application state (AppState).
    /// </summary>
    public class StoreService : IStoreService, IDisposable
    {
        private readonly ILocalStorageService _localStorageService;
        private IDisposableSubscription _appStateSubscription;

        // Slice name
        public const string APP_SLICE = "app";

        // localStorage key
        private const string APP_STATE_KEY = "app_state";

        public IStore<PartitionedState> store { get; }

        public StoreService(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;

            // Restore persisted state before creating the store
            AppState persistedAppState = LoadPersistedAppState();

            // Create single AppState slice
            var appSlice = StoreFactory.CreateSlice<AppState>(
                APP_SLICE,
                persistedAppState,
                builder =>
                {
                    builder
                        // Preferences
                        .AddCase(AppActions.setTheme, AppReducers.SetThemeReducer)
                        .AddCase(AppActions.setLanguage, AppReducers.SetLanguageReducer)
                        .AddCase(AppActions.setScale, AppReducers.SetScaleReducer)
                        .AddCase(AppActions.completeOnboarding, AppReducers.CompleteOnboardingReducer)
                        .AddCase(AppActions.setUser, AppReducers.SetUserReducer)
                        .AddCase(AppActions.logout, AppReducers.LogoutReducer)
                        .AddCase(AppActions.updateSessionTimestamp, AppReducers.UpdateSessionTimestampReducer)
                        .AddCase(AppActions.restoreState, AppReducers.RestoreStateReducer)
                        // Auth
                        .AddCase(AppActions.loginRequest, AppReducers.LoginRequestReducer)
                        .AddCase(AppActions.loginSuccess, AppReducers.LoginSuccessReducer)
                        .AddCase(AppActions.loginFailure, AppReducers.LoginFailureReducer)
                        .AddCase(AppActions.registerRequest, AppReducers.RegisterRequestReducer)
                        .AddCase(AppActions.registerSuccess, AppReducers.RegisterSuccessReducer)
                        .AddCase(AppActions.registerFailure, AppReducers.RegisterFailureReducer);
                }
            );

#if UNITY_EDITOR
            // Create store with DevTools enhancer in editor
            var enhancerConfig = new DefaultEnhancerConfiguration
            {
                devTools = new DevToolsConfiguration
                {
                    enabled = true,
                    name = "Foodmission Store"
                }
            };
            var enhancer = StoreFactory.DefaultEnhancer<PartitionedState>(enhancerConfig);
            store = StoreFactory.CreateStore(new[] { appSlice }, enhancer);
#else
            store = StoreFactory.CreateStore(new[] { appSlice });
#endif

            // Subscribe to AppState changes for auto-save
            _appStateSubscription = store.Subscribe(
                state => state.Get<AppState>(APP_SLICE),
                OnAppStateChanged
            );
        }

        /// <summary>
        /// Loads persisted state from localStorage
        /// </summary>
        private AppState LoadPersistedAppState()
        {
            try
            {
                return _localStorageService.GetValue<AppState>(APP_STATE_KEY, new AppState());
            }
            catch (Exception)
            {
                // Return default state if loading fails
                return new AppState();
            }
        }

        /// <summary>
        /// Callback when AppState changes - auto-persists
        /// </summary>
        private void OnAppStateChanged(AppState state)
        {
            SaveAppState();
        }

        // ==================== IStoreService Implementation ====================

        public AppState GetAppState() => store.GetState<AppState>(APP_SLICE);

        public void SaveAppState()
        {
            var state = GetAppState();
            _localStorageService.SetValue(APP_STATE_KEY, state);
        }

        public void RestoreAppState()
        {
            var persistedState = LoadPersistedAppState();
            store.Dispatch(AppActions.restoreState, persistedState);
        }

        // ==================== IDisposable ====================

        public void Dispose()
        {
            _appStateSubscription?.Dispose();
            _appStateSubscription = null;
        }
    }
}
