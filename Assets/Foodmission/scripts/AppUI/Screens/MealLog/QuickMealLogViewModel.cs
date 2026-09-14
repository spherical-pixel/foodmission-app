using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using UnityEngine;

namespace eu.foodmission.platform
{
    public class QuickMealCheckItem
    {
        public string Id { get; set; }
        public string Prompt { get; set; }
        public string EventType { get; set; }
        public bool IsChecked { get; set; }
        public string ActivityCode { get; set; }
        public DirectQuestionType QuestionType { get; set; } = DirectQuestionType.SingleChoice;
        public string[] SwapOptions { get; set; } = Array.Empty<string>();
        public string SelectedSwapOption { get; set; } = "";
    }

    [ObservableObject]
    public partial class QuickMealLogViewModel : ViewModelBase
    {
        private readonly IQuestService _questService;
        private readonly IEventService _eventService;
        private readonly IMealLogService _mealLogService;
        private readonly IMealService _mealService;
        private readonly IMissionService _missionService;
        private readonly IChallengeService _challengeService;
        private readonly IActivityEventMapper _activityEventMapper;
        private readonly ICatalogService _catalogService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedMealType = "LUNCH";

        [ObservableProperty]
        private Dictionary<string, string> _mealTypeLabels = new();

        [ObservableProperty]
        private string _mealName = "";

        [ObservableProperty]
        private string _activeQuestTitle = "";

        [ObservableProperty]
        private string _activeQuestCode = "";

        [ObservableProperty]
        private List<QuickMealCheckItem> _questions = new();

        [ObservableProperty]
        private bool _isSubmitting;

        [ObservableProperty]
        private bool _submitSuccess;

        [ObservableProperty]
        private string _successMessage = "";

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        public QuickMealLogViewModel(
            IStoreService storeService,
            IQuestService questService,
            IEventService eventService,
            IActivityEventMapper activityEventMapper,
            ICatalogService catalogService = null,
            IMealLogService mealLogService = null,
            IMealService mealService = null,
            IMissionService missionService = null,
            IChallengeService challengeService = null) : base(storeService)
        {
            _questService = questService;
            _eventService = eventService;
            _activityEventMapper = activityEventMapper;
            _catalogService = catalogService;
            _mealLogService = mealLogService;
            _mealService = mealService;
            _missionService = missionService;
            _challengeService = challengeService;

            InitializeDefaultMealType();
        }

        private void InitializeDefaultMealType()
        {
            int hour = DateTime.Now.Hour;
            if (hour >= 6 && hour < 12)
            {
                SelectedMealType = "BREAKFAST";
            }
            else if (hour >= 12 && hour < 17)
            {
                SelectedMealType = "LUNCH";
            }
            else if (hour >= 17 && hour < 23)
            {
                SelectedMealType = "DINNER";
            }
            else
            {
                SelectedMealType = "SNACK";
            }
        }

        public async Task LoadCatalogDataAsync()
        {
            if (_catalogService == null) return;
            try
            {
                string lang = _storeService?.GetAppState()?.lang ?? "es";
                var (types, err) = await _catalogService.GetTypeOfMealsAsync(lang);
                if (err == null && types != null && types.Length > 0)
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var t in types)
                    {
                        if (t != null && !string.IsNullOrEmpty(t.code))
                        {
                            string emoji = MealLogHelpers.GetEmojiForTypeOfMeal(t.code);
                            dict[t.code] = string.IsNullOrEmpty(t.label) ? t.code : $"{emoji} {t.label}";
                        }
                    }
                    MealTypeLabels = dict;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadCatalogDataAsync error: {ex.Message}");
            }
        }

