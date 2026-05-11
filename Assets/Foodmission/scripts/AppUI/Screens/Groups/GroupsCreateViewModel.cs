using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class GroupsCreateViewModel : ViewModelBase
    {
        private readonly IGroupService _groupService;

        [ObservableProperty]
        private string m_Name = "";

        [ObservableProperty]
        private string m_Description = "";

        [ObservableProperty]
        private bool m_IsCreating;

        [ObservableProperty]
        private string m_ErrorMessage = "";

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

        public GroupsCreateViewModel(IStoreService storeService, IGroupService groupService)
            : base(storeService)
        {
            _groupService = groupService;
        }

        public async Task CreateGroupAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GROUP_NAME_REQUIRED");
                return;
            }

            IsCreating = true;
            ErrorMessage = "";

            var (created, error) = await _groupService.CreateGroupAsync(Name, Description);

            IsCreating = false;

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_CREATING_GROUP");
                return;
            }

            ErrorDetail = null;
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_groups);
        }
    }
}
