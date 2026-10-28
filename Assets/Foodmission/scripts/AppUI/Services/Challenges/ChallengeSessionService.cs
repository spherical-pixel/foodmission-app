using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.Navigation.Generated;

using UnityEngine;

namespace eu.foodmission.platform
{
    public class ChallengeSessionService : IChallengeSessionService
    {
        private readonly IChallengeCompletionService _completion;
        private readonly HashSet<string> _reportedItems = new(StringComparer.OrdinalIgnoreCase);
        private ChallengeInteraction _interaction;
        private bool _isCompleting;

        public event Action<string, ContentReward> AutoCompleted;
        public event Action SessionChanged;

        public string ActiveChallengeCode { get; private set; }
        public ChallengeInteraction ActiveInteraction => _interaction;
        public string ActiveChallengeTitle { get; private set; }
        public string ActiveChallengeTask { get; private set; }
        public int ReportedCount => _reportedItems.Count;
        public ApiErrorResponse LastError { get; private set; }
        public bool IsCompletionPending => _interaction != null && _reportedItems.Count >= Math.Max(1, _interaction.RequiredCount);

        public ChallengeSessionService(IChallengeCompletionService completion)
        {
            _completion = completion;
        }

        /// <summary>
        /// Screens of the helper module flow. Anywhere else the user is no longer working on the challenge,
        /// so the session ends (otherwise its card shows up when opening a product from the pantry days later).
        /// </summary>
        public static bool IsFlowDestination(ChallengeCompletionTrigger trigger, string destinationName)
        {
            switch (trigger)
            {
                case ChallengeCompletionTrigger.ProductViewed:
                    return destinationName == Destinations.quick_search;
                case ChallengeCompletionTrigger.FoodFactRead:
                    return destinationName == Destinations.foodfact;
                case ChallengeCompletionTrigger.RecipeViewed:
                    return destinationName == Destinations.recipebook || destinationName == Destinations.recipedetail;
                case ChallengeCompletionTrigger.FoodWasteLogged:
                    return destinationName == Destinations.foodwaste || destinationName == Destinations.pantry ||
                           destinationName == Destinations.pantryitemdetail;
                default:
                    return false;
            }
        }

        public void OnDestinationChanged(string destinationName)
        {
            if (_interaction != null && !IsFlowDestination(_interaction.Trigger, destinationName))
            {
                Cancel();
            }
        }

        public bool HasReported(string itemKey) =>
            !string.IsNullOrEmpty(itemKey) && _reportedItems.Contains(itemKey.Trim());

        public void Begin(string challengeCode, ChallengeInteraction interaction, string title = null, string task = null)
        {
            Cancel();
            if (string.IsNullOrWhiteSpace(challengeCode) || interaction == null || !interaction.AutoCompletes ||
                interaction.Trigger == ChallengeCompletionTrigger.ComparisonDone)
            {
                return;
            }

            ActiveChallengeCode = challengeCode.Trim();
            ActiveChallengeTitle = title;
            ActiveChallengeTask = task;
            _interaction = interaction;
            SessionChanged?.Invoke();
        }

        public void Cancel()
        {
            bool wasActive = ActiveChallengeCode != null;
            ActiveChallengeCode = null;
            ActiveChallengeTitle = null;
            ActiveChallengeTask = null;
            LastError = null;
            _interaction = null;
            _reportedItems.Clear();
            if (wasActive)
            {
                SessionChanged?.Invoke();
            }
        }

        public async Task<ChallengeReportResult> ReportAsync(ChallengeCompletionTrigger trigger, string itemKey)
        {
            if (ActiveChallengeCode == null || _interaction == null || trigger != _interaction.Trigger || string.IsNullOrEmpty(itemKey))
            {
                return ChallengeReportResult.Ignored;
            }

            if (trigger == ChallengeCompletionTrigger.FoodFactRead &&
                !string.Equals(itemKey.Trim(), _interaction.FoodFactCode, StringComparison.OrdinalIgnoreCase))
            {
                return ChallengeReportResult.Ignored;
            }

            if (_reportedItems.Add(itemKey.Trim()))
            {
                SessionChanged?.Invoke();
            }
            if (_reportedItems.Count < Math.Max(1, _interaction.RequiredCount) || _isCompleting)
            {
                return ChallengeReportResult.Counted;
            }

            string code = ActiveChallengeCode;
            _isCompleting = true;
            try
            {
                ChallengeCompletionResult result = await _completion.CompleteAsync(code);
                if (!result.Success)
                {
                    Debug.LogWarning($"[ChallengeSessionService] Auto-completion of {code} failed; session kept for retry");
                    LastError = result.Error;
                    return ChallengeReportResult.Failed;
                }

                if (string.Equals(ActiveChallengeCode, code, StringComparison.OrdinalIgnoreCase))
                {
                    Cancel();
                }
                AutoCompleted?.Invoke(code, result.Reward);
                return ChallengeReportResult.Completed;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChallengeSessionService] ReportAsync failed for {code}: {ex.Message}");
                LastError = new ApiErrorResponse { message = ex.Message };
                return ChallengeReportResult.Failed;
            }
            finally
            {
                _isCompleting = false;
            }
        }
    }
}
