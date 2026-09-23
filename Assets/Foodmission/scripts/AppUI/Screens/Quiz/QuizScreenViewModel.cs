using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

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




        public QuizScreenViewModel(IStoreService storeService, IAvatarService avatarService, IQuizService quizService) : base(storeService)
        {
            _quizService = quizService;
            //_storeService = storeService;

        }

        public async Task LoadQuizDataByCodeOrId(string codeOrId)
        {
            if (_quizService != null)
            {
                IsLoading = true;
                (Quiz result, ApiErrorResponse error) = await _quizService.GetQuizAsync(codeOrId);

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
