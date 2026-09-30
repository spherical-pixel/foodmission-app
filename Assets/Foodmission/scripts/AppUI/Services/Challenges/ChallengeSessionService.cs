using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public string ActiveChallengeCode { get; private set; }

        public ChallengeSessionService(IChallengeCompletionService completion)
        {
            _completion = completion;
        }

        public void Begin(string challengeCode, ChallengeInteraction interaction)
        {
            Cancel();
            if (string.IsNullOrWhiteSpace(challengeCode) || interaction == null || !interaction.AutoCompletes ||
                interaction.Trigger == ChallengeCompletionTrigger.ComparisonDone)
            {
                return;
            }

            ActiveChallengeCode = challengeCode.Trim();
            _interaction = interaction;
        }

        public void Cancel()
        {
            ActiveChallengeCode = null;
            _interaction = null;
            _reportedItems.Clear();
        }

        public async Task ReportAsync(ChallengeCompletionTrigger trigger, string itemKey)
        {
            if (ActiveChallengeCode == null || _interaction == null || trigger != _interaction.Trigger || string.IsNullOrEmpty(itemKey))
            {
                return;
            }

            if (trigger == ChallengeCompletionTrigger.FoodFactRead &&
                !string.Equals(itemKey.Trim(), _interaction.FoodFactCode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _reportedItems.Add(itemKey.Trim());
            if (_reportedItems.Count < Math.Max(1, _interaction.RequiredCount) || _isCompleting)
            {
                return;
            }

            string code = ActiveChallengeCode;
            _isCompleting = true;
            try
            {
                ChallengeCompletionResult result = await _completion.CompleteAsync(code);
                if (!result.Success)
                {
                    Debug.LogWarning($"[ChallengeSessionService] Auto-completion of {code} failed; session kept for retry");
                    return;
                }

                if (string.Equals(ActiveChallengeCode, code, StringComparison.OrdinalIgnoreCase))
                {
                    Cancel();
                }
                AutoCompleted?.Invoke(code, result.Reward);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChallengeSessionService] ReportAsync failed for {code}: {ex.Message}");
            }
            finally
            {
                _isCompleting = false;
            }
        }
    }
}
