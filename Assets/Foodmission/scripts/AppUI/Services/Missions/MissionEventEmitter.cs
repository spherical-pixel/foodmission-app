using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    public class MissionEventEmitter : IMissionEventEmitter
    {
        private readonly IEventService _eventService;
        private readonly IMealLogService _mealLogService;

        public MissionEventEmitter(IEventService eventService, IMealLogService mealLogService)
        {
            _eventService = eventService;
            _mealLogService = mealLogService;
        }

        public async Task<MissionReportSendResult> SendAsync(IReadOnlyList<PendingReportItem> items)
        {
            if (items == null)
            {
                return MissionReportSendResult.Ok;
            }

            foreach (PendingReportItem item in items)
            {
                if (item == null || item.Sent)
                {
                    continue;
                }

                ApiErrorResponse error;
                try
                {
                    if (item.Event != null)
                    {
                        (_, error) = await _eventService.RecordClientEventAsync(item.Event);
                    }
                    else
                    {
                        (_, error) = await _mealLogService.CreateAsync(item.MealLog);
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
    }
}
