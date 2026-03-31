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

        public SettingsViewModel(IStoreService storeService) : base(storeService)
        {
            SynchronizeState(_storeService.GetAppState());
            _storeSubscription = _store.Subscribe(SelectSettingsState, OnSettingsStateChanged);
        }

        private (string theme, string lang, string scale, string font) SelectSettingsState(AppState state)
            => (state.theme, state.lang, state.scale, state.font);

        private void OnSettingsStateChanged((string theme, string lang, string scale, string font) s)
        {
            Theme = s.theme;
            Lang = s.lang;
            Scale = s.scale;
            Font = s.font;
        }

        private void SynchronizeState(AppState state)
        {
            Theme = state.theme;
            Lang = state.lang;
            Scale = state.scale;
            Font = state.font;
        }

        public void SetTheme(string theme) => _store.Dispatch(AppActions.setTheme.Invoke(theme));
        public void SetLanguage(string lang) => _store.Dispatch(AppActions.setLanguage.Invoke(lang));
        public void SetScale(string scale) => _store.Dispatch(AppActions.setScale.Invoke(scale));
        public void SetFont(string font) => _store.Dispatch(AppActions.setFont.Invoke(font));
    }
}
