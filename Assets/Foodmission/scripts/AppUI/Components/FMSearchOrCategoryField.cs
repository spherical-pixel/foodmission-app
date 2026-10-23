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

using eu.foodmission.platform;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMSearchOrCategoryField : VisualElement
    {
        // ========= UXML ATTRIBUTES =========
        [UxmlAttribute("placeholder")]
        [CreateProperty]
        public string Placeholder
        {
            get => _textField?.placeholder ?? "";
            set { if (_textField != null) _textField.placeholder = value; }
        }

        [UxmlAttribute("search-text")]
        [CreateProperty]
        public string SearchText
        {
            get => _textField?.value ?? "";
            set { if (_textField != null) _textField.value = value; }
        }

        [UxmlAttribute("skip-quantity-confirmation")]
        [CreateProperty]
        public bool SkipQuantityConfirmation { get; set; }

        [UxmlAttribute("open-food-info-on-select")]
        [CreateProperty]
        public bool OpenFoodInfoOnSelect { get; set; }

        [UxmlAttribute("auto-open-categories")]
        [CreateProperty]
        public bool AutoOpenCategories { get; set; }

        // ========= CALLBACKS (set by consuming screen) =========
        // (query, page, includeGeneric, includeProducts) -> one page of results, see FoodSearchFlow
        public Func<string, int, bool, bool, Task<FoodSearchPage>> SearchFoodsAsync { get; set; }
        public Func<Task<List<FoodGroupItem>>> GetFoodGroupsAsync { get; set; }
        public Func<OpenFoodFactsProduct, float?, string, Task> OnProductConfirmed { get; set; }
        public Func<GenericFood, float?, string, Task> OnGenericFoodConfirmed { get; set; }

        public Func<string, int, int, Task<PaginatedGenericFoodResponse>> SearchByFoodGroupAsync { get; set; }
        public Func<string, Task> OnCreateItemAsync { get; set; }
        public Action<string> OnTextChanged { get; set; }
        public Action<bool> OnPopoverVisibilityChanged { get; set; }
        public Action<OpenFoodFactsProduct> OnProductInfoRequested { get; set; }
        public Action<GenericFood> OnGenericFoodInfoRequested { get; set; }

        private static readonly Dictionary<string, string> CategoryEmojis = new()
        {
            // Slug keys (Primary for icon matching)
            { "alcoholic-beverages", "🍺" },
            { "bread", "🍞" },
            { "cereal-products-and-seeds", "🌾" },
            { "cereal-products-and-types-of-flour", "🌾" },
            { "cheese", "🧀" },
            { "cold-meat-cuts", "🥩" },
            { "eggs", "🥚" },
            { "fats-and-oils", "🫒" },
            { "fats-oils-and-savoury-spreads", "🫒" },
            { "fish-crustacean-shellfish", "🐟" },
            { "fish-crustacean-and-shellfish", "🐟" },
            { "foods-for-special-nutritional-use", "🍼" },
            { "fruit", "🍎" },
            { "fruits", "🍎" },
            { "herbs-and-spices", "🌿" },
            { "legumes", "🫘" },
            { "meat-and-poultry", "🍗" },
            { "meat-substitutes-and-dairy-substitutes", "🧈" },
            { "milk-and-milk-products", "🥛" },
            { "miscellaneous", "📦" },
            { "miscellaneous-foods", "📦" },
            { "mixed-dishes", "🍽️" },
            { "prepared-dishes", "🍽️" },
            { "non-alcoholic-beverages", "🥤" },
            { "nuts-and-seeds", "🥜" },
            { "pastry-and-biscuits", "🥐" },
            { "potatoes-and-tubers", "🥔" },
            { "savoury-bread-spreads", "🧴" },
            { "savoury-sauces", "🥫" },
            { "savouries", "🍿" },
            { "savoury-snacks", "🍿" },
            { "snacks", "🍿" },
            { "soups", "🍜" },
            { "sugar-sweets-sweet-spreads-and-sauce", "🍯" },
            { "sugar-sweets-and-sweet-sauces", "🍯" },
            { "vegetables", "🥦" },

            // Legacy English string keys (backward compatibility & test compatibility)
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

        public static IReadOnlyDictionary<string, string> CategoryEmojisPublic => CategoryEmojis;

        private static readonly List<FoodGroupItem> DefaultFoodGroupItems = new()
        {
            new FoodGroupItem { slug = "alcoholic-beverages", name = "Alcoholic beverages" },
            new FoodGroupItem { slug = "bread", name = "Bread" },
            new FoodGroupItem { slug = "cereal-products-and-types-of-flour", name = "Cereal products and types of flour" },
            new FoodGroupItem { slug = "cheese", name = "Cheese" },
            new FoodGroupItem { slug = "cold-meat-cuts", name = "Cold meat cuts" },
            new FoodGroupItem { slug = "eggs", name = "Eggs" },
            new FoodGroupItem { slug = "fats-and-oils", name = "Fats and oils" },
            new FoodGroupItem { slug = "fish-crustacean-shellfish", name = "Fish, crustacean and shellfish" },
            new FoodGroupItem { slug = "foods-for-special-nutritional-use", name = "Foods for special nutritional use" },
            new FoodGroupItem { slug = "fruits", name = "Fruits" },
            new FoodGroupItem { slug = "herbs-and-spices", name = "Herbs and spices" },
            new FoodGroupItem { slug = "legumes", name = "Legumes" },
            new FoodGroupItem { slug = "meat-and-poultry", name = "Meat and poultry" },
            new FoodGroupItem { slug = "meat-substitutes-and-dairy-substitutes", name = "Meat substitutes and dairy substitutes" },
            new FoodGroupItem { slug = "milk-and-milk-products", name = "Milk and milk products" },
            new FoodGroupItem { slug = "miscellaneous-foods", name = "Miscellaneous foods" },
            new FoodGroupItem { slug = "mixed-dishes", name = "Mixed dishes" },
            new FoodGroupItem { slug = "non-alcoholic-beverages", name = "Non-alcoholic beverages" },
            new FoodGroupItem { slug = "nuts-and-seeds", name = "Nuts and seeds" },
            new FoodGroupItem { slug = "pastry-and-biscuits", name = "Pastry and biscuits" },
            new FoodGroupItem { slug = "potatoes-and-tubers", name = "Potatoes and tubers" },
            new FoodGroupItem { slug = "savoury-bread-spreads", name = "Savoury bread spreads" },
            new FoodGroupItem { slug = "savoury-sauces", name = "Savoury sauces" },
            new FoodGroupItem { slug = "savoury-snacks", name = "Savoury snacks" },
            new FoodGroupItem { slug = "soups", name = "Soups" },
            new FoodGroupItem { slug = "sugar-sweets-and-sweet-sauces", name = "Sugar, sweets and sweet sauces" },
            new FoodGroupItem { slug = "vegetables", name = "Vegetables" },
        };

        private const string AlcoholicBeveragesSlug = "alcoholic-beverages";

        // Keeps the backend order and moves alcoholic beverages to the end of the list
        public static List<FoodGroupItem> OrderForDisplay(IEnumerable<FoodGroupItem> groups)
        {
            var ordered = new List<FoodGroupItem>();
            var alcoholic = new List<FoodGroupItem>();
            if (groups == null)
            {
                return ordered;
            }

            foreach (FoodGroupItem group in groups)
            {
                if (group == null)
                {
                    continue;
                }

                if (group.slug == AlcoholicBeveragesSlug)
                {
                    alcoholic.Add(group);
                }
                else
                {
                    ordered.Add(group);
                }
            }
            ordered.AddRange(alcoholic);
            return ordered;
        }

        public static string ToSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string cleaned = System.Text.RegularExpressions.Regex.Replace(input.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-");
            return cleaned.Trim('-');
        }

        public static string GetCategoryEmoji(string groupOrSlug)
        {
            if (string.IsNullOrEmpty(groupOrSlug)) return "📦";
            if (CategoryEmojis.TryGetValue(groupOrSlug, out string emoji)) return emoji;
            string slug = ToSlug(groupOrSlug);
            if (CategoryEmojis.TryGetValue(slug, out string slugEmoji)) return slugEmoji;
            return "📦";
        }

        private Func<string, Task<(FoodProduct Result, ApiErrorResponse Error)>> _importFromBarcodeAsync;
        public Func<string, Task<(FoodProduct Result, ApiErrorResponse Error)>> ImportFromBarcodeAsync
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
        private string _lastResultsQuery;
        private static List<FoodGroupItem> s_SessionFoodGroupsCache;

        public static void ClearSessionCache()
        {
            s_SessionFoodGroupsCache = null;
        }
        private List<object> _recentProducts = new();
        private object _selectedItem;
        private bool _genericEnabled = true;
        private bool _productsEnabled;
        private const int PageSize = 10;
        private int _currentPage;
        private int _totalPages;
        private string _searchQuery;
        private int _searchPage;
        private bool _searchHasPages;
        private string _currentFoodGroup;
        private string _currentFoodGroupSlug;
        private VisualElement _paginationContainer;
        private CancellationTokenSource _categoryLoadCts;

        private Unity.AppUI.UI.TextField _textField;
        protected Unity.AppUI.UI.Button _actionButton;
        private VisualElement _resultsContainer;
        private VisualElement _categoryContainer;
        private VisualElement _searchResultsContainer;
        private CircularProgress _spinner;
        private Unity.AppUI.UI.IconButton _scanButton;
        private VisualElement _confirmContainer;
        private Text _selectedNameLabel;
        private FMQuantityUnitPanel _confirmPanel;

        private ExVisualElement _checkboxContainer;
        private Checkbox _checkboxGeneric;
        private Checkbox _checkboxOpenFoodFacts;

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

            _actionButton.style.display = DisplayStyle.None; // By now it's not in use here

            _scanButton = new Unity.AppUI.UI.IconButton
            {
                icon = "barcode",
                quiet = true,
                tooltip = "Scan barcode"
            };
            _scanButton.AddToClassList("fm-icon-button--on-main-bg");
            _scanButton.style.minWidth = 36;
            _scanButton.style.minHeight = 36;
            _scanButton.style.marginRight = 4;
            _scanButton.style.display = DisplayStyle.Flex;
            searchRow.Add(_scanButton);

            Add(searchRow);

            _checkboxContainer = new ExVisualElement();
            _checkboxContainer.style.flexDirection = FlexDirection.Row;

            _checkboxContainer.style.justifyContent = Justify.FlexStart;
            _checkboxContainer.style.paddingLeft = 36;
            _checkboxContainer.style.paddingRight = 36;
            _checkboxContainer.style.marginTop = 16;
            _checkboxContainer.style.marginBottom = 16;

            Add(_checkboxContainer);

            _checkboxGeneric = new Checkbox();
            _checkboxGeneric.value = CheckboxState.Checked;
            _checkboxGeneric.label = "@UI:TitleGenericFood";
            _checkboxGeneric.RegisterValueChangedCallback(evt =>
            {
                _genericEnabled = evt.newValue == CheckboxState.Checked;
                ReRunSearchIfActive();
            });
            _checkboxContainer.Add(_checkboxGeneric);

            Spacer space = new Spacer
            {
                spacing = Unity.AppUI.UI.SpacerSpacing.M
            };
            _checkboxContainer.Add(space);

            _checkboxOpenFoodFacts = new Checkbox();
            _checkboxOpenFoodFacts.value = CheckboxState.Unchecked;
            _checkboxOpenFoodFacts.label = "@UI:TitleOpenFoodFactsProducts";
            _checkboxOpenFoodFacts.RegisterValueChangedCallback(evt =>
            {
                _productsEnabled = evt.newValue == CheckboxState.Checked;
                ReRunSearchIfActive();
            });
            _checkboxContainer.Add(_checkboxOpenFoodFacts);
            // TODO: Hidded checkbox selector as OFF kicks off by reaching limit with this searchs
            //_checkboxContainer.style.display = DisplayStyle.None;

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

            _confirmPanel = new FMQuantityUnitPanel();
            _confirmContainer.Add(_confirmPanel);

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

            _paginationContainer = new VisualElement();
            _paginationContainer.style.display = DisplayStyle.None;
            _paginationContainer.style.flexDirection = FlexDirection.Row;
            _paginationContainer.style.justifyContent = Justify.SpaceBetween;
            _paginationContainer.style.alignItems = Align.Center;
            _paginationContainer.style.marginTop = 8;
            _paginationContainer.style.marginBottom = 8;
            _paginationContainer.AddToClassList("fm-scf-pagination");
            _resultsContainer.Add(_paginationContainer);

            // Wire events
            _textField.RegisterCallback<FocusEvent>(OnTextFieldFocused);
            _textField.RegisterCallback<ClickEvent>(OnTextFieldClicked);

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

        public void OpenCategories()
        {
            if (string.IsNullOrWhiteSpace(_textField?.value))
            {
                _ = ShowGenericFoodsAsync();
            }
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (panel?.visualTree != null)
                panel.visualTree.RegisterCallback<PointerDownEvent>(OnPanelPointerDown, TrickleDown.TrickleDown);

            if (AutoOpenCategories)
            {
                schedule.Execute(() =>
                {
                    if (_currentMode == Mode.Idle && string.IsNullOrWhiteSpace(_textField?.value))
                    {
                        _ = ShowGenericFoodsAsync();
                    }
                }).ExecuteLater(0);
            }
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

            if (Contains(target)) return;

            if (AutoOpenCategories)
            {
                if (_currentMode == Mode.Categories)
                {
                    // In AutoOpenCategories mode, clicking outside when categories are showing does NOT close them
                    return;
                }

                if (_currentMode == Mode.Searching)
                {
                    _textField?.Blur();
                    if (string.IsNullOrWhiteSpace(_textField?.value))
                    {
                        _ = ShowGenericFoodsAsync();
                    }
                    return;
                }
            }

            // Keep what the user typed: only fold the results away
            if (!string.IsNullOrWhiteSpace(_textField?.value))
            {
                CollapseKeepingText();
                return;
            }

            ResetToIdle();
        }

        private void OnTextFieldFocused(FocusEvent evt)
        {
            ReopenFromIdle();
        }

        private void OnTextFieldClicked(ClickEvent evt)
        {
            ReopenFromIdle();
        }

        private void ReopenFromIdle()
        {
            if (_currentMode != Mode.Idle) return;

            string val = _textField.value;
            if (string.IsNullOrWhiteSpace(val))
            {
                _ = ShowGenericFoodsAsync();
                return;
            }

            ReopenSearchResults(val);
        }

        private void CollapseKeepingText()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
            _categoryLoadCts?.Cancel();
            _categoryLoadCts?.Dispose();
            _categoryLoadCts = null;
            _textField.Blur();

            SetMode(Mode.Idle);
            _resultsContainer.style.display = DisplayStyle.None;
            _categoryContainer.style.display = DisplayStyle.None;
            _searchResultsContainer.style.display = DisplayStyle.None;
            _confirmContainer.style.display = DisplayStyle.None;
            _paginationContainer.style.display = DisplayStyle.None;
            _spinner.style.display = DisplayStyle.None;
        }

        private void ReopenSearchResults(string query)
        {
            SetMode(Mode.Searching);

            // Results still match the text: show them again without a new request
            if (_lastResultsQuery == query && _searchResultsContainer.childCount > 0)
            {
                _categoryContainer.style.display = DisplayStyle.None;
                _confirmContainer.style.display = DisplayStyle.None;
                _searchResultsContainer.style.display = DisplayStyle.Flex;
                _paginationContainer.style.display = _searchHasPages ? DisplayStyle.Flex : DisplayStyle.None;
                _resultsContainer.style.display = DisplayStyle.Flex;
                return;
            }

            ReRunSearchIfActive();
        }

        private async Task ShowGenericFoodsAsync()
        {
            if (GetFoodGroupsAsync == null && CategoryEmojis.Count == 0) return;

            _categoryLoadCts?.Cancel();
            _categoryLoadCts?.Dispose();
            _categoryLoadCts = new CancellationTokenSource();
            CancellationToken ct = _categoryLoadCts.Token;

            SetMode(Mode.Categories);

            _searchResultsContainer.style.display = DisplayStyle.None;
            _confirmContainer.style.display = DisplayStyle.None;
            _paginationContainer.style.display = DisplayStyle.None;
            _categoryContainer.style.display = DisplayStyle.Flex;
            _resultsContainer.style.display = DisplayStyle.Flex;
            _spinner.style.display = DisplayStyle.None;

            // 1. If session cache exists, use it immediately
            if (s_SessionFoodGroupsCache != null && s_SessionFoodGroupsCache.Count > 0)
            {
                RenderCategoryList(s_SessionFoodGroupsCache);
                return;
            }

            // 2. Pre-render instantly using default items (0ms latency visual feedback)
            RenderCategoryList(DefaultFoodGroupItems);

            // 3. Perform background fetch if callback is supplied and not yet cached
            if (GetFoodGroupsAsync != null)
            {
                try
                {
                    List<FoodGroupItem> groups = await GetFoodGroupsAsync();
                    if (ct.IsCancellationRequested) return;

                    if (groups != null && groups.Count > 0)
                    {
                        s_SessionFoodGroupsCache = groups;
                        RenderCategoryList(groups);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{GetType().Name}] Background category load failed: {ex.Message}");
                }
            }
        }

        private void RenderCategoryList(IEnumerable<FoodGroupItem> groups)
        {
            _categoryContainer.Clear();

            // Recent products heading
            if (_recentProducts != null && _recentProducts.Count > 0)
            {
                var recentHeading = new Unity.AppUI.UI.Heading { text = "@UI:txtRECENTLY_ADDED" };
                recentHeading.AddToClassList("fm-scf-heading");
                recentHeading.size = HeadingSize.S;
                _categoryContainer.Add(recentHeading);

                foreach (var prod in _recentProducts)
                {
                    var row = MakeResultRow(prod, OnResultClicked);
                    _categoryContainer.Add(row);
                }
            }

            // Food groups heading
            var groupHeading = new Unity.AppUI.UI.Heading { text = "@UI:txtCATEGORIES" };
            groupHeading.size = HeadingSize.S;
            groupHeading.AddToClassList("fm-scf-heading");
            _categoryContainer.Add(groupHeading);

            foreach (var group in OrderForDisplay(groups))
            {
                string emoji = GetCategoryEmoji(group.slug);
                string localized = group.name;
                var btn = new Unity.AppUI.UI.Button();
                btn.trailingIcon = "fm-arrow-right";
                btn.style.flexGrow = 1;
                btn.AddToClassList("fm-scf-category-row");
                btn.AddToClassList("fm-button-align-left");
                btn.title = $"{emoji} {localized}";
                btn.quiet = true;
                btn.RegisterCallback<ClickEvent>(_ => ShowItemsForGroup(group.name, group.slug));
                btn.size = Size.M;
                _categoryContainer.Add(btn);
            }
        }

        private void ShowItemsForGroup(string foodGroup, string foodGroupSlug = null)
        {
            _currentFoodGroup = foodGroup;
            _currentFoodGroupSlug = foodGroupSlug;
            _currentPage = 1;
            _ = LoadCategoryPageAsync();
        }

        private async Task LoadCategoryPageAsync()
        {
            if (SearchByFoodGroupAsync == null) return;

            _categoryLoadCts?.Cancel();
            _categoryLoadCts?.Dispose();
            _categoryLoadCts = new CancellationTokenSource();
            CancellationToken ct = _categoryLoadCts.Token;

            _categoryContainer.Clear();
            _paginationContainer.style.display = DisplayStyle.None;
            _spinner.style.display = DisplayStyle.Flex;

            PaginatedGenericFoodResponse response = null;
            try
            {
                response = await SearchByFoodGroupAsync(_currentFoodGroup, _currentPage, PageSize);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadCategoryPageAsync failed: {ex.Message}");
            }

            if (ct.IsCancellationRequested) return;

            _spinner.style.display = DisplayStyle.None;

            // Back button row
            var backRow = new VisualElement();
            backRow.AddToClassList("fm-scf-back-row");
            backRow.AddToClassList("fm-click-sound");
            string backEmoji = GetCategoryEmoji(_currentFoodGroupSlug ?? _currentFoodGroup);
            string backLocalized = _currentFoodGroup;
            var backLabel = new Unity.AppUI.UI.Heading { text = $"{backEmoji} {backLocalized}" };
            backLabel.size = HeadingSize.S;
            backLabel.style.flexGrow = 1;

            var icon = new Unity.AppUI.UI.Icon { iconName = "fm-arrow-left" };
            icon.style.marginRight = 5;
            backRow.Add(icon);

            backRow.Add(backLabel);
            backRow.RegisterCallback<ClickEvent>(evt => { _ = ShowGenericFoodsAsync(); });
            _categoryContainer.Add(backRow);

            var items = response?.items;
            _totalPages = response?.totalPages ?? 1;

            if (items != null && items.Length > 0)
            {
                foreach (var gf in items)
                {
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
            else
            {
                var noResults = new Text { text = "No items in this category" };
                noResults.AddToClassList("fm-scf-no-results");
                _categoryContainer.Add(noResults);
            }

            if (_totalPages > 1)
            {
                BuildPaginationBar(_currentPage, _totalPages, _currentPage < _totalPages, page =>
                {
                    _currentPage = page;
                    _ = LoadCategoryPageAsync();
                });
            }

            _categoryContainer.style.display = DisplayStyle.Flex;
            _searchResultsContainer.style.display = DisplayStyle.None;
            _confirmContainer.style.display = DisplayStyle.None;
            _resultsContainer.style.display = DisplayStyle.Flex;
        }

        /// <param name="totalPages">0 when unknown: the label then shows only the current page.</param>
        private void BuildPaginationBar(int currentPage, int totalPages, bool hasNextPage, Action<int> goToPage)
        {
            _paginationContainer.Clear();

            var prevBtn = new Unity.AppUI.UI.Button { title = "@UI:txtPrevious", quiet = true };
            prevBtn.leadingIcon = "fm-arrow-left";
            prevBtn.SetEnabled(currentPage > 1);
            prevBtn.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                if (currentPage > 1)
                {
                    goToPage(currentPage - 1);
                }
            });
            _paginationContainer.Add(prevBtn);

            string pageText = totalPages > 0
                ? new LocalizedOption("UI", "txtPage_n_of_pages", currentPage, totalPages).GetText()
                : new LocalizedOption("UI", "txtPage_n", currentPage).GetText();
            var pageLabel = new Text { text = pageText };
            pageLabel.style.flexGrow = 1;
            pageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _paginationContainer.Add(pageLabel);

            var nextBtn = new Unity.AppUI.UI.Button { title = "@UI:TXT_NEXT", quiet = true };
            nextBtn.trailingIcon = "fm-arrow-right";
            nextBtn.SetEnabled(hasNextPage);
            nextBtn.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                if (hasNextPage)
                {
                    goToPage(currentPage + 1);
                }
            });
            _paginationContainer.Add(nextBtn);

            _paginationContainer.style.display = DisplayStyle.Flex;
        }

        private void OnGenericFoodClicked(GenericFood genericFood)
        {
            Debug.Log($"[FMSearch] OnGenericFoodClicked: {genericFood.foodName} mode={_currentMode} openInfo={OpenFoodInfoOnSelect}");

            if (OpenFoodInfoOnSelect)
            {
                OnGenericFoodInfoRequested?.Invoke(genericFood);
                return;
            }

            if (SkipQuantityConfirmation)
            {
                ConfirmItemDirectly(genericFood);
                return;
            }

            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;

            SetMode(Mode.Confirmed);
            _selectedItem = genericFood;

            _searchResultsContainer.style.display = DisplayStyle.None;
            _categoryContainer.style.display = DisplayStyle.None;
            _paginationContainer.style.display = DisplayStyle.None;

            _selectedNameLabel.text = genericFood.foodName;
            _confirmContainer.style.display = DisplayStyle.Flex;
            _textField.value = "";
        }


        private void OnTextFieldValueChanged(ChangeEvent<string> evt)
        {
            Debug.Log($"[FMSearch] OnTextFieldValueChanged: '{evt.newValue}' mode={_currentMode}");
            OnTextChanged?.Invoke(evt.newValue);

            string query = evt.newValue;
            if (string.IsNullOrWhiteSpace(query))
            {
                if (_currentMode == Mode.Searching)
                {
                    Debug.Log($"[FMSearch] OnTextFieldValueChanged -> empty query, AutoOpen={AutoOpenCategories}");
                    if (AutoOpenCategories)
                    {
                        _ = ShowGenericFoodsAsync();
                    }
                    else
                    {
                        ResetToIdle();
                    }
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
                await Task.Delay(500, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (ct.IsCancellationRequested) return;

            if (_currentMode != Mode.Categories && _currentMode != Mode.Searching) return;

            _searchQuery = query;
            _searchPage = 1;
            await LoadSearchPageAsync(ct);
        }

        private void GoToSearchPage(int page)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();

            _searchPage = page;
            _ = LoadSearchPageAsync(_debounceCts.Token);
        }

        private async Task LoadSearchPageAsync(CancellationToken ct)
        {
            SetMode(Mode.Searching);
            _categoryContainer.style.display = DisplayStyle.None;
            _searchResultsContainer.Clear();
            _paginationContainer.style.display = DisplayStyle.None;
            _spinner.style.display = DisplayStyle.Flex;

            FoodSearchPage result = null;
            if (SearchFoodsAsync != null && (_genericEnabled || _productsEnabled))
            {
                try
                {
                    result = await SearchFoodsAsync(_searchQuery, _searchPage, _genericEnabled, _productsEnabled);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{GetType().Name}] LoadSearchPageAsync failed: {ex.Message}");
                }
            }

            if (ct.IsCancellationRequested) return;

            _spinner.style.display = DisplayStyle.None;

            // Each source keeps the order the API returned (see FoodSearchFlow)
            if (result?.Items != null && result.Items.Count > 0)
            {
                foreach (object item in result.Items)
                {
                    _searchResultsContainer.Add(MakeResultRow(item, OnResultClicked));
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
            }

            // OFF search doesn't count its matches: TotalPages is 0 then, and only HasNextPage is known
            _searchHasPages = result != null && (_searchPage > 1 || result.HasNextPage);
            if (_searchHasPages)
            {
                BuildPaginationBar(_searchPage, result.TotalPages, result.HasNextPage, GoToSearchPage);
            }

            _lastResultsQuery = _searchQuery;
            _resultsContainer.style.display = DisplayStyle.Flex;
            _searchResultsContainer.style.display = DisplayStyle.Flex;
        }

        private void OnResultClicked(object item)
        {
            string itemDesc = item is OpenFoodFactsProduct p ? p.name : (item is GenericFood g ? g.foodName : item.ToString());
            Debug.Log($"[FMSearch] OnResultClicked: {itemDesc} type={item.GetType().Name} mode={_currentMode} openInfo={OpenFoodInfoOnSelect}");

            if (OpenFoodInfoOnSelect)
            {
                if (item is OpenFoodFactsProduct product)
                    OnProductInfoRequested?.Invoke(product);
                else if (item is GenericFood genericFood)
                    OnGenericFoodInfoRequested?.Invoke(genericFood);
                return;
            }

            if (SkipQuantityConfirmation)
            {
                ConfirmItemDirectly(item);
                return;
            }

            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;

            SetMode(Mode.Confirmed);
            _selectedItem = item;

            _resultsContainer.style.display = DisplayStyle.Flex;
            _searchResultsContainer.style.display = DisplayStyle.None;
            _categoryContainer.style.display = DisplayStyle.None;
            _paginationContainer.style.display = DisplayStyle.None;

            string name;
            if (item is OpenFoodFactsProduct food)
            {
                string brands = food.brands?.Length > 0 ? string.Join(", ", food.brands) : "";
                name = string.IsNullOrEmpty(brands) ? food.name : $"{food.name} · {brands}";
            }
            else if (item is GenericFood gf)
            {
                name = gf.foodName;
            }
            else
            {
                name = item.ToString();
            }

            _selectedNameLabel.text = name;
            _confirmContainer.style.display = DisplayStyle.Flex;
            _textField.value = "";
        }

        private async void ConfirmItemDirectly(object item)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;

            if (item is OpenFoodFactsProduct product && OnProductConfirmed != null)
            {
                try
                {
                    await OnProductConfirmed(product, null, null);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{GetType().Name}] OnProductConfirmed failed: {ex.Message}");
                }
            }
            else if (item is GenericFood genericFood && OnGenericFoodConfirmed != null)
            {
                try
                {
                    await OnGenericFoodConfirmed(genericFood, null, null);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{GetType().Name}] OnGenericFoodConfirmed failed: {ex.Message}");
                }
            }
            ResetToIdle();
        }



        private async void OnAddClicked()
        {
            float qty = _confirmPanel.Quantity;
            string unit = _confirmPanel.Unit;

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

        private void OnScanClicked()
        {
            if (ImportFromBarcodeAsync != null)
            {
                BarcodeScanOverlay.Show(this, ImportScannedBarcodeAsync);
            }
        }

        private async void ImportScannedBarcodeAsync(string barcode)
        {
            try
            {
                var (foodItem, error) = await ImportFromBarcodeAsync(barcode);
                string shownBarcode = BarcodeScan.Normalize(barcode);
                switch (BarcodeScan.Classify(foodItem, error))
                {
                    case BarcodeScanOutcome.Found:
                        OnResultClicked(new OpenFoodFactsProduct
                        {
                            id = foodItem.id,
                            name = foodItem.name,
                            barcode = foodItem.barcode,
                            imageFrontUrl = foodItem.imageFrontUrl ?? foodItem.imageUrl,
                            nutritionGrade = foodItem.nutriscoreGrade,
                            ecoscoreGrade = foodItem.ecoscoreGrade,
                            novaGroup = foodItem.novaGroup,
                            brands = Array.Empty<string>(),
                        });
                        break;
                    case BarcodeScanOutcome.NotFound:
                        ShowBarcodeAlert("BARCODE_SCAN_NOT_FOUND_TITLE", "BARCODE_SCAN_NOT_FOUND", shownBarcode);
                        break;
                    case BarcodeScanOutcome.NoProductInfo:
                        // The backend keeps OFF entries without a name as "Unknown Product": don't offer them
                        ShowBarcodeAlert("BARCODE_SCAN_NOT_FOUND_TITLE", "BARCODE_SCAN_NO_INFO", shownBarcode);
                        break;
                    case BarcodeScanOutcome.Busy:
                        FMDialog.ShowAlert(this,
                            LocalizationSettings.StringDatabase.GetLocalizedString("UI", "BARCODE_SCAN_BUSY_TITLE"),
                            LocalizationSettings.StringDatabase.GetLocalizedString("UI", "BARCODE_SCAN_BUSY", new object[] { shownBarcode }),
                            AlertSemantic.Warning,
                            "@UI:TXT_RETRY",
                            () => ImportScannedBarcodeAsync(barcode),
                            "@UI:TXT_CANCEL",
                            () => { });
                        break;
                    default:
                        FMDialog.ShowApiError(this,
                            LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ALERT_ERROR_TITLE"),
                            error);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FMSearchOrCategoryField] Barcode scan error: {ex.Message}");
                FMDialog.ShowAlert(this,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ALERT_ERROR_TITLE"),
                    ex.Message,
                    AlertSemantic.Error);
            }
        }

        private void ShowBarcodeAlert(string titleKey, string messageKey, string barcode)
        {
            FMDialog.ShowAlert(this,
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", titleKey),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", messageKey, new object[] { barcode }),
                AlertSemantic.Warning);
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
            Debug.Log($"[FMSearch] ResetToIdle called, prevMode={_currentMode}, AutoOpen={AutoOpenCategories}");
            _selectedItem = null;
            _categoryLoadCts?.Cancel();
            _categoryLoadCts?.Dispose();
            _categoryLoadCts = null;
            _textField.value = "";
            _textField.Blur();

            if (AutoOpenCategories)
            {
                _ = ShowGenericFoodsAsync();
                return;
            }

            SetMode(Mode.Idle);
            _resultsContainer.style.display = DisplayStyle.None;
            _categoryContainer.style.display = DisplayStyle.None;
            _categoryContainer.Clear();
            _searchResultsContainer.style.display = DisplayStyle.None;
            _searchResultsContainer.Clear();
            _lastResultsQuery = null;
            _confirmContainer.style.display = DisplayStyle.None;
            _paginationContainer.style.display = DisplayStyle.None;
            _spinner.style.display = DisplayStyle.None;
        }

        private void ReRunSearchIfActive()
        {
            string query = _textField?.value;
            if (string.IsNullOrWhiteSpace(query)) return;
            if (_currentMode != Mode.Searching) return;

            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            CancellationToken ct = _debounceCts.Token;

            _ = DebouncedSearchAsync(query, ct);
        }

        private void ShowError(string message)
        {
            var errorLabel = new Text { text = message };
            errorLabel.style.color = Color.red;
            errorLabel.style.marginTop = 8;
            _categoryContainer.Add(errorLabel);
            _resultsContainer.style.display = DisplayStyle.Flex;
        }

        private VisualElement MakeResultRow(object item, Action<object> onClick)
        {
            var btn = new Unity.AppUI.UI.Button();
            btn.AddToClassList("fm-scf-result-row");
            btn.style.flexGrow = 1;
            btn.AddToClassList("fm-button-align-left");

            btn.quiet = true;
            btn.size = Size.M;

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

            btn.title = $"{text}";

            object captured = item;
            btn.RegisterCallback<ClickEvent>(_ => onClick(captured));

            return btn;
        }

    }
}
