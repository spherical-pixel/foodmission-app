using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class PantryViewModel : ViewModelBase
    {
        private readonly IPantryService _pantryService;
        private readonly IFoodProductService _foodProductService;
        private readonly IGenericFoodService _genericFoodService;
        private readonly ILocalStorageService _localStorage;
        private readonly IOpenFoodFactsClientService _openFoodFactsClientService;

        private const string CacheKey = "pantry_cache";
        private const string FoodSearchCachePrefix = "food_search_";
        private const string GenericFoodSearchCachePrefix = "generic_food_search_";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

        private List<PantryItemView> _allItems = new();

        [ObservableProperty]
        private List<PantryItemView> m_Items = new();

        [ObservableProperty]
        private bool m_IsLoading;

        [ObservableProperty]
        private string m_ErrorMessage = "";

        [ObservableProperty]
        private string m_FilterText = "";

        [ObservableProperty]
        private ExpiredPantryItem[] m_ExpiredItems = Array.Empty<ExpiredPantryItem>();

        [ObservableProperty]
        private int m_ExpiredItemCount;

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

        public bool HasExpiredItems => ExpiredItemCount > 0;

        public PantryViewModel(
            IStoreService storeService,
            IPantryService pantryService,
            IFoodProductService foodProductService,
            IGenericFoodService genericFoodService,
            ILocalStorageService localStorage,
            IOpenFoodFactsClientService openFoodFactsClientService)
            : base(storeService)
        {
            _pantryService = pantryService;
            _foodProductService = foodProductService;
            _genericFoodService = genericFoodService;
            _localStorage = localStorage;
            _openFoodFactsClientService = openFoodFactsClientService;
        }

        public void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                Items = new List<PantryItemView>(_allItems);
            }
            else
            {
                string filter = FilterText.ToLowerInvariant();
                Items = _allItems.Where(v => v.DisplayName.ToLowerInvariant().Contains(filter)).ToList();
            }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorMessage = "";

            var pantryTask = _pantryService.GetPantryAsync();
            var expiredTask = _pantryService.GetExpiredItemsAsync();

            await Task.WhenAll(pantryTask, expiredTask);

            var (expiredItems, _) = expiredTask.Result;
            ExpiredItems = expiredItems ?? Array.Empty<ExpiredPantryItem>();
            ExpiredItemCount = ExpiredItems.Length;

            var (pantry, pantryError) = pantryTask.Result;

            if (pantryError != null)
            {
                PantryItem[] cached = _localStorage.GetValue<PantryItemArrayWrapper>(CacheKey)?.items;
                if (cached != null && cached.Length > 0)
                {
                    PantryItemView[] enriched = await EnrichItemsAsync(cached);
                    _allItems = new List<PantryItemView>(enriched);
                }
                else
                {
                    ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_LOADING_PANTRY");
                    _allItems = new List<PantryItemView>();
                }

                ErrorDetail = pantryError;

                IsLoading = false;
                ApplyFilter();
                return;
            }

            PantryItem[] rawItems = pantry.items ?? System.Array.Empty<PantryItem>();

            _localStorage.SetValue(CacheKey, new PantryItemArrayWrapper { items = rawItems });

            PantryItemView[] enrichedItems = await EnrichItemsAsync(rawItems);
            _allItems = new List<PantryItemView>(enrichedItems);
            ErrorDetail = null;
            IsLoading = false;
            ApplyFilter();
        }

        private async Task<PantryItemView[]> EnrichItemsAsync(PantryItem[] items)
        {
            Task<PantryItemView>[] tasks = new Task<PantryItemView>[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                tasks[i] = EnrichItemAsync(items[i]);
            }

            return await Task.WhenAll(tasks);
        }

        private void SaveCacheFromAllItems()
        {
            PantryItem[] raw = new PantryItem[_allItems.Count];
            for (int i = 0; i < _allItems.Count; i++)
                raw[i] = _allItems[i].Item;

            _localStorage.SetValue(CacheKey, new PantryItemArrayWrapper { items = raw });
        }

        private async Task<PantryItemView> EnrichItemAsync(PantryItem item)
        {
            string displayName = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
            string imageUrl = null;

            if (!string.IsNullOrEmpty(item.foodProductId))
            {
                var (food, _) = await _foodProductService.GetFoodByIdAsync(item.foodProductId);
                displayName = food?.name ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
            }
            else if (!string.IsNullOrEmpty(item.genericFoodId))
            {
                var (genericFood, _) = await _genericFoodService.GetGenericFoodByIdAsync(item.genericFoodId);
                displayName = genericFood?.foodName ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
            }

            return new PantryItemView
            {
                Item = item,
                DisplayName = displayName,
                ImageUrl = imageUrl
            };
        }

        public async Task<List<OpenFoodFactsProduct>> SearchFoodsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<OpenFoodFactsProduct>();
            }

            string normalized = query.Trim().ToLowerInvariant();
            string cacheKey = FoodSearchCachePrefix + normalized;

            CachedFoodSearch cached = _localStorage.GetValue<CachedFoodSearch>(cacheKey);
            if (cached?.data?.products != null && IsCacheFresh(cached.cachedAtTicks))
            {
                return new List<OpenFoodFactsProduct>(cached.data.products);
            }

            var (products, error) = await FoodProductFlow.SearchProductsAsync(_foodProductService, _openFoodFactsClientService, query);
            if (error != null)
            {
                ErrorDetail = error;
                if (cached?.data?.products != null)
                {
                    return new List<OpenFoodFactsProduct>(cached.data.products);
                }
                return new List<OpenFoodFactsProduct>();
            }

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
                return products;
            }

            if (cached?.data?.products != null)
            {
                return new List<OpenFoodFactsProduct>(cached.data.products);
            }

            return new List<OpenFoodFactsProduct>();
        }

        public async Task<List<GenericFood>> GetGenericFoodsAsync()
        {
            var (result, error) = await _genericFoodService.SearchGenericFoodsAsync(pageSize: 100);
            if (error != null)
            {
                ErrorDetail = error;
                return new List<GenericFood>();
            }
            return result?.items != null ? new List<GenericFood>(result.items) : new List<GenericFood>();
        }

        public async Task<List<GenericFood>> SearchGenericFoodsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<GenericFood>();
            }

            string normalized = query.Trim().ToLowerInvariant();
            string cacheKey = GenericFoodSearchCachePrefix + normalized;

            CachedGenericFoodSearch cached = _localStorage.GetValue<CachedGenericFoodSearch>(cacheKey);
            if (cached?.data?.items != null && IsCacheFresh(cached.cachedAtTicks))
            {
                return new List<GenericFood>(cached.data.items);
            }

            var (result, error) = await _genericFoodService.SearchGenericFoodsAsync(query, pageSize: 100);
            if (error != null)
            {
                ErrorDetail = error;
                return new List<GenericFood>();
            }

            if (result?.items != null)
            {
                _localStorage.SetValue(cacheKey, new CachedGenericFoodSearch
                {
                    data = result,
                    cachedAtTicks = DateTime.UtcNow.Ticks
                });
                return new List<GenericFood>(result.items);
            }

            if (cached?.data?.items != null)
            {
                return new List<GenericFood>(cached.data.items);
            }

            return new List<GenericFood>();
        }

        private static bool IsCacheFresh(long cachedAtTicks)
        {
            if (cachedAtTicks <= 0) return false;
            DateTime cachedAt = new DateTime(cachedAtTicks, DateTimeKind.Utc);
            return (DateTime.UtcNow - cachedAt) < CacheTtl;
        }

        public async Task ImportAndAddFoodItemAsync(OpenFoodFactsProduct product, float quantity, string unit)
        {
            var (foodItem, importError) = await ImportByBarcodeAsync(product.barcode);
            if (importError != null)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FAILED_IMPORT_FOOD", new object[] { product.name });
                ErrorDetail = importError;
                return;
            }

            var (added, addError) = await _pantryService.AddItemAsync(foodItem.id, null, quantity, unit);

            if (addError != null)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_ITEM_MISSING");
                ErrorDetail = addError;
                return;
            }

            ErrorDetail = null;
            var newItem = new PantryItemView { Item = added, DisplayName = product.name ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN") };
            _allItems.Add(newItem);
            FilterText = "";
            ApplyFilter();
            SaveCacheFromAllItems();
        }

        public async Task<(FoodProduct Result, ApiErrorResponse Error)> ImportByBarcodeAsync(string barcode)
        {
            return await FoodProductFlow.ImportByBarcodeAsync(_foodProductService, _openFoodFactsClientService, barcode);
        }

        public async Task AddGenericFoodItemAsync(GenericFood genericFood, float quantity, string unit)
        {
            if (!Guid.TryParse(genericFood.id, out _))
            {
                ErrorDetail = new ApiErrorResponse
                {
                    statusCode = 400,
                    error = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GENERIC_FOOD_NOT_AVAILABLE"),
                    message = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GENERIC_FOOD_NOT_AVAILABLE_DESC")
                };
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_CATEGORY_ITEM");
                return;
            }

            var (added, error) = await _pantryService.AddItemAsync(null, genericFood.id, quantity, unit);

            if (error != null)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_CATEGORY_ITEM");
                ErrorDetail = error;
                return;
            }

            ErrorDetail = null;
            var newItem = new PantryItemView { Item = added, DisplayName = genericFood.foodName ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN") };
            _allItems.Add(newItem);
            FilterText = "";
            ApplyFilter();
            SaveCacheFromAllItems();
        }

        public async Task DeleteItemAsync(string itemId)
        {
            ErrorMessage = "";
            var (success, error) = await _pantryService.DeleteItemAsync(itemId);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                _allItems = new List<PantryItemView>(_allItems.FindAll(v => v.Item.id != itemId));
                ApplyFilter();

                SaveCacheFromAllItems();

                ExpiredItems = ExpiredItems.Where(e => e.pantryItemId != itemId).ToArray();
                ExpiredItemCount = ExpiredItems.Length;

                ErrorDetail = null;
            }
        }

        public async Task<int> BatchWasteExpiredAsync()
        {
            if (ExpiredItems.Length == 0) return 0;

            var batchRequest = new BatchWasteRequest
            {
                items = ExpiredItems.Select(e => new BatchWasteItemRequest
                {
                    pantryItemId = e.pantryItemId,
                    quantity = e.quantity,
                    unit = e.unit,
                    notes = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "AUTO_DETECTED_EXPIRED")
                }).ToArray()
            };

            var (result, error) = await _pantryService.BatchWasteAsync(batchRequest);

            if (error != null)
            {
                ErrorDetail = error;
            }

            ExpiredItems = Array.Empty<ExpiredPantryItem>();
            ExpiredItemCount = 0;

            await LoadAsync();

            return result?.successCount ?? 0;
        }
    }
}
