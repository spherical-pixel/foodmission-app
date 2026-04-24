using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class ShoppingListViewModel : ViewModelBase
    {
        private readonly IShoppingListService _shoppingListService;

        [ObservableProperty]
        private List<ShoppingList> m_Lists = new();

        [ObservableProperty]
        private bool m_IsLoading;

        [ObservableProperty]
        private string m_ErrorMessage = "";

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
                return;
            }

            ShoppingList created = await _shoppingListService.CreateListAsync(name);

            if (created != null)
            {
                await LoadListsAsync();
            }
        }

        public async Task DeleteListAsync(string id)
        {
            bool success = await _shoppingListService.DeleteListAsync(id);

            if (success)
            {
                Lists = new List<ShoppingList>(Lists.FindAll(l => l.id != id));
            }
        }
    }
}
