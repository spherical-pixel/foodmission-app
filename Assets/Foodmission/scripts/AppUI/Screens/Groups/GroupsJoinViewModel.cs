using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;

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
                ErrorMessage = "Invite code is required";
                return;
            }

            IsJoining = true;
            ErrorMessage = "";

            UserGroup group = await _groupService.JoinGroupAsync(InviteCode);

            IsJoining = false;

            if (group == null)
            {
                ErrorMessage = "Invalid invite code or already a member";
                return;
            }

            JoinedGroup = group;
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.groups_to_detail);
        }
    }
}
