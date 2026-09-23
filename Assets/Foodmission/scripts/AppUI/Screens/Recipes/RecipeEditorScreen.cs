using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        protected override bool ApplySafeAreaTop => false;

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
        private Unity.AppUI.UI.Dropdown _difficultyDropdown;
        private Unity.AppUI.UI.Dropdown _categoryDropdown;
        private Unity.AppUI.UI.Dropdown _cuisineDropdown;
        private Unity.AppUI.UI.Checkbox _isPublicCheckbox;

        private readonly List<string> _difficultyOptions = new();
        private readonly List<string> _difficultyCodes = new();
        private readonly List<string> _categoryOptions = new();
        private readonly List<string> _categoryCodes = new();
        private readonly List<string> _cuisineOptions = new();
        private readonly List<string> _cuisineCodes = new();

        // Step 2: Ingredients
        private FMSearchOrCategoryField _searchIngredientsField;
        private Unity.AppUI.UI.Text _noIngredientsWarning;
        private VisualElement _ingredientsContainer;
        private ScrollView _ingredientsScroll;

        // Step 3: Review / Instructions
        private Unity.AppUI.UI.TextArea _instructionsField;
        private Unity.AppUI.UI.TextField _imageUrlField;

        // Navigation
        private FMButton _btnStepPrev;
        private FMButton _btnStepNext;

        // Accessibility
        private AccessibilityNode _prevButtonNode;
        private AccessibilityNode _nextButtonNode;

        public RecipeEditorScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.RecipeEditor));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
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
            _difficultyDropdown = contentContainer.Q<Unity.AppUI.UI.Dropdown>("difficulty-dropdown");
            _categoryDropdown = contentContainer.Q<Unity.AppUI.UI.Dropdown>("category-dropdown");
            _cuisineDropdown = contentContainer.Q<Unity.AppUI.UI.Dropdown>("cuisine-dropdown");
            _isPublicCheckbox = contentContainer.Q<Unity.AppUI.UI.Checkbox>("is-public-checkbox");

            // Step 2
            _searchIngredientsField = contentContainer.Q<FMSearchOrCategoryField>("search-ingredients-field");
            _noIngredientsWarning = contentContainer.Q<Unity.AppUI.UI.Text>("no-ingredients-warning");
            _ingredientsContainer = contentContainer.Q<VisualElement>("ingredients-container");
            _ingredientsScroll = contentContainer.Q<ScrollView>("ingredients-scroll");

            // Step 3
            _instructionsField = contentContainer.Q<Unity.AppUI.UI.TextArea>("instructions-field");
            _imageUrlField = contentContainer.Q<Unity.AppUI.UI.TextField>("image-url-field");

            // Nav bar
            _btnStepPrev = contentContainer.Q<FMButton>("btn-step-prev");
            _btnStepNext = contentContainer.Q<FMButton>("btn-step-next");

            if (_btnStepPrev != null)
                _btnStepPrev.AddToClassList("fm-re-nav-btn--prev");
            if (_btnStepNext != null)
                _btnStepNext.AddToClassList("fm-re-nav-btn--next");
        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            if (_viewModel == null) return;

            string recipeId = ExtractArg(args, "recipeId");
            string editMode = ExtractArg(args, "editMode");

            if (!string.IsNullOrEmpty(recipeId) && editMode == "true")
            {
                await SafeLoadForEditAsync(recipeId);
            }
            else
            {
                _viewModel.Reset();
                PopulateFieldsFromViewModel();
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel == null) return;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            if (_stepProgressBar != null)
            {
                _stepProgressBar.Labels = new[]
                {
                    L("RECIPES_STEP_GENERAL", "General"),
                    L("RECIPES_STEP_INGREDIENTS", "Ingredientes"),
                    L("RECIPES_STEP_INSTRUCTIONS", "Elaboración")
                };
                _stepProgressBar.Mode = StepProgressMode.Detailed;
                _stepProgressBar.StepCount = 3;
            }

            if (_searchIngredientsField != null)
            {
                _searchIngredientsField.SearchProductsAsync = query => _viewModel.SearchFoodsAsync(query);
                _searchIngredientsField.GetGenericFoodsAsync = () => _viewModel.GetGenericFoodsAsync();
                _searchIngredientsField.SearchGenericFoodsAsync = query => _viewModel.SearchGenericFoodsAsync(query);
                _searchIngredientsField.SearchByFoodGroupAsync = (foodGroup, page, pageSize) => _viewModel.SearchByFoodGroupAsync(foodGroup, page, pageSize);
                _searchIngredientsField.ImportFromBarcodeAsync = barcode => _viewModel.ImportByBarcodeAsync(barcode);
                _searchIngredientsField.OnProductConfirmed = (product, qty, unit) =>
                {
                    string name = product.name ?? product.genericName ?? "Product";
                    ShowQuantityDialog(name, qty ?? 1f, unit ?? "PIECES", async (selectedQty, selectedUnit) =>
                    {
                        string measure = FormatMeasure(selectedQty, selectedUnit);
                        string resolvedFoodProductId = null;
                        if (Guid.TryParse(product.id, out _))
                        {
                            resolvedFoodProductId = product.id;
                        }
                        else if (!string.IsNullOrEmpty(product.barcode))
                        {
                            var (importedProduct, _) = await _viewModel.ImportByBarcodeAsync(product.barcode);
                            if (importedProduct != null && Guid.TryParse(importedProduct.id, out _))
                            {
                                resolvedFoodProductId = importedProduct.id;
                            }
                        }
                        _viewModel.AddIngredientFromProduct(resolvedFoodProductId, name, measure, selectedQty, selectedUnit);
                    });
                    return Task.CompletedTask;
                };
                _searchIngredientsField.OnGenericFoodConfirmed = (food, qty, unit) =>
                {
                    string name = food.foodName ?? "Ingredient";
                    ShowQuantityDialog(name, qty ?? 1f, unit ?? "PIECES", (selectedQty, selectedUnit) =>
                    {
                        string measure = FormatMeasure(selectedQty, selectedUnit);
                        _viewModel.AddIngredientFromGenericFood(food.id, name, measure, selectedQty, selectedUnit);
                    });
                    return Task.CompletedTask;
                };
                _searchIngredientsField.OnPopoverVisibilityChanged += isVisible =>
                {
                    if (_ingredientsScroll != null)
                    {
                        _ingredientsScroll.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
                    }
                };
            }

            // Bind inputs to ViewModel
            _titleField?.RegisterValueChangedCallback(evt => _viewModel.Title = evt.newValue);
            _descriptionField?.RegisterValueChangedCallback(evt => _viewModel.Description = evt.newValue);
            _prepTimeField?.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out int val) && val > 0) _viewModel.PrepTime = val;
                else _viewModel.PrepTime = null;
            });
            _cookTimeField?.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out int val) && val > 0) _viewModel.CookTime = val;
                else _viewModel.CookTime = null;
            });
            _servingsField?.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out int val) && val > 0) _viewModel.Servings = val;
                else _viewModel.Servings = null;
            });
            SetupDropdowns();

            _difficultyDropdown?.RegisterValueChangedCallback(OnDifficultyDropdownChanged);
            _categoryDropdown?.RegisterValueChangedCallback(OnCategoryDropdownChanged);
            _cuisineDropdown?.RegisterValueChangedCallback(OnCuisineDropdownChanged);
            _isPublicCheckbox?.RegisterValueChangedCallback(evt => _viewModel.IsPublic = evt.newValue == CheckboxState.Checked);

            _instructionsField?.RegisterValueChangedCallback(evt => _viewModel.Instructions = evt.newValue);
            _imageUrlField?.RegisterValueChangedCallback(evt => _viewModel.ImageUrl = evt.newValue);

            if (_btnStepPrev != null)
                _btnStepPrev.clicked += OnPrevClicked;

            if (_btnStepNext != null)
                _btnStepNext.clicked += OnNextClicked;

            PopulateFieldsFromViewModel();
            UpdateStepUI(_viewModel.CurrentStepIndex);
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            if (_btnStepPrev != null)
                _btnStepPrev.clicked -= OnPrevClicked;

            if (_btnStepNext != null)
                _btnStepNext.clicked -= OnNextClicked;

            _difficultyDropdown?.UnregisterValueChangedCallback(OnDifficultyDropdownChanged);
            _categoryDropdown?.UnregisterValueChangedCallback(OnCategoryDropdownChanged);
            _cuisineDropdown?.UnregisterValueChangedCallback(OnCuisineDropdownChanged);

            FMLoadingOverlay.Hide();
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.CurrentStepIndex):
                    UpdateStepUI(_viewModel.CurrentStepIndex);
                    break;
                case nameof(_viewModel.CanGoNext):
                case nameof(_viewModel.IsLastStep):
                case nameof(_viewModel.IsFirstStep):
                    UpdateNavigationControls();
                    break;
                case nameof(_viewModel.IsSaving):
                    UpdateNavigationControls();
                    if (_viewModel.IsSaving)
                        FMLoadingOverlay.Show(L("RECIPES_SAVING", "Guardando receta..."));
                    else
                        FMLoadingOverlay.Hide();
                    break;
                case nameof(_viewModel.Ingredients):
                    RebuildIngredients();
                    break;
                case nameof(_viewModel.HasNoIngredientsWarning):
                    UpdateNoIngredientsWarning();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateErrorState();
                    break;
                case nameof(_viewModel.IsEditMode):
                    UpdateScreenTitle();
                    break;
                case nameof(_viewModel.Difficulty):
                case nameof(_viewModel.Category):
                case nameof(_viewModel.CuisineType):
                    PopulateDropdownsFromViewModel();
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
            PopulateDropdownsFromViewModel();
            _isPublicCheckbox?.SetValueWithoutNotify(_viewModel.IsPublic ? CheckboxState.Checked : CheckboxState.Unchecked);
            _instructionsField?.SetValueWithoutNotify(_viewModel.Instructions ?? "");
            _imageUrlField?.SetValueWithoutNotify(_viewModel.ImageUrl ?? "");

            RebuildIngredients();
            UpdateStepUI(_viewModel.CurrentStepIndex);
        }

        private void SetupDropdowns()
        {
            SetupDifficultyDropdown();
            SetupCategoryDropdown();
            SetupCuisineDropdown();
        }

        private void SetupDifficultyDropdown()
        {
            if (_difficultyDropdown == null) return;
            _difficultyOptions.Clear();
            _difficultyCodes.Clear();

            _difficultyOptions.Add(L("RECIPES_FIELD_DIFFICULTY_HINT", "Selecciona dificultad..."));
            _difficultyCodes.Add("");

            _difficultyOptions.Add($"🟢 {L("RECIPES_DIFFICULTY_EASY", "Fácil")}");
            _difficultyCodes.Add("easy");

            _difficultyOptions.Add($"🟡 {L("RECIPES_DIFFICULTY_MEDIUM", "Media")}");
            _difficultyCodes.Add("medium");

            _difficultyOptions.Add($"🔴 {L("RECIPES_DIFFICULTY_HARD", "Difícil")}");
            _difficultyCodes.Add("hard");

            _difficultyDropdown.sourceItems = _difficultyOptions;
            _difficultyDropdown.bindItem = (item, index) =>
            {
                if (index >= 0 && index < _difficultyOptions.Count)
                {
                    item.label = _difficultyOptions[index];
                    item.icon = null;
                }
            };
        }

        private void SetupCategoryDropdown()
        {
            if (_categoryDropdown == null) return;
            _categoryOptions.Clear();
            _categoryCodes.Clear();

            _categoryOptions.Add(L("RECIPES_FIELD_CATEGORY_HINT", "Selecciona categoría..."));
            _categoryCodes.Add("");

            var sortedCategories = RecipeCatalogs.Categories
                .OrderBy(c => c.GetLocalizedName())
                .ToList();

            foreach (var cat in sortedCategories)
            {
                _categoryOptions.Add($"{cat.Emoji} {cat.GetLocalizedName()}");
                _categoryCodes.Add(cat.Code);
            }

            _categoryDropdown.sourceItems = _categoryOptions;
            _categoryDropdown.bindItem = (item, index) =>
            {
                if (index >= 0 && index < _categoryOptions.Count)
                {
                    item.label = _categoryOptions[index];
                    item.icon = null;
                }
            };
        }

        private void SetupCuisineDropdown()
        {
            if (_cuisineDropdown == null) return;
            _cuisineOptions.Clear();
            _cuisineCodes.Clear();

            _cuisineOptions.Add(L("RECIPES_FIELD_CUISINE_HINT", "Selecciona tipo de cocina..."));
            _cuisineCodes.Add("");

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

        private void OnDifficultyDropdownChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            var value = evt.newValue?.ToArray();
            if (_viewModel == null || value == null || value.Length == 0) return;
            int idx = value[0];
            if (idx >= 0 && idx < _difficultyCodes.Count)
            {
                string code = _difficultyCodes[idx];
                _viewModel.Difficulty = string.IsNullOrEmpty(code) ? null : code;
            }
        }

        private void OnCategoryDropdownChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            var value = evt.newValue?.ToArray();
            if (_viewModel == null || value == null || value.Length == 0) return;
            int idx = value[0];
            if (idx >= 0 && idx < _categoryCodes.Count)
            {
                string code = _categoryCodes[idx];
                _viewModel.Category = string.IsNullOrEmpty(code) ? null : code;
            }
        }

        private void OnCuisineDropdownChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            var value = evt.newValue?.ToArray();
            if (_viewModel == null || value == null || value.Length == 0) return;
            int idx = value[0];
            if (idx >= 0 && idx < _cuisineCodes.Count)
            {
                string code = _cuisineCodes[idx];
                _viewModel.CuisineType = string.IsNullOrEmpty(code) ? null : code;
            }
        }

        private void PopulateDropdownsFromViewModel()
        {
            if (_viewModel == null) return;

            if (_difficultyDropdown != null && _difficultyCodes.Count > 0)
            {
                int diffIdx = 0;
                if (!string.IsNullOrEmpty(_viewModel.Difficulty))
                {
                    int found = _difficultyCodes.FindIndex(c => c.Equals(_viewModel.Difficulty, StringComparison.OrdinalIgnoreCase));
                    if (found >= 0) diffIdx = found;
                }
                _difficultyDropdown.SetValueWithoutNotify(new[] { diffIdx });
            }

            if (_categoryDropdown != null && _categoryCodes.Count > 0)
            {
                int catIdx = 0;
                if (!string.IsNullOrEmpty(_viewModel.Category))
                {
                    int found = _categoryCodes.FindIndex(c => c.Equals(_viewModel.Category, StringComparison.OrdinalIgnoreCase));
                    if (found >= 0) catIdx = found;
                }
                _categoryDropdown.SetValueWithoutNotify(new[] { catIdx });
            }

            if (_cuisineDropdown != null && _cuisineCodes.Count > 0)
            {
                int cuiIdx = 0;
                if (!string.IsNullOrEmpty(_viewModel.CuisineType))
                {
                    int found = _cuisineCodes.FindIndex(c => c.Equals(_viewModel.CuisineType, StringComparison.OrdinalIgnoreCase));
                    if (found >= 0) cuiIdx = found;
                }
                _cuisineDropdown.SetValueWithoutNotify(new[] { cuiIdx });
            }
        }

        private void UpdateScreenTitle()
        {
            if (appBar == null) return;
            appBar.title = _viewModel != null && _viewModel.IsEditMode
                ? L("RECIPES_EDIT_TITLE", "Editar Receta")
                : L("RECIPES_CREATE_TITLE", "Crear Receta");
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

            UpdateNavigationControls();
        }

        private void UpdateNavigationControls()
        {
            if (_viewModel == null) return;

            if (_btnStepPrev != null)
            {
                _btnStepPrev.style.visibility = _viewModel.IsFirstStep ? Visibility.Hidden : Visibility.Visible;
                _btnStepPrev.title = L("TXT_BACK", "Anterior");
            }

            if (_btnStepNext != null)
            {
                _btnStepNext.title = _viewModel.IsLastStep
                    ? L("RECIPES_SAVE_BTN", "Guardar Receta")
                    : L("TXT_NEXT", "Siguiente");
                _btnStepNext.SetEnabled(_viewModel.CanGoNext && !_viewModel.IsSaving);
            }
        }

        private void UpdateNoIngredientsWarning()
        {
            if (_noIngredientsWarning != null)
            {
                _noIngredientsWarning.style.display = _viewModel != null && _viewModel.HasNoIngredientsWarning
                    ? DisplayStyle.Flex : DisplayStyle.None;
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

            UpdateNoIngredientsWarning();

            var ingredients = _viewModel?.Ingredients;
            if (ingredients == null) return;

            for (int i = 0; i < ingredients.Count; i++)
            {
                var captured = ingredients[i];
                var idx = i;

                var (qty, unit) = ParseMeasure(captured.Measure, captured.Quantity, captured.Unit);
                string unitLabel = FMQuantityUnitPanel.GetUnitLabel(unit);
                string unitDisplay = string.IsNullOrEmpty(unitLabel) ? unit : unitLabel;
                string label = qty > 0
                    ? $"{captured.Name} \u00d7 {qty:0.##} {unitDisplay}".Trim()
                    : captured.Name;

                var card = new FMItemShoppingListDetail
                {
                    Text = label
                };
                card.Checkbox.style.display = DisplayStyle.None;
                card.EditButton.clicked += () => ShowEditIngredientDialog(idx);
                card.OpenButton.clicked += () => ShowEditIngredientDialog(idx);
                card.RemoveButton.clicked += () => _viewModel.RemoveIngredient(idx);

                _ingredientsContainer.Add(card);
            }
        }

        private void ShowEditIngredientDialog(int index)
        {
            if (_viewModel?.Ingredients == null || index < 0 || index >= _viewModel.Ingredients.Count) return;
            var item = _viewModel.Ingredients[index];
            var (qty, unit) = ParseMeasure(item.Measure, item.Quantity, item.Unit);

            ShowQuantityDialog(item.Name, qty, unit, (newQty, newUnit) =>
            {
                string newMeasure = FormatMeasure(newQty, newUnit);
                _viewModel.UpdateIngredient(index, newQty, newUnit, newMeasure);
            });
        }

        private void ShowQuantityDialog(string foodName, float initialQty, string initialUnit, Action<float, string> onConfirm)
        {
            var panel = new FMQuantityUnitPanel();
            panel.SetQuantityWithoutNotify(initialQty > 0 ? initialQty : 1f);
            panel.SetUnitWithoutNotify(string.IsNullOrEmpty(initialUnit) ? "PIECES" : initialUnit);

            FMDialog.ShowCustom(
                this,
                foodName,
                panel,
                new FMDialogAction("@UI:TXT_CANCEL", null),
                new FMDialogAction("@UI:SAVE", () =>
                {
                    onConfirm?.Invoke(panel.Quantity, panel.Unit);
                }, ButtonVariant.Accent));
        }

        private static string FormatMeasure(float qty, string unit)
        {
            string unitLabel = FMQuantityUnitPanel.GetUnitLabel(unit);
            string unitDisplay = string.IsNullOrEmpty(unitLabel) ? unit : unitLabel;
            return qty > 0 ? $"{qty} {unitDisplay}".Trim() : unitDisplay;
        }

        private static (float qty, string unit) ParseMeasure(string measure, float? fallbackQty, string fallbackUnit)
        {
            if (fallbackQty.HasValue && !string.IsNullOrEmpty(fallbackUnit))
            {
                return (fallbackQty.Value, fallbackUnit);
            }

            if (string.IsNullOrWhiteSpace(measure))
            {
                return (fallbackQty ?? 1f, string.IsNullOrEmpty(fallbackUnit) ? "PIECES" : fallbackUnit);
            }

            string[] parts = measure.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedQty))
            {
                string rawUnit = parts.Length > 1 ? parts[1].Trim() : "";
                string matchedUnit = FindMatchingUnitCode(rawUnit);
                return (parsedQty, !string.IsNullOrEmpty(matchedUnit) ? matchedUnit : (fallbackUnit ?? "PIECES"));
            }
            else if (parts.Length > 0 && float.TryParse(parts[0].Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedQty2))
            {
                string rawUnit = parts.Length > 1 ? parts[1].Trim() : "";
                string matchedUnit = FindMatchingUnitCode(rawUnit);
                return (parsedQty2, !string.IsNullOrEmpty(matchedUnit) ? matchedUnit : (fallbackUnit ?? "PIECES"));
            }

            return (fallbackQty ?? 1f, fallbackUnit ?? "PIECES");
        }

        private static string FindMatchingUnitCode(string rawUnit)
        {
            if (string.IsNullOrWhiteSpace(rawUnit)) return "PIECES";
            var values = FMQuantityUnitPanel.UnitValues;
            var choices = FMQuantityUnitPanel.UnitChoices;
            if (values != null)
            {
                int exactIdx = values.FindIndex(v => string.Equals(v, rawUnit, StringComparison.OrdinalIgnoreCase));
                if (exactIdx >= 0) return values[exactIdx];
                if (choices != null)
                {
                    int labelIdx = choices.FindIndex(c => string.Equals(c, rawUnit, StringComparison.OrdinalIgnoreCase));
                    if (labelIdx >= 0 && labelIdx < values.Count) return values[labelIdx];
                }
            }
            return rawUnit;
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

        private static string L(string key, string fallback = "") =>
            LocalizationSettings.StringDatabase.GetLocalizedString("UI", key, null, FallbackBehavior.UseProjectSettings, fallback);

        private static string ExtractArg(Argument[] args, string name)
        {
            if (args == null) return null;
            foreach (var a in args)
            {
                if (a.name == name) return a.value as string;
            }
            return null;
        }

        // ── Accessibility ───────────────────────────────────

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
        }

        protected override void TeardownAccessibilityNodes()
        {
            _nextButtonNode = null;
            _prevButtonNode = null;
            base.TeardownAccessibilityNodes();
        }
    }
}
