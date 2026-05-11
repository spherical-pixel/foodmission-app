using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

using Unity.AppUI.MVVM;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class MealLogAddViewModel : ViewModelBase
    {
        private readonly IMealLogService _mealLogService;
        private readonly IMealService _mealService;
        private readonly IPantryService _pantryService;

        private Meal _selectedMeal;

        private static readonly List<string> TypeOfMealValues = new()
        {
            "BREAKFAST", "LUNCH", "DINNER", "SNACK", "DRINKS", "OTHER"
        };

        public static List<string> TypeOfMealOptions => new()
        {
            LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TYPE_BREAKFAST"),
            LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TYPE_LUNCH"),
            LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TYPE_DINNER"),
            LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TYPE_SNACK"),
            LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TYPE_DRINKS"),
            LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TYPE_OTHER"),
        };

        [ObservableProperty]
        private List<Meal> m_MealSearchResults = new();

        [ObservableProperty]
        private bool m_IsSearchingMeals;

        [ObservableProperty]
        private string m_MealSearchQuery = "";

        [ObservableProperty]
        private int m_SelectedTypeOfMealIndex = 0;

        public string SelectedTypeOfMeal => m_SelectedTypeOfMealIndex >= 0 && m_SelectedTypeOfMealIndex < TypeOfMealValues.Count
            ? TypeOfMealValues[m_SelectedTypeOfMealIndex]
            : null;

        [ObservableProperty]
        private bool m_MealFromPantry;

        [ObservableProperty]
        private bool m_EatenOut;

        [ObservableProperty]
        private string m_SelectedMealName = "";

        [ObservableProperty]
        private bool m_HasSelectedMeal;

        [ObservableProperty]
        private List<PantryDeduction> m_PantryDeductions = new();

        [ObservableProperty]
        private bool m_IsSaving;

        [ObservableProperty]
        private string m_ErrorMessage = "";

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

        public MealLogAddViewModel(
            IStoreService storeService,
            IMealLogService mealLogService,
            IMealService mealService,
            IPantryService pantryService)
            : base(storeService)
        {
            _mealLogService = mealLogService;
            _mealService = mealService;
            _pantryService = pantryService;
        }

        public async Task SearchMealsAsync(string query)
        {
            MealSearchQuery = query;
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(query))
            {
                MealSearchResults = new List<Meal>();
                return;
            }

            IsSearchingMeals = true;
            var (response, error) = await _mealService.GetMealsAsync(search: query.Trim(), limit: 20);
            IsSearchingMeals = false;

            if (error != null)
            {
                ErrorDetail = error;
                MealSearchResults = new List<Meal>();
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_SEARCH_MEALS");
            }
            else
            {
                ErrorDetail = null;
                MealSearchResults = new List<Meal>(response.data);
            }
        }

        public async Task<bool> SaveAsync()
        {
            if (_selectedMeal == null)
            {
                if (!string.IsNullOrWhiteSpace(MealSearchQuery))
                {
                    bool autoCreated = await CreateAndSelectMealAsync(MealSearchQuery);
                    if (!autoCreated) return false;
                }
                else
                {
                    ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SELECT_MEAL_FIRST");
                    return false;
                }
            }

            IsSaving = true;
            ErrorMessage = "";

            CreateMealLogRequest request = new()
            {
                mealId = _selectedMeal.id,
                typeOfMeal = SelectedTypeOfMeal,
                eatenOut = EatenOut
            };

            var (created, error) = await _mealLogService.CreateAsync(request);
            IsSaving = false;

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_LOG_MEAL");
                return false;
            }

            ErrorDetail = null;

            if (MealFromPantry)
            {
                await ApplyPantryDeductionsAsync();
            }

            return true;
        }

        private async Task ApplyPantryDeductionsAsync()
        {
            try
            {
                List<string> batchDelete = new();

                foreach (PantryDeduction d in PantryDeductions)
                {
                    if (d.Quantity <= 0) continue;

                    float remaining = d.AvailableQuantity - d.Quantity;
                    if (remaining <= 0)
                    {
                        batchDelete.Add(d.PantryItemId);
                    }
                    else
                    {
                        await _pantryService.UpdateItemAsync(d.PantryItemId, remaining, d.Unit, null, null, null, d.FoodId, d.FoodCategoryId);
                    }
                }

                if (batchDelete.Count > 0)
                {
                    var batchRequest = new BatchWasteRequest
                    {
                        items = batchDelete.ConvertAll(id => new BatchWasteItemRequest
                        {
                            pantryItemId = id
                        }).ToArray()
                    };
                    await _pantryService.BatchWasteAsync(batchRequest);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] ApplyPantryDeductionsAsync failed: {ex.Message}");
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MEAL_PANTRY_UPDATE_ERROR");
            }
        }

        public async Task LoadPantryForDeductionAsync()
        {
            var (pantry, error) = await _pantryService.GetPantryAsync();
            if (error != null)
            {
                ErrorDetail = error;
                PantryDeductions = new List<PantryDeduction>();
                return;
            }

            PantryDeductions = (pantry?.items ?? Array.Empty<PantryItem>())
                .Select(item => new PantryDeduction
                {
                    PantryItemId = item.id,
                    FoodId = item.foodId,
                    FoodCategoryId = item.foodCategoryId,
                    FoodName = item.foodId ?? item.foodCategoryId ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN"),
                    AvailableQuantity = item.quantity,
                    Unit = item.unit,
                    Quantity = 0
                }).ToList();
        }

        public void SelectMeal(Meal meal)
        {
            _selectedMeal = meal;
            SelectedMealName = meal?.name ?? "";
            HasSelectedMeal = meal != null;
        }

        public async Task<bool> CreateAndSelectMealAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            var (created, error) = await _mealService.CreateMealAsync(new CreateMealRequest { name = name.Trim() });

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_CREATE_MEAL");
                return false;
            }

            ErrorDetail = null;
            SelectMeal(created);
            return true;
        }

        public void Reset()
        {
            _selectedMeal = null;
            SelectedMealName = "";
            HasSelectedMeal = false;
            MealSearchQuery = "";
            MealSearchResults = new List<Meal>();
            SelectedTypeOfMealIndex = 0;
            MealFromPantry = false;
            EatenOut = false;
            PantryDeductions = new List<PantryDeduction>();
            ErrorMessage = "";
        }
    }
}
