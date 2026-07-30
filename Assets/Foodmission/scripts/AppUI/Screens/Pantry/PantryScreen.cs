using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Newtonsoft.Json;

using eu.foodmission.platform.Components;

using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    [Preserve]
    class PantryScreen : NavigationScreenBase<PantryViewModel>
    {
        private VisualElement _itemsContainer;
        private Text _errorText;
        private Text _emptyState;
        private VisualElement _expiredBanner;
        private Text _expiredBannerText;
        private Unity.AppUI.UI.Button _btnMoveToWaste;
        private FMSearchOrCategoryField _searchCategoryField;
        private VisualElement _mainContent;

        private AccessibilityNode _moveToWasteNode;

        public PantryScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.Pantry));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _itemsContainer = contentContainer.Q<VisualElement>("items-container");
            _errorText = contentContainer.Q<Text>("error-message");
            _emptyState = contentContainer.Q<Text>("empty-state");
            _searchCategoryField = contentContainer.Q<FMSearchOrCategoryField>("search-category-field");
            _mainContent = contentContainer.Q<VisualElement>("main-content");
            _expiredBanner = contentContainer.Q<VisualElement>("expired-banner");
            _expiredBannerText = contentContainer.Q<Text>("expired-banner-text");
            _btnMoveToWaste = contentContainer.Q<Unity.AppUI.UI.Button>("btn-move-to-waste");
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            // State is preserved — no reload needed. CheckPendingFoodInfoAddRequest no
            // longer required since FoodInfo is now shown as an overlay.
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_searchCategoryField != null)
            {
                _searchCategoryField.OpenFoodInfoOnSelect = true;
                _searchCategoryField.SearchProductsAsync = query => _viewModel.SearchFoodsAsync(query);
                _searchCategoryField.GetGenericFoodsAsync = () => _viewModel.GetGenericFoodsAsync();
                _searchCategoryField.SearchGenericFoodsAsync = query => _viewModel.SearchGenericFoodsAsync(query);
                _searchCategoryField.SearchByFoodGroupAsync = (foodGroup, page, pageSize) => _viewModel.SearchByFoodGroupAsync(foodGroup, page, pageSize);
                _searchCategoryField.OnProductConfirmed = async (product, qty, unit) =>
                {
                    await SafeImportAndAddFoodItemAsync(product, qty ?? 1f, unit ?? "PIECES");
                    RebuildItems();
                };
                _searchCategoryField.OnGenericFoodConfirmed = async (food, qty, unit) =>
                {
                    await SafeAddGenericFoodItemAsync(food, qty ?? 1f, unit ?? "PIECES");
                    RebuildItems();
                };

                _searchCategoryField.OnTextChanged = text => _viewModel.FilterText = text;
                _searchCategoryField.ImportFromBarcodeAsync = barcode => _viewModel.ImportByBarcodeAsync(barcode);
                _searchCategoryField.OnProductInfoRequested = product =>
                {
                    FoodInfoOverlay.Show(this, FoodInfoType.Product, product.id, "pantry",
                        JsonConvert.SerializeObject(product),
                        () => _viewModel?.CheckPendingFoodInfoAddRequest());
                };
                _searchCategoryField.OnGenericFoodInfoRequested = genericFood =>
                {
                    FoodInfoOverlay.Show(this, FoodInfoType.Generic, genericFood.id, "pantry",
                        JsonConvert.SerializeObject(genericFood),
                        () => _viewModel?.CheckPendingFoodInfoAddRequest());
                };
                _searchCategoryField.OnPopoverVisibilityChanged += OnPopoverVisibilityChanged;
            }

            if (_btnMoveToWaste != null)
            {
                _btnMoveToWaste.clicked += OnMoveToWasteClicked;
            }
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            RebuildItems();
            UpdateLoadingState();
            UpdateErrorState();
            UpdateExpiredBanner();

            _ = _viewModel.LoadAsync();
        }

        protected override void OnViewModelUnbinding()
        {
            if (_searchCategoryField != null)
            {
                _searchCategoryField.OnPopoverVisibilityChanged -= OnPopoverVisibilityChanged;
            }
            if (_btnMoveToWaste != null)
            {
                _btnMoveToWaste.clicked -= OnMoveToWasteClicked;
            }
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            base.OnViewModelUnbinding();
        }

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            var h = _accessibilityHierarchy;

            _moveToWasteNode = CreateButtonNode(h, _btnMoveToWaste, "Move expired items to waste");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _moveToWasteNode = null;
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

        private void OnPopoverVisibilityChanged(bool isVisible)
        {
            if (_mainContent != null)
            {
                _mainContent.style.visibility = isVisible ? Visibility.Hidden : Visibility.Visible;
            }
        }

        private async Task SafeImportAndAddFoodItemAsync(OpenFoodFactsProduct product, float qty, string unit)
        {
            try
            {
                await _viewModel.ImportAndAddFoodItemAsync(product, qty, unit);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] ImportAndAddFoodItemAsync failed: {ex.Message}");
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

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.Items):
                    RebuildItems();
                    break;
                case nameof(_viewModel.FilterText):
                    _viewModel.ApplyFilter();
                    break;
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.ErrorMessage):
                    UpdateErrorState();
                    break;
                case nameof(_viewModel.ExpiredItemCount):
                    UpdateExpiredBanner();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
            }
        }

        private void RebuildItems()
        {
            _itemsContainer.Clear();

            if (_viewModel.Items == null || _viewModel.Items.Count == 0)
            {
                _emptyState?.EnableInClassList("visible", true);
                return;
            }

            _emptyState?.EnableInClassList("visible", false);

            DateTime today = DateTime.UtcNow.Date;

            // Ensure items are sorted by expiry date (earliest / expired first, items without expiry last)
            var sortedItems = _viewModel.Items
                .OrderBy(view =>
                {
                    if (string.IsNullOrEmpty(view.Item?.expiryDate)) return DateTime.MaxValue;
                    if (DateTime.TryParse(view.Item.expiryDate, out DateTime dt)) return dt.Date;
                    return DateTime.MaxValue;
                })
                .ThenBy(view => view.DisplayName)
                .ToList();

            List<PantryItemView> expired = new();
            List<PantryItemView> expiringSoon = new();
            List<PantryItemView> ok = new();

            foreach (PantryItemView view in sortedItems)
            {
                if (string.IsNullOrEmpty(view.Item.expiryDate))
                {
                    ok.Add(view);
                    continue;
                }

                if (DateTime.TryParse(view.Item.expiryDate, out DateTime expiry))
                {
                    if (expiry.Date < today)
                    {
                        expired.Add(view);
                    }
                    else if (expiry.Date <= today.AddDays(3))
                    {
                        expiringSoon.Add(view);
                    }
                    else
                    {
                        ok.Add(view);
                    }
                }
                else
                {
                    ok.Add(view);
                }
            }

            BuildItemGroup(expired, "Expired", "fm-p-item-expiry--expired");
            BuildItemGroup(expiringSoon, "Expiring soon", "fm-p-item-expiry--soon");
            BuildItemGroup(ok, null, "fm-p-item-expiry--ok");
        }

        private void BuildItemGroup(List<PantryItemView> items, string sectionName, string defaultExpiryClass)
        {
            if (items.Count == 0) return;

            if (sectionName != null)
            {
                var header = new Text { text = sectionName };
                header.AddToClassList("fm-p-section-header");
                _itemsContainer.Add(header);
            }

            DateTime today = DateTime.UtcNow.Date;

            foreach (PantryItemView view in items)
            {
                PantryItemView captured = view;

                string detail = $"{captured.Item.quantity:0.##} {captured.Item.unit}";
                if (!string.IsNullOrEmpty(captured.Item.location))
                {
                    detail += $" · {captured.Item.location}";
                }

                string formattedExpiry = "";
                string expiryColorClass = defaultExpiryClass;

                if (!string.IsNullOrEmpty(captured.Item.expiryDate))
                {
                    if (DateTime.TryParse(captured.Item.expiryDate, out DateTime expiry))
                    {
                        formattedExpiry = expiry.ToString("yyyy-MM-dd");

                        if (expiry.Date < today)
                        {
                            expiryColorClass = "fm-p-item-expiry--expired";
                        }
                        else if (expiry.Date <= today.AddDays(3))
                        {
                            expiryColorClass = "fm-p-item-expiry--soon";
                        }
                        else
                        {
                            expiryColorClass = "fm-p-item-expiry--ok";
                        }
                    }
                    else
                    {
                        formattedExpiry = captured.Item.expiryDate;
                    }
                }

                var itemCard = new FMItemPantry
                {
                    Text = captured.DisplayName,
                    Detail = detail,
                    ExpiryText = formattedExpiry
                };

                if (!string.IsNullOrEmpty(expiryColorClass) && !string.IsNullOrEmpty(formattedExpiry))
                {
                    itemCard.ExpiryLabel.AddToClassList(expiryColorClass);
                }

                bool hasFoodInfo = !string.IsNullOrEmpty(captured.Item.foodProductId) || !string.IsNullOrEmpty(captured.Item.genericFoodId);
                itemCard.InfoButton.style.display = hasFoodInfo ? DisplayStyle.Flex : DisplayStyle.None;

                itemCard.OpenButton.clicked += () =>
                {
                    PantryItemDetailOverlay.Show(
                        this,
                        captured.Item.id,
                        onSaved: () => _ = _viewModel.LoadAsync(),
                        onDeleted: () => _ = _viewModel.LoadAsync());
                };

                itemCard.InfoButton.clicked += () =>
                {
                    if (!string.IsNullOrEmpty(captured.Item.foodProductId))
                    {
                        FoodInfoOverlay.Show(this, FoodInfoType.Product, captured.Item.foodProductId, "none");
                    }
                    else if (!string.IsNullOrEmpty(captured.Item.genericFoodId))
                    {
                        FoodInfoOverlay.Show(this, FoodInfoType.Generic, captured.Item.genericFoodId, "none");
                    }
                };

                itemCard.RemoveButton.clicked += () =>
                {
                    FMDialog.ShowConfirm(
                        this,
                        "@UI:DELETE_ITEM",
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CONFIRM_DELETE_MSG", new object[] { captured.DisplayName }),
                        onConfirm: async () => await _viewModel.DeleteItemAsync(captured.Item.id),
                        semantic: AlertSemantic.Destructive);
                };

                _itemsContainer.Add(itemCard);
            }
        }

        private void UpdateLoadingState()
        {
            bool isLoading = _viewModel.IsLoading;
            if (isLoading)
                FMLoadingOverlay.Show();
            else
                FMLoadingOverlay.Hide();
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

        private void UpdateExpiredBanner()
        {
            if (_expiredBanner == null) return;

            bool show = _viewModel.ExpiredItemCount > 0;
            _expiredBanner.EnableInClassList("visible", show);

            if (show && _expiredBannerText != null)
            {
                _expiredBannerText.text = _viewModel.ExpiredItemCount == 1
                    ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "EXPIRED_BANNER")
                    : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "EXPIRED_BANNER_PLURAL", new object[] { _viewModel.ExpiredItemCount });
            }
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private void OnMoveToWasteClicked()
        {
            if (_viewModel.ExpiredItemCount == 0) return;

            int count = _viewModel.ExpiredItemCount;
            string message = count == 1
                ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MOVE_EXPIRED_MSG")
                : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MOVE_EXPIRED_MSG_PLURAL", new object[] { count });

            FMDialog.ShowConfirm(
                this,
                "@UI:MOVE_TO_WASTE",
                message,
                onConfirm: async () =>
                {
                    int wasted = await _viewModel.BatchWasteExpiredAsync();
                    if (wasted > 0)
                        Toast.Build(this, (wasted == 1 ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ITEMS_MOVED_WASTE", new object[] { wasted }) : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ITEMS_MOVED_WASTE_PLURAL", new object[] { wasted })), NotificationDuration.Short)
                            .SetStyle(NotificationStyle.Positive)
                            .SetPosition(PopupNotificationPlacement.Bottom)
                            .Show();
                },
                semantic: AlertSemantic.Destructive);
        }

    }
}
