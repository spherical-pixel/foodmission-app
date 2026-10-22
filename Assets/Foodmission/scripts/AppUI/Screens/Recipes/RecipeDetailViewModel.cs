using System;
using System.Collections.Generic;
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
        private readonly IPantryService _pantryService;
        private readonly ICatalogService _catalogService;
        private readonly IChallengeSessionService _challengeSession;

        [ObservableProperty] private Recipe m_Recipe;
        [ObservableProperty] private bool m_IsLoading;
        [ObservableProperty] private ApiErrorResponse m_ErrorDetail;
        [ObservableProperty] private bool m_IsOwner;
        [ObservableProperty] private bool m_IsAddingToShoppingList;
        [ObservableProperty] private bool m_HasNutritionInfo;
        [ObservableProperty] private bool m_HasVideo;

        // Rating: average/count shown under the hero image; MyRating (0 = not rated) drives the "rate this recipe" card.
        [ObservableProperty] private float _averageRating;
        [ObservableProperty] private int _ratingCount;
        [ObservableProperty] private int _myRating;
        [ObservableProperty] private bool _canRate;
        [ObservableProperty] private bool _isRatingBusy;
        /// <summary>Rating save/remove failures, kept apart from ErrorDetail so the screen can title them correctly.</summary>
        [ObservableProperty] private ApiErrorResponse _ratingErrorDetail;

        public RecipeDetailViewModel(
            IStoreService storeService,
            IRecipeService recipeService,
            IShoppingListService shoppingListService,
            ICatalogService catalogService,
            IPantryService pantryService,
            IChallengeSessionService challengeSession = null) : base(storeService)
        {
            _recipeService = recipeService;
            _shoppingListService = shoppingListService;
            _pantryService = pantryService;
            _catalogService = catalogService;
            _challengeSession = challengeSession ?? App.current?.services?.GetService<IChallengeSessionService>();
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
                if (!string.IsNullOrEmpty(recipe?.id))
                {
                    _ = _challengeSession?.ReportAsync(ChallengeCompletionTrigger.RecipeViewed, recipe.id);
                }
                var state = _storeService.GetAppState();
                IsOwner = !string.IsNullOrEmpty(recipe?.userId) && recipe.userId == state.userId;
                HasNutritionInfo = recipe?.nutritionalInfo != null;
                HasVideo = !string.IsNullOrEmpty(recipe?.videoUrl);

                AverageRating = recipe?.rating ?? 0f;
                RatingCount = recipe?.ratingCount ?? 0;
                MyRating = 0;
                // Authors cannot rate their own recipes (avoids inflating the average).
                CanRate = recipe != null && !IsOwner;
                if (CanRate)
                {
                    await LoadRatingAsync(recipe.id);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RecipeDetailViewModel] LoadAsync: {ex}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
            finally { IsLoading = false; }
        }

        private async Task LoadRatingAsync(string recipeId)
        {
            var (rating, error) = await _recipeService.GetRatingAsync(recipeId);
            if (error != null)
            {
                // Not critical: the average from the recipe stays visible and the user can still rate.
                Debug.LogWarning($"[RecipeDetailViewModel] Could not load rating for {recipeId}: {error.message}");
                return;
            }
            ApplyRating(rating);
        }

        public async Task RateAsync(int stars)
        {
            if (!CanRate || Recipe == null || IsRatingBusy || stars < 1 || stars > 5 || stars == MyRating)
            {
                return;
            }

            int previous = MyRating;
            MyRating = stars;
            IsRatingBusy = true;
            try
            {
                var (rating, error) = await _recipeService.RateAsync(Recipe.id, stars);
                if (error != null)
                {
                    MyRating = previous;
                    RatingErrorDetail = error;
                    return;
                }
                ApplyRating(rating);
            }
            finally
            {
                IsRatingBusy = false;
            }
        }

        public async Task RemoveRatingAsync()
        {
            if (!CanRate || Recipe == null || IsRatingBusy || MyRating == 0)
            {
                return;
            }

            int previous = MyRating;
            MyRating = 0;
            IsRatingBusy = true;
            try
            {
                var (rating, error) = await _recipeService.RemoveRatingAsync(Recipe.id);
                if (error != null)
                {
                    MyRating = previous;
                    RatingErrorDetail = error;
                    return;
                }
                ApplyRating(rating);
            }
            finally
            {
                IsRatingBusy = false;
            }
        }

        private void ApplyRating(RecipeRating rating)
        {
            if (rating == null)
            {
                return;
            }
            AverageRating = rating.rating;
            RatingCount = rating.ratingCount;
            MyRating = rating.myRating ?? 0;
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

        /// <summary>
        /// Recipe ingredients that can go to the shopping list (they have a food id), checked against the pantry:
        /// pre-selected when missing, or when the pantry has less than the recipe needs (then only the difference is
        /// offered). When the amounts can't be compared (free-text measure, pieces vs grams…) having it is enough.
        /// If the pantry can't be loaded every ingredient counts as missing.
        /// </summary>
        public async Task<RecipeShoppingCandidate[]> GetShoppingListCandidatesAsync()
        {
            var ingredients = Recipe?.ingredients?
                .Where(ing => !string.IsNullOrEmpty(ing.foodProductId) || !string.IsNullOrEmpty(ing.genericFoodId))
                .ToArray();
            if (ingredients == null || ingredients.Length == 0)
            {
                return Array.Empty<RecipeShoppingCandidate>();
            }

            PantryItem[] pantryItems = null;
            if (_pantryService != null)
            {
                IsAddingToShoppingList = true;
                try
                {
                    var (items, error) = await _pantryService.GetItemsAsync();
                    if (error != null)
                    {
                        Debug.LogWarning($"[RecipeDetailViewModel] Pantry not available, offering every ingredient: {error.message}");
                    }
                    pantryItems = items;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RecipeDetailViewModel] GetShoppingListCandidatesAsync: {ex}");
                }
                finally { IsAddingToShoppingList = false; }
            }

            return ingredients.Select(ing => BuildCandidate(ing, pantryItems)).ToArray();
        }

        public static RecipeShoppingCandidate BuildCandidate(RecipeIngredient ingredient, IReadOnlyList<PantryItem> pantryItems)
        {
            var candidate = BuildCandidateCore(ingredient, pantryItems);
            // Show in the dialog exactly what the shopping list will store
            candidate.Quantity = UnitCodes.ToWholeQuantity(candidate.Quantity, candidate.Unit, out string wholeUnit);
            candidate.Unit = wholeUnit;
            return candidate;
        }

        private static RecipeShoppingCandidate BuildCandidateCore(RecipeIngredient ingredient, IReadOnlyList<PantryItem> pantryItems)
        {
            bool hasQty = UnitCatalog.Current.TryParseExactMeasure(ingredient.measure, out float recipeQty, out string recipeUnit) && recipeQty > 0f;
            var candidate = new RecipeShoppingCandidate
            {
                Ingredient = ingredient,
                HasRecipeQuantity = hasQty,
                Quantity = hasQty ? recipeQty : 1f,
                Unit = hasQty ? recipeUnit : UnitCodes.Default,
                Selected = true
            };

            var matches = pantryItems?
                .Where(p => p != null &&
                    ((!string.IsNullOrEmpty(ingredient.foodProductId) && p.foodProductId == ingredient.foodProductId) ||
                     (!string.IsNullOrEmpty(ingredient.genericFoodId) && p.genericFoodId == ingredient.genericFoodId)))
                .ToList();
            if (matches == null || matches.Count == 0)
            {
                return candidate;
            }

            candidate.InPantry = true;
            candidate.PantryUnit = string.IsNullOrEmpty(matches[0].unit) ? UnitCodes.Default : matches[0].unit;
            candidate.PantryQuantity = SumConvertible(matches, candidate.PantryUnit, out _);

            bool comparable = false;
            float available = hasQty ? SumConvertible(matches, recipeUnit, out comparable) : 0f;
            if (!comparable)
            {
                candidate.Selected = false;
                return candidate;
            }

            float missing = recipeQty - available;
            if (missing > 0.0001f)
            {
                candidate.PantryShort = true;
                candidate.Quantity = missing;
            }
            else
            {
                candidate.Selected = false;
            }
            return candidate;
        }

        /// <summary>Sum of the pantry items expressible in <paramref name="unit"/>; <paramref name="any"/> is false when none is.</summary>
        private static float SumConvertible(IEnumerable<PantryItem> items, string unit, out bool any)
        {
            any = false;
            float total = 0f;
            foreach (var item in items)
            {
                if (UnitCodes.TryConvert(item.quantity, item.unit, unit, out float converted))
                {
                    total += converted;
                    any = true;
                }
            }
            return total;
        }

        public async Task<bool> AddIngredientsToShoppingListAsync(IReadOnlyList<RecipeShoppingCandidate> candidates, string listId = null)
        {
            var selected = candidates?.Where(c => c?.Ingredient != null && c.Selected).ToArray();
            if (selected == null || selected.Length == 0) return false;
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

                var (success, error) = await TryAddIngredientsAsync(targetListId, selected);

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

                    (success, error) = await TryAddIngredientsAsync(newList.id, selected);
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

        private async Task<(bool Success, ApiErrorResponse Error)> TryAddIngredientsAsync(string targetListId, RecipeShoppingCandidate[] candidates)
        {
            ApiErrorResponse firstError = null;
            var (existingItems, _) = await _shoppingListService.GetItemsAsync(targetListId);
            var itemsList = existingItems != null ? new System.Collections.Generic.List<ShoppingListItem>(existingItems) : new System.Collections.Generic.List<ShoppingListItem>();

            for (int i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                var ing = candidate.Ingredient;
                if (i > 0)
                {
                    await Task.Delay(150);
                }

                ShoppingListItem existingItem = itemsList.FirstOrDefault(x =>
                    (!string.IsNullOrEmpty(ing.foodProductId) && x.foodProductId == ing.foodProductId) ||
                    (!string.IsNullOrEmpty(ing.genericFoodId) && x.genericFoodId == ing.genericFoodId));

                ApiErrorResponse itemErr = null;

                if (existingItem != null && !UnitCodes.TryConvert(candidate.Quantity, candidate.Unit, existingItem.unit, out _))
                {
                    // Already on the list in a unit that can't be added up (pieces vs grams…): leave it as it is
                }
                else if (existingItem != null)
                {
                    UnitCodes.TryConvert(candidate.Quantity, candidate.Unit, existingItem.unit, out float extra);
                    int newQty = UnitCodes.ToWholeQuantity(existingItem.quantity + extra, existingItem.unit, out string newUnit);
                    var (updated, updateErr) = await _shoppingListService.UpdateItemAsync(
                        targetListId, existingItem.id, newQty, newUnit, existingItem.notes, existingItem.@checked);

                    if (updated != null)
                    {
                        existingItem.quantity = newQty;
                        existingItem.unit = newUnit;
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
                    int addQty = UnitCodes.ToWholeQuantity(candidate.Quantity, candidate.Unit, out string addUnit);
                    var (added, addErr) = await _shoppingListService.AddItemAsync(
                        targetListId,
                        foodProductId: ing.foodProductId,
                        genericFoodId: ing.genericFoodId,
                        quantity: addQty,
                        unit: addUnit);

                    if (addErr != null && (addErr.statusCode == 429 || (addErr.error != null && addErr.error.Equals("ThrottlerException", StringComparison.OrdinalIgnoreCase))))
                    {
                        await Task.Delay(500);
                        (added, addErr) = await _shoppingListService.AddItemAsync(
                            targetListId,
                            foodProductId: ing.foodProductId,
                            genericFoodId: ing.genericFoodId,
                            quantity: addQty,
                            unit: addUnit);
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
