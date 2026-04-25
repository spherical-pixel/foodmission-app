using System.Collections.Generic;
using System.ComponentModel;

using eu.foodmission.platform.Components;

using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;

using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class ShoppingListDetailScreen : NavigationScreenBase<ShoppingListDetailViewModel>
    {
        private VisualElement _itemsContainer;
        private CircularProgress _spinner;
        private Unity.AppUI.UI.Button _btnAdd;
        private Unity.AppUI.UI.Button _btnClearChecked;

        public ShoppingListDetailScreen()
        {
            InitializeComponent(FoodmissionAppBuilder.instance.ShoppingListDetailTemplate);
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _itemsContainer = contentContainer.Q<VisualElement>("items-container");
            _spinner = contentContainer.Q<CircularProgress>("loading-spinner");
            _btnAdd = contentContainer.Q<Button>("btn-add");
            _btnClearChecked = contentContainer.Q<Button>("btn-clear-checked");
        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            string listId = null;

            if (args != null)
            {
                foreach (Argument arg in args)
                {
                    if (arg.name == "listId")
                    {
                        listId = arg.value?.ToString();
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(listId))
            {
                await _viewModel.LoadAsync(listId);
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            _btnAdd.clicked += OnAddClicked;
            _btnClearChecked.clicked += async () => await _viewModel.ClearCheckedItemsAsync();

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        protected override void OnViewModelUnbinding()
        {
            _btnAdd.clicked -= OnAddClicked;
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
                _spinner?.EnableInClassList("visible", _viewModel.IsLoadingItems);
            }
        }

        private void RebuildItems()
        {
            _itemsContainer.Clear();

            if (_viewModel.Items == null)
            {
                return;
            }

            foreach (ShoppingListItemView view in _viewModel.Items)
            {
                ShoppingListItemView captured = view;

                var row = new VisualElement();
                row.AddToClassList("fm-sld-item-row");

                var toggle = new Checkbox { value = captured.Item.@checked };
                toggle.RegisterValueChangedCallback(_ =>
                    _ = _viewModel.ToggleItemAsync(captured.Item.id));

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

                var deleteBtn = new IconButton { icon = "trash" };
                deleteBtn.clicked += async () => await _viewModel.DeleteItemAsync(captured.Item.id);

                row.Add(toggle);
                row.Add(nameLabel);
                row.Add(qtyLabel);
                row.Add(deleteBtn);
                _itemsContainer.Add(row);
            }
        }

        private void OnAddClicked()
        {
            // ── Search state ───────────────────────────────────────────────
            var searchContainer = new VisualElement();
            searchContainer.style.minWidth = 280;

            var searchField = new UnityEngine.UIElements.TextField { label = "Search product" };
            searchField.style.marginBottom = 8;

            var searchSpinner = new CircularProgress { size = Size.S };
            searchSpinner.style.display = DisplayStyle.None;
            searchSpinner.style.alignSelf = Align.Center;

            var resultsScroll = new ScrollView();
            resultsScroll.style.maxHeight = 200;
            resultsScroll.style.display = DisplayStyle.None;

            var resultsContainer = new VisualElement();
            resultsScroll.Add(resultsContainer);

            // ── Confirm state ──────────────────────────────────────────────
            var confirmContainer = new VisualElement();
            confirmContainer.style.display = DisplayStyle.None;

            var selectedNameLabel = new Text();
            selectedNameLabel.style.marginBottom = 8;

            var qtyField = new Unity.AppUI.UI.FloatField { label = "Quantity", value = 1f };
            qtyField.style.marginBottom = 8;

            var unitChoices = new List<string> { "PIECES", "G", "KG", "ML", "L", "CUPS" };
            var unitDropdown = new Dropdown { sourceItems = unitChoices, selectedIndex = 0 };
            unitDropdown.style.marginBottom = 8;

            confirmContainer.Add(selectedNameLabel);
            confirmContainer.Add(qtyField);
            confirmContainer.Add(unitDropdown);

            searchContainer.Add(searchField);
            searchContainer.Add(searchSpinner);
            searchContainer.Add(resultsScroll);
            searchContainer.Add(confirmContainer);

            OpenFoodFactsProduct selectedProduct = null;

            // ── Search wiring ──────────────────────────────────────────────
            searchField.RegisterValueChangedCallback(async evt =>
            {
                string query = evt.newValue;

                if (string.IsNullOrWhiteSpace(query))
                {
                    resultsScroll.style.display = DisplayStyle.None;
                    resultsContainer.Clear();
                    return;
                }

                searchSpinner.style.display = DisplayStyle.Flex;
                resultsScroll.style.display = DisplayStyle.None;
                resultsContainer.Clear();

                await _viewModel.SearchFoodsAsync(query);

                searchSpinner.style.display = DisplayStyle.None;

                if (_viewModel.SearchResults?.Count > 0)
                {
                    foreach (OpenFoodFactsProduct product in _viewModel.SearchResults)
                    {
                        OpenFoodFactsProduct capturedProduct = product;

                        var resultRow = new VisualElement();
                        resultRow.style.flexDirection = FlexDirection.Row;
                        resultRow.style.paddingTop = 6;
                        resultRow.style.paddingBottom = 6;

                        string brands = capturedProduct.brands?.Length > 0
                            ? string.Join(", ", capturedProduct.brands)
                            : "";
                        string rowText = string.IsNullOrEmpty(brands)
                            ? capturedProduct.name
                            : $"{capturedProduct.name} · {brands}";

                        var label = new Text { text = rowText };
                        label.style.flexGrow = 1;
                        resultRow.Add(label);

                        resultRow.RegisterCallback<ClickEvent>(_ =>
                        {
                            selectedProduct = capturedProduct;
                            selectedNameLabel.text = capturedProduct.name;
                            searchField.style.display = DisplayStyle.None;
                            resultsScroll.style.display = DisplayStyle.None;
                            confirmContainer.style.display = DisplayStyle.Flex;
                        });

                        resultsContainer.Add(resultRow);
                    }

                    resultsScroll.style.display = DisplayStyle.Flex;
                }
            });

            // ── Modal ──────────────────────────────────────────────────────
            FMDialog.ShowCustom(
                this,
                "Add item",
                searchContainer,
                new FMDialogAction("Cancel", null),
                new FMDialogAction("Add", async () =>
                {
                    if (selectedProduct != null)
                    {
                        string unit = unitChoices[unitDropdown.selectedIndex];
                        await _viewModel.ImportAndAddItemAsync(selectedProduct, qtyField.value, unit);
                    }
                }, isPrimary: true));
        }
    }
}
