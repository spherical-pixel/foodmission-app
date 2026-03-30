using System;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public class FoodmissionVisualController : INavVisualController
    {
        private Drawer _profileDrawer;
        private NavController _cachedNavController;
        private Label _userNameLabel;

        // --------------------------------------------------------------------
        // Profile Drawer
        // --------------------------------------------------------------------

        /// <summary>
        /// Creates the profile drawer and adds it to the root visual element.
        /// Must be called after the NavHost is added so the drawer renders on top.
        /// </summary>
        public void CreateProfileDrawer(VisualElement root)
        {
            _profileDrawer = new Drawer();
            _profileDrawer.anchor = DrawerAnchor.Left;
            _profileDrawer.swipeable = true;

            BuildDrawerContent();

            root.Add(_profileDrawer);

            _profileDrawer.opened += OnDrawerOpened;

        }

        private void BuildDrawerContent()
        {
            // ── Header: avatar + username + level bar ──
            var header = new VisualElement();
            header.style.alignItems = Align.Center;
            header.style.paddingTop = 48;
            header.style.paddingBottom = 24;
            header.style.paddingLeft = 24;
            header.style.paddingRight = 24;

            var avatar = new VisualElement();
            avatar.style.width = 72;
            avatar.style.height = 72;
            avatar.style.borderTopLeftRadius = 36;
            avatar.style.borderTopRightRadius = 36;
            avatar.style.borderBottomLeftRadius = 36;
            avatar.style.borderBottomRightRadius = 36;
            avatar.style.backgroundColor = new Color(0.39f, 0.58f, 0.93f);

            _userNameLabel = new Label();
            _userNameLabel.style.marginTop = 10;
            _userNameLabel.style.fontSize = 18;
            _userNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            // Level progress bar (placeholder)
            var levelRow = new VisualElement();
            levelRow.style.flexDirection = FlexDirection.Row;
            levelRow.style.alignItems = Align.Center;
            levelRow.style.marginTop = 6;
            levelRow.style.width = 140;

            var levelBar = new VisualElement();
            levelBar.style.flexGrow = 1;
            levelBar.style.height = 6;
            levelBar.style.borderTopLeftRadius = 3;
            levelBar.style.borderTopRightRadius = 3;
            levelBar.style.borderBottomLeftRadius = 3;
            levelBar.style.borderBottomRightRadius = 3;
            levelBar.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.4f);

            var levelFill = new VisualElement();
            levelFill.style.width = Length.Percent(40); // placeholder progress
            levelFill.style.height = Length.Percent(100);
            levelFill.style.borderTopLeftRadius = 3;
            levelFill.style.borderTopRightRadius = 3;
            levelFill.style.borderBottomLeftRadius = 3;
            levelFill.style.borderBottomRightRadius = 3;
            levelFill.style.backgroundColor = new Color(0.39f, 0.78f, 0.58f);
            levelBar.Add(levelFill);

            var levelLabel = new Label("1");
            levelLabel.style.marginLeft = 6;
            levelLabel.style.fontSize = 11;
            levelLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            levelRow.Add(levelBar);
            levelRow.Add(levelLabel);

            header.Add(avatar);
            header.Add(_userNameLabel);
            header.Add(levelRow);
            _profileDrawer.Add(header);

            _profileDrawer.Add(CreateDivider(16));

            // ── Menu items ──
            var menuContainer = new VisualElement();
            menuContainer.style.paddingLeft = 8;
            menuContainer.style.paddingRight = 8;
            menuContainer.style.paddingTop = 4;
            _profileDrawer.Add(menuContainer);

            // No-op items (functionality to be added later)
            AddDrawerButton(menuContainer, "Edit Profile", null);
            AddDrawerButton(menuContainer, "Edit Avatar", null);

            AddDrawerButton(menuContainer, "Manage Groups", () =>
            {
                _profileDrawer.Close();
                _cachedNavController?.Navigate(Actions.go_to_groups);
            });

            AddDrawerButton(menuContainer, "View Badges", null);

            menuContainer.Add(CreateDivider(8));

            AddDrawerButton(menuContainer, "Settings", () =>
            {
                _profileDrawer.Close();
                _cachedNavController?.Navigate(Actions.go_to_settings);
            });

            AddDrawerButton(menuContainer, "Log out", () =>
            {
                _profileDrawer.Close();
                var storeService = App.current?.services?.GetService<IStoreService>();
                storeService?.store.Dispatch(AppActions.logout.Invoke());
                _cachedNavController?.Navigate(Actions.go_to_auth);
            });
        }

        private void AddDrawerButton(VisualElement container, string label, Action onClick)
        {
            var btn = new Unity.AppUI.UI.Button();
            btn.title = label;
            btn.quiet = true;
            btn.size = Size.L;
            btn.style.width = Length.Percent(100);
            if (onClick != null)
            {
                btn.clicked += onClick;
            }
            container.Add(btn);
        }

        private static VisualElement CreateDivider(float horizontalMargin)
        {
            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            divider.style.marginLeft = horizontalMargin;
            divider.style.marginRight = horizontalMargin;
            divider.style.marginTop = 8;
            divider.style.marginBottom = 8;
            return divider;
        }

        private void OnDrawerOpened(Drawer drawer)
        {
            var storeService = App.current?.services?.GetService<IStoreService>();
            if (_userNameLabel != null && storeService != null)
            {
                _userNameLabel.text = storeService.GetAppState().userName;
            }
        }

        // --------------------------------------------------------------------
        // INavVisualController
        // --------------------------------------------------------------------

        public void SetupBottomNavBar(BottomNavBar bottomNavBar, NavDestination destination, NavController navController)
        {
            _cachedNavController = navController;
            bottomNavBar.Clear();

            var activeTab = BottomNavBarHelper.GetActiveTab(destination.name);

            AddNavItem(bottomNavBar, "fm-home",          "@UI:NAV_HOME",          activeTab == NavTab.Home,          navController, Actions.go_to_home);
            AddNavItem(bottomNavBar, "fm-menu",          "@UI:NAV_MENU",          activeTab == NavTab.Menu,          navController, Actions.go_to_menu);
            AddNavItem(bottomNavBar, "fm-notifications", "@UI:NAV_NOTIFICATIONS", activeTab == NavTab.Notifications, navController, Actions.go_to_notifications);
            AddNavItem(bottomNavBar, "fm-meal-log",      "@UI:NAV_MEAL_LOG",      activeTab == NavTab.MealLog,       navController, Actions.go_to_meallog);

            // Profile tab toggles the drawer — does not navigate
            var profileItem = new BottomNavBarItem("fm-profile", "@UI:NAV_PROFILE", () => _profileDrawer?.Toggle());
            profileItem.isSelected = false;
            profileItem.AddToClassList("no-tint");
            bottomNavBar.Add(profileItem);
        }

        public void SetupAppBar(AppBar appBar, NavDestination destination, NavController navController)
        {
            if (appBar == null || destination == null || navController == null)
            {
                Debug.LogWarning($"[{GetType().Name}] SetupAppBar - null parameters: appBar={appBar != null}, destination={destination != null}, navController={navController != null}");
                return;
            }

            appBar.title = destination.label;
            appBar.stretch = true;

            var themeService = App.current?.services.GetService<IThemeService>();
            if (themeService != null)
            {
                var bar = appBar.Q<VisualElement>(className: "appui-appbar__bar");
                if (bar != null)
                {
                    var safeAreaTop = themeService.safeAreaTop;
                    bar.style.paddingTop = safeAreaTop;

                    if (safeAreaTop > 0)
                    {
                        bar.style.paddingBottom = safeAreaTop * 0.30f;
                    }
                }
            }
        }

        public void SetupDrawer(Drawer drawer, NavDestination destination, NavController navController)
        {
            // Not used — profile drawer is managed independently via CreateProfileDrawer
        }

        public void SetupNavigationRail(NavigationRail navigationRail, NavDestination destination, NavController navController)
        {
            // Empty by now
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------

        static void AddNavItem(
            BottomNavBar bottomNavBar,
            string icon,
            string label,
            bool isSelected,
            NavController navController,
            string action,
            bool isNoTint = false)
        {
            var item = new BottomNavBarItem(icon, label, () => navController.Navigate(action));
            item.isSelected = isSelected;
            item.EnableInClassList("active", isSelected);

            if (isNoTint)
            {
                item.AddToClassList("no-tint");
            }

            bottomNavBar.Add(item);
        }
    }
}
