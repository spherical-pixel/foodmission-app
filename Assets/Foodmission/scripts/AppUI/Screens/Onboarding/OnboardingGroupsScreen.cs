using System;
using System.ComponentModel;

using eu.foodmission.platform.Components;

using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class OnboardingGroupsScreen : NavigationScreenBase<OnboardingGroupsViewModel>
    {
        private Unity.AppUI.UI.Button _btnCreate;
        private Unity.AppUI.UI.Button _btnJoin;
        private Unity.AppUI.UI.Button _skipButton;
        private Unity.AppUI.UI.TextField _createNameField;
        private Unity.AppUI.UI.TextField _joinCodeField;
        private Text _errorText;

        private AccessibilityNode _createButtonNode;
        private AccessibilityNode _joinButtonNode;
        private AccessibilityNode _skipButtonNode;
        private AccessibilityNode _createNameFieldNode;
        private AccessibilityNode _joinCodeFieldNode;

        protected override bool IsFixedContent => false;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;

        public OnboardingGroupsScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.OnboardingGroups));
            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _btnCreate = contentContainer.Q<Unity.AppUI.UI.Button>("btn-create");
            _btnJoin = contentContainer.Q<Unity.AppUI.UI.Button>("btn-join");
            _skipButton = contentContainer.Q<Unity.AppUI.UI.Button>("skip-button");
            _createNameField = contentContainer.Q<Unity.AppUI.UI.TextField>("create-name-field");
            _joinCodeField = contentContainer.Q<Unity.AppUI.UI.TextField>("join-code-field");
            _errorText = contentContainer.Q<Text>("error-message");
        }

        private void RegisterManualEvents()
        {
            if (_btnCreate != null)
                _btnCreate.clicked += OnCreateClicked;

            if (_btnJoin != null)
                _btnJoin.clicked += OnJoinClicked;

            if (_skipButton != null)
                _skipButton.clicked += OnSkipClicked;
        }

        private void UnregisterManualEvents()
        {
            if (_btnCreate != null)
                _btnCreate.clicked -= OnCreateClicked;

            if (_btnJoin != null)
                _btnJoin.clicked -= OnJoinClicked;

            if (_skipButton != null)
                _skipButton.clicked -= OnSkipClicked;
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnPropertyChanged;
                _viewModel.ShowErrorRequest += OnShowErrorRequested;
                _viewModel.ShowSuccessRequest += OnShowSuccessRequested;
                _viewModel.NavigationRequested += OnNavigationRequested;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnPropertyChanged;
                _viewModel.ShowErrorRequest -= OnShowErrorRequested;
                _viewModel.ShowSuccessRequest -= OnShowSuccessRequested;
                _viewModel.NavigationRequested -= OnNavigationRequested;
            }

            UnregisterManualEvents();
            base.OnViewModelUnbinding();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.ErrorMessage):
                    UpdateErrorState();
                    break;
            }
        }

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            var h = _accessibilityHierarchy;

            _createButtonNode = CreateButtonNode(h, _btnCreate, "Create group");
            _joinButtonNode = CreateButtonNode(h, _btnJoin, "Join group");
            _skipButtonNode = CreateButtonNode(h, _skipButton, "Skip groups");

            if (_createNameField != null)
            {
                _createNameFieldNode = h.AddNode("Group name");
                _createNameFieldNode.role = AccessibilityRole.TextField;
                _createNameFieldNode.frameGetter = MakeElementFrameGetter(_createNameField);
            }

            if (_joinCodeField != null)
            {
                _joinCodeFieldNode = h.AddNode("Invite code");
                _joinCodeFieldNode.role = AccessibilityRole.TextField;
                _joinCodeFieldNode.frameGetter = MakeElementFrameGetter(_joinCodeField);
            }
        }

        protected override void TeardownAccessibilityNodes()
        {
            _createButtonNode = null;
            _joinButtonNode = null;
            _skipButtonNode = null;
            _createNameFieldNode = null;
            _joinCodeFieldNode = null;
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

        private static Func<Rect> MakeElementFrameGetter(VisualElement element)
        {
            return () =>
            {
                if (element == null || element.panel == null) return Rect.zero;
                var r = element.worldBound;
                var s = element.panel.scaledPixelsPerPoint;
                return new Rect(r.position * s, r.size * s);
            };
        }

        private void UpdateLoadingState()
        {
            bool loading = _viewModel.IsLoading;
            if (loading)
                FMLoadingOverlay.Show(contentContainer);
            else
                FMLoadingOverlay.Hide(contentContainer);

            if (_btnCreate != null) _btnCreate.SetEnabled(!loading);
            if (_btnJoin != null) _btnJoin.SetEnabled(!loading);
            if (_skipButton != null) _skipButton.SetEnabled(!loading);
            if (_createNameField != null) _createNameField.SetEnabled(!loading);
            if (_joinCodeField != null) _joinCodeField.SetEnabled(!loading);
        }

        private void UpdateErrorState()
        {
            bool hasError = !string.IsNullOrEmpty(_viewModel.ErrorMessage);
            _errorText?.EnableInClassList("visible", hasError);

            if (_errorText != null)
                _errorText.text = _viewModel.ErrorMessage;
        }

        private async void OnCreateClicked()
        {
            if (_viewModel == null) return;

            if (!string.IsNullOrEmpty(_createNameField?.value))
                _viewModel.CreateName = _createNameField.value;

            await _viewModel.CreateGroupAsync();
        }

        private async void OnJoinClicked()
        {
            if (_viewModel == null) return;

            if (!string.IsNullOrEmpty(_joinCodeField?.value))
                _viewModel.JoinCode = _joinCodeField.value;

            await _viewModel.JoinGroupAsync();
        }

        private void OnSkipClicked()
        {
            _viewModel?.Skip();
        }

        private void OnShowErrorRequested(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            Toast.Build(this, message, NotificationDuration.Long)
                .SetStyle(NotificationStyle.Negative)
                .SetPosition(PopupNotificationPlacement.Bottom)
                .Show();
        }

        private void OnShowSuccessRequested(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            Toast.Build(this, message, NotificationDuration.Short)
                .SetStyle(NotificationStyle.Positive)
                .SetPosition(PopupNotificationPlacement.Bottom)
                .Show();
        }
    }
}
