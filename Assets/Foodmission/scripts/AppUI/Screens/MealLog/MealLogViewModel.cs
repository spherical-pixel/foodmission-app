using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform
{
    public enum MealLogStep
    {
        SelectingTypeOfMeal,
        SelectingSource,
        SelectingDishes,
        Saving
    }

    [ObservableObject]
    public partial class MealLogViewModel : ViewModelBase
    {
        private readonly IMealLogService _mealLogService;
        private readonly IMealService _mealService;
        private readonly IRecipeService _recipeService;
        private readonly IFoodProductService _foodProductService;
        private readonly IGenericFoodService _genericFoodService;
        private readonly IMealItemService _mealItemService;
        private readonly ICatalogService _catalogService;
        private readonly ILocalStorageService _localStorage;

        [ObservableProperty] private List<MealLog> m_LastTenLogs = new();

        private List<MealLog> _allLogs = new();

        private CancellationTokenSource _presetSearchCts;

        private List<MealLogItem> _originalItemsSnapshot = new();
        private string _pendingPresetName;

        public event Action<string> OnConfirmUpdateRequired;

        [ObservableProperty] private MealLogStep m_CurrentStep = MealLogStep.SelectingTypeOfMeal;

        [ObservableProperty] private CatalogItem[] m_TypeOfMealOptions = Array.Empty<CatalogItem>();

        [ObservableProperty] private int m_SelectedTypeOfMealIndex = -1;

        public CatalogItem SelectedTypeOfMeal => SelectedTypeOfMealIndex >= 0 && SelectedTypeOfMealIndex < TypeOfMealOptions.Length
            ? TypeOfMealOptions[SelectedTypeOfMealIndex] : null;

        [ObservableProperty] private bool m_MealFromPantry;

        [ObservableProperty] private bool m_EatenOut;

        [ObservableProperty] private string m_MealContainerName = "";

        [ObservableProperty] private Meal m_SelectedMealPreset;

        [ObservableProperty] private List<Meal> m_PresetResults = new();

        [ObservableProperty] private bool m_IsSearching;

        [ObservableProperty] private List<MealLogItem> m_SelectedItems = new();

        [ObservableProperty] private bool m_IsSaving;

        [ObservableProperty] private string m_ErrorMessage = "";

        [ObservableProperty] private ApiErrorResponse m_ErrorDetail;

        [ObservableProperty] private bool m_TodayLoaded;

        [ObservableProperty] private int m_CaloriesConsumed = 0;

        [ObservableProperty] private int m_CaloriesLeft = 2200;

        [ObservableProperty] private float m_CaloriesProgress = 0f;

        [ObservableProperty] private bool m_IsSearchingPresets;

        public MealLogViewModel(
            IStoreService storeService,
            IMealLogService mealLogService,
            IMealService mealService,
            IRecipeService recipeService,
            IFoodProductService foodProductService,
            IGenericFoodService genericFoodService,
            IMealItemService mealItemService,
            ICatalogService catalogService,
            ILocalStorageService localStorage)
            : base(storeService)
        {
            _mealLogService = mealLogService;
            _mealService = mealService;
            _recipeService = recipeService;
            _foodProductService = foodProductService;
            _genericFoodService = genericFoodService;
            _mealItemService = mealItemService;
            _catalogService = catalogService;
            _localStorage = localStorage;
        }

        public void RequestFoodInfo(FoodInfoType foodType, string foodId)
        {
            RaiseNavigationRequested(Actions.go_to_food_info,
                new Unity.AppUI.Navigation.Argument("foodType", foodType == FoodInfoType.Product ? "product" : "generic"),
                new Unity.AppUI.Navigation.Argument("foodId", foodId),
                new Unity.AppUI.Navigation.Argument("entryContext", "mealLog"));
        }

        public async Task InitializeAsync()
        {
            IsSearching = true;

            var (types, typeErr) = await _catalogService.GetTypeOfMealsAsync();
            if (typeErr == null && types != null)
                TypeOfMealOptions = types;

            IsSearching = false;
        }

        public void SelectTypeOfMeal(int index)
        {
            SelectedTypeOfMealIndex = index;
            if (SelectedTypeOfMeal != null)
                CurrentStep = MealLogStep.SelectingSource;
        }

        public void SetSource(bool fromPantry, bool eatenOut)
        {
            MealFromPantry = fromPantry;
            EatenOut = eatenOut;
            CurrentStep = MealLogStep.SelectingDishes;
        }

        public async void SelectMealPreset(Meal meal)
        {
            try
            {
                PresetResults = new List<Meal>();

                if (meal.isRecipe)
                {
                    SelectedMealPreset = meal; // name field stays as-is

                    var (recipe, err) = await _recipeService.GetRecipeAsync(meal.recipeId);
                    if (err != null)
                    {
                        ErrorDetail = err;
                        return;
                    }

                    var items = new List<MealLogItem>();
                    if (recipe?.ingredients != null)
                    {
                        var itemsDict = new Dictionary<string, MealLogItem>();
                        foreach (RecipeIngredient ing in recipe.ingredients)
                        {
                            if (string.IsNullOrEmpty(ing.foodProductId) && string.IsNullOrEmpty(ing.genericFoodId))
                                continue;

                            string key;
                            if (!string.IsNullOrEmpty(ing.foodProductId))
                                key = "fp:" + ing.foodProductId;
                            else
                                key = "gf:" + ing.genericFoodId;
                            var (qty, unit) = TryParseMeasure(ing.measure);

                            if (itemsDict.TryGetValue(key, out var existing))
                            {
                                existing.quantity += qty;
                            }
                            else
                            {
                                itemsDict[key] = new MealLogItem
                                {
                                    foodProductId = ing.foodProductId,
                                    genericFoodId = ing.genericFoodId,
                                    name = ing.name,
                                    quantity = qty,
                                    unit = unit,
                                    isProduct = !string.IsNullOrEmpty(ing.foodProductId),
                                    isGenericFood = !string.IsNullOrEmpty(ing.genericFoodId),
                                };
                            }
                        }
                        items = new List<MealLogItem>(itemsDict.Values);
                    }

                    SelectedItems = items;
                    _originalItemsSnapshot = DeepCopyItems(items);
                }
                else if (!string.IsNullOrEmpty(meal.id))
                {
                    MealContainerName = meal.name;
                    SelectedMealPreset = meal;

                    var (details, err) = await _mealItemService.GetByMealIdAsync(meal.id);
                    if (err != null)
                    {
                        ErrorDetail = err;
                        return;
                    }

                    var items = new List<MealLogItem>();
                    if (details != null)
                    {
                        foreach (MealItemDetail d in details)
                        {
                            string name = d.foodProduct?.name ?? d.genericFood?.foodName ?? "Unknown";
                            items.Add(new MealLogItem
                            {
                                id = d.id,
                                foodProductId = d.foodProductId,
                                genericFoodId = d.genericFoodId,
                                name = name,
                                quantity = d.quantity,
                                unit = d.unit,
                                isProduct = d.itemType == "food_product",
                                isGenericFood = d.itemType == "generic_food",
                            });
                        }
                    }

                    SelectedItems = items;
                    _originalItemsSnapshot = DeepCopyItems(items);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SelectMealPreset failed: {ex.Message}");
                ErrorMessage = "Could not load preset items";
            }
        }

        public void ClearMealPreset()
        {
            SelectedMealPreset = null;
            MealContainerName = "";
            _originalItemsSnapshot = new List<MealLogItem>();
        }

        public async Task SearchPresetsAsync(string query)
        {
            _presetSearchCts?.Cancel();
            _presetSearchCts?.Dispose();
            _presetSearchCts = new CancellationTokenSource();
            CancellationToken ct = _presetSearchCts.Token;

            if (!string.IsNullOrWhiteSpace(query))
            {
                try
                {
                    await Task.Delay(300, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                if (ct.IsCancellationRequested) return;
            }

            IsSearchingPresets = true;

            string trimmed = query?.Trim() ?? "";
            Task<(PaginatedMealResponse, ApiErrorResponse)> mealsTask = _mealService.GetMealsAsync(search: trimmed, limit: 10);
            Task<(PaginatedRecipeResponse, ApiErrorResponse)> recipesTask = _recipeService.GetRecipesAsync(search: trimmed, limit: 10);

            await recipesTask;
            await mealsTask;

            if (ct.IsCancellationRequested) return;

            var (recipesResp, recipesErr) = recipesTask.Result;
            var (mealsResp, mealsErr) = mealsTask.Result;

            var results = new List<Meal>();



            if (mealsErr == null && mealsResp?.data != null)
            {
                foreach (Meal m in mealsResp.data)
                    results.Add(m);
            }

            if (recipesErr == null && recipesResp?.data != null)
            {
                foreach (Recipe r in recipesResp.data)
                {
                    results.Add(new Meal
                    {
                        id = r.id,
                        name = r.title,
                        recipeId = r.id,
                        isRecipe = true,
                    });
                }
            }

            PresetResults = results;
            IsSearchingPresets = false;
        }

        // ========= FMSearchOrCategoryField delegates =========

        public async Task<List<OpenFoodFactsProduct>> SearchFoodsAsync(string query)
        {
            var (response, error) = await _foodProductService.SearchOpenFoodFactsAsync(query, 1, 20);
            if (error != null)
            {
                ErrorDetail = error;
                return new List<OpenFoodFactsProduct>();
            }
            return response?.products != null ? new List<OpenFoodFactsProduct>(response.products) : new List<OpenFoodFactsProduct>();
        }

        public async Task<List<GenericFood>> GetGenericFoodsAsync()
        {
            var (response, error) = await _genericFoodService.SearchGenericFoodsAsync(pageSize: 100);
            if (error != null)
            {
                ErrorDetail = error;
                return new List<GenericFood>();
            }
            return response?.items != null ? new List<GenericFood>(response.items) : new List<GenericFood>();
        }

        public async Task<List<GenericFood>> SearchGenericFoodsAsync(string query)
        {
            var (response, error) = await _genericFoodService.SearchGenericFoodsAsync(query, pageSize: 20);
            if (error != null)
            {
                ErrorDetail = error;
                return new List<GenericFood>();
            }
            return response?.items != null ? new List<GenericFood>(response.items) : new List<GenericFood>();
        }

        public async Task AddProductItem(OpenFoodFactsProduct product, float qty, string unit)
        {
            var (existing, findError) = await _foodProductService.FindByBarcodeAsync(product.barcode, includeOpenFoodFacts: true);
            FoodProduct foodItem;
            if (findError == null && existing != null)
            {
                foodItem = existing;
            }
            else
            {
                var (imported, importError) = await _foodProductService.ImportFromBarcodeAsync(product.barcode);
                if (importError != null)
                {
                    if (importError.statusCode == 400)
                    {
                        var (existingFood, findErr2) = await _foodProductService.FindByBarcodeAsync(product.barcode, includeOpenFoodFacts: true);
                        if (findErr2 == null && existingFood != null)
                        {
                            foodItem = existingFood;
                        }
                        else
                        {
                            ErrorDetail = importError;
                            return;
                        }
                    }
                    else
                    {
                        ErrorDetail = importError;
                        return;
                    }
                }
                else
                {
                    foodItem = imported;
                }
            }

            var newItem = new MealLogItem
            {
                foodProductId = foodItem.id,
                name = product.name ?? foodItem.name,
                quantity = qty,
                unit = unit,
                isProduct = true,
            };
            SelectedItems = new List<MealLogItem>(SelectedItems) { newItem };
        }

        public async Task AddGenericFoodItem(GenericFood food, float qty, string unit)
        {
            if (!Guid.TryParse(food.id, out _))
            {
                ErrorDetail = new ApiErrorResponse
                {
                    statusCode = 400,
                    error = "Generic food not available",
                    message = "This generic food is not available yet",
                };
                return;
            }

            var newItem = new MealLogItem
            {
                genericFoodId = food.id,
                name = food.foodName,
                quantity = qty,
                unit = unit,
                isGenericFood = true,
            };
            SelectedItems = new List<MealLogItem>(SelectedItems) { newItem };
        }

        public async Task<(FoodProduct Result, ApiErrorResponse Error)> ImportByBarcodeAsync(string barcode)
        {
            var (existing, findError) = await _foodProductService.FindByBarcodeAsync(barcode, includeOpenFoodFacts: false);
            if (findError == null && existing != null)
                return (existing, null);

            var (foodItem, importError) = await _foodProductService.ImportFromBarcodeAsync(barcode);
            if (importError != null)
            {
                var (existingFood, findErr2) = await _foodProductService.FindByBarcodeAsync(barcode, includeOpenFoodFacts: true);
                if (findErr2 == null && existingFood != null)
                    return (existingFood, null);
                return (null, importError);
            }
            return (foodItem, null);
        }

        public void RemoveItem(MealLogItem item)
        {
            SelectedItems = new List<MealLogItem>(SelectedItems.Where(i => i != item));
        }

        // ========= Save =========

        private static List<MealLogItem> DeepCopyItems(List<MealLogItem> items)
        {
            return items.Select(i => new MealLogItem
            {
                id = i.id,
                foodProductId = i.foodProductId,
                genericFoodId = i.genericFoodId,
                name = i.name,
                quantity = i.quantity,
                unit = i.unit,
                isProduct = i.isProduct,
                isGenericFood = i.isGenericFood,
            }).ToList();
        }

        public bool HasModifications()
        {
            if (_originalItemsSnapshot.Count != SelectedItems.Count)
                return true;

            foreach (MealLogItem snap in _originalItemsSnapshot)
            {
                MealLogItem current = SelectedItems.FirstOrDefault(i =>
                    (i.isProduct && i.foodProductId == snap.foodProductId) ||
                    (i.isGenericFood && i.genericFoodId == snap.genericFoodId));
                if (current == null)
                    return true;
                if (Math.Abs(current.quantity - snap.quantity) > 0.001f || current.unit != snap.unit)
                    return true;
            }

            return false;
        }

        public async Task<bool> SaveAsync()
        {
            if (SelectedTypeOfMeal == null)
            {
                ErrorMessage = "Select a meal type";
                return false;
            }

            IsSaving = true;
            ErrorMessage = "";
            string timestamp = DateTime.UtcNow.ToString("o");

            try
            {
                string mealId = null;
                bool createdMeal = false;

                bool hasPreset = SelectedMealPreset != null;
                bool hasModifications = hasPreset && HasModifications();
                string trimmedName = MealContainerName?.Trim() ?? "";
                bool isRecipe = SelectedMealPreset?.isRecipe == true;

                if (isRecipe)
                {
                    if (string.IsNullOrWhiteSpace(trimmedName))
                    {
                        ErrorMessage = "Enter a meal name";
                        IsSaving = false;
                        return false;
                    }

                    var (created, err) = await _mealService.CreateMealAsync(new CreateMealRequest
                    {
                        name = trimmedName,
                        recipeId = SelectedMealPreset.recipeId,
                    });
                    if (err != null)
                    {
                        ErrorDetail = err;
                        IsSaving = false;
                        return false;
                    }
                    mealId = created.id;
                    createdMeal = true;
                }
                else if (hasPreset && !hasModifications)
                {
                    mealId = SelectedMealPreset.id;
                }
                else if (hasPreset && hasModifications && trimmedName != SelectedMealPreset.name)
                {
                    if (string.IsNullOrWhiteSpace(trimmedName))
                    {
                        ErrorMessage = "Enter a meal name";
                        IsSaving = false;
                        return false;
                    }

                    var (created, err) = await _mealService.CreateMealAsync(new CreateMealRequest
                    {
                        name = trimmedName,
                    });
                    if (err != null)
                    {
                        ErrorDetail = err;
                        IsSaving = false;
                        return false;
                    }
                    mealId = created.id;
                    createdMeal = true;
                }
                else if (hasPreset && hasModifications)
                {
                    _pendingPresetName = SelectedMealPreset.name;
                    IsSaving = false;
                    OnConfirmUpdateRequired?.Invoke(SelectedMealPreset.name);
                    return false;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(trimmedName))
                    {
                        ErrorMessage = "Enter a meal name";
                        IsSaving = false;
                        return false;
                    }

                    var (created, err) = await _mealService.CreateMealAsync(new CreateMealRequest
                    {
                        name = trimmedName,
                    });
                    if (err != null)
                    {
                        ErrorDetail = err;
                        IsSaving = false;
                        return false;
                    }
                    mealId = created.id;
                    createdMeal = true;
                }

                if (createdMeal)
                {
                    foreach (MealLogItem entry in SelectedItems)
                    {
                        var req = new CreateMealItemRequest
                        {
                            quantity = Math.Max(1, (int)entry.quantity),
                            unit = entry.unit,
                        };

                        if (entry.isProduct && !string.IsNullOrEmpty(entry.foodProductId))
                            req.foodProductId = entry.foodProductId;
                        else if (entry.isGenericFood && !string.IsNullOrEmpty(entry.genericFoodId))
                            req.genericFoodId = entry.genericFoodId;
                        else
                            continue;

                        var (_, itemErr) = await _mealItemService.CreateAsync(mealId, req);
                        if (itemErr != null)
                        {
                            ErrorDetail = itemErr;
                            IsSaving = false;
                            return false;
                        }
                    }
                }

                var logRequest = new CreateMealLogRequest
                {
                    mealId = mealId,
                    typeOfMeal = SelectedTypeOfMeal.code,
                    timestamp = timestamp,
                    mealFromPantry = MealFromPantry,
                    eatenOut = EatenOut
                };

                var (_, logErr) = await _mealLogService.CreateAsync(logRequest);
                if (logErr != null)
                {
                    ErrorDetail = logErr;
                    IsSaving = false;
                    return false;
                }

                ErrorDetail = null;
                SelectedItems = new List<MealLogItem>();
                _originalItemsSnapshot = new List<MealLogItem>();
                ResetToStep1();
                await LoadTodayAsync();
                IsSaving = false;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SaveAsync failed: {ex.Message}");
                ErrorMessage = "Unexpected error saving meal log";
                IsSaving = false;
                return false;
            }
        }

        public async Task<bool> ConfirmUpdateAndSaveAsync()
        {
            if (SelectedMealPreset == null)
                return false;

            IsSaving = true;
            ErrorMessage = "";
            string timestamp = DateTime.UtcNow.ToString("o");

            try
            {
                string mealId = SelectedMealPreset.id;

                var itemsToDelete = _originalItemsSnapshot
                    .Where(snap => !SelectedItems.Any(current =>
                        (current.isProduct && current.foodProductId == snap.foodProductId) ||
                        (current.isGenericFood && current.genericFoodId == snap.genericFoodId)))
                    .ToList();

                foreach (MealLogItem item in itemsToDelete)
                {
                    if (string.IsNullOrEmpty(item.id)) continue;
                    var (_, delErr) = await _mealItemService.DeleteAsync(mealId, item.id);
                    if (delErr != null)
                    {
                        ErrorDetail = delErr;
                        IsSaving = false;
                        return false;
                    }
                }

                foreach (MealLogItem current in SelectedItems)
                {
                    MealLogItem snap = _originalItemsSnapshot.FirstOrDefault(s =>
                        (s.isProduct && s.foodProductId == current.foodProductId) ||
                        (s.isGenericFood && s.genericFoodId == current.genericFoodId));

                    var req = new CreateMealItemRequest
                    {
                        quantity = Math.Max(1, (int)current.quantity),
                        unit = current.unit,
                    };
                    if (current.isProduct && !string.IsNullOrEmpty(current.foodProductId))
                        req.foodProductId = current.foodProductId;
                    else if (current.isGenericFood && !string.IsNullOrEmpty(current.genericFoodId))
                        req.genericFoodId = current.genericFoodId;
                    else
                        continue;

                    if (snap == null)
                    {
                        var (_, createErr) = await _mealItemService.CreateAsync(mealId, req);
                        if (createErr != null)
                        {
                            ErrorDetail = createErr;
                            IsSaving = false;
                            return false;
                        }
                    }
                    else if (Math.Abs(current.quantity - snap.quantity) > 0.001f || current.unit != snap.unit)
                    {
                        if (string.IsNullOrEmpty(snap.id)) continue;
                        var (_, updateErr) = await _mealItemService.UpdateAsync(mealId, snap.id, req);
                        if (updateErr != null)
                        {
                            ErrorDetail = updateErr;
                            IsSaving = false;
                            return false;
                        }
                    }
                }

                var logRequest = new CreateMealLogRequest
                {
                    mealId = mealId,
                    typeOfMeal = SelectedTypeOfMeal.code,
                    timestamp = timestamp,
                    mealFromPantry = MealFromPantry,
                    eatenOut = EatenOut
                };

                var (_, logErr) = await _mealLogService.CreateAsync(logRequest);
                if (logErr != null)
                {
                    ErrorDetail = logErr;
                    IsSaving = false;
                    return false;
                }

                _pendingPresetName = null;
                ErrorDetail = null;
                SelectedItems = new List<MealLogItem>();
                _originalItemsSnapshot = new List<MealLogItem>();
                ResetToStep1();
                await LoadTodayAsync();
                IsSaving = false;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] ConfirmUpdateAndSaveAsync failed: {ex.Message}");
                ErrorMessage = "Unexpected error saving meal log";
                IsSaving = false;
                return false;
            }
        }

        public void CancelUpdate()
        {
            _pendingPresetName = null;
        }

        // ========= Load / Delete =========

        public async Task LoadTodayAsync()
        {
            var (response, error) = await _mealLogService.GetLogsAsync(
                page: 1,
                limit: 50);

            if (error != null)
            {
                ErrorDetail = error;
                return;
            }

            ErrorDetail = null;
            _allLogs = response?.data != null ? new List<MealLog>(response.data) : new List<MealLog>();
            
            LastTenLogs = _allLogs
                .OrderByDescending(l => DateTime.TryParse(l.timestamp, out var t) ? t : DateTime.MinValue)
                .Take(10)
                .ToList();
            
            UpdateCalorieStats();
            TodayLoaded = true;
        }

        public void ResetToStep1()
        {
            MealContainerName = "";
            SelectedMealPreset = null;
            PresetResults = new List<Meal>();
            SelectedTypeOfMealIndex = -1;
            MealFromPantry = false;
            EatenOut = false;
            SelectedItems = new List<MealLogItem>();
            _originalItemsSnapshot = new List<MealLogItem>();
            _pendingPresetName = null;
            ErrorMessage = "";
            CurrentStep = MealLogStep.SelectingTypeOfMeal;
        }

        public void GoBack()
        {
            switch (CurrentStep)
            {
                case MealLogStep.SelectingSource:
                    CurrentStep = MealLogStep.SelectingTypeOfMeal;
                    break;
                case MealLogStep.SelectingDishes:
                    CurrentStep = MealLogStep.SelectingSource;
                    break;
            }
        }

        public async Task DeleteLogAsync(string logId)
        {
            var (success, error) = await _mealLogService.DeleteLogAsync(logId);
            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                _allLogs = _allLogs.FindAll(l => l.id != logId);
                LastTenLogs = _allLogs
                    .OrderByDescending(l => DateTime.Parse(l.timestamp))
                    .Take(10)
                    .ToList();
                UpdateCalorieStats();
            }
        }

        // ========= Helpers =========

        private void UpdateCalorieStats()
        {
            int consumed = 0;
            foreach (var log in _allLogs)
            {
                consumed += (int)(log.meal?.calories ?? 0f);
            }
            int target = 2200;
            int left = Mathf.Max(0, target - consumed);

            CaloriesConsumed = consumed;
            CaloriesLeft = left;
            CaloriesProgress = target > 0 ? (float)consumed / target : 0f;
        }

        public void DisposeSearchCts()
        {
            _presetSearchCts?.Cancel();
            _presetSearchCts?.Dispose();
            _presetSearchCts = null;
        }

        public static (float quantity, string unit) TryParseMeasure(string measure)
        {
            if (string.IsNullOrWhiteSpace(measure))
                return (1f, "PIECES");

            Match match = Regex.Match(measure.Trim(), @"^([\d.]+)\s*(.*)$");
            if (match.Success && float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float qty))
            {
                string unitText = match.Groups[2].Value.Trim().ToLowerInvariant();
                return (qty, TryParseUnit(unitText));
            }

            return (1f, "PIECES");
        }

        private static string TryParseUnit(string unitText)
        {
            if (string.IsNullOrEmpty(unitText)) return "PIECES";

            return unitText switch
            {
                "g" or "gr" or "gram" or "grams" => "G",
                "kg" or "kgs" or "kilogram" or "kilograms" => "KG",
                "ml" or "milliliter" or "milliliters" => "ML",
                "l" or "liter" or "liters" or "litre" or "litres" => "L",
                "cup" or "cups" => "CUPS",
                "piece" or "pieces" or "pcs" or "unit" or "units" => "PIECES",
                _ => "PIECES",
            };
    }
}

}
