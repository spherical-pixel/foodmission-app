using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

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

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

        public GroupsViewModel(IStoreService storeService, IGroupService groupService)
            : base(storeService)
        {
            _groupService = groupService;
        }

        public async Task LoadGroupsAsync()
        {
            IsLoading = true;
            ErrorMessage = "";

            var (groups, error) = await _groupService.GetGroupsAsync();

            IsLoading = false;

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_LOADING_GROUPS");
                Groups = new List<UserGroup>();
                return;
            }

            ErrorDetail = null;
            Groups = new List<UserGroup>(groups);
        }

        public async Task LeaveGroupAsync(string groupId)
        {
            var (success, error) = await _groupService.LeaveGroupAsync(groupId);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                Groups = new List<UserGroup>(Groups.FindAll(g => g.id != groupId));
            }
        }

        public async Task DeleteGroupAsync(string groupId)
        {
            var (success, error) = await _groupService.DeleteGroupAsync(groupId);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                Groups = new List<UserGroup>(Groups.FindAll(g => g.id != groupId));
            }
        }
    }
}
