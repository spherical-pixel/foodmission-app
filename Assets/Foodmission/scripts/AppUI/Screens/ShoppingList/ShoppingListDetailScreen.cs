using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
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
        private Text _errorText;
        private Heading _listTitle;
        private Unity.AppUI.UI.Button _btnAdd;
        private Unity.AppUI.UI.Button _btnClearChecked;
        private CancellationTokenSource _searchCts;

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
            _spinner = contentContainer.Q<CircularProgress>("loading-spinner");
            _errorText = contentContainer.Q<Text>("error-message");
            _listTitle = contentContainer.Q<Heading>("list-title");
            _btnAdd = contentContainer.Q<Unity.AppUI.UI.Button>("btn-add");
            _btnClearChecked = contentContainer.Q<Unity.AppUI.UI.Button>("btn-clear-checked");
        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            string listId = null;
            string listTitle = null;

            if (args != null)
            {
                foreach (Argument arg in args)
                {
                    if (arg.name == "listId")
                    {
                        listId = arg.value?.ToString();
                    }
                    else if (arg.name == "listTitle")
                    {
                        listTitle = arg.value?.ToString();
                    }
                }
            }

            if (!string.IsNullOrEmpty(listId))
            {
                await _viewModel.LoadAsync(listId, listTitle);
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            _btnAdd.clicked += OnAddClicked;
            _btnClearChecked.clicked += OnClearCheckedClicked;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateLoadingState();
            UpdateErrorState();
            UpdateListTitle();
        }

        protected override void OnViewModelUnbinding()
        {
            _btnAdd.clicked -= OnAddClicked;
            _btnClearChecked.clicked -= OnClearCheckedClicked;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = null;
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
            else if (e.PropertyName == nameof(_viewModel.ListName))
            {
                UpdateListTitle();
            }
        }

        private async void OnClearCheckedClicked()
        {
            await _viewModel.ClearCheckedItemsAsync();
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

                var toggle = new Checkbox
                {
                    value = captured.Item.@checked ? CheckboxState.Checked : CheckboxState.Unchecked
                };
                toggle.RegisterValueChangedCallback(evt =>
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

        private void UpdateLoadingState()
        {
            bool isLoading = _viewModel.IsLoadingItems;
            _spinner?.EnableInClassList("visible", isLoading);
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

        private void OnAddClicked()
        {
            // ── Search state ───────────────────────────────────────────────
            var searchContainer = new VisualElement();
            searchContainer.style.minWidth = 280;

            var searchField = new Unity.AppUI.UI.TextField { placeholder = "Search product" };
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

            var qtyLabel = new Text { text = "Quantity" };
            qtyLabel.style.marginBottom = 4;

            var qtyField = new Unity.AppUI.UI.FloatField { value = 1f };
            qtyField.style.marginBottom = 8;

            var unitChoices = new List<string> { "PIECES", "G", "KG", "ML", "L", "CUPS" };
            var unitDropdown = new Dropdown { sourceItems = unitChoices, selectedIndex = 0 };
            unitDropdown.style.marginBottom = 8;

            confirmContainer.Add(selectedNameLabel);
            confirmContainer.Add(qtyLabel);
            confirmContainer.Add(qtyField);
            confirmContainer.Add(unitDropdown);

            searchContainer.Add(searchField);
            searchContainer.Add(searchSpinner);
            searchContainer.Add(resultsScroll);
            searchContainer.Add(confirmContainer);

            OpenFoodFactsProduct selectedProduct = null;

            // ── Search wiring ──────────────────────────────────────────────
            // RegisterValueChangedCallback on AppUI TextField only fires on commit (blur/Enter).
            // Wire to the inner UI Toolkit TextField to get per-keystroke updates + debounce.
            searchContainer.schedule.Execute(() =>
            {
                var innerField = searchField.Q<UnityEngine.UIElements.TextField>();
                if (innerField == null)
                {
                    return;
                }

                innerField.RegisterValueChangedCallback(async innerEvt =>
                {
                    string query = innerEvt.newValue;

                    _searchCts?.Cancel();
                    _searchCts?.Dispose();
                    _searchCts = new CancellationTokenSource();
                    CancellationToken ct = _searchCts.Token;

                    if (string.IsNullOrWhiteSpace(query))
                    {
                        resultsScroll.style.display = DisplayStyle.None;
                        resultsContainer.Clear();
                        return;
                    }

                    try
                    {
                        await System.Threading.Tasks.Task.Delay(400, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    searchSpinner.style.display = DisplayStyle.Flex;
                    resultsScroll.style.display = DisplayStyle.None;
                    resultsContainer.Clear();

                    await _viewModel.SearchFoodsAsync(query);

                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

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
                }); // closes innerField.RegisterValueChangedCallback
            }).ExecuteLater(0); // closes schedule.Execute

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
                        bool added = await _viewModel.ImportAndAddItemAsync(selectedProduct, qtyField.value, unit);
                        if (!added)
                        {
                            FMDialog.ShowAlert(this, "Add item", "Could not add the selected item.", AlertSemantic.Error);
                        }
                    }
                    else
                    {
                        FMDialog.ShowAlert(this, "Add item", "Select a product first.", AlertSemantic.Warning);
                    }
                }, isPrimary: true));
        }
    }
}
