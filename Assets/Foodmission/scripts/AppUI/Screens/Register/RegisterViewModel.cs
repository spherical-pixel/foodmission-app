using System.Collections.Generic;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class RegisterViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _username = "";

        [ObservableProperty]
        private string _email = "";

        [ObservableProperty]
        private string _password = "";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = "";

        [ObservableProperty]
        private bool _isAuthenticated;

        
        public List<string> AgeOptions { get; } = new()
        {
            "Under 18",
            "18-24",
            "25-34",
            "35-44",
            "45-54",
            "55+",
            "Prefer not to say"
        };

        /// <summary>
        /// Selected Age Range
        /// </summary>
        [ObservableProperty]
        private string _selectedAge = "";
        public RegisterViewModel(IAuthService authService, IStoreService storeService) : base(storeService)
        {
            _authService = authService;

            
            AppState state = _storeService.GetAppState();
            SynchronizeState(state);

            
            _storeSubscription = _store.Subscribe(
                SelectAuthState,
                OnAuthStateChanged
            );
        }

        private (bool isAuthenticating, string authError, string userId) SelectAuthState(PartitionedState state)
        {
            AppState appState = state.Get<AppState>(StoreService.APP_SLICE);
            return (appState.isAuthenticating, appState.authError, appState.userId);
        }

        private void OnAuthStateChanged((bool isAuthenticating, string authError, string userId) authState)
        {
            IsLoading = authState.isAuthenticating;
            ErrorMessage = authState.authError;

            bool wasAuthenticated = IsAuthenticated;
            IsAuthenticated = !string.IsNullOrEmpty(authState.userId);

            // Navigate to home when regiter succesful
            if (IsAuthenticated && !wasAuthenticated)
            {
                RaiseNavigationRequested(Actions.loading_to_home);
            }
        }

        private void SynchronizeState(AppState state)
        {
            IsLoading = state.isAuthenticating;
            ErrorMessage = state.authError;
            IsAuthenticated = !string.IsNullOrEmpty(state.userId);
        }

        public async void Register()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                // TODO: Check localization tables
                ErrorMessage = "Por favor, rellena todos los campos";
                return;
            }

            if (Username.Length < 3)
            {
                // TODO: Check localization tables
                ErrorMessage = "El nombre de usuario debe tener al menos 3 caracteres";
                return;
            }

            // Basic email verification
            if (!Email.Contains("@") || !Email.Contains("."))
            {
                // TODO: Check localization tables
                ErrorMessage = "Por favor, introduce un email válido";
                return;
            }

            if (Password.Length < 6)
            {
                // TODO: Check localization tables
                ErrorMessage = "La contraseña debe tener al menos 6 caracteres";
                return;
            }

            await _authService.RegisterAsync(Username, Email, Password);
        }

        /// <summary>
        /// Back to login screen
        /// </summary>
        public void NavigateToLogin()
        {
            RaiseNavigationRequested(Actions.login_to_register);
        }
    }
}
