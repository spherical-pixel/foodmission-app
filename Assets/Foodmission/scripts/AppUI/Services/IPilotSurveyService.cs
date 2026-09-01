using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IPilotSurveyService
    {
        /// <summary>
        /// Checks if the user's country is one of the designated pilot countries (de, gr, it, nl, no, si).
        /// </summary>
        bool IsPilotCountry(string countryCode = null);

        /// <summary>
        /// Checks whether the user has accepted the pilot consent form.
        /// </summary>
        Task<bool> HasAcceptedPilotConsentAsync();

        /// <summary>
        /// Records the user's acceptance of the pilot consent form.
        /// </summary>
        Task<bool> AcceptPilotConsentAsync();

        /// <summary>
        /// Records an active app usage day for today in the current cycle if not already recorded.
        /// </summary>
        void RecordDailyUsage();

        /// <summary>
        /// Returns the number of distinct active days recorded in the current survey cycle.
        /// </summary>
        int GetActiveDaysCountInCurrentCycle();

        /// <summary>
        /// Returns the number of days elapsed since the start of the current survey cycle.
        /// </summary>
        int GetDaysSinceCurrentCycleStart();

        /// <summary>
        /// Gets the current persisted survey cycle state.
        /// </summary>
        PilotSurveyCycleState GetCurrentCycleState();

        /// <summary>
        /// Advances to the next survey cycle, resetting active usage dates and completed surveys for the new cycle.
        /// </summary>
        void AdvanceToNextCycle();

        /// <summary>
        /// Resets all cycle state (used on logout or testing).
        /// </summary>
        void ResetCycles();

        /// <summary>
        /// Evaluates survey schedule rules and returns the next pending survey for the current cycle, or null if none.
        /// </summary>
        Task<SurveyDto> GetPendingPilotSurveyAsync(string lang = null);

        /// <summary>
        /// Postpones the survey for the current app session.
        /// </summary>
        void PostponeSurvey(string slug);

        /// <summary>
        /// Skips the survey in the current cycle. If this was the last survey of the cycle, advances to next cycle.
        /// </summary>
        void SkipSurvey(string slug);

        /// <summary>
        /// Marks the survey as completed for the current cycle. If this was the last survey of the cycle, advances to next cycle.
        /// </summary>
        Task<bool> MarkSurveyCompletedAsync(string slug, string surveyId);

        /// <summary>
        /// Returns the configured pilot survey scheduling rules in priority order.
        /// </summary>
        IReadOnlyList<PilotSurveyRule> GetRules();

        /// <summary>
        /// Debug/Testing helper to simulate active usage days and days elapsed in current cycle.
        /// </summary>
        void SetDebugDays(int activeDaysCount, int daysSinceStart);

        /// <summary>
        /// Debug/Testing helper to clear completed/skipped survey flags in current cycle without resetting cycle number.
        /// </summary>
        void ResetCycleSurveysOnly();

        /// <summary>
        /// Debug flag to bypass country and consent checks for testing.
        /// </summary>
        bool DebugBypassEligibility { get; set; }

        /// <summary>
        /// Sets a debug country code in AppState.
        /// </summary>
        void SetDebugUserCountry(string countryCode);
    }
}
