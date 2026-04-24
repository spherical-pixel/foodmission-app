using UnityEngine.Scripting;

namespace eu.foodmission.platform
{
    [Preserve]
    class ShoppingListScreen : NavigationScreenBase<ShoppingListViewModel>
    {
        public ShoppingListScreen() { }

        protected override async void OnViewModelBound()
        {
            base.OnViewModelBound();
            await _viewModel.LoadListsAsync();
        }
    }
}
