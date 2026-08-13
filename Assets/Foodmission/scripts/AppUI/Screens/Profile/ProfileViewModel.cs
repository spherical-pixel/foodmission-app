using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class ProfileViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string m_UserName = "";

        private readonly IAuthService _authService;

        public ProfileViewModel(IStoreService storeService, IAuthService authService = null) : base(storeService)
        {
            _authService = authService;
            var state = _storeService.GetAppState();
            UserName = state.userName;

            _storeSubscription = _store.Subscribe(SelectUserName, OnUserNameChanged);
        }

        private string SelectUserName(AppState state) => state.userName;

        private void OnUserNameChanged(string userName)
        {
            UserName = userName;
        }

        public void Logout()
        {
            var auth = _authService ?? App.current?.services?.GetService<IAuthService>();
            auth?.Logout();
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_auth);
        }
    }
}
