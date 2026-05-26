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

        private const string CacheKey = "pantry_cache";
        private const string FoodSearchCachePrefix = "food_search_";
        private const string CategorySearchCachePrefix = "category_search_";
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
        private List<OpenFoodFactsProduct> m_FoodSearchResults = new();

        [ObservableProperty]
        private List<GenericFood> m_CategorySearchResults = new();

        [ObservableProperty]
        private string m_SearchQuery = "";

        [ObservableProperty]
        private bool m_IsSearching;

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
            ILocalStorageService localStorage)
            : base(storeService)
        {
            _pantryService = pantryService;
            _foodProductService = foodProductService;
            _genericFoodService = genericFoodService;
            _localStorage = localStorage;
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
                var (category, _) = await _genericFoodService.GetCategoryByIdAsync(item.genericFoodId);
                displayName = category?.name ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
            }

            return new PantryItemView
            {
                Item = item,
                DisplayName = displayName,
                ImageUrl = imageUrl
            };
        }

        public async Task SearchFoodsAsync(string query)
        {
            SearchQuery = query;

            if (string.IsNullOrWhiteSpace(query))
            {
                FoodSearchResults = new List<OpenFoodFactsProduct>();
                return;
            }

            string normalized = query.Trim().ToLowerInvariant();
            string cacheKey = FoodSearchCachePrefix + normalized;

            CachedFoodSearch cached = _localStorage.GetValue<CachedFoodSearch>(cacheKey);
            if (cached?.data?.products != null && IsCacheFresh(cached.cachedAtTicks))
            {
                FoodSearchResults = new List<OpenFoodFactsProduct>(cached.data.products);
                return;
            }

            IsSearching = true;
            var (response, _) = await _foodProductService.SearchOpenFoodFactsAsync(query);
            IsSearching = false;

            if (response?.products != null)
            {
                _localStorage.SetValue(cacheKey, new CachedFoodSearch
                {
                    data = response,
                    cachedAtTicks = DateTime.UtcNow.Ticks
                });
                FoodSearchResults = new List<OpenFoodFactsProduct>(response.products);
            }
            else if (cached?.data?.products != null)
            {
                FoodSearchResults = new List<OpenFoodFactsProduct>(cached.data.products);
            }
            else
            {
                FoodSearchResults = new List<OpenFoodFactsProduct>();
            }
        }

        public async Task SearchCategoriesAsync(string query)
        {
            SearchQuery = query;

            if (string.IsNullOrWhiteSpace(query))
            {
                CategorySearchResults = new List<GenericFood>();
                return;
            }

            string normalized = query.Trim().ToLowerInvariant();
            string cacheKey = CategorySearchCachePrefix + normalized;

            CachedCategorySearch cached = _localStorage.GetValue<CachedCategorySearch>(cacheKey);
            if (cached?.data?.data != null && IsCacheFresh(cached.cachedAtTicks))
            {
                CategorySearchResults = new List<GenericFood>(cached.data.data);
                return;
            }

            IsSearching = true;
            var (response, _) = await _genericFoodService.SearchCategoriesAsync(query);
            IsSearching = false;

            if (response?.data != null)
            {
                _localStorage.SetValue(cacheKey, new CachedCategorySearch
                {
                    data = response,
                    cachedAtTicks = DateTime.UtcNow.Ticks
                });
                CategorySearchResults = new List<GenericFood>(response.data);
            }
            else if (cached?.data?.data != null)
            {
                CategorySearchResults = new List<GenericFood>(cached.data.data);
            }
            else
            {
                CategorySearchResults = new List<GenericFood>();
            }
        }

        private static bool IsCacheFresh(long cachedAtTicks)
        {
            if (cachedAtTicks <= 0) return false;
            DateTime cachedAt = new DateTime(cachedAtTicks, DateTimeKind.Utc);
            return (DateTime.UtcNow - cachedAt) < CacheTtl;
        }

        public async Task ImportAndAddFoodItemAsync(OpenFoodFactsProduct product, float quantity, string unit)
        {
            var (foodItem, foodError) = await _foodProductService.ImportFromBarcodeAsync(product.barcode);

            if (foodError != null)
            {
                if (foodError.statusCode == 400)
                {
                    var (existingFood, findError) = await _foodProductService.FindByBarcodeAsync(product.barcode);
                    if (findError == null && existingFood != null)
                    {
                        foodItem = existingFood;
                        foodError = null;
                    }
                }
            }

            if (foodError != null)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FAILED_IMPORT_FOOD", new object[] { product.name });
                ErrorDetail = foodError;
                return;
            }

            var (added, addError) = await _pantryService.AddItemAsync(foodItem.id, null, quantity, unit);

            if (addError != null)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_ITEM_MISSING");
                ErrorDetail = addError;
            }
            else
            {
                FoodSearchResults = new List<OpenFoodFactsProduct>();
                SearchQuery = "";
                ErrorMessage = "";
                ErrorDetail = null;
                await LoadAsync();
            }
        }

        public async Task<FoodProduct> ImportByBarcodeAsync(string barcode)
        {
            var (foodItem, error) = await _foodProductService.ImportFromBarcodeAsync(barcode);
            if (error != null && error.statusCode == 400)
            {
                var (existingFood, findError) = await _foodProductService.FindByBarcodeAsync(barcode);
                if (findError == null && existingFood != null)
                    return existingFood;
            }
            return foodItem;
        }

        public async Task AddCategoryItemAsync(GenericFood category, float quantity, string unit)
        {
            var (added, error) = await _pantryService.AddItemAsync(null, category.id, quantity, unit);

            if (error != null)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_ADD_CATEGORY_ITEM");
                ErrorDetail = error;
            }
            else
            {
                CategorySearchResults = new List<GenericFood>();
                SearchQuery = "";
                ErrorMessage = "";
                ErrorDetail = null;
                await LoadAsync();
            }
        }

        public async Task DeleteItemAsync(string itemId)
        {
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
