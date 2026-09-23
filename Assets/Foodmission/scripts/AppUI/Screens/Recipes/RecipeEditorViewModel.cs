using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;

namespace eu.foodmission.platform
{
    public class RecipeIngredientInput
    {
        public string Name;
        public string Measure;
        public string FoodProductId;
        public string GenericFoodId;
        public float? Quantity;
        public string Unit;
        public bool IsFreeText => string.IsNullOrEmpty(FoodProductId) && string.IsNullOrEmpty(GenericFoodId);
    }

    public partial class RecipeEditorViewModel : StepFlowViewModelBase
    {
        private readonly IRecipeService _recipeService;
        private readonly IFoodProductService _foodProductService;
        private readonly IOpenFoodFactsClientService _openFoodFactsClientService;
        private readonly IGenericFoodService _genericFoodService;

        // Step 1 — Meta
        private string m_Title = "";
        public string Title
        {
            get => m_Title;
            set
            {
                if (SetProperty(ref m_Title, value))
                {
                    InvalidateValidation();
                }
            }
        }
        [ObservableProperty] private string m_Description = "";
        [ObservableProperty] private string m_Instructions = "";
        [ObservableProperty] private string m_Difficulty;
        [ObservableProperty] private string m_Category;
        [ObservableProperty] private string m_CuisineType;
        [ObservableProperty] private string m_ImageUrl;
        [ObservableProperty] private int? m_PrepTime;
        [ObservableProperty] private int? m_CookTime;
        [ObservableProperty] private int? m_Servings;
        [ObservableProperty] private string[] m_Tags = Array.Empty<string>();
        [ObservableProperty] private string[] m_DietaryLabels = Array.Empty<string>();
        [ObservableProperty] private bool m_IsPublic;

        // Step 2 — Ingredients
        [ObservableProperty] private List<RecipeIngredientInput> m_Ingredients = new();
        [ObservableProperty] private bool m_HasNoIngredientsWarning;

        // Step 3 — Common
        [ObservableProperty] private bool m_IsSaving;
        [ObservableProperty] private string m_EditingRecipeId; // null = create mode

        public RecipeEditorViewModel(
            IStoreService storeService,
            IRecipeService recipeService,
            IFoodProductService foodProductService,
            IOpenFoodFactsClientService openFoodFactsClientService,
            IGenericFoodService genericFoodService)
            : base(storeService)
        {
            _recipeService = recipeService;
            _foodProductService = foodProductService;
            _openFoodFactsClientService = openFoodFactsClientService;
            _genericFoodService = genericFoodService;
            StepCount = GetStepCount();
        }

        public bool IsEditMode => !string.IsNullOrEmpty(EditingRecipeId);

        public bool TestValidateStep(int stepIndex, bool showError = true) => ValidateStep(stepIndex, showError);

        protected override int GetStepCount() => 3;

        protected override string GetStepTitle(int stepIndex) => stepIndex switch
        {
            0 => UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_STEP_GENERAL", null, UnityEngine.Localization.Settings.FallbackBehavior.UseProjectSettings, "General"),
            1 => UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_STEP_INGREDIENTS", null, UnityEngine.Localization.Settings.FallbackBehavior.UseProjectSettings, "Ingredients"),
            2 => UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_STEP_INSTRUCTIONS", null, UnityEngine.Localization.Settings.FallbackBehavior.UseProjectSettings, "Instructions"),
            _ => ""
        };

        public void Reset()
        {
            StepCount = GetStepCount();
            EditingRecipeId = null;
            Title = "";
            Description = "";
            Instructions = "";
            Difficulty = null;
            Category = null;
            CuisineType = null;
            ImageUrl = null;
            PrepTime = null;
            CookTime = null;
            Servings = null;
            Tags = Array.Empty<string>();
            DietaryLabels = Array.Empty<string>();
            IsPublic = false;
            Ingredients = new();
            HasNoIngredientsWarning = false;
            ErrorDetail = null;
            IsSaving = false;
            CurrentStepIndex = 0;
            RefreshStepState();
        }

        public override void Initialize()
        {
            base.Initialize();
            if (!IsEditMode)
            {
                Reset();
            }
        }

