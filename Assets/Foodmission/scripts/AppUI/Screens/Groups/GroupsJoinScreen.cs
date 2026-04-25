using Unity.AppUI.Navigation;

using UnityEngine.Scripting;

namespace eu.foodmission.platform
{
    [Preserve]
    class GroupsJoinScreen : NavigationScreenBase<GroupsJoinViewModel>
    {
        public GroupsJoinScreen() { }

        protected override void OnNavigationRequested(string navigationAction)
        {
            if (navigationAction == Unity.AppUI.Navigation.Generated.Actions.groups_to_detail
                && _viewModel.JoinedGroup != null)
            {
                _navController?.Navigate(
                    navigationAction,
                    new[] { new Argument("groupId", _viewModel.JoinedGroup.id) });
            }
            else
            {
                base.OnNavigationRequested(navigationAction);
            }
        }
    }
}
