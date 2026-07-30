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

        private List<UserGroup> _allGroups = new();

        [ObservableProperty]
        private List<UserGroup> m_Groups = new();

        [ObservableProperty]
        private string m_SearchText = "";

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
                _allGroups = new List<UserGroup>();
                ApplyFilter();
                return;
            }

            ErrorDetail = null;
            _allGroups = new List<UserGroup>(groups ?? System.Array.Empty<UserGroup>());
            ApplyFilter();
        }

        public void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Groups = new List<UserGroup>(_allGroups);
            }
            else
            {
                string query = SearchText.Trim();
                Groups = _allGroups.FindAll(g =>
                    (!string.IsNullOrEmpty(g.name) && g.name.Contains(query, System.StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(g.description) && g.description.Contains(query, System.StringComparison.OrdinalIgnoreCase)));
            }
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
                _allGroups.RemoveAll(g => g.id == groupId);
                ApplyFilter();
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
                _allGroups.RemoveAll(g => g.id == groupId);
                ApplyFilter();
            }
        }
    }
}
