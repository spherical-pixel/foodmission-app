using System;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class ForceUpdateScreen : NavigationScreenBase<ForceUpdateScreenViewModel>
    {
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => true;
        protected override bool IsFixedContent => true;

        private Unity.AppUI.UI.Button _updateButton;
        private Unity.AppUI.UI.Button _skipButton;

        private AccessibilityNode _updateButtonNode;
        private AccessibilityNode _skipButtonNode;

        public ForceUpdateScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.ForceUpdate));
            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _updateButton = contentContainer.Q<Unity.AppUI.UI.Button>("update-button");
            _skipButton = contentContainer.Q<Unity.AppUI.UI.Button>("skip-button");
        }

        private void RegisterManualEvents()
        {
            if (_updateButton != null)
                _updateButton.clicked += OnUpdateClicked;
            if (_skipButton != null)
                _skipButton.clicked += OnSkipClicked;
        }

        protected override void OnViewModelUnbinding()
        {
            if (_updateButton != null)
                _updateButton.clicked -= OnUpdateClicked;
            if (_skipButton != null)
                _skipButton.clicked -= OnSkipClicked;
            _updateButton = null;
            _skipButton = null;
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

            _updateButtonNode = CreateButtonNode(h, _updateButton, "Update app");
            _skipButtonNode = CreateButtonNode(h, _skipButton, "Skip update");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _updateButtonNode = null;
            _skipButtonNode = null;
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

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            if (args != null)
            {
                foreach (Argument arg in args)
                {
                    if (arg.name == "updateData")
                    {
                        string json = arg.value?.ToString();
                        if (!string.IsNullOrEmpty(json))
                        {
                            var data = JsonUtility.FromJson<AppVersionCheckResult>(json);
                            _viewModel?.LoadData(data);
                        }
                    }
                    else if (arg.name == "returnAction")
                    {
                        _viewModel?.SetReturnAction(arg.value?.ToString());
                    }
                }
            }

            if (_skipButton != null)
            {
                _skipButton.style.display = _viewModel != null && !_viewModel.IsForced
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private void OnUpdateClicked()
        {
            _viewModel?.OpenStore();
        }

        private void OnSkipClicked()
        {
            _viewModel?.Skip();
        }
    }
}
