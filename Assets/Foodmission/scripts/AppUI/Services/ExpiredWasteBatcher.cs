using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;

namespace eu.foodmission.platform
{
    public class ExpiredWasteBatcher : IExpiredWasteBatcher
    {
        private readonly IPantryService _pantryService;
        private readonly INotificationService _notificationService;
        private readonly IChallengeSessionService _challengeSession;

        public ExpiredWasteBatcher(
            IPantryService pantryService,
            INotificationService notificationService = null,
            IChallengeSessionService challengeSession = null)
        {
            _pantryService = pantryService;
            _notificationService = notificationService;
            _challengeSession = challengeSession ?? App.current?.services?.GetService<IChallengeSessionService>();
        }

        public async Task<(int WastedCount, ApiErrorResponse Error)> WasteAsync(IReadOnlyCollection<string> pantryItemIds)
        {
            if (pantryItemIds == null || pantryItemIds.Count == 0)
            {
                return (0, null);
            }

            var request = new BatchWasteRequest
            {
                items = pantryItemIds.Select(id => new BatchWasteItemRequest { pantryItemId = id }).ToArray()
            };

            BatchWasteResult result;
            ApiErrorResponse error;
            try
            {
                (result, error) = await _pantryService.BatchWasteAsync(request);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExpiredWasteBatcher] WasteAsync failed: {ex.Message}");
                return (0, null);
            }

            if (result?.successes != null)
            {
                foreach (FoodWaste created in result.successes)
                {
                    if (!string.IsNullOrEmpty(created?.pantryItemId))
                    {
                        _notificationService?.CancelPantryReminder(created.pantryItemId);
                    }
                    _ = _challengeSession?.ReportAsync(ChallengeCompletionTrigger.FoodWasteLogged, created?.id ?? Guid.NewGuid().ToString());
                }
            }

            // Partial failures come back as HTTP 200 with per-item errors.
            if (error == null && result?.errors != null && result.errors.Length > 0)
            {
                error = new ApiErrorResponse
                {
                    message = string.Join("\n", result.errors
                        .Where(e => !string.IsNullOrEmpty(e?.error))
                        .Select(e => e.error))
                };
            }

            return (result?.successCount ?? 0, error);
        }
    }
}
