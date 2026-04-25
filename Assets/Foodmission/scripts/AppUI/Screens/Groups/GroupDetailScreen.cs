using Unity.AppUI.Navigation;

using UnityEngine.Scripting;

namespace eu.foodmission.platform
{
    [Preserve]
    class GroupDetailScreen : NavigationScreenBase<GroupDetailViewModel>
    {
        public GroupDetailScreen() { }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            string groupId = null;

            if (args != null)
            {
                foreach (Argument arg in args)
                {
                    if (arg.name == "groupId")
                    {
                        groupId = arg.value?.ToString();
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(groupId))
            {
                await _viewModel.LoadAsync(groupId);
            }
        }
    }
}
