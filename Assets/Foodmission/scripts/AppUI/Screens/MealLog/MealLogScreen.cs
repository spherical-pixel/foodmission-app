using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [Preserve]
    class MealLogScreen : NavigationScreenBase<MealLogViewModel>
    {
        private VisualElement _step1;
        private VisualElement _step2;
        private VisualElement _step3;
        private VisualElement _step1Buttons;
        private VisualElement _step2Buttons;
        private VisualElement _btnBackStep2;
        private VisualElement _btnBackStep3;
        private Unity.AppUI.UI.TextField _mealNameField;
        private UnityEngine.UIElements.TextField _mealNameInnerField;
        private VisualElement _presetResults;
        private FMSearchOrCategoryField _searchCategoryField;
        private VisualElement _selectionAndButton;
        private VisualElement _selectedChips;
        private FMButton _btnLogSelected;
        private VisualElement _loggedMealsZone;
        private VisualElement _mealList;
        private AccessibilityNode _logButtonNode;

        private static readonly Dictionary<string, string> TypeEmojis = new()
        {
            { "BREAKFAST", "\U0001F305" },
            { "LUNCH", "\u2600\uFE0F" },
            { "DINNER", "\U0001F319" },
            { "SNACK", "\U0001F97F" },
            { "DRINKS", "\U0001F964" },
            { "OTHER", "\U0001F37D\uFE0F" },
        };

        public MealLogScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.MealLog));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _step1 = contentContainer.Q<VisualElement>("step-1");
            _step2 = contentContainer.Q<VisualElement>("step-2");
            _step3 = contentContainer.Q<VisualElement>("step-3");
            _step1Buttons = contentContainer.Q<VisualElement>("step-1-buttons");
            _step2Buttons = contentContainer.Q<VisualElement>("step-2-buttons");
            _btnBackStep2 = contentContainer.Q<VisualElement>("btn-back-step-2");
            _btnBackStep3 = contentContainer.Q<VisualElement>("btn-back-step-3");
            _mealNameField = contentContainer.Q<Unity.AppUI.UI.TextField>("meal-name-field");
            _presetResults = contentContainer.Q<VisualElement>("preset-results");
            _searchCategoryField = contentContainer.Q<FMSearchOrCategoryField>("search-category-field");
            _selectionAndButton = contentContainer.Q<VisualElement>("selection-and-button");
            _selectedChips = contentContainer.Q<VisualElement>("selected-chips");
            _btnLogSelected = contentContainer.Q<FMButton>("btn-log-selected");
            _loggedMealsZone = contentContainer.Q<VisualElement>("logged-meals-zone");
            _mealList = contentContainer.Q<VisualElement>("list-meals-today");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            _btnLogSelected.clicked += OnLogSelectedClicked;
            _btnBackStep2?.RegisterCallback<ClickEvent>(OnBackClicked);
            _btnBackStep3?.RegisterCallback<ClickEvent>(OnBackClicked);
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            UpdateStepVisibility();

            _mealNameField?.schedule.Execute(() =>
            {
                _mealNameInnerField = _mealNameField.Q<UnityEngine.UIElements.TextField>();
                if (_mealNameInnerField != null)
                    _mealNameInnerField.RegisterValueChangedCallback(OnMealNameChanged);
            }).ExecuteLater(0);

            if (_searchCategoryField != null)
            {
                _searchCategoryField.SearchProductsAsync = query => _viewModel.SearchFoodsAsync(query);
                _searchCategoryField.GetGenericFoodsAsync = () => _viewModel.GetGenericFoodsAsync();
                _searchCategoryField.SearchGenericFoodsAsync = query => _viewModel.SearchGenericFoodsAsync(query);
                _searchCategoryField.OnProductConfirmed = async (product, qty, unit) =>
                {
                    await _viewModel.AddProductItem(product, qty, unit);
                };
                _searchCategoryField.OnGenericFoodConfirmed = async (food, qty, unit) =>
                {
                    await _viewModel.AddGenericFoodItem(food, qty, unit);
                };
                _searchCategoryField.ImportFromBarcodeAsync = barcode => _viewModel.ImportByBarcodeAsync(barcode);
                _searchCategoryField.OnPopoverVisibilityChanged += OnPopoverVisibilityChanged;
            }

            _viewModel.InitializeAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.LogError($"[{GetType().Name}] InitializeAsync failed: {t.Exception?.InnerException?.Message}");
                else
                    ExecuteOnMainThread(() =>
                    {
                        RebuildTypeButtons();
                        RebuildSourceButtons();
                        UpdateStepVisibility();
                    });
            });

            _viewModel.LoadTodayAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.LogError($"[{GetType().Name}] LoadTodayAsync failed: {t.Exception?.InnerException?.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected override void OnViewModelUnbinding()
        {
            _viewModel.DisposeSearchCts();
            _btnLogSelected.clicked -= OnLogSelectedClicked;
            _btnBackStep2?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _btnBackStep3?.UnregisterCallback<ClickEvent>(OnBackClicked);

            if (_searchCategoryField != null)
                _searchCategoryField.OnPopoverVisibilityChanged -= OnPopoverVisibilityChanged;

            if (_mealNameInnerField != null)
            {
                _mealNameInnerField.UnregisterValueChangedCallback(OnMealNameChanged);
                _mealNameInnerField = null;
            }
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            base.OnViewModelUnbinding();
        }

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            _logButtonNode = CreateButtonNode(_accessibilityHierarchy, _btnLogSelected, "Log selected dishes");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _logButtonNode = null;
            base.TeardownAccessibilityNodes();
        }

        private AccessibilityNode CreateButtonNode(AccessibilityHierarchy hierarchy, VisualElement button, string label)
        {
            if (button == null) return null;
            var node = hierarchy.AddNode(label);
            node.role = AccessibilityRole.Button;
            if (!button.enabledSelf) node.state = AccessibilityState.Disabled;
            node.frameGetter = () =>
            {
                if (button.panel == null) return Rect.zero;
                var r = button.worldBound;
                var s = button.panel.scaledPixelsPerPoint;
                return new Rect(r.position * s, r.size * s);
            };
            node.invoked += () =>
            {
                using var evt = NavigationSubmitEvent.GetPooled();
                evt.target = button;
                button.SendEvent(evt);
                return true;
            };
            return node;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.CurrentStep):
                    UpdateStepVisibility();
                    if (_viewModel.CurrentStep == MealLogStep.SelectingSource)
                        RebuildSourceButtons();
                    break;
                case nameof(_viewModel.TypeOfMealOptions):
                    RebuildTypeButtons();
                    break;
                case nameof(_viewModel.PresetResults):
                    RebuildPresetResults();
                    break;
                case nameof(_viewModel.IsSearchingPresets):
                    if (_viewModel.IsSearchingPresets)
                        FMLoadingOverlay.Show(_step3, "Searching presets...");
                    else
                        FMLoadingOverlay.Hide(_step3);
                    break;
                case nameof(_viewModel.SelectedMealPreset):
                    if (_viewModel.SelectedMealPreset != null)
                        _mealNameField.value = _viewModel.SelectedMealPreset.name;
                    UpdateLogButtonState();
                    break;
                case nameof(_viewModel.SelectedItems):
                    RebuildSelectedChips();
                    UpdateLogButtonState();
                    break;
                case nameof(_viewModel.IsSaving):
                    if (_viewModel.IsSaving)
                        FMLoadingOverlay.Show(contentContainer);
                    else
                        FMLoadingOverlay.Hide(contentContainer);
                    break;
                case nameof(_viewModel.Groups):
                    RebuildMealCards();
                    break;
                case nameof(_viewModel.ErrorMessage):
                    break;
                case nameof(_viewModel.ErrorDetail):
                    if (_viewModel.ErrorDetail != null)
                    {
                        FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                        _viewModel.ErrorDetail = null;
                    }
                    break;
            }
        }

        private void UpdateStepVisibility()
        {
            bool step1 = _viewModel.CurrentStep == MealLogStep.SelectingTypeOfMeal;
            bool step2 = _viewModel.CurrentStep == MealLogStep.SelectingSource;
            bool step3 = _viewModel.CurrentStep == MealLogStep.SelectingDishes || _viewModel.CurrentStep == MealLogStep.Saving;

            _step1?.EnableInClassList("visible", step1);
            _step2?.EnableInClassList("visible", step2);
            _step3?.EnableInClassList("visible", step3);

            _btnBackStep2?.EnableInClassList("visible", step2);
            _btnBackStep3?.EnableInClassList("visible", step3);

            _loggedMealsZone?.EnableInClassList("visible", step1);

            if (step3)
            {
                var heading = _step3?.Q<Heading>("step-3-heading");
                if (heading != null && _viewModel.SelectedTypeOfMeal != null)
                    heading.text = $"What did you have for {_viewModel.SelectedTypeOfMeal.label.ToLowerInvariant()}?";
            }
        }

        private void RebuildTypeButtons()
        {
            _step1Buttons?.Clear();
            if (_viewModel.TypeOfMealOptions == null) return;

            for (int i = 0; i < _viewModel.TypeOfMealOptions.Length; i++)
            {
                CatalogItem item = _viewModel.TypeOfMealOptions[i];
                string emoji = GetEmojiForType(item.code);
                string label = item.label;
                int capturedIndex = i;

                var btn = new FMButton();
                btn.title = $"{emoji} {label}";
                btn.variant = ButtonVariant.Accent;
                btn.trailingIcon = "fm-arrow-right";
                btn.size = Size.L;
                btn.quiet = true;
                btn.AddToClassList("fm-button-align-left");
                btn.AddToClassList("fm-button-list");
                btn.clicked += () => _viewModel.SelectTypeOfMeal(capturedIndex);

                _step1Buttons.Add(btn);
            }
        }

        private void RebuildSourceButtons()
        {
            _step2Buttons?.Clear();

            var pantryBtn = new FMButton();
            pantryBtn.title = "\U0001F9FA From the pantry";
            pantryBtn.variant = ButtonVariant.Accent;
            pantryBtn.trailingIcon = "fm-arrow-right";
            pantryBtn.size = Size.L;
            pantryBtn.quiet = true;
            pantryBtn.AddToClassList("fm-button-align-left");
            pantryBtn.AddToClassList("fm-button-list");
            pantryBtn.clicked += () => _viewModel.SetSource(true, false);

            _step2Buttons.Add(pantryBtn);

            var eatenOutBtn = new FMButton();
            eatenOutBtn.title = "\U0001F37D\uFE0F Eaten out";
            eatenOutBtn.variant = ButtonVariant.Accent;
            eatenOutBtn.trailingIcon = "fm-arrow-right";
            eatenOutBtn.size = Size.L;
            eatenOutBtn.quiet = true;
            eatenOutBtn.AddToClassList("fm-button-align-left");
            eatenOutBtn.AddToClassList("fm-button-list");
            eatenOutBtn.clicked += () => _viewModel.SetSource(false, true);

            _step2Buttons.Add(eatenOutBtn);
        }

        private void RebuildMealCards()
        {
            _mealList?.Clear();
            if (_viewModel.Groups == null || _viewModel.Groups.Count == 0)
            {
                _mealList?.Add(new Text { text = "No meals logged today." });
                return;
            }

            foreach (MealLogGroup group in _viewModel.Groups)
            {
                string emoji = GetEmojiForType(group.TypeOfMeal);
                string typeLabel = FormatTypeOfMeal(group.TypeOfMeal);
                int totalCalories = group.Logs.Sum(l => (int)(l.meal?.calories ?? 0f));

                var card = new VisualElement();
                card.AddToClassList("fm-meal-card");

                var title = new Heading
                {
                    text = $"{emoji} {typeLabel}:",
                    size = HeadingSize.M,
                };
                title.AddToClassList("bold-text");
                title.style.paddingBottom = 16;
                title.style.paddingTop = 0;
                card.Add(title);

                var info = new Text
                {
                    text = $"{totalCalories} kcal",
                };
                info.AddToClassList("fm-meal-card-text");
                info.style.paddingBottom = 0;
                card.Add(info);

                bool firstSource = group.Logs[0].mealFromPantry;
                bool allSameSource = group.Logs.All(l => l.mealFromPantry == firstSource && l.eatenOut == group.Logs[0].eatenOut);
                if (allSameSource)
                {
                    var badge = new Text();
                    if (firstSource)
                    {
                        badge.text = "From pantry";
                        badge.AddToClassList("fm-ml-card-badge");
                        badge.AddToClassList("fm-ml-card-badge--pantry");
                    }
                    else if (group.Logs[0].eatenOut)
                    {
                        badge.text = "Eaten out";
                        badge.AddToClassList("fm-ml-card-badge");
                        badge.AddToClassList("fm-ml-card-badge--out");
                    }
                    if (!string.IsNullOrEmpty(badge.text))
                        card.Add(badge);
                }

                _mealList.Add(card);
            }
        }

        private void OnPopoverVisibilityChanged(bool isVisible)
        {
            if (_selectionAndButton != null)
            {
                _selectionAndButton.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private async void OnMealNameChanged(ChangeEvent<string> evt)
        {
            _viewModel.MealContainerName = evt.newValue;
            if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                _viewModel.ClearMealPreset();
                await _viewModel.SearchPresetsAsync("");
            }
            else
            {
                await _viewModel.SearchPresetsAsync(evt.newValue);
            }
        }

        private void RebuildPresetResults()
        {
            _presetResults?.Clear();
            List<Meal> presets = _viewModel.PresetResults;
            if (presets.Count == 0) return;

            foreach (Meal preset in presets)
            {
                bool isRecipe = !string.IsNullOrEmpty(preset.recipeId);

                var row = new VisualElement();
                row.AddToClassList("fm-ml-search-result-row");

                var nameLabel = new Text { text = preset.name };
                nameLabel.AddToClassList("fm-ml-search-result-name");
                nameLabel.pickingMode = PickingMode.Ignore;
                row.Add(nameLabel);

                if (isRecipe)
                {
                    var badge = new Text { text = "Recipe" };
                    badge.AddToClassList("fm-ml-search-result-badge");
                    badge.pickingMode = PickingMode.Ignore;
                    row.Add(badge);
                }

                Meal captured = preset;
                row.RegisterCallback<ClickEvent>(_ => _viewModel.SelectMealPreset(captured));

                _presetResults?.Add(row);
            }
        }

        private void RebuildSelectedChips()
        {
            _selectedChips?.Clear();
            if (_viewModel.SelectedItems.Count == 0) return;

            foreach (MealLogItem entry in _viewModel.SelectedItems)
            {
                MealLogItem captured = entry;
                var chip = new VisualElement();
                chip.AddToClassList("fm-ml-chip");

                var label = new Text
                {
                    text = $"{entry.name} × {entry.quantity} {entry.unit}"
                };
                label.AddToClassList("fm-ml-chip-label");
                label.pickingMode = PickingMode.Ignore;
                chip.Add(label);

                var removeBtn = new Text { text = "\u2715" };
                removeBtn.AddToClassList("fm-ml-chip-remove");
                removeBtn.pickingMode = PickingMode.Ignore;
                chip.Add(removeBtn);

                chip.RegisterCallback<ClickEvent>(_ => _viewModel.RemoveItem(captured));

                _selectedChips.Add(chip);
            }
        }

        private async void OnLogSelectedClicked()
        {
            try
            {
                bool success = await _viewModel.SaveAsync();
                if (success)
                {
                    RebuildMealCards();
                    UpdateStepVisibility();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] OnLogSelectedClicked failed: {ex.Message}");
            }
        }

        private void UpdateLogButtonState()
        {
            if (_btnLogSelected == null) return;
            _btnLogSelected.SetEnabled(
                _viewModel.SelectedItems.Count > 0 ||
                _viewModel.SelectedMealPreset != null);
        }

        private void OnBackClicked(ClickEvent evt)
        {
            _viewModel.GoBack();
        }

        private void ExecuteOnMainThread(Action action)
        {
            _step1?.schedule.Execute(action).ExecuteLater(0);
        }

        private static string FormatTypeOfMeal(string type)
        {
            return type switch
            {
                "BREAKFAST" => "Breakfast",
                "LUNCH" => "Lunch",
                "DINNER" => "Dinner",
                "SNACK" => "Snack",
                "DRINKS" => "Drinks",
                "OTHER" => "Other",
                _ => type
            };
        }

        private static string GetEmojiForType(string type)
        {
            return TypeEmojis.TryGetValue(type, out string emoji) ? emoji : "\U0001F37D\uFE0F";
        }
    }

}
