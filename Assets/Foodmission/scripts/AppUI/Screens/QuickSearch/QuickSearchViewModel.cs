using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    public class QuickMealLogOptions
    {
        public FoodInfoType FoodType { get; set; }
        public string FoodId { get; set; }
        public string FoodData { get; set; }
        public string FoodName { get; set; }
    }

    [ObservableObject]
    public partial class QuickSearchViewModel : ViewModelBase
    {
        private readonly IFoodProductService _foodProductService;
        private readonly IGenericFoodService _genericFoodService;
        private readonly IPantryService _pantryService;
        private readonly IShoppingListService _shoppingListService;
        private readonly ILocalStorageService _localStorage;
        private readonly IOpenFoodFactsClientService _openFoodFactsClientService;
        private readonly ICatalogService _catalogService;

        private const string FoodSearchCachePrefix = "food_search_cache_";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1440);

        [ObservableProperty] private List<OpenFoodFactsProduct> _searchResults = new();
        [ObservableProperty] private string _searchQuery = "";
        [ObservableProperty] private bool _isSearching;
        [ObservableProperty] private string _errorMessage = "";
        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private ApiErrorResponse m_ErrorDetail;
        [ObservableProperty] private QuickMealLogOptions _pendingMealLogAdd;

        public QuickSearchViewModel(
            IStoreService storeService,
            IFoodProductService foodProductService,
            IGenericFoodService genericFoodService,
            IPantryService pantryService,
            IShoppingListService shoppingListService,
            ILocalStorageService localStorage,
            IOpenFoodFactsClientService openFoodFactsClientService,
            ICatalogService catalogService)
            : base(storeService)
        {
            _foodProductService = foodProductService;
            _genericFoodService = genericFoodService;
            _pantryService = pantryService;
            _shoppingListService = shoppingListService;
            _localStorage = localStorage;
            _openFoodFactsClientService = openFoodFactsClientService;
            _catalogService = catalogService;
        }

        public async Task<CatalogItem[]> GetMealTypesAsync()
        {
            try
            {
                string lang = _storeService?.GetAppState()?.lang ?? "en";
                if (_catalogService != null)
                {
                    var (types, error) = await _catalogService.GetTypeOfMealsAsync(lang);
                    if (error == null && types != null && types.Length > 0)
                    {
                        return types;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] GetMealTypesAsync failed: {ex.Message}");
            }
            return Array.Empty<CatalogItem>();
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
                SearchResults = cached?.data?.products != null ? new List<OpenFoodFactsProduct>(cached.data.products) : new List<OpenFoodFactsProduct>();
            }
            else
            {
                if (products != null && products.Count > 0)
                {
                    var response = new OpenFoodFactsSearchResponse { products = products.ToArray() };
                    _localStorage.SetValue(cacheKey, new CachedFoodSearch
                    {
                        data = response,
                        cachedAtTicks = DateTime.UtcNow.Ticks
                    });
                    SearchResults = products;
                }
                else
                {
                    SearchResults = cached?.data?.products != null ? new List<OpenFoodFactsProduct>(cached.data.products) : new List<OpenFoodFactsProduct>();
                }
            }

            return SearchResults;
        }

        public async Task<List<GenericFood>> GetGenericFoodsAsync()
        {
            var (result, error) = await _genericFoodService.SearchGenericFoodsAsync(query: null, pageSize: 100);
            if (error != null)
            {
                ErrorDetail = error;
                return new List<GenericFood>();
            }
            return result?.items != null ? new List<GenericFood>(result.items) : new List<GenericFood>();
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

        public async Task<(FoodProduct Result, ApiErrorResponse Error)> ImportByBarcodeAsync(string barcode)
        {
            return await FoodProductFlow.ImportByBarcodeAsync(_foodProductService, _openFoodFactsClientService, barcode);
        }

        public void CheckPendingFoodInfoAddRequest()
        {
            var state = _storeService.GetAppState();
            if (state.foodInfoAddRequest == null) return;

            var request = state.foodInfoAddRequest;
            _store.Dispatch(AppActions.foodInfoAddRequestConsumed.Invoke());

            if (request.EntryContext == "pantry")
            {
                _ = AddToPantryDirectAsync(request);
            }
            else if (request.EntryContext == "shoppingList")
            {
                _ = AddToShoppingListDirectAsync(request);
            }
            else if (request.EntryContext == "mealLog")
            {
                string name = ExtractFoodName(request);
                PendingMealLogAdd = new QuickMealLogOptions
                {
                    FoodType = request.FoodType,
                    FoodId = request.FoodId,
                    FoodData = request.FoodData,
                    FoodName = name
                };
            }
        }

        public async Task AddToPantryDirectAsync(AddToContextRequestedAction request)
        {
            StatusMessage = "";
            ErrorMessage = "";

            try
            {
                string productId = null;
                string genericId = null;

                if (request.FoodType == FoodInfoType.Product)
                {
                    if (!string.IsNullOrEmpty(request.FoodData))
                    {
                        var offProduct = JsonConvert.DeserializeObject<OpenFoodFactsProduct>(request.FoodData);
                        if (offProduct != null)
                        {
                            var (impProduct, impErr) = await ImportByBarcodeAsync(offProduct.barcode);
                            if (impProduct != null) productId = impProduct.id;
                        }
                    }
                    if (string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(request.FoodId))
                    {
                        productId = request.FoodId;
                    }
                }
                else
                {
                    genericId = request.FoodId;
                }

                var (added, error) = await _pantryService.AddItemAsync(productId, genericId, 1f, "PIECES");
                if (error != null)
                {
                    ErrorDetail = error;
                    return;
                }

                StatusMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ITEM_ADDED_TO_PANTRY") ?? "Añadido a la despensa";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuickSearchViewModel] AddToPantryDirectAsync failed: {ex.Message}");
            }
        }

        public async Task AddToShoppingListDirectAsync(AddToContextRequestedAction request)
        {
            StatusMessage = "";
            ErrorMessage = "";

            try
            {
                var (lists, listsError) = await _shoppingListService.GetListsAsync();
                if (listsError != null)
                {
                    ErrorDetail = listsError;
                    return;
                }

                string targetListId = lists != null && lists.Length > 0 ? lists[0].id : null;
                if (string.IsNullOrEmpty(targetListId))
                {
                    var (newList, createErr) = await _shoppingListService.CreateListAsync("Mi Lista");
                    if (createErr != null || newList == null)
                    {
                        ErrorDetail = createErr;
                        return;
                    }
                    targetListId = newList.id;
                }

                string productId = null;
                string genericId = null;

                if (request.FoodType == FoodInfoType.Product)
                {
                    if (!string.IsNullOrEmpty(request.FoodData))
                    {
                        var offProduct = JsonConvert.DeserializeObject<OpenFoodFactsProduct>(request.FoodData);
                        if (offProduct != null)
                        {
                            var (impProduct, impErr) = await ImportByBarcodeAsync(offProduct.barcode);
                            if (impProduct != null) productId = impProduct.id;
                        }
                    }
                    if (string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(request.FoodId))
                    {
                        productId = request.FoodId;
                    }
                }
                else
                {
                    genericId = request.FoodId;
                }

                var (added, itemError) = await _shoppingListService.AddItemAsync(targetListId, productId, 1f, "PIECES", null, false, genericId);
                if (itemError != null)
                {
                    ErrorDetail = itemError;
                    return;
                }

                StatusMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ITEM_ADDED_TO_SHOPPING_LIST") ?? "Añadido a la lista de la compra";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuickSearchViewModel] AddToShoppingListDirectAsync failed: {ex.Message}");
            }
        }

        private static string ExtractFoodName(AddToContextRequestedAction request)
        {
            if (!string.IsNullOrEmpty(request.FoodData))
            {
                try
                {
                    if (request.FoodType == FoodInfoType.Product)
                    {
                        var p = JsonConvert.DeserializeObject<OpenFoodFactsProduct>(request.FoodData);
                        if (!string.IsNullOrEmpty(p?.name)) return p.name;
                    }
                    else
                    {
                        var g = JsonConvert.DeserializeObject<GenericFood>(request.FoodData);
                        if (!string.IsNullOrEmpty(g?.foodName)) return g.foodName;
                    }
                }
                catch { }
            }
            return "Alimento";
        }

        private static bool IsCacheFresh(long cachedAtTicks)
        {
            if (cachedAtTicks <= 0) return false;
            DateTime cachedAt = new DateTime(cachedAtTicks, DateTimeKind.Utc);
            return (DateTime.UtcNow - cachedAt) < CacheTtl;
        }
    }
}
