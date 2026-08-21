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
    public static class QuizFilterLevel
    {
        public const string All = "ALL";
        public const string Beginner = QuizLevel.Beginner;
        public const string Intermediate = QuizLevel.Intermediate;
        public const string Advanced = QuizLevel.Advanced;

        public static readonly string[] Options = { All, Beginner, Intermediate, Advanced };
    }

    public static class QuizFilterStatus
    {
        public const string All = "ALL";
        public const string Pending = "PENDING";
        public const string Completed = "COMPLETED";

        public static readonly string[] Options = { All, Pending, Completed };
    }

    public class QuizDisplayItem
    {
        public Quiz Quiz { get; set; }
        public bool IsCompleted { get; set; }
        public bool? IsCorrect { get; set; }
    }

    public class QuizTopicGroup
    {
        public Topic Topic { get; set; }
        public List<QuizDisplayItem> Quizzes { get; set; } = new List<QuizDisplayItem>();
    }

    public class QuizDisplayGroup
    {
        public Dimension Dimension { get; set; }
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public bool IsExpanded { get; set; } = false;
        public List<QuizTopicGroup> Topics { get; set; } = new List<QuizTopicGroup>();
    }

    [ObservableObject]
    public partial class QuizzesViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedLevel = QuizFilterLevel.All;

        [ObservableProperty]
        private string _selectedStatus = QuizFilterStatus.All;

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private IReadOnlyList<QuizDisplayGroup> _displayGroups = new List<QuizDisplayGroup>();

        [ObservableProperty]
        private int _totalQuizzesCount;

        [ObservableProperty]
        private int _completedQuizzesCount;

        private readonly IQuizService _quizService;
        private readonly IDimensionService _dimensionService;
        private readonly HashSet<string> _expandedDimensionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private Quiz[] _rawQuizzes = Array.Empty<Quiz>();
        private QuizProgress[] _rawProgress = Array.Empty<QuizProgress>();
        private string _lastLoadedLang;

        public QuizzesViewModel(
            IStoreService storeService,
            IQuizService quizService,
            IDimensionService dimensionService) : base(storeService)
        {
            _quizService = quizService;
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

                Task<(PaginatedQuizResponse Result, ApiErrorResponse Error)> quizzesTask =
                    _quizService != null
                        ? _quizService.GetQuizzesAsync(limit: 200)
                        : Task.FromResult<(PaginatedQuizResponse, ApiErrorResponse)>((null, null));

                Task<(QuizProgress[] Result, ApiErrorResponse Error)> progressTask =
                    _quizService != null
                        ? _quizService.GetUserProgressListAsync()
                        : Task.FromResult<(QuizProgress[], ApiErrorResponse)>((null, null));

                await Task.WhenAll(quizzesTask, progressTask);

                var quizzesResponse = await quizzesTask;
                var progressResponse = await progressTask;

                if (quizzesResponse.Error != null)
                {
                    ErrorDetail = quizzesResponse.Error;
                    ErrorMessage = quizzesResponse.Error.message;
                    IsLoading = false;
                    return;
                }

                _rawQuizzes = quizzesResponse.Result?.data ?? Array.Empty<Quiz>();
                _rawProgress = progressResponse.Result ?? Array.Empty<QuizProgress>();

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
                level = QuizFilterLevel.All;

            if (_selectedLevel != level)
            {
                SelectedLevel = level;
                RebuildDisplayGroups();
            }
        }

        public void SetStatusFilter(string status)
        {
            if (string.IsNullOrEmpty(status))
                status = QuizFilterStatus.All;

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
                        g.IsExpanded = !wasExpanded; // was expanded -> now collapsed, or vice-versa
                        break;
                    }
                }
            }
        }

        public void OpenQuiz(Quiz quiz)
        {
            if (quiz == null) return;
            OpenQuiz(quiz.code, quiz.id);
        }

        public void OpenQuiz(string quizCode, string quizId)
        {
            RaiseNavigationRequested(Actions.open_quiz, new[]
            {
                new Argument("code", quizCode ?? ""),
                new Argument("id", quizId ?? "")
            });
        }

        public void OpenRandomQuiz()
        {
            if (_rawQuizzes == null || _rawQuizzes.Length == 0)
            {
                return;
            }

            var progressMap = new Dictionary<string, QuizProgress>(StringComparer.OrdinalIgnoreCase);
            if (_rawProgress != null)
            {
                foreach (var p in _rawProgress)
                {
                    if (p == null) continue;
                    if (!string.IsNullOrEmpty(p.quizId)) progressMap[p.quizId] = p;
                    if (!string.IsNullOrEmpty(p.quizCode)) progressMap[p.quizCode] = p;
                }
            }

            var matchingLevelQuizzes = new List<Quiz>();
            var pendingQuizzes = new List<Quiz>();

            foreach (var q in _rawQuizzes)
            {
                if (q == null) continue;

                if (!string.Equals(_selectedLevel, QuizFilterLevel.All, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(q.level, _selectedLevel, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                matchingLevelQuizzes.Add(q);

                bool isCompleted = false;
                if ((!string.IsNullOrEmpty(q.id) && progressMap.TryGetValue(q.id, out var prog)) ||
                    (!string.IsNullOrEmpty(q.code) && progressMap.TryGetValue(q.code, out prog)))
                {
                    isCompleted = prog.completed && prog.isCorrect == true;
                }

                if (!isCompleted)
                {
                    pendingQuizzes.Add(q);
                }
            }

            if (matchingLevelQuizzes.Count == 0)
            {
                return;
            }

            var candidatePool = pendingQuizzes.Count > 0 ? pendingQuizzes : matchingLevelQuizzes;
            int randomIndex = UnityEngine.Random.Range(0, candidatePool.Count);
            Quiz selectedQuiz = candidatePool[randomIndex];

            OpenQuiz(selectedQuiz);
        }

        public void SetRawDataForTesting(Quiz[] quizzes, QuizProgress[] progress)
        {
            _rawQuizzes = quizzes ?? Array.Empty<Quiz>();
            _rawProgress = progress ?? Array.Empty<QuizProgress>();
            RebuildDisplayGroups();
        }

        private void RebuildDisplayGroups()
        {
            if (_rawQuizzes == null || _rawQuizzes.Length == 0)
            {
                DisplayGroups = new List<QuizDisplayGroup>();
                TotalQuizzesCount = 0;
                CompletedQuizzesCount = 0;
                return;
            }

            // Map progress by quizId and quizCode
            var progressMap = new Dictionary<string, QuizProgress>(StringComparer.OrdinalIgnoreCase);
            if (_rawProgress != null)
            {
                foreach (var p in _rawProgress)
                {
                    if (!string.IsNullOrEmpty(p.quizId))
                        progressMap[p.quizId] = p;
                    if (!string.IsNullOrEmpty(p.quizCode))
                        progressMap[p.quizCode] = p;
                }
            }

            // Filter items
            var displayItems = new List<QuizDisplayItem>();
            int totalMatchingLevel = 0;
            int totalCompletedMatchingLevel = 0;

            foreach (var q in _rawQuizzes)
            {
                if (q == null) continue;

                // Level Filter
                if (!string.Equals(_selectedLevel, QuizFilterLevel.All, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(q.level, _selectedLevel, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                totalMatchingLevel++;

                bool isCompleted = false;
                bool? isCorrect = null;

                if ((!string.IsNullOrEmpty(q.id) && progressMap.TryGetValue(q.id, out var prog)) ||
                    (!string.IsNullOrEmpty(q.code) && progressMap.TryGetValue(q.code, out prog)))
                {
                    isCorrect = prog.isCorrect;
                    isCompleted = prog.completed && prog.isCorrect == true;
                }

                if (isCompleted)
                {
                    totalCompletedMatchingLevel++;
                }

                // Status Filter
                if (string.Equals(_selectedStatus, QuizFilterStatus.Completed, StringComparison.OrdinalIgnoreCase) && !isCompleted)
                {
                    continue;
                }
                if (string.Equals(_selectedStatus, QuizFilterStatus.Pending, StringComparison.OrdinalIgnoreCase) && isCompleted)
                {
                    continue;
                }

                displayItems.Add(new QuizDisplayItem
                {
                    Quiz = q,
                    IsCompleted = isCompleted,
                    IsCorrect = isCorrect
                });
            }

            TotalQuizzesCount = totalMatchingLevel;
            CompletedQuizzesCount = totalCompletedMatchingLevel;

            // Group by Topic
            var itemsByTopicId = new Dictionary<string, List<QuizDisplayItem>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in displayItems)
            {
                string topicKey = item.Quiz.topicId ?? "UNKNOWN_TOPIC";
                if (!itemsByTopicId.TryGetValue(topicKey, out var list))
                {
                    list = new List<QuizDisplayItem>();
                    itemsByTopicId[topicKey] = list;
                }
                list.Add(item);
            }

            // Sort quizzes within each topic by code ascending
            foreach (var list in itemsByTopicId.Values)
            {
                list.Sort((a, b) => string.Compare(a.Quiz?.code, b.Quiz?.code, StringComparison.OrdinalIgnoreCase));
            }

            // Build hierarchical display groups using IDimensionService
            var allDimensions = _dimensionService?.GetAllDimensions();
            var groups = new List<QuizDisplayGroup>();

            if (allDimensions != null && allDimensions.Count > 0)
            {
                foreach (var dim in allDimensions)
                {
                    if (dim == null) continue;

                    var topicGroups = new List<QuizTopicGroup>();
                    int dimTotal = 0;
                    int dimCompleted = 0;

                    var dimTopics = _dimensionService.GetTopicsForDimension(dim.code) ?? dim.topics;
                    if (dimTopics != null)
                    {
                        foreach (var topic in dimTopics)
                        {
                            if (topic == null) continue;

                            // Match either topic id or topic code
                            List<QuizDisplayItem> topicQuizzes = null;
                            if (!string.IsNullOrEmpty(topic.id) && itemsByTopicId.TryGetValue(topic.id, out var listById))
                            {
                                topicQuizzes = listById;
                            }
                            else if (!string.IsNullOrEmpty(topic.code) && itemsByTopicId.TryGetValue(topic.code, out var listByCode))
                            {
                                topicQuizzes = listByCode;
                            }

                            if (topicQuizzes != null && topicQuizzes.Count > 0)
                            {
                                topicGroups.Add(new QuizTopicGroup
                                {
                                    Topic = topic,
                                    Quizzes = topicQuizzes
                                });

                                foreach (var qItem in topicQuizzes)
                                {
                                    dimTotal++;
                                    if (qItem.IsCompleted)
                                        dimCompleted++;
                                }
                            }
                        }
                    }

                    if (topicGroups.Count > 0)
                    {
                        bool isExpanded = _expandedDimensionCodes.Contains(dim.code) ||
                                          (!string.IsNullOrEmpty(dim.id) && _expandedDimensionCodes.Contains(dim.id));

                        groups.Add(new QuizDisplayGroup
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
                // Fallback if dimensions not preloaded: group topics directly
                var fallbackTopics = new List<QuizTopicGroup>();
                foreach (var kvp in itemsByTopicId)
                {
                    fallbackTopics.Add(new QuizTopicGroup
                    {
                        Topic = new Topic { id = kvp.Key, code = kvp.Key, name = kvp.Key },
                        Quizzes = kvp.Value
                    });
                }

                if (fallbackTopics.Count > 0)
                {
                    groups.Add(new QuizDisplayGroup
                    {
                        Dimension = new Dimension { id = "DEFAULT", code = "ALL_QUIZZES", name = "Quizzes" },
                        TotalCount = displayItems.Count,
                        CompletedCount = displayItems.Count(i => i.IsCompleted),
                        IsExpanded = false,
                        Topics = fallbackTopics
                    });
                }
            }

            DisplayGroups = groups;
        }
    }
}
