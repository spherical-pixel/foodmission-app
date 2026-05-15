using Unity.AppUI.MVVM;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class ForceUpdateScreenViewModel : ViewModelBase
    {
        [ObservableProperty] private string _currentVersion;
        [ObservableProperty] private string _latestVersion;
        [ObservableProperty] private string _releaseNotes;
        [ObservableProperty] private bool _isForced;

        private string _storeUrl;
        private string _returnAction;

        public ForceUpdateScreenViewModel(IStoreService storeService)
            : base(storeService) { }

        public void LoadData(AppVersionCheckResult updateData)
        {
            CurrentVersion = Application.version;
            LatestVersion = updateData.latestVersion;
            StoreUrl = updateData.storeUrl;
            ReleaseNotes = updateData.releaseNotes;
            IsForced = updateData.isForced;
        }

        public string StoreUrl
        {
            get => _storeUrl;
            set => _storeUrl = value;
        }

        public void SetReturnAction(string action) => _returnAction = action;

        public void OpenStore()
        {
            if (!string.IsNullOrEmpty(_storeUrl))
                Application.OpenURL(_storeUrl);
        }

        public void Skip()
        {
            if (!string.IsNullOrEmpty(_returnAction))
                RaiseNavigationRequested(_returnAction);
        }
    }
}
