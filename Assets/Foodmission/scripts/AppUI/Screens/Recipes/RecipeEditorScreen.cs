using System;
using System.ComponentModel;
using System.Threading.Tasks;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    public class RecipeEditorScreen : NavigationScreenBase<RecipeEditorViewModel>
    {
        protected override bool IsFixedContent => true;
        protected override bool ApplySafeAreaBottom => false;

        private Heading _screenTitle;
        private FMStepProgressBar _stepProgressBar;
        private VisualElement _step1Container;
        private VisualElement _step2Container;
        private VisualElement _step3Container;

        // Step 1: Meta
        private Unity.AppUI.UI.TextField _titleField;
        private Unity.AppUI.UI.TextArea _descriptionField;
        private Unity.AppUI.UI.TextField _prepTimeField;
        private Unity.AppUI.UI.TextField _cookTimeField;
        private Unity.AppUI.UI.TextField _servingsField;
        private Unity.AppUI.UI.TextField _difficultyField;
        private Unity.AppUI.UI.TextField _categoryField;
        private Unity.AppUI.UI.TextField _cuisineField;
        private Unity.AppUI.UI.Checkbox _isPublicCheckbox;

        // Step 2: Ingredients
        private FMSearchOrCategoryField _searchIngredientsField;
        private Unity.AppUI.UI.TextField _freeTextName;
        private Unity.AppUI.UI.TextField _freeTextMeasure;
        private FMButton _btnAddFreeText;
        private Unity.AppUI.UI.Text _noIngredientsWarning;
        private VisualElement _ingredientsContainer;

        // Step 3: Review / Instructions
        private Unity.AppUI.UI.TextArea _instructionsField;
        private Unity.AppUI.UI.TextField _imageUrlField;

        // Navigation
        private FMButton _btnStepPrev;
        private FMButton _btnStepNext;

        // Accessibility
        private AccessibilityNode _prevButtonNode;
        private AccessibilityNode _nextButtonNode;
        private AccessibilityNode _addIngredientNode;

        public RecipeEditorScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.RecipeEditor));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _screenTitle = contentContainer.Q<Heading>("screen-title");
            _stepProgressBar = contentContainer.Q<FMStepProgressBar>("step-progress-bar");
            _step1Container = contentContainer.Q<VisualElement>("step-1");
            _step2Container = contentContainer.Q<VisualElement>("step-2");
            _step3Container = contentContainer.Q<VisualElement>("step-3");

            // Step 1
            _titleField = contentContainer.Q<Unity.AppUI.UI.TextField>("title-field");
            _descriptionField = contentContainer.Q<Unity.AppUI.UI.TextArea>("description-field");
            _prepTimeField = contentContainer.Q<Unity.AppUI.UI.TextField>("prep-time-field");
            _cookTimeField = contentContainer.Q<Unity.AppUI.UI.TextField>("cook-time-field");
            _servingsField = contentContainer.Q<Unity.AppUI.UI.TextField>("servings-field");
            _difficultyField = contentContainer.Q<Unity.AppUI.UI.TextField>("difficulty-field");
            _categoryField = contentContainer.Q<Unity.AppUI.UI.TextField>("category-field");
            _cuisineField = contentContainer.Q<Unity.AppUI.UI.TextField>("cuisine-field");
            _isPublicCheckbox = contentContainer.Q<Unity.AppUI.UI.Checkbox>("is-public-checkbox");

            // Step 2
            _searchIngredientsField = contentContainer.Q<FMSearchOrCategoryField>("search-ingredients-field");
            _freeTextName = contentContainer.Q<Unity.AppUI.UI.TextField>("free-text-name");
            _freeTextMeasure = contentContainer.Q<Unity.AppUI.UI.TextField>("free-text-measure");
            _btnAddFreeText = contentContainer.Q<FMButton>("btn-add-free-text");
            _noIngredientsWarning = contentContainer.Q<Unity.AppUI.UI.Text>("no-ingredients-warning");
            _ingredientsContainer = contentContainer.Q<VisualElement>("ingredients-container");

            // Step 3
            _instructionsField = contentContainer.Q<Unity.AppUI.UI.TextArea>("instructions-field");
            _imageUrlField = contentContainer.Q<Unity.AppUI.UI.TextField>("image-url-field");

            // Nav bar
            _btnStepPrev = contentContainer.Q<FMButton>("btn-step-prev");
            _btnStepNext = contentContainer.Q<FMButton>("btn-step-next");
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            string recipeId = ExtractArg(args, "recipeId");
            string editMode = ExtractArg(args, "editMode");

            if (!string.IsNullOrEmpty(recipeId) && editMode == "true")
            {
                _ = SafeLoadForEditAsync(recipeId);
            }
            else
            {
                _viewModel.Reset();
                PopulateFieldsFromViewModel();
            }

            ConfigureStepProgressBar();
            UpdateStepUI(_viewModel.CurrentStepIndex);
        }

        private static string ExtractArg(Argument[] args, string name)
        {
            if (args == null) return null;
            foreach (var a in args)
                if (a.name == name) return a.value as string;
            return null;
        }

        private void ConfigureStepProgressBar()
        {
            if (_stepProgressBar == null) return;
            _stepProgressBar.StepCount = 3;
            _stepProgressBar.Labels = new[]
            {
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_STEP_GENERAL", null, FallbackBehavior.UseProjectSettings, "General"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_STEP_INGREDIENTS", null, FallbackBehavior.UseProjectSettings, "Ingredientes"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_STEP_INSTRUCTIONS", null, FallbackBehavior.UseProjectSettings, "Elaboración")
            };
            _stepProgressBar.Mode = StepProgressMode.Detailed;
            _stepProgressBar.CurrentStep = _viewModel?.CurrentStepIndex ?? 0;
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            // Step 1 input bindings
            _titleField?.RegisterValueChangedCallback(evt => _viewModel.Title = evt.newValue);
            _descriptionField?.RegisterValueChangedCallback(evt => _viewModel.Description = evt.newValue);
            _prepTimeField?.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out var v)) _viewModel.PrepTime = v;
                else _viewModel.PrepTime = null;
            });
            _cookTimeField?.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out var v)) _viewModel.CookTime = v;
                else _viewModel.CookTime = null;
            });
            _servingsField?.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out var v)) _viewModel.Servings = v;
                else _viewModel.Servings = null;
            });
            _difficultyField?.RegisterValueChangedCallback(evt => _viewModel.Difficulty = NormalizeDifficulty(evt.newValue));
            _categoryField?.RegisterValueChangedCallback(evt => _viewModel.Category = evt.newValue);
            _cuisineField?.RegisterValueChangedCallback(evt => _viewModel.CuisineType = evt.newValue);
            _isPublicCheckbox?.RegisterValueChangedCallback(evt => _viewModel.IsPublic = evt.newValue == CheckboxState.Checked);

            // Step 2 input bindings
            if (_btnAddFreeText != null)
                _btnAddFreeText.clicked += OnAddFreeTextClicked;

            if (_searchIngredientsField != null)
            {
                _searchIngredientsField.OnProductConfirmed = async (prod, qty, unit) =>
                {
                    _viewModel.AddIngredientFromProduct(prod.id, prod.name ?? prod.genericName ?? "Product", unit);
                    RebuildIngredients();
                    await Task.CompletedTask;
                };
                _searchIngredientsField.OnGenericFoodConfirmed = async (food, qty, unit) =>
                {
                    _viewModel.AddIngredientFromGenericFood(food.id, food.foodName ?? "Ingredient", unit);
                    RebuildIngredients();
                    await Task.CompletedTask;
                };
            }

            // Step 3 input bindings
            _instructionsField?.RegisterValueChangedCallback(evt => _viewModel.Instructions = evt.newValue);
            _imageUrlField?.RegisterValueChangedCallback(evt => _viewModel.ImageUrl = evt.newValue);

            // Nav buttons
            if (_btnStepPrev != null)
                _btnStepPrev.clicked += OnPrevClicked;

            if (_btnStepNext != null)
                _btnStepNext.clicked += OnNextClicked;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            PopulateFieldsFromViewModel();
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            if (_btnAddFreeText != null)
                _btnAddFreeText.clicked -= OnAddFreeTextClicked;

            if (_btnStepPrev != null)
                _btnStepPrev.clicked -= OnPrevClicked;

            if (_btnStepNext != null)
                _btnStepNext.clicked -= OnNextClicked;

            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.CurrentStepIndex):
                case nameof(_viewModel.IsLastStep):
                case nameof(_viewModel.IsFirstStep):
                    UpdateStepUI(_viewModel.CurrentStepIndex);
                    break;
                case nameof(_viewModel.CanGoNext):
                    _btnStepNext?.SetEnabled(_viewModel.CanGoNext && !_viewModel.IsSaving);
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
                    _btnStepNext?.SetEnabled(_viewModel.CanGoNext && !_viewModel.IsSaving);
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateErrorState();
                    break;
                case nameof(_viewModel.EditingRecipeId):
                    UpdateScreenTitle();
                    break;
            }
        }

        private void PopulateFieldsFromViewModel()
        {
            if (_viewModel == null) return;

            UpdateScreenTitle();
            _titleField?.SetValueWithoutNotify(_viewModel.Title ?? "");
            _descriptionField?.SetValueWithoutNotify(_viewModel.Description ?? "");
            _prepTimeField?.SetValueWithoutNotify(_viewModel.PrepTime?.ToString() ?? "");
            _cookTimeField?.SetValueWithoutNotify(_viewModel.CookTime?.ToString() ?? "");
            _servingsField?.SetValueWithoutNotify(_viewModel.Servings?.ToString() ?? "");
            _difficultyField?.SetValueWithoutNotify(_viewModel.Difficulty ?? "");
            _categoryField?.SetValueWithoutNotify(_viewModel.Category ?? "");
            _cuisineField?.SetValueWithoutNotify(_viewModel.CuisineType ?? "");
            _isPublicCheckbox?.SetValueWithoutNotify(_viewModel.IsPublic ? CheckboxState.Checked : CheckboxState.Unchecked);
            _instructionsField?.SetValueWithoutNotify(_viewModel.Instructions ?? "");
            _imageUrlField?.SetValueWithoutNotify(_viewModel.ImageUrl ?? "");

            RebuildIngredients();
            UpdateStepUI(_viewModel.CurrentStepIndex);
        }

        private void UpdateScreenTitle()
        {
            if (_screenTitle == null) return;
            _screenTitle.text = _viewModel.IsEditMode
                ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_EDIT_TITLE", null, FallbackBehavior.UseProjectSettings, "Editar Receta")
                : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_CREATE_TITLE", null, FallbackBehavior.UseProjectSettings, "Crear Receta");
        }

        private void UpdateStepUI(int stepIndex)
        {
            if (_step1Container != null)
                _step1Container.style.display = (stepIndex == 0) ? DisplayStyle.Flex : DisplayStyle.None;
            if (_step2Container != null)
                _step2Container.style.display = (stepIndex == 1) ? DisplayStyle.Flex : DisplayStyle.None;
            if (_step3Container != null)
                _step3Container.style.display = (stepIndex == 2) ? DisplayStyle.Flex : DisplayStyle.None;

            if (_stepProgressBar != null)
            {
                _stepProgressBar.CurrentStep = stepIndex;
            }

            if (_btnStepPrev != null)
            {
                _btnStepPrev.style.visibility = _viewModel.IsFirstStep ? Visibility.Hidden : Visibility.Visible;
                _btnStepPrev.title = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TXT_BACK", null, FallbackBehavior.UseProjectSettings, "Anterior");
            }

            if (_btnStepNext != null)
            {
                _btnStepNext.title = _viewModel.IsLastStep
                    ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPES_SAVE_BTN", null, FallbackBehavior.UseProjectSettings, "Guardar Receta")
                    : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TXT_NEXT", null, FallbackBehavior.UseProjectSettings, "Siguiente");
                _btnStepNext.SetEnabled(_viewModel.CanGoNext && !_viewModel.IsSaving);
            }
        }

        private async void OnPrevClicked()
        {
            try
            {
                await _viewModel.GoPreviousAsync();
                UpdateStepUI(_viewModel.CurrentStepIndex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RecipeEditorScreen] OnPrevClicked: {ex}");
            }
        }

        private async void OnNextClicked()
        {
            try
            {
                await _viewModel.GoNextAsync();
                UpdateStepUI(_viewModel.CurrentStepIndex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RecipeEditorScreen] OnNextClicked: {ex}");
            }
        }

        private void RebuildIngredients()
        {
            if (_ingredientsContainer == null) return;
            _ingredientsContainer.Clear();
            if (_viewModel?.Ingredients == null) return;

            if (_noIngredientsWarning != null)
            {
                _noIngredientsWarning.style.display = _viewModel.Ingredients.Count == 0
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }

            for (int i = 0; i < _viewModel.Ingredients.Count; i++)
            {
                var captured = _viewModel.Ingredients[i];
                var idx = i;

                var ingredientRow = new FMItemRecipeIngredient
                {
                    NameText = captured.Name,
                    MeasureText = captured.Measure
                };

                if (ingredientRow.RemoveButton != null)
                {
                    ingredientRow.RemoveButton.clicked += () =>
                    {
                        _viewModel.RemoveIngredient(idx);
                        RebuildIngredients();
                    };
                }

                _ingredientsContainer.Add(ingredientRow);
            }
        }

        private void OnAddFreeTextClicked()
        {
            var name = _freeTextName?.value;
            var measure = _freeTextMeasure?.value;
            if (string.IsNullOrWhiteSpace(name)) return;
            _viewModel.AddFreeTextIngredient(name.Trim(), string.IsNullOrWhiteSpace(measure) ? null : measure.Trim());
            _freeTextName?.SetValueWithoutNotify("");
            _freeTextMeasure?.SetValueWithoutNotify("");
            RebuildIngredients();
        }

        private async Task SafeLoadForEditAsync(string recipeId)
        {
            try
            {
                await _viewModel.LoadForEditAsync(recipeId);
                PopulateFieldsFromViewModel();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RecipeEditorScreen] SafeLoadForEditAsync: {ex}");
            }
        }

        private void UpdateErrorState()
        {
            if (_viewModel?.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, "RECIPE_ERROR_SAVE", _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private static string NormalizeDifficulty(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var lower = input.Trim().ToLowerInvariant();
            if (lower.Contains("fácil") || lower.Contains("facil") || lower == "easy") return "easy";
            if (lower.Contains("media") || lower.Contains("medio") || lower == "medium") return "medium";
            if (lower.Contains("difícil") || lower.Contains("dificil") || lower == "hard") return "hard";
            return lower;
        }

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            if (_btnStepNext != null)
            {
                _nextButtonNode = _accessibilityHierarchy.AddNode("Next step or save recipe");
                _nextButtonNode.role = AccessibilityRole.Button;
            }

            if (_btnStepPrev != null)
            {
                _prevButtonNode = _accessibilityHierarchy.AddNode("Previous step");
                _prevButtonNode.role = AccessibilityRole.Button;
            }

            if (_btnAddFreeText != null)
            {
                _addIngredientNode = _accessibilityHierarchy.AddNode("Add free text ingredient");
                _addIngredientNode.role = AccessibilityRole.Button;
            }
        }

        protected override void TeardownAccessibilityNodes()
        {
            _nextButtonNode = null;
            _prevButtonNode = null;
            _addIngredientNode = null;
            base.TeardownAccessibilityNodes();
        }
    }
}

