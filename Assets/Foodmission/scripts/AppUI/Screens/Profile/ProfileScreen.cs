using Unity.AppUI.Navigation.Generated;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class ProfileScreen : NavigationScreenBase<ProfileViewModel>
    {
        private Unity.AppUI.UI.Button _settingsButton;
        private Unity.AppUI.UI.Button _groupsButton;
        private Unity.AppUI.UI.Button _logoutButton;

        public ProfileScreen()
        {
            InitializeComponent(FoodmissionAppBuilder.instance.ProfileTemplate);
            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _settingsButton = contentContainer.Q<Unity.AppUI.UI.Button>("btn-settings");
            _groupsButton = contentContainer.Q<Unity.AppUI.UI.Button>("btn-groups");
            _logoutButton = contentContainer.Q<Unity.AppUI.UI.Button>("btn-logout");
        }

        private void RegisterManualEvents()
        {
            if (_settingsButton != null)
            {
                _settingsButton.clicked += OnSettingsClicked;
            }

            if (_groupsButton != null)
            {
                _groupsButton.clicked += OnGroupsClicked;
            }

            if (_logoutButton != null)
            {
                _logoutButton.clicked += OnLogoutClicked;
            }
        }

        private void UnregisterManualEvents()
        {
            if (_settingsButton != null)
            {
                _settingsButton.clicked -= OnSettingsClicked;
            }

            if (_groupsButton != null)
            {
                _groupsButton.clicked -= OnGroupsClicked;
            }

            if (_logoutButton != null)
            {
                _logoutButton.clicked -= OnLogoutClicked;
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
        }

        protected override void OnViewModelUnbinding()
        {
            UnregisterManualEvents();
            _settingsButton = null;
            _groupsButton = null;
            _logoutButton = null;

            base.OnViewModelUnbinding();
        }

        private void OnSettingsClicked()
        {
            _navController?.Navigate(Actions.go_to_settings);
        }

        private void OnGroupsClicked()
        {
            _navController?.Navigate(Actions.go_to_groups);
        }

        private void OnLogoutClicked()
        {
            _viewModel?.Logout();
        }
    }
}
