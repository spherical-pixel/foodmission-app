using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Onboarding progress: shows which steps of the run are done, skipped or next, and opens or skips the current one.
    /// One-step flow so the screen reuses the StepFlow companion (Nutri), buttons and ✕.
    /// </summary>
    public partial class OnboardingProgressViewModel : StepFlowViewModelBase
    {
        // A second tap on Skip right after the first must not skip the step that just became current
        private const double k_SkipCooldownSeconds = 0.5;

        private readonly IOnboardingRun _onboardingRun;
        private readonly IAuthService _authService;
        private DateTime _lastSkipUtc = DateTime.MinValue;

        private IReadOnlyList<OnboardingStepState> _stepStates = Array.Empty<OnboardingStepState>();
        public IReadOnlyList<OnboardingStepState> StepStates
        {
            get => _stepStates;
            private set => SetProperty(ref _stepStates, value);
        }

        private OnboardingStep? _currentStep;
        public OnboardingStep? CurrentStep
        {
            get => _currentStep;
            private set => SetProperty(ref _currentStep, value);
        }

        private bool _isSubmitting;
        public bool IsSubmitting
        {
            get => _isSubmitting;
            private set => SetProperty(ref _isSubmitting, value);
        }

        public OnboardingProgressViewModel(IStoreService storeService, IOnboardingRun onboardingRun, IAuthService authService) : base(storeService)
        {
            _onboardingRun = onboardingRun;
            _authService = authService;
        }

        /// <summary>Recomputes the steps; with nothing left the run ends and the user goes Home.</summary>
        public void Refresh()
        {
            AppState state = _storeService.GetAppState();
            StepStates = _onboardingRun.GetStates(state);
            CurrentStep = _onboardingRun.GetCurrent(state);
            RefreshStepState();

            if (CurrentStep == null)
            {
                _onboardingRun.End();
                RaiseNavigationRequested(Actions.go_to_home);
            }
        }

        /// <summary>"Skip this step": marks the current step skipped (the profile also records it in the backend).</summary>
        public async Task SkipCurrentAsync()
        {
            if (IsSubmitting || CurrentStep == null || (DateTime.UtcNow - _lastSkipUtc).TotalSeconds < k_SkipCooldownSeconds)
            {
                return;
            }

            OnboardingStep step = CurrentStep.Value;
            _lastSkipUtc = DateTime.UtcNow;
            IsSubmitting = true;
            try
            {
                if (step == OnboardingStep.Profile)
                {
                    await OnboardingProfileSkip.MarkAsync(_authService, _storeService);
                }
                _onboardingRun.MarkSkipped(step);
            }
            finally
            {
                IsSubmitting = false;
            }
            Refresh();
        }

        /// <summary>✕: leaves the onboarding. A profile not done yet is recorded as skipped so Home reminds it.</summary>
        public async Task CloseAsync()
        {
            if (IsSubmitting)
            {
                return;
            }

            IsSubmitting = true;
            try
            {
                AppState state = _storeService.GetAppState();
                if (_onboardingRun.GetStates(state)[(int)OnboardingStep.Profile] != OnboardingStepState.Done)
                {
                    await OnboardingProfileSkip.MarkAsync(_authService, _storeService);
                }
            }
            finally
            {
                IsSubmitting = false;
            }
            _onboardingRun.End();
            RaiseNavigationRequested(Actions.go_to_home);
        }

        protected override int GetStepCount() => 1;

        protected override bool ValidateStep(int stepIndex) => CurrentStep != null && !IsSubmitting;

        protected override string GetStepTitle(int stepIndex) => string.Empty;

        protected override Task OnStepEnteredAsync(int stepIndex) => Task.CompletedTask;

        protected override Task OnStepExitingAsync(int stepIndex) => Task.CompletedTask;

        /// <summary>"Continue": opens the current step.</summary>
        protected override Task OnFlowCompletedAsync()
        {
            switch (CurrentStep)
            {
                case OnboardingStep.Profile:
                    RaiseNavigationRequested(Actions.register_to_onboarding);
                    break;
                case OnboardingStep.Goals:
                    RaiseNavigationRequested(Actions.go_to_onboarding_goals,
                        new Argument("fromHome", "false"),
                        new Argument("fromEditProfile", "false"));
                    break;
                case OnboardingStep.Survey:
                    RaiseNavigationRequested(Actions.onboardingprofile_to_onboarding_survey);
                    break;
                case OnboardingStep.Avatar:
                    RaiseNavigationRequested(Actions.go_to_avatar_editor, new Argument("fromOnboarding", "true"));
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
