using UnityEngine.Scripting;

namespace eu.foodmission.platform
{
    [Preserve]
    class PantryScreen : NavigationScreenBase<PantryViewModel>
    {
        public PantryScreen() { }

        protected override async void OnViewModelBound()
        {
            base.OnViewModelBound();
            await _viewModel.LoadAsync();
        }
    }
}
