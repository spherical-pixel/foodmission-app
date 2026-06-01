using System;
using System.Collections.Generic;
using System.Linq;
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
        public Func<GenericFood, float, string, Task> OnGenericFoodConfirmed { get; set; }
        public Func<string, Task<List<GenericFood>>> SearchGenericFoodsAsync { get; set; }
        public Func<string, Task> OnCreateItemAsync { get; set; }
        public Action<string> OnTextChanged { get; set; }
        public Action<bool> OnPopoverVisibilityChanged { get; set; }

        private static readonly Dictionary<string, string> CategoryEmojis = new()
        {
            { "Alcoholic beverages", "🍺" },
            { "Bread", "🍞" },
            { "Cereal products and types of flour", "🌾" },
            { "Cheese", "🧀" },
            { "Cold meat cuts", "🥩" },
            { "Eggs", "🥚" },
            { "Fats and oils", "🫒" },
            { "Fish, crustacean and shellfish", "🐟" },
            { "Foods for special nutritional use", "🍼" },
            { "Fruits", "🍎" },
            { "Herbs and spices", "🌿" },
            { "Legumes", "🫘" },
            { "Meat and poultry", "🍗" },
            { "Meat substitutes and dairy substitutes", "🧈" },
            { "Milk and milk products", "🥛" },
            { "Miscellaneous foods", "📦" },
            { "Mixed dishes", "🍽️" },
            { "Non-alcoholic beverages", "🥤" },
            { "Nuts and seeds", "🥜" },
            { "Pastry and biscuits", "🥐" },
            { "Potatoes and tubers", "🥔" },
            { "Savoury bread spreads", "🧴" },
            { "Savoury sauces", "🥫" },
            { "Savoury snacks", "🍿" },
            { "Soups", "🍜" },
            { "Sugar, sweets and sweet sauces", "🍯" },
            { "Vegetables", "🥦" },
        };

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

        /* ========= INTERNAL ELEMENTS ========= */

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

        // ========= CONSTRUCTOR =========
        public FMSearchOrCategoryField()
        {
            style.flexDirection = FlexDirection.Column;

            // Top row: TextField + ScanButton
            var searchRow = new VisualElement();
            searchRow.style.flexDirection = FlexDirection.Row;
            searchRow.style.alignItems = Align.Center;

            var searchField = new VisualElement();
            searchField.style.flexGrow = 1;
            //searchField.style.alignContent = Align.Center;
            searchField.style.justifyContent = Justify.Center;
            //searchField.style.alignItems = Align.Center;
            searchRow.Add(searchField);

            _textField = new Unity.AppUI.UI.TextField
            {
                placeholder = "Search products or select category..."
            };
            _textField.style.flexGrow = 1;
            _textField.AddToClassList("fm-scf-field");
            searchField.Add(_textField);

            _actionButton = new Unity.AppUI.UI.Button();
            _actionButton.style.position = Position.Absolute;
            _actionButton.style.right = 0;
            _actionButton.quiet = true;
            _actionButton.leadingIcon = "fm-add-icon";
            _actionButton.size = Size.L;
            searchField.Add(_actionButton);

            _scanButton = new Unity.AppUI.UI.IconButton
            {
                icon = "barcode",
                quiet = true,
                tooltip = "Scan barcode"
            };
            _scanButton.style.minWidth = 36;
            _scanButton.style.minHeight = 36;
            _scanButton.style.marginRight = 4;
            _scanButton.style.display = DisplayStyle.Flex;
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

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (panel?.visualTree != null)
                panel.visualTree.RegisterCallback<PointerDownEvent>(OnPanelPointerDown, TrickleDown.TrickleDown);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (panel?.visualTree != null)
                panel.visualTree.UnregisterCallback<PointerDownEvent>(OnPanelPointerDown, TrickleDown.TrickleDown);
        }

        private void OnPanelPointerDown(PointerDownEvent evt)
        {
            if (_currentMode == Mode.Idle || _currentMode == Mode.Confirmed) return;
            if (evt.target is not VisualElement target) return;

            if (_resultsContainer.Contains(target)) return;
            if (target == _textField || _textField.Contains(target)) return;

            ResetToIdle();
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
                var recentHeading = new Unity.AppUI.UI.Heading { text = "RECENTLY ADDED" };
                recentHeading.AddToClassList("fm-scf-heading");
                recentHeading.size = HeadingSize.M;
                _categoryContainer.Add(recentHeading);

                foreach (var prod in _recentProducts)
                {
                    var row = MakeResultRow(prod, OnResultClicked);
                    _categoryContainer.Add(row);
                }
            }

            // Food groups heading
            //var groupHeading = new Text { text = "CATEGORIES" };
            var groupHeading = new Unity.AppUI.UI.Heading { text = "CATEGORIES" };
            groupHeading.size = HeadingSize.M;

            groupHeading.AddToClassList("fm-scf-heading");
            _categoryContainer.Add(groupHeading);

            foreach (var group in groups)
            {
                string emoji = CategoryEmojis.TryGetValue(group, out string e) ? e : "📦";
                string localized = LocalizationSettings.StringDatabase.GetLocalizedString("UI", group + "_title");
                var btn = new Unity.AppUI.UI.Button();
                btn.trailingIcon = "fm-arrow-right";
                btn.style.flexGrow = 1;
                btn.AddToClassList("fm-scf-category-row");
                btn.AddToClassList("fm-button-align-left");
                btn.title = $"{emoji} {localized}";
                btn.quiet = true;
                btn.RegisterCallback<ClickEvent>(_ => ShowItemsForGroup(group));
                //btn.variant = ButtonVariant.Accent;
                btn.size = Size.M;
                _categoryContainer.Add(btn);
            }

            _categoryContainer.style.display = DisplayStyle.Flex;
            _searchResultsContainer.style.display = DisplayStyle.None;
            _confirmContainer.style.display = DisplayStyle.None;
            _resultsContainer.style.display = DisplayStyle.Flex;
        }

        private void ShowItemsForGroup(string foodGroup)
        {
            _categoryContainer.Clear();

            // Back button row
            var backRow = new VisualElement();
            backRow.AddToClassList("fm-scf-back-row");
            string backEmoji = CategoryEmojis.TryGetValue(foodGroup, out string be) ? be : "📦";
            string backLocalized = LocalizationSettings.StringDatabase.GetLocalizedString("UI", foodGroup + "_title");
            var backLabel = new Unity.AppUI.UI.Heading { text = $"{backEmoji} {backLocalized}" };
            backLabel.size = HeadingSize.M;

            var icon = new Unity.AppUI.UI.Icon { iconName = "fm-arrow-left" };
            icon.style.marginRight = 5;
            backRow.Add(icon);

            backRow.Add(backLabel);
            backRow.RegisterCallback<ClickEvent>(evt => { _ = ShowGenericFoodsAsync(); });
            _categoryContainer.Add(backRow);

            // Items in this group
            foreach (var gf in _genericFoods)
            {
                string group = string.IsNullOrEmpty(gf.foodGroup) ? "Other" : gf.foodGroup;
                if (group != foodGroup) continue;

                //var row = new VisualElement();
                var btn = new Unity.AppUI.UI.Button();
                btn.AddToClassList("fm-scf-category-row");
                btn.style.flexGrow = 1;
                btn.AddToClassList("fm-button-align-left");
                btn.title = $"{gf.foodName}";
                btn.quiet = true;
                btn.size = Size.M;

                GenericFood captured = gf;
                btn.RegisterCallback<ClickEvent>(_ => OnGenericFoodClicked(captured));
                _categoryContainer.Add(btn);
            }
        }

        private void OnGenericFoodClicked(GenericFood genericFood)
        {
            SetMode(Mode.Confirmed);
            _selectedItem = genericFood;

            _searchResultsContainer.style.display = DisplayStyle.None;
            _categoryContainer.style.display = DisplayStyle.None;

            _selectedNameLabel.text = genericFood.foodName;
            _confirmContainer.style.display = DisplayStyle.Flex;
            _textField.value = "";
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

            Task<List<GenericFood>> genericTask = SearchGenericFoodsAsync != null
                ? SearchGenericFoodsAsync(query)
                : Task.FromResult<List<GenericFood>>(null);

            Task<List<OpenFoodFactsProduct>> productTask = SearchProductsAsync != null
                ? SearchProductsAsync(query)
                : Task.FromResult<List<OpenFoodFactsProduct>>(null);

            await Task.WhenAll(genericTask, productTask);

            if (ct.IsCancellationRequested) return;

            _spinner.style.display = DisplayStyle.None;

            List<GenericFood> genericResults = genericTask.Result;
            List<OpenFoodFactsProduct> productResults = productTask.Result;

            bool hasGeneric = genericResults != null && genericResults.Count > 0;
            bool hasProducts = productResults != null && productResults.Count > 0;

            if (hasGeneric)
            {
                var items = genericResults.Cast<object>().ToList();
                BuildCollapsibleSection(
                    _searchResultsContainer,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SECTION_GENERIC_FOODS"),
                    items
                );
            }

            if (hasProducts)
            {
                var items = productResults.Cast<object>().ToList();
                BuildCollapsibleSection(
                    _searchResultsContainer,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SECTION_PRODUCTS"),
                    items
                );
            }

            if (!hasGeneric && !hasProducts)
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
            float qty = _qtyField.value;
            int unitIdx = _unitDropdown.selectedIndex >= 0 ? _unitDropdown.selectedIndex : 0;
            string[] unitValues = { "PIECES", "G", "KG", "ML", "L", "CUPS" };
            string unit = unitValues[unitIdx];

            if (_selectedItem is OpenFoodFactsProduct product && OnProductConfirmed != null)
            {
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
            else if (_selectedItem is GenericFood genericFood && OnGenericFoodConfirmed != null)
            {
                try
                {
                    await OnGenericFoodConfirmed(genericFood, qty, unit);
                    ResetToIdle();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{GetType().Name}] OnGenericFoodConfirmed failed: {ex.Message}");
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
            Mode prev = _currentMode;
            _currentMode = mode;

            bool wasOpen = prev != Mode.Idle;
            bool nowOpen = mode != Mode.Idle;
            if (wasOpen != nowOpen)
            {
                style.flexGrow = nowOpen ? 1 : 0;
                OnPopoverVisibilityChanged?.Invoke(nowOpen);
            }
        }

        private void ResetToIdle()
        {
            SetMode(Mode.Idle);
            _selectedItem = null;
            _textField.value = "";
            _textField.Blur();
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

        private void BuildCollapsibleSection(VisualElement container, string headingText, List<object> items)
        {
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.AddToClassList("fm-scf-result-row");

            var heading = new Unity.AppUI.UI.Heading
            {
                text = headingText,
                size = HeadingSize.M
            };
            heading.style.flexGrow = 1;
            heading.AddToClassList("fm-scf-heading");
            headerRow.Add(heading);

            var chevron = new Unity.AppUI.UI.Icon { iconName = "fm-arrow-down" };
            headerRow.Add(chevron);

            var itemsContainer = new VisualElement();

            bool expanded = true;
            headerRow.RegisterCallback<ClickEvent>(_ =>
            {
                expanded = !expanded;
                itemsContainer.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                chevron.iconName = expanded ? "fm-arrow-down" : "fm-arrow-right";
            });

            container.Add(headerRow);
            container.Add(itemsContainer);

            foreach (var item in items)
            {
                itemsContainer.Add(MakeResultRow(item, OnResultClicked));
            }
        }
    }
}
