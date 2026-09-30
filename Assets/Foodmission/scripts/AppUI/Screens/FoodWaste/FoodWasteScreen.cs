using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class FoodWasteScreen : NavigationScreenBase<FoodWasteViewModel>
    {
        private FMPantryPickerField _pantrySearch;
        private VisualElement _mainContent;
        private VisualElement _expiredSection;
        private VisualElement _expiredContainer;
        private Text _expiredTitle;
        private Unity.AppUI.UI.Button _btnMoveAll;
        private Text _historyTitle;
        private Text _emptyState;
        private FMArrowStepper _monthStepper;
        private VisualElement _historyContainer;
        private readonly List<DateTime> _stepperMonths = new();

        private const int MonthsInStepper = 12;

        private AccessibilityNode _searchNode;
        private AccessibilityNode _moveAllNode;

        override protected bool IsFixedContent => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;

        public FoodWasteScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.FoodWaste));
            CacheUIElements();
        }

        private static string L(string key)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
        }

        private static CultureInfo CurrentCulture()
        {
            return LocalizationSettings.SelectedLocale?.Identifier.CultureInfo ?? CultureInfo.CurrentCulture;
        }

        private void CacheUIElements()
        {
            _pantrySearch = contentContainer.Q<FMPantryPickerField>("pantry-search");
            _mainContent = contentContainer.Q<VisualElement>("main-content");
            _expiredSection = contentContainer.Q<VisualElement>("expired-section");
            _expiredContainer = contentContainer.Q<VisualElement>("expired-container");
            _expiredTitle = contentContainer.Q<Text>("expired-title");
            _btnMoveAll = contentContainer.Q<Unity.AppUI.UI.Button>("btn-move-all-to-waste");
            _historyTitle = contentContainer.Q<Text>("history-title");
            _emptyState = contentContainer.Q<Text>("empty-state");
            _monthStepper = contentContainer.Q<FMArrowStepper>("month-stepper");
            _historyContainer = contentContainer.Q<VisualElement>("history-container");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            ApplyTexts();

            if (_pantrySearch != null)
            {
                _pantrySearch.SearchAsync = query => _viewModel.SearchCandidatesAsync(query);
                _pantrySearch.FormatItem = view =>
                    $"{view.DisplayName} · {FoodWasteFormatter.FormatQuantity(view.Item.quantity, UnitCatalog.Current.GetLabel(view.Item.unit))}";
                _pantrySearch.OnItemSelected += ShowRecordOverlay;
                _pantrySearch.OnPopoverVisibilityChanged += OnPopoverVisibilityChanged;
            }
            if (_btnMoveAll != null)
            {
                _btnMoveAll.clicked += OnMoveAllClicked;
            }
            SetupMonthStepper();
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            RebuildExpired();
            RebuildHistory();
            UpdateLoadingState();

            _ = SafeLoadAsync();
        }

        protected override void OnViewModelUnbinding()
        {
            if (_pantrySearch != null)
            {
                _pantrySearch.OnItemSelected -= ShowRecordOverlay;
                _pantrySearch.OnPopoverVisibilityChanged -= OnPopoverVisibilityChanged;
                _pantrySearch.SearchAsync = null;
                _pantrySearch.FormatItem = null;
                _pantrySearch.ClearSearch();
            }
            if (_btnMoveAll != null)
            {
                _btnMoveAll.clicked -= OnMoveAllClicked;
            }
            _monthStepper?.UnregisterValueChangedCallback(OnMonthChanged);
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            base.OnViewModelUnbinding();
        }

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null)
            {
                return;
            }

            _searchNode = CreateNode(_accessibilityHierarchy, _pantrySearch?.InputElement, L("FW_SEARCH_PROMPT"), AccessibilityRole.SearchField);
            _moveAllNode = CreateNode(_accessibilityHierarchy, _btnMoveAll, L("MOVE_TO_WASTE"), AccessibilityRole.Button);
        }

        protected override void TeardownAccessibilityNodes()
        {
            _searchNode = null;
            _moveAllNode = null;
            base.TeardownAccessibilityNodes();
        }

        private AccessibilityNode CreateNode(AccessibilityHierarchy hierarchy, VisualElement element, string label, AccessibilityRole role)
        {
            if (element == null)
            {
                return null;
            }
            var node = hierarchy.AddNode(label);
            node.role = role;
            node.frameGetter = () =>
            {
                if (element.panel == null)
                {
                    return Rect.zero;
                }
                var r = element.worldBound;
                var s = element.panel.scaledPixelsPerPoint;
                return new Rect(r.position * s, r.size * s);
            };
            node.invoked += () =>
            {
                using var evt = NavigationSubmitEvent.GetPooled();
                evt.target = element;
                element.SendEvent(evt);
                return true;
            };
            return node;
        }

        // --------------------------------------------------------------------
        // Rendering
        // --------------------------------------------------------------------

        private void ApplyTexts()
        {
            if (_pantrySearch != null)
            {
                _pantrySearch.Placeholder = L("FW_SEARCH_PROMPT");
                _pantrySearch.NoResultsText = L("FW_SEARCH_NO_RESULTS");
            }
            if (_expiredTitle != null)
            {
                _expiredTitle.text = L("FW_EXPIRED_TITLE");
            }
            if (_historyTitle != null)
            {
                _historyTitle.text = L("FW_HISTORY_TITLE");
            }
            if (_emptyState != null)
            {
                _emptyState.text = L("FW_MONTH_EMPTY");
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.History):
                    RebuildHistory();
                    break;
                case nameof(_viewModel.ExpiredItems):
                    RebuildExpired();
                    break;
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
            }
        }

        private void RebuildExpired()
        {
            _expiredContainer.Clear();

            List<PantryItemView> items = _viewModel.ExpiredItems;
            bool hasItems = items != null && items.Count > 0;
            _expiredSection.EnableInClassList("visible", hasItems);
            if (!hasItems)
            {
                return;
            }

            CultureInfo culture = CurrentCulture();
            foreach (PantryItemView view in items)
            {
                PantryItemView captured = view;
                var card = new FMItemPantry
                {
                    Text = captured.DisplayName,
                    Detail = FoodWasteFormatter.FormatQuantity(captured.Item.quantity, UnitCatalog.Current.GetLabel(captured.Item.unit)),
                    ExpiryText = FoodWasteFormatter.FormatShortDate(captured.Item.expiryDate, culture)
                };
                card.ExpiryLabel.AddToClassList("fm-fw-expired-label");
                card.InfoButton.style.display = DisplayStyle.None;
                card.OpenButton.clicked += () => ShowRecordOverlay(captured);
                card.RemoveButton.clicked += () => ShowRecordOverlay(captured);
                _expiredContainer.Add(card);
            }
        }

        private void SetupMonthStepper()
        {
            if (_monthStepper == null)
            {
                return;
            }

            _monthStepper.UnregisterValueChangedCallback(OnMonthChanged);
            _stepperMonths.Clear();

            CultureInfo culture = CurrentCulture();
            DateTime current = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            List<string> choices = new();
            for (int i = MonthsInStepper - 1; i >= 0; i--)
            {
                DateTime month = current.AddMonths(-i);
                _stepperMonths.Add(month);
                choices.Add(FoodWasteFormatter.FormatMonth(month.ToString("yyyy-MM", CultureInfo.InvariantCulture), culture));
            }

            _monthStepper.Cyclic = false;
            _monthStepper.Choices = choices.ToArray();
            int selected = _stepperMonths.IndexOf(_viewModel.SelectedMonth);
            _monthStepper.SelectedIndex = selected >= 0 ? selected : _stepperMonths.Count - 1;
            // Registered after setting the index: setting SelectedIndex fires valueChanged.
            _monthStepper.RegisterValueChangedCallback(OnMonthChanged);
        }

        private void OnMonthChanged(object sender, ChangeEvent<int> evt)
        {
            if (evt.newValue >= 0 && evt.newValue < _stepperMonths.Count)
            {
                _ = SafeSetMonthAsync(_stepperMonths[evt.newValue]);
            }
        }

        private void RebuildHistory()
        {
            _historyContainer.Clear();

            List<FoodWaste> history = _viewModel.History;
            bool isEmpty = history == null || history.Count == 0;
            _emptyState?.EnableInClassList("visible", isEmpty);
            if (isEmpty)
            {
                return;
            }

            CultureInfo culture = CurrentCulture();
            string unknown = L("UNKNOWN");

            foreach (FoodWaste waste in history)
            {
                FoodWaste captured = waste;
                string reasonKey = FoodWasteFormatter.GetReasonKey(captured.wasteReason);
                string reasonText = reasonKey != null ? L(reasonKey) : captured.wasteReason;

                var card = new FMItemFoodWaste
                {
                    Text = FoodWasteFormatter.GetDisplayName(captured, unknown),
                    Detail = FoodWasteFormatter.FormatDetail(captured.quantity, UnitCatalog.Current.GetLabel(captured.unit), reasonText),
                    DateText = FoodWasteFormatter.FormatShortDate(captured.wastedAt, culture)
                };
                card.RemoveButton.clicked += () => ConfirmDelete(captured.id);
                _historyContainer.Add(card);
            }
        }

        private void UpdateLoadingState()
        {
            if (_viewModel.IsLoading)
            {
                FMLoadingOverlay.Show();
            }
            else
            {
                FMLoadingOverlay.Hide();
            }
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, L("ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        // --------------------------------------------------------------------
        // Actions
        // --------------------------------------------------------------------

        private void ShowRecordOverlay(PantryItemView view)
        {
            if (view?.Item == null)
            {
                return;
            }
            FoodWasteRecordOverlay.Show(this, view, onSaved: () => _viewModel?.OnWasteRecorded());
        }

        private void OnPopoverVisibilityChanged(bool isVisible)
        {
            _mainContent?.EnableInClassList("fm-fw-hidden", isVisible);
        }

        private void ConfirmDelete(string wasteId)
        {
            FMDialog.ShowConfirm(
                this,
                "@UI:FW_DELETE_CONFIRM_TITLE",
                L("FW_DELETE_CONFIRM_MSG"),
                onConfirm: async () =>
                {
                    bool ok = await SafeDeleteAsync(wasteId);
                    if (ok)
                    {
                        ShowPositiveToast(L("FW_DELETE_SUCCESS"));
                    }
                },
                semantic: AlertSemantic.Destructive);
        }

        private void OnMoveAllClicked()
        {
            int count = _viewModel.ExpiredItems?.Count ?? 0;
            if (count == 0)
            {
                return;
            }

            string message = count == 1
                ? L("MOVE_EXPIRED_MSG")
                : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MOVE_EXPIRED_MSG_PLURAL", new object[] { count });

            FMDialog.ShowConfirm(
                this,
                "@UI:MOVE_TO_WASTE",
                message,
                onConfirm: async () =>
                {
                    int wasted = await SafeBatchWasteAsync();
                    if (wasted > 0)
                    {
                        string key = wasted == 1 ? "ITEMS_MOVED_WASTE" : "ITEMS_MOVED_WASTE_PLURAL";
                        ShowPositiveToast(LocalizationSettings.StringDatabase.GetLocalizedString("UI", key, new object[] { wasted }));
                    }
                },
                semantic: AlertSemantic.Destructive);
        }

        private void ShowPositiveToast(string message)
        {
            Toast.Build(this, message, NotificationDuration.Short)
                .SetStyle(NotificationStyle.Positive)
                .SetPosition(PopupNotificationPlacement.Bottom)
                .Show();
        }

        private async Task SafeLoadAsync()
        {
            try
            {
                await _viewModel.LoadAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FoodWasteScreen] LoadAsync failed: {ex.Message}");
            }
        }

        private async Task SafeSetMonthAsync(DateTime month)
        {
            try
            {
                await _viewModel.SetSelectedMonthAsync(month);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FoodWasteScreen] SetSelectedMonthAsync failed: {ex.Message}");
            }
        }

        private async Task<bool> SafeDeleteAsync(string wasteId)
        {
            try
            {
                return await _viewModel.DeleteWasteAsync(wasteId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FoodWasteScreen] DeleteWasteAsync failed: {ex.Message}");
                return false;
            }
        }

        private async Task<int> SafeBatchWasteAsync()
        {
            try
            {
                return await _viewModel.BatchWasteExpiredAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FoodWasteScreen] BatchWasteExpiredAsync failed: {ex.Message}");
                return 0;
            }
        }
    }
}
