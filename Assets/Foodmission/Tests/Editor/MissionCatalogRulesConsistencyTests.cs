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
        }

        /// <summary>
        /// Counters the app can't satisfy for past days with backend (v0.3.1): they count distinct server
        /// dayBucket (creation day), so back-dated meal logs land on today. Agreed with the user on 2026-10-01.
        /// Remove an entry when backend switches that rule to mealDayBucket (the test fails to remind you).
        /// </summary>
        private static readonly string[] KnownServerDayBucketGaps =
        {
            "M.A6.2.highFibreBreakfastDays", "M.B5.5.foodSavingDays", "M.B6.1.proteinDays", "M.B6.2.fruitVegDays", "M.B6.4.wholegrainDays"
        };

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
            }
            return s;
        }

        private static Signature MealSignature(string eventType, string fixedMealType)
        {
            var s = new Signature { EventType = eventType };
            s.Fields.UnionWith(new[] { "mealLogId", "mealId", "mealType", "mealDayBucket" });
            if (fixedMealType != null)
            {
                s.Metadata["mealType"] = fixedMealType;
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
                            list.Add(MealSignature(flag, step.FixedMealType));
                        }
                        break;
                }
            }
            return list;
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
                    if (!s.Metadata.TryGetValue(key, out object value) || !string.Equals(value?.ToString(), prop.Value.ToString(), StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }

            string distinctBy = (string)counter["distinctBy"];
            return string.IsNullOrEmpty(distinctBy) || s.Fields.Contains(StripMetadata(distinctBy));
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
                    if (!signatures.Any(s => Satisfies(s, (JObject)counter.Value)))
                    {
                        failures.Add($"{code}.{counter.Name}");
                    }
                }
            }

            var unexpected = failures.Except(KnownServerDayBucketGaps).ToList();
            var fixedGaps = KnownServerDayBucketGaps.Except(failures).ToList();
            Assert.IsEmpty(unexpected, "Counters not producible by the catalog:\n" + string.Join("\n", unexpected));
            Assert.IsEmpty(fixedGaps, "Backend fixed these dayBucket gaps, remove them from KnownServerDayBucketGaps:\n" + string.Join("\n", fixedGaps));
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
