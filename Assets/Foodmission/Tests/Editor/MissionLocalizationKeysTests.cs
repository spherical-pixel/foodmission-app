using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionLocalizationKeysTests
    {
        private static HashSet<string> UiKeys()
        {
            string path = Path.Combine(Application.dataPath, "Foodmission/localization/CSV/UI.csv");
            var keys = new HashSet<string>();
            foreach (string line in File.ReadAllLines(path))
            {
                if (line.StartsWith("\""))
                {
                    int end = line.IndexOf('"', 1);
                    if (end > 1)
                    {
                        keys.Add(line.Substring(1, end - 1));
                    }
                }
            }
            return keys;
        }

        [Test]
        public void EveryKeyTheCheckInAndCatalogShow_ExistsInUiCsv()
        {
            HashSet<string> keys = UiKeys();
            var needed = new SortedSet<string>
            {
                "MISSION_CHECKIN_Q_MEALS", "MISSION_REPORT_SUMMARY_TITLE", "MISSION_CHECKIN_UP_TO_DATE"
            };
            foreach (MissionInteraction interaction in MissionInteractionCatalog.Entries.Values)
            {
                foreach (MissionModuleLink module in interaction.AutoModules.Concat(interaction.HelperModules))
                {
                    needed.Add(module.ButtonKey);
                }
                foreach (MissionReportStep step in interaction.Steps)
                {
                    if (step.Type == MissionStepType.MealReport)
                    {
                        // Meal steps are shown as meal questions with the shared flag labels
                        IEnumerable<string> events = step.EventType != null ? new[] { step.EventType } : step.Options.Select(o => o.EventType);
                        foreach (string eventType in events)
                        {
                            needed.Add(MealFlagLabels.KeyFor(eventType) ?? "<no label for " + eventType + ">");
                        }
                        continue;
                    }
                    needed.Add(step.PromptKey);
                    foreach (MissionStepOption option in step.Options)
                    {
                        needed.Add(option.LabelKey);
                    }
                }
            }

            var missing = needed.Where(k => !keys.Contains(k)).ToList();
            Assert.IsEmpty(missing, "Missing UI.csv keys:\n" + string.Join("\n", missing));
        }
    }
}
