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
                        _missionEvents = MissionMealEvents((quest.items ?? Array.Empty<QuestItem>())
                            .Where(i => i != null && string.Equals(i.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase))
                            .Select(i => i.contentCode));
                    }
                }

                Questions = GetDefaultQuestions();
                _allSections = BuildStandardSections();
                ApplySectionFilter();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadActiveQuestQuestionsAsync error: {ex.Message}");
                Questions = GetDefaultQuestions();
                _allSections = BuildStandardSections();
                ApplySectionFilter();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private List<QuickMealSection> BuildStandardSections()
        {
            var sections = new List<QuickMealSection>();

            // 1. Hábitos y Plato Sostenible (expandido por defecto)
            var dietItems = new List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q_meat_free",
                    Icon = "🥗",
                    Prompt = "@UI:EVENTS_MEAT_FREE",
                    EventType = ClientEventTypes.MealMeatFree
                },
                new QuickMealCheckItem
                {
                    Id = "q_legumes",
                    Icon = "🫘",
                    Prompt = "@UI:EVENTS_LEGUMES_CONSUMED",
                    EventType = ClientEventTypes.MealLegumeConsumed
                },
                new QuickMealCheckItem
                {
                    Id = "q_vegan",
                    Icon = "🌿",
                    Prompt = "@UI:EVENTS_VEGAN_MEAL",
                    EventType = ClientEventTypes.MealVegan
                },
                new QuickMealCheckItem
                {
                    Id = "q_sustainable_plate",
                    Icon = "🍽️",
                    Prompt = "@UI:EVENTS_SUSTAINABLE_PLATE",
                    EventType = ClientEventTypes.MealSustainablePlate
                },
                new QuickMealCheckItem
                {
                    Id = "q_ancient_grain",
                    Icon = "🌾",
                    Prompt = "@UI:EVENTS_ANCIENT_GRAIN",
                    EventType = ClientEventTypes.MealAncientGrain
                },
                new QuickMealCheckItem
                {
                    Id = "q_alternative_staple",
                    Icon = "🥔",
                    Prompt = "@UI:EVENTS_ALTERNATIVE_STAPLE",
                    EventType = ClientEventTypes.MealAlternativeStaple
                },
                new QuickMealCheckItem
                {
                    Id = "q_meat_consumed",
                    Icon = "🥩",
                    Prompt = "@UI:EVENTS_MEAT_CONSUMED",
                    EventType = ClientEventTypes.MealMeatConsumed
                }
            };
            sections.Add(new QuickMealSection
            {
                Id = "sec_diet",
                Title = "@UI:EVENTS_SECTIONS_DIET",
                Icon = "🌱",
                IsExpanded = false,
                Items = dietItems
            });

            // 2. Sustituciones (Swaps)
            var swapOptions = new[]
            {
                ClientEventTypes.SwapBeefToLegumes,
                ClientEventTypes.SwapBeefToChicken,
                ClientEventTypes.SwapBeefToPork,
                ClientEventTypes.SwapPorkToLegumes,
                ClientEventTypes.SwapPorkToChicken,
                ClientEventTypes.SwapChickenToLegumes,
                ClientEventTypes.SwapProcessedMeatToLegumes,
                ClientEventTypes.SwapReadyMealToHomecooked,
                ClientEventTypes.SwapSugaryDrinkToWater,
                ClientEventTypes.SwapSnackToFruitNuts,
                ClientEventTypes.SwapSugaryCerealToOats
            };

            var swapItems = swapOptions.Select(swap => new QuickMealCheckItem
            {
                Id = $"q_{swap.ToLowerInvariant()}",
                Icon = "🔄",
                Prompt = SwapLocalization.GetSwapLocalizationTag(swap) ?? SwapLocalization.GetSwapDisplayName(swap),
                EventType = swap,
                IsChecked = false
            }).ToList();

            sections.Add(new QuickMealSection
            {
                Id = "sec_swaps",
                Title = "@UI:EVENTS_SECTIONS_SWAPS",
                Icon = "🔄",
                IsExpanded = false,
                Items = swapItems
            });

            // 4. Nutrición y Salud
            var nutritionItems = new List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q_fruit_veg",
                    Icon = "🥦",
                    Prompt = "@UI:EVENTS_FRUIT_VEG_SERVING",
                    EventType = ClientEventTypes.NutritionFruitVegServingAdded
                },
                new QuickMealCheckItem
                {
                    Id = "q_wholegrain",
                    Icon = "🍞",
                    Prompt = "@UI:EVENTS_WHOLEGRAIN",
                    EventType = ClientEventTypes.NutritionWholegrainChosen
                },
                new QuickMealCheckItem
                {
                    Id = "q_high_fibre",
                    Icon = "🌾",
                    Prompt = "@UI:EVENTS_HIGH_FIBRE",
                    EventType = ClientEventTypes.NutritionHighFibreMeal
                },
                new QuickMealCheckItem
                {
                    Id = "q_salt_free",
                    Icon = "🧂",
                    Prompt = "@UI:EVENTS_SALT_FREE",
                    EventType = ClientEventTypes.NutritionSaltFreeTable
                },
                new QuickMealCheckItem
                {
                    Id = "q_healthy_fat",
                    Icon = "🥑",
                    Prompt = "@UI:EVENTS_HEALTHY_FAT",
                    EventType = ClientEventTypes.NutritionHealthyFatChosen
                },
                new QuickMealCheckItem
                {
                    Id = "q_added_sugar_avoided",
                    Icon = "🍬",
                    Prompt = "@UI:EVENTS_ADDED_SUGAR_AVOIDED",
                    EventType = ClientEventTypes.NutritionAddedSugarAvoided
                },
                new QuickMealCheckItem
                {
                    Id = "q_protein_included",
                    Icon = "🍳",
                    Prompt = "@UI:" + MealFlagLabels.KeyFor(ClientEventTypes.NutritionProteinIncluded),
                    EventType = ClientEventTypes.NutritionProteinIncluded
                },
                new QuickMealCheckItem
                {
                    Id = "q_rainbow",
                    Icon = "🌈",
                    Prompt = "@UI:" + MealFlagLabels.KeyFor(ClientEventTypes.NutritionRainbowColoursLogged),
                    EventType = ClientEventTypes.NutritionRainbowColoursLogged
                }
            };
            // 3. Origen y temporada
            sections.Add(new QuickMealSection
            {
                Id = "sec_origin",
                Title = "@UI:EVENTS_SECTIONS_ORIGIN",
                Icon = "🌍",
                IsExpanded = false,
                Items = new List<QuickMealCheckItem>
                {
                    FlagItem("q_seasonal", "🍂", ClientEventTypes.MealSeasonalProduce),
                    FlagItem("q_local", "📍", ClientEventTypes.MealLocalProduce),
                    FlagItem("q_certified", "🏷️", ClientEventTypes.MealCertifiedProduct)
                }
            });

            // 5. Desperdicio
            sections.Add(new QuickMealSection
            {
                Id = "sec_waste",
                Title = "@UI:EVENTS_SECTIONS_WASTE",
                Icon = "♻️",
                IsExpanded = false,
                Items = new List<QuickMealCheckItem>
                {
                    FlagItem("q_half_plate", "🍽️", ClientEventTypes.FoodWasteHalfPlateSaved),
                    FlagItem("q_leftovers", "♻️", ClientEventTypes.FoodWasteFullPlateSaved),
                    FlagItem("q_expired", "📅", ClientEventTypes.FoodWasteExpiredConsumed)
                }
            });

            sections.Add(new QuickMealSection
            {
                Id = "sec_nutrition",
                Title = "@UI:EVENTS_SECTIONS_NUTRITION",
                Icon = "🥗",
                IsExpanded = false,
                Items = nutritionItems
            });

            return sections;
        }

        private static QuickMealCheckItem FlagItem(string id, string icon, string eventType) => new QuickMealCheckItem
        {
            Id = id,
            Icon = icon,
            Prompt = "@UI:" + MealFlagLabels.KeyFor(eventType),
            EventType = eventType
        };

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
            if (!OnlyMissionItems || _missionEvents.Count == 0)
            {
                Sections = _allSections;
            }
            else
            {
                // Filtered sections share the item instances, so checks survive toggling the filter
                Sections = _allSections
                    .Select(s => new QuickMealSection
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Icon = s.Icon,
                        IsExpanded = true,
                        Items = s.Items.Where(i => _missionEvents.Contains(i.EventType ?? string.Empty)).ToList()
                    })
                    .Where(s => s.Items.Count > 0)
                    .ToList();
            }
            OnPropertyChanged(nameof(HasMissionItems));
        }

        private static HashSet<string> MissionMealEvents(IEnumerable<string> missionCodes)
        {
            var events = new HashSet<string>(StringComparer.Ordinal);
            foreach (string code in missionCodes)
            {
                foreach (MissionReportStep step in MissionInteractionCatalog.Get(code).Steps.Where(s => s.Type == MissionStepType.MealReport))
                {
                    if (step.EventType != null)
                    {
                        events.Add(step.EventType);
                    }
                    events.UnionWith(step.Options.Select(o => o.EventType));
                }
            }
            return events;
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
            if (target == null) return;

            target.IsChecked = !target.IsChecked;
            if (target.IsChecked && target.QuestionType == DirectQuestionType.SwapSelector &&
                target.SwapOptions != null && target.SwapOptions.Length > 0 && string.IsNullOrEmpty(target.SelectedSwapOption))
            {
                target.SelectedSwapOption = target.SwapOptions[0];
                target.EventType = target.SwapOptions[0];
            }
            else if (!target.IsChecked && target.QuestionType == DirectQuestionType.SwapSelector)
            {
                target.SelectedSwapOption = null;
                target.EventType = null;
            }

            if (!string.IsNullOrEmpty(target.EventType))
            {
                SyncItemsByEventType(target.EventType, target.IsChecked, target.Id);
            }

            if (target.IsChecked)
            {
                ApplyMeatExclusionRules(target);
            }

            NotifySectionsChanged();
        }

        private void ApplyMeatExclusionRules(QuickMealCheckItem target)
        {
            if (target == null) return;

            if (IsMeatFreeItem(target))
            {
                foreach (var item in GetAllItems())
                {
                    if (IsMeatConsumedItem(item))
                    {
                        item.IsChecked = false;
                    }
                }
            }
            else if (IsMeatConsumedItem(target))
            {
                foreach (var item in GetAllItems())
                {
                    if (IsMeatFreeItem(item))
                    {
                        item.IsChecked = false;
                    }
                }
            }
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

        private static bool IsMeatFreeItem(QuickMealCheckItem item)
        {
            if (item == null) return false;
            return item.EventType == ClientEventTypes.MealMeatFree ||
                   item.EventType == ClientEventTypes.MealVegan ||
                   item.Id == "q_meat_free" ||
                   item.Id == "q_plant_based" ||
                   item.Id == "q_vegan";
        }

        private static bool IsMeatConsumedItem(QuickMealCheckItem item)
        {
            if (item == null) return false;
            return item.EventType == ClientEventTypes.MealMeatConsumed ||
                   item.Id == "q_meat_consumed";
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
            if (target == null || string.IsNullOrEmpty(swapOption)) return;

            if (target.SelectedSwapOption == swapOption && target.IsChecked)
            {
                target.SelectedSwapOption = null;
                target.EventType = null;
                target.IsChecked = false;
            }
            else
            {
                target.SelectedSwapOption = swapOption;
                target.EventType = swapOption;
                target.IsChecked = true;
            }

            NotifySectionsChanged();
        }

        private void SyncItemsByEventType(string eventType, bool isChecked, string sourceId)
        {
            if (string.IsNullOrEmpty(eventType)) return;

            if (DataSections.Count > 0)
            {
                foreach (var sec in DataSections)
                {
                    if (sec.Items == null) continue;
                    foreach (var it in sec.Items)
                    {
                        if (it.Id != sourceId && it.EventType == eventType)
                        {
                            it.IsChecked = isChecked;
                        }
                    }
                }
            }

            if (Questions != null)
            {
                foreach (var it in Questions)
                {
                    if (it.Id != sourceId && it.EventType == eventType)
                    {
                        it.IsChecked = isChecked;
                    }
                }
            }
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
            var flagsSet = new HashSet<string>(log.flags ?? Array.Empty<string>());
            var swapsSet = new HashSet<string>(log.swaps ?? Array.Empty<string>());

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

            foreach (var item in allItems)
            {
                if (item == null) continue;

                // Check flags
                if (!string.IsNullOrEmpty(item.EventType) && flagsSet.Contains(item.EventType))
                {
                    item.IsChecked = true;
                }

                // Check swaps
                if (swapsSet.Count > 0)
                {
                    if (!string.IsNullOrEmpty(item.EventType) && swapsSet.Contains(item.EventType))
                    {
                        item.IsChecked = true;
                        item.SelectedSwapOption = item.EventType;
                    }
                    else if (item.SwapOptions != null && item.SwapOptions.Length > 0)
                    {
                        foreach (var opt in item.SwapOptions)
                        {
                            if (swapsSet.Contains(opt))
                            {
                                item.IsChecked = true;
                                item.SelectedSwapOption = opt;
                                item.EventType = opt;
                                break;
                            }
                        }
                    }
                }
            }

            // Mutual exclusion sanity check
            if (flagsSet.Contains(ClientEventTypes.MealVegan) || flagsSet.Contains(ClientEventTypes.MealMeatFree))
            {
                var meatConsumed = allItems.FirstOrDefault(i => i.EventType == ClientEventTypes.MealMeatConsumed);
                if (meatConsumed != null) meatConsumed.IsChecked = false;
            }

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

                var flags = new List<string>();
                var swaps = new List<string>();

                foreach (var q in allChecked)
                {
                    string ev = q.EventType;
                    if (!string.IsNullOrEmpty(q.SelectedSwapOption))
                    {
                        ev = q.SelectedSwapOption;
                    }

                    if (string.IsNullOrEmpty(ev)) continue;

                    if (ev.StartsWith("SWAP_"))
                    {
                        if (!swaps.Contains(ev))
                        {
                            swaps.Add(ev);
                        }
                    }
                    else if (ev != ClientEventTypes.MealLogged)
                    {
                        if (!flags.Contains(ev))
                        {
                            flags.Add(ev);
                        }
                    }
                }

                // Exclusion rule: MEAL_MEAT_CONSUMED cannot be combined with MEAL_MEAT_FREE or MEAL_VEGAN
                if (flags.Contains(ClientEventTypes.MealVegan) || flags.Contains(ClientEventTypes.MealMeatFree))
                {
                    flags.Remove(ClientEventTypes.MealMeatConsumed);
                }

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
