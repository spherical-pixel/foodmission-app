using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    [Preserve]
    class QuickSearchScreen : NavigationScreenBase<QuickSearchViewModel>
    {
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private FMSearchOrCategoryField _searchCategoryField;
        private VisualElement _mainContent;

        public QuickSearchScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.QuickSearchScreen));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _searchCategoryField = contentContainer.Q<FMSearchOrCategoryField>("search-category-field");
            _mainContent = contentContainer.Q<VisualElement>("main-content");
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            _navController = controller;
            base.OnEnter(controller, destination, args);
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_searchCategoryField != null)
            {
                _searchCategoryField.AutoOpenCategories = true;
                _searchCategoryField.OpenFoodInfoOnSelect = true;
                _searchCategoryField.SearchProductsAsync = query => _viewModel.SearchFoodsAsync(query);
                _searchCategoryField.GetGenericFoodsAsync = () => _viewModel.GetGenericFoodsAsync();
                _searchCategoryField.SearchGenericFoodsAsync = query => _viewModel.SearchGenericFoodsAsync(query);
                _searchCategoryField.SearchByFoodGroupAsync = (foodGroup, page, pageSize) => _viewModel.SearchByFoodGroupAsync(foodGroup, page, pageSize);

                _searchCategoryField.OpenCategories();

                _searchCategoryField.ImportFromBarcodeAsync = barcode => _viewModel.ImportByBarcodeAsync(barcode);
                _searchCategoryField.OnProductInfoRequested = product =>
                {
                    FoodInfoOverlay.Show(this, FoodInfoType.Product, product.id, "quickSearch",
                        JsonConvert.SerializeObject(product),
                        () => _viewModel?.CheckPendingFoodInfoAddRequest());
                };
                _searchCategoryField.OnGenericFoodInfoRequested = genericFood =>
                {
                    FoodInfoOverlay.Show(this, FoodInfoType.Generic, genericFood.id, "quickSearch",
                        JsonConvert.SerializeObject(genericFood),
                        () => _viewModel?.CheckPendingFoodInfoAddRequest());
                };

                _searchCategoryField.OnPopoverVisibilityChanged += isVisible =>
                {
                    if (_mainContent != null)
                    {
                        _mainContent.style.visibility = isVisible ? Visibility.Hidden : Visibility.Visible;
                    }
                };
            }

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateErrorState();
            UpdateStatusState();
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.ErrorMessage))
            {
                UpdateErrorState();
            }
            else if (e.PropertyName == nameof(_viewModel.StatusMessage))
            {
                UpdateStatusState();
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorDetail))
            {
                UpdateApiErrorState();
            }
            else if (e.PropertyName == nameof(_viewModel.PendingMealLogAdd))
            {
                if (_viewModel.PendingMealLogAdd != null)
                {
                    ShowMealLogQuickOptionsModal(_viewModel.PendingMealLogAdd);
                    _viewModel.PendingMealLogAdd = null;
                }
            }
        }

        private void UpdateStatusState()
        {
            if (!string.IsNullOrEmpty(_viewModel.StatusMessage))
            {
                Toast.Build(this, _viewModel.StatusMessage, NotificationDuration.Short)
                    .SetStyle(NotificationStyle.Positive)
                    .SetPosition(PopupNotificationPlacement.Bottom)
                    .Show();
                _viewModel.StatusMessage = "";
            }
        }

        private void UpdateErrorState()
        {
            if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
            {
                Toast.Build(this, _viewModel.ErrorMessage, NotificationDuration.Long)
                    .SetStyle(NotificationStyle.Negative)
                    .SetPosition(PopupNotificationPlacement.Bottom)
                    .Show();
                _viewModel.ErrorMessage = "";
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

        private async void ShowMealLogQuickOptionsModal(QuickMealLogOptions options)
        {

            bool isFromPantry = true;

            var content = new VisualElement();
            content.style.paddingTop = 16;
            content.style.paddingBottom = 16;
            content.style.paddingLeft = 16;
            content.style.paddingRight = 16;

            var foodLabel = new Unity.AppUI.UI.Text
            {
                size = TextSize.M,
                text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ADD_ITEM_TITLE") + $": {options.FoodName}"
            };
            foodLabel.style.marginBottom = 16;
            content.Add(foodLabel);

            var mealTypeHeader = new Unity.AppUI.UI.Text
            {
                size = TextSize.M,
                text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TYPE_OF_MEAL") + ":"
            };
            mealTypeHeader.style.marginBottom = 8;
            content.Add(mealTypeHeader);

            var catalogItems = _viewModel != null ? await _viewModel.GetMealTypesAsync() : null;
            var mealTypeNames = new System.Collections.Generic.List<string>();

            if (catalogItems != null && catalogItems.Length > 0)
            {
                foreach (var item in catalogItems)
                {
                    mealTypeNames.Add(item.label ?? item.code);
                }
            }

            var mealTypeDropdown = new Dropdown();
            mealTypeDropdown.bindItem = (item, i) => item.label = mealTypeNames[i];
            mealTypeDropdown.sourceItems = mealTypeNames;
            mealTypeDropdown.SetValueWithoutNotify(new[] { 0 });
            mealTypeDropdown.style.marginBottom = 16;
            content.Add(mealTypeDropdown);

            var sourceHeader = new Unity.AppUI.UI.Text
            {
                size = TextSize.S,
                text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ORIGIN") + ":"
            };
            sourceHeader.style.marginBottom = 8;
            content.Add(sourceHeader);

            var pantryCheckbox = new Unity.AppUI.UI.Checkbox();
            pantryCheckbox.label = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FROM_PANTRY");
            pantryCheckbox.value = CheckboxState.Checked;
            pantryCheckbox.RegisterValueChangedCallback(evt =>
            {
                isFromPantry = evt.newValue == CheckboxState.Checked;
            });
            content.Add(pantryCheckbox);

            FMDialog.ShowCustom(
                this,
                "@UI:ADD_TO_MEAL_LOG",
                content,
                new FMDialogAction("@UI:TXT_CANCEL", null, false),
                new FMDialogAction("@UI:TXT_CONTINUE", () =>
                {
                    bool eatenOut = !isFromPantry;
                    var args = new Argument[]
                    {
                        new Argument("mealTypeIndex", (mealTypeDropdown.value?.Any() == true ? mealTypeDropdown.value.First() : 0).ToString()),
                        new Argument("eatenOut", eatenOut ? "true" : "false"),
                        new Argument("foodType", ((int)options.FoodType).ToString()),
                        new Argument("foodId", options.FoodId ?? ""),
                        new Argument("foodName", options.FoodName ?? "")
                    };

                    if (_navController != null)
                    {
                        _navController.Navigate(Destinations.meallog, args);
                    }
                }, true)
            );
        }
    }
}
