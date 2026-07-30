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

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

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

            var (group, error) = await _groupService.GetGroupAsync(_groupId);

            IsLoading = false;

            if (error != null)
            {
                ErrorDetail = error;
                return;
            }

            ErrorDetail = null;
            Group = group;
            Members = group.members != null
                ? new List<GroupMember>(group.members)
                : new List<GroupMember>();

            string currentUserId = _storeService.GetAppState().userId;
            IsAdmin = Members.Any(m => m.userId == currentUserId && m.role == "ADMIN");
        }

        public async Task LoadInviteCodeAsync()
        {
            var (code, error) = await _groupService.GetInviteCodeAsync(_groupId);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                InviteCode = code;
            }
        }

        public async Task RegenerateInviteCodeAsync()
        {
            var (code, error) = await _groupService.RegenerateInviteCodeAsync(_groupId);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                InviteCode = code;
            }
        }

        public async Task AddVirtualMemberAsync(string name, int yearOfBirth = 0)
        {
            var (member, error) = await _groupService.AddVirtualMemberAsync(_groupId, name, yearOfBirth);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                Members = new List<GroupMember>(Members) { member };
            }
        }

        public async Task UpdateVirtualMemberAsync(string memberId, string name, int yearOfBirth = 0)
        {
            var (success, error) = await _groupService.UpdateVirtualMemberAsync(_groupId, memberId, name, yearOfBirth);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                await LoadAsync(_groupId);
            }
        }

        public async Task RemoveMemberAsync(string memberId)
        {
            var (success, error) = await _groupService.RemoveMemberAsync(_groupId, memberId);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                Members = new List<GroupMember>(Members.FindAll(m => m.id != memberId));
            }
        }

        public async Task MakeAdminAsync(string memberId)
        {
            var (success, error) = await _groupService.MakeAdminAsync(_groupId, memberId);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                await LoadAsync(_groupId);
            }
        }

        public async Task UpdateGroupAsync(string name, string description = null)
        {
            var (success, error) = await _groupService.UpdateGroupAsync(_groupId, name, description);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                await LoadAsync(_groupId);
            }
        }

        public async Task LeaveGroupAsync()
        {
            var (success, error) = await _groupService.LeaveGroupAsync(_groupId);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_groups);
            }
        }

        public async Task DeleteGroupAsync()
        {
            var (success, error) = await _groupService.DeleteGroupAsync(_groupId);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_groups);
            }
        }
    }
}
