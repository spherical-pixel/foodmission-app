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





        private readonly INotificationService _notificationService;
        private readonly ILegalService _legalService;

        public HomeScreenViewModel(
            IStoreService storeService,
            IAudioService audioService,
            INotificationService notificationService = null,
            ILegalService legalService = null) : base(storeService)
        {
            _notificationService = notificationService;
            _legalService = legalService ?? App.current?.services?.GetService<ILegalService>();

            // Get initial state
            AppState state = _storeService.GetAppState();

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

        public bool ShouldPromptForNotifications()
        {
            return _notificationService?.ShouldPromptForNotifications() ?? false;
        }

        public async System.Threading.Tasks.Task<bool> AcceptNotificationsAsync()
        {
            if (_notificationService != null)
            {
                return await _notificationService.AcceptNotificationsAsync();
            }
            return false;
        }

        public void DeclineNotifications()
        {
            _notificationService?.DeclineNotifications();
        }

        public async System.Threading.Tasks.Task<LegalConsentStatus> CheckPendingLegalConsentAsync()
        {
            if (_legalService == null) return null;
            var (status, error) = await _legalService.GetConsentStatusAsync();
            return status;
        }

        public async System.Threading.Tasks.Task<LegalDocument> GetLegalDocumentAsync(string docType)
        {
            if (_legalService == null) return null;
            var (doc, error) = await _legalService.GetLatestDocumentAsync(docType);
            return doc;
        }

        public async System.Threading.Tasks.Task<bool> AcceptLegalConsentAsync(string documentKey)
        {
            if (_legalService == null) return false;
            var (res, error) = await _legalService.AcceptConsentAsync(documentKey);
            return res != null && res.accepted;
        }

        public void NavigateToOnboardingProfile()
        {
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.register_to_onboarding);
        }
    }
}
