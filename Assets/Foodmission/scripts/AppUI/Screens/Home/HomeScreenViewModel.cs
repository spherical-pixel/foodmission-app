using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class HomeScreenViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _welcomeMessage = "Bienvenido";

        [ObservableProperty]
        private string _userName = "";

        public HomeScreenViewModel(IStoreService storeService) : base(storeService)
        {
            // Get initial state
            AppState state = _storeService.GetAppState();
            UpdateWelcomeMessage(state);

            // Subscribe to user state changes
            _storeSubscription = _store.Subscribe(
                SelectUserState,
                OnUserStateChanged
            );
        }

        private (string userId, string lang) SelectUserState(PartitionedState state)
        {
            AppState appState = state.Get<AppState>(StoreService.APP_SLICE);
            return (appState.userId, appState.lang);
        }

        private void OnUserStateChanged((string userId, string lang) userState)
        {
            AppState state = _storeService.GetAppState();
            UpdateWelcomeMessage(state);
        }

        private void UpdateWelcomeMessage(AppState state)
        {
            UserName = state.userId ?? "";
            // TODO: this is just a test 
            WelcomeMessage = state.lang switch
            {
                "es" => $"Bienvenido, {state.userId}",
                "en" => $"Welcome, {state.userId}",
                "ca" => $"Benvingut, {state.userId}",
                _ => $"Welcome, {state.userId}"
            };
        }
    }
}
