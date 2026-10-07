using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>Pure conversion of event-step answers into client events (spec §3.3). Meal steps are handled by CheckInBuilder.</summary>
    public static class MissionReportBuilder
    {
        public const string ReportedVia = "manual_nutri";

        public static List<PendingReportItem> Build(
            string missionCode,
            IReadOnlyList<MissionReportStep> steps,
            IReadOnlyList<MissionStepAnswer> answers,
            string reportId,
            DateTime? missionStartLocal,
            DateTime nowLocal,
            int indexOffset = 0)
        {
            var items = new List<PendingReportItem>();
            if (steps == null || answers == null)
            {
                return items;
            }

            IReadOnlyList<DateTime> allowedDays = MissionReportDays.Allowed(missionStartLocal, nowLocal);

            for (int s = 0; s < steps.Count && s < answers.Count; s++)
            {
                MissionReportStep step = steps[s];
                MissionStepAnswer answer = answers[s];
                if (step == null || answer == null)
                {
                    continue;
                }

                switch (step.Type)
                {
                    case MissionStepType.Count:
                        int count = Math.Clamp(answer.Count, 0, step.MaxCount);
                        for (int i = 0; i < count; i++)
                        {
                            AddEvent(items, indexOffset, missionCode, reportId, step.EventType, step, null);
                        }
                        break;

                    case MissionStepType.YesNo:
                        if (answer.Confirmed)
                        {
                            AddEvent(items, indexOffset, missionCode, reportId, step.EventType, step, null);
                        }
                        break;

                    case MissionStepType.DayPicker:
                        foreach (DateTime day in answer.Days.Select(d => d.Date).Where(allowedDays.Contains).Distinct().OrderBy(d => d).Take(step.MaxCount))
                        {
                            DateTime moment = MissionReportDays.EventTimestampFor(day, missionStartLocal, nowLocal);
                            AddEvent(items, indexOffset, missionCode, reportId, step.EventType, step, null, moment.ToUniversalTime().ToString("o"));
                        }
                        break;

                    case MissionStepType.OptionPicker:
                        foreach (KeyValuePair<int, int> kv in answer.OptionCounts.OrderBy(k => k.Key))
                        {
                            if (kv.Key < 0 || kv.Key >= step.Options.Count)
                            {
                                continue;
                            }
                            MissionStepOption option = step.Options[kv.Key];
                            int optionCount = Math.Clamp(kv.Value, 0, step.MaxCount);
                            for (int i = 0; i < optionCount; i++)
                            {
                                AddEvent(items, indexOffset, missionCode, reportId, option.EventType, step, option.Metadata);
                            }
                        }
                        break;

                    // MealReport steps are grouped into meal logs by the check-in (CheckInBuilder)
                }
            }

            return items;
        }

        private static void AddEvent(
            List<PendingReportItem> items,
            int indexOffset,
            string missionCode,
            string reportId,
            string eventType,
            MissionReportStep step,
            IReadOnlyDictionary<string, object> optionMetadata,
            string createdAt = null)
        {
            int index = indexOffset + items.Count;
            var metadata = new Dictionary<string, object>
            {
                { "missionCode", missionCode },
                { "reportedVia", ReportedVia }
            };
            foreach (KeyValuePair<string, object> kv in step.FixedMetadata)
            {
                metadata[kv.Key] = kv.Value;
            }
            if (optionMetadata != null)
            {
                foreach (KeyValuePair<string, object> kv in optionMetadata)
                {
                    metadata[kv.Key] = kv.Value;
                }
            }
            if (!string.IsNullOrEmpty(step.DistinctField))
            {
                metadata[step.DistinctField] = $"manual-{reportId}-{index}";
            }

            items.Add(new PendingReportItem(new CreateClientEventRequest
            {
                eventType = eventType,
                metadata = metadata,
                idempotencyKey = $"mission-report:{reportId}:{index}",
                createdAt = createdAt
            }));
        }
    }
}
