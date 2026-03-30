using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;


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
        
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => true;

        public LoginScreen()
        {
            InitializeComponent(FoodmissionAppBuilder.instance.LoginTemplate);
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
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            _viewModel.ShowErrorRequest += OnShowErrorRequested;
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.ShowErrorRequest -= OnShowErrorRequested;
            }

            UnregisterManualEvents();

            _loginButton = null;
            _registerButton = null;
            _forgotButton = null;

            base.OnViewModelUnbinding();
        }


        private void OnLoginClicked()
        {
            _viewModel?.Login();
        }

        private void OnRegisterClicked()
        {
            _navController.Navigate(Actions.login_to_register);
        }

        private void OnForgotClicked()
        {
            // TODO: We'll need to change this once the endpoint is working
            Application.OpenURL("https://test.auth.foodmission.eu/realms/foodmission/login-actions/reset-credentials");
            //_navController.Navigate(Actions.login_to_forgotpassword);
        }


        void OnShowErrorRequested(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            
            AlertDialog dialog = new AlertDialog
            {
                title = "@UI:ALERT_ERROR_TITLE",
                description = message,
                variant = AlertSemantic.Error
            };

            dialog.size = Size.L;
            dialog.scaleOverride = "large";
            dialog.SetPrimaryAction(0, "@UI:TXT_OK", () => Debug.LogError("Confirmed Alert"));
            //dialog.SetCancelAction(1, "Cancel");

            var modal = Modal
                .Build(this, dialog);
            modal.dismissed += (modalElement, dismissType) =>
            {
                Debug.LogError("Dismissed Alert");
                
            };
            modal.Show();
        }
    }
}
