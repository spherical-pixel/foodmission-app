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
    public static class FoodFactFilterLevel
    {
        public const string All = "ALL";
        public const string Beginner = FoodFactLevel.Beginner;
        public const string Intermediate = FoodFactLevel.Intermediate;
        public const string Advanced = FoodFactLevel.Advanced;

        public static readonly string[] Options = { All, Beginner, Intermediate, Advanced };
    }

    public class FoodFactDisplayItem
    {
        public FoodFact FoodFact { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class FoodFactTopicGroup
    {
        public Topic Topic { get; set; }
        public List<FoodFactDisplayItem> Facts { get; set; } = new List<FoodFactDisplayItem>();
    }

    public class FoodFactDisplayGroup
    {
        public Dimension Dimension { get; set; }
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public bool IsExpanded { get; set; } = false;
        public List<FoodFactTopicGroup> Topics { get; set; } = new List<FoodFactTopicGroup>();
    }

    [ObservableObject]
    public partial class FoodFactsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedLevel = FoodFactFilterLevel.All;

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private IReadOnlyList<FoodFactDisplayGroup> _displayGroups = new List<FoodFactDisplayGroup>();

        [ObservableProperty]
        private int _totalFactsCount;

        [ObservableProperty]
        private int _completedFactsCount;

        private readonly IFoodFactService _foodFactService;
        private readonly IDimensionService _dimensionService;
        private readonly HashSet<string> _expandedDimensionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private FoodFact[] _rawFacts = Array.Empty<FoodFact>();
        private FoodFactProgressResponse[] _rawProgress = Array.Empty<FoodFactProgressResponse>();
        private string _lastLoadedLang;

        public FoodFactsViewModel(
            IStoreService storeService,
            IFoodFactService foodFactService,
            IDimensionService dimensionService) : base(storeService)
        {
            _foodFactService = foodFactService;
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
                if (_dimensionService != null && (!_dimensionService.IsLoaded || forceRefresh))
                {
                    await _dimensionService.PreloadAsync(force: forceRefresh);
                }

                Task<(PaginatedFoodFactResponse Result, ApiErrorResponse Error)> factsTask =
                    _foodFactService != null
                        ? _foodFactService.GetFoodFactsAsync(limit: 200)
                        : Task.FromResult<(PaginatedFoodFactResponse, ApiErrorResponse)>((null, null));

                Task<(FoodFactProgressResponse[] Result, ApiErrorResponse Error)> progressTask =
                    _foodFactService != null
                        ? _foodFactService.GetUserProgressListAsync()
                        : Task.FromResult<(FoodFactProgressResponse[], ApiErrorResponse)>((null, null));

                await Task.WhenAll(factsTask, progressTask);

                var response = await factsTask;
                var progressResponse = await progressTask;

                if (response.Error != null)
                {
                    ErrorDetail = response.Error;
                    ErrorMessage = response.Error.message;
                    IsLoading = false;
                    return;
                }

                _rawFacts = response.Result?.data ?? Array.Empty<FoodFact>();
                _rawProgress = progressResponse.Result ?? Array.Empty<FoodFactProgressResponse>();

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
                level = FoodFactFilterLevel.All;

            if (_selectedLevel != level)
            {
                SelectedLevel = level;
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

        public void OpenFoodFact(FoodFact fact)
        {
            if (fact == null) return;
            OpenFoodFact(fact.code, fact.id);
        }

        public void OpenFoodFact(string factCode, string factId)
        {
            RaiseNavigationRequested(Actions.open_food_fact, new[]
            {
                new Argument("code", factCode ?? ""),
                new Argument("id", factId ?? "")
            });
        }

        public void OpenRandomFact()
        {
            if (_rawFacts == null || _rawFacts.Length == 0)
            {
                return;
            }

            var completedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_rawProgress != null)
            {
                foreach (var p in _rawProgress)
                {
                    if (p == null) continue;
                    if (!string.IsNullOrEmpty(p.foodFactId)) completedSet.Add(p.foodFactId);
                    if (!string.IsNullOrEmpty(p.foodFactCode)) completedSet.Add(p.foodFactCode);
                }
            }

            var matchingLevelFacts = new List<FoodFact>();
            var pendingFacts = new List<FoodFact>();

            foreach (var f in _rawFacts)
            {
                if (f == null) continue;

                if (!string.Equals(_selectedLevel, FoodFactFilterLevel.All, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(f.level, _selectedLevel, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                matchingLevelFacts.Add(f);

                bool isCompleted = (!string.IsNullOrEmpty(f.id) && completedSet.Contains(f.id)) ||
                                   (!string.IsNullOrEmpty(f.code) && completedSet.Contains(f.code));

                if (!isCompleted)
                {
                    pendingFacts.Add(f);
                }
            }

            if (matchingLevelFacts.Count == 0) return;

            var candidatePool = pendingFacts.Count > 0 ? pendingFacts : matchingLevelFacts;
            int randomIndex = UnityEngine.Random.Range(0, candidatePool.Count);
            FoodFact selectedFact = candidatePool[randomIndex];

            OpenFoodFact(selectedFact);
        }

        public void SetRawDataForTesting(FoodFact[] facts, FoodFactProgressResponse[] progress = null)
        {
            _rawFacts = facts ?? Array.Empty<FoodFact>();
            _rawProgress = progress ?? Array.Empty<FoodFactProgressResponse>();
            RebuildDisplayGroups();
        }

        private void RebuildDisplayGroups()
        {
            if (_rawFacts == null || _rawFacts.Length == 0)
            {
                DisplayGroups = new List<FoodFactDisplayGroup>();
                TotalFactsCount = 0;
                CompletedFactsCount = 0;
                return;
            }

            var completedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_rawProgress != null)
            {
                foreach (var p in _rawProgress)
                {
                    if (p == null) continue;
                    if (!string.IsNullOrEmpty(p.foodFactId)) completedSet.Add(p.foodFactId);
                    if (!string.IsNullOrEmpty(p.foodFactCode)) completedSet.Add(p.foodFactCode);
                }
            }

            var displayItems = new List<FoodFactDisplayItem>();
            int totalMatchingLevel = 0;
            int totalCompletedMatchingLevel = 0;

            foreach (var f in _rawFacts)
            {
                if (f == null) continue;

                if (!string.Equals(_selectedLevel, FoodFactFilterLevel.All, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(f.level, _selectedLevel, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                totalMatchingLevel++;

                bool isCompleted = (!string.IsNullOrEmpty(f.id) && completedSet.Contains(f.id)) ||
                                   (!string.IsNullOrEmpty(f.code) && completedSet.Contains(f.code));

                if (isCompleted)
                {
                    totalCompletedMatchingLevel++;
                }

                displayItems.Add(new FoodFactDisplayItem
                {
                    FoodFact = f,
                    IsCompleted = isCompleted
                });
            }

            TotalFactsCount = totalMatchingLevel;
            CompletedFactsCount = totalCompletedMatchingLevel;

            // Group by Topic
            var itemsByTopicId = new Dictionary<string, List<FoodFactDisplayItem>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in displayItems)
            {
                string topicKey = item.FoodFact.topicId ?? "UNKNOWN_TOPIC";
                if (!itemsByTopicId.TryGetValue(topicKey, out var list))
                {
                    list = new List<FoodFactDisplayItem>();
                    itemsByTopicId[topicKey] = list;
                }
                list.Add(item);
            }

            // Sort facts within each topic by code ascending
            foreach (var list in itemsByTopicId.Values)
            {
                list.Sort((a, b) => string.Compare(a.FoodFact?.code, b.FoodFact?.code, StringComparison.OrdinalIgnoreCase));
            }

            // Build hierarchical display groups using IDimensionService
            var allDimensions = _dimensionService?.GetAllDimensions();
            var groups = new List<FoodFactDisplayGroup>();

            if (allDimensions != null && allDimensions.Count > 0)
            {
                foreach (var dim in allDimensions)
                {
                    if (dim == null) continue;

                    var topicGroups = new List<FoodFactTopicGroup>();
                    int dimTotal = 0;
                    int dimCompleted = 0;

                    var dimTopics = _dimensionService.GetTopicsForDimension(dim.code) ?? dim.topics;
                    if (dimTopics != null)
                    {
                        foreach (var topic in dimTopics)
                        {
                            if (topic == null) continue;

                            List<FoodFactDisplayItem> topicFacts = null;
                            if (!string.IsNullOrEmpty(topic.id) && itemsByTopicId.TryGetValue(topic.id, out var listById))
                            {
                                topicFacts = listById;
                            }
                            else if (!string.IsNullOrEmpty(topic.code) && itemsByTopicId.TryGetValue(topic.code, out var listByCode))
                            {
                                topicFacts = listByCode;
                            }

                            if (topicFacts != null && topicFacts.Count > 0)
                            {
                                topicGroups.Add(new FoodFactTopicGroup
                                {
                                    Topic = topic,
                                    Facts = topicFacts
                                });

                                foreach (var factItem in topicFacts)
                                {
                                    dimTotal++;
                                    if (factItem.IsCompleted)
                                        dimCompleted++;
                                }
                            }
                        }
                    }

                    if (topicGroups.Count > 0)
                    {
                        bool isExpanded = _expandedDimensionCodes.Contains(dim.code) ||
                                          (!string.IsNullOrEmpty(dim.id) && _expandedDimensionCodes.Contains(dim.id));

                        groups.Add(new FoodFactDisplayGroup
                        {
                            Dimension = dim,
                            TotalCount = dimTotal,
                            CompletedCount = dimCompleted,
                            IsExpanded = isExpanded,
                            Topics = topicGroups
                        });
                    }
                }
            }
            else
            {
                var fallbackTopics = new List<FoodFactTopicGroup>();
                foreach (var kvp in itemsByTopicId)
                {
                    fallbackTopics.Add(new FoodFactTopicGroup
                    {
                        Topic = new Topic { id = kvp.Key, code = kvp.Key, name = kvp.Key },
                        Facts = kvp.Value
                    });
                }

                if (fallbackTopics.Count > 0)
                {
                    groups.Add(new FoodFactDisplayGroup
                    {
                        Dimension = new Dimension { id = "DEFAULT", code = "ALL_FOOD_FACTS", name = "Food Facts" },
                        TotalCount = displayItems.Count,
                        CompletedCount = totalCompletedMatchingLevel,
                        IsExpanded = false,
                        Topics = fallbackTopics
                    });
                }
            }

            DisplayGroups = groups;
        }
    }
}
