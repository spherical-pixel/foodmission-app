using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    /// <summary>Texts of the check-in day questions and the named missing-days nudge (spec 2026-10-27), in 9 languages.</summary>
    [TestFixture]
    public class CheckInDayQuestionLocalizationTests
    {
        private static readonly string[] Keys =
        {
            "MISSION_QDAY_NONE", "MISSION_CHECKIN_Q_DAY", "MISSION_CHECKIN_DAY_REQUIRED_HINT", "MISSION_CHECKIN_DAY_NOTHING_LEFT",
            "MISSION_NUDGE_MISSING_DAYS_FOR", "MISSION_NUDGE_MISSING_DAYS_FOR_MANY", "MISSION_SECURED_MESSAGE", "MISSION_SECURED_CHECKIN"
        };

        private static IEnumerable<string> AllKeys() =>
            Keys.Concat(MissionInteractionCatalog.Entries.Values
                .SelectMany(i => i.Steps)
                .Where(s => s.Type == MissionStepType.DayQuestion)
                .SelectMany(s => s.OptionLabelKeys().Prepend(s.PromptKey)))
                .Distinct();

        [Test]
        public void AllDayQuestionKeys_HaveNineNonEmptyTranslations()
        {
            Dictionary<string, string[]> csv = UiCsv.Read();
            var problems = new List<string>();
            foreach (string key in AllKeys())
            {
                if (!csv.TryGetValue(key, out string[] row) || row.Length < 11)
                {
                    problems.Add(key + ": missing");
                    continue;
                }
                for (int lang = 2; lang < 11; lang++)
                {
                    if (string.IsNullOrWhiteSpace(row[lang]))
                    {
                        problems.Add($"{key}: empty column {lang}");
                    }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void Placeholders_MatchEnglishInEveryLanguage()
        {
            Dictionary<string, string[]> csv = UiCsv.Read();
            var problems = new List<string>();
            foreach (string key in AllKeys().Where(csv.ContainsKey))
            {
                string[] row = csv[key];
                var english = Regex.Matches(row[2], @"\{\d\}").Select(m => m.Value).OrderBy(v => v).ToList();
                for (int lang = 3; lang < row.Length && lang < 11; lang++)
                {
                    var translated = Regex.Matches(row[lang], @"\{\d\}").Select(m => m.Value).OrderBy(v => v).ToList();
                    if (!english.SequenceEqual(translated))
                    {
                        problems.Add($"{key}: column {lang}");
                    }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }

    internal static class DayQuestionStepKeys
    {
        /// <summary>Label keys of the step's chips.</summary>
        public static IEnumerable<string> OptionLabelKeys(this MissionReportStep step) => step.Options.Select(o => o.LabelKey);
    }
}
