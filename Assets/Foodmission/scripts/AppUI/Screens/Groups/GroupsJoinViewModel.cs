using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class GroupsJoinViewModel : ViewModelBase
    {
        private readonly IGroupService _groupService;

        [ObservableProperty]
        private string m_InviteCode = "";

        [ObservableProperty]
        private bool m_IsJoining;

        [ObservableProperty]
        private string m_ErrorMessage = "";

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

        // Exposed so the Screen can read the joined group id for navigation with args
        public UserGroup JoinedGroup { get; private set; }

        public GroupsJoinViewModel(IStoreService storeService, IGroupService groupService)
            : base(storeService)
        {
            _groupService = groupService;
        }

        public async Task JoinGroupAsync()
        {
            if (string.IsNullOrWhiteSpace(InviteCode))
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "INVITE_CODE_REQUIRED");
                return;
            }

            IsJoining = true;
            ErrorMessage = "";

            var (group, error) = await _groupService.JoinGroupAsync(InviteCode);

            IsJoining = false;

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "INVALID_INVITE_CODE");
                return;
            }

            ErrorDetail = null;
            JoinedGroup = group;
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.groups_to_detail);
        }
    }
}
