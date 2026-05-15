using System;
using System.ComponentModel;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [Preserve]
    class MealLogScreen : NavigationScreenBase<MealLogViewModel>
    {
        private VisualElement _logsContainer;
        private Text _errorText;
        private Text _emptyState;
        private Unity.AppUI.UI.Button _btnAdd;
        private ScrollView _scrollView;
        private AccessibilityNode _addButtonNode;

        public MealLogScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.MealLog));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _logsContainer = contentContainer.Q<VisualElement>("logs-container");
            _errorText = contentContainer.Q<Text>("error-message");
            _emptyState = contentContainer.Q<Text>("empty-state");
            _btnAdd = contentContainer.Q<Unity.AppUI.UI.Button>("btn-add-log");
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

            _viewModel.LoadAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.LogError($"[{GetType().Name}] LoadAsync failed: {t.Exception?.InnerException?.Message}");
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

            _addButtonNode = CreateButtonNode(_accessibilityHierarchy, _btnAdd, "Add meal log");
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
                _viewModel.LoadNextPageAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        Debug.LogError($"[{GetType().Name}] LoadNextPageAsync failed: {t.Exception?.InnerException?.Message}");
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
            _logsContainer.Clear();

            if (_viewModel.Groups == null || _viewModel.Groups.Count == 0)
            {
                _emptyState?.EnableInClassList("visible", true);
                return;
            }

            _emptyState?.EnableInClassList("visible", false);

            foreach (MealLogGroup group in _viewModel.Groups)
            {
                string typeLabel = FormatTypeOfMeal(group.TypeOfMeal);
                var header = new Text { text = typeLabel };
                header.AddToClassList("fm-ml-section-header");
                _logsContainer.Add(header);

                foreach (MealLog log in group.Logs)
                {
                    MealLog captured = log;

                    var row = new VisualElement();
                    row.AddToClassList("fm-ml-row");

                    var info = new VisualElement();
                    info.AddToClassList("fm-ml-row-info");

                    string mealName = captured.meal?.name ?? "Unknown meal";
                    var nameLabel = new Text { text = mealName };
                    nameLabel.AddToClassList("fm-ml-row-name");

                    string timeStr = FormatTimestamp(captured.timestamp);
                    var timeLabel = new Text { text = timeStr };
                    timeLabel.AddToClassList("fm-ml-row-time");

                    info.Add(nameLabel);
                    info.Add(timeLabel);
                    row.Add(info);

                    if (captured.mealFromPantry)
                    {
                        var badge = new Text { text = "PANTRY" };
                        badge.AddToClassList("fm-ml-row-badge");
                        badge.AddToClassList("fm-ml-row-badge--pantry");
                        row.Add(badge);
                    }

                    if (captured.eatenOut)
                    {
                        var badge = new Text { text = "OUT" };
                        badge.AddToClassList("fm-ml-row-badge");
                        badge.AddToClassList("fm-ml-row-badge--out");
                        row.Add(badge);
                    }

                    var deleteBtn = new IconButton { icon = "trash" };
                    deleteBtn.RegisterCallback<ClickEvent>(evt =>
                    {
                        evt.StopPropagation();
                        FMDialog.ShowConfirm(
                            this,
                            "@UI:DELETE_LOG",
                            LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CONFIRM_DELETE_MSG", new object[] { mealName }),
                            onConfirm: async () =>
                            {
                                try
                                {
                                    await _viewModel.DeleteLogAsync(captured.id);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError($"[{GetType().Name}] DeleteLogAsync failed: {ex.Message}");
                                }
                            },
                            semantic: AlertSemantic.Destructive);
                    });
                    row.Add(deleteBtn);

                    _logsContainer.Add(row);
                }
            }
        }

        private void UpdateLoadingState()
        {
            bool isLoading = _viewModel.IsLoading;
            if (isLoading)
                FMLoadingOverlay.Show(contentContainer);
            else
                FMLoadingOverlay.Hide(contentContainer);
            _btnAdd?.SetEnabled(!isLoading);
        }

        private void UpdateErrorState()
        {
            bool hasError = !string.IsNullOrEmpty(_viewModel.ErrorMessage);
            _errorText?.EnableInClassList("visible", hasError);
            if (_errorText != null)
                _errorText.text = _viewModel.ErrorMessage;
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private void OnAddClicked()
        {
            _navController?.Navigate(Actions.go_to_meallog_add);
        }

        private static string FormatTypeOfMeal(string type)
        {
            return type switch
            {
                "BREAKFAST" => "Breakfast",
                "LUNCH" => "Lunch",
                "DINNER" => "Dinner",
                "SNACK" => "Snack",
                "DRINKS" => "Drinks",
                "OTHER" => "Other",
                _ => type
            };
        }

        private static string FormatTimestamp(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return "";
            if (DateTime.TryParse(iso, out DateTime dt))
                return dt.ToLocalTime().ToString("HH:mm");
            return "";
        }
    }
}
