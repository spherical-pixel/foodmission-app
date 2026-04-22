using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public class FoodmissionAppBuilder : UIToolkitAppBuilder<FoodmissionApp>
    {
        internal static FoodmissionAppBuilder instance { get; private set; }

        public VisualTreeAsset SplashTemplate;
        public VisualTreeAsset HomeTemplate;
        public VisualTreeAsset LoginTemplate;
        public VisualTreeAsset RegisterTemplate;
        public VisualTreeAsset ForgotPasswordTemplate;
        public VisualTreeAsset ProfileTemplate;
        public VisualTreeAsset SettingsTemplate;
        public VisualTreeAsset OnboardingProfileTemplate;

        public VisualTreeAsset NotificationCardTemplate;
        public VisualTreeAsset CompleteWelcomeTemplate;


        public NavGraphViewAsset GraphAsset;

        protected override void OnConfiguringApp(AppBuilder builder)
        {
            base.OnConfiguringApp(builder);
            Debug.Log($"[{GetType().Name}] OnConfiguringApp");

            instance = this;

            // Services - important: check order according to dependencies
            builder.services.AddSingleton<ILocalStorageService, LocalStorageService>();
            builder.services.AddSingleton<IStoreService, StoreService>();
            builder.services.AddSingleton<IAuthService, AuthService>();
            builder.services.AddSingleton<IThemeService, ThemeService>();
            builder.services.AddSingleton<IKeyboardService, KeyboardService>();
            builder.services.AddSingleton<INutriService, NutriService>();
            builder.services.AddSingleton<ICatalogService, CatalogService>();

            // ViewModels (Transient - new instance each time)
            builder.services.AddTransient<SplashScreenViewModel>();
            builder.services.AddTransient<HomeScreenViewModel>();
            builder.services.AddTransient<LoginViewModel>();
            builder.services.AddTransient<RegisterViewModel>();
            builder.services.AddTransient<ForgotPasswordViewModel>();
            builder.services.AddTransient<MealLogViewModel>();
            builder.services.AddTransient<MealLogAddViewModel>();
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
        }

        protected override void OnAppInitialized(FoodmissionApp app)
        {
            Debug.Log($"[{GetType().Name}] OnAppInitialized");
            base.OnAppInitialized(app);

            // Initialize keyboard service and panel adjuster
            InitializeKeyboardSystem();
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
    }
}
