using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class ShoppingListViewModel : ViewModelBase
    {
        private readonly IShoppingListService _shoppingListService;

        [ObservableProperty]
        private List<ShoppingList> _lists = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = "";

        public ShoppingListViewModel(IStoreService storeService, IShoppingListService shoppingListService)
            : base(storeService)
        {
            _shoppingListService = shoppingListService;
        }

        public async Task LoadListsAsync()
        {
            IsLoading = true;
            ErrorMessage = "";

            ShoppingList[] lists = await _shoppingListService.GetListsAsync();

            IsLoading = false;

            if (lists == null)
            {
                ErrorMessage = "Error loading lists";
                Lists = new List<ShoppingList>();
                return;
            }

            Lists = new List<ShoppingList>(lists);
        }

        public async Task CreateListAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = "List name is required";
                return;
            }

            ErrorMessage = "";
            IsLoading = true;
            ShoppingList created = await _shoppingListService.CreateListAsync(name);
            IsLoading = false;

            if (created != null)
            {
                await LoadListsAsync();
                return;
            }

            ErrorMessage = "Could not create the list";
        }

        public async Task DeleteListAsync(string id)
        {
            ErrorMessage = "";
            IsLoading = true;
            bool success = await _shoppingListService.DeleteListAsync(id);
            IsLoading = false;

            if (success)
            {
                Lists = new List<ShoppingList>(Lists.FindAll(l => l.id != id));
                return;
            }

            ErrorMessage = "Could not delete the list";
        }
    }
}
