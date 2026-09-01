using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine;


namespace eu.foodmission.platform.Components
{
    public static class FMProductSearchDialog
    {
        private static CancellationTokenSource _searchCts;

        public static void ShowFoodSearch(
            VisualElement anchor,
            string title,
            Func<string, Task<List<OpenFoodFactsProduct>>> searchAsync,
            Func<OpenFoodFactsProduct, float, string, Task> onConfirmed,
            Func<string, Task<FoodProduct>> importFromBarcodeAsync = null)
        {
            object selectedItem = null;

            var ui = BuildBaseUI(out var toggleRow);
            toggleRow.style.display = DisplayStyle.None;

            void OnItemSelected(object obj)
            {
                selectedItem = obj;
                ui.selectedNameLabel.text = FormatFoodName((OpenFoodFactsProduct)obj);
                ui.searchField.style.display = DisplayStyle.None;
                ui.resultsScroll.style.display = DisplayStyle.None;
                ui.confirmContainer.style.display = DisplayStyle.Flex;
            }

            WireSearch(ui, async query =>
            {
                var results = await searchAsync(query);
                return results != null ? new List<object>(results) : new List<object>();
            }, isDual: false, onItemSelected: OnItemSelected);

            WireScanButton(ui, anchor, title, importFromBarcodeAsync, OnItemSelected);

            ShowDialog(anchor, title, ui, () => selectedItem != null,
                       () => onConfirmed((OpenFoodFactsProduct)selectedItem, ui.qtyUnitPanel.Quantity, ui.qtyUnitPanel.Unit));
        }

