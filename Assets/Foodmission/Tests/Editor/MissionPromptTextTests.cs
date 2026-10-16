using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionPromptTextTests
    {
        [Test]
        public void Question_KeepsTextUpToFirstQuestionMark()
        {
            Assert.AreEqual("¿En cuántos productos miraste el Green-Score?",
                MissionPromptText.Question("¿En cuántos productos miraste el Green-Score? Resume su impacto ambiental."));
            Assert.AreEqual("Σε πόσα προϊόντα έλεγξες το Green-Score;",
                MissionPromptText.Question("Σε πόσα προϊόντα έλεγξες το Green-Score; Συνοψίζει τον αντίκτυπο."));
            Assert.AreEqual("No question here.", MissionPromptText.Question("No question here."));
            Assert.IsNull(MissionPromptText.Question(null));
        }

        private static List<string> SplitCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool quoted = false;
            foreach (char c in line)
            {
                if (c == '"')
                {
                    quoted = !quoted;
                }
                else if (c == ',' && !quoted)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            fields.Add(current.ToString());
            return fields;
        }

        [Test]
        public void CheckInPrompts_HaveAQuestionInEveryLanguage()
        {
            // The summary shows only the question, so every prompt must start with one in every language
            var keys = new HashSet<string>();
            foreach (MissionInteraction interaction in MissionInteractionCatalog.Entries.Values)
            {
                foreach (MissionReportStep step in interaction.Steps.Where(s => s.Type != MissionStepType.MealReport))
                {
                    keys.Add(step.PromptKey);
                }
            }

            string path = Path.Combine(Application.dataPath, "Foodmission/localization/CSV/UI.csv");
            var missing = new List<string>();
            int checkedRows = 0;
            foreach (string line in File.ReadAllLines(path))
            {
                List<string> fields = SplitCsvLine(line);
                if (fields.Count < 11 || !keys.Contains(fields[0]))
                {
                    continue;
                }
                checkedRows++;
                for (int i = 2; i < fields.Count; i++)
                {
                    if (fields[i].IndexOfAny(new[] { '?', ';', ';' }) < 0)
                    {
                        missing.Add(fields[0] + " col " + i);
                    }
                }
            }

            Assert.AreEqual(keys.Count, checkedRows, "Every check-in prompt should be a single-line row in UI.csv");
            Assert.IsEmpty(missing, "Prompts without a question mark:\n" + string.Join("\n", missing));
        }
    }
}
