using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace eu.foodmission.platform
{
    public enum PendingOnboardingType
    {
        None,
        Profile,
        Survey,
        Goals
    }

    [ObservableObject]
    public partial class HomeScreenViewModel : ViewModelBase
    {
        [ObservableProperty]
        private TimePeriod _selectedTimePeriod = TimePeriod.TODAY;

        [ObservableProperty]
        private UserScope _selectedUserScope = UserScope.ME;

        [ObservableProperty]
        private float _healthProgress = 0.65f;

        [ObservableProperty]
        private float _sustainabilityProgress = 0.42f;

        [ObservableProperty]
        private float _knowledgeProgress = 0.78f;

        [ObservableProperty]
        private int _caloriesConsumed = 1850;

        [ObservableProperty]
        private int _caloriesLeft = 350;

        [ObservableProperty]
        private bool _hasActiveQuest;

        [ObservableProperty]
        private string _currentQuestTitle = "";

        [ObservableProperty]
        private string _currentQuestCode = "";

        [ObservableProperty]
        private string _currentQuestId = "";

        [ObservableProperty]
        private bool[] _currentQuestActivityStates = System.Array.Empty<bool>();

        private readonly INotificationService _notificationService;
        private readonly ILegalService _legalService;
        private readonly IPilotSurveyService _pilotSurveyService;
        private readonly IMissionNudgeService _missionNudgeService;
        private readonly IMissionFailureService _missionFailureService;
        private readonly IDailyFoodFactService _dailyFoodFactService;
        private readonly IAuthService _authService;
        private readonly ICatalogService _catalogService;
        private readonly IQuestService _questService;
        private readonly IDimensionService _dimensionService;
        private readonly IQuizService _quizService;
        private readonly IMissionService _missionService;
        private readonly IChallengeService _challengeService;
        private readonly IFoodFactService _foodFactService;
        private readonly IGamificationService _gamificationService;
        private readonly IBadgeService _badgeService;
        private readonly BadgeCelebrationTracker _badgeTracker = new BadgeCelebrationTracker();
        private readonly IQuestProgressionService _questProgressionService;
        private readonly IProgressWheelService _progressWheelService;
        private IDisposableSubscription _wheelsSubscription;
        private bool _isCheckingRewards;

        public HomeScreenViewModel(
            IStoreService storeService,
            IAudioService audioService,
            INotificationService notificationService = null,
            ILegalService legalService = null,
            IPilotSurveyService pilotSurveyService = null,
            ICatalogService catalogService = null,
            IQuestService questService = null,
            IQuizService quizService = null,
            IMissionService missionService = null,
            IChallengeService challengeService = null,
            IFoodFactService foodFactService = null,
            IGamificationService gamificationService = null,
            IQuestProgressionService questProgressionService = null,
            IMissionNudgeService missionNudgeService = null,
            IAuthService authService = null,
            IBadgeService badgeService = null,
            IProgressWheelService progressWheelService = null,
            IMissionFailureService missionFailureService = null,
            IDailyFoodFactService dailyFoodFactService = null,
            IDimensionService dimensionService = null) : base(storeService)
        {
            _notificationService = notificationService;
            _legalService = legalService ?? App.current?.services?.GetService<ILegalService>();
            _pilotSurveyService = pilotSurveyService ?? App.current?.services?.GetService<IPilotSurveyService>();
            _catalogService = catalogService ?? App.current?.services?.GetService<ICatalogService>();
            _questService = questService ?? App.current?.services?.GetService<IQuestService>();
            _dimensionService = dimensionService ?? App.current?.services?.GetService<IDimensionService>();
            _quizService = quizService ?? App.current?.services?.GetService<IQuizService>();
            _missionService = missionService ?? App.current?.services?.GetService<IMissionService>();
            _challengeService = challengeService ?? App.current?.services?.GetService<IChallengeService>();
            _foodFactService = foodFactService ?? App.current?.services?.GetService<IFoodFactService>();
            _gamificationService = gamificationService ?? App.current?.services?.GetService<IGamificationService>();
            _questProgressionService = questProgressionService ?? App.current?.services?.GetService<IQuestProgressionService>() ?? new QuestProgressionService();
            _missionNudgeService = missionNudgeService ?? App.current?.services?.GetService<IMissionNudgeService>();
            _missionFailureService = missionFailureService ?? App.current?.services?.GetService<IMissionFailureService>();
            _dailyFoodFactService = dailyFoodFactService ?? App.current?.services?.GetService<IDailyFoodFactService>();
            _authService = authService ?? App.current?.services?.GetService<IAuthService>();
            _badgeService = badgeService ?? App.current?.services?.GetService<IBadgeService>();
            _progressWheelService = progressWheelService ?? App.current?.services?.GetService<IProgressWheelService>();
            if (_progressWheelService != null)
            {
                _progressWheelService.LoadingChanged += OnProgressWheelsLoadingChanged;
            }

            // Get initial state
            AppState state = _storeService?.GetAppState();

            // Subscribe to user state changes
            if (_store != null)
            {
                _storeSubscription = _store.Subscribe(
                    SelectUserState,
                    OnUserStateChanged
                );
                _wheelsSubscription = _store.Subscribe(ProgressWheelSection.Signature, _ => ProgressWheelsChanged?.Invoke());
            }

            _ = LoadActiveQuestAsync();
        }

        private (string userId, string lang, string currentQuestId) SelectUserState(AppState state)
        {
            return (state.userId, state.lang, state.userCurrentQuestId);
        }

        private void OnUserStateChanged((string userId, string lang, string currentQuestId) userState)
        {
            _ = LoadActiveQuestAsync();
        }

        public void SetTimePeriod(TimePeriod period)
        {
            SelectedTimePeriod = period;
            // TODO: Update progress and stats based on selected period
        }

        public void SetUserScope(UserScope scope)
        {
            SelectedUserScope = scope;
            // TODO: Update progress and stats based on selected scope
        }

        public bool ShouldPromptForNotifications()
        {
            return _notificationService?.ShouldPromptForNotifications() ?? false;
        }

        public async System.Threading.Tasks.Task<bool> AcceptNotificationsAsync()
        {
            if (_notificationService != null)
            {
                return await _notificationService.AcceptNotificationsAsync();
            }
            return false;
        }

        public void DeclineNotifications()
        {
            _notificationService?.DeclineNotifications();
        }

        public async System.Threading.Tasks.Task<LegalConsentStatus> CheckPendingLegalConsentAsync()
        {
            if (_legalService == null) return null;
            var (status, error) = await _legalService.GetConsentStatusAsync();
            return status;
        }

        public async System.Threading.Tasks.Task<LegalDocument> GetLegalDocumentAsync(string docType)
        {
            if (_legalService == null) return null;
            var (doc, error) = await _legalService.GetLatestDocumentAsync(docType);
            return doc;
        }

        public async System.Threading.Tasks.Task<bool> AcceptLegalConsentAsync(string documentKey)
        {
            if (_legalService == null) return false;
            var (res, error) = await _legalService.AcceptConsentAsync(documentKey);
            return res != null && res.accepted;
        }

        public PendingOnboardingType GetPendingOnboardingType()
        {
            var state = _storeService.GetAppState();
            if (!state.hasCompletedExtendedProfile)
            {
                return PendingOnboardingType.Profile;
            }

            // Same order as the onboarding flow: profile → goals → survey
            bool goalsSet = state.userGoals != null && state.userGoals.Length > 0;
            if (!goalsSet)
            {
                return PendingOnboardingType.Goals;
            }

            bool surveyAnswered = state.userOnboardingSurvey != null && state.userOnboardingSurvey.HasAnswers();
            if (!surveyAnswered)
            {
                return PendingOnboardingType.Survey;
            }

            return PendingOnboardingType.None;
        }

        public void NavigateToOnboardingProfile()
        {
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.register_to_onboarding);
        }

        public event System.Action ProgressWheelsChanged;

        public ProgressWheelSectionModel GetProgressWheelSection()
        {
            return ProgressWheelSection.Build(_storeService?.GetAppState(), _progressWheelService?.IsLoading ?? false);
        }

        public void RefreshProgressWheels()
        {
            if (_progressWheelService != null)
            {
                _ = _progressWheelService.RefreshAsync();
            }
        }

        public Task SaveHiddenWheelsAsync(IReadOnlyCollection<string> hiddenKinds)
        {
            return _progressWheelService != null ? _progressWheelService.SetHiddenAsync(hiddenKinds) : Task.CompletedTask;
        }

        private void OnProgressWheelsLoadingChanged()
        {
            ProgressWheelsChanged?.Invoke();
        }

        protected override void OnDispose()
        {
            _wheelsSubscription?.Dispose();
            _wheelsSubscription = null;
            if (_progressWheelService != null)
            {
                _progressWheelService.LoadingChanged -= OnProgressWheelsLoadingChanged;
            }
            base.OnDispose();
        }

        public void NavigateToOnboardingSurvey()
        {
            RaiseNavigationRequested(
                Unity.AppUI.Navigation.Generated.Actions.onboardingprofile_to_onboarding_survey,
                new Unity.AppUI.Navigation.Argument("fromHome", "true")
            );
        }

        public void NavigateToOnboardingGoals()
        {
            // fromHome=true → on completion, OnboardingGoalsViewModel returns to Home
            RaiseNavigationRequested(
                Unity.AppUI.Navigation.Generated.Actions.go_to_onboarding_goals,
                new Unity.AppUI.Navigation.Argument("fromHome", "true"),
                new Unity.AppUI.Navigation.Argument("fromEditProfile", "false")
            );
        }

        public async System.Threading.Tasks.Task<SurveyDto> CheckPendingPilotSurveyAsync()
        {
            if (_pilotSurveyService == null) return null;
            return await _pilotSurveyService.GetPendingPilotSurveyAsync();
        }

        public void PostponePilotSurvey(string slug)
        {
            _pilotSurveyService?.PostponeSurvey(slug);
        }

        public void SkipPilotSurvey(string slug)
        {
            _pilotSurveyService?.SkipSurvey(slug);
        }

        public System.Threading.Tasks.Task<MissionNudge> CheckMissionNudgeAsync()
        {
            return _missionNudgeService != null
                ? _missionNudgeService.GetNudgeAsync()
                : System.Threading.Tasks.Task.FromResult<MissionNudge>(null);
        }

        /// <summary>Code of today's food fact for the active quest, or null.</summary>
        public System.Threading.Tasks.Task<string> CheckDailyFoodFactAsync()
        {
            return _dailyFoodFactService != null
                ? _dailyFoodFactService.GetFactToShowAsync()
                : System.Threading.Tasks.Task.FromResult<string>(null);
        }

        public void OpenFoodFact(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return;
            }
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.open_food_fact, new Unity.AppUI.Navigation.Argument("code", code));
        }

        /// <summary>Failed missions of the current quest the user hasn't been told about yet.</summary>
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<MissionProgress>> CheckFailedMissionsAsync()
        {
            return _missionFailureService != null
                ? _missionFailureService.GetUnacknowledgedFailuresAsync()
                : System.Threading.Tasks.Task.FromResult<System.Collections.Generic.IReadOnlyList<MissionProgress>>(Array.Empty<MissionProgress>());
        }

        public void AcknowledgeFailedMission(MissionProgress failed)
        {
            _missionFailureService?.Acknowledge(failed);
        }

        public async System.Threading.Tasks.Task<ApiErrorResponse> RestartFailedMissionAsync(MissionProgress failed)
        {
            if (_missionFailureService == null)
            {
                return null;
            }

            var (_, error) = await _missionFailureService.RestartAsync(failed);
            return error;
        }

        /// <summary>Opens the Nutri check-in; with a code only that mission is asked.</summary>
        public void OpenCheckIn(string missionCode = null)
        {
            var args = string.IsNullOrEmpty(missionCode)
                ? Array.Empty<Unity.AppUI.Navigation.Argument>()
                : new[] { new Unity.AppUI.Navigation.Argument("code", missionCode) };
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.open_mission_checkin, args);
        }

        public void OpenMissionModule(MissionModuleLink module)
        {
            if (module == null || string.IsNullOrEmpty(module.Action))
            {
                return;
            }
            RaiseNavigationRequested(module.Action);
        }

        public void NavigateToPilotSurvey(string slugOrId)
        {
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.open_pilot_survey, new Unity.AppUI.Navigation.Argument[]
            {
                new Unity.AppUI.Navigation.Argument("slugOrId", slugOrId)
            });
        }

        public PilotSurveyCycleState GetPilotCycleState()
        {
            return _pilotSurveyService?.GetCurrentCycleState();
        }

        public int GetPilotActiveDays()
        {
            return _pilotSurveyService?.GetActiveDaysCountInCurrentCycle() ?? 0;
        }

        public int GetPilotDaysSinceStart()
        {
            return _pilotSurveyService?.GetDaysSinceCurrentCycleStart() ?? 0;
        }

        public bool IsUserInPilotCountry()
        {
            return _pilotSurveyService?.IsPilotCountry() ?? false;
        }

        public bool DebugBypassEligibility
        {
            get => _pilotSurveyService?.DebugBypassEligibility ?? false;
            set
            {
                if (_pilotSurveyService != null)
                {
                    _pilotSurveyService.DebugBypassEligibility = value;
                }
            }
        }

        public string GetCurrentUserCountry()
        {
            return _storeService?.GetAppState()?.userCountry ?? "";
        }

        public async System.Threading.Tasks.Task<bool> HasAcceptedPilotConsentAsync()
        {
            return _pilotSurveyService != null && await _pilotSurveyService.HasAcceptedPilotConsentAsync();
        }

        public async System.Threading.Tasks.Task AcceptPilotConsentAsync()
        {
            if (_pilotSurveyService != null)
            {
                await _pilotSurveyService.AcceptPilotConsentAsync();
            }
        }

        public async System.Threading.Tasks.Task<(string content, ApiErrorResponse error)> GetPilotConsentFormAsync()
        {
            string countryCode = _storeService?.GetAppState()?.userCountry;
            if (string.IsNullOrEmpty(countryCode)) return (null, null);

            string lang = _storeService?.GetAppState()?.lang ?? "en";
            var catalog = _catalogService ?? App.current?.services?.GetService<ICatalogService>();
            if (catalog == null) return (null, null);

            var (data, error) = await catalog.GetConsentFormAsync(countryCode, lang);
            return (data?.content, error);
        }

        public void SetDebugUserCountry(string countryCode)
        {
            _pilotSurveyService?.SetDebugUserCountry(countryCode);
        }

        public void SetPilotDebugDays(int activeDays, int daysSinceStart)
        {
            _pilotSurveyService?.SetDebugDays(activeDays, daysSinceStart);
        }

        public void ResetPilotCycleSurveys()
        {
            _pilotSurveyService?.ResetCycleSurveysOnly();
        }

        public async System.Threading.Tasks.Task LoadActiveQuestAsync()
        {
            AppState state = _storeService?.GetAppState();
            string questId = state?.userCurrentQuestId;

            if (string.IsNullOrEmpty(questId) || _questService == null)
            {
                HasActiveQuest = false;
                CurrentQuestTitle = "";
                CurrentQuestCode = "";
                CurrentQuestId = "";
                CurrentQuestActivityStates = System.Array.Empty<bool>();
                return;
            }

            try
            {
                var (quest, error) = await _questService.GetQuestAsync(questId);
                if (error != null || quest == null)
                {
                    HasActiveQuest = false;
                    return;
                }

                CurrentQuestTitle = quest.GetDisplayName();
                CurrentQuestCode = quest.code ?? "";
                CurrentQuestId = quest.id ?? questId;

                var items = quest.items != null ? quest.items.OrderBy(it => it.sortOrder).ToArray() : System.Array.Empty<QuestItem>();
                int totalItems = items.Length;
                if (totalItems > 0)
                {
                    var progressTask = _questService.GetQuestProgressAsync(questId);
                    var quizProgressTask = _quizService != null ? _quizService.GetUserProgressListAsync() : System.Threading.Tasks.Task.FromResult<(QuizProgress[], ApiErrorResponse)>((null, null));
                    var missionProgressTask = _missionService != null ? _missionService.GetUserProgressListAsync() : System.Threading.Tasks.Task.FromResult<(MissionProgress[], ApiErrorResponse)>((null, null));
                    var challengeProgressTask = _challengeService != null ? _challengeService.GetUserProgressListAsync() : System.Threading.Tasks.Task.FromResult<(ChallengeProgress[], ApiErrorResponse)>((null, null));
                    var foodFactProgressTask = _foodFactService != null ? _foodFactService.GetUserProgressListAsync() : System.Threading.Tasks.Task.FromResult<(FoodFactProgressResponse[], ApiErrorResponse)>((null, null));

                    await System.Threading.Tasks.Task.WhenAll(progressTask, quizProgressTask, missionProgressTask, challengeProgressTask, foodFactProgressTask);

                    var (progress, _) = await progressTask;
                    var (quizProgress, _) = await quizProgressTask;
                    var (missionProgress, _) = await missionProgressTask;
                    var (challengeProgress, _) = await challengeProgressTask;
                    var (foodFactProgress, _) = await foodFactProgressTask;

                    float progressPct = progress != null ? progress.progress : 0f;
                    bool isQuestCompleted = progress != null && (progress.completed || progressPct >= 100f);

                    var completedQuizCodes = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
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

                    var completedMissionCodes = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                    if (missionProgress != null)
                    {
                        foreach (var mp in missionProgress)
                        {
                            if (mp == null) continue;
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

                    var completedChallengeCodes = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
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

                    var completedFoodFactCodes = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                    if (foodFactProgress != null)
                    {
                        foreach (var fp in foodFactProgress)
                        {
                            if (fp == null) continue;
                            if (!string.IsNullOrEmpty(fp.foodFactId)) completedFoodFactCodes.Add(fp.foodFactId);
                            if (!string.IsNullOrEmpty(fp.foodFactCode)) completedFoodFactCodes.Add(fp.foodFactCode);
                        }
                    }

                    bool hasAnySubProgress = completedQuizCodes.Count > 0 ||
                                             completedMissionCodes.Count > 0 ||
                                             completedChallengeCodes.Count > 0 ||
                                             completedFoodFactCodes.Count > 0;
                    var states = new bool[totalItems];

                    if (isQuestCompleted)
                    {
                        for (int i = 0; i < totalItems; i++) states[i] = true;
                    }
                    else if (hasAnySubProgress)
                    {
                        for (int i = 0; i < totalItems; i++)
                        {
                            var it = items[i];
                            if (it == null) continue;

                            bool isItemCompleted = false;
                            string cType = it.contentType ?? string.Empty;
                            string code = it.contentCode ?? string.Empty;

                            if (string.Equals(cType, QuestContentType.Quiz, System.StringComparison.OrdinalIgnoreCase))
                            {
                                isItemCompleted = completedQuizCodes.Contains(code);
                            }
                            else if (string.Equals(cType, QuestContentType.FoodFact, System.StringComparison.OrdinalIgnoreCase))
                            {
                                isItemCompleted = completedFoodFactCodes.Contains(code);
                            }
                            else if (string.Equals(cType, QuestContentType.Mission, System.StringComparison.OrdinalIgnoreCase))
                            {
                                isItemCompleted = completedMissionCodes.Contains(code) ||
                                                  (!string.IsNullOrEmpty(it.label) && completedMissionCodes.Contains(it.label));
                            }
                            else if (string.Equals(cType, "CHALLENGE", System.StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(cType, QuestContentType.Challenge, System.StringComparison.OrdinalIgnoreCase))
                            {
                                isItemCompleted = completedChallengeCodes.Contains(code) ||
                                                  (!string.IsNullOrEmpty(it.label) && completedChallengeCodes.Contains(it.label));
                            }

                            states[i] = isItemCompleted;
                        }
                    }
                    else
                    {
                        int completedCount = Mathf.Clamp(Mathf.RoundToInt((progressPct / 100f) * totalItems), 0, totalItems);
                        for (int i = 0; i < completedCount; i++) states[i] = true;
                    }

                    CurrentQuestActivityStates = states;
                }
                else
                {
                    CurrentQuestActivityStates = System.Array.Empty<bool>();
                }

                HasActiveQuest = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] LoadActiveQuestAsync exception: {ex.Message}");
                HasActiveQuest = false;
            }
        }

        public void OpenCurrentQuest()
        {
            if (!HasActiveQuest) return;

            var args = new System.Collections.Generic.List<Unity.AppUI.Navigation.Argument>();
            if (!string.IsNullOrEmpty(CurrentQuestCode))
                args.Add(new Unity.AppUI.Navigation.Argument("code", CurrentQuestCode));
            if (!string.IsNullOrEmpty(CurrentQuestId))
                args.Add(new Unity.AppUI.Navigation.Argument("id", CurrentQuestId));

            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.open_quest, args.ToArray());
        }

        public void OpenQuest(Quest quest)
        {
            if (quest == null)
            {
                return;
            }

            var args = new System.Collections.Generic.List<Unity.AppUI.Navigation.Argument>();
            if (!string.IsNullOrEmpty(quest.code))
            {
                args.Add(new Unity.AppUI.Navigation.Argument("code", quest.code));
            }
            if (!string.IsNullOrEmpty(quest.id))
            {
                args.Add(new Unity.AppUI.Navigation.Argument("id", quest.id));
            }
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.open_quest, args.ToArray());
        }

        public string GetCurrentQuestIdFromStore() => _storeService?.GetAppState()?.userCurrentQuestId;

        public void NavigateToQuests()
        {
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_quests);
        }

        public void NavigateToQuickMealLog()
        {
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.open_quick_meal_log);
        }

        public void NavigateToFoodComparison(string mode = "proteins", string source = "shopping_list")
        {
            var args = new[]
            {
                new Unity.AppUI.Navigation.Argument("challengeCode", "CH.B1.1"),
                new Unity.AppUI.Navigation.Argument("mode", mode),
                new Unity.AppUI.Navigation.Argument("source", source)
            };
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_food_comparison, args);
        }

        public void SetCurrentQuestForTesting(string title, string code, string id, bool[] states)
        {
            CurrentQuestTitle = title;
            CurrentQuestCode = code;
            CurrentQuestId = id;
            CurrentQuestActivityStates = states ?? System.Array.Empty<bool>();
            HasActiveQuest = !string.IsNullOrEmpty(title);
        }

        /// <summary>
        /// Safety net for a completed quest whose QUEST_COMPLETED was never celebrated (e.g. it was completed while the app was
        /// closed and the reward check missed it): returns its celebration, offers the next quest and clears the current quest.
        /// </summary>
        public async System.Threading.Tasks.Task<PendingRewardCelebration> CheckCompletedActiveQuestAsync()
        {
            AppState state = _storeService?.GetAppState();
            string questId = state?.userCurrentQuestId;
            if (string.IsNullOrEmpty(questId) || string.IsNullOrEmpty(state.userId) || _questService == null)
            {
                return null;
            }

            try
            {
                var (progress, error) = await _questService.GetQuestProgressAsync(questId);
                if (error != null || progress == null || !(progress.completed || progress.progress >= 100f))
                {
                    return null;
                }

                var celebratedQuests = LoadCelebratedQuests(state.userId);
                bool alreadyCelebrated = celebratedQuests.Contains(questId) ||
                                         (!string.IsNullOrEmpty(progress.questCode) && celebratedQuests.Contains(progress.questCode));
                PendingRewardCelebration celebration = null;
                if (!alreadyCelebrated)
                {
                    string code = !string.IsNullOrEmpty(progress.questCode) ? progress.questCode : questId;
                    celebration = new PendingRewardCelebration
                    {
                        Reward = new ContentReward
                        {
                            xp = progress.reward?.xp ?? 100,
                            points = progress.reward?.points
                        },
                        ContextTitle = "@UI:QUEST_REWARD_TITLE",
                        Code = code,
                        IsQuest = true,
                        UnlockedQuest = await FindNextQuestAsync(questId, progress.questCode)
                    };
                    MarkQuestCelebrated(state.userId, questId, progress.questCode);
                }

                await ClearCurrentQuestIfAsync(questId, progress.questCode);
                return celebration;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] CheckCompletedActiveQuestAsync error: {ex.Message}");
                return null;
            }
        }

        private async System.Threading.Tasks.Task<Quest> FindNextQuestAsync(string questId, string questCode)
        {
            if (_questService == null || _questProgressionService == null)
            {
                return null;
            }

            var (allQuests, _) = await _questService.GetQuestsAsync();
            if (allQuests == null || allQuests.Length == 0)
            {
                return null;
            }

            Quest next = _questProgressionService.GetNextQuest(questId, allQuests) ??
                         (!string.IsNullOrEmpty(questCode) ? _questProgressionService.GetNextQuest(questCode, allQuests) : null);
            return UnlessAboveUserLevel(next);
        }

        /// <summary>
        /// Null when the quest is above the user's level in its dimension: completing a quest the user started above
        /// their level must not offer (and open) the next one, which is locked in the Quests list.
        /// </summary>
        private Quest UnlessAboveUserLevel(Quest quest)
        {
            if (quest == null)
            {
                return null;
            }
            var gate = new LevelGate(_storeService?.GetAppState(), _dimensionService);
            return gate.GetLock(gate.DimensionById(quest.dimensionId), quest.level, started: false) == null ? quest : null;
        }

        /// <summary>A completed quest stops being the active one: the user picks the next quest (offered after the celebration).</summary>
        private async System.Threading.Tasks.Task ClearCurrentQuestIfAsync(string questId, string questCode)
        {
            string current = _storeService?.GetAppState()?.userCurrentQuestId;
            bool isCurrent = !string.IsNullOrEmpty(current) &&
                             (string.Equals(current, questId, System.StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(current, questCode, System.StringComparison.OrdinalIgnoreCase));
            if (!isCurrent)
            {
                return;
            }

            if (_authService != null)
            {
                var (success, error) = await _authService.UpdateProfileAsync(new ProfileUpdateRequest { clearCurrentQuest = true });
                if (!success)
                {
                    Debug.LogWarning($"[{GetType().Name}] Clearing the completed current quest failed: {error?.message}");
                }
            }
            _storeService?.store?.Dispatch(AppActions.setCurrentQuest.Invoke(string.Empty));
        }

        private static bool IsQuestEventCelebrated(UserEvent evt, System.Collections.Generic.HashSet<string> celebratedQuests)
        {
            string questId = evt.metadata?["questId"]?.ToString();
            string questCode = evt.metadata?["questCode"]?.ToString();
            return (!string.IsNullOrEmpty(questId) && celebratedQuests.Contains(questId)) ||
                   (!string.IsNullOrEmpty(questCode) && celebratedQuests.Contains(questCode));
        }

        private static string CelebratedQuestsKey(string userId) => $"celebrated_quest_ids_{userId}";

        private static System.Collections.Generic.HashSet<string> LoadCelebratedQuests(string userId) =>
            new System.Collections.Generic.HashSet<string>(
                PlayerPrefs.GetString(CelebratedQuestsKey(userId), "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries),
                System.StringComparer.OrdinalIgnoreCase);

        private static void MarkQuestCelebrated(string userId, string questId, string questCode)
        {
            var set = LoadCelebratedQuests(userId);
            if (!string.IsNullOrEmpty(questId))
            {
                set.Add(questId);
            }
            if (!string.IsNullOrEmpty(questCode))
            {
                set.Add(questCode);
            }
            PlayerPrefs.SetString(CelebratedQuestsKey(userId), string.Join(",", set.TakeLast(50)));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Badges earned since the last check, as celebrations. The backend grants badges asynchronously and
        /// has no inbox, so this diffs profile.badges against BadgeCelebrationTracker. Codes are marked
        /// celebrated only once their celebrations are built; a failed /badges/me leaves them for the next check.
        /// </summary>
        private async System.Threading.Tasks.Task<System.Collections.Generic.List<PendingRewardCelebration>> CollectBadgeCelebrationsAsync(
            string userId, GamificationProfileResponse profile)
        {
            var results = new System.Collections.Generic.List<PendingRewardCelebration>();
            string[] earned = profile.badges ?? new string[0];
            _storeService?.store?.Dispatch(AppActions.setBadges.Invoke(earned));

            if (_badgeService == null)
            {
                return results;
            }

            UserBadge[] details = null;
            if (_badgeTracker.IsFirstRun(userId))
            {
                // earnedAt is needed to tell a just-earned FIRST_STEP from history; without it, try again next check.
                var (firstRunResponse, firstRunError) = await _badgeService.GetMyBadgesAsync();
                if (firstRunResponse == null || firstRunError != null)
                {
                    return results;
                }
                details = firstRunResponse.badges;
            }

            var newCodes = _badgeTracker.GetNewlyEarned(userId, earned, details, System.DateTime.UtcNow);
            if (newCodes.Count == 0)
            {
                return results;
            }

            if (details == null)
            {
                var (response, error) = await _badgeService.GetMyBadgesAsync();
                if (response == null || error != null)
                {
                    return results;
                }
                details = response.badges;
            }

            foreach (string code in newCodes)
            {
                UserBadge badge = details?.FirstOrDefault(b => b != null && b.code == code);
                var (xp, points) = SumBadgeWalletEntries(profile.recentWalletEntries, code);

                results.Add(new PendingRewardCelebration
                {
                    Reward = new ContentReward
                    {
                        badgeId = code,
                        badgeName = string.IsNullOrEmpty(badge?.name) ? code : badge.name,
                        xp = xp > 0 ? (int?)xp : null,
                        points = points > 0 ? (int?)points : null
                    },
                    ContextTitle = "@UI:BADGE_EARNED_TITLE",
                    Code = code,
                    IsBadge = true
                });
            }

            _badgeTracker.MarkCelebrated(userId, newCodes);
            return results;
        }

        /// <summary>A badge's own reward is credited with reason "Badge {CODE} completed" (CompletionRewardService).</summary>
        private static (int Xp, int Points) SumBadgeWalletEntries(WalletEntry[] entries, string code)
        {
            int xp = 0;
            int points = 0;
            string prefix = $"Badge {code} ";
            foreach (var w in entries ?? new WalletEntry[0])
            {
                if (w == null || w.amount <= 0 || string.IsNullOrEmpty(w.currency) || string.IsNullOrEmpty(w.reason))
                {
                    continue;
                }
                if (!w.reason.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    continue;
                }
                if (w.currency.Equals("XP", System.StringComparison.OrdinalIgnoreCase))
                {
                    xp += w.amount;
                }
                else if (w.currency.Equals("POINTS", System.StringComparison.OrdinalIgnoreCase))
                {
                    points += w.amount;
                }
            }
            return (xp, points);
        }

        /// <summary>PlayerPrefs key of the newest gamification event already considered for celebrations.</summary>
        public static string CelebrationCursorKey(string userId) => $"last_seen_gamif_ts_{userId}";

        /// <summary>Dev time travel: moves the celebration cursor <paramref name="days"/> days back. Empty or unparsable values are returned unchanged.</summary>
        public static string ShiftCelebrationCursor(string timestamp, int days)
        {
            if (string.IsNullOrEmpty(timestamp)
                || !DateTime.TryParse(timestamp, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                return timestamp;
            }
            return parsed.AddDays(-days).ToString("o", System.Globalization.CultureInfo.InvariantCulture);
        }

        public async System.Threading.Tasks.Task<System.Collections.Generic.List<PendingRewardCelebration>> CheckPendingGamificationRewardsAsync()
        {
            if (_isCheckingRewards) return null;
            if (_gamificationService == null) return null;

            _isCheckingRewards = true;
            try
            {
                AppState state = _storeService?.GetAppState();
                if (state == null || string.IsNullOrEmpty(state.userId) || string.IsNullOrEmpty(state.accessToken))
                    return null;

                string userId = state.userId;
                string cursorKey = CelebrationCursorKey(userId);
                string celebratedKey = $"celebrated_gamif_ids_{userId}";

                string lastSeenTs = PlayerPrefs.GetString(cursorKey, "");
                string celebratedIdsRaw = PlayerPrefs.GetString(celebratedKey, "");
                var celebratedIds = new System.Collections.Generic.HashSet<string>(
                    celebratedIdsRaw.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                );

                var (profile, err) = await _gamificationService.GetGamificationProfileAsync(30, 30);
                if (profile == null || err != null)
                    return null;

                // Sync wallet balance to Redux store
                if (profile.wallet != null)
                {
                    _storeService?.store?.Dispatch(AppActions.setWalletBalance.Invoke(
                        new AppActions.WalletPayload(profile.wallet.xp, profile.wallet.points)));
                }

                // Badges do not depend on the mission/quest cursor below, so they are collected first.
                var badgeCelebrations = await CollectBadgeCelebrationsAsync(userId, profile);

                // If first time checking for this user, establish cursor with current newest event timestamp
                // so historical past events are not celebrated on cold start.
                if (string.IsNullOrEmpty(lastSeenTs))
                {
                    string newest = profile.recentEvents?
                        .Where(e => !string.IsNullOrEmpty(e.timestamp))
                        .OrderByDescending(e => e.timestamp)
                        .FirstOrDefault()?.timestamp ?? System.DateTime.UtcNow.ToString("o");

                    PlayerPrefs.SetString(cursorKey, newest);
                    PlayerPrefs.Save();
                    return badgeCelebrations.Count > 0 ? badgeCelebrations : null;
                }

                System.DateTime lastSeenDate;
                bool hasValidDate = System.DateTime.TryParse(lastSeenTs, out lastSeenDate);

                // Filter events strictly to MISSION_COMPLETED and QUEST_COMPLETED
                var candidateEvents = profile.recentEvents?
                    .Where(e => !string.IsNullOrEmpty(e.id) && !celebratedIds.Contains(e.id))
                    .Where(e => e.eventType == "MISSION_COMPLETED" || e.eventType == "QUEST_COMPLETED")
                    .Where(e =>
                    {
                        if (string.IsNullOrEmpty(e.timestamp)) return false;
                        if (hasValidDate && System.DateTime.TryParse(e.timestamp, out var eDate))
                        {
                            return eDate > lastSeenDate;
                        }
                        return string.Compare(e.timestamp, lastSeenTs, System.StringComparison.Ordinal) > 0;
                    })
                    .OrderBy(e => e.timestamp)
                    .ToList();

                // The cursor is only a floor set on the first run (so history isn't celebrated). It is not advanced:
                // celebratedIds decides what was already shown, so events missed by a failed check are retried.
                var celebratedQuests = LoadCelebratedQuests(userId);
                candidateEvents = candidateEvents?
                    .Where(e => e.eventType != "QUEST_COMPLETED" || !IsQuestEventCelebrated(e, celebratedQuests))
                    .ToList();

                if (candidateEvents == null || candidateEvents.Count == 0)
                {
                    PlayerPrefs.Save();
                    return badgeCelebrations.Count > 0 ? badgeCelebrations : null;
                }

                var results = new System.Collections.Generic.List<PendingRewardCelebration>();
                Quest[] cachedQuests = null;

                foreach (var evt in candidateEvents)
                {
                    bool isMission = evt.eventType == "MISSION_COMPLETED";
                    bool isQuest = evt.eventType == "QUEST_COMPLETED";

                    string code = null;
                    if (evt.metadata != null)
                    {
                        if (isMission) code = evt.metadata["missionCode"]?.ToString();
                        else if (isQuest)
                        {
                            code = evt.metadata["questCode"]?.ToString();
                            if (string.IsNullOrEmpty(code) && evt.metadata.ContainsKey("code"))
                            {
                                code = evt.metadata["code"]?.ToString();
                            }
                        }
                    }

                    // Correlate with wallet entries
                    int xpEarned = 0;
                    int pointsEarned = 0;

                    if (profile.recentWalletEntries != null && profile.recentWalletEntries.Length > 0)
                    {
                        foreach (var w in profile.recentWalletEntries)
                        {
                            if (string.IsNullOrEmpty(w.currency) || w.amount <= 0) continue;

                            bool matches = false;
                            if (!string.IsNullOrEmpty(w.eventId) && w.eventId == evt.id)
                            {
                                matches = true;
                            }
                            else if (!string.IsNullOrEmpty(w.reason))
                            {
                                if (!string.IsNullOrEmpty(code) && w.reason.Contains(code))
                                {
                                    matches = true;
                                }
                                else if (isMission && w.reason.StartsWith("Mission ", System.StringComparison.OrdinalIgnoreCase))
                                {
                                    matches = true;
                                }
                                else if (isQuest && w.reason.StartsWith("Quest ", System.StringComparison.OrdinalIgnoreCase))
                                {
                                    matches = true;
                                }
                            }

                            if (matches)
                            {
                                if (w.currency.Equals("XP", System.StringComparison.OrdinalIgnoreCase))
                                    xpEarned += w.amount;
                                else if (w.currency.Equals("POINTS", System.StringComparison.OrdinalIgnoreCase))
                                    pointsEarned += w.amount;
                            }
                        }
                    }

                    // Fallback to service progress if wallet entry was not matched
                    if (xpEarned == 0 && pointsEarned == 0 && !string.IsNullOrEmpty(code))
                    {
                        if (isMission && _missionService != null)
                        {
                            var (mProg, _) = await _missionService.GetMissionProgressAsync(code);
                            if (mProg?.reward != null)
                            {
                                xpEarned = mProg.reward.xp ?? 0;
                                pointsEarned = mProg.reward.points ?? 0;
                            }
                        }
                        else if (isQuest && _questService != null)
                        {
                            var (qProg, _) = await _questService.GetQuestProgressAsync(code);
                            if (qProg?.reward != null)
                            {
                                xpEarned = qProg.reward.xp ?? 0;
                                pointsEarned = qProg.reward.points ?? 0;
                            }
                        }
                    }

                    // Resolve next quest unlocked in sequence for quests
                    Quest unlockedQuest = null;
                    if (isQuest && !string.IsNullOrEmpty(code) && _questService != null && _questProgressionService != null)
                    {
                        if (cachedQuests == null)
                        {
                            var (allQuests, _) = await _questService.GetQuestsAsync();
                            cachedQuests = allQuests;
                        }

                        if (cachedQuests != null && cachedQuests.Length > 0)
                        {
                            unlockedQuest = UnlessAboveUserLevel(_questProgressionService.GetNextQuest(code, cachedQuests));
                        }
                    }

                    celebratedIds.Add(evt.id);
                    if (isQuest)
                    {
                        string questId = evt.metadata?["questId"]?.ToString();
                        MarkQuestCelebrated(userId, questId, code);
                        await ClearCurrentQuestIfAsync(questId, code);
                    }

                    var reward = new ContentReward
                    {
                        xp = xpEarned > 0 ? (int?)xpEarned : null,
                        points = pointsEarned > 0 ? (int?)pointsEarned : null
                    };

                    if (!reward.xp.HasValue && !reward.points.HasValue)
                    {
                        reward.xp = isQuest ? 100 : 50;
                    }

                    string contextTitle = isQuest
                        ? "@UI:QUEST_REWARD_TITLE"
                        : "@UI:MISSION_REWARD_TITLE";

                    results.Add(new PendingRewardCelebration
                    {
                        Reward = reward,
                        ContextTitle = contextTitle,
                        EventId = evt.id,
                        Code = code,
                        IsQuest = isQuest,
                        UnlockedQuest = unlockedQuest
                    });
                }

                // Persist celebrated IDs (limit to last 50)
                var trimmedCelebrated = celebratedIds.TakeLast(50);
                PlayerPrefs.SetString(celebratedKey, string.Join(",", trimmedCelebrated));
                PlayerPrefs.Save();

                // Order so individual activities are presented first, and quest completion last
                // Individual activities first, then quest completion, then badges (earned as a consequence of both)
                return results.OrderBy(r => r.IsQuest ? 1 : 0).Concat(badgeCelebrations).ToList();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] CheckPendingGamificationRewardsAsync error: {ex.Message}");
                return null;
            }
            finally
            {
                _isCheckingRewards = false;
            }
        }
    }

    public class PendingRewardCelebration
    {
        public ContentReward Reward { get; set; }
        public string ContextTitle { get; set; }
        public string EventId { get; set; }
        public string Code { get; set; }
        public bool IsQuest { get; set; }
        public bool IsBadge { get; set; }
        public Quest UnlockedQuest { get; set; }
    }
}
