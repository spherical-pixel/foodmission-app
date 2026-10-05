using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class FoodFactScreenViewModel : ViewModelBase
    {
        [ObservableProperty]
        private FoodFact _foodFactData;

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ContentReward _earnedReward;

        private readonly IFoodFactService _foodFactService;
        private readonly IChallengeSessionService _challengeSession;
        private readonly IShareService _shareService;

        public FoodFactScreenViewModel(
            IStoreService storeService,
            IFoodFactService foodFactService,
            IChallengeSessionService challengeSession = null,
            IShareService shareService = null) : base(storeService)
        {
            _foodFactService = foodFactService;
            _challengeSession = challengeSession ?? App.current?.services?.GetService<IChallengeSessionService>();
            _shareService = shareService ?? App.current?.services?.GetService<IShareService>();
        }

        public async Task LoadFoodFactDataByCodeOrId(string codeOrId)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return;

            if (_foodFactService != null)
            {
                IsLoading = true;
                ErrorDetail = null;

                var (result, error) = await _foodFactService.GetFoodFactAsync(codeOrId);

                if (error != null)
                {
                    ErrorDetail = error;
                    IsLoading = false;
                    return;
                }

                ErrorDetail = null;
                IsLoading = false;
                FoodFactData = result;

                Debug.Log($"[{GetType().Name}] LoadFoodFactDataByCodeOrId -> {result?.code}");
            }
        }

        /// <summary>
        /// Opens the native share sheet with the fact text. The image is only passed by the dev long press for now.
        /// </summary>
        public Task<bool> ShareAsync(Sprite image = null)
        {
            if (_shareService == null || FoodFactData == null || string.IsNullOrWhiteSpace(FoodFactData.body))
            {
                return Task.FromResult(false);
            }

            string footer = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SHARE_FOOTER");
            return _shareService.ShareAsync(new ShareContent
            {
                Text = ShareTextBuilder.Build(FoodFactData.body, FoodFactData.source, footer),
                Subject = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SHARE_SUBJECT_FOOD_FACT"),
                Image = image
            });
        }

        public async Task<ContentReward> MarkAsReadAsync()
        {
            if (FoodFactData == null || string.IsNullOrEmpty(FoodFactData.code))
                return null;

            if (_foodFactService == null)
                return null;

            // Reading counts even if the fact was already marked as read before
            _ = _challengeSession?.ReportAsync(ChallengeCompletionTrigger.FoodFactRead, FoodFactData.code);

            try
            {
                var (progress, error) = await _foodFactService.MarkAsReadAsync(FoodFactData.code);
                Debug.Log($"[{GetType().Name}] MarkAsReadAsync result -> reward: {(progress?.reward != null ? $"xp={progress.reward.xp}, points={progress.reward.points}" : "null")}, error: {error?.message}");
                if (progress?.reward != null &&
                    ((progress.reward.xp.HasValue && progress.reward.xp.Value > 0) ||
                     (progress.reward.points.HasValue && progress.reward.points.Value > 0) ||
                     !string.IsNullOrEmpty(progress.reward.badgeId)))
                {
                    EarnedReward = progress.reward;
                    _storeService?.store?.Dispatch(AppActions.addWalletReward.Invoke(new AppActions.WalletPayload(EarnedReward.xp ?? 0, EarnedReward.points ?? 0)));
                    return EarnedReward;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] MarkAsReadAsync failed: {ex.Message}");
            }

            return null;
        }
    }
}
