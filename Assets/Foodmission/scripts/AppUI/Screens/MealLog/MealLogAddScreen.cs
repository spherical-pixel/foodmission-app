using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [Preserve]
    class MealLogAddScreen : NavigationScreenBase<MealLogAddViewModel>
    {
        private Unity.AppUI.UI.TextField _mealSearchField;
        private UnityEngine.UIElements.TextField _mealSearchInnerField;
        private VisualElement _mealSearchResults;
        private Unity.AppUI.UI.Dropdown _typeDropdown;
        private Unity.AppUI.UI.Toggle _toggleFromPantry;
        private Unity.AppUI.UI.Toggle _toggleEatenOut;
        private VisualElement _deductionSection;
        private VisualElement _deductionContainer;
        private Unity.AppUI.UI.Button _btnCreateMeal;
        private Unity.AppUI.UI.Button _btnSave;
        private CircularProgress _searchSpinner;
        private Text _errorText;
        private CancellationTokenSource _searchCts;

        public MealLogAddScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.MealLogAdd));
            CacheUIElements();
            SetupDropdown();
        }

        private void CacheUIElements()
        {
            _mealSearchField = contentContainer.Q<Unity.AppUI.UI.TextField>("meal-search-field");
            Debug.Log($"[{GetType().Name}] CacheUIElements _mealSearchField={(object)_mealSearchField ?? "NULL"}");
            _mealSearchResults = contentContainer.Q<VisualElement>("meal-search-results");
            _typeDropdown = contentContainer.Q<Unity.AppUI.UI.Dropdown>("type-of-meal-dropdown");
            _toggleFromPantry = contentContainer.Q<Unity.AppUI.UI.Toggle>("toggle-from-pantry");
            _toggleEatenOut = contentContainer.Q<Unity.AppUI.UI.Toggle>("toggle-eaten-out");
            _deductionSection = contentContainer.Q<VisualElement>("deduction-section");
            _deductionContainer = contentContainer.Q<VisualElement>("deduction-container");
            _btnCreateMeal = contentContainer.Q<Unity.AppUI.UI.Button>("btn-create-meal");
            _btnSave = contentContainer.Q<Unity.AppUI.UI.Button>("btn-save");
            _searchSpinner = contentContainer.Q<CircularProgress>("search-spinner");
            _errorText = contentContainer.Q<Text>("error-message");
        }

        private void SetupDropdown()
        {
            if (_typeDropdown == null) return;
            _typeDropdown.sourceItems = MealLogAddViewModel.TypeOfMealOptions;
            _typeDropdown.SetValueWithoutNotify(new[] { 0 });
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            Debug.Log($"[{GetType().Name}] OnViewModelBound _mealSearchField={(object)_mealSearchField ?? "NULL"}");

            _viewModel.Reset();
            _mealSearchResults?.Clear();

            _mealSearchField?.schedule.Execute(() =>
            {
                _mealSearchInnerField = _mealSearchField.Q<UnityEngine.UIElements.TextField>();
                if (_mealSearchInnerField != null)
                    _mealSearchInnerField.RegisterValueChangedCallback(OnSearchChanged);
            }).ExecuteLater(0);
            _toggleFromPantry?.RegisterValueChangedCallback(OnFromPantryChanged);
            _btnCreateMeal?.RegisterCallback<ClickEvent>(OnCreateMealClicked);
            _btnSave?.RegisterCallback<ClickEvent>(OnSaveClicked);
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            UpdateDeductionVisibility();
            UpdateSelectedMealState();
            UpdateCreateMealButton();
        }

        protected override void OnViewModelUnbinding()
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = null;

            if (_btnCreateMeal != null)
                _btnCreateMeal.UnregisterCallback<ClickEvent>(OnCreateMealClicked);
            if (_btnSave != null)
                _btnSave.UnregisterCallback<ClickEvent>(OnSaveClicked);
            if (_mealSearchInnerField != null)
            {
                _mealSearchInnerField.UnregisterValueChangedCallback(OnSearchChanged);
                _mealSearchInnerField = null;
            }
            if (_toggleFromPantry != null)
                _toggleFromPantry.UnregisterValueChangedCallback(OnFromPantryChanged);
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            base.OnViewModelUnbinding();
        }

        private async void OnSearchChanged(ChangeEvent<string> evt)
        {
            string query = evt.newValue;

            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            CancellationToken ct = _searchCts.Token;

            if (string.IsNullOrWhiteSpace(query))
            {
                _mealSearchResults?.Clear();
                _btnCreateMeal?.EnableInClassList("visible", false);
                return;
            }

            try
            {
                await Task.Delay(400, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (ct.IsCancellationRequested) return;

            Debug.Log($"[{GetType().Name}] OnSearchChanged showing spinner, calling SearchMealsAsync");

            _searchSpinner?.EnableInClassList("visible", true);

            await _viewModel.SearchMealsAsync(query);

            Debug.Log($"[{GetType().Name}] OnSearchChanged SearchMealsAsync returned");

            if (ct.IsCancellationRequested) return;

            _searchSpinner?.EnableInClassList("visible", false);
        }

        private async void OnFromPantryChanged(ChangeEvent<bool> evt)
        {
            try
            {
                _viewModel.MealFromPantry = evt.newValue;
                if (evt.newValue)
                {
                    await _viewModel.LoadPantryForDeductionAsync();
                }
                UpdateDeductionVisibility();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] OnFromPantryChanged failed: {ex.Message}");
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.Log($"[{GetType().Name}] PropertyChanged: {e.PropertyName}");
            switch (e.PropertyName)
            {
                case nameof(_viewModel.MealSearchResults):
                    RebuildSearchResults();
                    break;
                case nameof(_viewModel.IsSearchingMeals):
                    UpdateSearchingState();
                    break;
                case nameof(_viewModel.HasSelectedMeal):
                case nameof(_viewModel.SelectedMealName):
                    UpdateSelectedMealState();
                    break;
                case nameof(_viewModel.PantryDeductions):
                    RebuildDeductionItems();
                    break;
                case nameof(_viewModel.IsSaving):
                    UpdateSavingState();
                    break;
                case nameof(_viewModel.CanCreateMeal):
                    UpdateCreateMealButton();
                    break;
                case nameof(_viewModel.IsCreatingMeal):
                    UpdateCreatingState();
                    break;
                case nameof(_viewModel.ErrorMessage):
                    UpdateErrorState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
            }
        }

        private void RebuildSearchResults()
        {
            Debug.Log($"[{GetType().Name}] RebuildSearchResults container={_mealSearchResults != null} count={_viewModel.MealSearchResults?.Count ?? -1}");

            if (_mealSearchResults == null)
            {
                Debug.LogError($"[{GetType().Name}] RebuildSearchResults _mealSearchResults is NULL!");
                return;
            }

            _mealSearchResults.Clear();

            if (_viewModel.MealSearchResults == null || _viewModel.MealSearchResults.Count == 0)
                return;

            foreach (Meal meal in _viewModel.MealSearchResults)
            {
                Meal captured = meal;
                var row = new VisualElement();
                row.AddToClassList("fm-mla-search-result-row");

                var nameLabel = new Text { text = captured.name };
                nameLabel.AddToClassList("fm-mla-search-result-name");

                row.Add(nameLabel);
                row.RegisterCallback<ClickEvent>(_ => _viewModel.SelectMeal(captured));

                _mealSearchResults.Add(row);
            }

            Debug.Log($"[{GetType().Name}] RebuildSearchResults added {_viewModel.MealSearchResults.Count} rows");
        }

        private void UpdateSearchingState()
        {
            bool isSearching = _viewModel.IsSearchingMeals;
            _searchSpinner?.EnableInClassList("visible", isSearching);
        }

        private void UpdateSelectedMealState()
        {
            if (_viewModel.HasSelectedMeal)
            {
                _searchCts?.Cancel();
                _searchCts?.Dispose();
                _searchCts = null;

                _mealSearchField?.SetValueWithoutNotify(_viewModel.SelectedMealName);
                _mealSearchField?.SetEnabled(false);
                _mealSearchResults?.Clear();
            }
            else
            {
                _mealSearchField?.SetValueWithoutNotify("");
                _mealSearchField?.SetEnabled(true);
            }
        }

        private void UpdateDeductionVisibility()
        {
            bool show = _viewModel.MealFromPantry;
            _deductionSection?.EnableInClassList("visible", show);
        }

        private void RebuildDeductionItems()
        {
            _deductionContainer?.Clear();

            if (_viewModel.PantryDeductions == null || _viewModel.PantryDeductions.Count == 0)
                return;

            foreach (PantryDeduction d in _viewModel.PantryDeductions)
            {
                int idx = _viewModel.PantryDeductions.IndexOf(d);
                PantryDeduction captured = d;

                var row = new VisualElement();
                row.AddToClassList("fm-mla-deduction-item");

                var info = new VisualElement();
                info.AddToClassList("fm-mla-deduction-item-info");

                var nameLabel = new Text { text = captured.FoodName };
                nameLabel.AddToClassList("fm-mla-deduction-item-name");

                string availStr = $"{captured.AvailableQuantity:0.##} {captured.Unit} available";
                var availLabel = new Text { text = availStr };
                availLabel.AddToClassList("fm-mla-deduction-item-detail");

                info.Add(nameLabel);
                info.Add(availLabel);

                var qtyField = new Unity.AppUI.UI.FloatField { value = 0f };
                qtyField.AddToClassList("fm-mla-deduction-item-qty");
                qtyField.RegisterValueChangedCallback(evt =>
                {
                    _viewModel.PantryDeductions[idx].Quantity = evt.newValue;
                });

                row.Add(info);
                row.Add(qtyField);
                _deductionContainer.Add(row);
            }
        }

        private void UpdateSavingState()
        {
            bool isSaving = _viewModel.IsSaving;
            if (isSaving)
                FMLoadingOverlay.Show(contentContainer);
            else
                FMLoadingOverlay.Hide(contentContainer);
            _btnSave?.SetEnabled(!isSaving);
            _btnCreateMeal?.SetEnabled(!isSaving);
            _mealSearchField?.SetEnabled(!isSaving);
            _typeDropdown?.SetEnabled(!isSaving);
            _toggleFromPantry?.SetEnabled(!isSaving);
            _toggleEatenOut?.SetEnabled(!isSaving);
        }

        private void UpdateErrorState()
        {
            bool hasError = !string.IsNullOrEmpty(_viewModel.ErrorMessage);
            _errorText?.EnableInClassList("visible", hasError);
            if (_errorText != null)
                _errorText.text = _viewModel.ErrorMessage;
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private async void OnCreateMealClicked(ClickEvent evt)
        {
            try
            {
                string name = _mealSearchField?.value?.Trim();
                await _viewModel.CreateAndSelectMealAsync(name);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] OnCreateMealClicked failed: {ex.Message}");
            }
        }

        private void UpdateCreateMealButton()
        {
            Debug.Log($"[{GetType().Name}] UpdateCreateMealButton CanCreateMeal={_viewModel.CanCreateMeal} _btnCreateMeal={_btnCreateMeal != null}");
            _btnCreateMeal?.EnableInClassList("visible", _viewModel.CanCreateMeal);
        }

        private void UpdateCreatingState()
        {
            bool isCreating = _viewModel.IsCreatingMeal;
            _btnCreateMeal?.SetEnabled(!isCreating);
        }

        private async void OnSaveClicked(ClickEvent evt)
        {
            try
            {
                if (_typeDropdown != null && _typeDropdown.selectedIndex >= 0)
                {
                    _viewModel.SelectedTypeOfMealIndex = _typeDropdown.selectedIndex;
                }

                bool success = await _viewModel.SaveAsync();
                if (success)
                {
                    _navController?.PopBackStack();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] OnSaveClicked failed: {ex.Message}");
                _viewModel.ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNEXPECTED_ERROR_OCCURRED");
            }
        }
    }
}
