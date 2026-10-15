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
                foreach (MissionModuleLink module in interaction.AutoModules)
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

        [Test]
        public void MealStepHints_ExistInUiCsv()
        {
            HashSet<string> keys = UiKeys();
            int hints = 0;
            foreach (MissionInteraction interaction in MissionInteractionCatalog.Entries.Values)
            {
                foreach (MissionReportStep step in interaction.Steps)
                {
                    string hint = MealStepHints.KeyFor(step);
                    if (hint != null)
                    {
                        hints++;
                        Assert.IsTrue(keys.Contains(hint), $"Missing UI.csv key {hint}");
                    }
                }
            }
            Assert.Greater(hints, 0);
            Assert.IsNull(MealStepHints.KeyFor(null));
        }

        [Test]
        public void ChallengeProductReportKeys_ExistInUiCsv()
        {
            HashSet<string> keys = UiKeys();
            foreach (string key in new[] { "CHALLENGE_PRODUCT_DONE", "CHALLENGE_PRODUCT_COUNTED", "CHALLENGE_PRODUCT_PROGRESS", "CHALLENGE_PRODUCT_NOT_HERE" })
            {
                Assert.IsTrue(keys.Contains(key), $"Missing UI.csv key {key}");
            }
        }

        [Test]
        public void MissionProductReportKeys_ExistInUiCsv()
        {
            HashSet<string> keys = UiKeys();
            foreach (string key in new[] { "MISSION_PRODUCT_PROGRESS", "MISSION_PRODUCT_SEND", "MISSION_PRODUCT_SUGGESTED", "MISSION_CHECKIN_FROM_PRODUCTS", "MISSION_BTN_FIND_PRODUCT" })
            {
                Assert.IsTrue(keys.Contains(key), $"Missing UI.csv key {key}");
            }
        }

        [Test]
        public void MissionFailureKeys_ExistInUiCsv()
        {
            HashSet<string> keys = UiKeys();
            foreach (string key in new[]
            {
                "MISSION_STATUS_FAILED", "MISSION_FAILED_BADGE", "MISSION_FAILED_MESSAGE", "MISSION_FAILED_NOTICE",
                "MISSION_BTN_RESTART", "MISSION_RESTART_CONFIRM_TITLE", "MISSION_RESTART_CONFIRM_MESSAGE", "CHALLENGE_BTN_LATER"
            })
            {
                Assert.IsTrue(keys.Contains(key), $"Missing UI.csv key {key}");
            }
        }

        [Test]
        public void MissionStartKeys_ExistInUiCsv()
        {
            HashSet<string> keys = UiKeys();
            foreach (string key in new[] { "MISSION_START", "MISSION_NOT_STARTED_HINT", "MISSION_START_MEAT_QUESTION", "MISSION_START_MEAT_NONE", "MISSION_START_A11Y", "MISSION_START_CONFIRM", "MISSION_RESTART_FAILED_CONFIRM", "MISSION_BTN_RESTART", "TXT_OK", "TXT_CANCEL" })
            {
                Assert.IsTrue(keys.Contains(key), key + " missing in UI.csv");
            }
        }
    }
}
