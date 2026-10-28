using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    /// <summary>Texts of the day swap question and the result lines (spec 2026-10-27 swaps), in 9 languages.</summary>
    [TestFixture]
    public class CheckInSwapLocalizationTests
    {
        private static readonly string[] Keys =
        {
            MissionSwaps.PromptKey, CheckInResultLines.DoneKey, CheckInResultLines.FailedKey, CheckInResultLines.SwapsKey,
            CheckInResultLines.LevelsKey, CheckInResultLines.DaysKey, CheckInResultLines.PercentKey
        };

        [Test]
        public void AllKeys_HaveNineNonEmptyTranslations()
        {
            Dictionary<string, string[]> csv = UiCsv.Read();
            var problems = new List<string>();
            foreach (string key in Keys)
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
            foreach (string key in Keys.Where(csv.ContainsKey))
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
}
