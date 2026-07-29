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
        public bool IsFreeText => string.IsNullOrEmpty(FoodProductId) && string.IsNullOrEmpty(GenericFoodId);
    }

    [ObservableObject]
    public partial class RecipeEditorViewModel : StepFlowViewModelBase
    {
        private readonly IRecipeService _recipeService;

        // Step 1 — Meta
        [ObservableProperty] private string m_Title = "";
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
        [ObservableProperty] private ApiErrorResponse m_ErrorDetail;
        [ObservableProperty] private string m_EditingRecipeId; // null = create mode

        public RecipeEditorViewModel(IStoreService storeService, IRecipeService recipeService)
            : base(storeService)
        {
            _recipeService = recipeService;
        }

        public bool IsEditMode => !string.IsNullOrEmpty(EditingRecipeId);

        public bool TestValidateStep(int stepIndex) => ValidateStep(stepIndex);

        protected override int GetStepCount() => 3;

        protected override string GetStepTitle(int stepIndex) => stepIndex switch
        {
            0 => "General Info",
            1 => "Ingredients",
            2 => "Review",
            _ => ""
        };

        protected override Task OnStepEnteredAsync(int stepIndex) => Task.CompletedTask;
        protected override Task OnStepExitingAsync(int stepIndex) => Task.CompletedTask;
        protected override async Task OnFlowCompletedAsync() => await SaveAsync();

        protected override bool ValidateStep(int stepIndex)
        {
            switch (stepIndex)
            {
                case 0: // Meta
                    if (string.IsNullOrWhiteSpace(Title))
                    {
                        ErrorDetail = new ApiErrorResponse { message = "RECIPE_E_ERROR_TITLE_REQUIRED" };
                        return false;
                    }
                    ErrorDetail = null;
                    return true;
                case 1: // Ingredients
                    HasNoIngredientsWarning = Ingredients.Count == 0;
                    return true;
                case 2: // Review
                    return true;
                default:
                    return true;
            }
        }

        public void AddIngredientFromProduct(string foodProductId, string name, string measure = null)
        {
            Ingredients.Add(new RecipeIngredientInput
            {
                Name = name,
                Measure = measure,
                FoodProductId = foodProductId
            });
            OnPropertyChanged(nameof(Ingredients));
        }

        public void AddIngredientFromGenericFood(string genericFoodId, string name, string measure = null)
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
                GenericFoodId = genericFoodId
            });
            ErrorDetail = null;
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

        public async Task LoadForEditAsync(string recipeId)
        {
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
                IsPublic = false;

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
            if (!ValidateStep(0)) return;
            IsSaving = true;
            ErrorDetail = null;
            try
            {
                var req = new CreateRecipeRequest
                {
                    title = Title,
                    description = string.IsNullOrEmpty(Description) ? null : Description,
                    instructions = string.IsNullOrEmpty(Instructions) ? null : Instructions,
                    difficulty = string.IsNullOrEmpty(Difficulty) ? null : Difficulty,
                    category = string.IsNullOrEmpty(Category) ? null : Category,
                    cuisineType = string.IsNullOrEmpty(CuisineType) ? null : CuisineType,
                    imageUrl = string.IsNullOrEmpty(ImageUrl) ? null : ImageUrl,
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
                            measure = string.IsNullOrEmpty(i.Measure) ? null : i.Measure,
                            foodProductId = string.IsNullOrEmpty(i.FoodProductId) ? null : i.FoodProductId,
                            genericFoodId = string.IsNullOrEmpty(i.GenericFoodId) ? null : i.GenericFoodId
                        }).ToArray()
                        : null
                };

                if (IsEditMode)
                {
                    var (updated, updateErr) = await _recipeService.UpdateRecipeAsync(EditingRecipeId, req);
                    if (updateErr != null) { ErrorDetail = updateErr; return; }
                    RaiseNavigationRequested(Actions.recipes_to_detail,
                        new Argument("recipeId", EditingRecipeId));
                }
                else
                {
                    var (created, createErr) = await _recipeService.CreateRecipeAsync(req);
                    if (createErr != null) { ErrorDetail = createErr; return; }
                    RaiseNavigationRequested(Actions.recipes_to_detail,
                        new Argument("recipeId", created?.id));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RecipeEditorViewModel] SaveAsync: {ex}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
            finally { IsSaving = false; }
        }
    }
}
