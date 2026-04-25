using Unity.AppUI.Navigation;

using UnityEngine.Scripting;

namespace eu.foodmission.platform
{
    [Preserve]
    class PantryItemDetailScreen : NavigationScreenBase<PantryItemDetailViewModel>
    {
        public PantryItemDetailScreen() { }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            string itemId = null;

            if (args != null)
            {
                foreach (Argument arg in args)
                {
                    if (arg.name == "itemId")
                    {
                        itemId = arg.value?.ToString();
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(itemId))
            {
                await _viewModel.LoadAsync(itemId);
            }
        }
    }
}
