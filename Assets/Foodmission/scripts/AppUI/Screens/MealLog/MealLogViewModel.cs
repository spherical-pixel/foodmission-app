using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Localization.Settings;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Navigation;

namespace eu.foodmission.platform
{
    public enum MealLogStep
    {
        SelectingTypeOfMeal,
        SelectingSource,
        SelectingDishes,
        Saving,
        ReviewingFacts
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
        private readonly IOpenFoodFactsClientService _openFoodFactsClientService;
        private readonly IPantryService _pantryService;
        private readonly INotificationService _notificationService;
        private readonly IQuestService _questService;
        private readonly IAuthService _authService;
        private IDisposable _langSubscription;

        [ObservableProperty] private List<MealLog> m_LastTenLogs = new();
        [ObservableProperty] private DateTime m_SelectedDate = DateTime.Today;

        /// <summary>Local day the new meal is logged for (issue #65); <see cref="SelectedDate"/> only filters the list.</summary>
        [ObservableProperty] private DateTime _selectedLogDay = DateTime.Today;

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

        [ObservableProperty] private bool m_SaveAsPreset;

        [ObservableProperty] private Meal m_SelectedMealPreset;


        [ObservableProperty] private List<Meal> m_PresetResults = new();

        [ObservableProperty] private bool m_IsSearching;

        [ObservableProperty] private List<MealLogItem> m_SelectedItems = new();

        [ObservableProperty] private bool m_IsSaving;

        [ObservableProperty] private string m_ErrorMessage = "";

        [ObservableProperty] private ApiErrorResponse m_ErrorDetail;



        [ObservableProperty] private bool m_IsSearchingPresets;

        [ObservableProperty] private bool m_IsEditing;

        [ObservableProperty] private MealLog m_EditingMealLog;

        /// <summary>Flags/swaps shown in the review step (filtered when <see cref="OnlyMissionFacts"/>).</summary>
        [ObservableProperty] private List<QuickMealSection> _factSections = new();

        private List<QuickMealSection> _allFactSections = new();
        private HashSet<string> _missionFactEvents = new(StringComparer.Ordinal);

        // The staged log under review and, when it was created for it, its meal (deleted on discard)
        private MealLog _draftLog;
        private string _draftMealId;

        public bool OnlyMissionFacts { get; private set; }
        public bool HasMissionFacts => _missionFactEvents.Count > 0;

        public MealLogViewModel(
            IStoreService storeService,
            IMealLogService mealLogService,
            IMealService mealService,
            IRecipeService recipeService,
            IFoodProductService foodProductService,
            IGenericFoodService genericFoodService,
            IMealItemService mealItemService,
            ICatalogService catalogService,
            ILocalStorageService localStorage,
            IOpenFoodFactsClientService openFoodFactsClientService,
            IPantryService pantryService = null,
            INotificationService notificationService = null,
            IQuestService questService = null,
            IAuthService authService = null)
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
            _openFoodFactsClientService = openFoodFactsClientService;
            _pantryService = pantryService;
            _notificationService = notificationService;
            _questService = questService;
            _authService = authService;

            _langSubscription = _store.Subscribe(
                state => state.lang,
                OnLanguageChanged
            );

            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        }

        private void OnSelectedLocaleChanged(UnityEngine.Localization.Locale locale)
        {
            string code = locale?.Identifier.Code;
            if (!string.IsNullOrEmpty(code))
            {
                OnLanguageChanged(code);
            }
        }

        private async void OnLanguageChanged(string newLang)
        {
            if (string.IsNullOrEmpty(newLang)) return;
            await InitializeAsync();
            await LoadTodayAsync();
        }

        protected override void OnDispose()
        {
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            _langSubscription?.Dispose();
            _langSubscription = null;
            _ = DiscardDraftAsync();
            base.OnDispose();
        }



        /// <summary>
        /// Called from the FoodInfoOverlay action callback. Checks if FoodInfoOverlay
        /// dispatched an "add to meal log" request and, if so, processes it.
        /// </summary>
        public void CheckPendingFoodInfoAddRequest()
        {
            var state = _storeService.GetAppState();
            if (state.foodInfoAddRequest == null)
                return;

            var request = state.foodInfoAddRequest;
            _store.Dispatch(AppActions.foodInfoAddRequestConsumed.Invoke());

            if (request.EntryContext != "mealLog")
                return;

            // Add the food to the current meal log step 3 (selected items)
            if (request.FoodType == FoodInfoType.Generic)
                _ = SafeAddGenericFoodFromFoodInfoAsync(request);
            else
                _ = SafeAddProductFromFoodInfoAsync(request);
        }

