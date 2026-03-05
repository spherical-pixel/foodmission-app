using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;
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

            
            // Check session
            LoadingText = await LocalizationSettings.StringDatabase.GetLocalizedStringAsync("UI", "CHECK_AUTH").Task;
            var isAuthenticated = await _authService.CheckSessionAsync();

            
            await Task.Delay(500);
            

            return isAuthenticated ? Actions.loading_to_home : Actions.loading_to_auth;
        }
    }
}