        protected override Task OnStepEnteredAsync(int stepIndex) => Task.CompletedTask;
        protected override Task OnStepExitingAsync(int stepIndex) => Task.CompletedTask;
        protected override async Task OnFlowCompletedAsync() => await SaveAsync();

        protected override bool ValidateStep(int stepIndex) => ValidateStep(stepIndex, true);

        protected override bool ValidateStep(int stepIndex, bool showError)
        {
            switch (stepIndex)
            {
                case 0: // Meta
                    if (string.IsNullOrWhiteSpace(Title))
                    {
                        if (showError)
                        {
                            ErrorDetail = new ApiErrorResponse { message = "RECIPE_E_ERROR_TITLE_REQUIRED" };
                        }
                        return false;
                    }
                    if (showError) ErrorDetail = null;
                    return true;
                case 1: // Ingredients
                    if (showError)
                    {
                        HasNoIngredientsWarning = Ingredients == null || Ingredients.Count == 0;
                    }
                    return true;
                case 2: // Review
                    return true;
                default:
                    return true;
            }
        }

        public void AddIngredientFromProduct(string foodProductId, string name, string measure = null, float? quantity = null, string unit = null)
        {
            Ingredients.Add(new RecipeIngredientInput
            {
                Name = name,
                Measure = measure,
                FoodProductId = foodProductId,
                Quantity = quantity,
                Unit = unit
            });
            HasNoIngredientsWarning = false;
            OnPropertyChanged(nameof(Ingredients));
        }

        public void AddIngredientFromGenericFood(string genericFoodId, string name, string measure = null, float? quantity = null, string unit = null)
        {
            if (!Guid.TryParse(genericFoodId, out _))
            {
                ErrorDetail = new ApiErrorResponse { message = "GENERIC_FOOD_NOT_AVAILABLE_DESC" };
                return;
            }
            Ingredients.Add(new RecipeIngredientInput
            {
                Name = name,
                Measure = measure,
                GenericFoodId = genericFoodId,
                Quantity = quantity,
                Unit = unit
            });
            HasNoIngredientsWarning = false;
            ErrorDetail = null;
            OnPropertyChanged(nameof(Ingredients));
        }

        public void UpdateIngredient(int index, float quantity, string unit, string measure)
        {
            if (index < 0 || index >= Ingredients.Count) return;
            var item = Ingredients[index];
            item.Quantity = quantity;
            item.Unit = unit;
            item.Measure = measure;
            OnPropertyChanged(nameof(Ingredients));
        }

        public void AddFreeTextIngredient(string name, string measure = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            Ingredients.Add(new RecipeIngredientInput { Name = name, Measure = measure });
            OnPropertyChanged(nameof(Ingredients));
        }

        public void RemoveIngredient(int index)
        {
            if (index < 0 || index >= Ingredients.Count) return;
            Ingredients.RemoveAt(index);
            OnPropertyChanged(nameof(Ingredients));
        }

        // ========= FMSearchOrCategoryField delegates =========

