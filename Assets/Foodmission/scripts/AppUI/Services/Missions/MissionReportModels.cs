using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>User answer to one <see cref="MissionReportStep"/>; only the fields of the step's type are read.</summary>
    public sealed class MissionStepAnswer
    {
        public int Count;
        public bool Confirmed;
        public readonly List<DateTime> Days = new List<DateTime>();
        public readonly Dictionary<int, int> OptionCounts = new Dictionary<int, int>();
    }

    /// <summary>One request of a manual report. Exactly one of Event / MealLog is set.</summary>
    public sealed class PendingReportItem
    {
        public CreateClientEventRequest Event { get; }
        public CreateMealLogRequest MealLog { get; }
        public bool Sent { get; set; }

        public PendingReportItem(CreateClientEventRequest clientEvent)
        {
            Event = clientEvent;
        }

        public PendingReportItem(CreateMealLogRequest mealLog)
        {
            MealLog = mealLog;
        }
    }

    public sealed class MissionReportSendResult
    {
        public static readonly MissionReportSendResult Ok = new MissionReportSendResult(true, null);

        public bool Success { get; }
        public ApiErrorResponse Error { get; }

        private MissionReportSendResult(bool success, ApiErrorResponse error)
        {
            Success = success;
            Error = error;
        }

        public static MissionReportSendResult Fail(ApiErrorResponse error) =>
            new MissionReportSendResult(false, error ?? new ApiErrorResponse());
    }

    public sealed class MissionReportOutcome
    {
        public float Progress { get; }
        public bool Completed { get; }

        public MissionReportOutcome(float progress, bool completed)
        {
            Progress = progress;
            Completed = completed;
        }
    }
}
