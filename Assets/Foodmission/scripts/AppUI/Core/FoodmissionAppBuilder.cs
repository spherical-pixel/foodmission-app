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
        public VisualTreeAsset MenuTemplate;
        public VisualTreeAsset LoginTemplate;
        public VisualTreeAsset RegisterTemplate;
        public VisualTreeAsset ForgotPasswordTemplate;

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

            // ViewModels (Transient - new instance each time)
            builder.services.AddTransient<SplashScreenViewModel>();
            builder.services.AddTransient<HomeScreenViewModel>();
            builder.services.AddTransient<MenuScreenViewModel>();
            builder.services.AddTransient<LoginViewModel>();
            builder.services.AddTransient<RegisterViewModel>();
            builder.services.AddTransient<ForgotPasswordViewModel>();
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
