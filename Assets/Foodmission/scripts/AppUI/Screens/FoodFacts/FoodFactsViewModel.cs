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

        private readonly IFoodFactService _foodFactService;
        private readonly IDimensionService _dimensionService;
        private readonly HashSet<string> _expandedDimensionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private FoodFact[] _rawFacts = Array.Empty<FoodFact>();
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

                if (_foodFactService != null)
                {
                    var response = await _foodFactService.GetFoodFactsAsync(limit: 200);
                    if (response.Error != null)
                    {
                        ErrorDetail = response.Error;
                        ErrorMessage = response.Error.message;
                        IsLoading = false;
                        return;
                    }

                    _rawFacts = response.Result?.data ?? Array.Empty<FoodFact>();
                }
                else
                {
                    _rawFacts = Array.Empty<FoodFact>();
                }

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

            var matchingLevelFacts = new List<FoodFact>();
            foreach (var f in _rawFacts)
            {
                if (f == null) continue;

                if (!string.Equals(_selectedLevel, FoodFactFilterLevel.All, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(f.level, _selectedLevel, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                matchingLevelFacts.Add(f);
            }

            var candidatePool = matchingLevelFacts.Count > 0 ? matchingLevelFacts : _rawFacts.ToList();
            if (candidatePool.Count == 0) return;

            int randomIndex = UnityEngine.Random.Range(0, candidatePool.Count);
            FoodFact selectedFact = candidatePool[randomIndex];

            OpenFoodFact(selectedFact);
        }

        public void SetRawDataForTesting(FoodFact[] facts)
        {
            _rawFacts = facts ?? Array.Empty<FoodFact>();
            RebuildDisplayGroups();
        }

        private void RebuildDisplayGroups()
        {
            if (_rawFacts == null || _rawFacts.Length == 0)
            {
                DisplayGroups = new List<FoodFactDisplayGroup>();
                TotalFactsCount = 0;
                return;
            }

            var displayItems = new List<FoodFactDisplayItem>();
            int totalMatchingLevel = 0;

            foreach (var f in _rawFacts)
            {
                if (f == null) continue;

                if (!string.Equals(_selectedLevel, FoodFactFilterLevel.All, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(f.level, _selectedLevel, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                totalMatchingLevel++;
                displayItems.Add(new FoodFactDisplayItem
                {
                    FoodFact = f
                });
            }

            TotalFactsCount = totalMatchingLevel;

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

                                dimTotal += topicFacts.Count;
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
                        IsExpanded = false,
                        Topics = fallbackTopics
                    });
                }
            }

            DisplayGroups = groups;
        }
    }
}
