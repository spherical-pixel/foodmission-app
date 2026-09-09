using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;

namespace eu.foodmission.platform
{
    public static class QuestFilterLevel
    {
        public const string All = "ALL";
        public const string Beginner = QuestLevel.Beginner;
        public const string Intermediate = QuestLevel.Intermediate;
        public const string Advanced = QuestLevel.Advanced;

        public static readonly string[] Options = { All, Beginner, Intermediate, Advanced };
    }

    public static class QuestFilterStatus
    {
        public const string All = "ALL";
        public const string Pending = "PENDING";
        public const string Completed = "COMPLETED";

        public static readonly string[] Options = { All, Pending, Completed };
    }

    public class QuestDisplayItem
    {
        public Quest Quest { get; set; }
        public bool IsCompleted { get; set; }
        public float Progress { get; set; }
    }

    public class QuestDisplayGroup
    {
        public Dimension Dimension { get; set; }
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public bool IsExpanded { get; set; } = false;
        public List<QuestDisplayItem> Quests { get; set; } = new List<QuestDisplayItem>();
    }

    [ObservableObject]
    public partial class QuestsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedLevel = QuestFilterLevel.All;

        [ObservableProperty]
        private string _selectedStatus = QuestFilterStatus.All;

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private IReadOnlyList<QuestDisplayGroup> _displayGroups = new List<QuestDisplayGroup>();

        [ObservableProperty]
        private int _totalQuestsCount;

        [ObservableProperty]
        private int _completedQuestsCount;

        private readonly IQuestService _questService;
        private readonly IDimensionService _dimensionService;
        private readonly HashSet<string> _expandedDimensionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private Quest[] _rawQuests = Array.Empty<Quest>();
        private QuestProgress[] _rawProgress = Array.Empty<QuestProgress>();
        private string _lastLoadedLang;

        public event Action<Quest> OnQuestSelected;

        public QuestsViewModel(
            IStoreService storeService,
            IQuestService questService,
            IDimensionService dimensionService) : base(storeService)
        {
            _questService = questService;
            _dimensionService = dimensionService;

            if (_store != null)
            {
                _lastLoadedLang = _storeService?.GetAppState()?.lang;
                _storeSubscription = _store.Subscribe(
                    state => state.lang,
                    OnLanguageChanged
                );
            }
        }

        private void OnLanguageChanged(string newLang)
        {
            if (!string.IsNullOrEmpty(newLang) && !string.Equals(newLang, _lastLoadedLang, StringComparison.OrdinalIgnoreCase))
            {
                _lastLoadedLang = newLang;
                _ = LoadDataAsync(forceRefresh: true);
            }
        }

