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
        // public string TypeLabel { get; set; }
        // public string TypeIcon { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsFailed { get; set; }
        public float Progress { get; set; }
        public string StatusText { get; set; }
        /// <summary>"NN%" for a pending mission, so the quest shows that it is moving; null otherwise.</summary>
        public string ProgressLabel { get; set; }
        /// <summary>A mission of the current quest that the user hasn't started: the timeline shows a play button.</summary>
        public bool CanStart { get; set; }
        public string MissionCode => Item?.contentCode;

        public string TimelineDisplayLabel
        {
            get
            {
                if (Item == null)
                {
                    return string.Empty;
                }

                string prefix = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ACTIVITY_" + Item.contentType);
                string text;
                if (Item.contentType == QuestContentType.Mission || Item.contentType == QuestContentType.Challenge)
                {
                    text = Item.label;
                }
                else
                {
                    text = !string.IsNullOrEmpty(Item.label) ? Item.label : Item.contentCode;
                }

                // Backend labels sometimes already carry the type ("Challenge: ..."); don't prefix twice.
                string typeWord = (Item.contentType ?? string.Empty).Replace('_', ' ');
                if (HasTypePrefix(text, prefix) || HasTypePrefix(text, typeWord))
                {
                    return text;
                }
                return prefix + ": " + text;
            }
        }

        private static bool HasTypePrefix(string text, string type)
        {
            return !string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(type) &&
                   text.StartsWith(type + ":", StringComparison.OrdinalIgnoreCase);
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
        private readonly IFoodFactService _foodFactService;

        public Quest Quest => _quest;
        public QuestProgress QuestProgress => _questProgress;

        private readonly IGamificationService _gamificationService;

        public QuestDetailViewModel(
            IStoreService storeService,
            IQuestService questService,
            IDimensionService dimensionService,
            IQuizService quizService = null,
            IMissionService missionService = null,
            IChallengeService challengeService = null,
            IAuthService authService = null,
            IFoodFactService foodFactService = null,
            IGamificationService gamificationService = null) : base(storeService)
        {
            _gamificationService = gamificationService ?? App.current?.services?.GetService<IGamificationService>();
            _questService = questService;
            _dimensionService = dimensionService;
            _quizService = quizService;
            _missionService = missionService;
            _challengeService = challengeService;
            _authService = authService ?? App.current?.services?.GetService<IAuthService>();
            _foodFactService = foodFactService ?? App.current?.services?.GetService<IFoodFactService>();

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

                if (await TryLoadCurrentQuestAsync(codeOrId))
                {
                    return;
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

                Task<(FoodFactProgressResponse[] Result, ApiErrorResponse Error)> foodFactProgressTask =
                    _foodFactService != null
                        ? _foodFactService.GetUserProgressListAsync()
                        : Task.FromResult<(FoodFactProgressResponse[], ApiErrorResponse)>((null, null));

                await Task.WhenAll(questTask, progressTask, quizProgressTask, missionProgressTask, challengeProgressTask, foodFactProgressTask);

                var questResp = await questTask;
                var progressResp = await progressTask;
                var quizProgressResp = await quizProgressTask;
                var missionProgressResp = await missionProgressTask;
                var challengeProgressResp = await challengeProgressTask;
                var foodFactProgressResp = await foodFactProgressTask;

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
                    BuildStatusLookup(
                        quizProgressResp.Result,
                        missionProgressResp.Result,
                        challengeProgressResp.Result,
                        foodFactProgressResp.Result));
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
            ChallengeProgress[] challengeProgress = null,
            FoodFactProgressResponse[] foodFactProgress = null)
        {
            _quest = quest;
            _questProgress = progress;
            PopulateFromQuest(quest, progress, BuildStatusLookup(quizProgress, missionProgress, challengeProgress, foodFactProgress));
        }

        /// <summary>
        /// The user's current quest comes from GET current-quest: one call with the quest and every item's
        /// status. Returns false (load the usual way) for other quests, or when the server has no current quest.
        /// </summary>
        private async Task<bool> TryLoadCurrentQuestAsync(string codeOrId)
        {
            string currentQuestId = _storeService?.GetAppState()?.userCurrentQuestId;
            if (_gamificationService == null || string.IsNullOrEmpty(currentQuestId) || codeOrId != currentQuestId)
            {
                return false;
            }

            var (current, error) = await _gamificationService.GetCurrentQuestAsync();
            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = error.message;
                return true;
            }
            if (current == null || current.questId != codeOrId)
            {
                return false;
            }

            _quest = CurrentQuestView.ToQuest(current);
            _questProgress = new QuestProgress { questId = current.questId, progress = current.progress, completed = current.completed };
            PopulateFromQuest(_quest, _questProgress, CurrentQuestView.StatusLookup(current));
            return true;
        }

        /// <summary>Item status from the per-type progress lists (quests other than the current one).</summary>
        private Func<QuestItem, QuestItemStatus> BuildStatusLookup(
            QuizProgress[] quizProgress,
            MissionProgress[] missionProgress,
            ChallengeProgress[] challengeProgress,
            FoodFactProgressResponse[] foodFactProgress)
        {
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
            var missionPercentByCode = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            var failedMissionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (missionProgress != null)
            {
                foreach (var mp in missionProgress)
                {
                    if (mp == null) continue;
                    string progressCode = !string.IsNullOrEmpty(mp.missionCode) ? mp.missionCode : _missionService?.GetCachedCode(mp.missionId);
                    if (!string.IsNullOrEmpty(progressCode))
                    {
                        missionPercentByCode[progressCode] = mp.progress;
                        if (MissionProgressState.IsFailed(mp))
                        {
                            failedMissionCodes.Add(progressCode);
                        }
                    }
                    if (MissionProgressState.IsCompleted(mp))
                    {
                        if (!string.IsNullOrEmpty(mp.missionId))
                        {
                            completedMissionCodes.Add(mp.missionId);
                            string code = _missionService?.GetCachedCode(mp.missionId);
                            if (!string.IsNullOrEmpty(code)) completedMissionCodes.Add(code);
                        }
                        if (!string.IsNullOrEmpty(mp.missionCode)) completedMissionCodes.Add(mp.missionCode);
                        if (!string.IsNullOrEmpty(mp.missionTitle)) completedMissionCodes.Add(mp.missionTitle);
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
                        if (!string.IsNullOrEmpty(cp.challengeId))
                        {
                            completedChallengeCodes.Add(cp.challengeId);
                            string code = _challengeService?.GetCachedCode(cp.challengeId);
                            if (!string.IsNullOrEmpty(code)) completedChallengeCodes.Add(code);
                        }
                        if (!string.IsNullOrEmpty(cp.challengeCode)) completedChallengeCodes.Add(cp.challengeCode);
                        if (!string.IsNullOrEmpty(cp.challengeTitle)) completedChallengeCodes.Add(cp.challengeTitle);
                    }
                }
            }

            var completedFoodFactCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (foodFactProgress != null)
            {
                foreach (var fp in foodFactProgress)
                {
                    if (fp == null) continue;
                    if (!string.IsNullOrEmpty(fp.foodFactId)) completedFoodFactCodes.Add(fp.foodFactId);
                    if (!string.IsNullOrEmpty(fp.foodFactCode)) completedFoodFactCodes.Add(fp.foodFactCode);
                }
            }

            return it =>
            {
                if (it == null)
                {
                    return default;
                }
                string cType = it.contentType ?? string.Empty;
                string code = it.contentCode ?? string.Empty;
                bool isMission = string.Equals(cType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase);

                bool completed = false;
                if (string.Equals(cType, QuestContentType.Quiz, StringComparison.OrdinalIgnoreCase))
                {
                    completed = completedQuizCodes.Contains(code);
                }
                else if (string.Equals(cType, QuestContentType.FoodFact, StringComparison.OrdinalIgnoreCase))
                {
                    completed = completedFoodFactCodes.Contains(code);
                }
                else if (isMission)
                {
                    completed = completedMissionCodes.Contains(code) ||
                                (!string.IsNullOrEmpty(it.label) && completedMissionCodes.Contains(it.label));
                }
                else if (string.Equals(cType, QuestContentType.Challenge, StringComparison.OrdinalIgnoreCase))
                {
                    completed = completedChallengeCodes.Contains(code) ||
                                (!string.IsNullOrEmpty(it.label) && completedChallengeCodes.Contains(it.label));
                }

                missionPercentByCode.TryGetValue(code, out float percent);
                return new QuestItemStatus
                {
                    Completed = completed,
                    Failed = isMission && failedMissionCodes.Contains(code),
                    MissionPercent = isMission ? percent : 0f,
                };
            };
        }

        private void PopulateFromQuest(Quest quest, QuestProgress progress, Func<QuestItem, QuestItemStatus> statusOf)
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
            QuestTitle = quest.GetDisplayName();
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

            var items = quest.items != null ? quest.items.OrderBy(it => it.sortOrder).ToArray() : Array.Empty<QuestItem>();
            var activityItems = new List<QuestActivityDisplayItem>();
            int completedCount = 0;

            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                if (it == null) continue;

                QuestItemStatus status = statusOf != null ? statusOf(it) : default;
                bool isItemCompleted = questIsCompleted || status.Completed;

                if (isItemCompleted)
                {
                    completedCount++;
                }

                // var (typeLabel, typeIcon) = GetTypeMetadata(it.contentType);

                string statusText = isItemCompleted
                    ? (LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_STATUS_COMPLETED"))
                    : (LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_STATUS_PENDING"));

                bool isItemFailed = !isItemCompleted && status.Failed;

                bool canStart = !isItemCompleted && status.NotStarted && IsCurrentQuest &&
                                string.Equals(it.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase);

                string progressLabel = null;
                if (isItemFailed)
                {
                    progressLabel = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "MISSION_STATUS_FAILED");
                }
                else if (!isItemCompleted && !canStart && string.Equals(it.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase))
                {
                    progressLabel = $"{(int)Math.Clamp(status.MissionPercent, 0f, 100f)}%";
                }

                activityItems.Add(new QuestActivityDisplayItem
                {
                    Item = it,
                    StepIndex = i + 1,
                    Title = !string.IsNullOrEmpty(it.label) ? it.label : it.contentCode,
                    // TypeLabel = typeLabel,
                    // TypeIcon = typeIcon,
                    IsCompleted = isItemCompleted,
                    IsFailed = isItemFailed,
                    Progress = isItemCompleted ? 100f : 0f,
                    StatusText = statusText,
                    ProgressLabel = progressLabel,
                    CanStart = canStart
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

        // private static (string Label, string Icon) GetTypeMetadata(string contentType)
        // {
        //     if (string.IsNullOrEmpty(contentType))
        //         return (LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QUEST_CONTENT_ACTIVITY"), "star");

        //     switch (contentType.ToUpperInvariant())
        //     {
        //         case QuestContentType.Quiz:
        //             return (LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QUEST_CONTENT_QUIZ"), "help-circle");
        //         case QuestContentType.FoodFact:
        //             return (LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QUEST_CONTENT_FOOD_FACT"), "lightbulb");
        //         case QuestContentType.Mission:
        //             return (LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QUEST_CONTENT_MISSION"), "target");
        //         case "CHALLENGE":
        //             return (LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QUEST_CONTENT_CHALLENGE"), "award");
        //         case QuestContentType.MicroLearning:
        //             return (LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QUEST_CONTENT_READING"), "book-open");
        //         default:
        //             return (LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QUEST_CONTENT_ACTIVITY"), "star");
        //     }
        // }

        private bool _isStartingMission;

        /// <summary>
        /// Starts a mission from the timeline (M.B1.3 with its meat count), then reloads the quest. Returns the started
        /// progress, or null when nothing was started.
        /// </summary>
        public async Task<MissionProgress> StartMissionAsync(QuestActivityDisplayItem activity, int? previousMeatMeals)
        {
            if (_missionService == null || activity == null || !activity.CanStart || _isStartingMission)
            {
                return null;
            }

            _isStartingMission = true;
            try
            {
                string code = activity.MissionCode;
                var (started, error) = await _missionService.StartMissionProgressAsync(code, MissionInputs.For(code, previousMeatMeals));
                if (!string.IsNullOrEmpty(_lastLoadedCodeOrId))
                {
                    await LoadQuestAsync(_lastLoadedCodeOrId);
                }
                if (error != null)
                {
                    // After the reload, which clears ErrorDetail
                    ErrorDetail = error;
                    return null;
                }
                return started ?? new MissionProgress { missionCode = code };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestDetailViewModel] StartMissionAsync error: {ex.Message}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
                return null;
            }
            finally
            {
                _isStartingMission = false;
            }
        }

        public void OpenActivity(QuestActivityDisplayItem activity)
        {
            // Activities can only be opened from the user's active quest
            if (activity?.Item == null || !IsCurrentQuest)
            {
                return;
            }

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
            else if (string.Equals(contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(code))
                {
                    RaiseNavigationRequested(Actions.open_mission, new Argument("code", code));
                }
            }
            else if (string.Equals(contentType, "CHALLENGE", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(code))
                {
                    RaiseNavigationRequested(Actions.open_challenge, new Argument("code", code));
                }
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

        /// <summary>A completed quest can't be started again (and the active one is already started).</summary>
        public bool CanStartQuest => _quest != null && !IsCompleted && !IsCurrentQuest;

        public async Task<bool> StartQuestAsync()
        {
            if (!CanStartQuest) return false;
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
                // The new quest is played from Home (no need to reload this screen)
                RaiseNavigationRequested(Actions.go_to_home);
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
