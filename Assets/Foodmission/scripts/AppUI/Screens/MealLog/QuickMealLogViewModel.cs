using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;

namespace eu.foodmission.platform
{
    public class QuickMealCheckItem
    {
        public string Id { get; set; }
        public string Icon { get; set; }
        public string Prompt { get; set; }
        public string EventType { get; set; }
        public bool IsChecked { get; set; }
        public string ActivityCode { get; set; }
        public DirectQuestionType QuestionType { get; set; } = DirectQuestionType.SingleChoice;
        public string[] SwapOptions { get; set; } = Array.Empty<string>();
        public string SelectedSwapOption { get; set; } = "";
    }

    public class QuickMealSection
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public bool IsExpanded { get; set; }
        public List<QuickMealCheckItem> Items { get; set; } = new();
        public int SelectedCount => Items?.Count(i => i.IsChecked) ?? 0;
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
        private readonly ICatalogService _catalogService;
        private readonly IAuthService _authService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedMealType = "LUNCH";

        [ObservableProperty]
        private CatalogItem[] _typeOfMealOptions = Array.Empty<CatalogItem>();

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
        private List<QuickMealSection> _sections = new();

        [ObservableProperty]
        private bool _isSubmitting;

        [ObservableProperty]
        private bool _submitSuccess;

        [ObservableProperty]
        private string _successMessage = "";

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _editingMealLogId;

        [ObservableProperty]
        private MealLog _editingMealLog;

        [ObservableProperty]
        private string _errorMessage = "";

        public event Action<string> ShowToastRequest;

