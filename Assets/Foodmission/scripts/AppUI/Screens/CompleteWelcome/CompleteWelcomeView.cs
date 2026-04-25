using Unity.AppUI.Core;
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
    class CompleteWelcomeView : NavigationScreenBase<CompleteWelcomeViewModel>
    {
        // UI elements references
        
        
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => true;

        public CompleteWelcomeView()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.CompleteWelcome));
            CacheUIElements();
            RegisterManualEvents();
        }

        /// <summary>
        /// Cache UI elements references
        /// </summary>
        private void CacheUIElements()
        {
            //_loginButton = contentContainer.Q<Unity.AppUI.UI.Button>("login-button");
        }

        /// <summary>
        /// Manually register events
        /// </summary>
        private void RegisterManualEvents()
        {
            // if (_loginButton != null)
            // {
            //     _loginButton.clicked += OnLoginClicked;
            // }

        }

        /// <summary>
        /// Unregister manual events
        /// </summary>
        private void UnregisterManualEvents()
        {
            // if (_loginButton != null)
            // {
            //     _loginButton.clicked -= OnLoginClicked;
            // }

            
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            
        }

        protected override void OnViewModelUnbinding()
        {
            

            UnregisterManualEvents();

            //_loginButton = null;
            
            base.OnViewModelUnbinding();
        }


        private void OnLoginClicked()
        {
            //_viewModel?.Login();
        }

        

        


        
    }
}
