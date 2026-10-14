using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Loads the active quest's product missions and what was already sent for one food, and sends new answers with
    /// the real food id (spec 2026-10-14-mission-product-report-design.md).
    /// </summary>
    public class ProductMissionService : IProductMissionService
    {
        // The backend rejects eventsLimit above 100 (GamificationProfileQueryDto @Max(100))
        public const int EventsLimit = 100;
        public const string ReportedVia = "product_detail";

        private readonly IStoreService _storeService;
        private readonly IQuestService _questService;
        private readonly IMissionService _missionService;
        private readonly IGamificationService _gamificationService;
        private readonly IMissionEventEmitter _emitter;

        private ProductMissionInputs _inputs;
        // Only the latest load may set _inputs: an older one finishing last would point Replan/Send at another food
        private int _loadVersion;
        // Same card and selection again: resend only what failed (the emitter skips sent items)
        private string _pendingKey;
        private List<PendingReportItem> _pendingItems;

        public ProductMissionService(
            IStoreService storeService,
            IQuestService questService,
            IMissionService missionService,
            IGamificationService gamificationService,
            IMissionEventEmitter emitter)
        {
            _storeService = storeService;
            _questService = questService;
            _missionService = missionService;
            _gamificationService = gamificationService;
            _emitter = emitter;
        }

        public async Task<(IReadOnlyList<ProductMissionCard> Cards, ApiErrorResponse Error)> LoadAsync(
            FoodInfoType type, string foodId, IReadOnlyCollection<string> availableKeys)
        {
            int version = ++_loadVersion;
            _inputs = null;
            _pendingKey = null;
            _pendingItems = null;
            if (string.IsNullOrEmpty(foodId))
            {
                return (Array.Empty<ProductMissionCard>(), null);
            }

            try
            {
                var (missions, progress, error) = await CheckInService.LoadActiveMissionsAsync(_storeService, _questService, _missionService);
                if (error != null)
                {
                    return (Array.Empty<ProductMissionCard>(), error);
                }

                var inputs = new ProductMissionInputs { FoodType = type, FoodId = foodId, AvailableKeys = availableKeys ?? Array.Empty<string>() };
                inputs.Missions.AddRange(missions);
                foreach (KeyValuePair<string, MissionProgress> kv in progress)
                {
                    inputs.Progress[kv.Key] = kv.Value;
                }

                List<MissionReportStep> steps = missions.SelectMany(m => m.Interaction.Steps)
                    .Where(s => ProductMissionPlanner.IdFieldOf(s) != null)
                    .ToList();
                string[] eventTypes = steps.SelectMany(ProductMissionPlanner.EventTypesOf).Distinct(StringComparer.Ordinal).ToArray();
                if (eventTypes.Length == 0)
                {
                    SetInputs(version, inputs);
                    return (Array.Empty<ProductMissionCard>(), null);
                }

                var (events, eventsError) = await CheckInService.LoadRecentEventsAsync(_gamificationService, eventTypes, EventsLimit);
                if (eventsError != null)
                {
                    return (Array.Empty<ProductMissionCard>(), eventsError);
                }

                string idField = type == FoodInfoType.Generic ? ProductMissionPlanner.GenericFoodIdField : ProductMissionPlanner.ProductIdField;
                var arrayFields = steps.Where(s => s.Type == MissionStepType.ProductChecklist && s.Checklist.IsArray && s.EventType != null)
                    .GroupBy(s => s.EventType)
                    .ToDictionary(g => g.Key, g => g.First().Checklist.ArrayField, StringComparer.Ordinal);
                foreach (UserEvent userEvent in events ?? Array.Empty<UserEvent>())
                {
                    if (userEvent?.eventType == null || userEvent.metadata == null ||
                        !string.Equals(userEvent.metadata[idField]?.Type == JTokenType.String ? (string)userEvent.metadata[idField] : null, foodId, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    DateTime at = ParseLocalTime(userEvent.timestamp);
                    if (arrayFields.TryGetValue(userEvent.eventType, out string arrayField) && userEvent.metadata[arrayField] is JArray array)
                    {
                        foreach (string value in array.Where(t => t.Type == JTokenType.String).Select(t => ((string)t).ToUpperInvariant()))
                        {
                            MarkSent(inputs, userEvent.eventType, value, at);
                        }
                    }
                    else
                    {
                        MarkSent(inputs, userEvent.eventType, string.Empty, at);
                    }
                }

                SetInputs(version, inputs);
                return (ProductMissionPlanner.Plan(inputs), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProductMissionService] LoadAsync failed: {ex.Message}");
                return (Array.Empty<ProductMissionCard>(), new ApiErrorResponse { message = ex.Message });
            }
        }

        private void SetInputs(int version, ProductMissionInputs inputs)
        {
            if (version == _loadVersion)
            {
                _inputs = inputs;
            }
        }

        public IReadOnlyList<ProductMissionCard> Replan() =>
            _inputs == null ? Array.Empty<ProductMissionCard>() : ProductMissionPlanner.Plan(_inputs);

        /// <summary>Something new is selected and, for checklists, what was sent plus the selection satisfies the rule.</summary>
        public static bool CanSend(ProductMissionCard card, IReadOnlyCollection<int> selected)
        {
            if (card == null || selected == null || !selected.Any(i => i >= 0 && i < card.Options.Count && !card.Locked.Contains(i)))
            {
                return false;
            }
            if (card.Kind != ProductMissionCardKind.Checklist)
            {
                return true;
            }
            return ChecklistRule.Counts(card.Step.Checklist, card.Locked.Concat(selected).Distinct().ToList());
        }

        public static List<PendingReportItem> BuildEvents(ProductMissionCard card, IReadOnlyCollection<int> selected)
        {
            string foodId = card.FoodId;
            // Per mission window: a later or restarted mission must not be deduplicated into an event of an older window
            string window = card.MissionStartLocal?.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-";
            var items = new List<PendingReportItem>();
            List<int> fresh = selected.Where(i => i >= 0 && i < card.Options.Count && !card.Locked.Contains(i)).Distinct().OrderBy(i => i).ToList();
            if (fresh.Count == 0)
            {
                return items;
            }

            if (card.Kind == ProductMissionCardKind.Checklist && card.Step.Checklist.IsArray)
            {
                // The rule counts distinct values on the product: send everything it has now, not only the new ones
                string[] values = card.Locked.Concat(fresh).Distinct().OrderBy(i => i).Select(i => card.Options[i].Value).ToArray();
                var metadata = BaseMetadata(card, foodId, null);
                metadata[card.Step.Checklist.ArrayField] = values;
                items.Add(Item(card.Step.EventType, metadata, foodId, string.Join("|", values), window));
                return items;
            }

            foreach (int index in fresh)
            {
                ProductMissionOption option = card.Options[index];
                items.Add(Item(option.EventType, BaseMetadata(card, foodId, option.Metadata), foodId, string.Empty, window));
            }
            return items;
        }

        public async Task<MissionReportSendResult> SendAsync(ProductMissionCard card, IReadOnlyCollection<int> selected)
        {
            if (!CanSend(card, selected) || _inputs == null)
            {
                return MissionReportSendResult.Fail(null);
            }

            string key = $"{card.MissionCode}:{card.Step.PromptKey}:{string.Join(",", selected.OrderBy(i => i))}";
            if (key != _pendingKey || _pendingItems == null)
            {
                _pendingKey = key;
                _pendingItems = BuildEvents(card, selected);
            }

            MissionReportSendResult result = await _emitter.SendAsync(_pendingItems);
            if (!result.Success)
            {
                return result;
            }

            // Only into the inputs of the same food (another product may have been loaded meanwhile)
            if (_inputs != null && _inputs.FoodType == card.FoodType && _inputs.FoodId == card.FoodId)
            {
                foreach (int index in selected.Where(i => i >= 0 && i < card.Options.Count))
                {
                    ProductMissionOption option = card.Options[index];
                    MarkSent(_inputs, option.EventType, option.Value?.ToUpperInvariant() ?? string.Empty, DateTime.Now);
                }
            }
            _pendingKey = null;
            _pendingItems = null;
            return result;
        }

        private static Dictionary<string, object> BaseMetadata(ProductMissionCard card, string foodId, IReadOnlyDictionary<string, object> optionMetadata)
        {
            var metadata = new Dictionary<string, object>
            {
                { "missionCode", card.MissionCode },
                { "reportedVia", ReportedVia }
            };
            foreach (KeyValuePair<string, object> kv in card.Step.FixedMetadata)
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
            metadata[card.IdField] = foodId;
            return metadata;
        }

        private static PendingReportItem Item(string eventType, Dictionary<string, object> metadata, string foodId, string values, string window) =>
            new PendingReportItem(new CreateClientEventRequest
            {
                eventType = eventType,
                metadata = metadata,
                // Same food, event, values and mission window: the backend keeps one, even if the user sends twice
                idempotencyKey = $"product-report:{foodId}:{eventType}:{values}:{window}"
            });

        private static void MarkSent(ProductMissionInputs inputs, string eventType, string value, DateTime at)
        {
            if (!inputs.Sent.TryGetValue(eventType, out Dictionary<string, DateTime> values))
            {
                values = new Dictionary<string, DateTime>(StringComparer.Ordinal);
                inputs.Sent[eventType] = values;
            }
            if (!values.TryGetValue(value, out DateTime latest) || at > latest)
            {
                values[value] = at;
            }
        }

        /// <summary>Event time in local time; an unreadable timestamp counts as now (keeps the option locked rather than resent).</summary>
        private static DateTime ParseLocalTime(string timestamp)
        {
            if (!string.IsNullOrEmpty(timestamp) &&
                DateTime.TryParse(timestamp, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime utc))
            {
                return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
            }
            return DateTime.Now;
        }
    }
}
