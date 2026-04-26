using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class ProfileScreen : NavigationScreenBase<ProfileViewModel>
    {
        private VisualElement _menuItemProfile;
        private VisualElement _menuItemAvatar;
        private VisualElement _menuItemGroups;
        private VisualElement _menuItemBadges;
        private VisualElement _menuItemSettings;
        private VisualElement _menuItemLogout;

        public ProfileScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.Profile));
            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _menuItemProfile = contentContainer.Q<VisualElement>("menu-item-profile");
            _menuItemAvatar = contentContainer.Q<VisualElement>("menu-item-avatar");
            _menuItemGroups = contentContainer.Q<VisualElement>("menu-item-groups");
            _menuItemBadges = contentContainer.Q<VisualElement>("menu-item-badges");
            _menuItemSettings = contentContainer.Q<VisualElement>("menu-item-settings");
            _menuItemLogout = contentContainer.Q<VisualElement>("menu-item-logout");
        }

        private void RegisterManualEvents()
        {
            if (_menuItemProfile != null)
            {
                _menuItemProfile.RegisterCallback<PointerDownEvent>(OnProfileClicked);
            }

            if (_menuItemAvatar != null)
            {
                _menuItemAvatar.RegisterCallback<PointerDownEvent>(OnAvatarClicked);
            }

            if (_menuItemGroups != null)
            {
                _menuItemGroups.RegisterCallback<PointerDownEvent>(OnGroupsClicked);
            }

            if (_menuItemBadges != null)
            {
                _menuItemBadges.RegisterCallback<PointerDownEvent>(OnBadgesClicked);
            }

            if (_menuItemSettings != null)
            {
                _menuItemSettings.RegisterCallback<PointerDownEvent>(OnSettingsClicked);
            }

            if (_menuItemLogout != null)
            {
                _menuItemLogout.RegisterCallback<PointerDownEvent>(OnLogoutClicked);
            }
        }

        private void UnregisterManualEvents()
        {
            if (_menuItemProfile != null)
            {
                _menuItemProfile.UnregisterCallback<PointerDownEvent>(OnProfileClicked);
            }

            if (_menuItemAvatar != null)
            {
                _menuItemAvatar.UnregisterCallback<PointerDownEvent>(OnAvatarClicked);
            }

            if (_menuItemGroups != null)
            {
                _menuItemGroups.UnregisterCallback<PointerDownEvent>(OnGroupsClicked);
            }

            if (_menuItemBadges != null)
            {
                _menuItemBadges.UnregisterCallback<PointerDownEvent>(OnBadgesClicked);
            }

            if (_menuItemSettings != null)
            {
                _menuItemSettings.UnregisterCallback<PointerDownEvent>(OnSettingsClicked);
            }

            if (_menuItemLogout != null)
            {
                _menuItemLogout.UnregisterCallback<PointerDownEvent>(OnLogoutClicked);
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
        }

        protected override void OnViewModelUnbinding()
        {
            UnregisterManualEvents();
            _menuItemProfile = null;
            _menuItemAvatar = null;
            _menuItemGroups = null;
            _menuItemBadges = null;
            _menuItemSettings = null;
            _menuItemLogout = null;

            base.OnViewModelUnbinding();
        }

        private void OnProfileClicked(PointerDownEvent evt)
        {
            // Edit Profile - stub navigation, needs go_to_edit_profile action
            _navController?.Navigate(Actions.go_to_settings);
        }

        private void OnAvatarClicked(PointerDownEvent evt)
        {
            // Edit Avatar - stub navigation, needs go_to_edit_avatar action
            _navController?.Navigate(Actions.go_to_settings);
        }

        private void OnGroupsClicked(PointerDownEvent evt)
        {
            _navController?.Navigate(Actions.go_to_groups);
        }

        private void OnBadgesClicked(PointerDownEvent evt)
        {
            // View Badges - stub navigation, needs go_to_view_badges action
            _navController?.Navigate(Actions.go_to_settings);
        }

        private void OnSettingsClicked(PointerDownEvent evt)
        {
            _navController?.Navigate(Actions.go_to_settings);
        }

        private void OnLogoutClicked(PointerDownEvent evt)
        {
            _viewModel?.Logout();
        }
    }
}
