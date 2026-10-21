using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>Onboarding steps in their fixed order. Survey covers the survey and the level proposal.</summary>
    public enum OnboardingStep
    {
        Profile,
        Goals,
        Survey,
        Avatar
    }

    public enum OnboardingStepState
    {
        Pending,
        Current,
        Done,
        Skipped
    }

    /// <summary>
    /// The onboarding run in progress (after registration or from Home's profile reminder): which steps were done or
    /// skipped this run. Kept in memory only; Home ends it, an app restart forgets it.
    /// </summary>
    public interface IOnboardingRun
    {
        bool IsActive { get; }

        /// <summary>Starts a new run, forgetting the marks of a previous one.</summary>
        void Start();

        void End();

        /// <summary>No-op when no run is active.</summary>
        void MarkDone(OnboardingStep step);

        /// <summary>No-op when no run is active. Also silences Home's onboarding reminder for the session.</summary>
        void MarkSkipped(OnboardingStep step);

        /// <summary>One state per step, indexed by (int)OnboardingStep. Done also comes from the saved user data.</summary>
        IReadOnlyList<OnboardingStepState> GetStates(AppState state);

        /// <summary>First step neither done nor skipped; null when nothing is left.</summary>
        OnboardingStep? GetCurrent(AppState state);
    }
}
