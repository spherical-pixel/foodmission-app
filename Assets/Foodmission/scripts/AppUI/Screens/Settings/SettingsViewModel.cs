using System.Threading;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private CancellationTokenSource _syncCts;
        private const int SyncDelayMs = 1500;

        [ObservableProperty]
        private string m_Theme = "system";

        [ObservableProperty]
        private string m_Lang = "es";

        [ObservableProperty]
        private string m_Scale = "medium";

        [ObservableProperty]
        private string m_Font = "roboto";

        [ObservableProperty]
        private int m_Sound = 100;

        [ObservableProperty]
        private int m_Music = 100;

        [ObservableProperty]
        private bool m_PushNotifications = false;

        [ObservableProperty]
        private bool m_BackgroundPattern = true;

        [ObservableProperty]
        private string m_UserName = "User";

        public SettingsViewModel(IStoreService storeService, IAuthService authService) : base(storeService)
        {
            _authService = authService;
            SynchronizeState(_storeService.GetAppState());
            _storeSubscription = _store.Subscribe(SelectSettingsState, OnSettingsStateChanged);
        }

        private (string theme, string lang, string scale, string font, int soundVolume, int musicVolume, bool pushNotifications, bool backgroundPattern, string userName) SelectSettingsState(AppState state)
            => (state.theme, state.lang, state.scale, state.font, state.soundVolume, state.musicVolume, state.pushNotificationsEnabled, state.backgroundPattern, state.userName);

        private void OnSettingsStateChanged((string theme, string lang, string scale, string font, int soundVolume, int musicVolume, bool pushNotifications, bool backgroundPattern, string userName) s)
        {
            Theme = s.theme;
            Lang = s.lang;
            Scale = s.scale;
            Font = s.font;
            Sound = s.soundVolume;
            Music = s.musicVolume;
            PushNotifications = s.pushNotifications;
            BackgroundPattern = s.backgroundPattern;
            UserName = s.userName ?? "User";
        }

        private void SynchronizeState(AppState state)
        {
            Theme = state.theme;
            Lang = state.lang;
            Scale = state.scale;
            Font = state.font;
            Sound = state.soundVolume;
            Music = state.musicVolume;
            PushNotifications = state.pushNotificationsEnabled;
            BackgroundPattern = state.backgroundPattern;
            UserName = state.userName ?? "User";
        }

        public void SetTheme(string theme)
        {
            _store.Dispatch(AppActions.setTheme.Invoke(theme));
            ScheduleSettingsSync();
        }

        public void SetLanguage(string lang)
        {
            _store.Dispatch(AppActions.setLanguage.Invoke(lang));
            ScheduleSettingsSync();
        }

        public void SetScale(string scale)
        {
            _store.Dispatch(AppActions.setScale.Invoke(scale));
            ScheduleSettingsSync();
        }

        public void SetFont(string font)
        {
            _store.Dispatch(AppActions.setFont.Invoke(font));
            ScheduleSettingsSync();
        }

        public void SetSound(int volume)
        {
            _store.Dispatch(AppActions.setSound.Invoke(volume));
            ApplyMixerVolume(volume);
            ScheduleSettingsSync();
        }

        private static void ApplyMixerVolume(int volume)
        {
            // TODO: mixer call here
        }

        public void SetMusic(int volume)
        {
            _store.Dispatch(AppActions.setMusic.Invoke(volume));
            ApplyMixerMusicVolume(volume);
            ScheduleSettingsSync();
        }

        private static void ApplyMixerMusicVolume(int volume)
        {
            // TODO: mixer call here
        }

        public void SetPushNotifications(bool enabled)
        {
            _store.Dispatch(AppActions.setPushNotifications.Invoke(enabled));
            ApplyPushNotifications(enabled);
            ScheduleSettingsSync();
        }

        private static void ApplyPushNotifications(bool enabled)
        {
            // TODO: platform-specific calls here
        }

        public void SetBackgroundPattern(bool pattern)
        {
            _store.Dispatch(AppActions.setBackgroundPattern.Invoke(pattern));
            ScheduleSettingsSync();
        }

        public void Logout()
        {
            _store.Dispatch(AppActions.logout.Invoke());
            RaiseNavigationRequested("go_to_auth");
        }

        private void ScheduleSettingsSync()
        {
            _syncCts?.Cancel();
            _syncCts?.Dispose();
            _syncCts = new CancellationTokenSource();
            _ = SyncAfterDelay(_syncCts.Token);
        }

        private async Task SyncAfterDelay(CancellationToken token)
        {
            try
            {
                await Task.Delay(SyncDelayMs, token);
                await _authService.SyncSettingsAsync();
            }
            catch (System.OperationCanceledException)
            {
                
            }
        }

        protected override void OnDispose()
        {
            _syncCts?.Cancel();
            _syncCts?.Dispose();
            _syncCts = null;
            base.OnDispose();
        }
    }
}