        public async Task LoadActiveQuestQuestionsAsync()
        {
            IsLoading = true;
            SubmitSuccess = false;
            SuccessMessage = "";
            ErrorDetail = null;

            try
            {
                await LoadCatalogDataAsync();

                var appState = _storeService?.GetAppState();
                string currentQuestId = appState?.userCurrentQuestId;

                var defaultQuestions = GetDefaultQuestions();

                if (!string.IsNullOrEmpty(currentQuestId) && _questService != null)
                {
                    var (quest, _) = await _questService.GetQuestAsync(currentQuestId);
                    if (quest != null)
                    {
                        ActiveQuestTitle = !string.IsNullOrEmpty(quest.title) ? quest.title : quest.name;
                        ActiveQuestCode = quest.code ?? "";

                        var dynamicQuestions = await BuildQuestionsFromQuestAsync(quest);
                        if (dynamicQuestions != null && dynamicQuestions.Count > 0)
                        {
                            Questions = dynamicQuestions;
                            return;
                        }
                    }
                }

                Questions = defaultQuestions;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadActiveQuestQuestionsAsync error: {ex.Message}");
                Questions = GetDefaultQuestions();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private List<QuickMealCheckItem> GetDefaultQuestions()
        {
            return new List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q_plant_based",
                    Prompt = "🌱 ¿Comida 100% vegetal o con menos carne?",
                    EventType = ClientEventTypes.MealLogged,
                    IsChecked = false
                },
                new QuickMealCheckItem
                {
                    Id = "q_veg_legumes",
                    Prompt = "🥗 ¿Incluyó verduras frescas o legumbres?",
                    EventType = ClientEventTypes.MealLogged,
                    IsChecked = false
                },
                new QuickMealCheckItem
                {
                    Id = "q_local_season",
                    Prompt = "🏡 ¿Ingredientes locales o de temporada?",
                    EventType = ClientEventTypes.MealLogged,
                    IsChecked = false
                },
                new QuickMealCheckItem
                {
                    Id = "q_no_waste",
                    Prompt = "✨ ¿Aprovechaste sobras o no hubo desperdicio?",
                    EventType = ClientEventTypes.FoodWasteReported,
                    IsChecked = false
                }
            };
        }

