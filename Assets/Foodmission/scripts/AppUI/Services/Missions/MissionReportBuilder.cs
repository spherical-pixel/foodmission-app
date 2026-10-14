using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>Pure conversion of event-step answers into client events (spec §3.3). Meal steps are handled by CheckInBuilder.</summary>
    public static class MissionReportBuilder
    {
        public const string ReportedVia = "manual_nutri";
        public const string ProductIdField = "productId";

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

                    case MissionStepType.ProductChecklist:
                        for (int p = 0; p < answer.Products.Count && p < step.MaxCount; p++)
                        {
                            HashSet<int> selected = answer.Products[p];
                            if (!ChecklistRule.Counts(step.Checklist, selected))
                            {
                                continue;
                            }
                            // One id per product, so the backend groups its facts (rule groupBy productId)
                            string productId = $"manual-{reportId}-p{indexOffset + items.Count}";
                            if (step.Checklist.IsArray)
                            {
                                AddEvent(items, indexOffset, missionCode, reportId, step.EventType, step, new Dictionary<string, object>
                                {
                                    { ProductIdField, productId },
                                    { step.Checklist.ArrayField, ChecklistRule.ValuesOf(step.Checklist, selected) }
                                });
                            }
                            else
                            {
                                foreach (int option in selected.Where(o => o >= 0 && o < step.Checklist.Options.Count).OrderBy(o => o))
                                {
                                    AddEvent(items, indexOffset, missionCode, reportId, step.Checklist.Options[option].EventType, step,
                                        new Dictionary<string, object> { { ProductIdField, productId } });
                                }
                            }
                        }
                        break;

                    case MissionStepType.DayChecklist:
                        foreach (KeyValuePair<DateTime, HashSet<int>> day in answer.DayValues.Where(d => allowedDays.Contains(d.Key.Date)).OrderBy(d => d.Key))
                        {
                            string[] values = ChecklistRule.ValuesOf(step.Checklist, day.Value);
                            if (values.Length == 0)
                            {
                                continue;
                            }
                            DateTime moment = MissionReportDays.EventTimestampFor(day.Key.Date, missionStartLocal, nowLocal);
                            AddEvent(items, indexOffset, missionCode, reportId, step.EventType, step,
                                new Dictionary<string, object> { { step.Checklist.ArrayField, values } }, moment.ToUniversalTime().ToString("o"));
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
