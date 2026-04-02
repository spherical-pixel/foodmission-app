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

        [ObservableProperty]
        private float _healthProgress = 0.65f;

        [ObservableProperty]
        private float _sustainabilityProgress = 0.42f;

        [ObservableProperty]
        private float _knowledgeProgress = 0.78f;

        [ObservableProperty]
        private int _caloriesConsumed = 1850;

        [ObservableProperty]
        private int _caloriesLeft = 350;

        [ObservableProperty]
        private string _selectedTimePeriod = "Today";

        [ObservableProperty]
        private string _selectedUserScope = "Me";

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

        private (string userId, string lang) SelectUserState(AppState state)
        {
            return (state.userId, state.lang);
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

        public void SetTimePeriod(string period)
        {
            SelectedTimePeriod = period;
            // TODO: Update progress and stats based on selected period
        }

        public void SetUserScope(string scope)
        {
            SelectedUserScope = scope;
            // TODO: Update progress and stats based on selected scope
        }
    }
}
