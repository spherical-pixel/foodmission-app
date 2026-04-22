using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class SettingsViewModel : ViewModelBase
    {
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

        public SettingsViewModel(IStoreService storeService) : base(storeService)
        {
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

        public void SetTheme(string theme) => _store.Dispatch(AppActions.setTheme.Invoke(theme));
        public void SetLanguage(string lang) => _store.Dispatch(AppActions.setLanguage.Invoke(lang));
        public void SetScale(string scale) => _store.Dispatch(AppActions.setScale.Invoke(scale));
        public void SetFont(string font) => _store.Dispatch(AppActions.setFont.Invoke(font));

        public void SetSound(int volume)
        {
            _store.Dispatch(AppActions.setSound.Invoke(volume));
            ApplyMixerVolume(volume);
        }

        private static void ApplyMixerVolume(int volume)
        {
            // TODO: mixer call here
            // e.g.: _audioMixer.SetFloat("SoundVolume", Mathf.Log10(Mathf.Max(volume, 1) / 100f) * 20f);
        }

        public void SetMusic(int volume)
        {
            _store.Dispatch(AppActions.setMusic.Invoke(volume));
            ApplyMixerMusicVolume(volume);
        }

        private static void ApplyMixerMusicVolume(int volume)
        {
            // TODO: mixer call here
            // e.g.: _audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(volume, 1) / 100f) * 20f);
        }

        public void SetPushNotifications(bool enabled)
        {
            _store.Dispatch(AppActions.setPushNotifications.Invoke(enabled));
            ApplyPushNotifications(enabled);
        }

        private static void ApplyPushNotifications(bool enabled)
        {
            // TODO: platform-specific calls here
            // iOS:    UnityEngine.iOS.NotificationServices.RegisterForNotifications(...)
            // Android: Firebase.Messaging.FirebaseMessaging.SubscribeAsync(...) / UnsubscribeAsync(...)
        }

        public void SetBackgroundPattern(bool pattern) =>
            _store.Dispatch(AppActions.setBackgroundPattern.Invoke(pattern));
    }
}