        private async Task<List<QuickMealCheckItem>> BuildQuestionsFromQuestAsync(Quest quest)
        {
            var list = new List<QuickMealCheckItem>();
            if (quest?.items == null) return list;

            foreach (var it in quest.items)
            {
                if (it == null) continue;
                string cType = it.contentType ?? "";
                bool isMission = string.Equals(cType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase);
                bool isChallenge = string.Equals(cType, "CHALLENGE", StringComparison.OrdinalIgnoreCase);

                if (isMission || isChallenge)
                {
                    var mapping = isMission
                        ? _activityEventMapper?.GetMissionMapping(it.contentCode)
                        : _activityEventMapper?.GetChallengeMapping(it.contentCode);

                    string prompt = mapping?.DirectQuestionPrompt;

                    if (string.IsNullOrEmpty(prompt) && !string.IsNullOrEmpty(it.label))
                    {
                        prompt = it.label;
                    }

                    if (string.IsNullOrEmpty(prompt))
                    {
                        if (isMission && _missionService != null)
                        {
                            var (m, _) = await _missionService.GetMissionAsync(it.contentCode);
                            if (m != null)
                            {
                                prompt = !string.IsNullOrEmpty(m.title) ? $"🎯 ¿{m.title}?" : m.goal;
                            }
                        }
                        else if (isChallenge && _challengeService != null)
                        {
                            var (ch, _) = await _challengeService.GetChallengeAsync(it.contentCode);
                            if (ch != null)
                            {
                                prompt = !string.IsNullOrEmpty(ch.title) ? $"🏆 ¿{ch.title}?" : ch.task;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(prompt))
                    {
                        prompt = !string.IsNullOrEmpty(it.contentCode) ? $"✓ ¿Completado ({it.contentCode})?" : "✓ ¿Completado?";
                    }

                    string[] swapOpts = mapping?.SwapOptions ?? Array.Empty<string>();
                    string eventType = (swapOpts.Length > 0)
                        ? null
                        : ((mapping?.TargetEventTypes != null && mapping.TargetEventTypes.Length > 0)
                            ? mapping.TargetEventTypes[0]
                            : ClientEventTypes.MealLogged);

                    list.Add(new QuickMealCheckItem
                    {
                        Id = it.id ?? it.contentCode,
                        Prompt = prompt,
                        EventType = eventType,
                        ActivityCode = it.contentCode,
                        IsChecked = false,
                        QuestionType = mapping?.QuestionType ?? DirectQuestionType.SingleChoice,
                        SwapOptions = swapOpts,
                        SelectedSwapOption = null
                    });
                }
            }

            if (list.Count == 0)
            {
                return GetDefaultQuestions();
            }

            return list;
        }

        public void ToggleQuestion(string id)
        {
            if (Questions == null) return;
            var item = Questions.FirstOrDefault(q => q.Id == id);
            if (item != null)
            {
                item.IsChecked = !item.IsChecked;
                if (item.IsChecked && item.QuestionType == DirectQuestionType.SwapSelector &&
                    item.SwapOptions != null && item.SwapOptions.Length > 0 && string.IsNullOrEmpty(item.SelectedSwapOption))
                {
                    item.SelectedSwapOption = item.SwapOptions[0];
                    item.EventType = item.SwapOptions[0];
                }
                else if (!item.IsChecked && item.QuestionType == DirectQuestionType.SwapSelector)
                {
                    item.SelectedSwapOption = null;
                    item.EventType = null;
                }
                // Re-assign to trigger binding / notify
                Questions = new List<QuickMealCheckItem>(Questions);
            }
        }

        public void SelectSwapForQuestion(string id, string swapOption)
        {
            if (Questions == null || string.IsNullOrEmpty(swapOption)) return;
            var item = Questions.FirstOrDefault(q => q.Id == id);
            if (item != null)
            {
                if (item.SelectedSwapOption == swapOption && item.IsChecked)
                {
                    item.SelectedSwapOption = null;
                    item.EventType = null;
                    item.IsChecked = false;
                }
                else
                {
                    item.SelectedSwapOption = swapOption;
                    item.EventType = swapOption;
                    item.IsChecked = true;
                }
                Questions = new List<QuickMealCheckItem>(Questions);
            }
        }

        public void SetMealType(string mealType)
        {
            SelectedMealType = mealType;
        }

        public async Task<bool> SubmitQuickMealLogAsync()
        {
            if (_isSubmitting) return false;
            IsSubmitting = true;
            SubmitSuccess = false;
            ErrorDetail = null;

            try
            {
                string mealName = !string.IsNullOrEmpty(MealName) ? MealName : $"Comida ({SelectedMealType})";

                // 1. If meal service is available, create meal & meal log
                if (_mealService != null && _mealLogService != null)
                {
                    string course = (SelectedMealType == "SNACK") ? "SIDE_SNACK" : "MAIN_DISH";
                    var mealReq = new CreateMealRequest
                    {
                        name = mealName,
                        mealCourse = course
                    };
                    var (createdMeal, mealErr) = await _mealService.CreateMealAsync(mealReq);
                    if (createdMeal != null)
                    {
                        var logReq = new CreateMealLogRequest
                        {
                            mealId = createdMeal.id,
                            typeOfMeal = SelectedMealType,
                            timestamp = DateTime.UtcNow.ToString("o")
                        };
                        await _mealLogService.CreateAsync(logReq);
                    }
                }

                // 2. Emit client event for each checked affirmative answer
                if (_eventService != null && Questions != null)
                {
                    int emittedEventsCount = 0;
                    foreach (var q in Questions)
                    {
                        if (q.IsChecked)
                        {
                            var req = new CreateClientEventRequest
                            {
                                eventType = q.EventType ?? ClientEventTypes.MealLogged,
                                metadata = new
                                {
                                    questionId = q.Id,
                                    activityCode = q.ActivityCode,
                                    questCode = ActiveQuestCode,
                                    mealType = SelectedMealType,
                                    sessionId = _eventService.CurrentSessionId,
                                    reportedVia = "quick_meal_log"
                                }
                            };
                            await _eventService.RecordClientEventAsync(req);
                            emittedEventsCount++;
                        }
                    }

                    // Always record at least standard MealLogged if no specific questions were checked
                    if (emittedEventsCount == 0)
                    {
                        var standardReq = new CreateClientEventRequest
                        {
                            eventType = ClientEventTypes.MealLogged,
                            metadata = new
                            {
                                mealType = SelectedMealType,
                                sessionId = _eventService.CurrentSessionId,
                                reportedVia = "quick_meal_log"
                            }
                        };
                        await _eventService.RecordClientEventAsync(standardReq);
                    }
                }

                SubmitSuccess = true;
                SuccessMessage = "¡Comida y progresos registrados con éxito!";
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SubmitQuickMealLogAsync error: {ex.Message}");
                return false;
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}
