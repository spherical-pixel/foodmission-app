using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    public class QuestActivityDisplayItem
    {
        public QuestItem Item { get; set; }
        public int StepIndex { get; set; }
        public string Title { get; set; }
        public string TypeLabel { get; set; }
        public string TypeIcon { get; set; }
        public bool IsCompleted { get; set; }
        public float Progress { get; set; }
        public string StatusText { get; set; }

        public string TimelineDisplayLabel
        {
            get
            {
                if (Item == null)
                {
                    return string.Empty;
                }

                string generatedLabel = "";
                generatedLabel += LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ACTIVITY_" + Item.contentType) + ": ";
                if (Item.contentType == QuestContentType.Quiz)
                {
                    generatedLabel += Item.contentCode;
                }
                else if (Item.contentType == QuestContentType.Mission)
                {
                    generatedLabel += Item.label;
                }
                else if (Item.contentType == QuestContentType.Challenge)
                {
                    generatedLabel += Item.label;
                }
                else if (Item.contentType == QuestContentType.FoodFact)
                {
                    generatedLabel += Item.contentCode;
                }
                else
                {
                    generatedLabel += Item.label ?? Item.contentCode;
                }
                return generatedLabel;
            }
        }
    }

    [ObservableObject]
    public partial class QuestDetailViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _questCode = string.Empty;

        [ObservableProperty]
        private string _questTitle = string.Empty;

        [ObservableProperty]
        private string _questDescription = string.Empty;

        [ObservableProperty]
        private string _questLevel = eu.foodmission.platform.QuestLevel.Beginner;

        [ObservableProperty]
        private string _dimensionCode;

        [ObservableProperty]
        private string _dimensionName;

        [ObservableProperty]
        private float _progressPercent;

        [ObservableProperty]
        private bool _isCompleted;

        [ObservableProperty]
        private int _completedActivitiesCount;

        [ObservableProperty]
        private int _totalActivitiesCount;

        [ObservableProperty]
        private IReadOnlyList<QuestActivityDisplayItem> _activities = new List<QuestActivityDisplayItem>();

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private bool _isCurrentQuest;

        [ObservableProperty]
        private bool _isStartingQuest;

        [ObservableProperty]
        private ContentReward _earnedReward;

        private Quest _quest;
        private QuestProgress _questProgress;
        private string _lastLoadedCodeOrId;
        private string _lastLoadedLang;

        private readonly IQuestService _questService;
        private readonly IDimensionService _dimensionService;
        private readonly IQuizService _quizService;
        private readonly IMissionService _missionService;
        private readonly IChallengeService _challengeService;
        private readonly IAuthService _authService;

        public Quest Quest => _quest;
        public QuestProgress QuestProgress => _questProgress;

        public QuestDetailViewModel(
            IStoreService storeService,
            IQuestService questService,
            IDimensionService dimensionService,
            IQuizService quizService = null,
            IMissionService missionService = null,
            IChallengeService challengeService = null,
            IAuthService authService = null) : base(storeService)
        {
            _questService = questService;
            _dimensionService = dimensionService;
            _quizService = quizService;
            _missionService = missionService;
            _challengeService = challengeService;
            _authService = authService ?? App.current?.services?.GetService<IAuthService>();

            if (_store != null)
            {
                _lastLoadedLang = _storeService?.GetAppState()?.lang;
                _storeSubscription = _store.Subscribe(
                    state => (state.lang, state.userCurrentQuestId),
                    tuple =>
                    {
                        OnLanguageChanged(tuple.lang);
                        UpdateCurrentQuestStatus(tuple.userCurrentQuestId);
                    }
                );
            }
        }

        private void OnLanguageChanged(string newLang)
        {
            if (!string.IsNullOrEmpty(newLang) && !string.Equals(newLang, _lastLoadedLang, StringComparison.OrdinalIgnoreCase))
            {
                _lastLoadedLang = newLang;
                if (!string.IsNullOrEmpty(_lastLoadedCodeOrId))
                {
                    _ = LoadQuestAsync(_lastLoadedCodeOrId, forceRefresh: true);
                }
            }
        }

        public async Task LoadQuestAsync(string codeOrId, bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(codeOrId)) return;
            if (_isLoading) return;

            _lastLoadedCodeOrId = codeOrId;
            IsLoading = true;
            ErrorMessage = null;
            ErrorDetail = null;

            try
            {
                if (_dimensionService != null && (!_dimensionService.IsLoaded || forceRefresh))
                {
                    await _dimensionService.PreloadAsync(force: forceRefresh);
                }

                Task<(Quest Result, ApiErrorResponse Error)> questTask =
                    _questService != null
                        ? _questService.GetQuestAsync(codeOrId)
                        : Task.FromResult<(Quest, ApiErrorResponse)>((null, null));

                Task<(QuestProgress Result, ApiErrorResponse Error)> progressTask =
                    _questService != null
                        ? _questService.GetQuestProgressAsync(codeOrId)
                        : Task.FromResult<(QuestProgress, ApiErrorResponse)>((null, null));

                Task<(QuizProgress[] Result, ApiErrorResponse Error)> quizProgressTask =
                    _quizService != null
                        ? _quizService.GetUserProgressListAsync()
                        : Task.FromResult<(QuizProgress[], ApiErrorResponse)>((null, null));

                Task<(MissionProgress[] Result, ApiErrorResponse Error)> missionProgressTask =
                    _missionService != null
                        ? _missionService.GetUserProgressListAsync()
                        : Task.FromResult<(MissionProgress[], ApiErrorResponse)>((null, null));

                Task<(ChallengeProgress[] Result, ApiErrorResponse Error)> challengeProgressTask =
                    _challengeService != null
                        ? _challengeService.GetUserProgressListAsync()
                        : Task.FromResult<(ChallengeProgress[], ApiErrorResponse)>((null, null));

                await Task.WhenAll(questTask, progressTask, quizProgressTask, missionProgressTask, challengeProgressTask);

                var questResp = await questTask;
                var progressResp = await progressTask;
                var quizProgressResp = await quizProgressTask;
                var missionProgressResp = await missionProgressTask;
                var challengeProgressResp = await challengeProgressTask;

                if (questResp.Error != null)
                {
                    ErrorDetail = questResp.Error;
                    ErrorMessage = questResp.Error.message;
                    IsLoading = false;
                    return;
                }

                _quest = questResp.Result;
                _questProgress = progressResp.Result;

                PopulateFromQuest(
                    _quest,
                    _questProgress,
                    quizProgressResp.Result,
                    missionProgressResp.Result,
                    challengeProgressResp.Result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadQuestAsync failed: {ex.Message}");
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void SetQuestForTesting(
            Quest quest,
            QuestProgress progress,
            QuizProgress[] quizProgress = null,
            MissionProgress[] missionProgress = null,
            ChallengeProgress[] challengeProgress = null)
        {
            _quest = quest;
            _questProgress = progress;
            PopulateFromQuest(quest, progress, quizProgress, missionProgress, challengeProgress);
        }

        private void PopulateFromQuest(
            Quest quest,
            QuestProgress progress,
            QuizProgress[] quizProgress,
            MissionProgress[] missionProgress,
            ChallengeProgress[] challengeProgress)
        {
            if (quest == null)
            {
                QuestCode = string.Empty;
                QuestTitle = string.Empty;
                QuestDescription = string.Empty;
                QuestLevel = eu.foodmission.platform.QuestLevel.Beginner;
                DimensionCode = string.Empty;
                DimensionName = string.Empty;
                ProgressPercent = 0f;
                IsCompleted = false;
                TotalActivitiesCount = 0;
                CompletedActivitiesCount = 0;
                Activities = new List<QuestActivityDisplayItem>();
                return;
            }

            QuestCode = quest.code ?? string.Empty;
            QuestTitle = !string.IsNullOrEmpty(quest.title) ? quest.title : (!string.IsNullOrEmpty(quest.name) ? quest.name : quest.code);
            QuestDescription = quest.description ?? string.Empty;
            QuestLevel = quest.level ?? eu.foodmission.platform.QuestLevel.Beginner;

            UpdateCurrentQuestStatus(_storeService?.GetAppState()?.userCurrentQuestId);

            // Resolve dimension
            var dim = _dimensionService?.GetDimension(quest.dimensionId);
            DimensionCode = dim?.code ?? quest.dimensionId ?? string.Empty;
            DimensionName = dim?.name ?? DimensionCode;

            // Overall Quest progress
            bool questIsCompleted = progress != null && (progress.completed || progress.progress >= 100f);
            float overallProgress = progress != null ? progress.progress : 0f;

            if (progress?.reward != null &&
                ((progress.reward.xp.HasValue && progress.reward.xp.Value > 0) ||
                 (progress.reward.points.HasValue && progress.reward.points.Value > 0) ||
                 !string.IsNullOrEmpty(progress.reward.badgeId)))
            {
                EarnedReward = progress.reward;
                _storeService?.store?.Dispatch(AppActions.addWalletReward.Invoke(new AppActions.WalletPayload(progress.reward.xp ?? 0, progress.reward.points ?? 0)));
            }
            else
            {
                EarnedReward = null;
            }

            // Map sub-activity progress
            var completedQuizCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (quizProgress != null)
            {
                foreach (var qp in quizProgress)
                {
                    if (qp == null) continue;
                    if (qp.completed && (qp.isCorrect == null || qp.isCorrect == true))
                    {
                        if (!string.IsNullOrEmpty(qp.quizId)) completedQuizCodes.Add(qp.quizId);
                        if (!string.IsNullOrEmpty(qp.quizCode)) completedQuizCodes.Add(qp.quizCode);
                    }
                }
            }

            var completedMissionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (missionProgress != null)
            {
                foreach (var mp in missionProgress)
                {
                    if (mp == null) continue;
                    if (mp.completed || mp.progress >= 100f)
                    {
                        if (!string.IsNullOrEmpty(mp.missionId)) completedMissionCodes.Add(mp.missionId);
                    }
                }
            }

            var completedChallengeCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (challengeProgress != null)
            {
                foreach (var cp in challengeProgress)
                {
                    if (cp == null) continue;
                    if (cp.completed || cp.progress >= 100f)
                    {
                        if (!string.IsNullOrEmpty(cp.challengeId)) completedChallengeCodes.Add(cp.challengeId);
                    }
                }
            }

            var items = quest.items != null ? quest.items.OrderBy(it => it.sortOrder).ToArray() : Array.Empty<QuestItem>();
            var activityItems = new List<QuestActivityDisplayItem>();
            int completedCount = 0;

            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                if (it == null) continue;

                bool isItemCompleted = questIsCompleted;

                if (!isItemCompleted)
                {
                    string cType = it.contentType ?? string.Empty;
                    string code = it.contentCode ?? string.Empty;

                    if (string.Equals(cType, QuestContentType.Quiz, StringComparison.OrdinalIgnoreCase))
                    {
                        isItemCompleted = completedQuizCodes.Contains(code);
                    }
                    else if (string.Equals(cType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase))
                    {
                        isItemCompleted = completedMissionCodes.Contains(code);
                    }
                    else if (string.Equals(cType, "CHALLENGE", StringComparison.OrdinalIgnoreCase))
                    {
                        isItemCompleted = completedChallengeCodes.Contains(code);
                    }
                }

                if (isItemCompleted)
                {
                    completedCount++;
                }

                var (typeLabel, typeIcon) = GetTypeMetadata(it.contentType);

                string statusText = isItemCompleted
                    ? (LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_STATUS_COMPLETED") ?? "Completado")
                    : (LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_STATUS_PENDING") ?? "Pendiente");

                activityItems.Add(new QuestActivityDisplayItem
                {
                    Item = it,
                    StepIndex = i + 1,
                    Title = !string.IsNullOrEmpty(it.label) ? it.label : it.contentCode,
                    TypeLabel = typeLabel,
                    TypeIcon = typeIcon,
                    IsCompleted = isItemCompleted,
                    Progress = isItemCompleted ? 100f : 0f,
                    StatusText = statusText
                });
            }

            TotalActivitiesCount = activityItems.Count;
            CompletedActivitiesCount = completedCount;

            if (items.Length > 0 && !questIsCompleted)
            {
                overallProgress = Mathf.Max(overallProgress, (float)completedCount / items.Length * 100f);
            }

            ProgressPercent = overallProgress;
            IsCompleted = questIsCompleted || (items.Length > 0 && completedCount == items.Length);
            Activities = activityItems;
        }

        private static (string Label, string Icon) GetTypeMetadata(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return ("Actividad", "star");

            switch (contentType.ToUpperInvariant())
            {
                case QuestContentType.Quiz:
                    return ("Quiz", "help-circle");
                case QuestContentType.FoodFact:
                    return ("Dato curioso", "lightbulb");
                case QuestContentType.Mission:
                    return ("Misión", "target");
                case "CHALLENGE":
                    return ("Desafío", "award");
                case QuestContentType.MicroLearning:
                    return ("Lectura", "book-open");
                default:
                    return ("Actividad", "star");
            }
        }

        public void OpenActivity(QuestActivityDisplayItem activity)
        {
            if (activity?.Item == null) return;

            string contentType = activity.Item.contentType ?? string.Empty;
            string code = activity.Item.contentCode ?? string.Empty;

            if (string.Equals(contentType, QuestContentType.Quiz, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(code))
                {
                    RaiseNavigationRequested(Actions.open_quiz, new Argument("code", code));
                }
            }
            else if (string.Equals(contentType, QuestContentType.FoodFact, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(code))
                {
                    RaiseNavigationRequested(Actions.open_food_fact, new Argument("code", code));
                }
            }
            else if (string.Equals(contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(contentType, "CHALLENGE", StringComparison.OrdinalIgnoreCase))
            {
                // Future extension point: when Mission or Challenge detail screen is created, navigate here.
                Debug.Log($"[{GetType().Name}] OpenActivity for {contentType} ({code}): action deferred for future implementation.");
            }
        }

        private void UpdateCurrentQuestStatus(string currentQuestId)
        {
            if (_quest == null || string.IsNullOrEmpty(currentQuestId))
            {
                IsCurrentQuest = false;
                return;
            }

            IsCurrentQuest = string.Equals(currentQuestId, _quest.id, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(currentQuestId, _quest.code, StringComparison.OrdinalIgnoreCase);
        }

        public bool HasOtherActiveQuest
        {
            get
            {
                var currentQuestId = _storeService?.GetAppState()?.userCurrentQuestId;
                if (string.IsNullOrEmpty(currentQuestId) || _quest == null) return false;

                bool isSameQuest = string.Equals(currentQuestId, _quest.id, StringComparison.OrdinalIgnoreCase) ||
                                  (!string.IsNullOrEmpty(_quest.code) && string.Equals(currentQuestId, _quest.code, StringComparison.OrdinalIgnoreCase));

                return !isSameQuest;
            }
        }

        public string CurrentActiveQuestId => _storeService?.GetAppState()?.userCurrentQuestId;

        public async Task<bool> StartQuestAsync()
        {
            if (_quest == null) return false;
            string targetId = !string.IsNullOrEmpty(_quest.id) ? _quest.id : _quest.code;
            if (string.IsNullOrEmpty(targetId)) return false;
            if (_isStartingQuest) return false;

            IsStartingQuest = true;
            try
            {
                if (_authService != null)
                {
                    var req = new ProfileUpdateRequest
                    {
                        currentQuestId = targetId
                    };
                    var (success, error) = await _authService.UpdateProfileAsync(req);
                    if (!success)
                    {
                        Debug.LogWarning($"[{GetType().Name}] StartQuestAsync failed to update profile: {error?.message}");
                    }
                }

                _storeService?.store?.Dispatch(AppActions.setCurrentQuest.Invoke(targetId));
                IsCurrentQuest = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] StartQuestAsync exception: {ex.Message}");
                return false;
            }
            finally
            {
                IsStartingQuest = false;
            }
        }
    }
}
