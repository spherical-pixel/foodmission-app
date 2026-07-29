using System;
using System.ComponentModel;
using Unity.AppUI.UI;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Scripting;
using eu.foodmission.platform;
using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    [Preserve]
    public class RecipeEditorScreen : StepFlowScreenBase<RecipeEditorViewModel>
    {
        protected override int StepCount => 3;

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            return stepIndex switch
            {
                0 => contentContainer.Q<VisualElement>("step-1") ?? new VisualElement(),
                1 => contentContainer.Q<VisualElement>("step-2") ?? new VisualElement(),
                2 => contentContainer.Q<VisualElement>("step-3") ?? new VisualElement(),
                _ => new VisualElement()
            };
        }

        private Unity.AppUI.UI.Button _saveButton;
        private Unity.AppUI.UI.Button _addFreeTextButton;
        private UnityEngine.UIElements.TextField _freeTextName;
        private UnityEngine.UIElements.TextField _freeTextMeasure;
        private VisualElement _ingredientsContainer;
        private VisualElement _noIngredientsWarning;
        private FMSearchOrCategoryField _searchIngredientsField;
        private FMStepProgressBar _stepProgressBar;

        public RecipeEditorScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.RecipeEditor));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _saveButton = contentContainer.Q<Unity.AppUI.UI.Button>("btn-save");
            _addFreeTextButton = contentContainer.Q<Unity.AppUI.UI.Button>("btn-add-free-text");
            _freeTextName = contentContainer.Q<UnityEngine.UIElements.TextField>("free-text-name");
            _freeTextMeasure = contentContainer.Q<UnityEngine.UIElements.TextField>("free-text-measure");
            _ingredientsContainer = contentContainer.Q<VisualElement>("ingredients-container");
            _noIngredientsWarning = contentContainer.Q<VisualElement>("no-ingredients-warning");
            _searchIngredientsField = contentContainer.Q<FMSearchOrCategoryField>("search-ingredients-field");
            _stepProgressBar = contentContainer.Q<FMStepProgressBar>("step-progress-bar");
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            string recipeId = ExtractArg(args, "recipeId");
            string editMode = ExtractArg(args, "editMode");
            if (!string.IsNullOrEmpty(recipeId) && editMode == "true")
                _ = SafeLoadForEditAsync(recipeId);
        }

        private static string ExtractArg(Argument[] args, string name)
        {
            if (args == null) return null;
            foreach (var a in args)
                if (a.name == name) return a.value as string;
            return null;
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_saveButton != null)
                _saveButton.clickable.clicked += OnSaveClicked;

            if (_addFreeTextButton != null)
                _addFreeTextButton.clickable.clicked += OnAddFreeTextClicked;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            if (_searchIngredientsField != null)
            {
                _searchIngredientsField.OnProductConfirmed = async (product, qty, unit) =>
                {
                    _viewModel.AddIngredientFromProduct(product.id, product.name ?? product.genericName ?? "Product", unit);
                    RebuildIngredients();
                };
                _searchIngredientsField.OnGenericFoodConfirmed = async (food, qty, unit) =>
                {
                    _viewModel.AddIngredientFromGenericFood(food.id, food.foodName ?? "Ingredient", unit);
                    RebuildIngredients();
                };
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            if (_saveButton != null)
                _saveButton.clickable.clicked -= OnSaveClicked;

            if (_addFreeTextButton != null)
                _addFreeTextButton.clickable.clicked -= OnAddFreeTextClicked;

            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.CurrentStepIndex):
                    if (_stepProgressBar != null)
                        _stepProgressBar.CurrentStep = _viewModel.CurrentStepIndex;
                    break;
                case nameof(_viewModel.Ingredients):
                    RebuildIngredients();
                    break;
                case nameof(_viewModel.HasNoIngredientsWarning):
                    if (_noIngredientsWarning != null)
                        _noIngredientsWarning.style.display = _viewModel.HasNoIngredientsWarning
                            ? DisplayStyle.Flex : DisplayStyle.None;
                    break;
                case nameof(_viewModel.IsSaving):
                    _saveButton?.SetEnabled(!_viewModel.IsSaving);
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateErrorState();
                    break;
            }
        }

        private void RebuildIngredients()
        {
            if (_ingredientsContainer == null) return;
            _ingredientsContainer.Clear();
            if (_viewModel.Ingredients == null) return;

            for (int i = 0; i < _viewModel.Ingredients.Count; i++)
            {
                var captured = _viewModel.Ingredients[i];
                var idx = i;

                var ingredientRow = new FMItemRecipeIngredient
                {
                    NameText = captured.Name,
                    MeasureText = captured.Measure
                };

                ingredientRow.RemoveButton.clickable.clicked += () =>
                {
                    _viewModel.RemoveIngredient(idx);
                    RebuildIngredients();
                };

                _ingredientsContainer.Add(ingredientRow);
            }
        }

        private void OnAddFreeTextClicked()
        {
            var name = _freeTextName?.value;
            var measure = _freeTextMeasure?.value;
            if (string.IsNullOrWhiteSpace(name)) return;
            _viewModel.AddFreeTextIngredient(name, measure);
            if (_freeTextName != null) _freeTextName.SetValueWithoutNotify("");
            if (_freeTextMeasure != null) _freeTextMeasure.SetValueWithoutNotify("");
            RebuildIngredients();
        }

        private void OnSaveClicked() => _ = SafeSaveAsync();

        private async System.Threading.Tasks.Task SafeSaveAsync()
        {
            try { await _viewModel.SaveAsync(); }
            catch (Exception ex) { Debug.LogError($"[RecipeEditorScreen] SafeSaveAsync: {ex}"); }
        }

        private async System.Threading.Tasks.Task SafeLoadForEditAsync(string recipeId)
        {
            try { await _viewModel.LoadForEditAsync(recipeId); }
            catch (Exception ex) { Debug.LogError($"[RecipeEditorScreen] SafeLoadForEditAsync: {ex}"); }
        }

        private void UpdateErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, "RECIPE_ERROR_SAVE", _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private UnityEngine.Accessibility.AccessibilityNode _saveNode;

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;
            if (_saveButton != null)
            {
                _saveNode = _accessibilityHierarchy.AddNode("Save recipe");
                _saveNode.role = UnityEngine.Accessibility.AccessibilityRole.Button;
            }
        }

        protected override void TeardownAccessibilityNodes()
        {
            _saveNode = null;
            base.TeardownAccessibilityNodes();
        }
    }
}
