
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using eu.foodmission.platform.Components;

using Unity.Properties;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;


namespace eu.foodmission.platform
{
    /// <summary>
    /// Forgot Password Screen
    /// Allows user to request a password reset by entering their email
    /// </summary>
    [Preserve]
    class ForgotPasswordScreen : NavigationScreenBase<ForgotPasswordViewModel>
    {
        // UI elements references
        private Unity.AppUI.UI.Button _continueButton;
        private Unity.AppUI.UI.Button _backButton;
        
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => true;

        public ForgotPasswordScreen()
        {
            InitializeComponent(FoodmissionAppBuilder.instance.ForgotPasswordTemplate);
            CacheUIElements();
            RegisterManualEvents();
        }

        /// <summary>
        /// Cache UI elements references
        /// </summary>
        private void CacheUIElements()
        {
            _continueButton = contentContainer.Q<Unity.AppUI.UI.Button>("btContinue");
            _backButton = contentContainer.Q<Unity.AppUI.UI.Button>("btBack");
        }

        /// <summary>
        /// Manually register events
        /// </summary>
        private void RegisterManualEvents()
        {
            if (_continueButton != null)
            {
                _continueButton.clicked += OnContinueClicked;
            }

            if (_backButton != null)
            {
                _backButton.clicked += OnBackClicked;
            }
        }

        /// <summary>
        /// Unregister manual events
        /// </summary>
        private void UnregisterManualEvents()
        {
            if (_continueButton != null)
            {
                _continueButton.clicked -= OnContinueClicked;
            }

            if (_backButton != null)
            {
                _backButton.clicked -= OnBackClicked;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            UnregisterManualEvents();

            _continueButton = null;
            _backButton = null;
            

            if (_viewModel != null)
            {
                _viewModel.ShowErrorRequest -= OnShowErrorRequested;
                _viewModel.ShowSuccessRequest -= OnShowSuccessRequested;
            }

            base.OnViewModelUnbinding();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_viewModel != null)
            {
                _viewModel.ShowErrorRequest += OnShowErrorRequested;
                _viewModel.ShowSuccessRequest += OnShowSuccessRequested;
            }
        }

        private void OnContinueClicked()
        {
            _viewModel?.RequestPasswordReset();
        }

        private void OnBackClicked()
        {
            _navController.PopBackStack();//.Navigate(Actions.forgotpassword_to_login);
        }

void OnShowErrorRequested(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            FMDialog.ShowAlert(this, "@UI:ALERT_ERROR_TITLE", message, AlertSemantic.Error);
        }

void OnShowSuccessRequested(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            FMDialog.ShowAlert(
                this,
                "@UI:ALERT_SUCCESS_TITLE",
                message,
                AlertSemantic.Confirmation,
                onOk: () => _navController.PopBackStack()
            );
        }
    }
}
