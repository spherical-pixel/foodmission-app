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
        private readonly IQuestService _questService;

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

        private QuestMembership _membership = QuestMembership.Unknown;

        /// <summary>Challenges are done inside their quest. Only true when it's known not to be in the current one (a failed check doesn't block).</summary>
        public bool IsOutsideCurrentQuest => _membership.InCurrentQuest == false;

        /// <summary>The quest that has this challenge (the current one first); null when none was found.</summary>
        public Quest ContainingQuest => _membership.Quest;

        public bool ShowsNotCurrentQuest => Challenge != null && !IsCompleted && IsOutsideCurrentQuest;

        public bool ShowsGoToQuest => ShowsNotCurrentQuest && ContainingQuest != null;

        public bool IsCompleted =>
            ChallengeProgress?.completed == true ||
            (ChallengeProgress != null && ChallengeProgress.progress >= 100f) ||
            (Challenge != null && _completion != null && _completion.IsCompleted(Challenge.code));

        public bool HasHelperModule =>
            !IsCompleted && !IsOutsideCurrentQuest && Interaction != null && Interaction.Type != ChallengeInteractionType.Confirm &&
            !string.IsNullOrEmpty(Interaction.ModuleAction);

        public bool ShowsAutoCompleteHint => HasHelperModule && Interaction.AutoCompletes;

        public bool ShowsDoneButton => Challenge != null && !IsCompleted && !IsOutsideCurrentQuest;

        /// <summary>"Más tarde" stays visible when loading failed, so the user always has a way out.</summary>
        public bool ShowsLaterButton => !IsCompleted && !IsLoading && !IsOutsideCurrentQuest;

        /// <summary>All actions are disabled while the completion PATCH runs.</summary>
        public bool AreActionsEnabled => !IsCompleting;

        /// <summary>UI.csv key for the level badge, e.g. CHALLENGE_LEVEL_BEGINNER; null when unknown.</summary>
        public string LevelKey => string.IsNullOrEmpty(Challenge?.level) ? null : $"CHALLENGE_LEVEL_{Challenge.level.Trim().ToUpperInvariant()}";

        public ChallengeDetailViewModel(
            IStoreService storeService,
            IChallengeService challengeService,
            IDimensionService dimensionService,
            IChallengeCompletionService completion,
            IChallengeSessionService session,
            IQuestService questService) : base(storeService)
        {
            _questService = questService;
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
            _membership = QuestMembership.Unknown;

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

                _membership = await QuestMembership.FindAsync(
                    _questService, _storeService?.GetAppState()?.userCurrentQuestId, QuestContentType.Challenge, challenge?.code ?? codeOrId,
                    challenge?.dimensionId, challenge?.level);

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

            _session?.Begin(Challenge.code, Interaction, Challenge.title, Challenge.task);

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

        public void OpenContainingQuest()
        {
            if (!ShowsGoToQuest)
            {
                return;
            }
            RaiseNavigationRequested(Actions.open_quest, QuestMembership.OpenQuestArguments(ContainingQuest));
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

            if (IsOutsideCurrentQuest)
            {
                return false;
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
            OnPropertyChanged(nameof(IsOutsideCurrentQuest));
            OnPropertyChanged(nameof(ShowsNotCurrentQuest));
            OnPropertyChanged(nameof(ShowsGoToQuest));
        }
    }
}
