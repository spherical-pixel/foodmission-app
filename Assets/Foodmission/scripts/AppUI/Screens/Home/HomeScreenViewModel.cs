using System.Linq;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace eu.foodmission.platform
{
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
        private readonly ICatalogService _catalogService;
        private readonly IQuestService _questService;
        private readonly IQuizService _quizService;
        private readonly IMissionService _missionService;
        private readonly IChallengeService _challengeService;

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
            IChallengeService challengeService = null) : base(storeService)
        {
            _notificationService = notificationService;
            _legalService = legalService ?? App.current?.services?.GetService<ILegalService>();
            _pilotSurveyService = pilotSurveyService ?? App.current?.services?.GetService<IPilotSurveyService>();
            _catalogService = catalogService ?? App.current?.services?.GetService<ICatalogService>();
            _questService = questService ?? App.current?.services?.GetService<IQuestService>();
            _quizService = quizService ?? App.current?.services?.GetService<IQuizService>();
            _missionService = missionService ?? App.current?.services?.GetService<IMissionService>();
            _challengeService = challengeService ?? App.current?.services?.GetService<IChallengeService>();

            // Get initial state
            AppState state = _storeService?.GetAppState();

            // Subscribe to user state changes
            if (_store != null)
            {
                _storeSubscription = _store.Subscribe(
                    SelectUserState,
                    OnUserStateChanged
                );
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

        public void NavigateToOnboardingProfile()
        {
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.register_to_onboarding);
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

                CurrentQuestTitle = !string.IsNullOrEmpty(quest.title) ? quest.title : (!string.IsNullOrEmpty(quest.name) ? quest.name : quest.code);
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

                    await System.Threading.Tasks.Task.WhenAll(progressTask, quizProgressTask, missionProgressTask, challengeProgressTask);

                    var (progress, _) = await progressTask;
                    var (quizProgress, _) = await quizProgressTask;
                    var (missionProgress, _) = await missionProgressTask;
                    var (challengeProgress, _) = await challengeProgressTask;

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
                            if (mp.completed || mp.progress >= 100f)
                            {
                                if (!string.IsNullOrEmpty(mp.missionId)) completedMissionCodes.Add(mp.missionId);
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
                                if (!string.IsNullOrEmpty(cp.challengeId)) completedChallengeCodes.Add(cp.challengeId);
                            }
                        }
                    }

                    bool hasAnySubProgress = completedQuizCodes.Count > 0 || completedMissionCodes.Count > 0 || completedChallengeCodes.Count > 0;
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
                            else if (string.Equals(cType, QuestContentType.Mission, System.StringComparison.OrdinalIgnoreCase))
                            {
                                isItemCompleted = completedMissionCodes.Contains(code);
                            }
                            else if (string.Equals(cType, "CHALLENGE", System.StringComparison.OrdinalIgnoreCase))
                            {
                                isItemCompleted = completedChallengeCodes.Contains(code);
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

        public void NavigateToQuests()
        {
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_quests);
        }

        public void NavigateToQuickMealLog()
        {
            RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.open_quick_meal_log);
        }

        public void SetCurrentQuestForTesting(string title, string code, string id, bool[] states)
        {
            CurrentQuestTitle = title;
            CurrentQuestCode = code;
            CurrentQuestId = id;
            CurrentQuestActivityStates = states ?? System.Array.Empty<bool>();
            HasActiveQuest = !string.IsNullOrEmpty(title);
        }
    }
}
