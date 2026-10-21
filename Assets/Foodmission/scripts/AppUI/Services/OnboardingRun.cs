using System.Collections.Generic;

namespace eu.foodmission.platform
{
    public class OnboardingRun : IOnboardingRun
    {
        private const int k_StepCount = 4;

        private readonly HashSet<OnboardingStep> _done = new HashSet<OnboardingStep>();
        private readonly HashSet<OnboardingStep> _skipped = new HashSet<OnboardingStep>();

        public bool IsActive { get; private set; }

        public void Start()
        {
            _done.Clear();
            _skipped.Clear();
            IsActive = true;
        }

        public void End()
        {
            _done.Clear();
            _skipped.Clear();
            IsActive = false;
        }

        public void MarkDone(OnboardingStep step)
        {
            if (IsActive)
            {
                _done.Add(step);
            }
        }

        public void MarkSkipped(OnboardingStep step)
        {
            if (IsActive)
            {
                _skipped.Add(step);
                // The user just chose to skip: Home must not ask for it again this session
                HomePromptSession.OnboardingDeferred = true;
            }
        }

        public IReadOnlyList<OnboardingStepState> GetStates(AppState state)
        {
            var states = new OnboardingStepState[k_StepCount];
            bool currentFound = false;
            for (int i = 0; i < k_StepCount; i++)
            {
                var step = (OnboardingStep)i;
                if (IsDone(step, state))
                {
                    states[i] = OnboardingStepState.Done;
                }
                else if (_skipped.Contains(step))
                {
                    states[i] = OnboardingStepState.Skipped;
                }
                else if (!currentFound)
                {
                    states[i] = OnboardingStepState.Current;
                    currentFound = true;
                }
                else
                {
                    states[i] = OnboardingStepState.Pending;
                }
            }
            return states;
        }

        public OnboardingStep? GetCurrent(AppState state)
        {
            IReadOnlyList<OnboardingStepState> states = GetStates(state);
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i] == OnboardingStepState.Current)
                {
                    return (OnboardingStep)i;
                }
            }
            return null;
        }

        private bool IsDone(OnboardingStep step, AppState state)
        {
            if (_done.Contains(step))
            {
                return true;
            }
            if (state == null)
            {
                return false;
            }
            switch (step)
            {
                case OnboardingStep.Profile:
                    return state.hasCompletedExtendedProfile;
                case OnboardingStep.Goals:
                    return state.userGoals != null && state.userGoals.Length > 0;
                case OnboardingStep.Survey:
                    return state.userOnboardingSurvey != null && state.userOnboardingSurvey.HasAnswers();
                case OnboardingStep.Avatar:
                    return state.userHasAvatar;
                default:
                    return false;
            }
        }
    }
}
