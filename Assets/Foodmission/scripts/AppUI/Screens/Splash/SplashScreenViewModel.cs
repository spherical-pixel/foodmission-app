using System.Threading.Tasks;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;

using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class SplashScreenViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly ITemplateService _templateService;

        [ObservableProperty]
        private string _loadingText = "Loading...";

        public SplashScreenViewModel(
            IStoreService storeService,
            IAuthService authService,
            ITemplateService templateService) : base(storeService)
        {
            _authService = authService;
            _templateService = templateService;
        }

        public async Task<string> InitializeAppAsync()
        {
            LoadingText = "Loading localizations...";
            await Task.Delay(100);

            LocalizationSettings.ProjectLocale =
                LocalizationSettings.AvailableLocales.GetLocale(Application.systemLanguage)
                ?? LocalizationSettings.AvailableLocales.GetLocale("en");

            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                await LocalizationSettings.InitializationOperation.Task;
            }

            LoadingText = await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync("UI", "LOADING_ASSETS").Task;
            await Task.Delay(500);

            // Load Nutri from Addressables
            LoadingText = await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync("UI", "LOADING_NUTRI").Task;
            var nutriService = App.current.services.GetService<INutriService>();
            await nutriService.InitializeAsync();
            await Task.Delay(500);

            // Preload UI templates
            LoadingText = "Loading UI...";
            await _templateService.PreloadAllAsync();

            // Check session
            LoadingText = await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync("UI", "CHECK_AUTH").Task;
            var isAuthenticated = await _authService.CheckSessionAsync();

            if (!isAuthenticated)
            {
                _authService.Logout();
            }

            await Task.Delay(500);

            if (!isAuthenticated)
            {
                return Actions.loading_to_auth;
            }

            return Actions.loading_to_home;

            
            //return Actions.register_to_onboarding;
        }
    }
}
