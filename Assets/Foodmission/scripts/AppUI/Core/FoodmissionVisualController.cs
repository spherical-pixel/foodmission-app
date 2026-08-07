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
        private VisualElement _drawerAvatarElement;
        private ActionButton _lastAppBarDrawerButton;
        private bool _avatarSubscribed;

        private void SubscribeAvatarEvents()
        {
            if (_avatarSubscribed) return;
            var avatarService = App.current?.services?.GetService<IAvatarService>();
            if (avatarService != null)
            {
                avatarService.OnFaceTextureChanged += UpdateAvatarVisuals;
                _avatarSubscribed = true;
            }
        }

        public void UpdateAvatarVisuals()
        {
            var avatarService = App.current?.services?.GetService<IAvatarService>();
            if (avatarService == null) return;

            Texture2D tex = avatarService.GetFaceTexture(allowFallback: true);

            // If user has an avatar but face texture file hasn't been generated yet, trigger asynchronous render
            if (avatarService.HasAvatar && (tex == null || tex == AvatarService.GetDefaultAvatarTexture()))
            {
                _ = avatarService.EnsureFaceTextureAsync();
            }

            if (_drawerAvatarElement != null)
            {
                if (tex != null)
                {
                    _drawerAvatarElement.style.backgroundImage = Background.FromTexture2D(tex);
                }
                else
                {
                    _drawerAvatarElement.style.backgroundImage = StyleKeyword.Null;
                }
            }

            if (_lastAppBarDrawerButton != null)
            {
                _lastAppBarDrawerButton.icon = null;
                var avatarImg = _lastAppBarDrawerButton.Q("appbar-avatar-image");
                if (avatarImg == null)
                {
                    avatarImg = new VisualElement();
                    avatarImg.name = "appbar-avatar-image";
                    avatarImg.AddToClassList("fm-profile-avatar");
                    avatarImg.AddToClassList("fm-profile-avatar--appbar");
                    avatarImg.pickingMode = PickingMode.Ignore;
                    _lastAppBarDrawerButton.hierarchy.Add(avatarImg);
                }

                if (tex != null)
                {
                    avatarImg.style.display = DisplayStyle.Flex;
                    avatarImg.style.backgroundImage = Background.FromTexture2D(tex);
                }
                else
                {
                    avatarImg.style.display = DisplayStyle.None;
                }
            }
        }

        // --------------------------------------------------------------------
        // Profile Drawer
        // --------------------------------------------------------------------

        private void BuildDrawerContent(Drawer drawer)
        {
            SubscribeAvatarEvents();
            IThemeService themeService = App.current?.services.GetService<IThemeService>();
            if (themeService != null)
            {
                var safeAreaTop = themeService.safeAreaTop;
                if (safeAreaTop > 0)
                {

                    VisualElement filler = new VisualElement();
                    filler.name = "safe-area-filler";
                    filler.style.height = safeAreaTop;
                    filler.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
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
            _drawerAvatarElement = avatar;
            UpdateAvatarVisuals();

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
            AddDrawerButton(menuContainer, "✏️ " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "EDIT_PROFILE"), () =>
            {
                _profileDrawer.Close();
                _cachedNavController?.Navigate(Actions.go_to_editprofile);
            });
            AddDrawerButton(menuContainer, "🧑‍💻 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "EDIT_AVATAR"), () =>
            {
                _profileDrawer.Close();
                _cachedNavController?.Navigate(Actions.go_to_avatar_editor);
            });

            AddDrawerButton(menuContainer, "👥 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MANAGE_GROUPS"), () =>
            {
                _profileDrawer.Close();
                _cachedNavController?.Navigate(Actions.go_to_groups);
            });

            AddDrawerButton(menuContainer, "🏅 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "VIEW_BADGES"), null);


            AddDrawerButton(menuContainer, "⚙️ " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SETTINGS"), () =>
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

            AddDrawerButton(dangerContainer, "🗑️ " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DELETE_ACCOUNT"), () =>
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
                    "@UI:TXT_CANCEL", onKo: () => { }
                );

            });

            AddDrawerButton(menuContainer, "🚪 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LOG_OUT"), () =>
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
            drawer.RemoveFromClassList("fm-drawer-pre-init");
            var storeService = App.current?.services?.GetService<IStoreService>();
            if (_userNameLabel != null && storeService != null)
            {
                _userNameLabel.text = storeService.GetAppState().userName;
            }
            UpdateAvatarVisuals();
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
            AddMenuItem(container, "💡 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "KNOWLEDGE"), null);
            AddMenuItem(container, "🎯 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MISSIONS"), null);
            AddMenuItem(container, "🏆 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DAILY_CHALLENGE"), null);


            // AddMenuItem(container, "📝 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SHOPPING_LIST"), () =>
            // {
            //     CloseMenuDrawer();
            //     _cachedNavController?.Navigate(Actions.go_to_shopping_list, new[] { new Argument("fromMenu", "true") });
            // });
            // AddMenuItem(container, "🧺 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "PANTRY"), () =>
            // {
            //     CloseMenuDrawer();
            //     _cachedNavController?.Navigate(Actions.go_to_pantry);
            // });

            // Phase 3 — disabled
            AddMenuItem(container, "🍳 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RECIPE_BOOK"), () =>
            {
                CloseMenuDrawer();
                _cachedNavController?.Navigate(Actions.go_to_recipes);
            });
            // AddMenuItem(container, "🗑️ " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FOOD_WASTE"), () =>
            // {
            //     CloseMenuDrawer();
            //     _cachedNavController?.Navigate(Actions.go_to_foodwaste);
            // });
            AddMenuItem(container, "🌐 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GLOBAL_COMMUNITY"), null);
            AddMenuItem(container, "🗺️ " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MAP"), null);
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

            _notificationsTitleLabel = new Label();
            _notificationsTitleLabel.AddToClassList("fm-notifications-panel__title");

            _markAllReadBtn = new Unity.AppUI.UI.Button();
            _markAllReadBtn.title = string.Empty;
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

            AddNavItem(bottomNavBar, "fm-home", "@UI:NAV_HOME", activeTab == NavTab.Home, navController, Actions.go_to_home);
            AddNavItem(bottomNavBar, "fm-meal-log", "@UI:NAV_MEAL_LOG", activeTab == NavTab.MealLog, navController, Actions.go_to_meallog);
            AddNavItem(bottomNavBar, "fm-search", "@UI:QUICK_SEARCH", activeTab == NavTab.Search, navController, Actions.go_to_quicksearch);
            AddNavItem(bottomNavBar, "fm-shopping-list", "@UI:SHOPPING_LIST", activeTab == NavTab.ShoppingList, navController, Actions.go_to_shopping_list, false, new[] { new Argument("fromMenu", "true") });
            AddNavItem(bottomNavBar, "fm-pantry", "@UI:PANTRY", activeTab == NavTab.Pantry, navController, Actions.go_to_pantry);
            // AddMenuItem(container, "📝 " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SHOPPING_LIST"), () =>
            // {
            //     CloseMenuDrawer();
            //     _cachedNavController?.Navigate(Actions.go_to_shopping_list, new[] { new Argument("fromMenu", "true") });
            // });

            // Notifications tab toggles the bottom sheet — does not navigate
            // var notificationsItem = new BottomNavBarItem("fm-notifications", "@UI:NAV_NOTIFICATIONS", () => ToggleNotificationsPanel());
            // notificationsItem.isSelected = activeTab == NavTab.Notifications;
            // bottomNavBar.Insert(1, notificationsItem);

            // Menu tab toggles the bottom sheet — does not navigate
            // var menuItem = new BottomNavBarItem("fm-menu", "@UI:NAV_MENU", () => ToggleMenuDrawer());
            // menuItem.isSelected = activeTab == NavTab.Menu;
            // bottomNavBar.Insert(1, menuItem);

            // Profile tab toggles the drawer — does not navigate
            // var profileItem = new BottomNavBarItem("fm-avatar", "@UI:NAV_PROFILE", () => _profileDrawer?.Toggle());
            // profileItem.isSelected = false;
            // profileItem.AddToClassList("no-tint");
            // bottomNavBar.Add(profileItem);
        }

        public void SetupAppBar(AppBar appBar, NavDestination destination, NavController navController)
        {
            if (appBar == null || destination == null || navController == null)
            {
                Debug.LogWarning($"[{GetType().Name}] SetupAppBar - null parameters: appBar={appBar != null}, destination={destination != null}, navController={navController != null}");
                return;
            }

            appBar.compact = false;
            appBar.title = destination.label;
            appBar.stretch = false;

            // Customize drawer button icon (on the left) to fm-avatar and ensure vertical centering and standard 44x44 touch target
            var drawerButton = appBar.Q<ActionButton>(className: "appui-appbar__drawer-button");
            if (drawerButton != null)
            {
                _lastAppBarDrawerButton = drawerButton;
                drawerButton.style.marginTop = 0;
                drawerButton.style.marginBottom = 0;
                drawerButton.style.alignSelf = Align.Center;
                SubscribeAvatarEvents();
                UpdateAvatarVisuals();
            }

            // Ensure back button (on nested screens) also has standard 44x44 touch target
            var backButton = appBar.Q<ActionButton>(className: "appui-appbar__back-button");
            if (backButton != null)
            {
                backButton.style.marginTop = 0;
                backButton.style.marginBottom = 0;
                backButton.style.alignSelf = Align.Center;
            }

            // Ensure the action container is centered vertically
            var actionContainer = appBar.Q(className: "appui-appbar__action-container");
            if (actionContainer != null)
            {
                actionContainer.style.marginTop = 0;
                actionContainer.style.marginBottom = 0;
                actionContainer.style.alignSelf = Align.Center;
            }

            // Clean up old menu button to prevent duplicates
            var existingMenuBtn = appBar.Q("appbar-menu-button");
            existingMenuBtn?.RemoveFromHierarchy();
            // Also clean up the old home-menu-button name if it was left from previous sessions
            var existingHomeMenuBtn = appBar.Q("home-menu-button");
            existingHomeMenuBtn?.RemoveFromHierarchy();

            // Determine if the menu button should be shown.
            // By default, show it on all main screens (where the bottom navigation bar is visible).
            bool showMenu = BottomNavBarHelper.IsNavBarVisible(destination.name);

            // Allow overriding via custom argument defined in the navigation graph destination
            if (destination.arguments != null)
            {
                var arg = destination.arguments.Find(a => a.name == "showMenuButton");
                if (arg != null && bool.TryParse(arg.value, out bool overrideShow))
                {
                    showMenu = overrideShow;
                }
            }

            if (showMenu)
            {
                // Use ActionButton to align perfectly with the AppBar design and standard styling with 44x44 touch target
                var menuButton = new ActionButton
                {
                    name = "appbar-menu-button",
                    icon = "fm-menu",
                    quiet = true
                };
                menuButton.style.marginTop = 0;
                menuButton.style.marginBottom = 0;
                menuButton.style.alignSelf = Align.Center;
                menuButton.clicked += () => ToggleMenuDrawer();

                if (actionContainer != null)
                {
                    actionContainer.Add(menuButton);
                }
                else
                {
                    appBar.Add(menuButton);
                }
            }

            var themeService = App.current?.services.GetService<IThemeService>();
            if (themeService != null)
            {
                var safeAreaTop = themeService.safeAreaTop;
                if (safeAreaTop > 0)
                {
                    // Check if it already exists to prevent duplication on re-entry/re-load
                    var existingFiller = appBar.Q("appbar-safe-area-filler");
                    if (existingFiller == null)
                    {
                        VisualElement filler = new VisualElement();
                        filler.name = "appbar-safe-area-filler";
                        filler.style.height = safeAreaTop;
                        filler.AddToClassList("appui-appbar__bar"); // same background as __bar to seamlessly fill the safe area

                        appBar.hierarchy.Insert(0, filler);
                    }
                    else
                    {
                        existingFiller.style.height = safeAreaTop;
                    }
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





            // var bar = appBar.Q(className: "appui-appbar__bar");
            // var border = appBar.Q(className: "appui-appbar__bottom-border");
            // var safeAreaFiller = appBar.Q("appbar-safe-area-filler");

            // if (makeTransparent)
            // {
            //     appBar.style.backgroundColor = Color.clear;
            //     if (bar != null) bar.style.backgroundColor = Color.clear;
            //     if (safeAreaFiller != null) safeAreaFiller.style.backgroundColor = Color.clear;
            //     if (border != null) border.style.display = DisplayStyle.None;
            //     appBar.elevation = 0;
            // }
            // else
            // {
            //     appBar.style.backgroundColor = StyleKeyword.Null;
            //     if (bar != null) bar.style.backgroundColor = StyleKeyword.Null;
            //     if (safeAreaFiller != null) safeAreaFiller.style.backgroundColor = StyleKeyword.Null;
            //     if (border != null) border.style.display = StyleKeyword.Null;
            // }
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
            _profileDrawer.AddToClassList("fm-drawer-pre-init");
            //_profileDrawer.swipeAreaWidth = 0;
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
            bool isNoTint = false,
            params Argument[] args)
        {
            var item = new BottomNavBarItem(icon, label, () => navController.Navigate(action, args));
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
