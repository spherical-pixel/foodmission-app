using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class PantryViewModel : ViewModelBase
    {
        private readonly IPantryService _pantryService;
        private readonly IFoodService _foodService;
        private readonly IFoodCategoryService _foodCategoryService;

        [ObservableProperty]
        private List<PantryItemView> m_Items = new();

        [ObservableProperty]
        private bool m_IsLoading;

        [ObservableProperty]
        private string m_ErrorMessage = "";

        [ObservableProperty]
        private List<OpenFoodFactsProduct> m_FoodSearchResults = new();

        [ObservableProperty]
        private List<FoodCategory> m_CategorySearchResults = new();

        [ObservableProperty]
        private string m_SearchQuery = "";

        [ObservableProperty]
        private bool m_IsSearching;

        public PantryViewModel(
            IStoreService storeService,
            IPantryService pantryService,
            IFoodService foodService,
            IFoodCategoryService foodCategoryService)
            : base(storeService)
        {
            _pantryService = pantryService;
            _foodService = foodService;
            _foodCategoryService = foodCategoryService;
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorMessage = "";

            Pantry pantry = await _pantryService.GetPantryAsync();

            IsLoading = false;

            if (pantry == null)
            {
                ErrorMessage = "Error loading pantry";
                Items = new List<PantryItemView>();
                return;
            }

            PantryItem[] rawItems = pantry.items ?? System.Array.Empty<PantryItem>();
            Task<PantryItemView>[] tasks = new Task<PantryItemView>[rawItems.Length];

            for (int i = 0; i < rawItems.Length; i++)
            {
                tasks[i] = EnrichItemAsync(rawItems[i]);
            }

            PantryItemView[] enriched = await Task.WhenAll(tasks);
            Items = new List<PantryItemView>(enriched);
        }

        private async Task<PantryItemView> EnrichItemAsync(PantryItem item)
        {
            string displayName = "Unknown";
            string imageUrl = null;

            if (!string.IsNullOrEmpty(item.foodId))
            {
                FoodItem food = await _foodService.GetFoodByIdAsync(item.foodId);
                displayName = food?.name ?? "Unknown";
            }
            else if (!string.IsNullOrEmpty(item.foodCategoryId))
            {
                FoodCategory category = await _foodCategoryService.GetCategoryByIdAsync(item.foodCategoryId);
                displayName = category?.name ?? "Unknown";
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

            IsSearching = true;
            OpenFoodFactsSearchResponse response = await _foodService.SearchOpenFoodFactsAsync(query);
            IsSearching = false;

            FoodSearchResults = response?.products != null
                ? new List<OpenFoodFactsProduct>(response.products)
                : new List<OpenFoodFactsProduct>();
        }

        public async Task SearchCategoriesAsync(string query)
        {
            SearchQuery = query;

            if (string.IsNullOrWhiteSpace(query))
            {
                CategorySearchResults = new List<FoodCategory>();
                return;
            }

            IsSearching = true;
            PaginatedFoodCategoryResponse response = await _foodCategoryService.SearchCategoriesAsync(query);
            IsSearching = false;

            CategorySearchResults = response?.data != null
                ? new List<FoodCategory>(response.data)
                : new List<FoodCategory>();
        }

        public async Task ImportAndAddFoodItemAsync(OpenFoodFactsProduct product, float quantity, string unit)
        {
            FoodItem foodItem = await _foodService.ImportFromBarcodeAsync(product.barcode);

            if (foodItem == null)
            {
                Debug.LogError($"[{GetType().Name}] Failed to import food: {product.name}");
                return;
            }

            PantryItem added = await _pantryService.AddItemAsync(foodItem.id, null, quantity, unit);

            if (added != null)
            {
                FoodSearchResults = new List<OpenFoodFactsProduct>();
                SearchQuery = "";
                await LoadAsync();
            }
        }

        public async Task AddCategoryItemAsync(FoodCategory category, float quantity, string unit)
        {
            PantryItem added = await _pantryService.AddItemAsync(null, category.id, quantity, unit);

            if (added != null)
            {
                CategorySearchResults = new List<FoodCategory>();
                SearchQuery = "";
                await LoadAsync();
            }
        }

        public async Task DeleteItemAsync(string itemId)
        {
            bool success = await _pantryService.DeleteItemAsync(itemId);

            if (success)
            {
                Items = new List<PantryItemView>(Items.FindAll(v => v.Item.id != itemId));
            }
        }
    }
}
