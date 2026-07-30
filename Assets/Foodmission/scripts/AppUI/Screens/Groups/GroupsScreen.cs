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
        private VisualElement _groupsContainer;
        private Text _errorText;
        private Text _emptyState;
        private Unity.AppUI.UI.Button _btnFab;

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
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            _btnFab.clicked += OnFabClicked;
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
            _btnFab.clicked -= OnFabClicked;
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
            _groupsContainer.Clear();

            if (_viewModel.Groups == null || _viewModel.Groups.Count == 0)
            {
                _emptyState?.EnableInClassList("visible", true);
                return;
            }

            _emptyState?.EnableInClassList("visible", false);

            foreach (UserGroup group in _viewModel.Groups)
            {
                UserGroup captured = group;

                var row = new VisualElement();
                row.AddToClassList("fm-grp-row");

                var info = new VisualElement();
                info.AddToClassList("fm-grp-row-info");

                var nameLabel = new Text { text = captured.name };
                nameLabel.AddToClassList("fm-grp-row-name");

                string desc = !string.IsNullOrEmpty(captured.description)
                    ? captured.description
                    : "";
                var descLabel = new Text { text = desc };
                descLabel.AddToClassList("fm-grp-row-desc");

                info.Add(nameLabel);
                info.Add(descLabel);

                string memberCount = captured.members != null
                    ? $"{captured.members.Length}"
                    : "0";
                var countLabel = new Text { text = memberCount };
                countLabel.AddToClassList("fm-grp-row-count");

                row.Add(info);
                row.Add(countLabel);

                row.RegisterCallback<ClickEvent>(_ =>
                    _navController?.Navigate(
                        Actions.groups_to_detail,
                        new[] { new Argument("groupId", captured.id) }));

                _groupsContainer.Add(row);
            }
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
                new FMDialogAction(createLabel, () => _navController?.Navigate(Actions.go_to_groups_create)),
                new FMDialogAction(joinLabel, () => _navController?.Navigate(Actions.go_to_groups_join), isPrimary: true));
        }
    }
}
