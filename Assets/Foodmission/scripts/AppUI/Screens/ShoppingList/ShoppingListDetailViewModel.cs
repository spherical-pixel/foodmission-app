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
        private readonly IFoodProductService _foodProductService;
        private readonly IGenericFoodService _genericFoodService;
        private readonly ILocalStorageService _localStorage;
        private readonly IOpenFoodFactsClientService _openFoodFactsClientService;

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

        [ObservableProperty]
        private List<GenericFood> _genericFoods = new();

        [ObservableProperty]
        private bool _isLoadingGenericFoods;

        public ShoppingListDetailViewModel(
            IStoreService storeService,
            IShoppingListService shoppingListService,
            IFoodProductService foodProductService,
            IGenericFoodService genericFoodService,
            ILocalStorageService localStorage,
            IOpenFoodFactsClientService openFoodFactsClientService)
            : base(storeService)
        {
            _shoppingListService = shoppingListService;
            _foodProductService = foodProductService;
            _genericFoodService = genericFoodService;
            _localStorage = localStorage;
            _openFoodFactsClientService = openFoodFactsClientService;
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

        public async Task<List<GenericFood>> GetGenericFoodsAsync()
        {
            IsLoadingGenericFoods = true;
            var (result, error) = await _genericFoodService.SearchGenericFoodsAsync(pageSize: 100);

            if (error != null)
            {
                ErrorDetail = error;
                IsLoadingGenericFoods = false;
                return new List<GenericFood>();
            }

            GenericFoods = result?.items != null ? new List<GenericFood>(result.items) : new List<GenericFood>();
            IsLoadingGenericFoods = false;
            return GenericFoods;
        }

        public async Task<List<GenericFood>> SearchGenericFoodsAsync(string query)
        {
            var (result, error) = await _genericFoodService.SearchGenericFoodsAsync(query, pageSize: 100);
            if (error != null)
            {
                ErrorDetail = error;
                return new List<GenericFood>();
            }
            return result?.items != null ? new List<GenericFood>(result.items) : new List<GenericFood>();
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
            string foodName = item.foodProduct?.name ?? item.genericFood?.foodName;

            if (string.IsNullOrEmpty(foodName))
            {
                if (!string.IsNullOrEmpty(item.foodProductId))
                {
                    var (fetched, _) = await _foodProductService.GetFoodByIdAsync(item.foodProductId);
                    foodName = fetched?.name;
                }

                if (string.IsNullOrEmpty(foodName))
                {
                    foodName = item.genericFood?.foodName;
                }
            }

            if (string.IsNullOrEmpty(foodName))
            {
                foodName = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
            }

            return new ShoppingListItemView
            {
                Item = item,
                FoodName = foodName,
                FoodImageUrl = item.foodProduct?.imageUrl,
                FoodBrands = null
            };
        }

        public async Task<List<OpenFoodFactsProduct>> SearchFoodsAsync(string query)
        {
            SearchQuery = query;
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(query))
            {
                SearchResults = new List<OpenFoodFactsProduct>();
                return SearchResults;
            }

            IsSearching = true;
            var (products, error) = await FoodProductFlow.SearchProductsAsync(_foodProductService, _openFoodFactsClientService, query);
            IsSearching = false;

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_SEARCH_PRODUCTS");
                SearchResults = new List<OpenFoodFactsProduct>();
            }
            else
            {
                SearchResults = products ?? new List<OpenFoodFactsProduct>();
            }

            return SearchResults;
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
            var (foodItem, importError) = await ImportByBarcodeAsync(product.barcode);
            if (importError != null)
            {
                ErrorDetail = importError;
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
            var newItem = new ShoppingListItemView
            {
                Item = added,
                FoodName = product.name ?? added.foodProduct?.name ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN"),
            };
            _allItems.Add(newItem);
            FilterText = "";
            ApplyFilter();
            SaveCache();
            return true;
        }

        public async Task<bool> AddGenericFoodItemAsync(GenericFood food, float quantity, string unit)
        {
            if (string.IsNullOrEmpty(_currentListId) || food == null)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_LIST_ITEM");
                return false;
            }

            if (!Guid.TryParse(food.id, out _))
            {
                ErrorDetail = new ApiErrorResponse
                {
                    statusCode = 400,
                    error = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GENERIC_FOOD_NOT_AVAILABLE"),
                    message = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GENERIC_FOOD_NOT_AVAILABLE_DESC")
                };
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_TO_LIST");
                return false;
            }

            ErrorMessage = "";
            var (added, addError) = await _shoppingListService.AddItemAsync(_currentListId, genericFoodId: food.id, quantity: quantity, unit: unit);

            if (addError != null)
            {
                ErrorDetail = addError;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_TO_LIST");
                return false;
            }

            ErrorDetail = null;
            SearchResults = new List<OpenFoodFactsProduct>();
            SearchQuery = "";
            var newItem = new ShoppingListItemView
            {
                Item = added,
                FoodName = added.genericFood?.foodName ?? food.foodName ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN"),
            };
            _allItems.Add(newItem);
            FilterText = "";
            ApplyFilter();
            SaveCache();
            return true;
        }

        public async Task<(FoodProduct Result, ApiErrorResponse Error)> ImportByBarcodeAsync(string barcode)
        {
            return await FoodProductFlow.ImportByBarcodeAsync(_foodProductService, _openFoodFactsClientService, barcode);
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
