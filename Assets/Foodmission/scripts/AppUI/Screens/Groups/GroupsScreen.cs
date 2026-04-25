using UnityEngine.Scripting;

namespace eu.foodmission.platform
{
    [Preserve]
    class GroupsScreen : NavigationScreenBase<GroupsViewModel>
    {
        public GroupsScreen() { }

        protected override async void OnViewModelBound()
        {
            base.OnViewModelBound();
            await _viewModel.LoadGroupsAsync();
        }
    }
}
