using System.Threading.Tasks;

using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;

using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class SplashScreenViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly ITemplateService _templateService;
        private readonly IAppUpdateService _appUpdateService;
        private readonly IRemoteLocalizationService _remoteLocalizationService;
        private readonly ICatalogService _catalogService;

        [ObservableProperty]
        private string _loadingText = "Loading...";

        public AppVersionCheckResult PendingUpdate { get; set; }
        public string ReturnActionOnSkip { get; set; }

        public SplashScreenViewModel(
            IStoreService storeService,
            IAuthService authService,
            ITemplateService templateService,
            IAppUpdateService appUpdateService,
            IRemoteLocalizationService remoteLocalizationService,
            ICatalogService catalogService) : base(storeService)
        {
            _authService = authService;
            _templateService = templateService;
            _appUpdateService = appUpdateService;
            _remoteLocalizationService = remoteLocalizationService;
            _catalogService = catalogService;
        }

        public async Task<string> InitializeAppAsync()
        {
            AndroidSystemBar.ShowAndSetTransparent();

            LoadingText = "Loading localizations";
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                await LocalizationSettings.InitializationOperation.Task;
            }

            await _remoteLocalizationService.InitializeAsync();

            string lang = _storeService.GetAppState().lang ?? "en";
            await FMQuantityUnitPanel.InitializeAsync(_catalogService, lang);

            await Task.Delay(100);

            LoadingText = await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync("UI", "LOADING_ASSETS").Task;
            await Task.Delay(500);

            LoadingText = await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync("UI", "LOADING_NUTRI").Task;
            var nutriService = App.current.services.GetService<INutriService>();
            await nutriService.InitializeAsync();
            await Task.Delay(500);

            LoadingText = await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync("UI", "LOADING_AVATAR").Task;
            var avatarService = App.current.services.GetService<IAvatarService>();
            await avatarService.InitializeAsync();
            await Task.Delay(500);

            LoadingText = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LOADING_UI");
            await _templateService.PreloadAllAsync();

            // Check session first to know return action
            LoadingText = await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync("UI", "CHECK_AUTH").Task;
            var isAuthenticated = await _authService.CheckSessionAsync();

            if (!isAuthenticated)
            {
                _authService.Logout();
            }

            string returnAction = isAuthenticated ? Actions.loading_to_home : Actions.loading_to_auth;

            // Check for app updates (after session, so we know the return action)
            LoadingText = await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync("UI", "CHECKING_UPDATES").Task;
            var (updateResult, _) = await _appUpdateService.CheckForUpdateAsync();
            if (updateResult?.updateAvailable == true)
            {
                PendingUpdate = updateResult;
                ReturnActionOnSkip = returnAction;
                return Actions.loading_to_forceupdate;
            }

            await Task.Delay(500);

            return returnAction;
        }
    }
}