        public async Task<List<OpenFoodFactsProduct>> SearchFoodsAsync(string query)
        {
            var (products, error) = await FoodProductFlow.SearchProductsAsync(_foodProductService, _openFoodFactsClientService, query);
            if (error != null)
            {
                ErrorDetail = error;
                return new List<OpenFoodFactsProduct>();
            }
            return products ?? new List<OpenFoodFactsProduct>();
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

        public Task<(FoodProduct Result, ApiErrorResponse Error)> ImportByBarcodeAsync(string barcode)
        {
            return FoodProductFlow.ImportByBarcodeAsync(_foodProductService, _openFoodFactsClientService, barcode);
        }

        public async Task LoadForEditAsync(string recipeId)
        {
            Reset();
            if (string.IsNullOrEmpty(recipeId)) return;
            EditingRecipeId = recipeId;
            try
            {
                var (recipe, error) = await _recipeService.GetRecipeAsync(recipeId);
                if (error != null) { ErrorDetail = error; return; }
                if (recipe == null) return;
                Title = recipe.title ?? "";
                Description = recipe.description ?? "";
                Instructions = recipe.instructions ?? "";
                Difficulty = recipe.difficulty;
                Category = recipe.category;
                CuisineType = recipe.cuisineType;
                ImageUrl = recipe.imageUrl;
                PrepTime = recipe.prepTime > 0 ? recipe.prepTime : (int?)null;
                CookTime = recipe.cookTime > 0 ? recipe.cookTime : (int?)null;
                Servings = recipe.servings > 0 ? recipe.servings : (int?)null;
                Tags = recipe.tags ?? Array.Empty<string>();
                DietaryLabels = recipe.dietaryLabels ?? Array.Empty<string>();
                IsPublic = recipe.isPublic ?? false;

                Ingredients = recipe.ingredients?.Select(i => new RecipeIngredientInput
                {
                    Name = i.name,
                    Measure = i.measure,
                    FoodProductId = i.foodProductId,
                    GenericFoodId = i.genericFoodId
                }).ToList() ?? new();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RecipeEditorViewModel] LoadForEditAsync: {ex}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
        }

        public async Task SaveAsync()
        {
            Debug.Log($"[RecipeEditorViewModel] SaveAsync started. Title='{Title}', EditMode={IsEditMode}, Ingredients={Ingredients.Count}");
            if (!ValidateStep(0, true))
            {
                Debug.LogWarning("[RecipeEditorViewModel] SaveAsync failed Step 0 validation (Title required)");
                return;
            }

            IsSaving = true;
            ErrorDetail = null;
            try
            {
                var req = new CreateRecipeRequest
                {
                    title = Title?.Trim(),
                    description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    instructions = string.IsNullOrWhiteSpace(Instructions) ? null : Instructions.Trim(),
                    difficulty = string.IsNullOrWhiteSpace(Difficulty) ? null : Difficulty.Trim(),
                    category = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim(),
                    cuisineType = string.IsNullOrWhiteSpace(CuisineType) ? null : CuisineType.Trim(),
                    imageUrl = string.IsNullOrWhiteSpace(ImageUrl) ? null : ImageUrl.Trim(),
                    prepTime = PrepTime,
                    cookTime = CookTime,
                    servings = Servings,
                    tags = Tags?.Length > 0 ? Tags : null,
                    dietaryLabels = DietaryLabels?.Length > 0 ? DietaryLabels : null,
                    isPublic = IsPublic,
                    ingredients = Ingredients.Count > 0
                        ? Ingredients.Select(i => new CreateRecipeIngredientRequest
                        {
                            name = i.Name,
                            measure = string.IsNullOrWhiteSpace(i.Measure) ? null : i.Measure.Trim(),
                            foodProductId = (!string.IsNullOrEmpty(i.FoodProductId) && Guid.TryParse(i.FoodProductId, out _)) ? i.FoodProductId : null,
                            genericFoodId = (!string.IsNullOrEmpty(i.GenericFoodId) && Guid.TryParse(i.GenericFoodId, out _)) ? i.GenericFoodId : null
                        }).ToArray()
                        : null
                };

                if (IsEditMode)
                {
                    var (updated, updateErr) = await _recipeService.UpdateRecipeAsync(EditingRecipeId, req);
                    if (updateErr != null)
                    {
                        Debug.LogError($"[RecipeEditorViewModel] UpdateRecipeAsync error: {updateErr.message}");
                        ErrorDetail = updateErr;
                        return;
                    }
                    Debug.Log($"[RecipeEditorViewModel] Recipe updated successfully: {EditingRecipeId}");
                    RaiseNavigationRequested(Actions.recipes_to_detail,
                        new Argument("recipeId", EditingRecipeId));
                }
                else
                {
                    var (created, createErr) = await _recipeService.CreateRecipeAsync(req);
                    if (createErr != null)
                    {
                        Debug.LogError($"[RecipeEditorViewModel] CreateRecipeAsync error: {createErr.message}");
                        ErrorDetail = createErr;
                        return;
                    }
                    string targetId = created?.id;
                    Debug.Log($"[RecipeEditorViewModel] Recipe created successfully: {targetId}");
                    if (!string.IsNullOrEmpty(targetId))
                    {
                        RaiseNavigationRequested(Actions.recipes_to_detail,
                            new Argument("recipeId", targetId));
                    }
                    else
                    {
                        RaiseNavigationRequested(Actions.go_to_recipes, Array.Empty<Argument>());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RecipeEditorViewModel] SaveAsync: {ex}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
            finally
            {
                IsSaving = false;
            }
        }
    }
}
