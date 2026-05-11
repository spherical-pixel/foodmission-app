using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class ShoppingListDetailViewModel : ViewModelBase
    {
        private readonly IShoppingListService _shoppingListService;
        private readonly IFoodService _foodService;
        private readonly ILocalStorageService _localStorage;

        private const string CacheKeyPrefix = "shoppinglist_items_";
        private const string FoodSearchCachePrefix = "food_search_";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

        private string _currentListId;
        private List<ShoppingListItemView> _allItems = new();

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

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

        [ObservableProperty]
        private string _filterText = "";

        public ShoppingListDetailViewModel(
            IStoreService storeService,
            IShoppingListService shoppingListService,
            IFoodService foodService,
            ILocalStorageService localStorage)
            : base(storeService)
        {
            _shoppingListService = shoppingListService;
            _foodService = foodService;
            _localStorage = localStorage;
        }

        public void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                Items = new List<ShoppingListItemView>(_allItems);
            }
            else
            {
                string filter = FilterText.ToLowerInvariant();
                Items = _allItems.Where(v => (v.FoodName ?? "").ToLowerInvariant().Contains(filter)).ToList();
            }
        }

        public async Task LoadAsync(string listId, string listName = null)
        {
            if (string.IsNullOrEmpty(listId))
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "INVALID_SHOPPING_LIST");
                return;
            }

            _currentListId = listId;
            if (!string.IsNullOrWhiteSpace(listName))
            {
                ListName = listName.Trim();
            }

            IsLoadingItems = true;
            ErrorMessage = "";

            var (rawItems, error) = await _shoppingListService.GetItemsAsync(_currentListId);

            if (error != null)
            {
                ErrorDetail = error;
                ShoppingListItem[] cached = _localStorage.GetValue<ShoppingListItemPagedResponse>(GetCacheKey())?.data;
                if (cached != null && cached.Length > 0)
                {
                    ShoppingListItemView[] enriched = await EnrichItemsAsync(cached);
                    _allItems = new List<ShoppingListItemView>(enriched);
                }
                else
                {
                    ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_LOAD_ITEMS");
                    _allItems = new List<ShoppingListItemView>();
                }

                IsLoadingItems = false;
                ApplyFilter();
                return;
            }

            ErrorDetail = null;
            _localStorage.SetValue(GetCacheKey(), new ShoppingListItemPagedResponse { data = rawItems });

            ShoppingListItemView[] enrichedItems = await EnrichItemsAsync(rawItems);
            _allItems = new List<ShoppingListItemView>(enrichedItems);
            IsLoadingItems = false;
            ApplyFilter();
        }

        private string GetCacheKey()
        {
            return CacheKeyPrefix + _currentListId;
        }

        private async Task<ShoppingListItemView[]> EnrichItemsAsync(ShoppingListItem[] items)
        {
            Task<ShoppingListItemView>[] tasks = new Task<ShoppingListItemView>[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                tasks[i] = EnrichItemAsync(items[i]);
            }

            return await Task.WhenAll(tasks);
        }

        private void SaveCache()
        {
            ShoppingListItem[] raw = new ShoppingListItem[_allItems.Count];
            for (int i = 0; i < _allItems.Count; i++)
            {
                raw[i] = _allItems[i].Item;
            }

            _localStorage.SetValue(GetCacheKey(), new ShoppingListItemPagedResponse { data = raw });
        }

        private async Task<ShoppingListItemView> EnrichItemAsync(ShoppingListItem item)
        {
            string foodName = item.food?.name;

            if (string.IsNullOrEmpty(foodName))
            {
                var (fetched, _) = await _foodService.GetFoodByIdAsync(item.foodId);
                foodName = fetched?.name ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
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

            string normalized = query.Trim().ToLowerInvariant();
            string cacheKey = FoodSearchCachePrefix + normalized;

            CachedFoodSearch cached = _localStorage.GetValue<CachedFoodSearch>(cacheKey);
            if (cached?.data?.products != null && IsCacheFresh(cached.cachedAtTicks))
            {
                SearchResults = new List<OpenFoodFactsProduct>(cached.data.products);
                return;
            }

            IsSearching = true;
            var (response, _) = await _foodService.SearchOpenFoodFactsAsync(query);
            IsSearching = false;

            if (response?.products != null)
            {
                _localStorage.SetValue(cacheKey, new CachedFoodSearch
                {
                    data = response,
                    cachedAtTicks = DateTime.UtcNow.Ticks
                });
                SearchResults = new List<OpenFoodFactsProduct>(response.products);
            }
            else if (cached?.data?.products != null)
            {
                SearchResults = new List<OpenFoodFactsProduct>(cached.data.products);
            }
            else
            {
                SearchResults = new List<OpenFoodFactsProduct>();
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_SEARCH_PRODUCTS");
            }
        }

        private static bool IsCacheFresh(long cachedAtTicks)
        {
            if (cachedAtTicks <= 0) return false;
            DateTime cachedAt = new DateTime(cachedAtTicks, DateTimeKind.Utc);
            return (DateTime.UtcNow - cachedAt) < CacheTtl;
        }

        public async Task<bool> ImportAndAddItemAsync(OpenFoodFactsProduct product, float quantity, string unit)
        {
            if (string.IsNullOrEmpty(_currentListId) || product == null)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_LIST_ITEM");
                return false;
            }

            ErrorMessage = "";
            var (foodItem, error) = await _foodService.ImportFromBarcodeAsync(product.barcode);

            if (error != null)
            {
                if (error.statusCode == 400)
                {
                    var (existingFood, findError) = await _foodService.FindByBarcodeAsync(product.barcode);
                    if (findError == null && existingFood != null)
                    {
                        foodItem = existingFood;
                        error = null;
                    }
                }
            }

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_IMPORT_PRODUCT");
                return false;
            }

            var (added, addError) = await _shoppingListService.AddItemAsync(_currentListId, foodItem.id, quantity, unit);

            if (addError != null)
            {
                ErrorDetail = addError;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_TO_LIST");
                return false;
            }

            ErrorDetail = null;
            SearchResults = new List<OpenFoodFactsProduct>();
            SearchQuery = "";
            await LoadAsync(_currentListId);
            return true;
        }

        public async Task ToggleItemAsync(string itemId)
        {
            ErrorMessage = "";

            ShoppingListItemView view = _allItems.Find(v => v.Item.id == itemId);
            if (view == null) return;

            bool newChecked = !view.Item.@checked;
            var (updated, error) = await _shoppingListService.UpdateItemAsync(
                _currentListId, itemId, null, null, null, newChecked);

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_UPDATE_LIST_ITEM");
                return;
            }

            ErrorDetail = null;
            view.Item.@checked = updated.@checked;
            ApplyFilter();
            SaveCache();
        }

        public async Task RenameListAsync(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            var (success, error) = await _shoppingListService.UpdateListAsync(_currentListId, newName.Trim());
            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                ListName = newName.Trim();
            }
        }

        public async Task UpdateItemAsync(string itemId, float quantity, string unit)
        {
            ErrorMessage = "";
            var (updated, error) = await _shoppingListService.UpdateItemAsync(
                _currentListId, itemId, quantity, unit, null, null);

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_UPDATE_LIST_ITEM");
                return;
            }

            ErrorDetail = null;
            int idx = _allItems.FindIndex(v => v.Item.id == itemId);
            if (idx >= 0)
            {
                _allItems[idx].Item.quantity = updated.quantity;
                _allItems[idx].Item.unit = updated.unit;
                ApplyFilter();
                SaveCache();
            }
        }

        public async Task DeleteItemAsync(string itemId)
        {
            ErrorMessage = "";
            var (success, error) = await _shoppingListService.DeleteItemAsync(_currentListId, itemId);

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_DELETE_LIST_ITEM");
                return;
            }

            ErrorDetail = null;
            _allItems = new List<ShoppingListItemView>(_allItems.FindAll(v => v.Item.id != itemId));
            ApplyFilter();
            SaveCache();
        }

        public async Task ClearCheckedItemsAsync()
        {
            ErrorMessage = "";
            var (success, error) = await _shoppingListService.ClearCheckedItemsAsync(_currentListId);

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_CLEAR_ITEMS");
                return;
            }

            ErrorDetail = null;
            _allItems = new List<ShoppingListItemView>(_allItems.FindAll(v => !v.Item.@checked));
            ApplyFilter();
            SaveCache();
        }
    }
}
