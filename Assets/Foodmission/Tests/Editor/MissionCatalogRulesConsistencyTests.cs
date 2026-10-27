using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json.Linq;
using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// Every counter of every backend mission rule must be satisfiable by an event or meal log the catalog can produce
    /// (event type, where-metadata and distinctBy field). Regenerate Data/rules-coverage.json when backend rules change.
    /// </summary>
    [TestFixture]
    public class MissionCatalogRulesConsistencyTests
    {
        private sealed class Signature
        {
            public string EventType;
            public readonly Dictionary<string, object> Metadata = new();
            public readonly HashSet<string> Fields = new();
            /// <summary>Fields whose value other events can share (a meal log's id, a day), so rule groupBy can join them.</summary>
            public readonly HashSet<string> SharedFields = new();
            /// <summary>Array metadata the catalog may send (field → values), e.g. colours.</summary>
            public readonly Dictionary<string, string[]> Arrays = new();
        }

        private static JObject LoadRules()
        {
            string path = Path.Combine(Application.dataPath, "Foodmission/Tests/Editor/Data/rules-coverage.json");
            return JObject.Parse(File.ReadAllText(path));
        }

        private static IEnumerable<JObject> Missions() => ((JArray)LoadRules()["missions"]).Cast<JObject>();

        private static string StripMetadata(string path) =>
            path.StartsWith("metadata.", StringComparison.Ordinal) ? path.Substring("metadata.".Length) : path;

        private static Signature EventSignature(string eventType, IReadOnlyDictionary<string, object> fixedMetadata,
            IReadOnlyDictionary<string, object> optionMetadata, string distinctField, bool providesDay)
        {
            var s = new Signature { EventType = eventType };
            foreach (var kv in fixedMetadata)
            {
                s.Metadata[kv.Key] = kv.Value;
                s.Fields.Add(kv.Key);
            }
            if (optionMetadata != null)
            {
                foreach (var kv in optionMetadata)
                {
                    s.Metadata[kv.Key] = kv.Value;
                    s.Fields.Add(kv.Key);
                }
            }
            if (!string.IsNullOrEmpty(distinctField))
            {
                s.Fields.Add(distinctField);
            }
            if (providesDay)
            {
                s.Fields.Add("dayBucket");
                s.SharedFields.Add("dayBucket");
            }
            return s;
        }

        private static Signature MealSignature(string eventType, IReadOnlyList<string> mealTypes)
        {
            var s = new Signature { EventType = eventType };
            // Backend dates meal-log events at the meal, so their dayBucket is the meal day too.
            // The check-in sends every flag of one meal in a single meal log, so they share its ids.
            string[] fields = { "mealLogId", "mealId", "mealType", "mealDayBucket", "dayBucket", "body.typeOfMeal" };
            s.Fields.UnionWith(fields);
            s.SharedFields.UnionWith(fields);
            if (mealTypes.Count > 0)
            {
                // MEAL_LOGGED carries it as body.typeOfMeal, flag events as mealType
                s.Metadata["mealType"] = mealTypes.ToArray();
                s.Metadata["body.typeOfMeal"] = mealTypes.ToArray();
            }
            return s;
        }

        private static List<Signature> Signatures(MissionInteraction interaction)
        {
            var list = new List<Signature>();
            foreach (MissionReportStep step in interaction.Steps)
            {
                switch (step.Type)
                {
                    case MissionStepType.Count:
                    case MissionStepType.YesNo:
                        list.Add(EventSignature(step.EventType, step.FixedMetadata, null, step.DistinctField, false));
                        break;
                    case MissionStepType.DayPicker:
                        list.Add(EventSignature(step.EventType, step.FixedMetadata, null, step.DistinctField, true));
                        break;
                    case MissionStepType.OptionPicker:
                        foreach (MissionStepOption option in step.Options)
                        {
                            list.Add(EventSignature(option.EventType, step.FixedMetadata, option.Metadata, step.DistinctField, false));
                        }
                        break;
                    case MissionStepType.MealReport:
                        var flags = new List<string>();
                        if (step.EventType != null)
                        {
                            flags.Add(step.EventType);
                        }
                        flags.AddRange(step.Options.Select(o => o.EventType));
                        flags.Add(ClientEventTypes.MealLogged);
                        foreach (string flag in flags)
                        {
                            list.Add(MealSignature(flag, step.MealTypes));
                        }
                        break;
                    case MissionStepType.ProductChecklist:
                    case MissionStepType.DayChecklist:
                        list.AddRange(ChecklistSignatures(step));
                        break;
                    case MissionStepType.DayQuestion:
                        // One OTHER meal log per day: rules restricted to main meals must not count it
                        var dayFlags = step.Options.Where(o => o.EventType != null).Select(o => o.EventType).ToList();
                        dayFlags.Add(ClientEventTypes.MealLogged);
                        foreach (string flag in dayFlags)
                        {
                            list.Add(MealSignature(flag, new[] { "OTHER" }));
                        }
                        break;
                }
            }
            return list;
        }

        /// <summary>
        /// Checklist items share their key: every event of one product carries the same productId, and every event of
        /// one day lands in the same dayBucket.
        /// </summary>
        private static IEnumerable<Signature> ChecklistSignatures(MissionReportStep step)
        {
            ChecklistSpec spec = step.Checklist;
            string key = step.Type == MissionStepType.ProductChecklist ? "productId" : "dayBucket";
            IEnumerable<string> eventTypes = spec.IsArray ? new[] { step.EventType } : spec.Options.Select(o => o.EventType);
            foreach (string eventType in eventTypes)
            {
                var s = new Signature { EventType = eventType };
                s.Fields.Add(key);
                s.SharedFields.Add(key);
                if (spec.IsArray)
                {
                    s.Arrays[spec.ArrayField] = Enumerable.Range(0, spec.Options.Count).Select(spec.ValueOf).ToArray();
                }
                yield return s;
            }
        }

        private static bool Satisfies(Signature s, JObject counter)
        {
            var events = counter["event"] != null
                ? new[] { (string)counter["event"] }
                : ((JArray)counter["anyOf"]).Select(t => (string)t).ToArray();
            if (!events.Contains(s.EventType))
            {
                return false;
            }

            if (counter["where"] is JObject where)
            {
                foreach (var prop in where.Properties())
                {
                    string key = StripMetadata(prop.Name);
                    // An array means "any of these": every value the catalog may send must be one of them
                    var expected = prop.Value is JArray options
                        ? options.Select(o => o.ToString()).ToArray()
                        : new[] { prop.Value.ToString() };
                    if (!s.Metadata.TryGetValue(key, out object value))
                    {
                        return false;
                    }
                    var sent = value is string[] many ? many : new[] { value?.ToString() };
                    if (!sent.All(expected.Contains))
                    {
                        return false;
                    }
                }
            }

            string distinctBy = (string)counter["distinctBy"];
            if (!string.IsNullOrEmpty(distinctBy) && !s.Fields.Contains(StripMetadata(distinctBy)))
            {
                return false;
            }

            return !(counter["groupBy"] is JObject groupBy) || s.Fields.Contains(StripMetadata((string)groupBy["key"]));
        }

        /// <summary>
        /// groupBy without "of": some item (same key value) must collect minDistinct event types, including every "require".
        /// Only signatures whose key can be shared (meal log, day) can join; a synthetic productId is unique per event.
        /// </summary>
        private static bool SatisfiesGroup(List<Signature> signatures, JObject counter)
        {
            var groupBy = (JObject)counter["groupBy"];
            string key = StripMetadata((string)groupBy["key"]);
            int minDistinct = (int)groupBy["minDistinct"];
            var matching = signatures.Where(s => Satisfies(s, counter)).ToList();

            if (groupBy["of"] != null)
            {
                // Values of one array field joined across the item's events, limited to the allowed values
                string of = StripMetadata((string)groupBy["of"]);
                var allowed = groupBy["values"] is JArray v ? new HashSet<string>(v.Select(t => ((string)t).ToUpperInvariant())) : null;
                var values = matching
                    .Where(s => s.SharedFields.Contains(key) && s.Arrays.ContainsKey(of))
                    .SelectMany(s => s.Arrays[of])
                    .Select(x => x.ToUpperInvariant())
                    .Where(x => allowed == null || allowed.Contains(x))
                    .Distinct()
                    .Count();
                return values >= minDistinct;
            }

            var require = groupBy["require"] is JArray r ? r.Select(t => (string)t).ToArray() : Array.Empty<string>();

            var shared = matching.Where(s => s.SharedFields.Contains(key)).Select(s => s.EventType).Distinct().ToList();
            if (shared.Count >= minDistinct && require.All(shared.Contains))
            {
                return true;
            }

            return minDistinct <= 1 && matching.Any(s => require.All(t => t == s.EventType));
        }

        [Test]
        public void EveryRuleCounter_IsProducibleByTheCatalog()
        {
            var failures = new List<string>();
            foreach (JObject mission in Missions())
            {
                string code = (string)mission["code"];
                if ((string)mission["shape"] == "undecided")
                {
                    continue;
                }

                MissionInteraction interaction = MissionInteractionCatalog.Get(code);
                if (!interaction.CanReport)
                {
                    failures.Add($"{code}: has a rule but cannot be reported");
                    continue;
                }

                List<Signature> signatures = Signatures(interaction);
                foreach (var counter in ((JObject)mission["rule"]["counters"]).Properties())
                {
                    var rule = (JObject)counter.Value;
                    bool producible = rule["groupBy"] != null
                        ? SatisfiesGroup(signatures, rule)
                        : signatures.Any(s => Satisfies(s, rule));
                    if (!producible)
                    {
                        failures.Add($"{code}.{counter.Name}");
                    }
                }
            }

            Assert.IsEmpty(failures, "Counters not producible by the catalog:\n" + string.Join("\n", failures));
        }

        [Test]
        public void UndecidedRules_MatchPendingRuleEntries()
        {
            var undecided = Missions().Where(m => (string)m["shape"] == "undecided").Select(m => (string)m["code"]).ToArray();

            CollectionAssert.AreEquivalent(MissionInteractionCatalogTests.PendingRuleCodes, undecided);
        }

        [Test]
        public void RulesFile_ListsEveryCatalogCode()
        {
            var codes = Missions().Select(m => (string)m["code"]).ToArray();

            CollectionAssert.AreEquivalent(MissionInteractionCatalog.Entries.Keys.ToArray(), codes);
        }
    }
}
