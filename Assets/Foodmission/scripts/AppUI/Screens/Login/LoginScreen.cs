using System.ComponentModel;
using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using eu.foodmission.platform.Components;


namespace eu.foodmission.platform
{
    /// <summary>
    /// Login Screen
    /// Let's the user login with username and password, register or reset password.
    /// </summary>
    [Preserve]
    class LoginScreen : NavigationScreenBase<LoginViewModel>
    {
        // UI elements references
        private Unity.AppUI.UI.Button _loginButton;
        private Unity.AppUI.UI.Button _registerButton;
        private Unity.AppUI.UI.Button _forgotButton;
        private FormFieldItemTextField _usernameField;
        private FormFieldItemPassword _passwordField;
        private VisualElement _appearanceBar;
        private IconButton _appearanceButton;

        // Accessibility node references for cleanup
        private AccessibilityNode _loginButtonNode;
        private AccessibilityNode _registerButtonNode;
        private AccessibilityNode _forgotButtonNode;
        private AccessibilityNode _appearanceButtonNode;

        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => true;

        public LoginScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.Login));
            CacheUIElements();
            RegisterManualEvents();
        }

        /// <summary>
        /// Cache UI elements references
        /// </summary>
        private void CacheUIElements()
        {
            _loginButton = contentContainer.Q<Unity.AppUI.UI.Button>("login-button");
            _registerButton = contentContainer.Q<Unity.AppUI.UI.Button>("btRegister");
            _forgotButton = contentContainer.Q<Unity.AppUI.UI.Button>("btForgotPassword");
            _usernameField = contentContainer.Q<FormFieldItemTextField>("username");
            _passwordField = contentContainer.Q<FormFieldItemPassword>("password");
            _appearanceBar = contentContainer.Q<VisualElement>("appearance-bar");
            _appearanceButton = contentContainer.Q<IconButton>("btn-appearance");
        }

        /// <summary>
        /// Manually register events
        /// </summary>
        private void RegisterManualEvents()
        {
            if (_loginButton != null)
            {
                _loginButton.clicked += OnLoginClicked;
            }

            if (_registerButton != null)
            {
                _registerButton.clicked += OnRegisterClicked;
            }

            if (_forgotButton != null)
            {
                _forgotButton.clicked += OnForgotClicked;
            }

            if (_appearanceButton != null)
            {
                _appearanceButton.clicked += OnAppearanceClicked;
            }
        }

        /// <summary>
        /// Unregister manual events
        /// </summary>
        private void UnregisterManualEvents()
        {
            if (_loginButton != null)
            {
                _loginButton.clicked -= OnLoginClicked;
            }

            if (_registerButton != null)
            {
                _registerButton.clicked -= OnRegisterClicked;
            }

            if (_forgotButton != null)
            {
                _forgotButton.clicked -= OnForgotClicked;
            }

            if (_appearanceButton != null)
            {
                _appearanceButton.clicked -= OnAppearanceClicked;
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            // The screen ignores the safe area (full-bleed logo), but the gear must stay clear of the notch
            if (_appearanceBar != null)
            {
                _themeService?.ApplySafeAreaPadding(_appearanceBar, true, false, false, true);
            }
            if (_viewModel != null)
            {
                _viewModel.ShowErrorRequest += OnShowErrorRequested;
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                UpdateLoadingOverlay(_viewModel.IsLoading);
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.ShowErrorRequest -= OnShowErrorRequested;
            }

            HideLoadingOverlay();

            UnregisterManualEvents();

            _loginButton = null;
            _registerButton = null;
            _forgotButton = null;
            _usernameField = null;
            _passwordField = null;
            _appearanceBar = null;
            _appearanceButton = null;

            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.IsLoading))
            {
                UpdateLoadingOverlay(_viewModel.IsLoading);
            }
        }

        private void UpdateLoadingOverlay(bool isLoading)
        {
            if (isLoading)
            {
                ShowLoadingOverlay();
            }
            else
            {
                HideLoadingOverlay();
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

            _usernameField?.CreateAccessibilityNode(h, "Username");
            _passwordField?.CreateAccessibilityNode(h, "Password");

            _loginButtonNode = CreateButtonNode(h, _loginButton, "Sign in");
            _registerButtonNode = CreateButtonNode(h, _registerButton, "Register");
            _forgotButtonNode = CreateButtonNode(h, _forgotButton, "Forgot password");
            _appearanceButtonNode = CreateButtonNode(h, _appearanceButton, "Display and language settings");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _loginButtonNode = null;
            _registerButtonNode = null;
            _forgotButtonNode = null;
            _appearanceButtonNode = null;

            _usernameField?.DestroyAccessibilityNode();
            _passwordField?.DestroyAccessibilityNode();

            base.TeardownAccessibilityNodes();
        }

        private AccessibilityNode CreateButtonNode(AccessibilityHierarchy hierarchy, VisualElement button, string label)
        {
            if (button == null) return null;

            var node = hierarchy.AddNode(label);
            node.role = AccessibilityRole.Button;

            if (!button.enabledSelf)
            {
                node.state = AccessibilityState.Disabled;
            }

            node.frameGetter = () =>
            {
                if (button.panel == null) return Rect.zero;
                var rect = button.worldBound;
                var scale = button.panel.scaledPixelsPerPoint;
                return new Rect(rect.position * scale, rect.size * scale);
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

        private void OnLoginClicked()
        {
            _viewModel?.Login(this);
        }

        private void OnRegisterClicked()
        {
            _navController.Navigate(Actions.login_to_register);
        }

        private void OnAppearanceClicked()
        {
            AppearanceSettingsDialog.Show(contentContainer);
        }

        private void OnForgotClicked()
        {
            // TODO: We'll need to change this once the endpoint is working
            Application.OpenURL($"{ApiConfig.AuthBaseUrl}/realms/foodmission/login-actions/reset-credentials");
            //_navController.Navigate(Actions.login_to_forgotpassword);
        }


        void OnShowErrorRequested(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Toast.Build(this, message, NotificationDuration.Long)
                .SetStyle(NotificationStyle.Negative)
                .SetPosition(PopupNotificationPlacement.Bottom)
                .Show();
        }
    }
}