        public async Task LoadDataAsync(bool forceRefresh = false)
        {
            if (_isLoading) return;

            IsLoading = true;
            ErrorMessage = null;
            ErrorDetail = null;

            try
            {
                // Ensure dimensions are preloaded
                if (_dimensionService != null && (!_dimensionService.IsLoaded || forceRefresh))
                {
                    await _dimensionService.PreloadAsync(force: forceRefresh);
                }

                Task<(Quest[] Result, ApiErrorResponse Error)> questsTask =
                    _questService != null
                        ? _questService.GetQuestsAsync()
                        : Task.FromResult<(Quest[], ApiErrorResponse)>((null, null));

                Task<(QuestProgress[] Result, ApiErrorResponse Error)> progressTask =
                    _questService != null
                        ? _questService.GetUserProgressListAsync()
                        : Task.FromResult<(QuestProgress[], ApiErrorResponse)>((null, null));

                await Task.WhenAll(questsTask, progressTask);

                var questsResponse = await questsTask;
                var progressResponse = await progressTask;

                if (questsResponse.Error != null)
                {
                    ErrorDetail = questsResponse.Error;
                    ErrorMessage = questsResponse.Error.message;
                    IsLoading = false;
                    return;
                }

                _rawQuests = questsResponse.Result ?? Array.Empty<Quest>();
                _rawProgress = progressResponse.Result ?? Array.Empty<QuestProgress>();

                RebuildDisplayGroups();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadDataAsync failed: {ex.Message}");
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void SetLevelFilter(string level)
        {
            if (string.IsNullOrEmpty(level))
                level = QuestFilterLevel.All;

            if (_selectedLevel != level)
            {
                SelectedLevel = level;
                RebuildDisplayGroups();
            }
        }

        public void SetStatusFilter(string status)
        {
            if (string.IsNullOrEmpty(status))
                status = QuestFilterStatus.All;

            if (_selectedStatus != status)
            {
                SelectedStatus = status;
                RebuildDisplayGroups();
            }
        }

        public void ToggleDimensionExpanded(string dimensionCodeOrId)
        {
            if (string.IsNullOrEmpty(dimensionCodeOrId)) return;

            bool wasExpanded = _expandedDimensionCodes.Contains(dimensionCodeOrId);
            if (wasExpanded)
            {
                _expandedDimensionCodes.Remove(dimensionCodeOrId);
            }
            else
            {
                _expandedDimensionCodes.Add(dimensionCodeOrId);
            }

            if (_displayGroups != null)
            {
                foreach (var g in _displayGroups)
                {
                    string code = g.Dimension?.code ?? g.Dimension?.id;
                    if (string.Equals(code, dimensionCodeOrId, StringComparison.OrdinalIgnoreCase))
                    {
                        g.IsExpanded = !wasExpanded;
                        break;
                    }
                }
            }
        }

        public void OpenQuest(Quest quest)
        {
            if (quest == null) return;
            OnQuestSelected?.Invoke(quest);
            Debug.Log($"[{GetType().Name}] OpenQuest clicked: {quest.code} - {quest.title ?? quest.name}");

            var args = new List<Argument>();
            if (!string.IsNullOrEmpty(quest.code))
                args.Add(new Argument("code", quest.code));
            if (!string.IsNullOrEmpty(quest.id))
                args.Add(new Argument("id", quest.id));

            RaiseNavigationRequested(Actions.open_quest, args.ToArray());
        }

        public void SetRawDataForTesting(Quest[] quests, QuestProgress[] progress)
        {
            _rawQuests = quests ?? Array.Empty<Quest>();
            _rawProgress = progress ?? Array.Empty<QuestProgress>();
            RebuildDisplayGroups();
        }

        private void RebuildDisplayGroups()
        {
            if (_rawQuests == null || _rawQuests.Length == 0)
            {
                DisplayGroups = new List<QuestDisplayGroup>();
                TotalQuestsCount = 0;
                CompletedQuestsCount = 0;
                return;
            }

            // Map progress by questId and questCode
            var progressById = new Dictionary<string, QuestProgress>(StringComparer.OrdinalIgnoreCase);
            var progressByCode = new Dictionary<string, QuestProgress>(StringComparer.OrdinalIgnoreCase);

            if (_rawProgress != null)
            {
                foreach (var p in _rawProgress)
                {
                    if (p == null) continue;
                    if (!string.IsNullOrEmpty(p.questId))
                        progressById[p.questId] = p;
                    if (!string.IsNullOrEmpty(p.questCode))
                        progressByCode[p.questCode] = p;
                }
            }

            var displayItems = new List<QuestDisplayItem>();
            int totalMatchingLevel = 0;
            int totalCompletedMatchingLevel = 0;

            foreach (var q in _rawQuests)
            {
                if (q == null) continue;

                // Level Filter
                if (!string.Equals(_selectedLevel, QuestFilterLevel.All, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(q.level, _selectedLevel, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                totalMatchingLevel++;

                bool isCompleted = false;
                float progressVal = 0f;

                QuestProgress prog = null;
                if (!string.IsNullOrEmpty(q.id) && progressById.TryGetValue(q.id, out var progFoundId))
                {
                    prog = progFoundId;
                }
                else if (!string.IsNullOrEmpty(q.code) && progressByCode.TryGetValue(q.code, out var progFoundCode))
                {
                    prog = progFoundCode;
                }

                if (prog != null)
                {
                    isCompleted = prog.completed || prog.progress >= 100f;
                    progressVal = prog.progress;
                }

                if (isCompleted)
                {
                    totalCompletedMatchingLevel++;
                }

                // Status Filter
                if (string.Equals(_selectedStatus, QuestFilterStatus.Completed, StringComparison.OrdinalIgnoreCase) && !isCompleted)
                {
                    continue;
                }
                if (string.Equals(_selectedStatus, QuestFilterStatus.Pending, StringComparison.OrdinalIgnoreCase) && isCompleted)
                {
                    continue;
                }

                displayItems.Add(new QuestDisplayItem
                {
                    Quest = q,
                    IsCompleted = isCompleted,
                    Progress = progressVal
                });
            }

            TotalQuestsCount = totalMatchingLevel;
            CompletedQuestsCount = totalCompletedMatchingLevel;

            if (displayItems.Count == 0)
            {
                DisplayGroups = new List<QuestDisplayGroup>();
                return;
            }

            // Build hierarchical display groups using IDimensionService
            var allDimensions = _dimensionService?.GetAllDimensions();
            var groups = new List<QuestDisplayGroup>();
            var assignedItems = new HashSet<QuestDisplayItem>();

            if (allDimensions != null && allDimensions.Count > 0)
            {
                foreach (var dim in allDimensions)
                {
                    if (dim == null) continue;

                    // Find all items belonging to this dimension
                    var dimItems = displayItems.Where(item =>
                    {
                        if (string.Equals(item.Quest?.dimensionId, dim.id, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(item.Quest?.dimensionId, dim.code, StringComparison.OrdinalIgnoreCase))
                            return true;

                        var resolvedDim = _dimensionService.GetDimension(item.Quest?.dimensionId);
                        if (resolvedDim != null && (string.Equals(resolvedDim.id, dim.id, StringComparison.OrdinalIgnoreCase) ||
                                                    string.Equals(resolvedDim.code, dim.code, StringComparison.OrdinalIgnoreCase)))
                            return true;

                        return false;
                    }).ToList();

                    if (dimItems.Count == 0) continue;

                    foreach (var it in dimItems)
                        assignedItems.Add(it);

                    dimItems.Sort(CompareQuestDisplayItems);

                    int dimTotal = dimItems.Count;
                    int dimCompleted = dimItems.Count(it => it.IsCompleted);

                    bool isExpanded = _expandedDimensionCodes.Contains(dim.code) ||
                                      (!string.IsNullOrEmpty(dim.id) && _expandedDimensionCodes.Contains(dim.id));

                    groups.Add(new QuestDisplayGroup
                    {
                        Dimension = dim,
                        TotalCount = dimTotal,
                        CompletedCount = dimCompleted,
                        IsExpanded = isExpanded,
                        Quests = dimItems
                    });
                }
            }

            // Fallback for any unassigned items (or if dimensions not loaded)
            var leftoverItems = displayItems.Where(it => !assignedItems.Contains(it)).ToList();
            if (leftoverItems.Count > 0)
            {
                leftoverItems.Sort(CompareQuestDisplayItems);
                groups.Add(new QuestDisplayGroup
                {
                    Dimension = new Dimension { id = "DEFAULT", code = "ALL_QUESTS", name = "Quests" },
                    TotalCount = leftoverItems.Count,
                    CompletedCount = leftoverItems.Count(i => i.IsCompleted),
                    IsExpanded = true,
                    Quests = leftoverItems
                });
            }

            DisplayGroups = groups;
        }

        private static int GetLevelOrder(string level)
        {
            if (string.Equals(level, QuestLevel.Beginner, StringComparison.OrdinalIgnoreCase))
                return 1;
            if (string.Equals(level, QuestLevel.Intermediate, StringComparison.OrdinalIgnoreCase))
                return 2;
            if (string.Equals(level, QuestLevel.Advanced, StringComparison.OrdinalIgnoreCase))
                return 3;
            return 4;
        }

        private static int CompareQuestDisplayItems(QuestDisplayItem a, QuestDisplayItem b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int levelA = GetLevelOrder(a.Quest?.level);
            int levelB = GetLevelOrder(b.Quest?.level);
            if (levelA != levelB)
                return levelA.CompareTo(levelB);

            return string.Compare(a.Quest?.code, b.Quest?.code, StringComparison.OrdinalIgnoreCase);
        }
    }
}
