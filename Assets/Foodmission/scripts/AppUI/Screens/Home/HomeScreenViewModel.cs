using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class HomeScreenViewModel : ViewModelBase
    {
        [ObservableProperty]
        private TimePeriod _selectedTimePeriod = TimePeriod.TODAY;

        [ObservableProperty]
        private UserScope _selectedUserScope = UserScope.ME;

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





        public HomeScreenViewModel(IStoreService storeService, IAudioService audioService) : base(storeService)
        {
            // Get initial state
            AppState state = _storeService.GetAppState();

            //audioService.PlayNutriSfx(NutriSfxType.Celebration);


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

        }



        public void SetTimePeriod(TimePeriod period)
        {
            SelectedTimePeriod = period;
            // TODO: Update progress and stats based on selected period
        }

        public void SetUserScope(UserScope scope)
        {
            SelectedUserScope = scope;
            // TODO: Update progress and stats based on selected scope
        }

        public void NavigateToOnboardingProfile()
        {
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.register_to_onboarding);
        }
    }
}
