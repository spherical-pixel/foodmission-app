using System.Threading.Tasks;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;

using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class OnboardingGroupsViewModel : ViewModelBase
    {
        private readonly IGroupService _groupService;

        [ObservableProperty]
        private string m_CreateName = "";

        [ObservableProperty]
        private string m_JoinCode = "";

        [ObservableProperty]
        private bool m_IsLoading;

        [ObservableProperty]
        private string m_ErrorMessage = "";

        public event System.Action<string> ShowErrorRequest;
        public event System.Action<string> ShowSuccessRequest;

        public OnboardingGroupsViewModel(IStoreService storeService, IGroupService groupService)
            : base(storeService)
        {
            _groupService = groupService;
        }

        public async Task CreateGroupAsync()
        {
            if (string.IsNullOrWhiteSpace(CreateName))
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GROUP_NAME_REQUIRED");
                return;
            }

            IsLoading = true;
            ErrorMessage = "";

            var (created, _) = await _groupService.CreateGroupAsync(CreateName);

            IsLoading = false;

            if (created == null)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_CREATING_GROUP");
                ShowErrorRequest?.Invoke(ErrorMessage);
                return;
            }

            ShowSuccessRequest?.Invoke(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GROUP_CREATED"));
            RaiseNavigationRequested(Actions.go_to_home);
        }

        public async Task JoinGroupAsync()
        {
            if (string.IsNullOrWhiteSpace(JoinCode))
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "INVITE_CODE_REQUIRED");
                return;
            }

            IsLoading = true;
            ErrorMessage = "";

            var (group, _) = await _groupService.JoinGroupAsync(JoinCode);

            IsLoading = false;

            if (group == null)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "INVALID_INVITE_CODE");
                ShowErrorRequest?.Invoke(ErrorMessage);
                return;
            }

            ShowSuccessRequest?.Invoke(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GROUP_JOINED"));
            RaiseNavigationRequested(Actions.go_to_home);
        }

        public void Skip()
        {
            RaiseNavigationRequested(Actions.go_to_home);
        }
    }
}