        private async Task SafeAddGenericFoodFromFoodInfoAsync(AddToContextRequestedAction request)
        {
            try
            {
                GenericFood gf = null;
                if (!string.IsNullOrEmpty(request.FoodData))
                    gf = Newtonsoft.Json.JsonConvert.DeserializeObject<GenericFood>(request.FoodData);

                if (gf == null && !string.IsNullOrEmpty(request.FoodId))
                {
                    var (fetched, err) = await _genericFoodService.GetGenericFoodByIdAsync(request.FoodId);
                    if (err == null) gf = fetched;
                }

                if (gf != null)
                    await AddGenericFoodItem(gf, null, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SafeAddGenericFoodFromFoodInfoAsync failed: {ex.Message}");
            }
        }

        private async Task SafeAddProductFromFoodInfoAsync(AddToContextRequestedAction request)
        {
            try
            {
                OpenFoodFactsProduct product = null;
                if (!string.IsNullOrEmpty(request.FoodData))
                    product = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenFoodFactsProduct>(request.FoodData);

                if (product != null && !string.IsNullOrEmpty(product.barcode))
                {
                    await AddProductItem(product, null, null);
                }
                else if (!string.IsNullOrEmpty(request.FoodId))
                {
                    if (Guid.TryParse(request.FoodId, out _))
                    {
                        var (food, foodError) = await _foodProductService.GetFoodByIdAsync(request.FoodId);
                        if (foodError == null && food != null)
                        {
                            await AddProductItem(new OpenFoodFactsProduct
                            {
                                id = food.id,
                                name = food.name,
                                barcode = food.barcode,
                                brands = Array.Empty<string>(),
                            }, null, null);
                        }
                    }
                    else
                    {
                        var (food, foodError) = await ImportByBarcodeAsync(request.FoodId);
                        if (foodError == null && food != null)
                        {
                            await AddProductItem(new OpenFoodFactsProduct
                            {
                                id = food.id,
                                name = food.name,
                                barcode = food.barcode,
                                brands = Array.Empty<string>(),
                            }, null, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SafeAddProductFromFoodInfoAsync failed: {ex.Message}");
            }
        }

        public async Task InitializeAsync()
        {
            IsSearching = true;

            string lang = _storeService.GetAppState().lang ?? "en";
            var (types, typeErr) = await _catalogService.GetTypeOfMealsAsync(lang);
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

        public async Task SelectMealPreset(Meal meal)
        {
            try
            {
                PresetResults = new List<Meal>();

                if (meal.isRecipe)
                {
                    SelectedMealPreset = meal;

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
                        items = itemsDict.Values.ToList();
                    }

                    SelectedItems = items;
                    _originalItemsSnapshot = DeepCopyItems(items);
                    CurrentStep = MealLogStep.SelectingDishes;
                    return;
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
                            string name = d.foodProduct?.name ?? d.genericFood?.foodName ?? "@UI:UNKNOWN";
                            items.Add(new MealLogItem
                            {
                                id = d.id,
                                foodProductId = d.foodProductId,
                                genericFoodId = d.genericFoodId,
                                name = name,
                                quantity = (d.quantity.HasValue && d.quantity.Value > 0) ? (float?)d.quantity.Value : null,
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
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MEAL_LOG_ERROR_LOAD_PRESETS");
            }
        }

        public void ClearMealPreset()
        {
            SelectedMealPreset = null;
            MealContainerName = "";
            _originalItemsSnapshot = new List<MealLogItem>();
        }

        public async Task LoadRecipePresetAsync(string recipeId, int? mealTypeIndex = null, bool eatenOut = false)
        {
            if (string.IsNullOrEmpty(recipeId)) return;
            IsSearchingPresets = true;
            try
            {
                if (mealTypeIndex.HasValue)
                {
                    SelectedTypeOfMealIndex = mealTypeIndex.Value >= 0 ? mealTypeIndex.Value : 0;
                    EatenOut = eatenOut;
                    MealFromPantry = !eatenOut;
                }

                var (recipe, error) = await _recipeService.GetRecipeAsync(recipeId);
                if (error != null) { ErrorDetail = error; return; }
                if (recipe == null) return;

                var constructedMeal = new Meal
                {
                    id = recipe.id,
                    name = recipe.title,
                    recipeId = recipe.id,
                    isRecipe = true
                };

                await SelectMealPreset(constructedMeal);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MealLogViewModel] LoadRecipePresetAsync: {ex}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
            finally { IsSearchingPresets = false; }
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

            try
            {
                string trimmed = query?.Trim() ?? "";
                Task<(PaginatedMealResponse, ApiErrorResponse)> mealsTask = _mealService.GetMealsAsync(search: trimmed, limit: 50);
                Task<(PaginatedRecipeResponse, ApiErrorResponse)> recipesTask = _recipeService.GetRecipesAsync(search: trimmed, limit: 50);

                await recipesTask;
                await mealsTask;

                if (ct.IsCancellationRequested) return;

                var (recipesResp, recipesErr) = recipesTask.Result;
                var (mealsResp, mealsErr) = mealsTask.Result;

                var results = new List<Meal>();

                if (mealsErr == null && mealsResp?.data != null)
                {
                    foreach (Meal m in mealsResp.data)
                    {
                        if (!string.IsNullOrWhiteSpace(m.name))
                        {
                            results.Add(m);
                        }
                    }
                }

                if (recipesErr == null && recipesResp?.data != null)
                {
                    foreach (Recipe r in recipesResp.data)
                    {
                        if (!string.IsNullOrWhiteSpace(r.title))
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
                }

                PresetResults = results;
            }
            finally
            {
                IsSearchingPresets = false;
            }
        }

        // ========= FMSearchOrCategoryField delegates =========

        public async Task<FoodSearchPage> SearchFoodsPageAsync(string query, int page, bool includeGeneric, bool includeProducts)
        {
            var (result, error) = await FoodSearchFlow.SearchAsync(_genericFoodService, _foodProductService, _openFoodFactsClientService,
                query, page, includeGeneric, includeProducts);
            if (error != null)
            {
                ErrorDetail = error;
            }
            return result;
        }

        public async Task<List<FoodGroupItem>> GetFoodGroupsAsync()
        {
            var (result, error) = await _genericFoodService.GetFoodGroupsAsync();
            if (error != null)
            {
                ErrorDetail = error;
                return new List<FoodGroupItem>();
            }
            return result != null ? new List<FoodGroupItem>(result) : new List<FoodGroupItem>();
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

        public async Task AddProductItem(OpenFoodFactsProduct product, float? qty, string unit)
        {
            var (foodItem, importError) = await ImportByBarcodeAsync(product.barcode);
            if (importError != null)
            {
                ErrorDetail = importError;
                return;
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

        public async Task AddGenericFoodItem(GenericFood food, float? qty, string unit)
        {
            if (!Guid.TryParse(food.id, out _))
            {
                ErrorDetail = new ApiErrorResponse
                {
                    statusCode = 400,
                    error = "@UI:GENERIC_FOOD_NOT_AVAILABLE",
                    message = "@UI:GENERIC_FOOD_NOT_AVAILABLE_DESC",
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
            return await FoodProductFlow.ImportByBarcodeAsync(_foodProductService, _openFoodFactsClientService, barcode);
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
                bool quantityChanged = (!current.quantity.HasValue && snap.quantity.HasValue) ||
                                       (current.quantity.HasValue && !snap.quantity.HasValue) ||
                                       (current.quantity.HasValue && snap.quantity.HasValue && Math.Abs(current.quantity.Value - snap.quantity.Value) > 0.001f);
                if (quantityChanged || current.unit != snap.unit)
                    return true;
            }

            return false;
        }


        public async Task<bool> SaveAsync()
        {
            // A second tap while a save is in flight would log the meal (and its flags/swaps) twice
            if (IsSaving)
            {
                return false;
            }

            if (IsEditing)
            {
                return await SaveEditAsync();
            }

            if (TypeOfMealOptions == null || TypeOfMealOptions.Length == 0)
            {
                string lang = _storeService.GetAppState()?.lang ?? "en";
                var (types, typeErr) = await _catalogService.GetTypeOfMealsAsync(lang);
                if (typeErr == null && types != null)
                {
                    TypeOfMealOptions = types;
                }
            }

            if (SelectedTypeOfMealIndex >= 0 && TypeOfMealOptions != null && TypeOfMealOptions.Length > 0)
            {
                if (SelectedTypeOfMealIndex >= TypeOfMealOptions.Length)
                {
                    SelectedTypeOfMealIndex = 0;
                }
            }

            if (SelectedTypeOfMeal == null)
            {
                ErrorMessage = "@UI:SELECT_MEAL_TYPE";
                return false;
            }

            if (SelectedItems == null || SelectedItems.Count == 0)
            {
                ErrorMessage = "@UI:ERROR_NO_ITEMS_SELECTED";
                return false;
            }

            IsSaving = true;
            ErrorMessage = "";
            string timestamp = MealLogDate.BuildTimestamp(SelectedLogDay, SelectedTypeOfMeal?.code, DateTime.Now);

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
                            quantity = entry.quantity.HasValue ? (int?)Math.Max(1, (int)entry.quantity.Value) : null,
                            unit = !string.IsNullOrEmpty(entry.unit) ? entry.unit : null,
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

                return await StageDraftAsync(mealId, createdMeal, timestamp);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SaveAsync failed: {ex.Message}");
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MEAL_LOG_ERROR_SAVE");
                IsSaving = false;
                return false;
            }
        }

        private async Task<bool> DiffAndUpdateMealItemsAsync(string mealId)
        {
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
                    quantity = current.quantity.HasValue ? (int?)Math.Max(1, (int)current.quantity.Value) : null,
                    unit = !string.IsNullOrEmpty(current.unit) ? current.unit : null,
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
                        return false;
                    }
                }
                else
                {
                    bool qtyChanged = (!current.quantity.HasValue && snap.quantity.HasValue) ||
                                      (current.quantity.HasValue && !snap.quantity.HasValue) ||
                                      (current.quantity.HasValue && snap.quantity.HasValue && Math.Abs(current.quantity.Value - snap.quantity.Value) > 0.001f);
                    if (qtyChanged || current.unit != snap.unit)
                    {
                        if (string.IsNullOrEmpty(snap.id)) continue;
                        var (_, updateErr) = await _mealItemService.UpdateAsync(mealId, snap.id, req);
                        if (updateErr != null)
                        {
                            ErrorDetail = updateErr;
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private async Task<bool> SaveEditAsync()
        {
            if (EditingMealLog == null) return false;

            if (TypeOfMealOptions == null || TypeOfMealOptions.Length == 0)
            {
                string lang = _storeService.GetAppState()?.lang ?? "en";
                var (types, typeErr) = await _catalogService.GetTypeOfMealsAsync(lang);
                if (typeErr == null && types != null)
                {
                    TypeOfMealOptions = types;
                }
            }

            if (SelectedTypeOfMealIndex >= 0 && TypeOfMealOptions != null && TypeOfMealOptions.Length > 0)
            {
                if (SelectedTypeOfMealIndex >= TypeOfMealOptions.Length)
                {
                    SelectedTypeOfMealIndex = 0;
                }
            }

            if (SelectedTypeOfMealIndex < 0 || SelectedTypeOfMeal == null)
            {
                ErrorMessage = "@UI:SELECT_MEAL_TYPE";
                return false;
            }

            if (SelectedItems == null || SelectedItems.Count == 0)
            {
                ErrorMessage = "@UI:ERROR_NO_ITEMS_SELECTED";
                return false;
            }

            IsSaving = true;
            ErrorMessage = "";

            try
            {
                string mealId = EditingMealLog.meal?.id ?? EditingMealLog.mealId;
                bool hasPreset = SelectedMealPreset != null;
                bool hasModifications = HasModifications();
                string trimmedName = MealContainerName?.Trim() ?? "";

                // Preset protection: if it was a recipe or preset with a name, and ingredients were modified, and the user hasn't given it a new name:
                if (hasPreset && hasModifications && (SelectedMealPreset.isRecipe || !string.IsNullOrWhiteSpace(SelectedMealPreset.name)) && trimmedName == SelectedMealPreset.name)
                {
                    _pendingPresetName = SelectedMealPreset.name;
                    IsSaving = false;
                    OnConfirmUpdateRequired?.Invoke(SelectedMealPreset.name);
                    return false;
                }

                // If user renamed it (save as a separate meal template for this log)
                if (hasPreset && hasModifications && trimmedName != SelectedMealPreset.name)
                {
                    var (created, err) = await _mealService.CreateMealAsync(new CreateMealRequest
                    {
                        name = trimmedName
                    });
                    if (err != null)
                    {
                        ErrorDetail = err;
                        IsSaving = false;
                        return false;
                    }

                    mealId = created.id;
                    foreach (MealLogItem entry in SelectedItems)
                    {
                        var req = new CreateMealItemRequest
                        {
                            quantity = entry.quantity.HasValue ? (int?)Math.Max(1, (int)entry.quantity.Value) : null,
                            unit = !string.IsNullOrEmpty(entry.unit) ? entry.unit : null,
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
                else
                {
                    // Update existing meal items in place via diffing
                    if (!string.IsNullOrEmpty(mealId))
                    {
                        bool diffOk = await DiffAndUpdateMealItemsAsync(mealId);
                        if (!diffOk)
                        {
                            IsSaving = false;
                            return false;
                        }

                        // Also update meal name if changed
                        if (EditingMealLog.meal != null && trimmedName != (EditingMealLog.meal.name ?? ""))
                        {
                            await _mealService.UpdateMealAsync(mealId, new UpdateMealRequest { name = trimmedName });
                        }
                    }
                }

                // Update the meal log entry itself
                var updateLogRequest = new UpdateMealLogRequest
                {
                    mealId = mealId,
                    typeOfMeal = SelectedTypeOfMeal.code,
                    mealFromPantry = MealFromPantry,
                    eatenOut = EatenOut
                };

                var (updatedLog, logErr) = await _mealLogService.UpdateLogAsync(EditingMealLog.id, updateLogRequest);
                if (logErr != null)
                {
                    ErrorDetail = logErr;
                    IsSaving = false;
                    return false;
                }

                if (MealFromPantry && SelectedItems != null && SelectedItems.Count > 0)
                {
                    var snapshotToDeduct = SelectedItems.ToList();
                    await DeductPantryItemsAsync(snapshotToDeduct);
                }

                _pendingPresetName = null;
                ErrorDetail = null;
                IsEditing = false;
                EditingMealLog = null;
                SelectedItems = new List<MealLogItem>();
                _originalItemsSnapshot = new List<MealLogItem>();
                ResetToStep1();
                await LoadTodayAsync();
                IsSaving = false;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SaveEditAsync failed: {ex.Message}");
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MEAL_LOG_ERROR_UPDATE");
                IsSaving = false;
                return false;
            }
        }

        public async Task<bool> ConfirmUpdateAndSaveAsync()
        {
            if (SelectedMealPreset == null || IsSaving)
            {
                return false;
            }

            IsSaving = true;
            ErrorMessage = "";
            string timestamp = MealLogDate.BuildTimestamp(SelectedLogDay, SelectedTypeOfMeal?.code, DateTime.Now);

            try
            {
                string mealId = SelectedMealPreset.id;

                bool diffOk = await DiffAndUpdateMealItemsAsync(mealId);
                if (!diffOk)
                {
                    IsSaving = false;
                    return false;
                }

                if (IsEditing && EditingMealLog != null)
                {
                    string trimmedName = MealContainerName?.Trim() ?? "";
                    if (EditingMealLog.meal != null && trimmedName != (EditingMealLog.meal.name ?? ""))
                    {
                        await _mealService.UpdateMealAsync(mealId, new UpdateMealRequest { name = trimmedName });
                    }

                    var updateLogRequest = new UpdateMealLogRequest
                    {
                        mealId = mealId,
                        typeOfMeal = SelectedTypeOfMeal.code,
                        mealFromPantry = MealFromPantry,
                        eatenOut = EatenOut
                    };

                    var (_, updateLogErr) = await _mealLogService.UpdateLogAsync(EditingMealLog.id, updateLogRequest);
                    if (updateLogErr != null)
                    {
                        ErrorDetail = updateLogErr;
                        IsSaving = false;
                        return false;
                    }
                }
                else
                {
                    _pendingPresetName = null;
                    return await StageDraftAsync(mealId, false, timestamp);
                }

                if (MealFromPantry && SelectedItems != null && SelectedItems.Count > 0)
                {
                    var snapshotToDeduct = SelectedItems.ToList();
                    await DeductPantryItemsAsync(snapshotToDeduct);
                }

                _pendingPresetName = null;
                ErrorDetail = null;
                IsEditing = false;
                EditingMealLog = null;
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
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MEAL_LOG_ERROR_SAVE");
                IsSaving = false;
                return false;
            }
        }

        private async Task DeductPantryItemsAsync(IReadOnlyList<MealLogItem> loggedItems)
        {
            if (loggedItems == null || loggedItems.Count == 0 || _pantryService == null)
                return;

            try
            {
                var (pantryItems, error) = await _pantryService.GetItemsAsync();
                if (error != null || pantryItems == null || pantryItems.Length == 0)
                {
                    if (error != null)
                        Debug.LogWarning($"[{GetType().Name}] Failed to fetch pantry items for deduction: {error.message}");
                    return;
                }

                foreach (var item in loggedItems)
                {
                    var matches = pantryItems.Where(p =>
                        (item.isProduct && !string.IsNullOrEmpty(item.foodProductId) && p.foodProductId == item.foodProductId) ||
                        (item.isGenericFood && !string.IsNullOrEmpty(item.genericFoodId) && p.genericFoodId == item.genericFoodId)
                    ).OrderBy(p => p.ExpiryDateTime.HasValue ? 0 : 1)
                     .ThenBy(p => p.ExpiryDateTime)
                     .ToList();

                    if (matches.Count == 0) continue;

                    bool sameUnit = item.quantity.HasValue &&
                                     item.quantity.Value > 0 &&
                                     matches.Any(m => string.Equals(m.unit, item.unit, StringComparison.OrdinalIgnoreCase));

                    if (sameUnit)
                    {
                        float needed = item.quantity.Value;
                        foreach (var match in matches)
                        {
                            if (needed <= 0) break;

                            if (!string.Equals(match.unit, item.unit, StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (match.quantity > needed)
                            {
                                float newQty = match.quantity - needed;
                                needed = 0;
                                await _pantryService.UpdateItemAsync(
                                    match.id,
                                    newQty,
                                    match.unit,
                                    match.notes,
                                    match.location,
                                    match.expiryDate
                                );
                            }
                            else
                            {
                                needed -= match.quantity;
                                await _pantryService.DeleteItemAsync(match.id);
                                _notificationService?.CancelPantryReminder(match.id);
                            }
                        }
                    }
                    else
                    {
                        await _pantryService.DeleteItemAsync(matches[0].id);
                        _notificationService?.CancelPantryReminder(matches[0].id);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] Exception during DeductPantryItemsAsync: {ex.Message}");
            }
        }

        public void CancelUpdate()
        {
            _pendingPresetName = null;
        }

        // ========= Load / Delete =========

        private string _currentCacheLang;

        public void FilterLogsForSelectedDate()
        {
            if (_allLogs == null || _allLogs.Count == 0)
            {
                LastTenLogs = new List<MealLog>();
                return;
            }

            var logsForDay = _allLogs
                .Where(l => DateTime.TryParse(l.timestamp, out var t) && t.ToLocalTime().Date == SelectedDate.Date)
                .OrderByDescending(l => DateTime.TryParse(l.timestamp, out var t) ? t : DateTime.MinValue)
                .ToList();

            LastTenLogs = logsForDay;
        }

        public async Task SetSelectedDateAsync(DateTime date)
        {
            SelectedDate = date.Date;
            FilterLogsForSelectedDate();

            DateTime startUtc = SelectedDate.Date.ToUniversalTime();
            DateTime endUtc = SelectedDate.Date.AddDays(1).AddSeconds(-1).ToUniversalTime();

            var (response, error) = await _mealLogService.GetLogsAsync(
                page: 1,
                limit: 50,
                dateFrom: startUtc.ToString("o"),
                dateTo: endUtc.ToString("o"));

            if (error == null && response?.data != null)
            {
                MergeLogs(response.data);
                await FetchMissingDetailsAndTranslateAsync(response.data);
                FilterLogsForSelectedDate();
                SaveToCache();
            }
        }

        private void MergeLogs(IEnumerable<MealLog> newLogs)
        {
            if (newLogs == null) return;
            var existingIds = new HashSet<string>(_allLogs.Select(l => l.id));
            foreach (var log in newLogs)
            {
                if (!string.IsNullOrEmpty(log.id) && !existingIds.Contains(log.id))
                {
                    _allLogs.Add(log);
                    existingIds.Add(log.id);
                }
            }
        }

        private void SaveToCache()
        {
            if (_localStorage != null)
            {
                try
                {
                    string currentLang = _storeService.GetAppState().lang ?? "en";
                    string cacheKey = UserLocalData.MealLogsCacheKey(currentLang);
                    _localStorage.SetValue(cacheKey, _allLogs);
                    UserLocalData.Track(_localStorage, cacheKey);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{GetType().Name}] Failed to save meal logs cache: {ex.Message}");
                }
            }
        }

        public async Task LoadTodayAsync()
        {
            SelectedDate = DateTime.Today;
            string currentLang = _storeService.GetAppState().lang ?? "en";
            string cacheKey = UserLocalData.MealLogsCacheKey(currentLang);

            bool isLanguageChange = _currentCacheLang != currentLang;
            _currentCacheLang = currentLang;

            if (isLanguageChange)
            {
                LastTenLogs = new List<MealLog>();
                _allLogs = new List<MealLog>();
            }

            if (_localStorage != null && _localStorage.HasValue(cacheKey))
            {
                try
                {
                    var cachedLogs = _localStorage.GetValue<List<MealLog>>(cacheKey);
                    if (cachedLogs != null && cachedLogs.Count > 0)
                    {
                        _allLogs = new List<MealLog>(cachedLogs);
                        FilterLogsForSelectedDate();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{GetType().Name}] Failed to read meal logs cache for lang {currentLang}: {ex.Message}");
                }
            }

            DateTime startUtc = SelectedDate.Date.ToUniversalTime();
            DateTime endUtc = SelectedDate.Date.AddDays(1).AddSeconds(-1).ToUniversalTime();

            var (response, error) = await _mealLogService.GetLogsAsync(
                page: 1,
                limit: 50,
                dateFrom: startUtc.ToString("o"),
                dateTo: endUtc.ToString("o"));

            if (error != null)
            {
                if (LastTenLogs == null || LastTenLogs.Count == 0)
                {
                    ErrorDetail = error;
                }
                return;
            }

            ErrorDetail = null;
            _allLogs = response?.data != null ? new List<MealLog>(response.data) : new List<MealLog>();
            await FetchMissingDetailsAndTranslateAsync(_allLogs);
            FilterLogsForSelectedDate();
            SaveToCache();
        }

        private async Task FetchMissingDetailsAndTranslateAsync(IEnumerable<MealLog> logs)
        {
            if (logs == null) return;

            // Reuse cached items from existing _allLogs if available to minimize network calls
            if (_allLogs != null)
            {
                var existingItemsByMealId = new Dictionary<string, MealItemDetail[]>();
                foreach (var l in _allLogs)
                {
                    if (l.meal != null && !string.IsNullOrEmpty(l.meal.id) && l.meal.items != null && l.meal.items.Length > 0)
                    {
                        existingItemsByMealId[l.meal.id] = l.meal.items;
                    }
                }

                foreach (var log in logs)
                {
                    if (log.meal != null && !string.IsNullOrEmpty(log.meal.id) && (log.meal.items == null || log.meal.items.Length == 0))
                    {
                        if (existingItemsByMealId.TryGetValue(log.meal.id, out var cachedItems))
                        {
                            log.meal.items = cachedItems;
                        }
                    }
                }
            }

            if (_mealItemService != null)
            {
                var missingItemMeals = logs
                    .Where(log => log.meal != null && !string.IsNullOrEmpty(log.meal.id) && (log.meal.items == null || log.meal.items.Length == 0))
                    .ToList();

                if (missingItemMeals.Count > 0)
                {
                    var fetchTasks = missingItemMeals.Select(async log =>
                    {
                        var (details, _) = await _mealItemService.GetByMealIdAsync(log.meal.id);
                        if (details != null && details.Length > 0)
                        {
                            log.meal.items = details;
                        }
                    });

                    await Task.WhenAll(fetchTasks);
                }
            }

            if (_genericFoodService != null)
            {
                var translationTasks = new List<Task>();
                foreach (var log in logs)
                {
                    if (log.meal?.items != null)
                    {
                        foreach (var item in log.meal.items)
                        {
                            if (!string.IsNullOrEmpty(item.genericFoodId))
                            {
                                translationTasks.Add(TranslateMealItemAsync(item));
                            }
                        }
                    }
                }

                if (translationTasks.Count > 0)
                {
                    await Task.WhenAll(translationTasks);
                }
            }
        }

        private async Task TranslateMealItemAsync(MealItemDetail item)
        {
            if (item == null || string.IsNullOrEmpty(item.genericFoodId) || _genericFoodService == null)
                return;

            var (gf, _) = await _genericFoodService.GetGenericFoodByIdAsync(item.genericFoodId);
            if (gf != null && !string.IsNullOrEmpty(gf.foodName))
            {
                if (item.genericFood == null)
                    item.genericFood = new MealItemGenericFood { id = gf.id };
                item.genericFood.foodName = gf.foodName;
            }
        }

        /// <summary>Kept after a save so several meals of the same past day can be logged in a row; edits keep their day.</summary>
        public void SetLogDay(DateTime day)
        {
            if (IsEditing)
            {
                return;
            }
            SelectedLogDay = day.Date;
        }

        public void ResetToStep1()
        {
            _ = DiscardDraftAsync();
            MealContainerName = "";
            SaveAsPreset = false;
            SelectedMealPreset = null;
            PresetResults = new List<Meal>();
            SelectedTypeOfMealIndex = -1;
            MealFromPantry = false;
            EatenOut = false;
            SelectedItems = new List<MealLogItem>();
            _originalItemsSnapshot = new List<MealLogItem>();
            _pendingPresetName = null;
            ErrorMessage = "";
            IsEditing = false;
            EditingMealLog = null;
            ClearFacts();
            CurrentStep = MealLogStep.SelectingTypeOfMeal;
        }

        public void LoadForEdit(MealLog log)
        {
            if (log == null) return;

            EditingMealLog = log;
            IsEditing = true;

            if (TypeOfMealOptions != null && TypeOfMealOptions.Length > 0 && !string.IsNullOrEmpty(log.typeOfMeal))
            {
                int idx = Array.FindIndex(TypeOfMealOptions, o => o.code == log.typeOfMeal);
                SelectedTypeOfMealIndex = idx >= 0 ? idx : 0;
            }
            else
            {
                SelectedTypeOfMealIndex = 0;
            }

            MealFromPantry = log.mealFromPantry;
            EatenOut = log.eatenOut;

            MealContainerName = log.meal?.name ?? "";
            SelectedMealPreset = log.meal;
            SaveAsPreset = false;

            var items = new List<MealLogItem>();
            if (log.meal?.items != null)
            {
                foreach (MealItemDetail d in log.meal.items)
                {
                    string name = d.foodProduct?.name ?? d.genericFood?.foodName ?? d.notes;
                    if (string.IsNullOrEmpty(name)) name = "@UI:UNKNOWN";

                    items.Add(new MealLogItem
                    {
                        id = d.id,
                        foodProductId = d.foodProductId,
                        genericFoodId = d.genericFoodId,
                        name = name,
                        quantity = d.quantity,
                        unit = d.unit,
                        isProduct = d.itemType == "food_product" || !string.IsNullOrEmpty(d.foodProductId),
                        isGenericFood = d.itemType == "generic_food" || !string.IsNullOrEmpty(d.genericFoodId)
                    });
                }
            }

            SelectedItems = items;
            _originalItemsSnapshot = DeepCopyItems(items);

            CurrentStep = MealLogStep.SelectingDishes;
        }

        public async Task LoadForEditAsync(MealLog log)
        {
            LoadForEdit(log);
            if (log?.meal != null && !string.IsNullOrEmpty(log.meal.id) && (log.meal.items == null || log.meal.items.Length == 0))
            {
                var (details, err) = await _mealItemService.GetByMealIdAsync(log.meal.id);
                if (err == null && details != null)
                {
                    log.meal.items = details;
                    var items = new List<MealLogItem>();
                    foreach (MealItemDetail d in details)
                    {
                        string name = d.foodProduct?.name ?? d.genericFood?.foodName ?? d.notes ?? "@UI:UNKNOWN";
                        items.Add(new MealLogItem
                        {
                            id = d.id,
                            foodProductId = d.foodProductId,
                            genericFoodId = d.genericFoodId,
                            name = name,
                            quantity = d.quantity,
                            unit = d.unit,
                            isProduct = d.itemType == "food_product" || !string.IsNullOrEmpty(d.foodProductId),
                            isGenericFood = d.itemType == "generic_food" || !string.IsNullOrEmpty(d.genericFoodId)
                        });
                    }
                    SelectedItems = items;
                    _originalItemsSnapshot = DeepCopyItems(items);
                }
            }
        }

        public void CancelEdit()
        {
            ResetToStep1();
        }

        public void InitializeForQuickAdd(int mealTypeIndex, bool eatenOut, FoodInfoType foodType, string foodId, string foodName)
        {
            ResetToStep1();

            SelectedTypeOfMealIndex = mealTypeIndex >= 0 ? mealTypeIndex : 0;

            EatenOut = eatenOut;
            MealFromPantry = !eatenOut;

            var newItem = new MealLogItem
            {
                isProduct = foodType == FoodInfoType.Product,
                isGenericFood = foodType == FoodInfoType.Generic,
                foodProductId = foodType == FoodInfoType.Product ? foodId : null,
                genericFoodId = foodType == FoodInfoType.Generic ? foodId : null,
                name = foodName ?? "Alimento",
                quantity = 1f,
                unit = UnitCodes.Pieces
            };

            SelectedItems = new List<MealLogItem> { newItem };
            CurrentStep = MealLogStep.SelectingDishes;
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
                case MealLogStep.ReviewingFacts:
                    CurrentStep = MealLogStep.SelectingDishes;
                    _ = DiscardDraftAsync();
                    break;
            }
        }

        // ── Review step: flags and swaps ─────────────────────

        /// <summary>
        /// Stores the log as a DRAFT so the backend proposes the flags implied by the meal's items, then
        /// opens the review step with those proposals marked. Nothing counts until ConfirmLogAsync.
        /// </summary>
        private async Task<bool> StageDraftAsync(string mealId, bool createdMeal, string timestamp)
        {
            var (draft, logErr) = await _mealLogService.CreateAsync(new CreateMealLogRequest
            {
                mealId = mealId,
                typeOfMeal = SelectedTypeOfMeal.code,
                timestamp = timestamp,
                mealFromPantry = MealFromPantry,
                eatenOut = EatenOut,
                draft = true
            });
            if (logErr != null || draft == null)
            {
                ErrorDetail = logErr;
                if (createdMeal)
                {
                    await DeleteMealQuietlyAsync(mealId);
                }
                IsSaving = false;
                return false;
            }

            _draftLog = draft;
            _draftMealId = createdMeal ? mealId : null;
            ErrorDetail = null;
            IsSaving = false;
            await OpenReviewAsync(draft);
            return true;
        }

        private async Task OpenReviewAsync(MealLog draft)
        {
            ErrorMessage = "";
            ClearFacts();
            _allFactSections = MealFacts.BuildStandardSections();
            MealFacts.ApplySelections(MealFacts.AllItems(_allFactSections).ToList(), draft?.flags, draft?.swaps);
            FactSections = _allFactSections;
            CurrentStep = MealLogStep.ReviewingFacts;

            _missionFactEvents = await LoadMissionFactEventsAsync();
            ApplyFactFilter();
        }

        /// <summary>
        /// Submits the draft with the flags/swaps as reviewed. The lists replace the draft's, so proposals
        /// the user unmarked are dropped. On an error the user stays on the review step with the marks.
        /// </summary>
        public async Task<bool> ConfirmLogAsync()
        {
            if (IsSaving || _draftLog == null)
            {
                return false;
            }

            IsSaving = true;
            ErrorMessage = "";

            try
            {
                var (flags, swaps) = MealFacts.Build(MealFacts.AllItems(_allFactSections));
                var (_, err) = await _mealLogService.ConfirmAsync(_draftLog.id, new MealLogFactsRequest
                {
                    flags = flags,
                    swaps = swaps
                });
                if (err != null)
                {
                    ErrorDetail = err;
                    IsSaving = false;
                    return false;
                }

                _draftLog = null;
                _draftMealId = null;
                DateTime loggedDay = SelectedLogDay;
                await RefreshGamificationAsync();

                if (MealFromPantry && SelectedItems != null && SelectedItems.Count > 0)
                {
                    var snapshotToDeduct = SelectedItems.ToList();
                    await DeductPantryItemsAsync(snapshotToDeduct);
                }

                ErrorDetail = null;
                SelectedItems = new List<MealLogItem>();
                _originalItemsSnapshot = new List<MealLogItem>();
                ResetToStep1();
                await LoadTodayAsync();
                // Show the list of the day the meal went to, so a past-day log is visible right away
                if (!MealLogDate.IsToday(loggedDay, DateTime.Now))
                {
                    await SetSelectedDateAsync(loggedDay);
                }
                IsSaving = false;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] ConfirmLogAsync failed: {ex.Message}");
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MEAL_LOG_ERROR_SAVE");
                IsSaving = false;
                return false;
            }
        }

        /// <summary>
        /// Drops the pending draft (leaving the review step): the draft log and, when it was created for
        /// this log, its meal. Best-effort; the backend also removes drafts left for 7 days.
        /// </summary>
        private async Task DiscardDraftAsync()
        {
            MealLog draft = _draftLog;
            string mealId = _draftMealId;
            _draftLog = null;
            _draftMealId = null;
            if (draft == null)
            {
                return;
            }

            try
            {
                var (_, err) = await _mealLogService.DeleteLogAsync(draft.id);
                if (err != null)
                {
                    Debug.LogWarning($"[{GetType().Name}] Could not delete draft meal log {draft.id}: {err.message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] DiscardDraftAsync failed: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(mealId))
            {
                await DeleteMealQuietlyAsync(mealId);
            }
        }

        private async Task DeleteMealQuietlyAsync(string mealId)
        {
            try
            {
                var (_, err) = await _mealService.DeleteMealAsync(mealId);
                if (err != null)
                {
                    Debug.LogWarning($"[{GetType().Name}] Could not delete meal {mealId}: {err.message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] DeleteMealQuietlyAsync failed: {ex.Message}");
            }
        }

        private async Task<HashSet<string>> LoadMissionFactEventsAsync()
        {
            string questId = _storeService?.GetAppState()?.userCurrentQuestId;
            if (string.IsNullOrEmpty(questId) || _questService == null)
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            try
            {
                var (quest, error) = await _questService.GetQuestAsync(questId);
                if (error != null || quest == null)
                {
                    Debug.LogWarning($"[{GetType().Name}] Could not load the current quest for the review filter: {error?.message}");
                    return new HashSet<string>(StringComparer.Ordinal);
                }
                return MealFacts.MissionMealEvents((quest.items ?? Array.Empty<QuestItem>())
                    .Where(i => i != null && string.Equals(i.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase))
                    .Select(i => i.contentCode));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] LoadMissionFactEventsAsync failed: {ex.Message}");
                return new HashSet<string>(StringComparer.Ordinal);
            }
        }

        public void SetOnlyMissionFacts(bool value)
        {
            OnlyMissionFacts = value;
            ApplyFactFilter();
        }

        private void ApplyFactFilter()
        {
            FactSections = OnlyMissionFacts ? MealFacts.FilterByMissionEvents(_allFactSections, _missionFactEvents) : _allFactSections;
            OnPropertyChanged(nameof(HasMissionFacts));
        }

        public void ToggleFactSection(string sectionId)
        {
            QuickMealSection section = FactSections?.FirstOrDefault(s => s.Id == sectionId);
            if (section == null)
            {
                return;
            }
            section.IsExpanded = !section.IsExpanded;
            FactSections = new List<QuickMealSection>(FactSections);
        }

        public void ToggleFact(string id)
        {
            var all = MealFacts.AllItems(_allFactSections).ToList();
            QuickMealCheckItem target = all.FirstOrDefault(i => i.Id == id);
            if (target == null)
            {
                return;
            }
            MealFacts.Toggle(target, all);
            FactSections = new List<QuickMealSection>(FactSections);
        }

        public void SelectFactSwap(string id, string swapOption)
        {
            QuickMealCheckItem target = MealFacts.AllItems(_allFactSections).FirstOrDefault(i => i.Id == id);
            if (target == null)
            {
                return;
            }
            MealFacts.SelectSwap(target, swapOption);
            FactSections = new List<QuickMealSection>(FactSections);
        }

        private void ClearFacts()
        {
            _allFactSections = new List<QuickMealSection>();
            _missionFactEvents = new HashSet<string>(StringComparer.Ordinal);
            OnlyMissionFacts = false;
            FactSections = _allFactSections;
        }

        private async Task RefreshGamificationAsync()
        {
            if (_authService == null)
            {
                return;
            }
            try
            {
                await _authService.GetGamificationProfileAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] Failed to sync gamification after meal log: {ex.Message}");
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
                _allLogs.RemoveAll(l => l.id == logId);
                FilterLogsForSelectedDate();
                SaveToCache();
            }
        }



        public void DisposeSearchCts()
        {
            _presetSearchCts?.Cancel();
            _presetSearchCts?.Dispose();
            _presetSearchCts = null;
        }

        public static (float quantity, string unit) TryParseMeasure(string measure)
        {
            UnitCatalog.Current.TryParseMeasure(measure, out float quantity, out string unit);
            return (quantity, unit);
        }

        public static MealLog PendingQuickMealEditPayload { get; set; }

        public void NavigateToQuickMealLog()
        {
            RaiseNavigationRequested(Actions.open_quick_meal_log);
        }

        public void NavigateToQuickMealLogEdit(MealLog log)
        {
            if (log == null) return;
            PendingQuickMealEditPayload = log;
            RaiseNavigationRequested(
                Actions.open_quick_meal_log,
                new[]
                {
                    new Argument("mealLogId", log.id),
                    new Argument("mode", "edit")
                });
        }
    }
}
