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

        
        [ObservableProperty]
        private string _loadingText = "Loading...";

        

        public SplashScreenViewModel(IStoreService storeService, IAuthService authService) : base(storeService)
        {
            _authService = authService;

        }

        

        public async Task<string> InitializeAppAsync()
        {
            LoadingText = "Loading localizations...";
            await Task.Delay(100);
            
            // Paso 1: Esperar a que Localization esté inicializado
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                await LocalizationSettings.InitializationOperation.Task;
            }
            
            LoadingText = await LocalizationSettings.StringDatabase.GetLocalizedStringAsync("UI", "LOADING_ASSETS").Task;
            await Task.Delay(500);

            // Load Nutri from Addressables
            LoadingText = await LocalizationSettings.StringDatabase.GetLocalizedStringAsync("UI", "LOADING_NUTRI").Task;
            var nutriService = App.current.services.GetService<INutriService>();
            await nutriService.InitializeAsync();
            await Task.Delay(500);

            // Check session
            LoadingText = await LocalizationSettings.StringDatabase.GetLocalizedStringAsync("UI", "CHECK_AUTH").Task;
            var isAuthenticated = await _authService.CheckSessionAsync();

            // If session check failed, explicitly logout to reset preferences
            if (!isAuthenticated)
            {
                _authService.Logout();
            }

            await Task.Delay(500);

            if (!isAuthenticated)
            {
                return Actions.loading_to_auth;
            }

            // // Authenticated — check if extended profile is needed
            // AppState state = _storeService.GetAppState();
            // if (!state.hasCompletedExtendedProfile)
            // {
            //     return Actions.register_to_onboarding;
            // }

            //return Actions.register_to_onboarding;
            return Actions.loading_to_home;
        }
    }
}
