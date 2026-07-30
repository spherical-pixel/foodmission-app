using System;
using System.Threading.Tasks;
using System.Linq;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class RecipeDetailViewModel : ViewModelBase
    {
        private readonly IRecipeService _recipeService;
        private readonly IShoppingListService _shoppingListService;
        private readonly ICatalogService _catalogService;

        [ObservableProperty] private Recipe m_Recipe;
        [ObservableProperty] private bool m_IsLoading;
        [ObservableProperty] private ApiErrorResponse m_ErrorDetail;
        [ObservableProperty] private bool m_IsOwner;
        [ObservableProperty] private bool m_IsAddingToShoppingList;
        [ObservableProperty] private bool m_HasNutritionInfo;
        [ObservableProperty] private bool m_HasVideo;

        public RecipeDetailViewModel(
            IStoreService storeService,
            IRecipeService recipeService,
            IShoppingListService shoppingListService,
            ICatalogService catalogService) : base(storeService)
        {
            _recipeService = recipeService;
            _shoppingListService = shoppingListService;
            _catalogService = catalogService;
        }

        public async Task<CatalogItem[]> GetMealTypesAsync()
        {
            string lang = _storeService.GetAppState().lang ?? "en";
            var (types, error) = await _catalogService.GetTypeOfMealsAsync(lang);
            if (error != null)
            {
                ErrorDetail = error;
                return Array.Empty<CatalogItem>();
            }
            return types ?? Array.Empty<CatalogItem>();
        }

        public async Task LoadAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            IsLoading = true;
            ErrorDetail = null;
            try
            {
                var (recipe, error) = await _recipeService.GetRecipeAsync(id);
                if (error != null) { ErrorDetail = error; return; }
                Recipe = recipe;
                var state = _storeService.GetAppState();
                IsOwner = !string.IsNullOrEmpty(recipe?.userId) && recipe.userId == state.userId;
                HasNutritionInfo = recipe?.nutritionalInfo != null;
                HasVideo = !string.IsNullOrEmpty(recipe?.videoUrl);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RecipeDetailViewModel] LoadAsync: {ex}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
            finally { IsLoading = false; }
        }

        public void LogRecipe(int mealTypeIndex = 0, bool eatenOut = false)
        {
            if (Recipe == null) return;
            RaiseNavigationRequested(Actions.go_to_meallog,
                new Argument("recipeId", Recipe.id),
                new Argument("mealTypeIndex", mealTypeIndex.ToString()),
                new Argument("eatenOut", eatenOut ? "true" : "false"));
        }

        public void Edit()
        {
            if (Recipe == null) return;
            RaiseNavigationRequested(Actions.recipes_to_editor,
                new Argument("recipeId", Recipe.id),
                new Argument("editMode", "true"));
        }

        public async Task DeleteAsync()
        {
            if (Recipe == null) return;
            var (success, error) = await _recipeService.DeleteRecipeAsync(Recipe.id);
            if (error != null) { ErrorDetail = error; return; }
            if (success) RaiseNavigationRequested(Actions.go_to_recipes);
        }

        public async Task<bool> AddIngredientsToShoppingListAsync(string listId = null)
        {
            if (Recipe?.ingredients == null || Recipe.ingredients.Length == 0) return false;
            IsAddingToShoppingList = true;
            ErrorDetail = null;
            try
            {
                var (targetListId, resolveError) = await ResolveTargetShoppingListIdAsync(listId);
                if (resolveError != null || string.IsNullOrEmpty(targetListId))
                {
                    ErrorDetail = resolveError ?? new ApiErrorResponse { message = "Could not resolve shopping list" };
                    return false;
                }

                var validIngredients = Recipe.ingredients
                    .Where(ing => !string.IsNullOrEmpty(ing.foodProductId) || !string.IsNullOrEmpty(ing.genericFoodId))
                    .ToArray();

                if (validIngredients.Length == 0) return false;

                var (success, error) = await TryAddIngredientsAsync(targetListId, validIngredients);

                // If target list was deleted (404), clear stale state, create a new default list and retry once
                if (!success && error != null && (error.statusCode == 404 || (error.message != null && error.message.ToLowerInvariant().Contains("not found"))))
                {
                    Debug.LogWarning($"[RecipeDetailViewModel] Target list '{targetListId}' returned 404. Creating new default list and retrying.");
                    string defaultName = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DEFAULT_SHOPPING_LIST_NAME");
                    if (string.IsNullOrEmpty(defaultName) || defaultName.StartsWith("No translation found"))
                    {
                        defaultName = "Mi Lista de la Compra";
                    }

                    var (newList, createErr) = await _shoppingListService.CreateListAsync(defaultName);
                    if (createErr != null || newList == null)
                    {
                        ErrorDetail = createErr;
                        return false;
                    }

                    (success, error) = await TryAddIngredientsAsync(newList.id, validIngredients);
                }

                if (error != null)
                {
                    ErrorDetail = error;
                    return false;
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RecipeDetailViewModel] AddIngredientsToShoppingListAsync: {ex}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
                return false;
            }
            finally { IsAddingToShoppingList = false; }
        }

        private async Task<(string ListId, ApiErrorResponse Error)> ResolveTargetShoppingListIdAsync(string preferredListId = null)
        {
            var (lists, listsError) = await _shoppingListService.GetListsAsync();
            if (listsError != null)
                return (null, listsError);

            string targetListId = null;

            if (!string.IsNullOrEmpty(preferredListId) && lists != null)
            {
                var match = System.Array.Find(lists, l => l.id == preferredListId);
                if (match != null) targetListId = match.id;
            }

            if (string.IsNullOrEmpty(targetListId))
            {
                var state = _storeService.GetAppState();
                string lastListId = state.userLastShoppingListId;
                if (!string.IsNullOrEmpty(lastListId) && lists != null)
                {
                    var match = System.Array.Find(lists, l => l.id == lastListId);
                    if (match != null) targetListId = match.id;
                }
            }

            if (string.IsNullOrEmpty(targetListId) && lists != null && lists.Length > 0)
            {
                targetListId = lists[0].id;
            }

            if (string.IsNullOrEmpty(targetListId))
            {
                string defaultName = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DEFAULT_SHOPPING_LIST_NAME");
                if (string.IsNullOrEmpty(defaultName) || defaultName.StartsWith("No translation found"))
                {
                    defaultName = "Mi Lista de la Compra";
                }

                var (newList, createErr) = await _shoppingListService.CreateListAsync(defaultName);
                if (createErr != null || newList == null)
                    return (null, createErr);

                targetListId = newList.id;
            }

            return (targetListId, null);
        }

        private async Task<(bool Success, ApiErrorResponse Error)> TryAddIngredientsAsync(string targetListId, RecipeIngredient[] ingredients)
        {
            ApiErrorResponse firstError = null;
            var (existingItems, _) = await _shoppingListService.GetItemsAsync(targetListId);
            var itemsList = existingItems != null ? new System.Collections.Generic.List<ShoppingListItem>(existingItems) : new System.Collections.Generic.List<ShoppingListItem>();

            for (int i = 0; i < ingredients.Length; i++)
            {
                var ing = ingredients[i];
                if (i > 0)
                {
                    await Task.Delay(150);
                }

                ShoppingListItem existingItem = itemsList.FirstOrDefault(x =>
                    (!string.IsNullOrEmpty(ing.foodProductId) && x.foodProductId == ing.foodProductId) ||
                    (!string.IsNullOrEmpty(ing.genericFoodId) && x.genericFoodId == ing.genericFoodId));

                ApiErrorResponse itemErr = null;

                if (existingItem != null)
                {
                    float newQty = existingItem.quantity + 1f;
                    var (updated, updateErr) = await _shoppingListService.UpdateItemAsync(
                        targetListId, existingItem.id, newQty, existingItem.unit, existingItem.notes, existingItem.@checked);

                    if (updated != null)
                    {
                        existingItem.quantity = newQty;
                    }
                    else if (updateErr != null && (updateErr.statusCode == 409 || (updateErr.error != null && updateErr.error.Equals("ConflictException", StringComparison.OrdinalIgnoreCase))))
                    {
                        updateErr = null;
                    }
                    else
                    {
                        itemErr = updateErr;
                    }
                }
                else
                {
                    var (added, addErr) = await _shoppingListService.AddItemAsync(
                        targetListId,
                        foodProductId: ing.foodProductId,
                        genericFoodId: ing.genericFoodId,
                        quantity: 1f,
                        unit: "PIECES");

                    if (addErr != null && (addErr.statusCode == 429 || (addErr.error != null && addErr.error.Equals("ThrottlerException", StringComparison.OrdinalIgnoreCase))))
                    {
                        await Task.Delay(500);
                        (added, addErr) = await _shoppingListService.AddItemAsync(
                            targetListId,
                            foodProductId: ing.foodProductId,
                            genericFoodId: ing.genericFoodId,
                            quantity: 1f,
                            unit: "PIECES");
                    }

                    if (added != null)
                    {
                        itemsList.Add(added);
                    }
                    else if (addErr != null && (addErr.statusCode == 409 || (addErr.error != null && addErr.error.Equals("ConflictException", StringComparison.OrdinalIgnoreCase)) || (addErr.message != null && addErr.message.ToLowerInvariant().Contains("already"))))
                    {
                        addErr = null;
                    }
                    else
                    {
                        itemErr = addErr;
                    }
                }

                if (itemErr != null && firstError == null)
                {
                    firstError = itemErr;
                }
            }
            return (firstError == null, firstError);
        }
    }
}
