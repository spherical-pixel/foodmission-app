using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class QuizScreenViewModel : ViewModelBase
    {


        [ObservableProperty]
        private Quiz _quizData = null;

        [ObservableProperty]
        private QuizProgress _quizProgress = null;

        [ObservableProperty]
        private ApiErrorResponse _ErrorDetail;

        [ObservableProperty]
        private bool _IsLoading;


        private IQuizService _quizService;
        private readonly IShareService _shareService;
        private readonly IDimensionService _dimensionService;




        public QuizScreenViewModel(IStoreService storeService, IAvatarService avatarService, IQuizService quizService, IShareService shareService = null,
            IDimensionService dimensionService = null) : base(storeService)
        {
            _quizService = quizService;
            _shareService = shareService ?? App.current?.services?.GetService<IShareService>();
            _dimensionService = dimensionService ?? App.current?.services?.GetService<IDimensionService>();
            //_storeService = storeService;

        }

        public async Task LoadQuizDataByCodeOrId(string codeOrId)
        {
            if (_quizService != null)
            {
                IsLoading = true;
                var quizTask = _quizService.GetQuizAsync(codeOrId);
                // The topic banner resolves the quiz's topicId through the dimension catalog
                await DimensionCatalog.EnsureLoadedAsync(_dimensionService);
                (Quiz result, ApiErrorResponse error) = await quizTask;

                if (error != null)
                {
                    ErrorDetail = error;
                    IsLoading = false;
                    return;
                }

                ErrorDetail = null;
                IsLoading = false;

                QuizData = result;

                Debug.Log("LoadQuizDataByCodeOrId -> " + JsonUtility.ToJson(result));
            }
        }

        public Task<bool> ShareAsync()
        {
            if (_shareService == null || QuizData == null || string.IsNullOrWhiteSpace(QuizData.explanation))
            {
                return Task.FromResult(false);
            }

            string footer = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SHARE_FOOTER");
            return _shareService.ShareAsync(new ShareContent
            {
                Text = ShareTextBuilder.Build(QuizData.explanation, QuizData.source, footer),
                Subject = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SHARE_SUBJECT_QUIZ")
            });
        }

        public async Task SubmitResponse(QuizOption option)
        {
            (QuizProgress progress, ApiErrorResponse error) = await _quizService.SubmitQuizAnswerAsync(QuizData.id, option.label);
            if (error != null)
            {
                ErrorDetail = error;
                IsLoading = false;
                return;
            }

            ErrorDetail = null;
            IsLoading = false;

            QuizProgress = progress;

            if (progress?.reward != null &&
                ((progress.reward.xp.HasValue && progress.reward.xp.Value > 0) ||
                 (progress.reward.points.HasValue && progress.reward.points.Value > 0) ||
                 !string.IsNullOrEmpty(progress.reward.badgeId)))
            {
                _storeService?.store?.Dispatch(AppActions.addWalletReward.Invoke(new AppActions.WalletPayload(progress.reward.xp ?? 0, progress.reward.points ?? 0)));
            }

            Debug.Log("SubmitResponse -> " + JsonUtility.ToJson(progress));
        }




    }
}
