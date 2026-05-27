using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine;
using Unity.Properties;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMSearchOrCategoryField : VisualElement
    {
        private enum Mode { Idle, Categories, Searching, Confirmed }

        private Mode _currentMode = Mode.Idle;
        private CancellationTokenSource _debounceCts;
        private List<GenericFood> _genericFoods = new();
        private List<object> _recentProducts = new();
        private object _selectedItem;

        private Unity.AppUI.UI.TextField _textField;
        protected Unity.AppUI.UI.Button _actionButton;
        private VisualElement _resultsContainer;
        private VisualElement _categoryContainer;
        private VisualElement _searchResultsContainer;
        private CircularProgress _spinner;
        private Unity.AppUI.UI.IconButton _scanButton;
        private VisualElement _confirmContainer;
        private Unity.AppUI.UI.FloatField _qtyField;
        private Dropdown _unitDropdown;
        private Text _selectedNameLabel;

        // ========= UXML ATTRIBUTES =========
        [UxmlAttribute("placeholder")][CreateProperty]
        public string Placeholder
        {
            get => _textField?.placeholder ?? "";
            set { if (_textField != null) _textField.placeholder = value; }
        }

        [UxmlAttribute("search-text")][CreateProperty]
        public string SearchText
        {
            get => _textField?.value ?? "";
            set { if (_textField != null) _textField.value = value; }
        }

        // ========= CALLBACKS (set by consuming screen) =========
        public Func<string, Task<List<OpenFoodFactsProduct>>> SearchProductsAsync { get; set; }
        public Func<Task<List<GenericFood>>> GetGenericFoodsAsync { get; set; }
        public Func<OpenFoodFactsProduct, float, string, Task> OnProductConfirmed { get; set; }
        public Func<string, Task> OnCreateItemAsync { get; set; }
        public Action<string> OnTextChanged { get; set; }

        private Func<string, Task<FoodProduct>> _importFromBarcodeAsync;
        public Func<string, Task<FoodProduct>> ImportFromBarcodeAsync
        {
            get => _importFromBarcodeAsync;
            set
            {
                _importFromBarcodeAsync = value;
                _scanButton.style.display = value != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // ========= CONSTRUCTOR =========
        public FMSearchOrCategoryField()
        {
            style.flexDirection = FlexDirection.Column;

            // Top row: TextField + ScanButton
            var searchRow = new VisualElement();
            searchRow.style.flexDirection = FlexDirection.Row;
            searchRow.style.alignItems = Align.Center;

            _textField = new Unity.AppUI.UI.TextField
            {
                placeholder = "Search products or select category..."
            };
            _textField.style.flexGrow = 1;
            _textField.AddToClassList("fm-scf-field");
            searchRow.Add(_textField);

            _actionButton = new Unity.AppUI.UI.Button();
            _actionButton.style.position = Position.Absolute;
            _actionButton.style.right = 0;
            _actionButton.quiet = true;
            _actionButton.leadingIcon = "fm-add-icon";
            searchRow.Add(_actionButton);

            _scanButton = new Unity.AppUI.UI.IconButton
            {
                icon = "barcode",
                quiet = true,
                tooltip = "Scan barcode"
            };
            _scanButton.style.minWidth = 36;
            _scanButton.style.minHeight = 36;
            _scanButton.style.marginRight = 4;
            _scanButton.style.display = DisplayStyle.None;
            searchRow.Add(_scanButton);

            Add(searchRow);

            _spinner = new CircularProgress { size = Size.S };
            _spinner.style.display = DisplayStyle.None;
            _spinner.style.alignSelf = Align.Center;
            _spinner.style.marginTop = 8;
            Add(_spinner);

            _resultsContainer = new VisualElement();
            _resultsContainer.style.display = DisplayStyle.None;
            _resultsContainer.AddToClassList("fm-scf-results");
            Add(_resultsContainer);

            _categoryContainer = new VisualElement();
            _resultsContainer.Add(_categoryContainer);

            _searchResultsContainer = new VisualElement();
            _resultsContainer.Add(_searchResultsContainer);

            _confirmContainer = new VisualElement();
            _confirmContainer.style.display = DisplayStyle.None;
            _resultsContainer.Add(_confirmContainer);

            var selectedLabel = new Text { text = "Selected:" };
            selectedLabel.style.marginBottom = 4;
            _confirmContainer.Add(selectedLabel);

            _selectedNameLabel = new Text();
            _selectedNameLabel.style.marginBottom = 8;
            _selectedNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _confirmContainer.Add(_selectedNameLabel);

            var qtyLabel = new Text { text = "Quantity" };
            qtyLabel.style.marginBottom = 4;
            _confirmContainer.Add(qtyLabel);

            _qtyField = new Unity.AppUI.UI.FloatField { value = 1f };
            _qtyField.style.marginBottom = 8;
            _confirmContainer.Add(_qtyField);

            var unitLabel = new Text { text = "Unit" };
            unitLabel.style.marginBottom = 4;
            _confirmContainer.Add(unitLabel);

            var unitChoices = new List<string>
            {
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_PIECES"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_G"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_KG"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_ML"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_L"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_CUPS"),
            };
            _unitDropdown = new Dropdown { sourceItems = unitChoices, selectedIndex = 0 };
            _unitDropdown.bindItem = (item, i) => item.label = unitChoices[i];
            _unitDropdown.style.marginBottom = 8;
            _confirmContainer.Add(_unitDropdown);

            var confirmRow = new VisualElement();
            confirmRow.style.flexDirection = FlexDirection.Row;
            confirmRow.style.justifyContent = Justify.SpaceBetween;

            var cancelBtn = new Unity.AppUI.UI.Button { title = "Cancel", quiet = true };
            cancelBtn.clicked += () => ResetToIdle();
            confirmRow.Add(cancelBtn);

            var addBtn = new Unity.AppUI.UI.Button { title = "Add to list", variant = ButtonVariant.Accent };
            addBtn.clicked += OnAddClicked;
            confirmRow.Add(addBtn);

            _confirmContainer.Add(confirmRow);

            // Wire events
            _textField.RegisterCallback<FocusEvent>(OnTextFieldFocused);

            // Use inner field for real-time keystroke changes (AppUI wrapper only fires on Enter/blur)
            _textField.schedule.Execute(() =>
            {
                var innerField = _textField.Q<UnityEngine.UIElements.TextField>();
                if (innerField != null)
                {
                    innerField.RegisterValueChangedCallback(OnTextFieldValueChanged);
                }
            }).ExecuteLater(0);

            _scanButton.clicked += OnScanClicked;
        }

        private void OnTextFieldFocused(FocusEvent evt)
        {
            if (_currentMode != Mode.Idle) return;

            string val = _textField.value;
            if (string.IsNullOrWhiteSpace(val))
            {
                _ = ShowGenericFoodsAsync();
            }
        }

        private async Task ShowGenericFoodsAsync()
        {
            if (GetGenericFoodsAsync == null) return;

            SetMode(Mode.Categories);
            _categoryContainer.Clear();

            List<GenericFood> genericFoods = await GetGenericFoodsAsync();

            _genericFoods = genericFoods ?? new List<GenericFood>();

            // Collect unique food groups (Level 1)
            var groups = new List<string>();
            var seen = new HashSet<string>();
            foreach (var gf in _genericFoods)
            {
                string group = string.IsNullOrEmpty(gf.foodGroup) ? "Other" : gf.foodGroup;
                if (seen.Add(group)) groups.Add(group);
            }

            // Recent products heading
            if (_recentProducts.Count > 0)
            {
                var recentHeading = new Text { text = "RECENTLY ADDED" };
                recentHeading.AddToClassList("fm-scf-heading");
                _categoryContainer.Add(recentHeading);

                foreach (var prod in _recentProducts)
                {
                    var row = MakeResultRow(prod, OnResultClicked);
                    _categoryContainer.Add(row);
                }
            }

            // Food groups heading
            var groupHeading = new Text { text = "CATEGORIES" };
            groupHeading.AddToClassList("fm-scf-heading");
            _categoryContainer.Add(groupHeading);

            foreach (var group in groups)
            {
                var row = new VisualElement();
                row.AddToClassList("fm-scf-category-row");

                var label = new Text { text = group };
                label.style.flexGrow = 1;
                row.Add(label);

                var arrow = new Text { text = ">" };
                arrow.style.opacity = 0.4f;
                row.Add(arrow);

                string capturedGroup = group;
                row.RegisterCallback<ClickEvent>(_ => ShowItemsForGroup(capturedGroup));
                _categoryContainer.Add(row);
            }

            _resultsContainer.style.display = DisplayStyle.Flex;
        }

        private void ShowItemsForGroup(string foodGroup)
        {
            _categoryContainer.Clear();

            // Back button row
            var backRow = new VisualElement();
            backRow.AddToClassList("fm-scf-back-row");
            var backLabel = new Text { text = $"\u2190 {foodGroup}" };
            backRow.Add(backLabel);
            backRow.RegisterCallback<ClickEvent>(evt => { _ = ShowGenericFoodsAsync(); });
            _categoryContainer.Add(backRow);

            // Items in this group
            foreach (var gf in _genericFoods)
            {
                string group = string.IsNullOrEmpty(gf.foodGroup) ? "Other" : gf.foodGroup;
                if (group != foodGroup) continue;

                var row = new VisualElement();
                row.AddToClassList("fm-scf-category-row");

                var label = new Text { text = gf.foodName };
                row.Add(label);

                GenericFood captured = gf;
                row.RegisterCallback<ClickEvent>(_ => OnGenericFoodClicked(captured));
                _categoryContainer.Add(row);
            }
        }

        private void OnGenericFoodClicked(GenericFood genericFood)
        {
            _textField.value = genericFood.foodName;
            _categoryContainer.Clear();
            _resultsContainer.style.display = DisplayStyle.None;
            // Triggers search via RegisterValueChangedCallback
        }

        private void OnTextFieldValueChanged(ChangeEvent<string> evt)
        {
            OnTextChanged?.Invoke(evt.newValue);

            string query = evt.newValue;
            if (string.IsNullOrWhiteSpace(query))
            {
                if (_currentMode == Mode.Searching)
                {
                    ResetToIdle();
                }
                return;
            }

            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            CancellationToken ct = _debounceCts.Token;

            _ = DebouncedSearchAsync(query, ct);
        }

        private async Task DebouncedSearchAsync(string query, CancellationToken ct)
        {
            try
            {
                await Task.Delay(400, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (ct.IsCancellationRequested) return;

            SetMode(Mode.Searching);
            _categoryContainer.style.display = DisplayStyle.None;
            _searchResultsContainer.Clear();
            _spinner.style.display = DisplayStyle.Flex;

            if (SearchProductsAsync != null)
            {
                List<OpenFoodFactsProduct> results = await SearchProductsAsync(query);

                if (ct.IsCancellationRequested) return;

                _spinner.style.display = DisplayStyle.None;

                if (results != null && results.Count > 0)
                {
                    foreach (var product in results)
                    {
                        var row = MakeResultRow(product, OnResultClicked);
                        _searchResultsContainer.Add(row);
                    }
                }
                else
                {
                    var noResults = new Text
                    {
                        text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NO_RESULTS")
                    };
                    noResults.AddToClassList("fm-scf-no-results");
                    _searchResultsContainer.Add(noResults);

                    if (OnCreateItemAsync != null)
                    {
                        var createRow = new VisualElement();
                        createRow.AddToClassList("fm-scf-create-row");
                        var createLabel = new Text { text = $"+ Create \"{query}\"" };
                        createRow.Add(createLabel);
                        string capturedQuery = query;
                        createRow.RegisterCallback<ClickEvent>(_ => OnCreateClicked(capturedQuery));
                        _searchResultsContainer.Add(createRow);
                    }
                }

                _resultsContainer.style.display = DisplayStyle.Flex;
                _searchResultsContainer.style.display = DisplayStyle.Flex;
            }
        }

        private void OnResultClicked(object item)
        {
            SetMode(Mode.Confirmed);
            _selectedItem = item;

            _searchResultsContainer.style.display = DisplayStyle.None;
            _categoryContainer.style.display = DisplayStyle.None;

            string name;
            if (item is OpenFoodFactsProduct food)
            {
                string brands = food.brands?.Length > 0 ? string.Join(", ", food.brands) : "";
                name = string.IsNullOrEmpty(brands) ? food.name : $"{food.name} · {brands}";
            }
            else
            {
                name = item.ToString();
            }

            _selectedNameLabel.text = name;
            _confirmContainer.style.display = DisplayStyle.Flex;
            _textField.value = "";
        }

        private async void OnAddClicked()
        {
            if (_selectedItem is OpenFoodFactsProduct product && OnProductConfirmed != null)
            {
                float qty = _qtyField.value;
                int unitIdx = _unitDropdown.selectedIndex >= 0 ? _unitDropdown.selectedIndex : 0;
                string[] unitValues = { "PIECES", "G", "KG", "ML", "L", "CUPS" };
                string unit = unitValues[unitIdx];

                try
                {
                    await OnProductConfirmed(product, qty, unit);
                    ResetToIdle();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{GetType().Name}] OnProductConfirmed failed: {ex.Message}");
                }
            }
        }

        private async void OnCreateClicked(string query)
        {
            if (OnCreateItemAsync != null)
            {
                try
                {
                    await OnCreateItemAsync(query);
                    ResetToIdle();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{GetType().Name}] OnCreateItemAsync failed: {ex.Message}");
                }
            }
        }

        private async void OnScanClicked()
        {
            if (ImportFromBarcodeAsync != null)
            {
                BarcodeScanOverlay.Show(this, async barcode =>
                {
                    var foodItem = await ImportFromBarcodeAsync(barcode);
                    if (foodItem != null)
                    {
                        OnResultClicked(new OpenFoodFactsProduct
                        {
                            name = foodItem.name,
                            barcode = foodItem.barcode,
                            brands = Array.Empty<string>(),
                        });
                    }
                });
            }
        }

        private void SetMode(Mode mode)
        {
            _currentMode = mode;
        }

        private void ResetToIdle()
        {
            SetMode(Mode.Idle);
            _selectedItem = null;
            _textField.value = "";
            _resultsContainer.style.display = DisplayStyle.None;
            _categoryContainer.style.display = DisplayStyle.None;
            _categoryContainer.Clear();
            _searchResultsContainer.style.display = DisplayStyle.None;
            _searchResultsContainer.Clear();
            _confirmContainer.style.display = DisplayStyle.None;
            _spinner.style.display = DisplayStyle.None;
        }

        private void ShowError(string message)
        {
            var errorLabel = new Text { text = message };
            errorLabel.style.color = Color.red;
            errorLabel.style.marginTop = 8;
            _categoryContainer.Add(errorLabel);
            _resultsContainer.style.display = DisplayStyle.Flex;
        }

        private static VisualElement MakeResultRow(object item, Action<object> onClick)
        {
            var row = new VisualElement();
            row.AddToClassList("fm-scf-result-row");

            string text;
            if (item is OpenFoodFactsProduct food)
            {
                string brands = food.brands?.Length > 0 ? string.Join(", ", food.brands) : "";
                text = string.IsNullOrEmpty(brands) ? food.name : $"{food.name} · {brands}";
            }
            else if (item is GenericFood gf)
            {
                text = gf.foodName;
            }
            else
            {
                text = item.ToString();
            }

            var label = new Text { text = text };
            label.style.flexGrow = 1;
            row.Add(label);

            object captured = item;
            row.RegisterCallback<ClickEvent>(_ => onClick(captured));
            return row;
        }
    }
}
