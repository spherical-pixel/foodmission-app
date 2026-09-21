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

    [ObservableObject]
    public partial class RecipeBookViewModel : ViewModelBase
    {
        private readonly IRecipeService _recipeService;
        private readonly ICatalogService _catalogService;
        private readonly ILocalStorageService _localStorage;

        private List<RecipeView> _allRecipes = new();

        private const string CacheKeyPrefix = "recipes_cache_";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);


        [ObservableProperty] private RecipeBookTab m_CurrentTab = RecipeBookTab.ForYou;
        [ObservableProperty] private List<RecipeView> m_Recipes = new();
        [ObservableProperty] private List<RecipeView> m_Recommendations = new();
        [ObservableProperty] private List<RecipeView> m_MyRecipes = new();
        [ObservableProperty] private bool m_IsLoading;
        [ObservableProperty] private bool m_IsLoadingMore;
        [ObservableProperty] private bool m_HasMore = true;
        [ObservableProperty] private ApiErrorResponse m_ErrorDetail;
        [ObservableProperty] private string m_FilterText = "";
        [ObservableProperty] private string m_SearchText = "";
        [ObservableProperty] private string m_SelectedDifficulty = "all";
        [ObservableProperty] private string m_SelectedCategory = "all";
        [ObservableProperty] private string m_SelectedCuisine = "all";
        [ObservableProperty] private int m_CurrentPage = 1;
        [ObservableProperty] private int m_ExpiringItemsCount;
        [ObservableProperty] private int m_TotalPantryItems;
        [ObservableProperty] private bool m_IsPantryEmpty;

        public RecipeBookViewModel(
            IStoreService storeService,
            IRecipeService recipeService,
            ICatalogService catalogService,
            ILocalStorageService localStorage) : base(storeService)
        {
            _recipeService = recipeService;
            _catalogService = catalogService;
            _localStorage = localStorage;

            InitDefaultCuisine();
        }

        private void InitDefaultCuisine()
        {
            string country = _storeService?.GetAppState()?.userCountry;
            var defaultCuisine = RecipeCatalogs.GetCuisineByCountryCode(country);
            if (defaultCuisine != null)
            {
                m_SelectedCuisine = defaultCuisine.Code;
            }
            else
            {
                m_SelectedCuisine = "all";
            }
        }

        public void ApplyUserCountryCuisineDefaultIfUnset()
        {
            if (SelectedCuisine == "all")
            {
                string country = _storeService?.GetAppState()?.userCountry;
                var defaultCuisine = RecipeCatalogs.GetCuisineByCountryCode(country);
                if (defaultCuisine != null)
                {
                    SelectedCuisine = defaultCuisine.Code;
                }
            }
        }

        public async Task SetTabAsync(RecipeBookTab tab)
        {
            if (CurrentTab == tab) return;
            CurrentTab = tab;
            await LoadAsync();
        }

        public async Task SetDifficultyAsync(string difficulty)
        {
            SelectedDifficulty = string.IsNullOrEmpty(difficulty) ? "all" : difficulty;
            await LoadAsync();
        }

        public async Task SetCategoryAsync(string category)
        {
            SelectedCategory = string.IsNullOrEmpty(category) ? "all" : category;
            await LoadAsync();
        }

        public async Task SetCuisineAsync(string cuisine)
        {
            SelectedCuisine = string.IsNullOrEmpty(cuisine) ? "all" : cuisine;
            await LoadAsync();
        }

        public async Task ClearFiltersAsync()
        {
            SearchText = "";
            SelectedDifficulty = "all";
            SelectedCategory = "all";
            SelectedCuisine = "all";
            await LoadAsync();
        }

        public void OpenCreateRecipe()
        {
            RaiseNavigationRequested(Actions.recipes_to_editor);
        }

        public void GoToPantry()
        {
            RaiseNavigationRequested(Actions.go_to_pantry);
        }

        public void OpenRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return;
            RaiseNavigationRequested(Actions.recipes_to_detail, new[] { new Argument("recipeId", recipeId) });
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorDetail = null;
            CurrentPage = 1;
            HasMore = true;

            try
            {
                switch (CurrentTab)
                {
                    case RecipeBookTab.ForYou:
                        await LoadRecommendationsAsync();
                        break;
                    case RecipeBookTab.MyRecipes:
                        await LoadMyRecipesAsync();
                        break;
                    case RecipeBookTab.Explore:
                    default:
                        await LoadExploreAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RecipeBookViewModel] LoadAsync: {ex}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadRecommendationsAsync()
        {
            var (recs, err) = await _recipeService.GetRecommendationsAsync(expiringWithinDays: 7, limit: 20);
            if (err != null)
            {
                ErrorDetail = err;
                var cached = _localStorage.GetValue<List<RecipeView>>(CurrentCacheKey + "_rec");
                Recommendations = cached ?? new();
                IsPantryEmpty = Recommendations.Count == 0;
                return;
            }

            ExpiringItemsCount = recs?.expiringItemsCount ?? 0;
            TotalPantryItems = recs?.totalPantryItems ?? 0;
            IsPantryEmpty = TotalPantryItems == 0 && (recs?.data == null || recs.data.Length == 0);

            var list = new List<RecipeView>();
            if (recs?.data != null)
            {
                foreach (var r in recs.data)
                {
                    if (r?.recipe == null) continue;
                    var expiringNames = r.matchedIngredients?
                        .Where(m => m.isExpiringSoon)
                        .Select(m => m.pantryItemName ?? m.ingredientName)
                        .ToArray();

                    list.Add(new RecipeView
                    {
                        Item = r.recipe,
                        DisplayTitle = r.recipe.title,
                        PlaceholderEmoji = RecipeCatalogs.GetCategoryEmoji(r.recipe.category),
                        IsRecommendation = true,
                        MatchCount = r.matchCount ?? 0,
                        TotalIngredients = r.totalIngredients ?? (r.recipe.ingredients?.Length ?? 0),
                        ExpiringMatchCount = r.expiringMatchCount ?? 0,
                        ExpiringIngredientNames = expiringNames
                    });
                }
            }

            Recommendations = list;
            _localStorage.SetValue(CurrentCacheKey + "_rec", list);
        }

        private async Task LoadExploreAsync()
        {
            _allRecipes.Clear();
            var search = string.IsNullOrEmpty(SearchText) ? null : SearchText;
            var category = SelectedCategory == "all" ? null : SelectedCategory;
            var cuisine = SelectedCuisine == "all" ? null : SelectedCuisine;
            var difficulty = SelectedDifficulty == "all" ? null : SelectedDifficulty;

            var (page, pageErr) = await _recipeService.GetRecipesAsync(
                search: search,
                category: category,
                cuisineType: cuisine,
                difficulty: difficulty,
                page: 1,
                limit: 20);

            if (pageErr != null)
            {
                ErrorDetail = pageErr;
                var cached = _localStorage.GetValue<List<RecipeView>>(CurrentCacheKey);
                _allRecipes = cached ?? new();
                HasMore = false;
                Recipes = _allRecipes.ToList();
            }
            else
            {
                _allRecipes = page?.data?.Select(r => new RecipeView
                {
                    Item = r,
                    DisplayTitle = r.title,
                    PlaceholderEmoji = RecipeCatalogs.GetCategoryEmoji(r.category)
                }).ToList() ?? new();

                HasMore = page != null && page.page < page.totalPages;
                SaveCacheFromAll();
                Recipes = _allRecipes.ToList();
            }
        }

        private async Task LoadMyRecipesAsync()
        {
            var search = string.IsNullOrEmpty(SearchText) ? null : SearchText;
            var category = SelectedCategory == "all" ? null : SelectedCategory;
            var cuisine = SelectedCuisine == "all" ? null : SelectedCuisine;
            var difficulty = SelectedDifficulty == "all" ? null : SelectedDifficulty;

            var (page, err) = await _recipeService.GetMyRecipesAsync(
                search: search,
                category: category,
                cuisineType: cuisine,
                difficulty: difficulty,
                page: 1,
                limit: 20);

            if (err != null)
            {
                ErrorDetail = err;
                var cached = _localStorage.GetValue<List<RecipeView>>(CurrentCacheKey + "_mine");
                MyRecipes = cached ?? new();
                return;
            }

            var list = page?.data?.Select(r => new RecipeView
            {
                Item = r,
                DisplayTitle = r.title,
                PlaceholderEmoji = RecipeCatalogs.GetCategoryEmoji(r.category)
            }).ToList() ?? new();

            MyRecipes = list;
            _localStorage.SetValue(CurrentCacheKey + "_mine", list);
        }

        public async Task LoadNextPageAsync()
        {
            if (IsLoadingMore || !HasMore || CurrentTab == RecipeBookTab.ForYou) return;
            IsLoadingMore = true;
            try
            {
                CurrentPage++;
                var search = string.IsNullOrEmpty(SearchText) ? null : SearchText;
                var category = SelectedCategory == "all" ? null : SelectedCategory;
                var cuisine = SelectedCuisine == "all" ? null : SelectedCuisine;
                var difficulty = SelectedDifficulty == "all" ? null : SelectedDifficulty;

                if (CurrentTab == RecipeBookTab.MyRecipes)
                {
                    var (page, err) = await _recipeService.GetMyRecipesAsync(
                        search: search,
                        category: category,
                        cuisineType: cuisine,
                        difficulty: difficulty,
                        page: CurrentPage,
                        limit: 20);

                    if (err != null)
                    {
                        ErrorDetail = err;
                        CurrentPage--;
                        return;
                    }

                    var newMine = page?.data?.Select(r => new RecipeView
                    {
                        Item = r,
                        DisplayTitle = r.title,
                        PlaceholderEmoji = RecipeCatalogs.GetCategoryEmoji(r.category)
                    }).ToList() ?? new();

                    var combined = new List<RecipeView>(MyRecipes);
                    combined.AddRange(newMine);
                    MyRecipes = combined;
                    HasMore = page != null && page.page < page.totalPages;
                }
                else
                {
                    var (page, err) = await _recipeService.GetRecipesAsync(
                        search: search,
                        category: category,
                        cuisineType: cuisine,
                        difficulty: difficulty,
                        page: CurrentPage,
                        limit: 20);

                    if (err != null)
                    {
                        ErrorDetail = err;
                        CurrentPage--;
                        return;
                    }

                    var newRecipes = page?.data?.Select(r => new RecipeView
                    {
                        Item = r,
                        DisplayTitle = r.title,
                        PlaceholderEmoji = RecipeCatalogs.GetCategoryEmoji(r.category)
                    }).ToList() ?? new();

                    _allRecipes.AddRange(newRecipes);
                    HasMore = page != null && page.page < page.totalPages;
                    SaveCacheFromAll();
                    Recipes = _allRecipes.ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RecipeBookViewModel] LoadNextPageAsync: {ex}");
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        private string CurrentCacheKey => $"{CacheKeyPrefix}";

        private void SaveCacheFromAll()
        {
            _localStorage.SetValue(CurrentCacheKey, _allRecipes);
        }
    }
}
