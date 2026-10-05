using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ShareLocalizationKeysTests
    {
        [Test]
        public void ShareKeys_ExistInUiCsv()
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

            foreach (string key in new[] { "SHARE_FOOTER", "SHARE_SUBJECT_FOOD_FACT", "SHARE_SUBJECT_QUIZ", "SHARE", "SHARE_APP", "SHARE_APP_MESSAGE", "SHARE_SUBJECT_APP" })
            {
                Assert.IsTrue(keys.Contains(key), $"Missing UI key {key}");
            }
        }
    }
}
