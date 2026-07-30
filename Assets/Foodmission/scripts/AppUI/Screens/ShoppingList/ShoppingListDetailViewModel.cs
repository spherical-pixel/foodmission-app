using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;
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
        private readonly IAuthService _authService;
        private readonly IPantryService _pantryService;

        private const string CacheKeyPrefix = "shoppinglist_items_";
        private const string FoodSearchCachePrefix = "food_search_";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

        private string _currentListId;
        private List<ShoppingListItemView> _allItems = new();
        private readonly HashSet<string> _autoAddedItemIds = new();

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

        [ObservableProperty]
        private bool _autoAddToPantry;

        public ShoppingListDetailViewModel(
            IStoreService storeService,
            IShoppingListService shoppingListService,
            IFoodProductService foodProductService,
            IGenericFoodService genericFoodService,
            ILocalStorageService localStorage,
            IOpenFoodFactsClientService openFoodFactsClientService,
            IAuthService authService,
            IPantryService pantryService)
            : base(storeService)
        {
            _shoppingListService = shoppingListService;
            _foodProductService = foodProductService;
            _genericFoodService = genericFoodService;
            _localStorage = localStorage;
            _openFoodFactsClientService = openFoodFactsClientService;
            _authService = authService;
            _pantryService = pantryService;
            AutoAddToPantry = storeService.GetAppState().userAutoAddToPantry;
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

        public void CheckPendingFoodInfoAddRequest()
        {
            var state = _storeService.GetAppState();
            if (state.foodInfoAddRequest == null)
                return;

            var request = state.foodInfoAddRequest;
            _store.Dispatch(AppActions.foodInfoAddRequestConsumed.Invoke());

            if (request.EntryContext != "shoppingList")
                return;

            if (request.FoodType == FoodInfoType.Product)
                _ = SafeAddProductFromFoodInfoAsync(request);
            else
                _ = SafeAddGenericFoodFromFoodInfoAsync(request);
        }

        private async Task SafeAddProductFromFoodInfoAsync(AddToContextRequestedAction request)
        {
            try
            {
                if (!string.IsNullOrEmpty(request.FoodData))
                {
                    var product = JsonConvert.DeserializeObject<OpenFoodFactsProduct>(request.FoodData);
                    if (product != null && !string.IsNullOrEmpty(product.barcode))
                    {
                        await ImportAndAddItemAsync(product, null, null);
                        return;
                    }
                }

                if (string.IsNullOrEmpty(request.FoodId)) return;

                if (Guid.TryParse(request.FoodId, out _))
                {
                    var (food, foodError) = await _foodProductService.GetFoodByIdAsync(request.FoodId);
                    if (foodError != null || food == null)
                    {
                        Debug.LogWarning($"[{GetType().Name}] Could not load food product for add: {foodError?.message}");
                        return;
                    }
                    await AddFoodProductToShoppingListAsync(food);
                }
                else
                {
                    var (imported, importError) = await ImportByBarcodeAsync(request.FoodId);
                    if (importError != null || imported == null)
                    {
                        Debug.LogWarning($"[{GetType().Name}] Could not import food product by barcode {request.FoodId}: {importError?.message}");
                        return;
                    }
                    await AddFoodProductToShoppingListAsync(imported);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SafeAddProductFromFoodInfoAsync failed: {ex.Message}");
            }
        }

        private async Task SafeAddGenericFoodFromFoodInfoAsync(AddToContextRequestedAction request)
        {
            try
            {
                if (!string.IsNullOrEmpty(request.FoodData))
                {
                    var gf = JsonConvert.DeserializeObject<GenericFood>(request.FoodData);
                    if (gf != null)
                    {
                        await AddGenericFoodItemAsync(gf, null, null);
                        return;
                    }
                }

                if (string.IsNullOrEmpty(request.FoodId)) return;
                var (genericFood, gfError) = await _genericFoodService.GetGenericFoodByIdAsync(request.FoodId);
                if (gfError != null || genericFood == null)
                {
                    Debug.LogWarning($"[{GetType().Name}] Could not load generic food for add: {gfError?.message}");
                }
                await AddGenericFoodItemAsync(genericFood, null, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SafeAddGenericFoodFromFoodInfoAsync failed: {ex.Message}");
            }
        }

        private async Task AddFoodProductToShoppingListAsync(FoodProduct food)
        {
            if (string.IsNullOrEmpty(_currentListId))
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_LIST_ITEM");
                return;
            }

            var (added, addError) = await _shoppingListService.AddItemAsync(_currentListId, food.id, 1f, "PIECES");

            if (addError != null && (addError.statusCode == 409 || (addError.error != null && addError.error.Equals("ConflictException", StringComparison.OrdinalIgnoreCase)) || (addError.message != null && addError.message.ToLowerInvariant().Contains("already"))))
            {
                var match = _allItems.Find(i => i.Item?.foodProductId == food.id);
                if (match != null && match.Item != null)
                {
                    float newQty = match.Item.quantity + 1f;
                    var (updated, updateErr) = await _shoppingListService.UpdateItemAsync(_currentListId, match.Item.id, newQty, match.Item.unit, match.Item.notes, match.Item.@checked);
                    if (updateErr == null && updated != null)
                    {
                        match.Item.quantity = newQty;
                        ApplyFilter();
                        SaveCache();
                    }
                }
                addError = null;
            }

            if (addError != null)
            {
                ErrorDetail = addError;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_TO_LIST");
                return;
            }

            ErrorDetail = null;
            if (added != null)
            {
                var newItem = new ShoppingListItemView
                {
                    Item = added,
                    FoodName = food.name ?? added.foodProduct?.name ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN"),
                };
                _allItems.Add(newItem);
            }
            FilterText = "";
            ApplyFilter();
            SaveCache();
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

        public async Task<PaginatedGenericFoodResponse> SearchByFoodGroupAsync(string foodGroup, int page, int pageSize)
        {
            var (result, error) = await _genericFoodService.SearchGenericFoodsAsync(foodGroup: foodGroup, page: page, pageSize: pageSize);
            if (error != null)
            {
                ErrorDetail = error;
                return null;
            }
            return result;
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

            var state = _storeService.GetAppState();
            if (state.userLastShoppingListId != listId)
            {
                var updateRequest = new ProfileUpdateRequest
                {
                    preferences = new ProfileUpdatePreferences
                    {
                        shoppingResponsibility = state.userShoppingResponsibility,
                        dietaryPreference = state.userDietaryPreference,
                        onboardingSurvey = state.userOnboardingSurvey,
                        lastShoppingListId = listId,
                        autoAddToPantry = state.userAutoAddToPantry
                    }
                };
                _ = _authService.UpdateProfileAsync(updateRequest);
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

        /// <summary>
        /// Reloads the shopping list items using the already-stored list ID.
        /// Used when the FoodInfoOverlay action callback requires a refresh.
        /// </summary>
        public Task ReloadAsync()
        {
            if (string.IsNullOrEmpty(_currentListId))
            {
                return Task.CompletedTask;
            }
            return LoadAsync(_currentListId);
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

                if (string.IsNullOrEmpty(foodName) && !string.IsNullOrEmpty(item.genericFoodId))
                {
                    var (fetched, _) = await _genericFoodService.GetGenericFoodByIdAsync(item.genericFoodId);
                    foodName = fetched?.foodName;
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

            string normalized = query.Trim().ToLowerInvariant();
            string cacheKey = FoodSearchCachePrefix + normalized;

            CachedFoodSearch cached = _localStorage.GetValue<CachedFoodSearch>(cacheKey);
            if (cached?.data?.products != null && IsCacheFresh(cached.cachedAtTicks))
            {
                SearchResults = new List<OpenFoodFactsProduct>(cached.data.products);
                return SearchResults;
            }

            IsSearching = true;
            var (products, error) = await FoodProductFlow.SearchProductsAsync(_foodProductService, _openFoodFactsClientService, query);
            IsSearching = false;

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_SEARCH_PRODUCTS");
                if (cached?.data?.products != null)
                {
                    SearchResults = new List<OpenFoodFactsProduct>(cached.data.products);
                }
                else
                {
                    SearchResults = new List<OpenFoodFactsProduct>();
                }
            }
            else
            {
                if (products != null && products.Count > 0)
                {
                    var response = new OpenFoodFactsSearchResponse
                    {
                        products = products.ToArray()
                    };
                    _localStorage.SetValue(cacheKey, new CachedFoodSearch
                    {
                        data = response,
                        cachedAtTicks = DateTime.UtcNow.Ticks
                    });
                    SearchResults = products;
                }
                else if (cached?.data?.products != null)
                {
                    SearchResults = new List<OpenFoodFactsProduct>(cached.data.products);
                }
                else
                {
                    SearchResults = new List<OpenFoodFactsProduct>();
                }
            }

            return SearchResults;
        }

        private static bool IsCacheFresh(long cachedAtTicks)
        {
            if (cachedAtTicks <= 0) return false;
            DateTime cachedAt = new DateTime(cachedAtTicks, DateTimeKind.Utc);
            return (DateTime.UtcNow - cachedAt) < CacheTtl;
        }

        public async Task<bool> ImportAndAddItemAsync(OpenFoodFactsProduct product, float? quantity = null, string unit = null)
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

            float addQty = quantity ?? 1f;
            string addUnit = unit ?? "PIECES";
            var (added, addError) = await _shoppingListService.AddItemAsync(_currentListId, foodItem.id, addQty, addUnit);

            if (addError != null && (addError.statusCode == 409 || (addError.error != null && addError.error.Equals("ConflictException", StringComparison.OrdinalIgnoreCase)) || (addError.message != null && addError.message.ToLowerInvariant().Contains("already"))))
            {
                var match = _allItems.Find(i => i.Item?.foodProductId == foodItem.id);
                if (match != null && match.Item != null)
                {
                    float newQty = match.Item.quantity + addQty;
                    var (updated, updateErr) = await _shoppingListService.UpdateItemAsync(_currentListId, match.Item.id, newQty, match.Item.unit, match.Item.notes, match.Item.@checked);
                    if (updateErr == null && updated != null)
                    {
                        match.Item.quantity = newQty;
                        ApplyFilter();
                        SaveCache();
                    }
                }
                addError = null;
            }

            if (addError != null)
            {
                ErrorDetail = addError;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_TO_LIST");
                return false;
            }

            ErrorDetail = null;
            SearchResults = new List<OpenFoodFactsProduct>();
            SearchQuery = "";
            if (added != null)
            {
                var newItem = new ShoppingListItemView
                {
                    Item = added,
                    FoodName = product.name ?? added.foodProduct?.name ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN"),
                };
                _allItems.Add(newItem);
            }
            FilterText = "";
            ApplyFilter();
            SaveCache();
            return true;
        }

        public async Task<bool> AddGenericFoodItemAsync(GenericFood food, float? quantity = null, string unit = null)
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
            float addQty = quantity ?? 1f;
            string addUnit = unit ?? "PIECES";
            var (added, addError) = await _shoppingListService.AddItemAsync(_currentListId, genericFoodId: food.id, quantity: addQty, unit: addUnit);

            if (addError != null && (addError.statusCode == 409 || (addError.error != null && addError.error.Equals("ConflictException", StringComparison.OrdinalIgnoreCase)) || (addError.message != null && addError.message.ToLowerInvariant().Contains("already"))))
            {
                var match = _allItems.Find(i => i.Item?.genericFoodId == food.id);
                if (match != null && match.Item != null)
                {
                    float newQty = match.Item.quantity + addQty;
                    var (updated, updateErr) = await _shoppingListService.UpdateItemAsync(_currentListId, match.Item.id, newQty, match.Item.unit, match.Item.notes, match.Item.@checked);
                    if (updateErr == null && updated != null)
                    {
                        match.Item.quantity = newQty;
                        ApplyFilter();
                        SaveCache();
                    }
                }
                addError = null;
            }

            if (addError != null)
            {
                ErrorDetail = addError;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_TO_LIST");
                return false;
            }

            ErrorDetail = null;
            SearchResults = new List<OpenFoodFactsProduct>();
            SearchQuery = "";
            if (added != null)
            {
                var newItem = new ShoppingListItemView
                {
                    Item = added,
                    FoodName = added.genericFood?.foodName ?? food.foodName ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN"),
                };
                _allItems.Add(newItem);
            }
            FilterText = "";
            ApplyFilter();
            SaveCache();
            return true;
        }

        public async Task<(FoodProduct Result, ApiErrorResponse Error)> ImportByBarcodeAsync(string barcode)
        {
            return await FoodProductFlow.ImportByBarcodeAsync(_foodProductService, _openFoodFactsClientService, barcode);
        }

        public event Action<string> OnPantryItemAdded;

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

            if (newChecked && AutoAddToPantry && !_autoAddedItemIds.Contains(itemId))
            {
                await AutoAddToPantryAsync(view, itemId);
            }

            ApplyFilter();
            SaveCache();
        }

        private async Task AutoAddToPantryAsync(ShoppingListItemView view, string itemId)
        {
            string foodProductId = view.Item.foodProductId;
            string genericFoodId = view.Item.genericFoodId;
            float quantity = view.Item.quantity > 0 ? view.Item.quantity : 1f;
            string unit = !string.IsNullOrEmpty(view.Item.unit) ? view.Item.unit : "PIECES";

            var (pantryItem, pantryError) = await _pantryService.AddItemAsync(
                foodProductId, genericFoodId, quantity, unit);

            if (pantryError != null)
            {
                ErrorDetail = pantryError;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_TO_PANTRY");
                return;
            }

            _autoAddedItemIds.Add(itemId);
            OnPantryItemAdded?.Invoke(view.FoodName);
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

        public async Task ResetListAsync()
        {
            ErrorMessage = "";

            var checkedItems = _allItems.Where(v => v.Item.@checked).ToList();
            if (checkedItems.Count == 0) return;

            var tasks = checkedItems.Select(v =>
                _shoppingListService.UpdateItemAsync(_currentListId, v.Item.id, null, null, null, false)).ToArray();
            var results = await Task.WhenAll(tasks);

            var firstError = results.FirstOrDefault(r => r.Error != null);
            if (firstError.Error != null)
            {
                var resetErr = firstError.Error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_RESET_LIST");
                await LoadAsync(_currentListId, ListName);
                ErrorDetail = resetErr;
                return;
            }

            ErrorDetail = null;
            foreach (var v in checkedItems)
            {
                v.Item.@checked = false;
            }
            _autoAddedItemIds.Clear();
            ApplyFilter();
            SaveCache();
        }

        public async Task SyncAutoAddToPantryAsync(bool value)
        {
            var state = _storeService.GetAppState();
            var request = new ProfileUpdateRequest
            {
                preferences = new ProfileUpdatePreferences
                {
                    shoppingResponsibility = state.userShoppingResponsibility,
                    dietaryPreference = state.userDietaryPreference,
                    onboardingSurvey = state.userOnboardingSurvey,
                    lastShoppingListId = state.userLastShoppingListId,
                    autoAddToPantry = value
                }
            };

            var (success, error) = await _authService.UpdateProfileAsync(request);
            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_UPDATE_PREFERENCES");
                return;
            }
            ErrorDetail = null;
        }
    }
}
