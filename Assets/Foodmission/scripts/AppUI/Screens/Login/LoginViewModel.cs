using System;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;
using UnityEngine;


namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Username for auth
        /// </summary>
        [ObservableProperty]
        private string _username = "";

        /// <summary>
        /// Holds momentary password entered by the user for auth
        /// </summary>
        [ObservableProperty]
        private string _password = "";

        /// <summary>
        /// True if waiting for login results, false otherwise. 
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        
        /// <summary>
        /// It indicates whether the user is authenticated. 
        /// It is automatically updated when the state of Redux changes. 
        /// The screen can observe this property to navigate.
        /// </summary>
        [ObservableProperty]
        private bool _isAuthenticated;

        public event System.Action<string> ShowErrorRequest;

        public LoginViewModel(IAuthService authService, IStoreService storeService) : base(storeService)
        {
            _authService = authService;

            // Get's initial state of Redux and synchronizes it with the ViewModel
            AppState state = _storeService.GetAppState();
            SynchronizeState(state);

            // Subscribe to changes in auth status
            _storeSubscription = _store.Subscribe(
                SelectAuthState,
                OnAuthStateChanged
            );
        }

        
        /// <summary>
        /// Selector for extracting only the relevant auth state
        /// </summary>
        private (bool isAuthenticating, string authError, string userId) SelectAuthState(PartitionedState state)
        {
            AppState appState = state.Get<AppState>(StoreService.APP_SLICE);
            return (appState.isAuthenticating, appState.authError, appState.userId);
        }

        /// <summary>
        /// Callback for auth state changed
        /// </summary>
        private void OnAuthStateChanged((bool isAuthenticating, string authError, string userId) authState)
        {
            IsLoading = authState.isAuthenticating;
            

            bool wasAuthenticated = IsAuthenticated;
            IsAuthenticated = !string.IsNullOrEmpty(authState.userId);

            // If has just authenticated (transition from not authenticated to authenticated), navigate to home)
            if (IsAuthenticated && !wasAuthenticated)
            {
                RaiseNavigationRequested(Actions.loading_to_home);
            }else if(!string.IsNullOrEmpty(authState.authError))
            {
                ShowErrorRequest(authState.authError);
            }
        }

        /// <summary>
        /// Synchronizes the local state with the Redux state
        /// </summary>
        private void SynchronizeState(AppState state)
        {
            IsLoading = state.isAuthenticating;
            //ErrorMessage = state.authError;
            IsAuthenticated = !string.IsNullOrEmpty(state.userId);
        }

        /// <summary>
        /// Called when user clicks Login button
        /// </summary>
        public async void Login()
        {
            Debug.LogError($"[{GetType().Name}] - Login -> username:"+Username+", password:"+Password);
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ShowErrorRequest?.Invoke("Please fill the user and password fields");
                return;
            }

            await _authService.LoginAsync(Username, Password);
        }

        
    }
}
