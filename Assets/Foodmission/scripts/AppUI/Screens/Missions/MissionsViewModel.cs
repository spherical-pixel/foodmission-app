using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using UnityEngine;

namespace eu.foodmission.platform
{
    public static class MissionFilterLevel
    {
        public const string All = "ALL";
        public const string Beginner = MissionLevel.Beginner;
        public const string Intermediate = MissionLevel.Intermediate;
        public const string Advanced = MissionLevel.Advanced;

        public static readonly string[] Options = { All, Beginner, Intermediate, Advanced };
    }

    public static class MissionFilterStatus
    {
        public const string All = "ALL";
        public const string Pending = "PENDING";
        public const string Completed = "COMPLETED";

        public static readonly string[] Options = { All, Pending, Completed };
    }

    public class MissionDisplayItem
    {
        public Mission Mission { get; set; }
        public bool IsCompleted { get; set; }
        public float Progress { get; set; }
    }

    public class MissionDisplayGroup
    {
        public Dimension Dimension { get; set; }
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public bool IsExpanded { get; set; } = false;
        public List<MissionDisplayItem> Missions { get; set; } = new List<MissionDisplayItem>();
    }

    [ObservableObject]
    public partial class MissionsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedLevel = MissionFilterLevel.All;

        [ObservableProperty]
        private string _selectedStatus = MissionFilterStatus.All;

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private IReadOnlyList<MissionDisplayGroup> _displayGroups = new List<MissionDisplayGroup>();

        [ObservableProperty]
        private int _totalMissionsCount;

        [ObservableProperty]
        private int _completedMissionsCount;

        private readonly IMissionService _missionService;
        private readonly IDimensionService _dimensionService;
        private readonly HashSet<string> _expandedDimensionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private Mission[] _rawMissions = Array.Empty<Mission>();
        private MissionProgress[] _rawProgress = Array.Empty<MissionProgress>();
        private string _lastLoadedLang;

        public event Action<Mission> OnMissionSelected;

        public MissionsViewModel(
            IStoreService storeService,
            IMissionService missionService,
            IDimensionService dimensionService) : base(storeService)
        {
            _missionService = missionService;
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

                Task<(Mission[] Result, ApiErrorResponse Error)> missionsTask =
                    _missionService != null
                        ? _missionService.GetMissionsAsync()
                        : Task.FromResult<(Mission[], ApiErrorResponse)>((null, null));

                Task<(MissionProgress[] Result, ApiErrorResponse Error)> progressTask =
                    _missionService != null
                        ? _missionService.GetUserProgressListAsync()
                        : Task.FromResult<(MissionProgress[], ApiErrorResponse)>((null, null));

                await Task.WhenAll(missionsTask, progressTask);

                var missionsResponse = await missionsTask;
                var progressResponse = await progressTask;

                if (missionsResponse.Error != null)
                {
                    ErrorDetail = missionsResponse.Error;
                    ErrorMessage = missionsResponse.Error.message;
                    IsLoading = false;
                    return;
                }

                _rawMissions = missionsResponse.Result ?? Array.Empty<Mission>();
                _rawProgress = progressResponse.Result ?? Array.Empty<MissionProgress>();

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
                level = MissionFilterLevel.All;

            if (_selectedLevel != level)
            {
                SelectedLevel = level;
                RebuildDisplayGroups();
            }
        }

