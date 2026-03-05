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

            // ViewModels (Transient - new instance each time)
            builder.services.AddTransient<SplashScreenViewModel>();
            builder.services.AddTransient<HomeScreenViewModel>();
            builder.services.AddTransient<MenuScreenViewModel>();
            builder.services.AddTransient<LoginViewModel>();
            builder.services.AddTransient<RegisterViewModel>();
        }

        protected override void OnAppInitialized(FoodmissionApp app)
        {
            Debug.Log($"[{GetType().Name}] OnAppInitialized");
            base.OnAppInitialized(app);

            
        }
    }
}
