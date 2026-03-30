using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class ProfileViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string m_UserName = "";

        public ProfileViewModel(IStoreService storeService) : base(storeService)
        {
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
            _store.Dispatch(AppActions.logout.Invoke());
            RaiseNavigationRequested("go_to_auth");
        }
    }
}
