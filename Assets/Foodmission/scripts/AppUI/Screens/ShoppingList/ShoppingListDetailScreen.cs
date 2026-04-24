using Unity.AppUI.Navigation;

using UnityEngine.Scripting;

namespace eu.foodmission.platform
{
    [Preserve]
    class ShoppingListDetailScreen : NavigationScreenBase<ShoppingListDetailViewModel>
    {
        public ShoppingListDetailScreen() { }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            string listId = null;

            if (args != null)
            {
                foreach (Argument arg in args)
                {
                    if (arg.name == "listId")
                    {
                        listId = arg.value?.ToString();
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(listId))
            {
                await _viewModel.LoadAsync(listId);
            }
        }
    }
}
