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
        private readonly IActivityEventMapper _activityEventMapper;
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

        public QuickMealLogViewModel(
            IStoreService storeService,
            IQuestService questService,
            IEventService eventService,
            IActivityEventMapper activityEventMapper,
            ICatalogService catalogService = null,
            IMealLogService mealLogService = null,
            IMealService mealService = null,
            IMissionService missionService = null,
            IChallengeService challengeService = null,
            IAuthService authService = null) : base(storeService)
        {
            _questService = questService;
            _eventService = eventService;
            _activityEventMapper = activityEventMapper;
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

                if (!string.IsNullOrEmpty(currentQuestId) && _questService != null)
                {
                    var (quest, _) = await _questService.GetQuestAsync(currentQuestId);
                    if (quest != null)
                    {
                        ActiveQuestTitle = !string.IsNullOrEmpty(quest.title) ? quest.title : quest.name;
                        ActiveQuestCode = quest.code ?? "";
                    }
                }

                Questions = GetDefaultQuestions();
                Sections = BuildStandardSections();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadActiveQuestQuestionsAsync error: {ex.Message}");
                Questions = GetDefaultQuestions();
                Sections = BuildStandardSections();
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
                    Prompt = "🥗 Comida sin carne / vegetariana",
                    EventType = ClientEventTypes.MealMeatFree
                },
                new QuickMealCheckItem
                {
                    Id = "q_legumes",
                    Prompt = "🫘 Ración de legumbres consumida",
                    EventType = ClientEventTypes.MealLegumeConsumed
                },
                new QuickMealCheckItem
                {
                    Id = "q_vegan",
                    Prompt = "🌿 Comida 100% vegetal / vegana",
                    EventType = ClientEventTypes.MealVegan
                },
                new QuickMealCheckItem
                {
                    Id = "q_sustainable_plate",
                    Prompt = "🍽️ Plato equilibrado (½ verdura, ¼ proteína, ¼ carbohidratos)",
                    EventType = ClientEventTypes.MealSustainablePlate
                },
                new QuickMealCheckItem
                {
                    Id = "q_ancient_grain",
                    Prompt = "🌾 Cereales tradicionales o integrales",
                    EventType = ClientEventTypes.MealAncientGrain
                },
                new QuickMealCheckItem
                {
                    Id = "q_alternative_staple",
                    Prompt = "🥔 Tubérculo o alimento básico alternativo",
                    EventType = ClientEventTypes.MealAlternativeStaple
                },
                new QuickMealCheckItem
                {
                    Id = "q_meat_consumed",
                    Prompt = "🥩 Ración de carne contabilizada",
                    EventType = ClientEventTypes.MealMeatConsumed
                }
            };
            sections.Add(new QuickMealSection
            {
                Id = "sec_diet",
                Title = "Hábitos y Plato Sostenible",
                Icon = "🌱",
                IsExpanded = true,
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
                Prompt = ActivityEventMapper.GetSwapDisplayName(swap),
                EventType = swap,
                IsChecked = false
            }).ToList();

            sections.Add(new QuickMealSection
            {
                Id = "sec_swaps",
                Title = "Sustituciones de Alimentos (Swaps)",
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
                    Prompt = "🥦 Ración de verdura fresca o ensalada",
                    EventType = ClientEventTypes.NutritionFruitVegServingAdded
                },
                new QuickMealCheckItem
                {
                    Id = "q_wholegrain",
                    Prompt = "🍞 Pan o cereales 100% integrales",
                    EventType = ClientEventTypes.NutritionWholegrainChosen
                },
                new QuickMealCheckItem
                {
                    Id = "q_high_fibre",
                    Prompt = "🌾 Comida rica en fibra vegetal",
                    EventType = ClientEventTypes.NutritionHighFibreMeal
                },
                new QuickMealCheckItem
                {
                    Id = "q_salt_free",
                    Prompt = "🧂 Sin sal añadida en la mesa",
                    EventType = ClientEventTypes.NutritionSaltFreeTable
                },
                new QuickMealCheckItem
                {
                    Id = "q_healthy_fat",
                    Prompt = "🥑 Grasa saludable (aceite de oliva, frutos secos)",
                    EventType = ClientEventTypes.NutritionHealthyFatChosen
                },
                new QuickMealCheckItem
                {
                    Id = "q_added_sugar_avoided",
                    Prompt = "🍬 Sin azúcares añadidos ni dulces industriales",
                    EventType = ClientEventTypes.NutritionAddedSugarAvoided
                }
            };
            sections.Add(new QuickMealSection
            {
                Id = "sec_nutrition",
                Title = "Nutrición y Salud",
                Icon = "🥗",
                IsExpanded = false,
                Items = nutritionItems
            });

            return sections;
        }

        private List<QuickMealCheckItem> GetDefaultQuestions()
        {
            return new List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q_plant_based",
                    Prompt = "🌱 ¿Comida 100% vegetal o sin carne?",
                    EventType = ClientEventTypes.MealMeatFree,
                    IsChecked = false
                },
                new QuickMealCheckItem
                {
                    Id = "q_veg_legumes",
                    Prompt = "🥗 ¿Incluyó verduras frescas o legumbres?",
                    EventType = ClientEventTypes.MealLegumeConsumed,
                    IsChecked = false
                },
                new QuickMealCheckItem
                {
                    Id = "q_local_season",
                    Prompt = "🌾 ¿Plato sostenible o cereal alternativo?",
                    EventType = ClientEventTypes.MealSustainablePlate,
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
            if (Sections != null)
            {
                foreach (var sec in Sections)
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

            NotifySectionsChanged();
        }

        public void SelectSwapForQuestion(string id, string swapOption)
        {
            QuickMealCheckItem target = null;
            if (Sections != null)
            {
                foreach (var sec in Sections)
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

            if (Sections != null)
            {
                foreach (var sec in Sections)
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
                // 1. Separate checked items into flags and swaps across all sections
                var allChecked = new List<QuickMealCheckItem>();
                if (Sections != null && Sections.Count > 0)
                {
                    foreach (var sec in Sections)
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
                        if (!swaps.Contains(ev)) swaps.Add(ev);
                    }
                    else if ((ev.StartsWith("MEAL_") || ev.StartsWith("NUTRITION_")) && ev != ClientEventTypes.MealLogged)
                    {
                        if (!flags.Contains(ev)) flags.Add(ev);
                    }
                }

                // Exclusion rule: MEAL_MEAT_CONSUMED cannot be combined with MEAL_MEAT_FREE or MEAL_VEGAN
                if (flags.Contains(ClientEventTypes.MealVegan) || flags.Contains(ClientEventTypes.MealMeatFree))
                {
                    flags.Remove(ClientEventTypes.MealMeatConsumed);
                }

                // Backend requires flags when mealId is omitted
                if (flags.Count == 0)
                {
                    if (swaps.Any(s => s.Contains("TO_LEGUMES") || s.Contains("TO_CHICKEN") || s.Contains("TO_PORK")))
                    {
                        flags.Add(ClientEventTypes.MealMeatFree);
                    }
                    else
                    {
                        flags.Add(ClientEventTypes.MealSustainablePlate);
                    }
                }

                // 2. Submit meal log directly with flags and swaps
                if (_mealLogService != null)
                {
                    var logReq = new CreateMealLogRequest
                    {
                        typeOfMeal = SelectedMealType,
                        flags = flags.ToArray(),
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
