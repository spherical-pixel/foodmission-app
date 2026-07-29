using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
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


        [ObservableProperty] private List<RecipeView> m_Recipes = new();
        [ObservableProperty] private bool m_IsLoading;
        [ObservableProperty] private bool m_IsLoadingMore;
        [ObservableProperty] private bool m_HasMore = true;
        [ObservableProperty] private ApiErrorResponse m_ErrorDetail;
        [ObservableProperty] private string m_FilterText = "";
        [ObservableProperty] private string m_SearchText = "";
        [ObservableProperty] private int m_CurrentPage = 1;

        public RecipeBookViewModel(
            IStoreService storeService,
            IRecipeService recipeService,
            ICatalogService catalogService,
            ILocalStorageService localStorage) : base(storeService)
        {
            _recipeService = recipeService;
            _catalogService = catalogService;
            _localStorage = localStorage;
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorDetail = null;
            _allRecipes.Clear();
            CurrentPage = 1;
            HasMore = true;

            try
            {
                var search = string.IsNullOrEmpty(SearchText) ? null : SearchText;


                var pageTask = _recipeService.GetRecipesAsync(search: search, page: 1, limit: 20);

                var (page, pageErr) = await pageTask;

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
                        PlaceholderEmoji = "📚"
                    }).ToList() ?? new();
                    HasMore = page != null && page.page < page.totalPages;
                    SaveCacheFromAll();
                    Recipes = _allRecipes.ToList();
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

        public async Task LoadNextPageAsync()
        {
            if (IsLoadingMore || !HasMore) return;
            IsLoadingMore = true;
            try
            {
                CurrentPage++;
                var search = string.IsNullOrEmpty(SearchText) ? null : SearchText;
                var (page, err) = await _recipeService.GetRecipesAsync(search: search, page: CurrentPage, limit: 20);

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
                    PlaceholderEmoji = "📚"
                }).ToList() ?? new();

                _allRecipes.AddRange(newRecipes);
                HasMore = page != null && page.page < page.totalPages;
                SaveCacheFromAll();
                Recipes = _allRecipes.ToList();
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
