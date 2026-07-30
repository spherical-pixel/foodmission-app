using System;
using System.ComponentModel;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.Core;
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
    class GroupsScreen : NavigationScreenBase<GroupsViewModel>
    {
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private VisualElement _groupsContainer;
        private Text _errorText;
        private Text _emptyState;
        private Unity.AppUI.UI.Button _btnFab;
        private FMSearchOrCreateField _searchOrCreateField;
        private UnityEngine.UIElements.TextField _searchField;

        private AccessibilityNode _fabButtonNode;

        public GroupsScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.Groups));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _groupsContainer = contentContainer.Q<VisualElement>("groups-container");
            _errorText = contentContainer.Q<Text>("error-message");
            _emptyState = contentContainer.Q<Text>("empty-state");
            _btnFab = contentContainer.Q<Unity.AppUI.UI.Button>("btn-fab");
            _searchOrCreateField = contentContainer.Q<FMSearchOrCreateField>("search-or-create-field");
            _searchField = _searchOrCreateField?.Q<UnityEngine.UIElements.TextField>();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_btnFab != null)
                _btnFab.clicked += OnFabClicked;

            if (_searchOrCreateField?.ActionButton != null)
                _searchOrCreateField.ActionButton.clicked += OnFabClicked;

            if (_searchField != null)
                _searchField.RegisterValueChangedCallback(OnSearchTextChanged);

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            RebuildGroups();
            UpdateLoadingState();
            UpdateErrorState();

            _ = _viewModel.LoadGroupsAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.LogError($"[{GetType().Name}] LoadGroupsAsync failed: {t.Exception}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected override void OnViewModelUnbinding()
        {
            if (_btnFab != null)
                _btnFab.clicked -= OnFabClicked;

            if (_searchOrCreateField?.ActionButton != null)
                _searchOrCreateField.ActionButton.clicked -= OnFabClicked;

            if (_searchField != null)
                _searchField.UnregisterValueChangedCallback(OnSearchTextChanged);

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

            _fabButtonNode = CreateButtonNode(_accessibilityHierarchy, _btnFab, "Create or join group");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _fabButtonNode = null;
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

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.Groups):
                    RebuildGroups();
                    break;
                case nameof(_viewModel.SearchText):
                    _viewModel.ApplyFilter();
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

        private void OnSearchTextChanged(ChangeEvent<string> evt)
        {
            _viewModel.SearchText = evt.newValue;
        }

        private void RebuildGroups()
        {
            _groupsContainer.Clear();

            if (_viewModel.Groups == null || _viewModel.Groups.Count == 0)
            {
                if (_emptyState != null)
                {
                    _emptyState.style.display = DisplayStyle.Flex;
                    _emptyState.EnableInClassList("visible", true);
                }
                return;
            }

            if (_emptyState != null)
            {
                _emptyState.style.display = DisplayStyle.None;
                _emptyState.EnableInClassList("visible", false);
            }

            foreach (UserGroup group in _viewModel.Groups)
            {
                UserGroup captured = group;

                FMItemGroup item = new FMItemGroup
                {
                    Text = captured.name,
                    Detail = !string.IsNullOrEmpty(captured.description) ? captured.description : "",
                    CountText = captured.members != null ? $"{captured.members.Length}" : "0"
                };

                item.OpenButton.clicked += () => OnGroupClicked(captured);

                _groupsContainer.Add(item);
            }
        }

        private void OnGroupClicked(UserGroup group)
        {
            _navController?.Navigate(
                Actions.groups_to_detail,
                new[] { new Argument("groupId", group.id) });
        }

        private void UpdateLoadingState()
        {
            bool isLoading = _viewModel.IsLoading;
            if (isLoading)
                FMLoadingOverlay.Show();
            else
                FMLoadingOverlay.Hide();

            if (_btnFab != null)
                _btnFab.SetEnabled(!isLoading);
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

        private void OnFabClicked()
        {
            string createLabel = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CREATE_GROUP");
            string joinLabel = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "JOIN_GROUP");

            FMDialog.ShowCustom(
                this,
                "@UI:CREATE_OR_JOIN",
                null,
                new FMDialogAction("@UI:TXT_CANCEL", null),
                new FMDialogAction(createLabel, () => _navController?.Navigate(Actions.go_to_groups_create)),
                new FMDialogAction(joinLabel, () => _navController?.Navigate(Actions.go_to_groups_join), isPrimary: true));
        }
    }
}
