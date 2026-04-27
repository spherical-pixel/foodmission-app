using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class ShoppingListDetailViewModel : ViewModelBase
    {
        private readonly IShoppingListService _shoppingListService;
        private readonly IFoodService _foodService;

        private string _currentListId;

        [ObservableProperty]
        private List<ShoppingListItemView> m_Items = new();

        [ObservableProperty]
        private List<OpenFoodFactsProduct> m_SearchResults = new();

        [ObservableProperty]
        private string m_SearchQuery = "";

        [ObservableProperty]
        private bool m_IsLoadingItems;

        [ObservableProperty]
        private bool m_IsSearching;

        [ObservableProperty]
        private string m_ListName = "";

        public ShoppingListDetailViewModel(
            IStoreService storeService,
            IShoppingListService shoppingListService,
            IFoodService foodService)
            : base(storeService)
        {
            _shoppingListService = shoppingListService;
            _foodService = foodService;
        }

        public async Task LoadAsync(string listId)
        {
            if (string.IsNullOrEmpty(listId))
            {
                return;
            }

            _currentListId = listId;
            IsLoadingItems = true;

            ShoppingListItem[] rawItems = await _shoppingListService.GetItemsAsync(_currentListId);

            IsLoadingItems = false;

            if (rawItems == null)
            {
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
        }

        public async Task ImportAndAddItemAsync(OpenFoodFactsProduct product, float quantity, string unit)
        {
            if (string.IsNullOrEmpty(_currentListId))
            {
                return;
            }

            FoodItem foodItem = await _foodService.ImportFromBarcodeAsync(product.barcode);

            if (foodItem == null)
            {
                Debug.LogError($"[{GetType().Name}] Failed to import food: {product.name}");
                return;
            }

            ShoppingListItem added = await _shoppingListService.AddItemAsync(_currentListId, foodItem.id, quantity, unit);

            if (added != null)
            {
                SearchResults = new List<OpenFoodFactsProduct>();
                SearchQuery = "";
                await LoadAsync(_currentListId);
            }
        }

        public async Task ToggleItemAsync(string itemId)
        {
            ShoppingListItem updated = await _shoppingListService.ToggleItemCheckedAsync(_currentListId, itemId);

            if (updated != null)
            {
                int idx = Items.FindIndex(v => v.Item.id == itemId);

                if (idx >= 0)
                {
                    Items[idx].Item.@checked = updated.@checked;
                    Items = new List<ShoppingListItemView>(Items);
                }
            }
        }

        public async Task DeleteItemAsync(string itemId)
        {
            bool success = await _shoppingListService.DeleteItemAsync(_currentListId, itemId);

            if (success)
            {
                Items = new List<ShoppingListItemView>(Items.FindAll(v => v.Item.id != itemId));
            }
        }

        public async Task ClearCheckedItemsAsync()
        {
            bool success = await _shoppingListService.ClearCheckedItemsAsync(_currentListId);

            if (success)
            {
                Items = new List<ShoppingListItemView>(Items.FindAll(v => !v.Item.@checked));
            }
        }
    }
}
