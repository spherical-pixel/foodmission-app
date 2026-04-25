using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class GroupsViewModel : ViewModelBase
    {
        private readonly IGroupService _groupService;

        [ObservableProperty]
        private List<UserGroup> m_Groups = new();

        [ObservableProperty]
        private bool m_IsLoading;

        [ObservableProperty]
        private string m_ErrorMessage = "";

        public GroupsViewModel(IStoreService storeService, IGroupService groupService)
            : base(storeService)
        {
            _groupService = groupService;
        }

        public async Task LoadGroupsAsync()
        {
            IsLoading = true;
            ErrorMessage = "";

            UserGroup[] groups = await _groupService.GetGroupsAsync();

            IsLoading = false;

            if (groups == null)
            {
                ErrorMessage = "Error loading groups";
                Groups = new List<UserGroup>();
                return;
            }

            Groups = new List<UserGroup>(groups);
        }

        public async Task LeaveGroupAsync(string groupId)
        {
            bool success = await _groupService.LeaveGroupAsync(groupId);

            if (success)
            {
                Groups = new List<UserGroup>(Groups.FindAll(g => g.id != groupId));
            }
        }

        public async Task DeleteGroupAsync(string groupId)
        {
            bool success = await _groupService.DeleteGroupAsync(groupId);

            if (success)
            {
                Groups = new List<UserGroup>(Groups.FindAll(g => g.id != groupId));
            }
        }
    }
}
