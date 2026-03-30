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

        public SettingsViewModel(IStoreService storeService) : base(storeService)
        {
            SynchronizeState(_storeService.GetAppState());
            _storeSubscription = _store.Subscribe(SelectSettingsState, OnSettingsStateChanged);
        }

        private (string theme, string lang, string scale) SelectSettingsState(AppState state)
            => (state.theme, state.lang, state.scale);

        private void OnSettingsStateChanged((string theme, string lang, string scale) s)
        {
            Theme = s.theme;
            Lang = s.lang;
            Scale = s.scale;
        }

        private void SynchronizeState(AppState state)
        {
            Theme = state.theme;
            Lang = state.lang;
            Scale = state.scale;
        }

        public void SetTheme(string theme) => _store.Dispatch(AppActions.setTheme.Invoke(theme));
        public void SetLanguage(string lang) => _store.Dispatch(AppActions.setLanguage.Invoke(lang));
        public void SetScale(string scale) => _store.Dispatch(AppActions.setScale.Invoke(scale));
    }
}
