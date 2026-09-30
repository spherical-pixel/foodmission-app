using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;

using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class ChallengeDetailViewModel : ViewModelBase
    {
        private readonly IChallengeService _challengeService;
        private readonly IDimensionService _dimensionService;
        private readonly IChallengeCompletionService _completion;
        private readonly IChallengeSessionService _session;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isCompleting;

        [ObservableProperty]
        private Challenge _challenge;

        [ObservableProperty]
        private Dimension _dimension;

        [ObservableProperty]
        private ChallengeInteraction _interaction;

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        [ObservableProperty]
        private ContentReward _earnedReward;

        private ChallengeProgress _challengeProgress;
        public ChallengeProgress ChallengeProgress
        {
            get => _challengeProgress;
            set
            {
                if (SetProperty(ref _challengeProgress, value))
                {
                    NotifyStateChanged();
                }
            }
        }

        public bool IsCompleted =>
            ChallengeProgress?.completed == true ||
            (ChallengeProgress != null && ChallengeProgress.progress >= 100f) ||
            (Challenge != null && _completion != null && _completion.IsCompleted(Challenge.code));

        public bool HasHelperModule =>
            !IsCompleted && Interaction != null && Interaction.Type != ChallengeInteractionType.Confirm &&
            !string.IsNullOrEmpty(Interaction.ModuleAction);

        public bool ShowsAutoCompleteHint => HasHelperModule && Interaction.AutoCompletes;

        public bool ShowsDoneButton => Challenge != null && !IsCompleted;

        /// <summary>"Más tarde" stays visible when loading failed, so the user always has a way out.</summary>
        public bool ShowsLaterButton => !IsCompleted && !IsLoading;

        /// <summary>All actions are disabled while the completion PATCH runs.</summary>
        public bool AreActionsEnabled => !IsCompleting;

        /// <summary>UI.csv key for the level badge, e.g. CHALLENGE_LEVEL_BEGINNER; null when unknown.</summary>
        public string LevelKey => string.IsNullOrEmpty(Challenge?.level) ? null : $"CHALLENGE_LEVEL_{Challenge.level.Trim().ToUpperInvariant()}";

        public ChallengeDetailViewModel(
            IStoreService storeService,
            IChallengeService challengeService,
            IDimensionService dimensionService,
            IChallengeCompletionService completion,
            IChallengeSessionService session) : base(storeService)
        {
            _challengeService = challengeService;
            _dimensionService = dimensionService;
            _completion = completion;
            _session = session;

            // Auto-completion from a helper module can finish while this screen is visible again
            if (_completion != null)
            {
                _completion.ChallengeCompleted += OnChallengeCompleted;
            }
        }

        protected override void OnDispose()
        {
            if (_completion != null)
            {
                _completion.ChallengeCompleted -= OnChallengeCompleted;
            }
            base.OnDispose();
        }

        private void OnChallengeCompleted(string challengeCode, ContentReward reward)
        {
            if (Challenge == null || IsCompleted ||
                !string.Equals(Challenge.code, challengeCode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ChallengeProgress = new ChallengeProgress
            {
                challengeId = Challenge.id,
                challengeCode = Challenge.code,
                completed = true,
                progress = 100f
            };
        }

        public async Task LoadChallengeAsync(string codeOrId, bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(codeOrId))
            {
                return;
            }

            IsLoading = true;
            NotifyStateChanged();
            ErrorDetail = null;
            EarnedReward = null;

            try
            {
                if (_dimensionService != null && (!_dimensionService.IsLoaded || forceRefresh))
                {
                    await _dimensionService.PreloadAsync(force: forceRefresh);
                }

                var (challenge, challengeErr) = await _challengeService.GetChallengeAsync(codeOrId);
                if (challengeErr != null)
                {
                    ErrorDetail = challengeErr;
                    return;
                }

                Challenge = challenge;
                Interaction = ChallengeInteractionCatalog.Get(challenge?.code);

                var (progress, _) = await _challengeService.GetChallengeProgressAsync(codeOrId);
                ChallengeProgress = progress;

                if (_dimensionService != null && !string.IsNullOrEmpty(challenge?.dimensionId))
                {
                    Dimension = _dimensionService.GetDimension(challenge.dimensionId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChallengeDetailViewModel] LoadChallengeAsync error: {ex.Message}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public void OpenHelperModule()
        {
            if (!HasHelperModule || Challenge == null)
            {
                return;
            }

            _session?.Begin(Challenge.code, Interaction);

            var args = new List<Argument>();
            if (Interaction.Type == ChallengeInteractionType.Comparator)
            {
                args.Add(new Argument("challengeCode", Challenge.code));
                args.Add(new Argument("mode", Interaction.ComparisonMode));
                args.Add(new Argument("source", "shopping_list"));
            }
            else if (!string.IsNullOrEmpty(Interaction.FoodFactCode))
            {
                args.Add(new Argument("code", Interaction.FoodFactCode));
            }

            RaiseNavigationRequested(Interaction.ModuleAction, args.ToArray());
        }

        /// <summary>"¡Ya lo he hecho!". Returns true when the challenge is (already) completed.</summary>
        public async Task<bool> MarkCompletedAsync()
        {
            if (Challenge == null)
            {
                return false;
            }

            if (IsCompleted)
            {
                return true;
            }

            if (IsCompleting || _completion == null)
            {
                return false;
            }

            IsCompleting = true;
            NotifyStateChanged();
            ErrorDetail = null;
            try
            {
                ChallengeCompletionResult result = await _completion.CompleteAsync(Challenge.code);
                if (!result.Success)
                {
                    ErrorDetail = result.Error ?? new ApiErrorResponse();
                    return false;
                }

                if (_session != null && string.Equals(_session.ActiveChallengeCode, Challenge.code, StringComparison.OrdinalIgnoreCase))
                {
                    _session.Cancel();
                }

                EarnedReward = result.Reward;
                ChallengeProgress = new ChallengeProgress
                {
                    challengeId = Challenge.id,
                    challengeCode = Challenge.code,
                    completed = true,
                    progress = 100f
                };
                return true;
            }
            finally
            {
                IsCompleting = false;
                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged()
        {
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(HasHelperModule));
            OnPropertyChanged(nameof(ShowsAutoCompleteHint));
            OnPropertyChanged(nameof(LevelKey));
            OnPropertyChanged(nameof(ShowsDoneButton));
            OnPropertyChanged(nameof(ShowsLaterButton));
            OnPropertyChanged(nameof(AreActionsEnabled));
        }
    }
}
