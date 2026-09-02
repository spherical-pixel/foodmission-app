using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public class FoodmissionAppBuilder : UIToolkitAppBuilder<FoodmissionApp>
    {
        internal static FoodmissionAppBuilder instance { get; private set; }

        public VisualTreeAsset SplashTemplate;
        public VisualTreeAsset NotificationCardTemplate;


        public NavGraphViewAsset GraphAsset;
        public AudioMixer audioMixer;
        public AudioCatalogSO audioCatalog;

        protected override void OnConfiguringApp(AppBuilder builder)
        {
            base.OnConfiguringApp(builder);
            Debug.Log($"[{GetType().Name}] OnConfiguringApp");

            instance = this;

            // Services - important: check order according to dependencies
            builder.services.AddSingleton<ILocalStorageService, LocalStorageService>();
            builder.services.AddSingleton<IStoreService, StoreService>();
            builder.services.AddSingleton<IAudioService, AudioService>();
            builder.services.AddSingleton<IAuthService, AuthService>();
            builder.services.AddSingleton<IThemeService, ThemeService>();
            builder.services.AddSingleton<IKeyboardService, KeyboardService>();
            builder.services.AddSingleton<INutriService, NutriService>();
            builder.services.AddSingleton<IAvatarService, AvatarService>();
            builder.services.AddSingleton<ICatalogService, CatalogService>();
            builder.services.AddSingleton<IFoodProductService, FoodProductService>();
            builder.services.AddSingleton<IOpenFoodFactsClientService, OpenFoodFactsClientService>();
            builder.services.AddSingleton<IShoppingListService, ShoppingListService>();
            builder.services.AddSingleton<IGroupService, GroupService>();
            builder.services.AddSingleton<IGenericFoodService, GenericFoodService>();
            builder.services.AddSingleton<IPantryService, PantryService>();
            builder.services.AddSingleton<IMealService, MealService>();
            builder.services.AddSingleton<IMealLogService, MealLogService>();
            builder.services.AddSingleton<IMealItemService, MealItemService>();
            builder.services.AddSingleton<IRecipeService, RecipeService>();
            builder.services.AddSingleton<IFoodWasteService, FoodWasteService>();
            builder.services.AddSingleton<ITemplateService, TemplateService>();
            builder.services.AddSingleton<IImageService, ImageService>();
            builder.services.AddSingleton<IAppUpdateService, AppUpdateService>();
            builder.services.AddSingleton<IWhatsNewService, WhatsNewService>();
            builder.services.AddSingleton<IRemoteLocalizationService, RemoteLocalizationService>();
            builder.services.AddSingleton<IAccessibilityService, AccessibilityService>();
            builder.services.AddSingleton<IEventService, EventService>();
            builder.services.AddSingleton<IQuizService, QuizService>();
            builder.services.AddSingleton<IFoodFactService, FoodFactService>();
            builder.services.AddSingleton<IQuestService, QuestService>();
            builder.services.AddSingleton<IDimensionService, DimensionService>();
            builder.services.AddSingleton<ILegalService, LegalService>();
            builder.services.AddSingleton<ISurveyService, SurveyService>();
            builder.services.AddSingleton<IPilotSurveyService, PilotSurveyService>();
            builder.services.AddSingleton<INotificationService, NotificationService>();
            builder.services.AddSingleton<NotificationRoutingService>();

            // ViewModels (Transient - new instance each time)
            builder.services.AddTransient<SplashScreenViewModel>();
            builder.services.AddTransient<HomeScreenViewModel>();
            builder.services.AddTransient<LoginViewModel>();
            builder.services.AddTransient<RegisterViewModel>();
            builder.services.AddTransient<ForgotPasswordViewModel>();
            builder.services.AddTransient<MealLogViewModel>();
            builder.services.AddTransient<FoodWasteViewModel>();
            builder.services.AddTransient<FoodWasteAddViewModel>();
            builder.services.AddTransient<ProfileViewModel>();
            builder.services.AddTransient<SettingsViewModel>();
            builder.services.AddTransient<GroupsViewModel>();
            builder.services.AddTransient<GroupsCreateViewModel>();
            builder.services.AddTransient<GroupsJoinViewModel>();
            builder.services.AddTransient<GroupDetailViewModel>();
            builder.services.AddTransient<ShoppingListViewModel>();
            builder.services.AddTransient<ShoppingListDetailViewModel>();
            builder.services.AddTransient<PantryViewModel>();
            builder.services.AddTransient<PantryItemDetailViewModel>();
            builder.services.AddTransient<OnboardingProfileViewModel>();
            builder.services.AddTransient<OnboardingAvatarViewModel>();
            builder.services.AddTransient<OnboardingGroupsViewModel>();
            builder.services.AddTransient<EditProfileViewModel>();
            builder.services.AddTransient<AvatarEditorViewModel>();
            builder.services.AddTransient<ForceUpdateScreenViewModel>();
            builder.services.AddTransient<FoodInfoViewModel>();
            builder.services.AddTransient<QuickSearchViewModel>();
            builder.services.AddTransient<TestSurveyViewModel>();
            builder.services.AddTransient<OnboardingSurveyViewModel>();
            builder.services.AddTransient<PilotSurveyViewModel>();
            builder.services.AddTransient<RecipeBookViewModel>();
            builder.services.AddTransient<RecipeDetailViewModel>();
            builder.services.AddTransient<RecipeEditorViewModel>();
            builder.services.AddTransient<QuizScreenViewModel>();
            builder.services.AddTransient<QuizzesViewModel>();
            builder.services.AddTransient<FoodFactsViewModel>();
            builder.services.AddTransient<FoodFactScreenViewModel>();
        }

        protected override void OnAppInitialized(FoodmissionApp app)
        {
            Debug.Log($"[{GetType().Name}] OnAppInitialized");
            base.OnAppInitialized(app);

            // Initialize keyboard service and panel adjuster
            InitializeKeyboardSystem();

            // Initialize notification system
            InitializeNotificationSystem();

            // Subscribe to session expiration
            var authService = App.current?.services?.GetService<IAuthService>();
            if (authService != null)
            {
                authService.OnSessionExpired += HandleSessionExpired;
            }
        }

        private void OnDestroy()
        {
            var authService = App.current?.services?.GetService<IAuthService>();
            if (authService != null)
            {
                authService.OnSessionExpired -= HandleSessionExpired;
            }
        }

        private void HandleSessionExpired()
        {
            var navHost = GetComponentInChildren<NavHost>();
            if (navHost != null && navHost.navController != null)
            {
                Debug.LogWarning($"[{GetType().Name}] Session expired — navigating to login screen");
                navHost.navController.Navigate(Actions.go_to_auth);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                CheckSessionOnResume();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                var eventService = App.current?.services?.GetService<IEventService>();
                if (eventService != null)
                {
                    _ = eventService.TrackSessionEndAsync();
                }
            }
            else
            {
                CheckSessionOnResume();
            }
        }

        private async void CheckSessionOnResume()
        {
            var authService = App.current?.services?.GetService<IAuthService>();
            if (authService != null)
            {
                var storeService = App.current?.services?.GetService<IStoreService>();
                if (storeService != null && !string.IsNullOrEmpty(storeService.GetAppState().accessToken))
                {
                    Debug.Log($"[{GetType().Name}] App resumed — verifying auth session");
                    bool valid = await authService.CheckSessionAsync();
                    if (!valid)
                    {
                        Debug.LogWarning($"[{GetType().Name}] Session invalid on app resume — navigating to login");
                        authService.Logout();
                        HandleSessionExpired();
                    }
                    else
                    {
                        var eventService = App.current?.services?.GetService<IEventService>();
                        if (eventService != null)
                        {
                            _ = eventService.TrackSessionStartAsync();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the keyboard service and panel adjuster.
        /// </summary>
        private void InitializeKeyboardSystem()
        {
            // Get the keyboard service from DI
            var keyboardService = App.current?.services?.GetService<IKeyboardService>() as KeyboardService;
            if (keyboardService == null)
            {
                Debug.LogWarning($"[{GetType().Name}] KeyboardService not found in DI container");
                return;
            }

            // Initialize the service
            keyboardService.Initialize();

            // Create or get the updater
            var updater = gameObject.GetComponent<KeyboardServiceUpdater>();
            if (updater == null)
            {
                updater = gameObject.AddComponent<KeyboardServiceUpdater>();
            }
            updater.Initialize(keyboardService);

            // Get the root visual element from the app and create the panel adjuster
            var app = FoodmissionApp.current;
            if (app != null && app.rootVisualElement != null)
            {
                var rootElement = app.rootVisualElement;
                var panelAdjuster = gameObject.GetComponent<KeyboardPanelAdjuster>();
                if (panelAdjuster == null)
                {
                    panelAdjuster = gameObject.AddComponent<KeyboardPanelAdjuster>();
                }
                panelAdjuster.Initialize(rootElement, keyboardService);
                Debug.Log($"[{GetType().Name}] KeyboardPanelAdjuster initialized");
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] Could not access root visual element for keyboard adjuster");
            }

            Debug.Log($"[{GetType().Name}] KeyboardService initialized with updater");
        }

        /// <summary>
        /// Initializes the Avatar service (optional - called explicitly when needed)
        /// </summary>
        internal void InitializeAvatarSystem()
        {
            // Get the avatar service from DI
            var avatarService = App.current?.services?.GetService<IAvatarService>();
            if (avatarService == null)
            {
                Debug.LogWarning($"[{GetType().Name}] AvatarService not found in DI container");
                return;
            }

            // Initialize the service (asynchronously)
            avatarService.InitializeAsync();

            Debug.Log($"[{GetType().Name}] AvatarService initialized");
        }

        /// <summary>
        /// Initializes the Notification service asynchronously.
        /// </summary>
        private async void InitializeNotificationSystem()
        {
            var notificationService = App.current?.services?.GetService<INotificationService>();
            if (notificationService == null)
            {
                Debug.LogWarning($"[{GetType().Name}] NotificationService not found in DI container");
                return;
            }

            await notificationService.InitializeAsync();
            Debug.Log($"[{GetType().Name}] NotificationService initialized");
        }
    }
}
