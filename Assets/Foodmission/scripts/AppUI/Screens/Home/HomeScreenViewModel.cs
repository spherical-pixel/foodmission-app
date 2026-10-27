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
        private float _healthProgress;

        [ObservableProperty]
        private float _foodChoicesProgress;

        [ObservableProperty]
        private float _foodWasteProgress;

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
        private readonly IOnboardingRun _onboardingRun;
        private readonly IMissionNudgeService _missionNudgeService;
        private readonly IMissionLastDayService _missionLastDayService;
        private readonly IMissionFailureService _missionFailureService;
        private readonly IMissionInputsService _missionInputsService;
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
        private readonly IKnowledgeProgressService _knowledgeProgressService;
        private IDisposableSubscription _knowledgeSubscription;
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
            IDimensionService dimensionService = null,
            IMissionLastDayService missionLastDayService = null,
            IKnowledgeProgressService knowledgeProgressService = null,
            IMissionInputsService missionInputsService = null,
            IOnboardingRun onboardingRun = null) : base(storeService)
        {
            _onboardingRun = onboardingRun ?? App.current?.services?.GetService<IOnboardingRun>();
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
            _missionLastDayService = missionLastDayService ?? App.current?.services?.GetService<IMissionLastDayService>();
            _missionFailureService = missionFailureService ?? App.current?.services?.GetService<IMissionFailureService>();
            _missionInputsService = missionInputsService ?? App.current?.services?.GetService<IMissionInputsService>();
            _dailyFoodFactService = dailyFoodFactService ?? App.current?.services?.GetService<IDailyFoodFactService>();
            _authService = authService ?? App.current?.services?.GetService<IAuthService>();
            _badgeService = badgeService ?? App.current?.services?.GetService<IBadgeService>();
            _progressWheelService = progressWheelService ?? App.current?.services?.GetService<IProgressWheelService>();
            _knowledgeProgressService = knowledgeProgressService ?? App.current?.services?.GetService<IKnowledgeProgressService>();
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
                _knowledgeSubscription = _store.Subscribe(KnowledgeBars.Signature, _ => ApplyKnowledgeBars(_storeService.GetAppState()));
            }
            ApplyKnowledgeBars(state);

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

            // Skipped goals are not reminded (issue #31): after the profile comes the survey
            // bool goalsSet = state.userGoals != null && state.userGoals.Length > 0;
            // if (!goalsSet)
            // {
            //     return PendingOnboardingType.Goals;
            // }

            bool surveyAnswered = state.userOnboardingSurvey != null && state.userOnboardingSurvey.HasAnswers();
            if (!surveyAnswered)
            {
                return PendingOnboardingType.Survey;
            }

            return PendingOnboardingType.None;
        }

        /// <summary>Profile reminder: starts an onboarding run, so the progress screen shows what is left.</summary>
        public void NavigateToOnboardingProfile()
        {
            _onboardingRun?.Start();
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_onboarding_progress);
        }

        /// <summary>Reaching Home ends any onboarding run (finished, skipped or left with the ✕).</summary>
        public void EndOnboardingRun()
        {
            _onboardingRun?.End();
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

        private void ApplyKnowledgeBars(AppState state)
        {
            HealthProgress = KnowledgeBars.Value(state, KnowledgeKind.Health);
            FoodChoicesProgress = KnowledgeBars.Value(state, KnowledgeKind.FoodChoices);
            FoodWasteProgress = KnowledgeBars.Value(state, KnowledgeKind.FoodWaste);
        }

        /// <summary>Bar value as a whole percentage, for screen-reader labels.</summary>
        public int KnowledgeBarPercent(string kind)
        {
            return Mathf.RoundToInt(KnowledgeBars.Value(_storeService?.GetAppState(), kind) * 100f);
        }

        public void RefreshKnowledgeProgress()
        {
            if (_knowledgeProgressService != null)
            {
                _ = _knowledgeProgressService.RefreshAsync();
            }
        }

        public void OpenKnowledgeBar(string kind)
        {
            RaiseNavigationRequested(
                Unity.AppUI.Navigation.Generated.Actions.open_knowledge_bar,
                new Unity.AppUI.Navigation.Argument("kind", kind));
        }

        public void OpenProgressWheelInfo(string kind)
        {
            RaiseNavigationRequested(
                Unity.AppUI.Navigation.Generated.Actions.open_progress_wheel_info,
                new Unity.AppUI.Navigation.Argument("kind", kind));
        }

        protected override void OnDispose()
        {
            _wheelsSubscription?.Dispose();
            _wheelsSubscription = null;
            _knowledgeSubscription?.Dispose();
            _knowledgeSubscription = null;
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

        /// <summary>Counts today as an active survey day and retries a failed cycle sync. Call on every Home entry.</summary>
        public void RecordPilotHomeEntry()
        {
            _pilotSurveyService?.OnHomeEntered();
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

        public System.Threading.Tasks.Task<MissionDeadlines.LastDay> CheckMissionLastDayAsync()
        {
            return _missionLastDayService != null
                ? _missionLastDayService.GetTodayAsync()
                : System.Threading.Tasks.Task.FromResult<MissionDeadlines.LastDay>(null);
        }

        public System.Threading.Tasks.Task<MissionDeadlines.ActiveMission> CheckMissionSecuredAsync()
        {
            return _missionLastDayService != null
                ? _missionLastDayService.GetSecuredToAnnounceAsync()
                : System.Threading.Tasks.Task.FromResult<MissionDeadlines.ActiveMission>(null);
        }

        public void MarkMissionSecuredAnnounced(MissionDeadlines.ActiveMission mission)
        {
            _missionLastDayService?.MarkSecuredAnnounced(mission);
        }

        public void MarkMissionLastDayShown()
        {
            _missionLastDayService?.MarkShown();
        }

        public bool IsMissionLastDayShownToday => _missionLastDayService?.ShownToday ?? false;

        /// <summary>Reschedules the last-day notifications from the current missions (progress, preferred time).</summary>
        public void SyncMissionLastDayReminders()
        {
            if (_missionLastDayService != null)
            {
                _ = _missionLastDayService.SyncRemindersAsync();
            }
        }

        /// <summary>Code of today's food fact for the active quest, or null.</summary>
        public void MarkNudgeShown(MissionNudge nudge)
        {
            _missionNudgeService?.MarkShown(nudge);
        }

        public void MarkDailyFoodFactShown(string code)
        {
            _dailyFoodFactService?.MarkShown(code);
        }

        public System.Threading.Tasks.Task<string> CheckDailyFoodFactAsync()
        {
            return _dailyFoodFactService != null
                ? _dailyFoodFactService.GetFactToShowAsync()
                : System.Threading.Tasks.Task.FromResult<string>(null);
        }

        /// <summary>
        /// What Nutri can remind about the quest: no current quest, or a current quest with missions waiting to start
        /// and none running. Null when there is nothing to remind or the current quest can't be loaded.
        /// </summary>
        public async System.Threading.Tasks.Task<QuestStartReminder> CheckQuestStartReminderAsync()
        {
            if (string.IsNullOrEmpty(_storeService?.GetAppState()?.userCurrentQuestId))
            {
                return QuestStartReminder.NoQuest();
            }
            if (_gamificationService == null)
            {
                return null;
            }

            var (quest, error) = await _gamificationService.GetCurrentQuestAsync();
            if (error != null)
            {
                return null;
            }
            if (quest == null)
            {
                // The stored quest is gone on the server (Home hides the card too)
                return QuestStartReminder.NoQuest();
            }
            return CurrentQuestView.MissionsWaitingToStart(quest) > 0 ? QuestStartReminder.MissionsToStart(quest) : null;
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

        /// <summary>Restarts a failed mission; M.B1.3 sends the meals with meat of last week for the new attempt.</summary>
        public async System.Threading.Tasks.Task<(MissionProgress Result, ApiErrorResponse Error)> RestartFailedMissionAsync(MissionProgress failed, int? previousMeatMeals = null)
        {
            if (_missionFailureService == null)
            {
                return (null, null);
            }

            return await _missionFailureService.RestartAsync(failed, MissionInputs.For(failed?.missionCode, previousMeatMeals));
        }

        /// <summary>Initial value of Nutri's "meals with meat last week" question.</summary>
        public System.Threading.Tasks.Task<int> SuggestPreviousMeatMealsAsync()
        {
            return _missionInputsService != null ? _missionInputsService.SuggestPreviousMeatMealsAsync() : System.Threading.Tasks.Task.FromResult(0);
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

        /// <summary>
        /// The active quest card from GET current-quest: one call with the quest and each item's status
        /// (no per-type progress lists). No current quest on the server, or an error, hides the card.
        /// </summary>
        public async System.Threading.Tasks.Task LoadActiveQuestAsync()
        {
            AppState state = _storeService?.GetAppState();
            string questId = state?.userCurrentQuestId;

            if (string.IsNullOrEmpty(questId) || _gamificationService == null)
            {
                ClearActiveQuest();
                return;
            }

            try
            {
                var (quest, error) = await _gamificationService.GetCurrentQuestAsync();
                if (error != null || quest == null)
                {
                    ClearActiveQuest();
                    return;
                }

                CurrentQuestTitle = quest.name ?? quest.questCode ?? "";
                CurrentQuestCode = quest.questCode ?? "";
                CurrentQuestId = quest.questId ?? questId;
                CurrentQuestActivityStates = CurrentQuestView.TimelineStates(quest);
                HasActiveQuest = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] LoadActiveQuestAsync exception: {ex.Message}");
                HasActiveQuest = false;
            }
        }

        private void ClearActiveQuest()
        {
            HasActiveQuest = false;
            CurrentQuestTitle = "";
            CurrentQuestCode = "";
            CurrentQuestId = "";
            CurrentQuestActivityStates = System.Array.Empty<bool>();
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
                if (alreadyCelebrated)
                {
                    // Nothing to show: just stop treating the completed quest as the active one
                    await ClearCurrentQuestIfAsync(questId, progress.questCode);
                    return null;
                }

                string code = !string.IsNullOrEmpty(progress.questCode) ? progress.questCode : questId;
                var celebration = new PendingRewardCelebration
                {
                    Reward = new ContentReward
                    {
                        xp = progress.reward?.xp ?? 100,
                        points = progress.reward?.points
                    },
                    ContextTitle = "@UI:QUEST_REWARD_TITLE",
                    ContextSubtitle = progress.questTitle,
                    Code = code,
                    QuestId = questId,
                    IsQuest = true
                };
                var (allQuests, _) = await _questService.GetQuestsAsync();
                Quest completedQuest = allQuests != null ? FindQuest(allQuests, questId) ?? FindQuest(allQuests, code) : null;
                if (!string.IsNullOrEmpty(completedQuest?.name) || string.IsNullOrEmpty(celebration.ContextSubtitle))
                {
                    celebration.ContextSubtitle = completedQuest?.GetDisplayName();
                }
                if (_questProgressionService != null)
                {
                    await ResolveQuestFollowUpAsync(celebration, allQuests);
                }
                return celebration;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] CheckCompletedActiveQuestAsync error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// What follows a completed quest: the next quest of its level. When it was the last one, the backend's
        /// DIMENSION_LEVEL_UP (celebrated on its own) offers the first quest of the new level; when every quest of the
        /// dimension is completed, the dimension is celebrated as completed.
        /// </summary>
        private async System.Threading.Tasks.Task ResolveQuestFollowUpAsync(PendingRewardCelebration celebration, Quest[] allQuests)
        {
            if (allQuests == null || allQuests.Length == 0 || _questProgressionService == null)
            {
                return;
            }

            Quest completed = FindQuest(allQuests, celebration.QuestId) ?? FindQuest(allQuests, celebration.Code);
            if (completed == null)
            {
                return;
            }

            Quest next = _questProgressionService.GetNextQuest(!string.IsNullOrEmpty(completed.id) ? completed.id : completed.code, allQuests);
            if (next != null)
            {
                celebration.UnlockedQuest = UnlessAboveUserLevel(next);
                return;
            }

            if (_questService == null)
            {
                return;
            }

            var (progress, error) = await _questService.GetUserProgressListAsync();
            if (error != null || progress == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Quest progress unavailable, no level up checked: {error?.message}");
                return;
            }

            var gate = new LevelGate(_storeService?.GetAppState(), _dimensionService);
            Dimension dimension = gate.DimensionById(completed.dimensionId);

            // Unknown dimension (catalog not loaded): no card rather than an id as its name
            if (_questProgressionService.IsDimensionCompleted(completed, allQuests, progress))
            {
                celebration.CompletedDimensionName = DimensionDisplayName(dimension);
            }
        }

        /// <summary>
        /// The celebration of a backend DIMENSION_LEVEL_UP event (metadata { dimensionCode, from, to }): the level card and the
        /// first quest of the new level. Null for an unknown dimension or level, so it is retried once the catalog is loaded.
        /// </summary>
        private PendingRewardCelebration BuildLevelUpCelebration(UserEvent evt, Quest[] allQuests)
        {
            string code = evt.metadata?["dimensionCode"]?.ToString();
            string level = ContentLevel.Normalize(evt.metadata?["to"]?.ToString());
            Dimension dimension = !string.IsNullOrEmpty(code) ? _dimensionService?.GetDimension(code) : null;
            if (dimension == null || level == null)
            {
                return null;
            }

            var celebration = new PendingRewardCelebration
            {
                ContextTitle = "@UI:DIMENSION_LEVEL_UP_TITLE",
                EventId = evt.id,
                LevelUp = new DimensionLevelUp
                {
                    DimensionCode = dimension.code,
                    DimensionName = DimensionDisplayName(dimension),
                    Level = level
                }
            };
            if (allQuests != null && _questProgressionService != null)
            {
                // Not filtered by the stored level: it is the new level, even if the refresh failed
                celebration.UnlockedQuest = _questProgressionService.GetFirstQuest(dimension.id, level, allQuests);
            }
            return celebration;
        }

        private static string DimensionDisplayName(Dimension dimension)
        {
            if (dimension == null)
            {
                return null;
            }
            return !string.IsNullOrEmpty(dimension.name) ? dimension.name : dimension.code;
        }

        private static Quest FindQuest(Quest[] quests, string codeOrId)
        {
            if (string.IsNullOrEmpty(codeOrId))
            {
                return null;
            }
            return quests.FirstOrDefault(q => q != null &&
                                              (string.Equals(q.id, codeOrId, System.StringComparison.OrdinalIgnoreCase) ||
                                               string.Equals(q.code, codeOrId, System.StringComparison.OrdinalIgnoreCase)));
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

        private const string LevelUpEventType = "DIMENSION_LEVEL_UP";
        private static readonly string[] CelebrationEventTypes = { "MISSION_COMPLETED", "QUEST_COMPLETED", LevelUpEventType };

        private static string CelebratedEventsKey(string userId) => $"celebrated_gamif_ids_{userId}";

        private static void MarkEventCelebrated(string userId, string eventId)
        {
            var ids = PlayerPrefs.GetString(CelebratedEventsKey(userId), "")
                .Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                .ToList();
            if (!ids.Contains(eventId))
            {
                ids.Add(eventId);
            }
            PlayerPrefs.SetString(CelebratedEventsKey(userId), string.Join(",", ids.Skip(System.Math.Max(0, ids.Count - 50))));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Call when a celebration is shown: only then is it remembered as celebrated (and a completed current quest
        /// cleared), so leaving Home before it shows never loses it.
        /// </summary>
        public async System.Threading.Tasks.Task MarkCelebrationShownAsync(PendingRewardCelebration celebration)
        {
            string userId = _storeService?.GetAppState()?.userId;
            if (celebration == null || string.IsNullOrEmpty(userId))
            {
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(celebration.EventId))
                {
                    MarkEventCelebrated(userId, celebration.EventId);
                }
                if (celebration.IsBadge && !string.IsNullOrEmpty(celebration.Code))
                {
                    _badgeTracker.MarkCelebrated(userId, new[] { celebration.Code });
                }
                if (celebration.IsQuest)
                {
                    MarkQuestCelebrated(userId, celebration.QuestId, celebration.Code);
                    await ClearCurrentQuestIfAsync(celebration.QuestId, celebration.Code);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] MarkCelebrationShownAsync error: {ex.Message}");
            }
        }

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
        /// celebrated when shown (MarkCelebrationShownAsync); a failed /badges/me leaves them for the next check.
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
                string celebratedKey = CelebratedEventsKey(userId);

                string lastSeenTs = PlayerPrefs.GetString(cursorKey, "");
                string celebratedIdsRaw = PlayerPrefs.GetString(celebratedKey, "");
                var celebratedIds = new System.Collections.Generic.HashSet<string>(
                    celebratedIdsRaw.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                );

                // Only celebration events: a meal log or check-in records many others that would push completions out of the 30
                var (profile, err) = await _gamificationService.GetGamificationProfileAsync(30, 30, CelebrationEventTypes);
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

                // Completed missions and quests, and the level ups the backend applied when a quest completed a level
                var candidateEvents = profile.recentEvents?
                    .Where(e => !string.IsNullOrEmpty(e.id) && !celebratedIds.Contains(e.id))
                    .Where(e => e.eventType == "MISSION_COMPLETED" || e.eventType == "QUEST_COMPLETED" || e.eventType == LevelUpEventType)
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

                if (candidateEvents.Any(e => e.eventType == LevelUpEventType))
                {
                    // The backend already raised the level: refresh the stored levels (gate, locks, "Tu nivel")
                    await DimensionLevels.RefreshAsync(_gamificationService, _storeService);
                }

                foreach (var evt in candidateEvents)
                {
                    if (evt.eventType == LevelUpEventType)
                    {
                        if (cachedQuests == null && _questService != null)
                        {
                            var (allQuests, _) = await _questService.GetQuestsAsync();
                            cachedQuests = allQuests;
                        }
                        PendingRewardCelebration levelUp = BuildLevelUpCelebration(evt, cachedQuests);
                        if (levelUp != null)
                        {
                            results.Add(levelUp);
                        }
                        continue;
                    }

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

                    string eventQuestId = isQuest ? evt.metadata?["questId"]?.ToString() : null;

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

                    var celebration = new PendingRewardCelebration
                    {
                        Reward = reward,
                        ContextTitle = contextTitle,
                        EventId = evt.id,
                        QuestId = eventQuestId,
                        Code = code,
                        IsQuest = isQuest
                    };

                    if (isQuest && !string.IsNullOrEmpty(code) && _questService != null)
                    {
                        if (cachedQuests == null)
                        {
                            var (allQuests, _) = await _questService.GetQuestsAsync();
                            cachedQuests = allQuests;
                        }
                        Quest completedQuest = cachedQuests != null
                            ? FindQuest(cachedQuests, eventQuestId) ?? FindQuest(cachedQuests, code)
                            : null;
                        celebration.ContextSubtitle = completedQuest?.GetDisplayName();

                        // The next quest unlocked in sequence, or the level up when it was the last one of the user's level
                        if (_questProgressionService != null)
                        {
                            await ResolveQuestFollowUpAsync(celebration, cachedQuests);
                        }
                    }
                    else if (isMission && !string.IsNullOrEmpty(code) && _missionService != null)
                    {
                        var (mission, _) = await _missionService.GetMissionAsync(code);
                        celebration.ContextSubtitle = mission?.title;
                    }

                    results.Add(celebration);
                }

                // Individual activities first, then quest completion, then the level up it caused, then badges
                if (results.Count == 0)
                {
                    PlayerPrefs.Save();
                    return badgeCelebrations.Count > 0 ? badgeCelebrations : null;
                }
                return results.OrderBy(r => r.LevelUp != null ? 2 : r.IsQuest ? 1 : 0).Concat(badgeCelebrations).ToList();
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
        /// <summary>Name of the completed quest or mission, shown under the title.</summary>
        public string ContextSubtitle { get; set; }
        public string EventId { get; set; }
        public string QuestId { get; set; }
        public string Code { get; set; }
        public bool IsQuest { get; set; }
        public bool IsBadge { get; set; }
        public Quest UnlockedQuest { get; set; }
        /// <summary>Set when the backend raised the user's level in the quest's dimension (the quest completed that level).</summary>
        public DimensionLevelUp LevelUp { get; set; }
        /// <summary>Set when the quest was the last one left in its dimension (every level) and no other quest is offered.</summary>
        public string CompletedDimensionName { get; set; }
    }

    public class DimensionLevelUp
    {
        public string DimensionCode { get; set; }
        public string DimensionName { get; set; }
        /// <summary>The new level (ContentLevel).</summary>
        public string Level { get; set; }
    }
}
