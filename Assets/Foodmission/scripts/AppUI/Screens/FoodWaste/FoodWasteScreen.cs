using System;
using System.ComponentModel;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Accessibility;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;

using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [Preserve]
    class FoodWasteScreen : NavigationScreenBase<FoodWasteViewModel>
    {
        private VisualElement _wasteContainer;
        private Text _errorText;
        private Text _emptyState;
        private Unity.AppUI.UI.Button _btnAdd;
        private ScrollView _scrollView;

        private AccessibilityNode _addButtonNode;

        public FoodWasteScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.FoodWaste));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _wasteContainer = contentContainer.Q<VisualElement>("waste-container");
            _errorText = contentContainer.Q<Text>("error-message");
            _emptyState = contentContainer.Q<Text>("empty-state");
            _btnAdd = contentContainer.Q<Unity.AppUI.UI.Button>("btn-add-waste");
            _scrollView = contentContainer.Q<ScrollView>("scroll-view");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            _btnAdd.clicked += OnAddClicked;
            _scrollView?.RegisterCallback<GeometryChangedEvent>(OnScrollChanged);
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            RebuildGroups();
            UpdateLoadingState();
            UpdateErrorState();

            _ = _viewModel.LoadAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.LogError($"[{GetType().Name}] LoadAsync failed: {t.Exception}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected override void OnViewModelUnbinding()
        {
            _btnAdd.clicked -= OnAddClicked;
            if (_scrollView != null)
                _scrollView.UnregisterCallback<GeometryChangedEvent>(OnScrollChanged);
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

            _addButtonNode = CreateButtonNode(_accessibilityHierarchy, _btnAdd, "Add food waste");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _addButtonNode = null;
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

        private void OnScrollChanged(GeometryChangedEvent evt)
        {
            if (_scrollView?.verticalScroller == null) return;
            if (_scrollView.verticalScroller.value >= _scrollView.verticalScroller.highValue - 50f)
            {
                _ = _viewModel.LoadNextPageAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        Debug.LogError($"[{GetType().Name}] LoadNextPageAsync failed: {t.Exception}");
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.Groups):
                    RebuildGroups();
                    break;
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.ErrorMessage):
                    UpdateErrorState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
            }
        }

        private void RebuildGroups()
        {
            _wasteContainer.Clear();

            if (_viewModel.Groups == null || _viewModel.Groups.Count == 0)
            {
                _emptyState?.EnableInClassList("visible", true);
                return;
            }

            _emptyState?.EnableInClassList("visible", false);

            foreach (FoodWasteGroup group in _viewModel.Groups)
            {
                var header = new Text { text = FormatMonth(group.MonthKey) };
                header.AddToClassList("fm-fw-section-header");
                _wasteContainer.Add(header);

                foreach (FoodWaste item in group.Items)
                {
                    FoodWaste captured = item;

                    var row = new VisualElement();
                    row.AddToClassList("fm-fw-row");

                    var info = new VisualElement();
                    info.AddToClassList("fm-fw-row-info");

                    string foodName = captured.foodProduct?.name ?? captured.foodProductId ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
                    var nameLabel = new Text { text = foodName };
                    nameLabel.AddToClassList("fm-fw-row-name");

                    string detail = $"{captured.quantity} {captured.unit} · {FormatWasteReason(captured.wasteReason)}";
                    var detailLabel = new Text { text = detail };
                    detailLabel.AddToClassList("fm-fw-row-detail");

                    info.Add(nameLabel);
                    info.Add(detailLabel);
                    row.Add(info);

                    if (captured.carbonFootprint > 0)
                    {
                        var badge = new Text { text = $"{captured.carbonFootprint:F1} kg CO₂" };
                        badge.AddToClassList("fm-fw-row-badge");
                        badge.AddToClassList("fm-fw-row-badge--carbon");
                        row.Add(badge);
                    }

                    if (captured.costEstimate > 0)
                    {
                        var badge = new Text { text = $"€{captured.costEstimate:F2}" };
                        badge.AddToClassList("fm-fw-row-badge");
                        badge.AddToClassList("fm-fw-row-badge--cost");
                        row.Add(badge);
                    }

                    var deleteBtn = new IconButton { icon = "trash" };
                    string capturedId = captured.id;
                    deleteBtn.RegisterCallback<ClickEvent>(evt =>
                    {
                        evt.StopPropagation();
                        FMDialog.ShowConfirm(
                            this,
                            "@UI:DELETE_WASTE",
                            LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CONFIRM_DELETE_MSG", new object[] { foodName }),
                            onConfirm: () => _ = DeleteWasteSafeAsync(capturedId),
                            semantic: AlertSemantic.Destructive);
                    });
                    row.Add(deleteBtn);

                    _wasteContainer.Add(row);
                }
            }
        }

        private void UpdateLoadingState()
        {
            bool isLoading = _viewModel.IsLoading;
            if (isLoading)
                FMLoadingOverlay.Show();
            else
                FMLoadingOverlay.Hide();
            _btnAdd?.SetEnabled(!isLoading);
        }

        private void UpdateErrorState()
        {
            bool hasError = !string.IsNullOrEmpty(_viewModel.ErrorMessage);
            _errorText?.EnableInClassList("visible", hasError);
            if (_errorText != null)
                _errorText.text = _viewModel.ErrorMessage;
        }

        private void OnAddClicked()
        {
            _navController?.Navigate(Actions.go_to_foodwaste_add);
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private static string FormatWasteReason(string reason)
        {
            return reason switch
            {
                "EXPIRED" => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_EXPIRED"),
                "SPOILED" => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_SPOILED"),
                "OVERCOOKED" => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_OVERCOOKED"),
                "UNWANTED" => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_UNWANTED"),
                "PORTION_TOO_LARGE" => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_PORTION_LARGE"),
                "OTHER" => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_OTHER"),
                _ => reason
            };
        }

        private static string FormatMonth(string key)
        {
            if (string.IsNullOrEmpty(key) || key == "Unknown") return LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
            if (DateTime.TryParse(key + "-01", out DateTime dt))
                return dt.ToString("MMMM yyyy");
            return key;
        }

        private async Task DeleteWasteSafeAsync(string wasteId)
        {
            try
            {
                await _viewModel.DeleteWasteAsync(wasteId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FoodWasteScreen] Delete failed: {ex.Message}");
            }
        }
    }
}
