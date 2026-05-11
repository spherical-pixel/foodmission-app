using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform.Components
{
    public static class FMProductSearchDialog
    {
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
        private static CancellationTokenSource _searchCts;

        public static void ShowFoodSearch(
            VisualElement anchor,
            string title,
            Func<string, Task<List<OpenFoodFactsProduct>>> searchAsync,
            Func<OpenFoodFactsProduct, float, string, Task> onConfirmed)
        {
            object selectedItem = null;

            var ui = BuildBaseUI(out var toggleRow);
            toggleRow.style.display = DisplayStyle.None;

            WireSearch(ui, async query =>
            {
                var results = await searchAsync(query);
                return results != null ? new List<object>(results) : new List<object>();
            }, isDual: false, onItemSelected: obj =>
            {
                selectedItem = obj;
                ui.selectedNameLabel.text = FormatFoodName((OpenFoodFactsProduct)obj);
                ui.searchField.style.display = DisplayStyle.None;
                ui.resultsScroll.style.display = DisplayStyle.None;
                ui.confirmContainer.style.display = DisplayStyle.Flex;
            });

            ShowDialog(anchor, title, ui, () => selectedItem != null,
                       () => onConfirmed((OpenFoodFactsProduct)selectedItem, ui.qtyField.value, UnitValues[ui.unitDropdown.selectedIndex]));
        }

        public static void ShowDualSearch(
            VisualElement anchor,
            string title,
            Func<string, Task<List<OpenFoodFactsProduct>>> searchFoodsAsync,
            Func<string, Task<List<FoodCategory>>> searchCategoriesAsync,
            Func<object, float, string, Task> onConfirmed)
        {
            object selectedItem = null;
            bool isFoodSearch = true;

            var ui = BuildBaseUI(out var toggleRow);

            var btnFoodTab = new Unity.AppUI.UI.Button
            {
                title = "Products",
                variant = ButtonVariant.Accent
            };
            btnFoodTab.style.flexGrow = 1;
            btnFoodTab.style.marginRight = 8;

            var btnCategoryTab = new Unity.AppUI.UI.Button
            {
                title = "Categories",
                variant = ButtonVariant.Default
            };
            btnCategoryTab.style.flexGrow = 1;

            toggleRow.Add(btnFoodTab);
            toggleRow.Add(btnCategoryTab);

            void SetActiveTab(bool foodSelected)
            {
                isFoodSearch = foodSelected;
                btnFoodTab.variant = foodSelected ? ButtonVariant.Accent : ButtonVariant.Default;
                btnCategoryTab.variant = foodSelected ? ButtonVariant.Default : ButtonVariant.Accent;
                ui.resultsContainer.Clear();
                ui.resultsScroll.style.display = DisplayStyle.None;
                ui.confirmContainer.style.display = DisplayStyle.None;
                selectedItem = null;
                ui.searchField.value = "";
                ui.searchField.Focus();
            }

            btnFoodTab.clicked += () => SetActiveTab(true);
            btnCategoryTab.clicked += () => SetActiveTab(false);

            WireSearch(ui, async query =>
            {
                if (isFoodSearch)
                {
                    var results = await searchFoodsAsync(query);
                    return results != null ? new List<object>(results) : new List<object>();
                }
                else
                {
                    var results = await searchCategoriesAsync(query);
                    return results != null ? new List<object>(results) : new List<object>();
                }
            }, isDual: true, onItemSelected: obj =>
            {
                selectedItem = obj;
                string name = isFoodSearch
                    ? FormatFoodName((OpenFoodFactsProduct)obj)
                    : ((FoodCategory)obj).name;
                ui.selectedNameLabel.text = name;
                ui.searchField.style.display = DisplayStyle.None;
                toggleRow.style.display = DisplayStyle.None;
                ui.resultsScroll.style.display = DisplayStyle.None;
                ui.confirmContainer.style.display = DisplayStyle.Flex;
            });

            ShowDialog(anchor, title, ui, () => selectedItem != null,
                       () => onConfirmed(selectedItem, ui.qtyField.value, UnitValues[ui.unitDropdown.selectedIndex]));
        }

        private struct SearchUI
        {
            public VisualElement container;
            public Unity.AppUI.UI.TextField searchField;
            public CircularProgress searchSpinner;
            public ScrollView resultsScroll;
            public VisualElement resultsContainer;
            public VisualElement confirmContainer;
            public Text selectedNameLabel;
            public Unity.AppUI.UI.FloatField qtyField;
            public Dropdown unitDropdown;
        }

        private static SearchUI BuildBaseUI(out VisualElement toggleRow)
        {
            var container = new VisualElement();
            container.style.minWidth = 280;

            toggleRow = new VisualElement();
            toggleRow.style.flexDirection = FlexDirection.Row;
            toggleRow.style.marginBottom = 8;
            toggleRow.style.flexGrow = 1;
            container.Add(toggleRow);

            var searchField = new Unity.AppUI.UI.TextField { placeholder = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SEARCH_PLACEHOLDER") };
            searchField.style.marginBottom = 8;
            container.Add(searchField);

            var searchSpinner = new CircularProgress { size = Size.S };
            searchSpinner.style.display = DisplayStyle.None;
            searchSpinner.style.alignSelf = Align.Center;
            container.Add(searchSpinner);

            var resultsScroll = new ScrollView();
            resultsScroll.style.maxHeight = 200;
            resultsScroll.style.display = DisplayStyle.None;

            var resultsContainer = new VisualElement();
            resultsScroll.Add(resultsContainer);
            container.Add(resultsScroll);

            var confirmContainer = new VisualElement();
            confirmContainer.style.display = DisplayStyle.None;

            var selectedNameLabel = new Text();
            selectedNameLabel.style.marginBottom = 8;

            var qtyLabel = new Text { text = "Quantity" };
            qtyLabel.style.marginBottom = 4;

            var qtyField = new Unity.AppUI.UI.FloatField { value = 1f };
            qtyField.style.marginBottom = 8;

            var unitDropdown = new Dropdown { sourceItems = UnitChoices, selectedIndex = 0 };
            unitDropdown.bindItem = (item, i) => item.label = UnitChoices[i];
            unitDropdown.style.marginBottom = 8;

            confirmContainer.Add(selectedNameLabel);
            confirmContainer.Add(qtyLabel);
            confirmContainer.Add(qtyField);
            confirmContainer.Add(unitDropdown);
            container.Add(confirmContainer);

            return new SearchUI
            {
                container = container,
                searchField = searchField,
                searchSpinner = searchSpinner,
                resultsScroll = resultsScroll,
                resultsContainer = resultsContainer,
                confirmContainer = confirmContainer,
                selectedNameLabel = selectedNameLabel,
                qtyField = qtyField,
                unitDropdown = unitDropdown
            };
        }

        private static void WireSearch(
            SearchUI ui,
            Func<string, Task<List<object>>> searchAsync,
            bool isDual,
            Action<object> onItemSelected)
        {
            ui.searchField.schedule.Execute(() =>
            {
                var innerField = ui.searchField.Q<UnityEngine.UIElements.TextField>();
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
                        ui.resultsScroll.style.display = DisplayStyle.None;
                        ui.resultsContainer.Clear();
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

                    ui.searchSpinner.style.display = DisplayStyle.Flex;
                    ui.resultsScroll.style.display = DisplayStyle.None;
                    ui.resultsContainer.Clear();

                    List<object> results = await searchAsync(query);

                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    if (results?.Count > 0)
                    {
                        RenderResults(results, ui.resultsContainer, ui.resultsScroll, isDual, onItemSelected);
                    }

                    ui.searchSpinner.style.display = DisplayStyle.None;
                });
            }).ExecuteLater(0);
        }

        private static void RenderResults(
            List<object> items,
            VisualElement resultsContainer,
            ScrollView resultsScroll,
            bool isDual,
            Action<object> onItemSelected)
        {
            foreach (object item in items)
            {
                object captured = item;

                var resultRow = new VisualElement();
                resultRow.style.flexDirection = FlexDirection.Row;
                resultRow.style.paddingTop = 6;
                resultRow.style.paddingBottom = 6;

                string rowText;
                if (item is OpenFoodFactsProduct food)
                {
                    string brands = food.brands?.Length > 0
                        ? string.Join(", ", food.brands)
                        : "";
                    rowText = string.IsNullOrEmpty(brands)
                        ? food.name
                        : $"{food.name} · {brands}";
                }
                else if (item is FoodCategory cat)
                {
                    rowText = cat.name;
                }
                else
                {
                    rowText = item.ToString();
                }

                var label = new Text { text = rowText };
                label.style.flexGrow = 1;
                resultRow.Add(label);

                resultRow.RegisterCallback<ClickEvent>(_ => onItemSelected(captured));
                resultsContainer.Add(resultRow);
            }

            resultsScroll.style.display = DisplayStyle.Flex;
        }

        private static void ShowDialog(
            VisualElement anchor,
            string title,
            SearchUI ui,
            Func<bool> hasSelection,
            Func<Task> onAdd)
        {
            FMDialog.ShowCustom(
                anchor,
                title,
                ui.container,
                new FMDialogAction("@UI:TXT_CANCEL", null),
                new FMDialogAction("@UI:ADD_ITEM", async () =>
                {
                    if (!hasSelection())
                    {
                        FMDialog.ShowAlert(anchor, title, "@UI:SELECT_FIRST", AlertSemantic.Warning);
                        return;
                    }

                    try
                    {
                        await onAdd();
                    }
                    catch (Exception ex)
                    {
                        FMDialog.ShowAlert(anchor, title, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CANNOT_ADD_PRODUCT", new object[] { ex.Message }), AlertSemantic.Error);
                    }
                }, isPrimary: true));
        }

        private static string FormatFoodName(OpenFoodFactsProduct product)
        {
            string brands = product.brands?.Length > 0
                ? string.Join(", ", product.brands)
                : "";
            return string.IsNullOrEmpty(brands) ? product.name : $"{product.name} · {brands}";
        }
    }
}
