using System.Threading;
using System.Threading.Tasks;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly ICatalogService _catalogService;
        private readonly IAudioService _audioService;
        private readonly INotificationService _notificationService;
        private CancellationTokenSource _syncCts;
        private const int SyncDelayMs = 600;

        [ObservableProperty]
        private string m_Theme = "system";

        [ObservableProperty]
        private string m_Lang = "en";

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
        private string m_NotificationPreferredTime = "10:00";

        [ObservableProperty]
        private bool m_BackgroundPattern = true;

        [ObservableProperty]
        private string m_UserName = "User";


        public SettingsViewModel(IStoreService storeService, IAuthService authService, ICatalogService catalogService, IAudioService audioService = null, INotificationService notificationService = null) : base(storeService)
        {
            _authService = authService;
            _catalogService = catalogService;
            _audioService = audioService;
            _notificationService = notificationService;
            SynchronizeState(_storeService.GetAppState());
            _storeSubscription = _store.Subscribe(SelectSettingsState, OnSettingsStateChanged);
        }

        private (string theme, string lang, string scale, string font, int soundVolume, int musicVolume, bool pushNotifications, string notificationPreferredTime, bool backgroundPattern, string userName) SelectSettingsState(AppState state)
            => (state.theme, state.lang, state.scale, state.font, state.soundVolume, state.musicVolume, state.pushNotificationsEnabled, state.notificationPreferredTime, state.backgroundPattern, state.userName);

        private void OnSettingsStateChanged((string theme, string lang, string scale, string font, int soundVolume, int musicVolume, bool pushNotifications, string notificationPreferredTime, bool backgroundPattern, string userName) s)
        {
            Theme = s.theme;
            Lang = s.lang;
            Scale = s.scale;
            Font = s.font;
            Sound = s.soundVolume;
            Music = s.musicVolume;
            PushNotifications = s.pushNotifications;
            NotificationPreferredTime = !string.IsNullOrEmpty(s.notificationPreferredTime) ? s.notificationPreferredTime : "10:00";
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
            NotificationPreferredTime = !string.IsNullOrEmpty(state.notificationPreferredTime) ? state.notificationPreferredTime : "10:00";
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
            FMQuantityUnitPanel.InitializeAsync(_catalogService, lang)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        Debug.LogError($"[SettingsViewModel] Failed to refresh units: {t.Exception?.InnerException?.Message}");
                }, TaskContinuationOptions.OnlyOnFaulted);
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
            _audioService?.SetSoundVolume(volume);
            ScheduleSettingsSync();
        }

        public void SetMusic(int volume)
        {
            _store.Dispatch(AppActions.setMusic.Invoke(volume));
            _audioService?.SetMusicVolume(volume);
            ScheduleSettingsSync();
        }

        public async void SetPushNotifications(bool enabled)
        {
            if (enabled && _notificationService != null)
            {
                bool granted = await _notificationService.RequestPermissionsAsync();
                if (!granted)
                {
                    PushNotifications = false;
                    _store.Dispatch(AppActions.setPushNotifications.Invoke(false));
                    return;
                }
            }

            _notificationService?.SetNotificationsEnabled(enabled);
            _store.Dispatch(AppActions.setPushNotifications.Invoke(enabled));
            if (enabled)
            {
                string preferredTime = !string.IsNullOrEmpty(NotificationPreferredTime) ? NotificationPreferredTime : "10:00";
                if (System.TimeSpan.TryParse(preferredTime, out var ts))
                {
                    _notificationService?.RescheduleAllNotifications(ts);
                }
            }
            ScheduleSettingsSync();
        }

        public void SetNotificationPreferredTime(string time)
        {
            string formattedTime = !string.IsNullOrEmpty(time) ? time : "10:00";
            NotificationPreferredTime = formattedTime;
            _store.Dispatch(AppActions.setNotificationPreferredTime.Invoke(formattedTime));
            if (PushNotifications && System.TimeSpan.TryParse(formattedTime, out var ts))
            {
                _notificationService?.RescheduleAllNotifications(ts);
            }
            ScheduleSettingsSync();
        }

        public string GetPushToken()
        {
            return _notificationService?.GetDevicePushRegistration()?.token ?? "";
        }

        public void SetBackgroundPattern(bool pattern)
        {
            _store.Dispatch(AppActions.setBackgroundPattern.Invoke(pattern));
            ScheduleSettingsSync();
        }

        public void Logout()
        {
            _authService?.Logout();
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_auth);
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
