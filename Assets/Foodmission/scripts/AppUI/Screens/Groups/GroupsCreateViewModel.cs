using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;

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

        public GroupsCreateViewModel(IStoreService storeService, IGroupService groupService)
            : base(storeService)
        {
            _groupService = groupService;
        }

        public async Task CreateGroupAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Name is required";
                return;
            }

            IsCreating = true;
            ErrorMessage = "";

            UserGroup created = await _groupService.CreateGroupAsync(Name, Description);

            IsCreating = false;

            if (created == null)
            {
                ErrorMessage = "Error creating group";
                return;
            }

            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_groups);
        }
    }
}