        public void SetStatusFilter(string status)
        {
            if (string.IsNullOrEmpty(status))
                status = MissionFilterStatus.All;

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

        public void OpenMission(Mission mission)
        {
            if (mission == null) return;
            OnMissionSelected?.Invoke(mission);
            Debug.Log($"[{GetType().Name}] OpenMission clicked: {mission.code} - {mission.title}");
        }

        public void SetRawDataForTesting(Mission[] missions, MissionProgress[] progress)
        {
            _rawMissions = missions ?? Array.Empty<Mission>();
            _rawProgress = progress ?? Array.Empty<MissionProgress>();
            RebuildDisplayGroups();
        }

        private void RebuildDisplayGroups()
        {
            if (_rawMissions == null || _rawMissions.Length == 0)
            {
                DisplayGroups = new List<MissionDisplayGroup>();
                TotalMissionsCount = 0;
                CompletedMissionsCount = 0;
                return;
            }

            // Map progress by missionId
            var progressMap = new Dictionary<string, MissionProgress>(StringComparer.OrdinalIgnoreCase);
            if (_rawProgress != null)
            {
                foreach (var p in _rawProgress)
                {
                    if (p == null) continue;
                    if (!string.IsNullOrEmpty(p.missionId))
                        progressMap[p.missionId] = p;
                }
            }

            var displayItems = new List<MissionDisplayItem>();
            int totalMatchingLevel = 0;
            int totalCompletedMatchingLevel = 0;

            foreach (var m in _rawMissions)
            {
                if (m == null) continue;

                // Level Filter
                if (!string.Equals(_selectedLevel, MissionFilterLevel.All, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(m.level, _selectedLevel, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                totalMatchingLevel++;

                bool isCompleted = false;
                float progressVal = m.progress ?? 0f;

                if (!string.IsNullOrEmpty(m.id) && progressMap.TryGetValue(m.id, out var prog))
                {
                    isCompleted = prog.completed || prog.progress >= 100f;
                    progressVal = prog.progress;
                }
                else if (m.progress.HasValue && m.progress.Value >= 100f)
                {
                    isCompleted = true;
                }

                if (isCompleted)
                {
                    totalCompletedMatchingLevel++;
                }

                // Status Filter
                if (string.Equals(_selectedStatus, MissionFilterStatus.Completed, StringComparison.OrdinalIgnoreCase) && !isCompleted)
                {
                    continue;
                }
                if (string.Equals(_selectedStatus, MissionFilterStatus.Pending, StringComparison.OrdinalIgnoreCase) && isCompleted)
                {
                    continue;
                }

                displayItems.Add(new MissionDisplayItem
                {
                    Mission = m,
                    IsCompleted = isCompleted,
                    Progress = progressVal
                });
            }

            TotalMissionsCount = totalMatchingLevel;
            CompletedMissionsCount = totalCompletedMatchingLevel;

            if (displayItems.Count == 0)
            {
                DisplayGroups = new List<MissionDisplayGroup>();
                return;
            }

            // Build hierarchical display groups using IDimensionService
            var allDimensions = _dimensionService?.GetAllDimensions();
            var groups = new List<MissionDisplayGroup>();
            var assignedItems = new HashSet<MissionDisplayItem>();

            if (allDimensions != null && allDimensions.Count > 0)
            {
                foreach (var dim in allDimensions)
                {
                    if (dim == null) continue;

                    // Find all items belonging to this dimension
                    var dimItems = displayItems.Where(item =>
                    {
                        if (string.Equals(item.Mission?.dimensionId, dim.id, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(item.Mission?.dimensionId, dim.code, StringComparison.OrdinalIgnoreCase))
                            return true;

                        var resolvedDim = _dimensionService.GetDimension(item.Mission?.dimensionId);
                        if (resolvedDim != null && (string.Equals(resolvedDim.id, dim.id, StringComparison.OrdinalIgnoreCase) ||
                                                    string.Equals(resolvedDim.code, dim.code, StringComparison.OrdinalIgnoreCase)))
                            return true;

                        return false;
                    }).ToList();

                    if (dimItems.Count == 0) continue;

                    foreach (var it in dimItems)
                        assignedItems.Add(it);

                    dimItems.Sort(CompareMissionDisplayItems);

                    int dimTotal = dimItems.Count;
                    int dimCompleted = dimItems.Count(it => it.IsCompleted);

                    bool isExpanded = _expandedDimensionCodes.Contains(dim.code) ||
                                      (!string.IsNullOrEmpty(dim.id) && _expandedDimensionCodes.Contains(dim.id));

                    groups.Add(new MissionDisplayGroup
                    {
                        Dimension = dim,
                        TotalCount = dimTotal,
                        CompletedCount = dimCompleted,
                        IsExpanded = isExpanded,
                        Missions = dimItems
                    });
                }
            }

            // Fallback for any unassigned items (or if dimensions not loaded)
            var leftoverItems = displayItems.Where(it => !assignedItems.Contains(it)).ToList();
            if (leftoverItems.Count > 0)
            {
                leftoverItems.Sort(CompareMissionDisplayItems);
                groups.Add(new MissionDisplayGroup
                {
                    Dimension = new Dimension { id = "DEFAULT", code = "ALL_MISSIONS", name = "Missions" },
                    TotalCount = leftoverItems.Count,
                    CompletedCount = leftoverItems.Count(i => i.IsCompleted),
                    IsExpanded = true,
                    Missions = leftoverItems
                });
            }

            DisplayGroups = groups;
        }

        private static int GetLevelOrder(string level)
        {
            if (string.Equals(level, MissionLevel.Beginner, StringComparison.OrdinalIgnoreCase))
                return 1;
            if (string.Equals(level, MissionLevel.Intermediate, StringComparison.OrdinalIgnoreCase))
                return 2;
            if (string.Equals(level, MissionLevel.Advanced, StringComparison.OrdinalIgnoreCase))
                return 3;
            return 4;
        }

        private static int CompareMissionDisplayItems(MissionDisplayItem a, MissionDisplayItem b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int levelA = GetLevelOrder(a.Mission?.level);
            int levelB = GetLevelOrder(b.Mission?.level);
            if (levelA != levelB)
                return levelA.CompareTo(levelB);

            return string.Compare(a.Mission?.code, b.Mission?.code, StringComparison.OrdinalIgnoreCase);
        }
    }
}
