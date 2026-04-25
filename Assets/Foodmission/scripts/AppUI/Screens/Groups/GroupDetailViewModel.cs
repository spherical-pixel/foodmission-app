using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class GroupDetailViewModel : ViewModelBase
    {
        private readonly IGroupService _groupService;
        private string _groupId;

        [ObservableProperty]
        private UserGroup m_Group;

        [ObservableProperty]
        private List<GroupMember> m_Members = new();

        [ObservableProperty]
        private bool m_IsAdmin;

        [ObservableProperty]
        private string m_InviteCode = "";

        [ObservableProperty]
        private bool m_IsLoading;

        public GroupDetailViewModel(IStoreService storeService, IGroupService groupService)
            : base(storeService)
        {
            _groupService = groupService;
        }

        public async Task LoadAsync(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return;
            }

            _groupId = groupId;
            IsLoading = true;

            UserGroup group = await _groupService.GetGroupAsync(_groupId);

            IsLoading = false;

            if (group == null)
            {
                return;
            }

            Group = group;
            Members = group.members != null
                ? new List<GroupMember>(group.members)
                : new List<GroupMember>();

            string currentUserId = _storeService.GetAppState().userId;
            IsAdmin = Members.Any(m => m.userId == currentUserId && m.role == "ADMIN");
        }

        public async Task LoadInviteCodeAsync()
        {
            string code = await _groupService.GetInviteCodeAsync(_groupId);

            if (code != null)
            {
                InviteCode = code;
            }
        }

        public async Task RegenerateInviteCodeAsync()
        {
            string code = await _groupService.RegenerateInviteCodeAsync(_groupId);

            if (code != null)
            {
                InviteCode = code;
            }
        }

        public async Task AddVirtualMemberAsync(string name, int yearOfBirth = 0)
        {
            GroupMember member = await _groupService.AddVirtualMemberAsync(_groupId, name, yearOfBirth);

            if (member != null)
            {
                Members = new List<GroupMember>(Members) { member };
            }
        }

        public async Task RemoveMemberAsync(string memberId)
        {
            bool success = await _groupService.RemoveMemberAsync(_groupId, memberId);

            if (success)
            {
                Members = new List<GroupMember>(Members.FindAll(m => m.id != memberId));
            }
        }

        public async Task MakeAdminAsync(string memberId)
        {
            bool success = await _groupService.MakeAdminAsync(_groupId, memberId);

            if (success)
            {
                await LoadAsync(_groupId);
            }
        }

        public async Task UpdateGroupAsync(string name, string description = null)
        {
            bool success = await _groupService.UpdateGroupAsync(_groupId, name, description);

            if (success)
            {
                await LoadAsync(_groupId);
            }
        }

        public async Task LeaveGroupAsync()
        {
            bool success = await _groupService.LeaveGroupAsync(_groupId);

            if (success)
            {
                RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_groups);
            }
        }

        public async Task DeleteGroupAsync()
        {
            bool success = await _groupService.DeleteGroupAsync(_groupId);

            if (success)
            {
                RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_groups);
            }
        }
    }
}