        public static void ShowDualSearch(
            VisualElement anchor,
            string title,
            Func<string, Task<List<OpenFoodFactsProduct>>> searchFoodsAsync,
            Func<string, Task<List<GenericFood>>> searchGenericFoodsAsync,
            Func<object, float, string, Task> onConfirmed,
            Func<string, Task<FoodProduct>> importFromBarcodeAsync = null)
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
                    var results = await searchGenericFoodsAsync(query);
                    return results != null ? new List<object>(results) : new List<object>();
                }
            }, isDual: true, onItemSelected: obj =>
            {
                selectedItem = obj;
                string name = isFoodSearch
                    ? FormatFoodName((OpenFoodFactsProduct)obj)
                    : ((GenericFood)obj).foodName;
                ui.selectedNameLabel.text = name;
                ui.searchField.style.display = DisplayStyle.None;
                toggleRow.style.display = DisplayStyle.None;
                ui.resultsScroll.style.display = DisplayStyle.None;
                ui.confirmContainer.style.display = DisplayStyle.Flex;
            });

            WireScanButton(ui, anchor, title, importFromBarcodeAsync, obj =>
            {
                selectedItem = obj;
                string name = FormatFoodName((OpenFoodFactsProduct)obj);
                ui.selectedNameLabel.text = name;
                ui.searchField.style.display = DisplayStyle.None;
                toggleRow.style.display = DisplayStyle.None;
                ui.resultsScroll.style.display = DisplayStyle.None;
                ui.confirmContainer.style.display = DisplayStyle.Flex;
            });

            ShowDialog(anchor, title, ui, () => selectedItem != null,
                       () => onConfirmed(selectedItem, ui.qtyUnitPanel.Quantity, ui.qtyUnitPanel.Unit));
        }

        private struct SearchUI
        {
            public VisualElement container;
            public VisualElement searchRow;
            public Unity.AppUI.UI.IconButton scanButton;
            public Unity.AppUI.UI.TextField searchField;
            public CircularProgress searchSpinner;
            public ScrollView resultsScroll;
            public VisualElement resultsContainer;
            public VisualElement confirmContainer;
            public Text selectedNameLabel;
            public FMQuantityUnitPanel qtyUnitPanel;
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

            var searchRow = new VisualElement();
            searchRow.style.flexDirection = FlexDirection.Row;
            searchRow.style.marginBottom = 8;
            searchRow.style.alignItems = Align.Center;
            container.Add(searchRow);



            var searchField = new Unity.AppUI.UI.TextField { placeholder = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SEARCH_PLACEHOLDER") };
            searchField.style.flexGrow = 1;
            searchRow.Add(searchField);

            var spacer = new Unity.AppUI.UI.Spacer();
            spacer.spacing = SpacerSpacing.L;
            searchRow.Add(spacer);

            var scanButton = new Unity.AppUI.UI.IconButton
            {
                icon = "barcode",
                quiet = true,
            };
            scanButton.style.width = 72;
            scanButton.style.height = 72;
            scanButton.style.minWidth = 36;
            scanButton.style.marginLeft = 5;
            scanButton.style.marginRight = 30;
            scanButton.style.display = DisplayStyle.None;
            scanButton.tooltip = "Scan barcode";
            searchRow.Add(scanButton);

            var searchSpinner = new CircularProgress { size = Size.S };
            searchSpinner.style.display = DisplayStyle.None;
            searchSpinner.style.alignSelf = Align.Center;
            container.Add(searchSpinner);

            var resultsScroll = new ScrollView();
            resultsScroll.style.maxHeight = 600;
            resultsScroll.style.display = DisplayStyle.None;
            resultsScroll.style.marginTop = 15;
            resultsScroll.style.borderTopWidth = 2f;
            resultsScroll.style.borderBottomWidth = 2f;
            resultsScroll.style.borderLeftWidth = 2f;
            resultsScroll.style.borderRightWidth = 2f;
            resultsScroll.style.borderBottomLeftRadius = 25f;
            resultsScroll.style.borderBottomRightRadius = 25f;
            resultsScroll.style.borderTopLeftRadius = 25f;
            resultsScroll.style.borderTopRightRadius = 25f;
            resultsScroll.style.paddingTop = 40;
            resultsScroll.style.paddingBottom = 40;
            resultsScroll.style.paddingRight = 40;
            resultsScroll.style.paddingLeft = 40;
            resultsScroll.style.borderBottomColor = new StyleColor(Color.grey);
            resultsScroll.style.backgroundColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f, 0.25f));

            var resultsContainer = new VisualElement();


            resultsScroll.Add(resultsContainer);
            container.Add(resultsScroll);

            var confirmContainer = new VisualElement();
            confirmContainer.style.display = DisplayStyle.None;

            var selectedNameLabel = new Text();
            selectedNameLabel.style.marginBottom = 8;

            var qtyUnitPanel = new FMQuantityUnitPanel();

            confirmContainer.Add(selectedNameLabel);
            confirmContainer.Add(qtyUnitPanel);
            container.Add(confirmContainer);

            return new SearchUI
            {
                container = container,
                searchRow = searchRow,
                scanButton = scanButton,
                searchField = searchField,
                searchSpinner = searchSpinner,
                resultsScroll = resultsScroll,
                resultsContainer = resultsContainer,
                confirmContainer = confirmContainer,
                selectedNameLabel = selectedNameLabel,
                qtyUnitPanel = qtyUnitPanel
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

        private static void WireScanButton(
            SearchUI ui,
            VisualElement anchor,
            string title,
            Func<string, Task<FoodProduct>> importFromBarcodeAsync,
            Action<object> onItemSelected)
        {
            if (importFromBarcodeAsync == null) return;

            ui.scanButton.style.display = DisplayStyle.Flex;
            ui.scanButton.clicked += () =>
            {
                BarcodeScanOverlay.Show(anchor, barcode =>
                {
                    ImportAndSelectAsync(anchor, title, barcode, importFromBarcodeAsync, onItemSelected);
                });
            };
        }

        private static async void ImportAndSelectAsync(
            VisualElement anchor,
            string title,
            string barcode,
            Func<string, Task<FoodProduct>> importAsync,
            Action<object> onItemSelected)
        {
            try
            {
                var foodItem = await importAsync(barcode);
                if (foodItem != null)
                {
                    onItemSelected(new OpenFoodFactsProduct
                    {
                        name = foodItem.name,
                        barcode = foodItem.barcode,
                        brands = Array.Empty<string>(),
                    });
                }
                else
                {
                    FMDialog.ShowAlert(anchor, title, $"Could not find product for barcode: {barcode}", AlertSemantic.Warning);
                }
            }
            catch (Exception ex)
            {
                FMDialog.ShowAlert(anchor, title, $"Error importing barcode: {ex.Message}", AlertSemantic.Error);
            }
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
                resultRow.style.marginTop = 10;
                resultRow.style.marginBottom = 10;
                resultRow.style.borderBottomLeftRadius = 25f;
                resultRow.style.borderBottomRightRadius = 25f;
                resultRow.style.borderTopLeftRadius = 25f;
                resultRow.style.borderTopRightRadius = 25f;
                //resultRow.style.backgroundColor = new StyleColor(new Color(0.95f,0.95f,0.95f,0.36f));

                resultRow.style.height = 72;

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
                else if (item is GenericFood gf)
                {
                    rowText = gf.foodName;
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
                }, ButtonVariant.Accent));
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
