using System;
using System.Collections.Generic;
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
        private VisualElement _menuBackdrop;
        private VisualElement _menuPanel;
        private bool _menuOpen;
        private VisualElement _notificationsBackdrop;
        private VisualElement _notificationsPanel;
        private bool _notificationsOpen;
        private readonly List<NotificationModel> _notifications = new List<NotificationModel>();
        private VisualElement _notificationsListContainer;
        private Unity.AppUI.UI.Button _markAllReadBtn;
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
        // Menu Bottom Sheet (custom overlay)
        // --------------------------------------------------------------------

        /// <summary>
        /// Creates the menu bottom sheet overlay and adds it to the root visual element.
        /// Must be called after the NavHost is added so it renders on top.
        /// </summary>
        public void CreateMenuDrawer(VisualElement root)
        {
            _menuBackdrop = new VisualElement();
            _menuBackdrop.AddToClassList("fm-menu-backdrop");
            _menuBackdrop.style.display = DisplayStyle.None;
            _menuBackdrop.pickingMode = PickingMode.Ignore;
            _menuBackdrop.RegisterCallback<ClickEvent>(e =>
            {
                if (e.target == _menuBackdrop)
                {
                    CloseMenuDrawer();
                }
            });

            var handle = new VisualElement();
            handle.AddToClassList("fm-menu-panel__handle");

            _menuPanel = new VisualElement();
            _menuPanel.AddToClassList("fm-menu-panel");
            _menuPanel.Add(handle);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            _menuPanel.Add(scroll);

            BuildMenuContent(scroll.contentContainer);

            _menuBackdrop.Add(_menuPanel);
            root.Add(_menuBackdrop);
        }

        private void ToggleMenuDrawer()
        {
            if (_menuOpen)
            {
                CloseMenuDrawer();
            }
            else
            {
                OpenMenuDrawer();
            }
        }

        private void OpenMenuDrawer()
        {
            _menuOpen = true;
            _menuBackdrop.style.display = DisplayStyle.Flex;
            _menuBackdrop.pickingMode = PickingMode.Position;
            _menuPanel.schedule.Execute(() => _menuPanel.AddToClassList("fm-menu-panel--visible")).StartingIn(16);
        }

        private void CloseMenuDrawer()
        {
            _menuOpen = false;
            _menuPanel.RemoveFromClassList("fm-menu-panel--visible");
            _menuPanel.schedule.Execute(() =>
            {
                _menuBackdrop.style.display = DisplayStyle.None;
                _menuBackdrop.pickingMode = PickingMode.Ignore;
            }).StartingIn(300);
        }

        private void BuildMenuContent(VisualElement container)
        {
            // Phase 2 — disabled
            AddMenuItem(container, "scene",            "Daily challenge",  null);
            AddMenuItem(container, "scene",            "Missions",         null);

            // Phase 1 Sprint 3 — active
            AddMenuItem(container, "list",             "Shopping list",    () =>
            {
                CloseMenuDrawer();
                _cachedNavController?.Navigate(Actions.go_to_shopping_list);
            });
            AddMenuItem(container, "scene",            "Pantry",           () =>
            {
                CloseMenuDrawer();
                _cachedNavController?.Navigate(Actions.go_to_pantry);
            });

            // Phase 3 — disabled
            AddMenuItem(container, "magnifying-glass", "Recipe book",      null);
            AddMenuItem(container, "warning",          "Food waste",       null);
            AddMenuItem(container, "scene",            "Games",            null);
            AddMenuItem(container, "info",             "Knowledge",        null);
            AddMenuItem(container, "users",            "Global community", null);
            AddMenuItem(container, "scene",            "Map",              null);
        }

        private void AddMenuItem(VisualElement container, string icon, string label, Action onClick)
        {
            bool isEnabled = onClick != null;

            var btn = new Unity.AppUI.UI.Button();
            btn.title = label;
            btn.leadingIcon = icon;
            btn.trailingIcon = "caret-right";
            btn.quiet = true;
            btn.size = Size.L;
            btn.style.width = Length.Percent(100);
            btn.SetEnabled(isEnabled);

            if (isEnabled)
            {
                btn.clicked += onClick;
            }

            container.Add(btn);
        }

        // --------------------------------------------------------------------
        // Notifications Bottom Sheet
        // --------------------------------------------------------------------

        /// <summary>
        /// Creates the notifications bottom sheet overlay.
        /// Must be called after the NavHost is added so it renders on top.
        /// </summary>
        public void CreateNotificationsPanel(VisualElement root)
        {
            _notifications.AddRange(CreateMockNotifications());

            _notificationsBackdrop = new VisualElement();
            _notificationsBackdrop.AddToClassList("fm-notifications-backdrop");
            _notificationsBackdrop.style.display = DisplayStyle.None;
            _notificationsBackdrop.pickingMode = PickingMode.Ignore;
            _notificationsBackdrop.RegisterCallback<ClickEvent>(e =>
            {
                if (e.target == _notificationsBackdrop)
                {
                    CloseNotificationsPanel();
                }
            });

            // Handle bar
            var handle = new VisualElement();
            handle.AddToClassList("fm-notifications-panel__handle");

            // Header row
            var header = new VisualElement();
            header.AddToClassList("fm-notifications-panel__header");

            var title = new Label("Notifications");
            title.AddToClassList("fm-notifications-panel__title");

            _markAllReadBtn = new Unity.AppUI.UI.Button();
            _markAllReadBtn.title = "Mark all as read";
            _markAllReadBtn.quiet = true;
            _markAllReadBtn.size = Unity.AppUI.UI.Size.S;
            _markAllReadBtn.clicked += OnMarkAllReadClicked;

            header.Add(title);
            header.Add(_markAllReadBtn);

            var divider = new VisualElement();
            divider.AddToClassList("fm-notifications-panel__divider");

            // Scrollable list
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.maxHeight = new Length(65, LengthUnit.Percent);

            _notificationsListContainer = scroll.contentContainer;

            _notificationsPanel = new VisualElement();
            _notificationsPanel.AddToClassList("fm-notifications-panel");
            _notificationsPanel.Add(handle);
            _notificationsPanel.Add(header);
            _notificationsPanel.Add(divider);
            _notificationsPanel.Add(scroll);

            _notificationsBackdrop.Add(_notificationsPanel);
            root.Add(_notificationsBackdrop);

            RefreshNotificationsList();
            UpdateMarkAllReadButton();
        }

        private void ToggleNotificationsPanel()
        {
            if (_notificationsOpen)
            {
                CloseNotificationsPanel();
            }
            else
            {
                OpenNotificationsPanel();
            }
        }

        private void OpenNotificationsPanel()
        {
            _notificationsOpen = true;
            _notificationsBackdrop.style.display = DisplayStyle.Flex;
            _notificationsBackdrop.pickingMode = PickingMode.Position;
            _notificationsPanel.schedule
                .Execute(() => _notificationsPanel.AddToClassList("fm-notifications-panel--visible"))
                .StartingIn(16);
        }

        private void CloseNotificationsPanel()
        {
            _notificationsOpen = false;
            _notificationsPanel.RemoveFromClassList("fm-notifications-panel--visible");
            _notificationsPanel.schedule.Execute(() =>
            {
                _notificationsBackdrop.style.display = DisplayStyle.None;
                _notificationsBackdrop.pickingMode = PickingMode.Ignore;
            }).StartingIn(300);
        }

        private void RefreshNotificationsList()
        {
            if (_notificationsListContainer == null)
            {
                return;
            }

            _notificationsListContainer.Clear();

            if (_notifications.Count == 0)
            {
                var empty = new Label("No notifications");
                empty.AddToClassList("fm-notifications-empty");
                _notificationsListContainer.Add(empty);
                return;
            }

#if UNITY_EDITOR
            var template = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Foodmission/AppUI/Template/NotificationCard.uxml");
#else
            VisualTreeAsset template = null;
#endif

            if (template == null)
            {
                Debug.LogWarning("[FoodmissionVisualController] NotificationCard template not found — assign it in FoodmissionAppBuilder (Task 8)");
                return;
            }

            foreach (var model in _notifications)
            {
                var card = new NotificationCard(template);
                card.Bind(model);
                card.OnDelete += OnNotificationDeleted;
                _notificationsListContainer.Add(card);
            }
        }

        private void OnNotificationDeleted(string id)
        {
            _notifications.RemoveAll(n => n.Id == id);
            RefreshNotificationsList();
            UpdateMarkAllReadButton();
        }

        private void OnMarkAllReadClicked()
        {
            foreach (var n in _notifications)
            {
                n.IsRead = true;
            }

            RefreshNotificationsList();
            UpdateMarkAllReadButton();
        }

        private void UpdateMarkAllReadButton()
        {
            if (_markAllReadBtn == null)
            {
                return;
            }

            bool anyUnread = _notifications.Exists(n => !n.IsRead);
            _markAllReadBtn.SetEnabled(anyUnread);
        }

        private static List<NotificationModel> CreateMockNotifications()
        {
            return new List<NotificationModel>
            {
                new NotificationModel
                {
                    Id = "n1",
                    Text = "The user Laura has been successfully added to your group.",
                    Timestamp = "1 h",
                    Type = NotificationType.Social,
                    IsRead = false
                },
                new NotificationModel
                {
                    Id = "n2",
                    Text = "You are very close to earning the Balanced Starter badge. Keep it up!",
                    Timestamp = "2 h",
                    Type = NotificationType.Badge,
                    IsRead = false
                },
                new NotificationModel
                {
                    Id = "n3",
                    Text = "Welcome to Foodmission! Complete your profile to get started.",
                    Timestamp = "Yesterday",
                    Type = NotificationType.System,
                    IsRead = true
                }
            };
        }

        // --------------------------------------------------------------------
        // INavVisualController
        // --------------------------------------------------------------------

        public void SetupBottomNavBar(BottomNavBar bottomNavBar, NavDestination destination, NavController navController)
        {
            _cachedNavController = navController;
            bottomNavBar.Clear();

            var activeTab = BottomNavBarHelper.GetActiveTab(destination.name);

            AddNavItem(bottomNavBar, "fm-home",     "@UI:NAV_HOME",     activeTab == NavTab.Home,    navController, Actions.go_to_home);
            AddNavItem(bottomNavBar, "fm-meal-log", "@UI:NAV_MEAL_LOG", activeTab == NavTab.MealLog, navController, Actions.go_to_meallog);

            // Notifications tab toggles the bottom sheet — does not navigate
            var notificationsItem = new BottomNavBarItem("fm-notifications", "@UI:NAV_NOTIFICATIONS", () => ToggleNotificationsPanel());
            notificationsItem.isSelected = activeTab == NavTab.Notifications;
            bottomNavBar.Insert(1, notificationsItem);

            // Menu tab toggles the bottom sheet — does not navigate
            var menuItem = new BottomNavBarItem("fm-menu", "@UI:NAV_MENU", () => ToggleMenuDrawer());
            menuItem.isSelected = activeTab == NavTab.Menu;
            bottomNavBar.Insert(1, menuItem);

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
