using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    public class MissionEventEmitter : IMissionEventEmitter
    {
        // Backend throttler allows 5 requests/s per route; a check-in can send many events at once.
        private const int MinRequestSpacingMs = 250;
        private const int MaxThrottleRetries = 2;
        private const int ThrottleBackoffMs = 1000;

        private readonly IEventService _eventService;
        private readonly IMealLogService _mealLogService;

        public MissionEventEmitter(IEventService eventService, IMealLogService mealLogService)
        {
            _eventService = eventService;
            _mealLogService = mealLogService;
        }

        // Tests replace it to avoid real waits.
        public Func<int, Task> Delay { get; set; } = ms => Task.Delay(ms);

        public async Task<MissionReportSendResult> SendAsync(IReadOnlyList<PendingReportItem> items)
        {
            if (items == null)
            {
                return MissionReportSendResult.Ok;
            }

            bool first = true;
            foreach (PendingReportItem item in items)
            {
                if (item == null || item.Sent)
                {
                    continue;
                }

                ApiErrorResponse error = null;
                try
                {
                    for (int attempt = 0; ; attempt++)
                    {
                        if (!first)
                        {
                            await Delay(attempt == 0 ? MinRequestSpacingMs : ThrottleBackoffMs * attempt);
                        }
                        first = false;

                        if (item.Event != null)
                        {
                            (_, error) = await _eventService.RecordClientEventAsync(item.Event);
                        }
                        else
                        {
                            (_, error) = await _mealLogService.CreateAsync(item.MealLog);
                        }

                        if (!IsThrottled(error) || attempt >= MaxThrottleRetries)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MissionEventEmitter] Send failed: {ex.Message}");
                    return MissionReportSendResult.Fail(new ApiErrorResponse { message = ex.Message });
                }

                if (error != null)
                {
                    return MissionReportSendResult.Fail(error);
                }

                item.Sent = true;
            }

            return MissionReportSendResult.Ok;
        }

        private static bool IsThrottled(ApiErrorResponse error)
        {
            return error != null
                && (error.statusCode == 429 || string.Equals(error.error, "ThrottlerException", StringComparison.OrdinalIgnoreCase));
        }
    }
}
