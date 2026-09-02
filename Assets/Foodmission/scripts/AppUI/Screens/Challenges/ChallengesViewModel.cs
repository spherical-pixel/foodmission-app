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
    public static class ChallengeFilterLevel
    {
        public const string All = "ALL";
        public const string Beginner = ChallengeLevel.Beginner;
        public const string Intermediate = ChallengeLevel.Intermediate;
        public const string Advanced = ChallengeLevel.Advanced;

        public static readonly string[] Options = { All, Beginner, Intermediate, Advanced };
    }

    public static class ChallengeFilterStatus
    {
        public const string All = "ALL";
        public const string Pending = "PENDING";
        public const string Completed = "COMPLETED";

        public static readonly string[] Options = { All, Pending, Completed };
    }

    public class ChallengeDisplayItem
    {
        public Challenge Challenge { get; set; }
        public bool IsCompleted { get; set; }
        public float Progress { get; set; }
    }

    public class ChallengeDisplayGroup
    {
        public Dimension Dimension { get; set; }
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public bool IsExpanded { get; set; } = false;
        public List<ChallengeDisplayItem> Challenges { get; set; } = new List<ChallengeDisplayItem>();
    }

    [ObservableObject]
    public partial class ChallengesViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedLevel = ChallengeFilterLevel.All;

        [ObservableProperty]
        private string _selectedStatus = ChallengeFilterStatus.All;

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private IReadOnlyList<ChallengeDisplayGroup> _displayGroups = new List<ChallengeDisplayGroup>();

        [ObservableProperty]
        private int _totalChallengesCount;

        [ObservableProperty]
        private int _completedChallengesCount;

        private readonly IChallengeService _challengeService;
        private readonly IDimensionService _dimensionService;
        private readonly HashSet<string> _expandedDimensionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private Challenge[] _rawChallenges = Array.Empty<Challenge>();
        private ChallengeProgress[] _rawProgress = Array.Empty<ChallengeProgress>();
        private string _lastLoadedLang;

        public event Action<Challenge> OnChallengeSelected;

        public ChallengesViewModel(
            IStoreService storeService,
            IChallengeService challengeService,
            IDimensionService dimensionService) : base(storeService)
        {
            _challengeService = challengeService;
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

                Task<(Challenge[] Result, ApiErrorResponse Error)> challengesTask =
                    _challengeService != null
                        ? _challengeService.GetChallengesAsync()
                        : Task.FromResult<(Challenge[], ApiErrorResponse)>((null, null));

                Task<(ChallengeProgress[] Result, ApiErrorResponse Error)> progressTask =
                    _challengeService != null
                        ? _challengeService.GetUserProgressListAsync()
                        : Task.FromResult<(ChallengeProgress[], ApiErrorResponse)>((null, null));

                await Task.WhenAll(challengesTask, progressTask);

                var challengesResponse = await challengesTask;
                var progressResponse = await progressTask;

                if (challengesResponse.Error != null)
                {
                    ErrorDetail = challengesResponse.Error;
                    ErrorMessage = challengesResponse.Error.message;
                    IsLoading = false;
                    return;
                }

                _rawChallenges = challengesResponse.Result ?? Array.Empty<Challenge>();
                _rawProgress = progressResponse.Result ?? Array.Empty<ChallengeProgress>();

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
                level = ChallengeFilterLevel.All;

            if (_selectedLevel != level)
            {
                SelectedLevel = level;
                RebuildDisplayGroups();
            }
        }

        public void SetStatusFilter(string status)
        {
            if (string.IsNullOrEmpty(status))
                status = ChallengeFilterStatus.All;

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

        public void OpenChallenge(Challenge challenge)
        {
            if (challenge == null) return;
            OnChallengeSelected?.Invoke(challenge);
            Debug.Log($"[{GetType().Name}] OpenChallenge clicked: {challenge.code} - {challenge.title}");
        }

        public void SetRawDataForTesting(Challenge[] challenges, ChallengeProgress[] progress)
        {
            _rawChallenges = challenges ?? Array.Empty<Challenge>();
            _rawProgress = progress ?? Array.Empty<ChallengeProgress>();
            RebuildDisplayGroups();
        }

        private void RebuildDisplayGroups()
        {
            if (_rawChallenges == null || _rawChallenges.Length == 0)
            {
                DisplayGroups = new List<ChallengeDisplayGroup>();
                TotalChallengesCount = 0;
                CompletedChallengesCount = 0;
                return;
            }

            // Map progress by challengeId
            var progressMap = new Dictionary<string, ChallengeProgress>(StringComparer.OrdinalIgnoreCase);
            if (_rawProgress != null)
            {
                foreach (var p in _rawProgress)
                {
                    if (p == null) continue;
                    if (!string.IsNullOrEmpty(p.challengeId))
                        progressMap[p.challengeId] = p;
                }
            }

            var displayItems = new List<ChallengeDisplayItem>();
            int totalMatchingLevel = 0;
            int totalCompletedMatchingLevel = 0;

            foreach (var ch in _rawChallenges)
            {
                if (ch == null) continue;

                // Level Filter
                if (!string.Equals(_selectedLevel, ChallengeFilterLevel.All, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(ch.level, _selectedLevel, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                totalMatchingLevel++;

                bool isCompleted = false;
                float progressVal = ch.progress ?? 0f;

                if (!string.IsNullOrEmpty(ch.id) && progressMap.TryGetValue(ch.id, out var prog))
                {
                    isCompleted = prog.completed || prog.progress >= 100f;
                    progressVal = prog.progress;
                }
                else if (ch.progress.HasValue && ch.progress.Value >= 100f)
                {
                    isCompleted = true;
                }

                if (isCompleted)
                {
                    totalCompletedMatchingLevel++;
                }

                // Status Filter
                if (string.Equals(_selectedStatus, ChallengeFilterStatus.Completed, StringComparison.OrdinalIgnoreCase) && !isCompleted)
                {
                    continue;
                }
                if (string.Equals(_selectedStatus, ChallengeFilterStatus.Pending, StringComparison.OrdinalIgnoreCase) && isCompleted)
                {
                    continue;
                }

                displayItems.Add(new ChallengeDisplayItem
                {
                    Challenge = ch,
                    IsCompleted = isCompleted,
                    Progress = progressVal
                });
            }

            TotalChallengesCount = totalMatchingLevel;
            CompletedChallengesCount = totalCompletedMatchingLevel;

            if (displayItems.Count == 0)
            {
                DisplayGroups = new List<ChallengeDisplayGroup>();
                return;
            }

            // Build hierarchical display groups using IDimensionService
            var allDimensions = _dimensionService?.GetAllDimensions();
            var groups = new List<ChallengeDisplayGroup>();
            var assignedItems = new HashSet<ChallengeDisplayItem>();

            if (allDimensions != null && allDimensions.Count > 0)
            {
                foreach (var dim in allDimensions)
                {
                    if (dim == null) continue;

                    // Find all items belonging to this dimension
                    var dimItems = displayItems.Where(item =>
                    {
                        if (string.Equals(item.Challenge?.dimensionId, dim.id, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(item.Challenge?.dimensionId, dim.code, StringComparison.OrdinalIgnoreCase))
                            return true;

                        var resolvedDim = _dimensionService.GetDimension(item.Challenge?.dimensionId);
                        if (resolvedDim != null && (string.Equals(resolvedDim.id, dim.id, StringComparison.OrdinalIgnoreCase) ||
                                                    string.Equals(resolvedDim.code, dim.code, StringComparison.OrdinalIgnoreCase)))
                            return true;

                        return false;
                    }).ToList();

                    if (dimItems.Count == 0) continue;

                    foreach (var it in dimItems)
                        assignedItems.Add(it);

                    dimItems.Sort(CompareChallengeDisplayItems);

                    int dimTotal = dimItems.Count;
                    int dimCompleted = dimItems.Count(it => it.IsCompleted);

                    bool isExpanded = _expandedDimensionCodes.Contains(dim.code) ||
                                      (!string.IsNullOrEmpty(dim.id) && _expandedDimensionCodes.Contains(dim.id));

                    groups.Add(new ChallengeDisplayGroup
                    {
                        Dimension = dim,
                        TotalCount = dimTotal,
                        CompletedCount = dimCompleted,
                        IsExpanded = isExpanded,
                        Challenges = dimItems
                    });
                }
            }

            // Fallback for any unassigned items (or if dimensions not loaded)
            var leftoverItems = displayItems.Where(it => !assignedItems.Contains(it)).ToList();
            if (leftoverItems.Count > 0)
            {
                leftoverItems.Sort(CompareChallengeDisplayItems);
                groups.Add(new ChallengeDisplayGroup
                {
                    Dimension = new Dimension { id = "DEFAULT", code = "ALL_CHALLENGES", name = "Challenges" },
                    TotalCount = leftoverItems.Count,
                    CompletedCount = leftoverItems.Count(i => i.IsCompleted),
                    IsExpanded = true,
                    Challenges = leftoverItems
                });
            }

            DisplayGroups = groups;
        }

        private static int GetLevelOrder(string level)
        {
            if (string.Equals(level, ChallengeLevel.Beginner, StringComparison.OrdinalIgnoreCase))
                return 1;
            if (string.Equals(level, ChallengeLevel.Intermediate, StringComparison.OrdinalIgnoreCase))
                return 2;
            if (string.Equals(level, ChallengeLevel.Advanced, StringComparison.OrdinalIgnoreCase))
                return 3;
            return 4;
        }

        private static int CompareChallengeDisplayItems(ChallengeDisplayItem a, ChallengeDisplayItem b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int levelA = GetLevelOrder(a.Challenge?.level);
            int levelB = GetLevelOrder(b.Challenge?.level);
            if (levelA != levelB)
                return levelA.CompareTo(levelB);

            return string.Compare(a.Challenge?.code, b.Challenge?.code, StringComparison.OrdinalIgnoreCase);
        }
    }
}
