using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

using UnityEngine;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;

using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [Preserve]
    class ShoppingListDetailScreen : NavigationScreenBase<ShoppingListDetailViewModel>
    {
        private VisualElement _itemsContainer;
        private Text _errorText;
        private Text _emptyState;
        private Heading _listTitle;
        private Text _progressLabel;
        private Unity.AppUI.UI.TextField _filterField;
        private Unity.AppUI.UI.Button _btnAdd;
        private Unity.AppUI.UI.Button _btnClearChecked;

        public ShoppingListDetailScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.ShoppingListDetail));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _itemsContainer = contentContainer.Q<VisualElement>("items-container");
            _errorText = contentContainer.Q<Text>("error-message");
            _emptyState = contentContainer.Q<Text>("empty-state");
            _listTitle = contentContainer.Q<Heading>("list-title");
            _progressLabel = contentContainer.Q<Text>("progress-label");
            _filterField = contentContainer.Q<Unity.AppUI.UI.TextField>("filter-field");
            _btnAdd = contentContainer.Q<Unity.AppUI.UI.Button>("btn-add");
            _btnClearChecked = contentContainer.Q<Unity.AppUI.UI.Button>("btn-clear-checked");
        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            try
            {
                string listId = null;
                string listTitle = null;

                if (args != null)
                {
                    foreach (Argument arg in args)
                    {
                        if (arg.name == "listId")
                            listId = arg.value?.ToString();
                        else if (arg.name == "listTitle")
                            listTitle = arg.value?.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(listId))
                    await _viewModel.LoadAsync(listId, listTitle);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] OnEnter failed: {ex.Message}");
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            _btnAdd.clicked += OnAddClicked;
            _btnClearChecked.clicked += OnClearCheckedClicked;
            _listTitle.RegisterCallback<ClickEvent>(_ => ShowRenameDialog());

            if (_filterField != null)
            {
                _filterField.RegisterValueChangedCallback(OnFilterChanged);
            }

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateLoadingState();
            UpdateErrorState();
            UpdateListTitle();
        }

        protected override void OnViewModelUnbinding()
        {
            _btnAdd.clicked -= OnAddClicked;
            _btnClearChecked.clicked -= OnClearCheckedClicked;
            if (_filterField != null)
            {
                _filterField.UnregisterValueChangedCallback(OnFilterChanged);
            }
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            base.OnViewModelUnbinding();
        }

        private void OnFilterChanged(ChangeEvent<string> evt)
        {
            _viewModel.FilterText = evt.newValue;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Items))
            {
                RebuildItems();
            }
            else if (e.PropertyName == nameof(_viewModel.FilterText))
            {
                _viewModel.ApplyFilter();
            }
            else if (e.PropertyName == nameof(_viewModel.IsLoadingItems))
            {
                UpdateLoadingState();
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorMessage))
            {
                UpdateErrorState();
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorDetail))
            {
                UpdateApiErrorState();
            }
            else if (e.PropertyName == nameof(_viewModel.ListName))
            {
                UpdateListTitle();
            }
        }

        private async void OnClearCheckedClicked()
        {
            try
            {
                await _viewModel.ClearCheckedItemsAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] ClearCheckedItemsAsync failed: {ex.Message}");
            }
        }

        private void RebuildItems()
        {
            _itemsContainer.Clear();

            if (_viewModel.Items == null || _viewModel.Items.Count == 0)
            {
                _emptyState?.EnableInClassList("visible", true);
                UpdateProgress();
                return;
            }

            _emptyState?.EnableInClassList("visible", false);
            UpdateProgress();

            foreach (ShoppingListItemView view in _viewModel.Items)
            {
                ShoppingListItemView captured = view;

                var row = new VisualElement();
                row.AddToClassList("fm-sld-item-row");

                string toggleItemId = captured.Item.id;
                var toggle = new Checkbox
                {
                    value = captured.Item.@checked ? CheckboxState.Checked : CheckboxState.Unchecked
                };
                toggle.RegisterValueChangedCallback(evt =>
                    _ = SafeToggleItemAsync(toggleItemId));

                var nameLabel = new Text { text = captured.FoodName };
                nameLabel.AddToClassList("fm-sld-item-name");
                if (captured.Item.@checked)
                {
                    nameLabel.AddToClassList("fm-sld-item-name--checked");
                }

                var qtyLabel = new Text
                {
                    text = $"{captured.Item.quantity:0.##} {captured.Item.unit}"
                };
                qtyLabel.AddToClassList("fm-sld-item-qty");
                qtyLabel.RegisterCallback<ClickEvent>(_ => ShowEditItemDialog(captured));

                string deleteItemId = captured.Item.id;
                var deleteBtn = new IconButton { icon = "trash" };
                deleteBtn.clicked += () => _ = SafeDeleteItemAsync(deleteItemId);

                row.Add(toggle);
                row.Add(nameLabel);
                row.Add(qtyLabel);
                row.Add(deleteBtn);
                _itemsContainer.Add(row);
            }
        }

        private void UpdateLoadingState()
        {
            bool isLoading = _viewModel.IsLoadingItems;
            if (isLoading)
                FMLoadingOverlay.Show(contentContainer);
            else
                FMLoadingOverlay.Hide(contentContainer);
            _btnAdd?.SetEnabled(!isLoading);
            _btnClearChecked?.SetEnabled(!isLoading);
        }

        private void UpdateErrorState()
        {
            bool hasError = !string.IsNullOrEmpty(_viewModel.ErrorMessage);
            _errorText?.EnableInClassList("visible", hasError);
            if (_errorText != null)
            {
                _errorText.text = _viewModel.ErrorMessage;
            }
        }

        private void UpdateListTitle()
        {
            if (_listTitle != null && !string.IsNullOrWhiteSpace(_viewModel.ListName))
            {
                _listTitle.text = _viewModel.ListName;
            }
        }

        private void UpdateProgress()
        {
            int total = _viewModel.Items?.Count ?? 0;
            int checked_ = 0;
            if (_viewModel.Items != null)
            {
                foreach (var v in _viewModel.Items)
                {
                    if (v.Item.@checked) checked_++;
                }
            }

            if (_progressLabel != null)
            {
                _progressLabel.text = total > 0 ? $"{checked_}/{total}" : "";
            }

            bool hasChecked = checked_ > 0;
            if (_btnClearChecked != null)
            {
                _btnClearChecked.style.display = hasChecked ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ShowRenameDialog()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;

            var nameField = new Unity.AppUI.UI.TextField
            {
                placeholder = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LIST_NAME_PLACEHOLDER"),
                value = _viewModel.ListName
            };

            container.Add(nameField);

            FMDialog.ShowCustom(
                this,
                "@UI:RENAME_LIST",
                container,
                new FMDialogAction("@UI:TXT_CANCEL", null),
                new FMDialogAction("@UI:SAVE", () =>
                {
                    _ = SafeRenameListAsync(nameField.value);
                }, isPrimary: true));
        }

        private static readonly List<string> UnitValues = new() { "PIECES", "G", "KG", "ML", "L", "CUPS" };

        private static List<string> _unitChoices;
        private static List<string> UnitChoices
        {
            get
            {
                if (_unitChoices == null)
                {
                    _unitChoices = new List<string>
                    {
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_PIECES"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_G"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_KG"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_ML"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_L"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_CUPS"),
                    };
                }
                return _unitChoices;
            }
        }

        private void ShowEditItemDialog(ShoppingListItemView captured)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;

            var qtyField = new Unity.AppUI.UI.TextField
            {
                placeholder = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QUANTITY_PLACEHOLDER"),
                value = $"{captured.Item.quantity:0.##}"
            };

            var unitDropdown = new Dropdown
            {
                sourceItems = UnitChoices
            };
            int unitIdx = UnitValues.IndexOf(captured.Item.unit);
            if (unitIdx >= 0)
            {
                unitDropdown.SetValueWithoutNotify(new[] { unitIdx });
            }

            container.Add(qtyField);
            container.Add(unitDropdown);

            string editItemId = captured.Item.id;
            FMDialog.ShowCustom(
                this,
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "EDIT_ITEM_TITLE", new object[] { captured.FoodName }),
                container,
                new FMDialogAction("@UI:TXT_CANCEL", null),
                new FMDialogAction("@UI:SAVE", () =>
                {
                    float.TryParse(qtyField.value, out float qty);
                    string unit = unitDropdown.selectedIndex >= 0
                        ? UnitValues[unitDropdown.selectedIndex]
                        : captured.Item.unit ?? "";
                    _ = SafeUpdateItemAsync(editItemId, qty, unit);
                }, isPrimary: true));
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private void OnAddClicked()
        {
            FMProductSearchDialog.ShowFoodSearch(
                this,
                "@UI:ADD_ITEM",
                async query =>
                {
                    await _viewModel.SearchFoodsAsync(query);
                    return _viewModel.SearchResults;
                },
                async (product, qty, unit) =>
                {
                    bool added = await _viewModel.ImportAndAddItemAsync(product, qty, unit);
                    if (!added)
                    {
                        FMDialog.ShowAlert(this, "@UI:ADD_ITEM", "@UI:ADD_ITEM_ERROR", AlertSemantic.Error);
                    }
                });
        }
        private async Task SafeToggleItemAsync(string itemId)
        {
            try
            {
                await _viewModel.ToggleItemAsync(itemId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] ToggleItemAsync failed: {ex.Message}");
            }
        }

        private async Task SafeDeleteItemAsync(string itemId)
        {
            try
            {
                await _viewModel.DeleteItemAsync(itemId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] DeleteItemAsync failed: {ex.Message}");
            }
        }

        private async Task SafeRenameListAsync(string name)
        {
            try
            {
                await _viewModel.RenameListAsync(name);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] RenameListAsync failed: {ex.Message}");
            }
        }

        private async Task SafeUpdateItemAsync(string itemId, float quantity, string unit)
        {
            try
            {
                await _viewModel.UpdateItemAsync(itemId, quantity, unit);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] UpdateItemAsync failed: {ex.Message}");
            }
        }
    }
}
