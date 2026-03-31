using System;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;


namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private bool _hasNavigated; // Prevents double navigation

        /// <summary>
        /// Username for auth
        /// </summary>
        [ObservableProperty]
        private string _username = "";

        [ObservableProperty]
        private string _usernameHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _usernameHelpTextVariant = HelpTextVariant.Default;

        /// <summary>
        /// Holds momentary password entered by the user for auth
        /// </summary>
        [ObservableProperty]
        private string _password = "";

        [ObservableProperty]
        private string _passwordHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _passwordHelpTextVariant = HelpTextVariant.Default;

        /// <summary>
        /// True if waiting for login results, false otherwise. 
        /// </summary>
        [ObservableProperty]
        private DisplayStyle _isLoading;

        
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
        private (bool isAuthenticating, string authError, string userId) SelectAuthState(AppState state)
        {
            return (state.isAuthenticating, state.authError, state.userId);
        }

        /// <summary>
        /// Callback for auth state changed
        /// </summary>
        private void OnAuthStateChanged((bool isAuthenticating, string authError, string userId) authState)
        {
            Debug.Log($"[{GetType().Name}] OnAuthStateChanged: isAuthenticating={authState.isAuthenticating}, userId={authState.userId}, authError={authState.authError}");

            if(authState.isAuthenticating)
            {
                IsLoading = DisplayStyle.Flex;
            }
            else
            {
                IsLoading = DisplayStyle.None;
            }

            bool wasAuthenticated = IsAuthenticated;
            IsAuthenticated = !string.IsNullOrEmpty(authState.userId);

            Debug.Log($"[{GetType().Name}] Auth state transition: wasAuthenticated={wasAuthenticated}, IsAuthenticated={IsAuthenticated}, _hasNavigated={_hasNavigated}");

            // If has just authenticated (transition from not authenticated to authenticated), navigate to home)
            if (IsAuthenticated && !wasAuthenticated)
            {
                if (!_hasNavigated)
                {
                    _hasNavigated = true;
                    Debug.Log($"[{GetType().Name}] Authentication successful - navigating to home");
                    RaiseNavigationRequested(Actions.go_to_home);
                }
                else
                {
                    Debug.Log($"[{GetType().Name}] Navigation already triggered - ignoring duplicate");
                }
            }
            else if(!string.IsNullOrEmpty(authState.authError))
            {
                Debug.Log($"[{GetType().Name}] Authentication failed: {authState.authError}");
                ShowErrorRequest?.Invoke(authState.authError);
            }
        }

        /// <summary>
        /// Synchronizes the local state with the Redux state
        /// </summary>
        private void SynchronizeState(AppState state)
        {
            if(state.isAuthenticating)
            {
                IsLoading = DisplayStyle.Flex;
            }
            else
            {
                IsLoading = DisplayStyle.None;
            }
            IsAuthenticated = !string.IsNullOrEmpty(state.userId);
        }

        /// <summary>
        /// Called when user clicks Login button
        /// </summary>
        public async void Login()
        {
            Debug.Log($"[{GetType().Name}] Login started for user: {Username}");
            _hasNavigated = false; // Reset navigation flag on new login attempt

            bool fieldsOk = true;

            if (!ValidateUsername())
            {
                fieldsOk = false;
            }

            if (!ValidatePassword())
            {
                fieldsOk = false;
            }

            if(fieldsOk)
            {
                try
                {
                    await _authService.LoginAsync(Username, Password);
                    // Navigation happens via OnAuthStateChanged callback
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{GetType().Name}] Login exception: {ex.Message}");
                    ShowErrorRequest?.Invoke("Login failed. Please try again.");
                    IsLoading = DisplayStyle.None;
                }
            }
            else
            {
                Debug.Log($"[{GetType().Name}] Field validation failed");
                ShowErrorRequest?.Invoke("@UI:ERROR_FIELDS_VALIDATION");
            }
        }


        public bool ValidateUsername()
        {
            if (string.IsNullOrEmpty(Username))
            {
                UsernameHelpTextValue = "@UI:ERROR_NO_EMPTY";
                UsernameHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            UsernameHelpTextValue = string.Empty;
            UsernameHelpTextVariant = HelpTextVariant.Default;
            return true;
        }


        public bool ValidatePassword()
        {
            if (string.IsNullOrEmpty(Password))
            {
                PasswordHelpTextValue = "@UI:ERROR_NO_EMPTY";
                PasswordHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            if (Password.Length < 6)
            {
                PasswordHelpTextValue = "@UI:ERROR_PASS_SHORT";
                PasswordHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            PasswordHelpTextValue = string.Empty;
            PasswordHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        
    }
}
