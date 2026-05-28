using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Accessibility;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;

using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Unity.AppUI.Core;

namespace eu.foodmission.platform
{
    [Preserve]
    class ShoppingListDetailScreen : NavigationScreenBase<ShoppingListDetailViewModel>
    {

        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;


        private VisualElement _itemsContainer;
        
        private Text _emptyState;
        private Heading _listTitle;
        private Text _progressLabel;
        private FMSearchOrCategoryField _searchCategoryField;
        private VisualElement _mainContent;
        


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
            _emptyState = contentContainer.Q<Text>("empty-state");
            _listTitle = contentContainer.Q<Heading>("list-title");
            _searchCategoryField = contentContainer.Q<FMSearchOrCategoryField>("search-category-field");
            _mainContent = contentContainer.Q<VisualElement>("main-content");
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

            _listTitle.RegisterCallback<ClickEvent>(_ => ShowRenameDialog());

            if (_searchCategoryField != null)
            {
                _searchCategoryField.SearchProductsAsync = query => _viewModel.SearchFoodsAsync(query);
                _searchCategoryField.GetGenericFoodsAsync = () => _viewModel.GetGenericFoodsAsync();
                _searchCategoryField.OnProductConfirmed = async (product, qty, unit) =>
                {
                    await SafeImportAndAddItemAsync(product, qty, unit);
                    RebuildItems();
                };
                _searchCategoryField.OnGenericFoodConfirmed = async (food, qty, unit) =>
                {
                    await SafeAddGenericFoodItemAsync(food, qty, unit);
                    RebuildItems();
                };
                _searchCategoryField.ImportFromBarcodeAsync = barcode => _viewModel.ImportByBarcodeAsync(barcode);
                _searchCategoryField.OnTextChanged = text => _viewModel.FilterText = text;

                _searchCategoryField.OnPopoverVisibilityChanged += isVisible =>
                {
                    // When the popover is open, we want to disable scrolling on the main content to prevent accidental scrolls
                    if (_mainContent != null)
                    {
                        _mainContent.style.visibility = isVisible ? Visibility.Hidden : Visibility.Visible;
                    }
                };
            }

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateLoadingState();
            UpdateErrorState();
            UpdateListTitle();
        }

        protected override void OnViewModelUnbinding()
        {
            
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Items))
            {
                RebuildItems();
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

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            var h = _accessibilityHierarchy;

            
        }

        protected override void TeardownAccessibilityNodes()
        {
            
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

        private static Func<Rect> MakeElementFrameGetter(VisualElement element)
        {
            return () =>
            {
                if (element == null || element.panel == null) return Rect.zero;
                var r = element.worldBound;
                var s = element.panel.scaledPixelsPerPoint;
                return new Rect(r.position * s, r.size * s);
            };
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
            Debug.Log($"[{GetType().Name}] RebuildItems - Rebuilding items, count: {_viewModel.Items?.Count ?? 0}");
            _itemsContainer.Clear();

            if (_viewModel.Items == null || _viewModel.Items.Count == 0)
            {
                _emptyState.style.visibility = Visibility.Visible;
                UpdateProgress();
                return;
            }

            _emptyState.style.visibility = Visibility.Hidden;
            UpdateProgress();

            foreach (ShoppingListItemView view in _viewModel.Items)
            {
                ShoppingListItemView captured = view;

                FMItemShoppingListDetail item = new FMItemShoppingListDetail { Text = captured.FoodName };

                item.Checkbox.value = captured.Item.@checked ? CheckboxState.Checked : CheckboxState.Unchecked;
                item.Checkbox.RegisterValueChangedCallback(evt =>_ = SafeToggleItemAsync(captured.Item.id));
                item.EditButton.clicked += () => ShowEditItemDialog(captured);
                item.RemoveButton.clicked += () => _ = SafeDeleteItemAsync(captured.Item.id);
                _itemsContainer.Add(item);
            }
        }

        private void UpdateLoadingState()
        {
            bool isLoading = _viewModel.IsLoadingItems;
            if (isLoading)
                FMLoadingOverlay.Show(contentContainer);
            else
                FMLoadingOverlay.Hide(contentContainer);
            
        }

        private void UpdateErrorState()
        {
            if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
            {
                Toast.Build(this, _viewModel.ErrorMessage, NotificationDuration.Long)
                    .SetStyle(NotificationStyle.Negative)
                    .SetPosition(PopupNotificationPlacement.Bottom)
                    .Show();
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
            unitDropdown.bindItem = (item, i) => item.label = UnitChoices[i];
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

        private async Task SafeImportAndAddItemAsync(OpenFoodFactsProduct product, float qty, string unit)
        {
            try
            {
                await _viewModel.ImportAndAddItemAsync(product, qty, unit);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] ImportAndAddItemAsync failed: {ex.Message}");
            }
        }

        private async Task SafeAddGenericFoodItemAsync(GenericFood food, float qty, string unit)
        {
            try
            {
                await _viewModel.AddGenericFoodItemAsync(food, qty, unit);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] AddGenericFoodItemAsync failed: {ex.Message}");
            }
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
