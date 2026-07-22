using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.Core;
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
        protected override bool IsFixedContent => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;

        private VisualElement _step1;
        private VisualElement _step2;
        private VisualElement _step3;
        private VisualElement _step1Buttons;
        private VisualElement _step2Buttons;
        private VisualElement _btnBackStep2;
        private VisualElement _btnBackStep3;
        private Unity.AppUI.UI.Checkbox _chkSavePreset;
        private VisualElement _presetResults;
        private FMButton _btnLoadPreset;
        private Unity.AppUI.UI.TextField _presetSearchField;
        private UnityEngine.UIElements.TextField _presetSearchInnerField;
        private VisualElement _presetResultsList;
        private FMSearchOrCategoryField _searchCategoryField;
        private VisualElement _step3Content;
        private VisualElement _selectionAndButton;
        private VisualElement _selectedChips;
        private FMButton _btnLogSelected;
        private VisualElement _loggedMealsZone;
        private VisualElement _mealList;
        private AccessibilityNode _logButtonNode;

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
            _chkSavePreset = contentContainer.Q<Unity.AppUI.UI.Checkbox>("chk-save-preset");
            _presetResults = contentContainer.Q<VisualElement>("preset-results");
            _btnLoadPreset = contentContainer.Q<FMButton>("btn-load-preset");
            _presetSearchField = contentContainer.Q<Unity.AppUI.UI.TextField>("preset-search-field");
            _presetResultsList = contentContainer.Q<VisualElement>("preset-results-list");
            _searchCategoryField = contentContainer.Q<FMSearchOrCategoryField>("search-category-field");
            _step3Content = contentContainer.Q<VisualElement>("step-3-content");
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

            _viewModel.OnConfirmUpdateRequired += OnConfirmUpdateRequired;

            UpdateStepVisibility();
            UpdateSavePresetCheckboxVisibility();

            if (_chkSavePreset != null)
            {
                _chkSavePreset.value = _viewModel.SaveAsPreset ? CheckboxState.Checked : CheckboxState.Unchecked;
                _chkSavePreset.RegisterValueChangedCallback(OnSavePresetChanged);
            }


            if (_btnLoadPreset != null)
                _btnLoadPreset.clicked += OnLoadPresetClicked;

            _presetSearchField?.schedule.Execute(() =>
            {
                _presetSearchInnerField = _presetSearchField.Q<UnityEngine.UIElements.TextField>();
                if (_presetSearchInnerField != null)
                    _presetSearchInnerField.RegisterValueChangedCallback(OnPresetSearchChanged);
            }).ExecuteLater(0);

            if (_searchCategoryField != null)
            {
                _searchCategoryField.SearchProductsAsync = query => _viewModel.SearchFoodsAsync(query);
                _searchCategoryField.GetGenericFoodsAsync = () => _viewModel.GetGenericFoodsAsync();
                _searchCategoryField.SearchGenericFoodsAsync = query => _viewModel.SearchGenericFoodsAsync(query);
                _searchCategoryField.SearchByFoodGroupAsync = (foodGroup, page, pageSize) => _viewModel.SearchByFoodGroupAsync(foodGroup, page, pageSize);
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
                {
                    Debug.LogError($"[{GetType().Name}] InitializeAsync failed: {t.Exception?.InnerException?.Message}");

                }
                else
                {
                    ExecuteOnMainThread(() =>
                    {
                        RebuildTypeButtons();
                        RebuildSourceButtons();
                        UpdateStepVisibility();
                    });
                }
            });

            _viewModel.LoadTodayAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError($"[{GetType().Name}] LoadTodayAsync failed: {t.Exception?.InnerException?.Message}");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected override void OnViewModelUnbinding()
        {
            _viewModel.DisposeSearchCts();
            _btnLogSelected.clicked -= OnLogSelectedClicked;
            _btnBackStep2?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _btnBackStep3?.UnregisterCallback<ClickEvent>(OnBackClicked);

            _viewModel.OnConfirmUpdateRequired -= OnConfirmUpdateRequired;

            if (_searchCategoryField != null)
                _searchCategoryField.OnPopoverVisibilityChanged -= OnPopoverVisibilityChanged;

            if (_chkSavePreset != null)
            {
                _chkSavePreset.UnregisterValueChangedCallback(OnSavePresetChanged);
            }

            if (_btnLoadPreset != null)
                _btnLoadPreset.clicked -= OnLoadPresetClicked;

            if (_presetSearchInnerField != null)
            {
                _presetSearchInnerField.UnregisterValueChangedCallback(OnPresetSearchChanged);
                _presetSearchInnerField = null;
            }

            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            base.OnViewModelUnbinding();
        }

        private void OnSavePresetChanged(ChangeEvent<CheckboxState> evt)
        {
            _viewModel.SaveAsPreset = evt.newValue == CheckboxState.Checked;
        }

        private void UpdateSavePresetCheckboxVisibility()
        {
            if (_chkSavePreset == null) return;
            bool hasPreset = _viewModel.SelectedMealPreset != null;
            _chkSavePreset.style.display = hasPreset ? DisplayStyle.None : DisplayStyle.Flex;
            if (hasPreset)
            {
                _viewModel.SaveAsPreset = false;
            }
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
                        FMLoadingOverlay.Show(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SEARCHING_PRESETS"));
                    else
                        FMLoadingOverlay.Hide(this);
                    break;
                case nameof(_viewModel.SaveAsPreset):
                    if (_chkSavePreset != null)
                        _chkSavePreset.value = _viewModel.SaveAsPreset ? CheckboxState.Checked : CheckboxState.Unchecked;
                    break;
                case nameof(_viewModel.SelectedMealPreset):
                    UpdateSavePresetCheckboxVisibility();
                    RebuildSelectedChips();
                    UpdateLogButtonState();
                    ClosePresetPanel();
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
                case nameof(_viewModel.LastTenLogs):
                    RebuildMealCards();
                    break;
                case nameof(_viewModel.ErrorMessage):
                    if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
                    {
                        Toast.Build(this, _viewModel.ErrorMessage, NotificationDuration.Short).Show();
                        _viewModel.ErrorMessage = "";
                    }
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
                    heading.text = new LocalizedOption("UI", "txtWHAT_DID_YOU_HAVE_FOR", _viewModel.SelectedTypeOfMeal.label.ToLowerInvariant()).GetText();
            }
            else
            {
                ResetStep3VisualState();
            }
        }

        private void RebuildTypeButtons()
        {
            _step1Buttons?.Clear();
            if (_viewModel.TypeOfMealOptions == null) return;

            for (int i = 0; i < _viewModel.TypeOfMealOptions.Length; i++)
            {
                CatalogItem item = _viewModel.TypeOfMealOptions[i];
                string emoji = MealLogHelpers.GetEmojiForTypeOfMeal(item.code);
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
            pantryBtn.title = "\U0001F9FA " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FROM_PANTRY");
            pantryBtn.variant = ButtonVariant.Accent;
            pantryBtn.trailingIcon = "fm-arrow-right";
            pantryBtn.size = Size.L;
            pantryBtn.quiet = true;
            pantryBtn.AddToClassList("fm-button-align-left");
            pantryBtn.AddToClassList("fm-button-list");
            pantryBtn.clicked += () => _viewModel.SetSource(true, false);

            _step2Buttons.Add(pantryBtn);

            var eatenOutBtn = new FMButton();
            eatenOutBtn.title = "\U0001F37D\uFE0F " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "EATEN_OUT");
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
            if (_viewModel.LastTenLogs == null || _viewModel.LastTenLogs.Count == 0)
            {
                _mealList?.Add(new Text { text = "@UI:txtNO_RECENT_MEALS_LOGGED" });
                return;
            }

            CatalogItem[] typeOptions = _viewModel.TypeOfMealOptions;

            foreach (MealLog log in _viewModel.LastTenLogs)
            {
                string typeLabel = typeOptions != null
                    ? Array.Find(typeOptions, o => o.code == log.typeOfMeal)?.label ?? log.typeOfMeal
                    : log.typeOfMeal;

                FMMealLogCard card = new FMMealLogCard
                {
                    MealLogData = log,
                    TypeLabel = typeLabel
                };

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

        private void OnLoadPresetClicked()
        {
            if (_presetResults == null) return;
            bool isOpen = _presetResults.style.display == DisplayStyle.Flex;
            _presetResults.style.display = isOpen ? DisplayStyle.None : DisplayStyle.Flex;
            if (_step3Content != null)
                _step3Content.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
            _btnLoadPreset.title = isOpen ? "@UI:LOAD_PRESET_OR_RECIPE" : "@UI:CANCEL_SEARCH";
            if (!isOpen && _presetSearchField != null)
                _presetSearchField.SetValueWithoutNotify("");
        }

        private void ClosePresetPanel()
        {
            if (_presetResults != null)
                _presetResults.style.display = DisplayStyle.None;
            if (_step3Content != null)
                _step3Content.style.display = DisplayStyle.Flex;
            if (_btnLoadPreset != null)
                _btnLoadPreset.title = "@UI:LOAD_PRESET_OR_RECIPE";
        }

        private void ResetStep3VisualState()
        {
            ClosePresetPanel();
            if (_searchCategoryField != null)
                _searchCategoryField.SearchText = "";
            if (_presetSearchField != null)
                _presetSearchField.SetValueWithoutNotify("");
        }

        private async void OnPresetSearchChanged(ChangeEvent<string> evt)
        {
            try
            {
                await _viewModel.SearchPresetsAsync(evt.newValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] OnPresetSearchChanged failed: {ex.Message}");
            }
        }

        private void OnConfirmUpdateRequired(string mealName)
        {
            var textContainer = new VisualElement();
            var text = new Unity.AppUI.UI.Text
            {
                text = new LocalizedOption("UI", "PRESET_MODIFIED", mealName).GetText()
            };
            textContainer.Add(text);

            FMDialog.ShowCustom(
                this,
                "@UI:PRESET_MODIFIED_TITLE",
                textContainer,
                new FMDialogAction("@UI:TXT_CANCEL", () => _viewModel.CancelUpdate()),
                new FMDialogAction("@UI:SAVE_AS_NEW", () => PromptNewMealPresetNameAndSave()),
                new FMDialogAction("@UI:UPDATE", async () => await _viewModel.ConfirmUpdateAndSaveAsync(), isPrimary: true));
        }

        private void PromptNewMealPresetNameAndSave()
        {
            var nameField = new Unity.AppUI.UI.TextField
            {
                placeholder = "@UI:PLACEHOLDER_PRESET_NAME",
                value = _viewModel.SelectedMealPreset?.name ?? ""
            };

            FMDialog.ShowCustom(
                this,
                "@UI:SAVE_AS_NEW_PRESET",
                nameField,
                new FMDialogAction("@UI:TXT_CANCEL", () => _viewModel.CancelUpdate()),
                new FMDialogAction("@UI:SAVE", async () =>
                {
                    string newName = nameField.value?.Trim();
                    if (string.IsNullOrEmpty(newName))
                    {
                        _viewModel.ErrorMessage = "@UI:ERROR_PRESET_NAME_REQUIRED";
                        return;
                    }
                    _viewModel.MealContainerName = newName;
                    await PerformSaveAsync();
                }, isPrimary: true));
        }



        private void RebuildPresetResults()
        {
            _presetResultsList?.Clear();
            List<Meal> presets = _viewModel.PresetResults;
            if (presets.Count == 0) return;

            foreach (Meal preset in presets)
            {
                bool isRecipe = preset.isRecipe;

                var row = new VisualElement();
                row.AddToClassList("fm-scf-result-row");

                var nameLabel = new Text { text = preset.name };
                nameLabel.style.flexGrow = 1;
                nameLabel.pickingMode = PickingMode.Ignore;
                nameLabel.style.width = Length.Percent(70);
                row.Add(nameLabel);

                if (isRecipe)
                {
                    var badge = new Text { text = "@UI:RECIPE" };
                    badge.pickingMode = PickingMode.Ignore;
                    row.Add(badge);
                }

                Meal captured = preset;
                row.RegisterCallback<ClickEvent>(_ => _viewModel.SelectMealPreset(captured));

                _presetResultsList?.Add(row);
            }
        }

        private void RebuildSelectedChips()
        {
            _selectedChips?.Clear();
            if (_viewModel.SelectedMealPreset != null)
            {
                bool isRecipe = _viewModel.SelectedMealPreset.isRecipe;
                var card = new FMItemShoppingListDetail
                {
                    Text = isRecipe
                        ? $"\U0001F9D3\u200D\U0001F373 {_viewModel.SelectedMealPreset.name}"
                        : $"\U0001F372 {_viewModel.SelectedMealPreset.name}"
                };
                card.Checkbox.style.display = DisplayStyle.None;
                card.AddToClassList("fm-ml-chip-preset");
                card.RemoveButton.clicked += () => _viewModel.ClearMealPreset();
                card.EditButton.style.display = DisplayStyle.None;
                _selectedChips.Add(card);
            }
            if (_viewModel.SelectedItems.Count == 0) return;
            foreach (MealLogItem entry in _viewModel.SelectedItems)
            {
                MealLogItem captured = entry;
                string unitLabel = FMQuantityUnitPanel.GetUnitLabel(entry.unit);
                string label = (entry.quantity.HasValue && entry.quantity.Value > 0)
                    ? $"{entry.name} \u00d7 {entry.quantity.Value}{(string.IsNullOrEmpty(unitLabel) ? "" : " " + unitLabel)}"
                    : entry.name;
                var card = new FMItemShoppingListDetail
                {
                    Text = label
                };
                card.Checkbox.style.display = DisplayStyle.None;
                card.EditButton.clicked += () => ShowEditItemDialog(captured);
                card.RemoveButton.clicked += () => _viewModel.RemoveItem(captured);
                _selectedChips.Add(card);
            }
        }

        private void ShowEditItemDialog(MealLogItem item)
        {
            var panel = new FMQuantityUnitPanel();
            panel.SetQuantityWithoutNotify(item.quantity ?? 1f);
            panel.SetUnitWithoutNotify(!string.IsNullOrEmpty(item.unit) ? item.unit : "PIECES");

            FMDialog.ShowCustom(
                this,
                item.name,
                panel,
                new FMDialogAction("@UI:TXT_CANCEL", null),
                new FMDialogAction("@UI:SAVE", () =>
                {
                    item.quantity = panel.Quantity;
                    item.unit = panel.Unit;
                    _viewModel.SelectedItems = new List<MealLogItem>(_viewModel.SelectedItems);
                }, isPrimary: true));
        }


        private void OnLogSelectedClicked()
        {
            if (_viewModel.SaveAsPreset)
            {
                var nameField = new Unity.AppUI.UI.TextField
                {
                    placeholder = "@UI:PLACEHOLDER_PRESET_NAME",
                    value = _viewModel.MealContainerName
                };

                FMDialog.ShowCustom(
                    this,
                    "@UI:SAVE_AS_NEW_PRESET",
                    nameField,
                    new FMDialogAction("@UI:TXT_CANCEL", null),
                    new FMDialogAction("@UI:SAVE", async () =>
                    {
                        string presetName = nameField.value?.Trim();
                        if (string.IsNullOrEmpty(presetName))
                        {
                            _viewModel.ErrorMessage = "@UI:ERROR_PRESET_NAME_REQUIRED";
                            return;
                        }
                        _viewModel.MealContainerName = presetName;
                        await PerformSaveAsync();
                    }, isPrimary: true));
            }
            else
            {
                if (_viewModel.SelectedMealPreset == null)
                    _viewModel.MealContainerName = "";

                _ = PerformSaveAsync();
            }
        }

        private async Task PerformSaveAsync()
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
                Debug.LogError($"[{GetType().Name}] PerformSaveAsync failed: {ex.Message}");
            }
        }



        private void UpdateLogButtonState()
        {
            if (_btnLogSelected == null) return;
            _btnLogSelected.SetEnabled(_viewModel.SelectedItems != null && _viewModel.SelectedItems.Count > 0);
        }

        private void OnBackClicked(ClickEvent evt)
        {
            if (_viewModel.CurrentStep == MealLogStep.SelectingDishes)
                ResetStep3VisualState();
            _viewModel.GoBack();
        }

        private void ExecuteOnMainThread(Action action)
        {
            _step1?.schedule.Execute(action).ExecuteLater(0);
        }


    }

}
