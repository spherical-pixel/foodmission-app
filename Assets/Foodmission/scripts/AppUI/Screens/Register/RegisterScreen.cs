using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Basic register screen
    /// </summary>
    [Preserve]
    class RegisterScreen : NavigationScreenBase<RegisterViewModel>
    {
        private Unity.AppUI.UI.Button _registerButton;
        private Unity.AppUI.UI.Text _loginText;
        private Dropdown _ageDropdown;

        protected override bool IsFixedContent => false;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;

        public RegisterScreen()
        {
            InitializeComponent(FoodmissionAppBuilder.instance.RegisterTemplate);
            CacheUIElements();
            RegisterManualEvents();
        }

        /// <summary>
        /// Cache UI elements for later use.
        /// </summary>
        private void CacheUIElements()
        {
            _registerButton = contentContainer.Q<Unity.AppUI.UI.Button>("register-button");
            _loginText = contentContainer.Q<Unity.AppUI.UI.Text>("goto-login-button");
            _ageDropdown = contentContainer.Q<Dropdown>("age-dropdown");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            // Configure age dropdown options
            if (_ageDropdown != null && _viewModel != null)
            {
                // Set the source items (options)
                _ageDropdown.sourceItems = _viewModel.AgeOptions;

                // Configure how to display each item
                _ageDropdown.bindItem = (item, index) =>
                {
                    item.label = _viewModel.AgeOptions[index];
                    item.icon = null; 
                };

                // Set the selected value by finding its index
                var selectedIndex = _viewModel.AgeOptions.IndexOf(_viewModel.SelectedAge);
                if (selectedIndex >= 0)
                {
                    _ageDropdown.SetValueWithoutNotify(new[] { selectedIndex });
                }
            }
        }

        /// <summary>
        /// Manually register events
        /// </summary>
        private void RegisterManualEvents()
        {
            if (_registerButton != null)
                _registerButton.clicked += OnRegisterClicked;

            if (_loginText != null)
                _loginText.RegisterCallback<ClickEvent>(OnLoginClicked);
        }

        /// <summary>
        /// Un register manual events
        /// </summary>
        private void UnregisterManualEvents()
        {
            if (_registerButton != null)
                _registerButton.clicked -= OnRegisterClicked;

            if (_loginText != null)
                _loginText.UnregisterCallback<ClickEvent>(OnLoginClicked);
        }

        protected override void OnViewModelUnbinding()
        {
            UnregisterManualEvents();

            _registerButton = null;
            _loginText = null;
            _ageDropdown = null;

            base.OnViewModelUnbinding();
        }

        private void OnRegisterClicked()
        {
            _viewModel?.Register();
        }

        private void OnLoginClicked(ClickEvent evt)
        {
            Debug.Log($"[{GetType().Name}] Navigate back to login");
            _navController.Navigate(Destinations.login);
        }

        
    }
}
