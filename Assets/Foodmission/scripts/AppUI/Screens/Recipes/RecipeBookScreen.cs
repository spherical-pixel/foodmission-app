using System;
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
        private VisualElement _itemsContainer;
        private SearchBar _searchBar;
        private ScrollView _scrollView;
        private VisualElement _scrollSentinel;
        private Text _emptyState;
        private Text _counterText;
        private CancellationTokenSource _searchCts;

        public RecipeBookScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.RecipeBook));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _itemsContainer = contentContainer.Q<VisualElement>("items-container");
            _scrollView = contentContainer.Q<ScrollView>("scroll-view");
            _scrollSentinel = contentContainer.Q<VisualElement>("scroll-sentinel");
            _emptyState = contentContainer.Q<Text>("empty-state");
            _counterText = contentContainer.Q<Text>("counter-text");
            _searchBar = contentContainer.Q<SearchBar>("search-bar");
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

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            RebuildRecipes();
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

            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = null;

            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.Recipes):
                    RebuildRecipes();
                    break;
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateErrorState();
                    break;
                case nameof(_viewModel.IsLoadingMore):
                    UpdateLoadingMoreState();
                    break;
            }
        }

        private void RebuildRecipes()
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
                    ratingStr += LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NO_REVIEWS");
                }

                var card = new FMItemRecipe
                {
                    Text = captured.DisplayTitle,
                    Author = authorStr,
                    RatingText = ratingStr,
                    ImageUrl = r?.imageUrl
                };
                card.RegisterCallback<ClickEvent>(evt =>
                {
                    _navController.Navigate(Actions.recipes_to_detail, new[] { new Argument("recipeId", captured.Item.id) });
                });

                _itemsContainer.Add(card);
            }
        }

        private static System.Collections.Generic.IEnumerable<string> BuildBadgeStrings(Recipe r)
        {
            if (r?.dietaryLabels == null) yield break;
            foreach (var label in r.dietaryLabels) yield return label;
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
                FMLoadingOverlay.Show(contentContainer);
            else
                FMLoadingOverlay.Hide(contentContainer);
        }

        private void UpdateLoadingMoreState()
        {
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
