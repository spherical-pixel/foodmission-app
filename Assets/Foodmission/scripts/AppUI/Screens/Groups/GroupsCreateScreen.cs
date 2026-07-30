using System;
using System.ComponentModel;
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
    class GroupsCreateScreen : NavigationScreenBase<GroupsCreateViewModel>
    {
        private Unity.AppUI.UI.TextField _nameField;
        private Unity.AppUI.UI.TextField _descField;
        private Unity.AppUI.UI.Button _btnCreate;

        private AccessibilityNode _createButtonNode;
        private AccessibilityNode _nameFieldNode;
        private AccessibilityNode _descFieldNode;

        public GroupsCreateScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.GroupsCreate));
            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _nameField = contentContainer.Q<Unity.AppUI.UI.TextField>("name-field");
            _descField = contentContainer.Q<Unity.AppUI.UI.TextField>("desc-field");
            _btnCreate = contentContainer.Q<Unity.AppUI.UI.Button>("btn-create");
        }

        private void RegisterManualEvents()
        {
            if (_btnCreate != null)
                _btnCreate.clicked += OnCreateClicked;

            if (_nameField != null)
                _nameField.RegisterValueChangedCallback(OnNameChanged);
        }

        private void UnregisterManualEvents()
        {
            if (_btnCreate != null)
                _btnCreate.clicked -= OnCreateClicked;

            if (_nameField != null)
                _nameField.UnregisterValueChangedCallback(OnNameChanged);
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnPropertyChanged;
                _viewModel.NavigationRequested += OnNavigationRequested;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnPropertyChanged;
                _viewModel.NavigationRequested -= OnNavigationRequested;
            }

            UnregisterManualEvents();
            base.OnViewModelUnbinding();
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

            if (_nameField != null)
            {
                _nameFieldNode = h.AddNode("Group name");
                _nameFieldNode.role = AccessibilityRole.TextField;
                _nameFieldNode.frameGetter = MakeElementFrameGetter(_nameField);
            }

            if (_descField != null)
            {
                _descFieldNode = h.AddNode("Group description");
                _descFieldNode.role = AccessibilityRole.TextField;
                _descFieldNode.frameGetter = MakeElementFrameGetter(_descField);
            }
        }

        protected override void TeardownAccessibilityNodes()
        {
            _createButtonNode = null;
            _nameFieldNode = null;
            _descFieldNode = null;
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

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            if (_viewModel != null)
                _viewModel.Name = evt.newValue;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.IsCreating):
                    UpdateCreatingState();
                    break;
                case nameof(_viewModel.ErrorMessage):
                    UpdateErrorState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
            }
        }

        private void UpdateCreatingState()
        {
            bool isCreating = _viewModel.IsCreating;
            if (isCreating)
                FMLoadingOverlay.Show();
            else
                FMLoadingOverlay.Hide();

            if (_btnCreate != null) _btnCreate.SetEnabled(!isCreating);
            if (_nameField != null) _nameField.SetEnabled(!isCreating);
            if (_descField != null) _descField.SetEnabled(!isCreating);
        }

        private void UpdateErrorState()
        {
            if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
            {
                Toast.Build(this, _viewModel.ErrorMessage, NotificationDuration.Long)
                    .SetStyle(NotificationStyle.Negative)
                    .SetPosition(PopupNotificationPlacement.Bottom)
                    .Show();
            }
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private async void OnCreateClicked()
        {
            if (_viewModel == null) return;

            if (_nameField != null)
                _viewModel.Name = _nameField.value;

            if (_descField != null)
                _viewModel.Description = _descField.value;

            await _viewModel.CreateGroupAsync();
        }
    }
}
