using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.AppUI.UI;
using Unity.AppUI.Navigation.Generated;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Scripting;
using eu.foodmission.platform;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using eu.foodmission.platform.Components;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [Preserve]
    public class RecipeBookScreen : NavigationScreenBase<RecipeBookViewModel>
    {
        // Top Action Header
        private FMButton _btnCreateRecipe;

        // Tabs
        private ActionGroup _recipesTabs;
        private ActionButton _btnTabForYou;
        private ActionButton _btnTabExplore;
        private ActionButton _btnTabMyRecipes;

        // Filters
        private VisualElement _filterSection;
        private SearchBar _searchBar;
        private Dropdown _cuisineDropdown;
        private readonly List<string> _cuisineOptions = new();
        private readonly List<string> _cuisineCodes = new();
        private ActionGroup _groupDifficultyFilters;
        private ActionButton _btnDiffAll;
        private ActionButton _btnDiffEasy;
        private ActionButton _btnDiffMedium;
        private ActionButton _btnDiffHard;
        private VisualElement _categoriesGrid;
        private readonly List<(string Code, Text Chip)> _categoryChips = new();

        // Scroll and Views
        private ScrollView _scrollView;
        private VisualElement _viewForYou;
        private VisualElement _foryouEmptyPantry;
        private FMNutriView _nutriAvatar;
        private FMButton _btnGoToPantry;
        private VisualElement _foryouContent;
        private VisualElement _recommendationsContainer;

        private VisualElement _viewExplore;
        private Text _counterText;
        private Text _emptyState;
        private VisualElement _itemsContainer;
        private VisualElement _scrollSentinel;

        private VisualElement _viewMyRecipes;
        private VisualElement _myRecipesEmpty;
        private FMButton _btnCreateFirstRecipe;
        private VisualElement _myRecipesContainer;

        private CancellationTokenSource _searchCts;

        override protected bool IsFixedContent => false;
        override protected bool ApplySafeAreaTop => false;
        override protected bool ApplySafeAreaBottom => false;
        override protected bool ApplySafeAreaLeft => false;
        override protected bool ApplySafeAreaRight => false;

        public RecipeBookScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.RecipeBook));
            CacheUIElements();
            BuildCategoryChips();
            BuildCuisineDropdown();
        }

        private void CacheUIElements()
        {
            _btnCreateRecipe = contentContainer.Q<FMButton>("btn-create-recipe");
            _recipesTabs = contentContainer.Q<ActionGroup>("recipes-tabs");
            _btnTabForYou = contentContainer.Q<ActionButton>("btn-tab-for-you");
            _btnTabExplore = contentContainer.Q<ActionButton>("btn-tab-explore");
            _btnTabMyRecipes = contentContainer.Q<ActionButton>("btn-tab-my-recipes");

            _filterSection = contentContainer.Q<VisualElement>("filter-section");
            _searchBar = contentContainer.Q<SearchBar>("search-bar");
            _cuisineDropdown = contentContainer.Q<Dropdown>("cuisine-dropdown");
            _groupDifficultyFilters = contentContainer.Q<ActionGroup>("group-difficulty-filters");
            _btnDiffAll = contentContainer.Q<ActionButton>("btn-diff-all");
            _btnDiffEasy = contentContainer.Q<ActionButton>("btn-diff-easy");
            _btnDiffMedium = contentContainer.Q<ActionButton>("btn-diff-medium");
            _btnDiffHard = contentContainer.Q<ActionButton>("btn-diff-hard");
            _categoriesGrid = contentContainer.Q<VisualElement>("categories-grid");

            _scrollView = contentContainer.Q<ScrollView>("scroll-view");
            _viewForYou = contentContainer.Q<VisualElement>("view-for-you");
            _foryouEmptyPantry = contentContainer.Q<VisualElement>("foryou-empty-pantry");
            _nutriAvatar = contentContainer.Q<FMNutriView>("nutri-avatar");
            _btnGoToPantry = contentContainer.Q<FMButton>("btn-go-to-pantry");
            _foryouContent = contentContainer.Q<VisualElement>("foryou-content");
            _recommendationsContainer = contentContainer.Q<VisualElement>("recommendations-container");

            _viewExplore = contentContainer.Q<VisualElement>("view-explore");
            _counterText = contentContainer.Q<Text>("counter-text");
            _emptyState = contentContainer.Q<Text>("empty-state");
            _itemsContainer = contentContainer.Q<VisualElement>("items-container");
            _scrollSentinel = contentContainer.Q<VisualElement>("scroll-sentinel");

            _viewMyRecipes = contentContainer.Q<VisualElement>("view-my-recipes");
            _myRecipesEmpty = contentContainer.Q<VisualElement>("my-recipes-empty");
            _btnCreateFirstRecipe = contentContainer.Q<FMButton>("btn-create-first-recipe");
            _myRecipesContainer = contentContainer.Q<VisualElement>("my-recipes-container");
        }

        private void BuildCategoryChips()
        {
            if (_categoriesGrid == null) return;
            _categoriesGrid.Clear();
            _categoryChips.Clear();

            // "Todas" chip
            var allText = "🍽️ " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_FILTER_ALL");
            CreateChip("all", allText, true);

            foreach (var cat in RecipeCatalogs.Categories)
            {
                CreateChip(cat.Code, $"{cat.Emoji} {cat.GetLocalizedName()}", false);
            }
        }

        private void CreateChip(string code, string text, bool isActive)
        {
            var chip = new Text { text = text };
            chip.AddToClassList("fm-r-cat-chip");
            if (isActive)
            {
                chip.AddToClassList("fm-r-cat-chip--active");
            }
            chip.RegisterCallback<ClickEvent>(_ => SelectCategory(code));
            _categoriesGrid.Add(chip);
            _categoryChips.Add((code, chip));
        }

        private void SelectCategory(string code)
        {
            UpdateCategorySelectionVisuals(code);
            _ = _viewModel?.SetCategoryAsync(code);
        }

        private void UpdateCategorySelectionVisuals(string selectedCode)
        {
            string target = string.IsNullOrEmpty(selectedCode) ? "all" : selectedCode;
            foreach (var (code, chip) in _categoryChips)
            {
                chip.EnableInClassList("fm-r-cat-chip--active", code.Equals(target, StringComparison.OrdinalIgnoreCase));
            }
        }

        private void BuildCuisineDropdown()
        {
            if (_cuisineDropdown == null) return;
            _cuisineOptions.Clear();
            _cuisineCodes.Clear();

            string allLabel = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_CUISINE_ALL");
            if (string.IsNullOrEmpty(allLabel) || allLabel.StartsWith("No translation"))
            {
                allLabel = "🍽️ Todas las cocinas";
            }
            _cuisineOptions.Add(allLabel);
            _cuisineCodes.Add("all");

            var sortedCuisines = RecipeCatalogs.Cuisines
                .OrderBy(c => c.GetLocalizedName())
                .ToList();

            foreach (var cuisine in sortedCuisines)
            {
                _cuisineOptions.Add($"{cuisine.Emoji} {cuisine.GetLocalizedName()}");
                _cuisineCodes.Add(cuisine.Code);
            }

            _cuisineDropdown.sourceItems = _cuisineOptions;
            _cuisineDropdown.bindItem = (item, index) =>
            {
                if (index >= 0 && index < _cuisineOptions.Count)
                {
                    item.label = _cuisineOptions[index];
                    item.icon = null;
                }
            };
        }

        private void OnCuisineDropdownChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            var value = evt.newValue?.ToArray();
            if (value != null && value.Length > 0 && value[0] >= 0 && value[0] < _cuisineCodes.Count)
            {
                string code = _cuisineCodes[value[0]];
                if (_viewModel != null && !_viewModel.SelectedCuisine.Equals(code, StringComparison.OrdinalIgnoreCase))
                {
                    _ = _viewModel.SetCuisineAsync(code);
                }
            }
        }

        private void UpdateCuisineSelectionVisuals(string selectedCuisine)
        {
            if (_cuisineDropdown == null || _cuisineCodes.Count == 0) return;
            string target = string.IsNullOrEmpty(selectedCuisine) ? "all" : selectedCuisine;
            int idx = _cuisineCodes.FindIndex(c => c.Equals(target, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) idx = 0;
            _cuisineDropdown.SetValueWithoutNotify(new[] { idx });
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_scrollView != null && _scrollView.verticalScroller != null)
            {
                _scrollView.verticalScroller.valueChanged += OnScrollValueChanged;
            }

            if (_searchBar != null)
            {
                _searchBar.RegisterValueChangingCallback(OnSearchChanging);
            }

            if (_cuisineDropdown != null)
            {
                _cuisineDropdown.RegisterValueChangedCallback(OnCuisineDropdownChanged);
            }

            // Temporarily disabled: recipe creation & My Recipes tab
            // if (_btnCreateRecipe != null)
            //     _btnCreateRecipe.clicked += () => _viewModel?.OpenCreateRecipe();
            // if (_btnCreateFirstRecipe != null)
            //     _btnCreateFirstRecipe.clicked += () => _viewModel?.OpenCreateRecipe();

            if (_btnGoToPantry != null)
                _btnGoToPantry.clicked += () => _viewModel?.GoToPantry();

            if (_btnTabForYou != null) _btnTabForYou.clicked += () => _ = _viewModel?.SetTabAsync(RecipeBookTab.ForYou);
            if (_btnTabExplore != null) _btnTabExplore.clicked += () => _ = _viewModel?.SetTabAsync(RecipeBookTab.Explore);
            // if (_btnTabMyRecipes != null) _btnTabMyRecipes.clicked += () => _ = _viewModel?.SetTabAsync(RecipeBookTab.MyRecipes);

            if (_btnDiffAll != null) _btnDiffAll.clicked += () => _ = _viewModel?.SetDifficultyAsync("all");
            if (_btnDiffEasy != null) _btnDiffEasy.clicked += () => _ = _viewModel?.SetDifficultyAsync("easy");
            if (_btnDiffMedium != null) _btnDiffMedium.clicked += () => _ = _viewModel?.SetDifficultyAsync("medium");
            if (_btnDiffHard != null) _btnDiffHard.clicked += () => _ = _viewModel?.SetDifficultyAsync("hard");

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            UpdateTabViews();
            UpdateCategorySelectionVisuals(_viewModel?.SelectedCategory);
            UpdateCuisineSelectionVisuals(_viewModel?.SelectedCuisine);
            RebuildCurrentTabView();
            UpdateLoadingState();
            UpdateErrorState();

            _ = _viewModel.LoadAsync();
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (_scrollView != null && _scrollView.verticalScroller != null)
            {
                _scrollView.verticalScroller.valueChanged -= OnScrollValueChanged;
            }

            if (_searchBar != null)
            {
                _searchBar.UnregisterValueChangingCallback(OnSearchChanging);
            }

            if (_cuisineDropdown != null)
            {
                _cuisineDropdown.UnregisterValueChangedCallback(OnCuisineDropdownChanged);
            }

            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = null;

            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.CurrentTab):
                case nameof(_viewModel.SelectedDifficulty):
                    UpdateTabViews();
                    break;
                case nameof(_viewModel.SelectedCategory):
                    UpdateCategorySelectionVisuals(_viewModel.SelectedCategory);
                    break;
                case nameof(_viewModel.SelectedCuisine):
                    UpdateCuisineSelectionVisuals(_viewModel.SelectedCuisine);
                    break;
                case nameof(_viewModel.Recommendations):
                    RebuildRecommendations();
                    break;
                case nameof(_viewModel.Recipes):
                    RebuildExploreRecipes();
                    break;
                case nameof(_viewModel.MyRecipes):
                    RebuildMyRecipes();
                    break;
                case nameof(_viewModel.IsPantryEmpty):
                    UpdatePantryEmptyState();
                    break;
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateErrorState();
                    break;
            }
        }

        private void UpdateTabViews()
        {
            if (_viewModel == null) return;

            int tabIdx = _viewModel.CurrentTab switch
            {
                RecipeBookTab.Explore => 1,
                _ => 0
            };
            _recipesTabs?.SetSelectionWithoutNotify(new[] { tabIdx });

            int diffIndex = _viewModel.SelectedDifficulty switch
            {
                "easy" => 1,
                "medium" => 2,
                "hard" => 3,
                _ => 0
            };
            _groupDifficultyFilters?.SetSelectionWithoutNotify(new[] { diffIndex });

            if (_viewForYou != null)
                _viewForYou.style.display = _viewModel.CurrentTab == RecipeBookTab.ForYou ? DisplayStyle.Flex : DisplayStyle.None;

            if (_viewExplore != null)
                _viewExplore.style.display = _viewModel.CurrentTab == RecipeBookTab.Explore ? DisplayStyle.Flex : DisplayStyle.None;

            if (_viewMyRecipes != null)
                _viewMyRecipes.style.display = DisplayStyle.None;

            // Search and filter row are visible on Explore (hidden on For You)
            if (_filterSection != null)
                _filterSection.style.display = _viewModel.CurrentTab == RecipeBookTab.ForYou ? DisplayStyle.None : DisplayStyle.Flex;

            RebuildCurrentTabView();
        }

        private void RebuildCurrentTabView()
        {
            if (_viewModel == null) return;
            switch (_viewModel.CurrentTab)
            {
                case RecipeBookTab.ForYou:
                    RebuildRecommendations();
                    UpdatePantryEmptyState();
                    break;
                case RecipeBookTab.MyRecipes:
                    RebuildMyRecipes();
                    break;
                case RecipeBookTab.Explore:
                default:
                    RebuildExploreRecipes();
                    break;
            }
        }

        private void UpdatePantryEmptyState()
        {
            bool empty = _viewModel.IsPantryEmpty || (_viewModel.Recommendations == null || _viewModel.Recommendations.Count == 0);
            if (_foryouEmptyPantry != null)
            {
                _foryouEmptyPantry.style.display = empty ? DisplayStyle.Flex : DisplayStyle.None;
                if (empty)
                {
                    App.current?.services?.GetService<INutriService>()?.SetAction(NutriAction.Talking);
                }
            }
            if (_foryouContent != null)
            {
                _foryouContent.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private void RebuildRecommendations()
        {
            if (_recommendationsContainer == null) return;
            _recommendationsContainer.Clear();

            UpdatePantryEmptyState();
            if (_viewModel.Recommendations == null || _viewModel.Recommendations.Count == 0)
                return;

            foreach (var rec in _viewModel.Recommendations)
            {
                var card = CreateRecipeCard(rec);
                _recommendationsContainer.Add(card);
            }
        }

        private void RebuildExploreRecipes()
        {
            if (_itemsContainer == null) return;
            _itemsContainer.Clear();

            var count = _viewModel.Recipes?.Count ?? 0;
            if (_counterText != null)
            {
                _counterText.text = new LocalizedOption("UI", "RECIPES_FOUND_N", count).GetText();
            }

            if (_viewModel.Recipes == null || _viewModel.Recipes.Count == 0)
            {
                _emptyState?.EnableInClassList("visible", true);
                return;
            }
            _emptyState?.EnableInClassList("visible", false);

            foreach (var recipeView in _viewModel.Recipes)
            {
                var card = CreateRecipeCard(recipeView);
                _itemsContainer.Add(card);
            }
        }

        private void RebuildMyRecipes()
        {
            if (_myRecipesContainer == null) return;
            _myRecipesContainer.Clear();

            bool empty = _viewModel.MyRecipes == null || _viewModel.MyRecipes.Count == 0;
            if (_myRecipesEmpty != null)
                _myRecipesEmpty.style.display = empty ? DisplayStyle.Flex : DisplayStyle.None;

            if (empty) return;

            foreach (var recipeView in _viewModel.MyRecipes)
            {
                var card = CreateRecipeCard(recipeView);
                _myRecipesContainer.Add(card);
            }
        }

        private FMItemRecipe CreateRecipeCard(RecipeView recipeView)
        {
            var captured = recipeView;
            var r = captured.Item;
            var authorStr = !string.IsNullOrEmpty(r?.userId) ? $"by User_{r.userId.Substring(0, Math.Min(6, r.userId.Length))}" : "";

            var ratingCount = r?.ratingCount ?? 0;
            var ratingVal = r?.rating ?? 0f;
            var ratingStr = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RATING") + ":";
            if (ratingCount > 0)
            {
                ratingStr += $" {ratingVal:F1}/5 ({ratingCount})";
            }
            else
            {
                ratingStr = null;
            }

            int totalMinutes = (r?.prepTime ?? 0) + (r?.cookTime ?? 0);
            string timeStr = totalMinutes > 0 ? $"⏱️ {totalMinutes} min" : null;

            string difficultyStr = null;
            if (!string.IsNullOrEmpty(r?.difficulty))
            {
                difficultyStr = r.difficulty.ToLowerInvariant() switch
                {
                    "easy" => "🟢 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_DIFFICULTY_EASY"),
                    "medium" => "🟡 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_DIFFICULTY_MEDIUM"),
                    "hard" => "🔴 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_DIFFICULTY_HARD"),
                    _ => r.difficulty
                };
            }

            string categoryStr = !string.IsNullOrEmpty(r?.category)
                ? $"{RecipeCatalogs.GetCategoryEmoji(r.category)} {RecipeCatalogs.GetLocalizedCategoryName(r.category)}"
                : null;

            string pantryBadge = null;
            if (captured.IsRecommendation)
            {
                if (captured.ExpiringMatchCount > 0)
                {
                    string template = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_EXPIRING_MATCH");
                    pantryBadge = string.Format(template, captured.ExpiringMatchCount);
                }
                else if (captured.MatchCount > 0)
                {
                    string template = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_PANTRY_MATCH");
                    pantryBadge = string.Format(template, captured.MatchCount, captured.TotalIngredients);
                }
            }

            var card = new FMItemRecipe
            {
                Text = captured.DisplayTitle,
                Author = authorStr,
                RatingText = ratingStr,
                ImageUrl = r?.imageUrl,
                Emoji = captured.PlaceholderEmoji ?? "🍲",
                TimeText = timeStr,
                DifficultyText = difficultyStr,
                CategoryText = categoryStr,
                PantryBadgeText = pantryBadge
            };

            card.RegisterCallback<ClickEvent>(_ =>
            {
                _viewModel.OpenRecipe(captured.Item.id);
            });

            return card;
        }

        private void OnScrollValueChanged(float value)
        {
            if (_viewModel == null || !_viewModel.HasMore || _viewModel.IsLoadingMore || _viewModel.IsLoading)
                return;

            if (_scrollView == null || _scrollView.verticalScroller == null) return;

            float maxScroll = _scrollView.verticalScroller.highValue;
            if (maxScroll > 0 && value >= maxScroll - 150f)
            {
                _ = SafeLoadNextPageAsync();
            }
        }

        private void OnSearchChanging(ChangingEvent<string> evt)
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;
            var query = evt.newValue;
            _ = Task.Delay(300, token).ContinueWith(_ =>
            {
                if (token.IsCancellationRequested) return;
                _viewModel.SearchText = query;
                _ = SafeReloadAsync();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void UpdateLoadingState()
        {
            if (_viewModel.IsLoading)
                FMLoadingOverlay.Show();
            else
                FMLoadingOverlay.Hide();
        }

        private void UpdateErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, "RECIPE_ERROR_LOAD", _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private async Task SafeLoadNextPageAsync()
        {
            try { await _viewModel.LoadNextPageAsync(); }
            catch (Exception ex) { Debug.LogError($"[RecipeBookScreen] SafeLoadNextPageAsync: {ex}"); }
        }

        private async Task SafeReloadAsync()
        {
            try { await _viewModel.LoadAsync(); }
            catch (Exception ex) { Debug.LogError($"[RecipeBookScreen] SafeReloadAsync: {ex}"); }
        }
    }
}
