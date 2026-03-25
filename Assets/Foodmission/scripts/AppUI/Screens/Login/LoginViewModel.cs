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
            Debug.LogError($"[{GetType().Name}] - Login -> username:"+Username+", password:"+Password);

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
                await _authService.LoginAsync(Username, Password);
            }
            else
            {
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
