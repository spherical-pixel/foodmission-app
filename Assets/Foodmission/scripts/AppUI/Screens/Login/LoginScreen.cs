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
        private Unity.AppUI.UI.Text _registerText;
        private Unity.AppUI.UI.Text _forgotText;
        
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
            _registerText = contentContainer.Q<Unity.AppUI.UI.Text>("register");
            _forgotText = contentContainer.Q<Unity.AppUI.UI.Text>("forgot-password");
            
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

            if (_registerText != null)
            {
                _registerText.RegisterCallback<ClickEvent>(OnRegisterClicked);
            }

            if (_forgotText != null)
            {
                _forgotText.RegisterCallback<ClickEvent>(OnForgotClicked);
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

            if (_registerText != null)
            {
                _registerText.UnregisterCallback<ClickEvent>(OnRegisterClicked);
            }

            if (_forgotText != null)
            {
                _forgotText.UnregisterCallback<ClickEvent>(OnForgotClicked);
            }
        }

        protected override void OnViewModelUnbinding()
        {
            UnregisterManualEvents();

            _loginButton = null;
            _registerText = null;
            _forgotText = null;

            _viewModel.ShowErrorRequest -=OnShowErrorRequested;

            base.OnViewModelUnbinding();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            _viewModel.ShowErrorRequest +=OnShowErrorRequested;
        }


        private void OnLoginClicked()
        {
            _viewModel?.Login();
        }

        private void OnRegisterClicked(ClickEvent evt)
        {
            _navController.Navigate(Actions.login_to_register);
        }

        private void OnForgotClicked(ClickEvent evt)
        {
            // TODO: Implement navigation to forgot password screen
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

            dialog.SetPrimaryAction(0, "Confirm", () => Debug.LogError("Confirmed Alert"));
            //dialog.SetCancelAction(1, "Cancel");

            var modal = Modal
                .Build(_loginButton, dialog);
            modal.dismissed += (modalElement, dismissType) =>
            {
                Debug.LogError("Dismissed Alert");
                
            };
            modal.Show();
        }
    }
}
