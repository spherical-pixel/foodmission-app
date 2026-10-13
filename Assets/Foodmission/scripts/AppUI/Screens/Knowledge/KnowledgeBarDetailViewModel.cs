using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>One knowledge bar with its items grouped by content type (GET knowledge-progress/{kind}).</summary>
    [ObservableObject]
    public partial class KnowledgeBarDetailViewModel : ViewModelBase
    {
        private readonly IGamificationService _gamificationService;
        private int _loadVersion;

        [ObservableProperty]
        private string _kind = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        [ObservableProperty]
        private int _finished;

        [ObservableProperty]
        private int _total;

        [ObservableProperty]
        private float _progress;

        [ObservableProperty]
        private IReadOnlyList<KnowledgeBarSection> _sections = Array.Empty<KnowledgeBarSection>();

        /// <summary>Raised when the screen has nothing to show for the requested kind.</summary>
        public event Action BackRequested;

        public KnowledgeBarDetailViewModel(IStoreService storeService, IGamificationService gamificationService = null) : base(storeService)
        {
            _gamificationService = gamificationService ?? App.current?.services?.GetService<IGamificationService>();
        }

        public async Task LoadAsync(string kind)
        {
            if (!KnowledgeKind.IsKnown(kind) || _gamificationService == null)
            {
                BackRequested?.Invoke();
                return;
            }

            int version = ++_loadVersion;
            Kind = kind;
            // Show the cached Home values while the detail loads (and if it fails), not 0 of 0.
            KnowledgeProgress cached = KnowledgeBars.Find(_storeService?.GetAppState(), kind);
            if (cached != null && Total == 0)
            {
                Finished = cached.finishedItems;
                Total = cached.totalItems;
                Progress = Mathf.Clamp01(cached.percentComplete / 100f);
            }
            IsLoading = true;
            ErrorDetail = null;
            try
            {
                var (bar, error) = await _gamificationService.GetKnowledgeBarAsync(kind);
                if (version != _loadVersion)
                {
                    return;
                }
                if (error != null || bar == null)
                {
                    ErrorDetail = error ?? new ApiErrorResponse();
                    return;
                }

                Finished = bar.finishedItems;
                Total = bar.totalItems;
                Progress = Mathf.Clamp01(bar.percentComplete / 100f);
                Sections = KnowledgeBarSections.Build(bar.items);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(KnowledgeBarDetailViewModel)}] LoadAsync failed: {ex.Message}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
            finally
            {
                if (version == _loadVersion)
                {
                    IsLoading = false;
                }
            }
        }

        public void Open(KnowledgeBarRow row)
        {
            if (row == null || !row.CanOpen)
            {
                return;
            }
            if (row.ContentType == QuestContentType.Quiz)
            {
                RaiseNavigationRequested(Actions.open_quiz, new Argument("code", row.Code));
            }
            else if (row.ContentType == QuestContentType.FoodFact)
            {
                RaiseNavigationRequested(Actions.open_food_fact, new Argument("code", row.Code));
            }
        }
    }
}