        public QuickMealLogViewModel(
            IStoreService storeService,
            IQuestService questService,
            IEventService eventService,
            ICatalogService catalogService = null,
            IMealLogService mealLogService = null,
            IMealService mealService = null,
            IMissionService missionService = null,
            IChallengeService challengeService = null,
            IAuthService authService = null) : base(storeService)
        {
            _questService = questService;
            _eventService = eventService;
            _catalogService = catalogService;
            _mealLogService = mealLogService;
            _mealService = mealService;
            _missionService = missionService;
            _challengeService = challengeService;
            _authService = authService;

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
                    TypeOfMealOptions = types;
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

                    if (!types.Any(t => t != null && string.Equals(t.code, SelectedMealType, StringComparison.OrdinalIgnoreCase)))
                    {
                        SelectedMealType = types[0].code;
                    }
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

                _missionEvents = new HashSet<string>(StringComparer.Ordinal);
                if (!string.IsNullOrEmpty(currentQuestId) && _questService != null)
                {
                    var (quest, _) = await _questService.GetQuestAsync(currentQuestId);
                    if (quest != null)
                    {
                        ActiveQuestTitle = quest.GetDisplayName();
                        ActiveQuestCode = quest.code ?? "";
                        _missionEvents = MealFacts.MissionMealEvents((quest.items ?? Array.Empty<QuestItem>())
                            .Where(i => i != null && string.Equals(i.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase))
                            .Select(i => i.contentCode));
                    }
                }

                Questions = GetDefaultQuestions();
                _allSections = MealFacts.BuildStandardSections();
                ApplySectionFilter();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadActiveQuestQuestionsAsync error: {ex.Message}");
                Questions = GetDefaultQuestions();
                _allSections = MealFacts.BuildStandardSections();
                ApplySectionFilter();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ── "Only my missions" filter ─────────────────────────

        private List<QuickMealSection> _allSections = new List<QuickMealSection>();
        private HashSet<string> _missionEvents = new HashSet<string>(StringComparer.Ordinal);

        public bool OnlyMissionItems { get; private set; }

        /// <summary>Every section, including items the mission filter hides (checks, submit and hydration use these).</summary>
        private List<QuickMealSection> DataSections => OnlyMissionItems ? _allSections : (Sections ?? _allSections);
        public bool HasMissionItems => _missionEvents.Count > 0;

        public void SetOnlyMissionItems(bool value)
        {
            OnlyMissionItems = value;
            ApplySectionFilter();
        }

        private void ApplySectionFilter()
        {
            Sections = OnlyMissionItems ? MealFacts.FilterByMissionEvents(_allSections, _missionEvents) : _allSections;
            OnPropertyChanged(nameof(HasMissionItems));
        }

        private List<QuickMealCheckItem> GetDefaultQuestions()
        {
            return new List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q_plant_based",
                    Icon = "🌱",
                    Prompt = "@UI:EVENTS_Q_PLANT_BASED",
                    EventType = ClientEventTypes.MealMeatFree,
                    IsChecked = false
                },
                new QuickMealCheckItem
                {
                    Id = "q_veg_legumes",
                    Icon = "🥗",
                    Prompt = "@UI:EVENTS_Q_VEG_LEGUMES",
                    EventType = ClientEventTypes.MealLegumeConsumed,
                    IsChecked = false
                },
                new QuickMealCheckItem
                {
                    Id = "q_local_season",
                    Icon = "🌾",
                    Prompt = "@UI:EVENTS_Q_LOCAL_SEASON",
                    EventType = ClientEventTypes.MealSustainablePlate,
                    IsChecked = false
                }
            };
        }

        public void ToggleSection(string sectionId)
        {
            if (Sections == null) return;
            var sec = Sections.FirstOrDefault(s => s.Id == sectionId);
            if (sec != null)
            {
                sec.IsExpanded = !sec.IsExpanded;
                NotifySectionsChanged();
            }
        }

        public void ToggleQuestion(string id)
        {
            QuickMealCheckItem target = null;
            if (DataSections.Count > 0)
            {
                foreach (var sec in DataSections)
                {
                    var found = sec.Items?.FirstOrDefault(q => q.Id == id);
                    if (found != null) { target = found; break; }
                }
            }
            if (target == null && Questions != null)
            {
                target = Questions.FirstOrDefault(q => q.Id == id);
            }
            if (target == null)
            {
                return;
            }

            MealFacts.Toggle(target, GetAllItems().ToList());
            NotifySectionsChanged();
        }

        private IEnumerable<QuickMealCheckItem> GetAllItems()
        {
            if (DataSections.Count > 0)
            {
                foreach (var sec in DataSections)
                {
                    if (sec.Items == null) continue;
                    foreach (var item in sec.Items)
                    {
                        yield return item;
                    }
                }
            }

            if (Questions != null)
            {
                foreach (var item in Questions)
                {
                    yield return item;
                }
            }
        }

        public void SelectSwapForQuestion(string id, string swapOption)
        {
            QuickMealCheckItem target = null;
            if (DataSections.Count > 0)
            {
                foreach (var sec in DataSections)
                {
                    var found = sec.Items?.FirstOrDefault(q => q.Id == id);
                    if (found != null) { target = found; break; }
                }
            }
            if (target == null && Questions != null)
            {
                target = Questions.FirstOrDefault(q => q.Id == id);
            }
            if (target == null || string.IsNullOrEmpty(swapOption))
            {
                return;
            }

            MealFacts.SelectSwap(target, swapOption);
            NotifySectionsChanged();
        }

        private void NotifySectionsChanged()
        {
            if (Sections != null)
            {
                Sections = new List<QuickMealSection>(Sections);
            }
            if (Questions != null)
            {
                Questions = new List<QuickMealCheckItem>(Questions);
            }
        }

        public async Task LoadForEditAsync(MealLog log)
        {
            if (log == null) return;

            IsEditing = true;
            EditingMealLogId = log.id;
            EditingMealLog = log;

            if (!string.IsNullOrEmpty(log.typeOfMeal))
            {
                SelectedMealType = log.typeOfMeal;
            }

            MealName = log.meal?.name ?? "";

            // Ensure questions and sections are loaded
            await LoadActiveQuestQuestionsAsync();

            // Hydrate questions and sections with existing flags and swaps
            ApplySelectionsFromLog(log);
        }

        public async Task LoadForEditByIdAsync(string mealLogId)
        {
            if (string.IsNullOrEmpty(mealLogId)) return;
            if (_mealLogService == null) return;

            var (log, err) = await _mealLogService.GetLogAsync(mealLogId);
            if (err == null && log != null)
            {
                await LoadForEditAsync(log);
            }
        }

        private void ApplySelectionsFromLog(MealLog log)
        {
            var allItems = new List<QuickMealCheckItem>();
            if (DataSections.Count > 0)
            {
                foreach (var sec in DataSections)
                {
                    if (sec.Items != null) allItems.AddRange(sec.Items);
                }
            }
            else if (Questions != null)
            {
                allItems.AddRange(Questions);
            }

            MealFacts.ApplySelections(allItems, log.flags, log.swaps);

            if (Sections != null && Sections.Count > 0)
            {
                Sections = new List<QuickMealSection>(Sections);
            }
            else if (Questions != null)
            {
                Questions = new List<QuickMealCheckItem>(Questions);
            }
        }

        public void ResetEditState()
        {
            IsEditing = false;
            EditingMealLogId = null;
            EditingMealLog = null;
            MealName = "";
            InitializeDefaultMealType();
            SubmitSuccess = false;
            ErrorMessage = "";
            ErrorDetail = null;
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
            ErrorMessage = "";
            ErrorDetail = null;

            try
            {
                // 1. Separate checked items into flags, swaps, and nutrition events across all sections
                var allChecked = new List<QuickMealCheckItem>();
                if (DataSections.Count > 0)
                {
                    foreach (var sec in DataSections)
                    {
                        if (sec.Items != null)
                        {
                            allChecked.AddRange(sec.Items.Where(i => i.IsChecked));
                        }
                    }
                }
                else if (Questions != null)
                {
                    allChecked.AddRange(Questions.Where(q => q.IsChecked));
                }

                var (flagArray, swapArray) = MealFacts.Build(allChecked);
                var flags = flagArray.ToList();
                var swaps = swapArray.ToList();

                // Selection check: require at least one flag or swap
                if (flags.Count == 0 && swaps.Count == 0)
                {
                    ErrorMessage = "@UI:QUICK_MEAL_LOG_EMPTY_SELECTION";
                    ShowToastRequest?.Invoke(ErrorMessage);
                    return false;
                }

                // 2. Submit meal log: if in Edit Mode, update the existing log via PATCH
                if (IsEditing && !string.IsNullOrEmpty(EditingMealLogId))
                {
                    if (_mealLogService != null)
                    {
                        var updateReq = new UpdateMealLogRequest
                        {
                            typeOfMeal = SelectedMealType,
                            flags = flags.Count > 0 ? flags.ToArray() : null,
                            swaps = swaps.Count > 0 ? swaps.ToArray() : null
                        };

                        var (updatedLog, updateErr) = await _mealLogService.UpdateLogAsync(EditingMealLogId, updateReq);
                        if (updateErr != null)
                        {
                            ErrorDetail = updateErr;
                            return false;
                        }
                    }

                    SubmitSuccess = true;
                    SuccessMessage = "@UI:QUICK_MEAL_LOG_UPDATE_SUCCESS";
                    return true;
                }

                // 2b. Creation mode: submit meal log with its flags and swaps
                if (_mealLogService != null)
                {
                    var logReq = new CreateMealLogRequest
                    {
                        typeOfMeal = SelectedMealType,
                        flags = flags.Count > 0 ? flags.ToArray() : null,
                        swaps = swaps.Count > 0 ? swaps.ToArray() : null,
                        timestamp = DateTime.UtcNow.ToString("o")
                    };
                    var (createdLog, logErr) = await _mealLogService.CreateAsync(logReq);
                    if (logErr != null)
                    {
                        ErrorDetail = logErr;
                        return false;
                    }
                }

                // 3. Sync gamification/wallet so any awarded points/XP from rules are updated in state
                if (_authService != null)
                {
                    try
                    {
                        await _authService.GetGamificationProfileAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[{GetType().Name}] Failed to sync gamification after quick meal log: {ex.Message}");
                    }
                }

                SubmitSuccess = true;
                SuccessMessage = "@UI:QUICK_MEAL_LOG_SUCCESS";
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SubmitQuickMealLogAsync error: {ex.Message}");
                ErrorDetail = new ApiErrorResponse
                {
                    message = ex.Message,
                    statusCode = 500
                };
                return false;
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        public void NavigateToHome()
        {
            RaiseNavigationRequested(Actions.go_to_home);
        }
    }
}
