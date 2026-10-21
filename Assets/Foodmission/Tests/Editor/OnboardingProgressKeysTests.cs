using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class OnboardingProgressKeysTests
    {
        [Test]
        public void AllOnboardingProgressKeys_ExistInUiCsv()
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

            var expected = new List<string>
            {
                OnboardingProgressTexts.SkipKey,
                OnboardingProgressTexts.SkipStepKey,
                OnboardingProgressTexts.MinutesKey,
                OnboardingProgressTexts.OptionalKey,
                OnboardingProgressTexts.CloseMessageKey
            };
            foreach (OnboardingStep step in Enum.GetValues(typeof(OnboardingStep)))
            {
                expected.Add(OnboardingProgressTexts.StepTitleKey(step));
                expected.Add(OnboardingProgressTexts.NutriMessageKey(step));
            }
            foreach (OnboardingStepState state in Enum.GetValues(typeof(OnboardingStepState)))
            {
                expected.Add(OnboardingProgressTexts.StateKey(state));
            }

            foreach (string key in expected)
            {
                Assert.IsTrue(keys.Contains(key), $"Missing UI key {key}");
            }
        }

        [Test]
        public void Minutes_OnlyAvatarHasNone()
        {
            Assert.AreEqual(2, OnboardingProgressTexts.Minutes(OnboardingStep.Profile));
            Assert.AreEqual(1, OnboardingProgressTexts.Minutes(OnboardingStep.Goals));
            Assert.AreEqual(2, OnboardingProgressTexts.Minutes(OnboardingStep.Survey));
            Assert.IsNull(OnboardingProgressTexts.Minutes(OnboardingStep.Avatar));
        }
    }
}
