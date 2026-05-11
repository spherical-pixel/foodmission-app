using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class ShoppingListDetailViewModel : ViewModelBase
    {
        private readonly IShoppingListService _shoppingListService;
        private readonly IFoodService _foodService;

        private string _currentListId;

        [ObservableProperty]
        private List<ShoppingListItemView> _items = new();

        [ObservableProperty]
        private List<OpenFoodFactsProduct> _searchResults = new();

        [ObservableProperty]
        private string _searchQuery = "";

        [ObservableProperty]
        private bool _isLoadingItems;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private string _listName = "";

        [ObservableProperty]
        private string _errorMessage = "";

        public ShoppingListDetailViewModel(
            IStoreService storeService,
            IShoppingListService shoppingListService,
            IFoodService foodService)
            : base(storeService)
        {
            _shoppingListService = shoppingListService;
            _foodService = foodService;
        }

        public async Task LoadAsync(string listId, string listName = null)
        {
            if (string.IsNullOrEmpty(listId))
            {
                ErrorMessage = "Invalid shopping list";
                return;
            }

            _currentListId = listId;
            if (!string.IsNullOrWhiteSpace(listName))
            {
                ListName = listName.Trim();
            }

            IsLoadingItems = true;
            ErrorMessage = "";

            ShoppingListItem[] rawItems = await _shoppingListService.GetItemsAsync(_currentListId);

            IsLoadingItems = false;

            if (rawItems == null)
            {
                ErrorMessage = "Could not load items";
                Items = new List<ShoppingListItemView>();
                return;
            }

            // Resolve food names in parallel — FoodService caches by id after first call
            Task<ShoppingListItemView>[] tasks = new Task<ShoppingListItemView>[rawItems.Length];

            for (int i = 0; i < rawItems.Length; i++)
            {
                tasks[i] = EnrichItemAsync(rawItems[i]);
            }

            ShoppingListItemView[] enriched = await Task.WhenAll(tasks);
            Items = new List<ShoppingListItemView>(enriched);
        }

        private async Task<ShoppingListItemView> EnrichItemAsync(ShoppingListItem item)
        {
            string foodName = item.food?.name;

            if (string.IsNullOrEmpty(foodName))
            {
                FoodItem fetched = await _foodService.GetFoodByIdAsync(item.foodId);
                foodName = fetched?.name ?? "Unknown";
            }

            return new ShoppingListItemView
            {
                Item = item,
                FoodName = foodName,
                FoodImageUrl = item.food?.imageUrl,
                FoodBrands = null
            };
        }

        public async Task SearchFoodsAsync(string query)
        {
            SearchQuery = query;
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(query))
            {
                SearchResults = new List<OpenFoodFactsProduct>();
                return;
            }

            IsSearching = true;
            OpenFoodFactsSearchResponse response = await _foodService.SearchOpenFoodFactsAsync(query);
            IsSearching = false;

            SearchResults = response?.products != null
                ? new List<OpenFoodFactsProduct>(response.products)
                : new List<OpenFoodFactsProduct>();

            if (response == null)
            {
                ErrorMessage = "Could not search products";
            }
        }

        public async Task<bool> ImportAndAddItemAsync(OpenFoodFactsProduct product, float quantity, string unit)
        {
            if (string.IsNullOrEmpty(_currentListId) || product == null)
            {
                ErrorMessage = "Could not add the item";
                return false;
            }

            ErrorMessage = "";
            FoodItem foodItem = await _foodService.ImportFromBarcodeAsync(product.barcode);

            if (foodItem == null)
            {
                ErrorMessage = "Could not import the selected product";
                return false;
            }

            ShoppingListItem added = await _shoppingListService.AddItemAsync(_currentListId, foodItem.id, quantity, unit);

            if (added != null)
            {
                SearchResults = new List<OpenFoodFactsProduct>();
                SearchQuery = "";
                await LoadAsync(_currentListId);
                return true;
            }

            ErrorMessage = "Could not add the item to the list";
            return false;
        }

        public async Task ToggleItemAsync(string itemId)
        {
            ErrorMessage = "";
            ShoppingListItem updated = await _shoppingListService.ToggleItemCheckedAsync(_currentListId, itemId);

            if (updated != null)
            {
                int idx = Items.FindIndex(v => v.Item.id == itemId);

                if (idx >= 0)
                {
                    Items[idx].Item.@checked = updated.@checked;
                    Items = new List<ShoppingListItemView>(Items);
                }

                return;
            }

            ErrorMessage = "Could not update the item";
        }

        public async Task DeleteItemAsync(string itemId)
        {
            ErrorMessage = "";
            bool success = await _shoppingListService.DeleteItemAsync(_currentListId, itemId);

            if (success)
            {
                Items = new List<ShoppingListItemView>(Items.FindAll(v => v.Item.id != itemId));
                return;
            }

            ErrorMessage = "Could not delete the item";
        }

        public async Task ClearCheckedItemsAsync()
        {
            ErrorMessage = "";
            bool success = await _shoppingListService.ClearCheckedItemsAsync(_currentListId);

            if (success)
            {
                Items = new List<ShoppingListItemView>(Items.FindAll(v => !v.Item.@checked));
                return;
            }

            ErrorMessage = "Could not clear completed items";
        }
    }
}
