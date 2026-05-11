using System;
using System.Collections.Generic;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public class FoodmissionVisualController : INavVisualController
    {
        private Drawer _profileDrawer;
        private VisualElement _menuBackdrop;
        private VisualElement _menuPanel;
        private VisualElement _menuContentContainer;
        private bool _menuOpen;
        private VisualElement _notificationsBackdrop;
        private VisualElement _notificationsPanel;
        private bool _notificationsOpen;
        private readonly List<NotificationModel> _notifications = new List<NotificationModel>();
        private VisualElement _notificationsListContainer;
        private Unity.AppUI.UI.Button _markAllReadBtn;
        private Label _notificationsTitleLabel;
        private VisualTreeAsset _notificationCardTemplate;
        private NavController _cachedNavController;
        private TextElement _userNameLabel;

        // --------------------------------------------------------------------
        // Profile Drawer
        // --------------------------------------------------------------------

        private void BuildDrawerContent(Drawer drawer)
        {
            IThemeService themeService = App.current?.services.GetService<IThemeService>();
            if (themeService != null)
            {
                var safeAreaTop = themeService.safeAreaTop;
                if (safeAreaTop > 0)
                {

                    VisualElement filler = new VisualElement();
                    filler.name = "safe-area-filler";
                    filler.style.height = safeAreaTop;
                    filler.style.backgroundColor = new Color(0f, 0f, 0f,0f);
                    drawer.Add(filler);
                }
            }

            VisualElement drawerRoot = new VisualElement();
            drawerRoot.style.flexDirection = FlexDirection.Row;
            drawerRoot.style.flexGrow = 1;
            drawer.Add(drawerRoot);

            Spacer spacer = new Spacer();
            spacer.spacing = SpacerSpacing.XL;
            drawerRoot.Add(spacer);


            VisualElement content = new VisualElement();
            content.style.flexGrow = 1;
            content.style.paddingRight = 10;
            drawerRoot.Add(content);


            // ── Header: [avatar] | [name / xp bar + badge] ──
            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.paddingTop = 48;
            header.style.paddingBottom = 24;
            header.style.paddingLeft = 24;
            header.style.paddingRight = 24;

            var avatar = new VisualElement();
            avatar.AddToClassList("fm-profile-avatar");

            // Right column
            var rightColumn = new VisualElement();
            rightColumn.style.flexDirection = FlexDirection.Column;
            rightColumn.style.justifyContent = Justify.Center;
            rightColumn.style.flexGrow = 1;

            var nameHeading = new Heading();
            nameHeading.AddToClassList("fm-profile-username");
            nameHeading.size = HeadingSize.XL;
            nameHeading.style.marginBottom = 8;
            _userNameLabel = nameHeading;

            // XP row: bar + star badge
            var xpRow = new VisualElement();
            xpRow.style.flexDirection = FlexDirection.Row;
            xpRow.style.alignItems = Align.Center;

            var xpBar = new LinearProgress();
            xpBar.value = 0.3f;
            xpBar.AddToClassList("fm-xp-progress");
            xpBar.AddToClassList("appui-progress--rounded-corners");
            xpBar.style.flexGrow = 1;
            //xpBar.style.marginRight = -20;
            xpBar.variant = Progress.Variant.Determinate;

            var xpBadge = new VisualElement();
            xpBadge.AddToClassList("fm-profile-xp-badge");

            var xpLabel = new Label("20");
            xpLabel.AddToClassList("fm-profile-xp-label");
            xpBadge.Add(xpLabel);

            xpRow.Add(xpBar);
            xpRow.Add(xpBadge);

            rightColumn.Add(nameHeading);
            rightColumn.Add(xpRow);

            header.Add(avatar);
            header.Add(rightColumn);

            content.Add(header);

            content.Add(CreateDivider(16));

            // ── Menu items ──
            var menuContainer = new VisualElement();
            menuContainer.style.paddingLeft = 8;
            menuContainer.style.paddingRight = 4;
            menuContainer.style.paddingTop = 4;
            content.Add(menuContainer);

            // No-op items (functionality to be added later)
            AddDrawerButton(menuContainer, "✏️ " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","EDIT_PROFILE"), ()=>
            {
                _profileDrawer.Close();
                _cachedNavController?.Navigate(Actions.go_to_editprofile);
            });
            AddDrawerButton(menuContainer, "🧑‍💻 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","EDIT_AVATAR"), () =>
            {
                _profileDrawer.Close();
                _cachedNavController?.Navigate(Actions.go_to_avatar_editor);
            });

            AddDrawerButton(menuContainer, "👥 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","MANAGE_GROUPS"), () =>
            {
                _profileDrawer.Close();
                _cachedNavController?.Navigate(Actions.go_to_groups);
            });

            AddDrawerButton(menuContainer, "🏅 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","VIEW_BADGES"), null);

            
            AddDrawerButton(menuContainer, "⚙️ " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","SETTINGS"), () =>
            {
                _profileDrawer.Close();
                _cachedNavController?.Navigate(Actions.go_to_settings);
            });

            content.Add(CreateDivider(16));
            var dangerContainer = new VisualElement();
            dangerContainer.style.paddingLeft = 8;
            dangerContainer.style.paddingRight = 4;
            dangerContainer.style.paddingTop = 4;
            content.Add(dangerContainer);

            AddDrawerButton(dangerContainer, "🗑️ " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","DELETE_ACCOUNT"), () =>
            {
                FMDialog.ShowAlert(
                    App.current?.rootVisualElement,
                    "@UI:DELETE_ACCOUNT_TITLE",
                    "@UI:DELETE_ACCOUNT_MESSAGE",
                    AlertSemantic.Destructive,
                    "@UI:TXT_ACCEPT", onOk: async () =>
                    {
                        var authService = App.current?.services?.GetService<IAuthService>();
                        if (authService == null)
                        {
                            return;
                        }

                        _profileDrawer.Close();
                        var (success, error) = await authService.DeleteAccountAsync();
                        if (success)
                        {
                            var storeService = App.current?.services?.GetService<IStoreService>();
                            storeService?.store.Dispatch(AppActions.logout.Invoke());
                            _cachedNavController?.Navigate(Actions.go_to_auth);
                        }
                        else
                        {
                            Debug.LogError($"[FoodmissionVisualController] Delete account failed: {error}");
                        }
                    },
                    "@UI:TXT_CANCEL", onKo: () => {}
                );
                
            });

            AddDrawerButton(menuContainer, "🚪 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","LOG_OUT"), () =>
            {
                _profileDrawer.Close();
                var storeService = App.current?.services?.GetService<IStoreService>();
                storeService?.store.Dispatch(AppActions.logout.Invoke());
                _cachedNavController?.Navigate(Actions.go_to_auth);
            });
        }

        public void RefreshLocalizedContent()
        {
            if (_menuContentContainer != null)
            {
                _menuContentContainer.Clear();
                BuildMenuContent(_menuContentContainer);
            }

            if (_profileDrawer != null)
            {
                _profileDrawer.Clear();
                BuildDrawerContent(_profileDrawer);
            }

            if (_notificationsTitleLabel != null)
            {
                _notificationsTitleLabel.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NAV_NOTIFICATIONS");
            }

            if (_markAllReadBtn != null)
            {
                _markAllReadBtn.title = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MARK_ALL_AS_READ");
            }

            RefreshNotificationsList();
        }

        private void AddDrawerButton(VisualElement container, string label, Action onClick)
        {
            var btn = new Unity.AppUI.UI.Button();
            btn.title = label;
            btn.quiet = true;
            btn.size = Size.L;
            btn.AddToClassList("fm-button-align-left");
            btn.AddToClassList("fm-button-drawer");
            btn.style.width = Length.Percent(100);
            btn.trailingIcon = "fm-arrow-right";
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

            _menuContentContainer = scroll.contentContainer;
            BuildMenuContent(_menuContentContainer);

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
            AddMenuItem(container, "🏆 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","DAILY_CHALLENGE"),  null);
            AddMenuItem(container, "🎯 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","MISSIONS"),         null);

            // Phase 1 Sprint 3 — active
            AddMenuItem(container, "📝 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","SHOPPING_LIST"),    () =>
            {
                CloseMenuDrawer();
                _cachedNavController?.Navigate(Actions.go_to_shopping_list);
            });
            AddMenuItem(container, "🧺 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","PANTRY"),           () =>
            {
                CloseMenuDrawer();
                _cachedNavController?.Navigate(Actions.go_to_pantry);
            });

            // Phase 3 — disabled
            AddMenuItem(container, "🍳 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","RECIPE_BOOK"),      null);
            AddMenuItem(container, "🗑️ " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","FOOD_WASTE"),       () =>
            {
                CloseMenuDrawer();
                _cachedNavController?.Navigate(Actions.go_to_foodwaste);
            });
            AddMenuItem(container, "💡 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","KNOWLEDGE"),        null);
            AddMenuItem(container, "🌐 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","GLOBAL_COMMUNITY"), null);
            AddMenuItem(container, "🗺️ " + LocalizationSettings.StringDatabase.GetLocalizedString("UI","MAP"),              null);
        }

        private void AddMenuItem(VisualElement container, string label, Action onClick)
        {
            
            var btn = new Unity.AppUI.UI.Button();
            btn.AddToClassList("fm-button-align-left");
            btn.AddToClassList("fm-button-drawer");
            btn.title = label;
            btn.trailingIcon = "fm-arrow-right";
            btn.quiet = true;
            btn.size = Size.L;
            btn.style.width = Length.Percent(100);
            
            if (onClick != null)
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
        public void CreateNotificationsPanel(VisualElement root, VisualTreeAsset notificationCardTemplate)
        {
            _notificationCardTemplate = notificationCardTemplate;
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

            _notificationsTitleLabel = new Label(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NAV_NOTIFICATIONS"));
            _notificationsTitleLabel.AddToClassList("fm-notifications-panel__title");

            _markAllReadBtn = new Unity.AppUI.UI.Button();
            _markAllReadBtn.title = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MARK_ALL_AS_READ");
            _markAllReadBtn.quiet = true;
            _markAllReadBtn.size = Unity.AppUI.UI.Size.S;
            _markAllReadBtn.clicked += OnMarkAllReadClicked;

            header.Add(_notificationsTitleLabel);
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
                var empty = new Label(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NO_NOTIFICATIONS"));
                empty.AddToClassList("fm-notifications-empty");
                _notificationsListContainer.Add(empty);
                return;
            }

            var template = _notificationCardTemplate;

            if (template == null)
            {
                Debug.LogWarning("[FoodmissionVisualController] NotificationCardTemplate is null — assign it in FoodmissionAppBuilder Inspector");
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
            var profileItem = new BottomNavBarItem("fm-avatar", "@UI:NAV_PROFILE", () => _profileDrawer?.Toggle());
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
            appBar.stretch = false;
            

            
            var themeService = App.current?.services.GetService<IThemeService>();
            if (themeService != null)
            {
                var safeAreaTop = themeService.safeAreaTop;
                if (safeAreaTop > 0)
                {

                     VisualElement filler = new VisualElement();
                    filler.name = "appbar-safe-area-filler";
                    filler.style.height = safeAreaTop;
                    filler.AddToClassList("appui-appbar__bar"); // same background as __bar to seamlessly fill the safe area

                    appBar.hierarchy.Insert(0, filler); 
                }
            }

            var screen = appBar.parent;
            if (screen != null)
            {
                appBar.RegisterCallback<GeometryChangedEvent>(OnAppBarGeometryChanged);

                void OnAppBarGeometryChanged(GeometryChangedEvent e)
                {
                    appBar.UnregisterCallback<GeometryChangedEvent>(OnAppBarGeometryChanged);

                    // Defer to next frame so paddingTop/border are fully applied before measuring.
                    appBar.schedule.Execute(() =>
                    {
                        float contentMargin = appBar.worldBound.yMax - screen.worldBound.y;
                        if (contentMargin <= 0) return;

                        // Apply margin to the direct sibling of AppBar inside screen.
                        // The AppBar is position:absolute so it doesn't push content down automatically.
                        foreach (var child in screen.Children())
                        {
                            if (child == appBar) continue;
                            child.style.marginTop = contentMargin;
                            break;
                        }
                    });
                }
            }
        }

        public void SetupDrawer(Drawer drawer, NavDestination destination, NavController navController)
        {
            if (drawer == null || destination == null || navController == null)
            {
                Debug.LogWarning($"[{GetType().Name}] SetupDrawer - null parameters: drawer={drawer != null}, destination={destination != null}, navController={navController != null}");
                return;
            }

            if (_profileDrawer == drawer)
            {
                return;
            }

            if (_profileDrawer != null)
            {
                _profileDrawer.opened -= OnDrawerOpened;
            }

            _profileDrawer = drawer;
            _profileDrawer.Clear();
            _profileDrawer.swipeAreaWidth = 0;
            BuildDrawerContent(_profileDrawer);
            _profileDrawer.opened += OnDrawerOpened;
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
