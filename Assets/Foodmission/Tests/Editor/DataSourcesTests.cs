using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class DataSourcesTests
    {
        private static string FakeLocalize(string key) => $"<{key}>";

        [Test]
        public void ForFoodType_ProductIsOpenFoodFacts_GenericIsNevo()
        {
            Assert.AreEqual(DataSource.OpenFoodFacts, DataSources.ForFoodType(FoodInfoType.Product));
            Assert.AreEqual(DataSource.Nevo, DataSources.ForFoodType(FoodInfoType.Generic));
        }

        [TestCase("auto_foodkeeper", true)]
        [TestCase("AUTO_FOODKEEPER", true)]
        [TestCase("manual", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsFoodKeeperExpiry_OnlyForBackendEstimate(string source, bool expected)
        {
            Assert.AreEqual(expected, DataSources.IsFoodKeeperExpiry(source));
        }

        [Test]
        public void EstimatedExpiryNotice_ReusesFoodKeeperNoticeAndLink()
        {
            string notice = DataSources.EstimatedExpiryNotice(FakeLocalize);

            StringAssert.StartsWith("<EXPIRY_DATE_ESTIMATED>. <DATA_SOURCE_FOODKEEPER>\n", notice);
            StringAssert.Contains("<link=\"https://www.foodsafety.gov/keep-food-safe/foodkeeper-app\">", notice);
        }

        [Test]
        public void NoticeWithLinks_OpenFoodFacts_HasNoticeThenBothLinks()
        {
            string note = DataSources.NoticeWithLinks(DataSource.OpenFoodFacts, FakeLocalize);

            Assert.AreEqual(
                "<DATA_SOURCE_OFF>\n"
                + "<link=\"https://world.openfoodfacts.org\"><u>openfoodfacts.org</u></link>  ·  "
                + "<link=\"https://opendatacommons.org/licenses/odbl/1-0/\"><u>Open Database License</u></link>",
                note);
        }

        [Test]
        public void NoticeWithLinks_EverySource_LinksAreOpenable()
        {
            foreach (var info in DataSources.All)
            {
                string note = DataSources.NoticeWithLinks(info.Source, FakeLocalize);
                foreach (var link in info.Links)
                {
                    StringAssert.Contains($"<link=\"{link.Url}\">", note, info.Name);
                    Assert.IsTrue(DataSources.IsWebUrl(link.Url), link.Url);
                }
            }
        }

        [TestCase("https://world.openfoodfacts.org", true)]
        [TestCase("http://example.com", false)]
        [TestCase("javascript:alert(1)", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsWebUrl_OnlyHttps(string url, bool expected)
        {
            Assert.AreEqual(expected, DataSources.IsWebUrl(url));
        }

        [Test]
        public void AboutMarkdown_ListsEverySourceWithNoticeAndLinks()
        {
            string md = DataSources.AboutMarkdown(FakeLocalize, "1.2.3");

            StringAssert.StartsWith("## <DATA_SOURCES>", md);
            foreach (var info in DataSources.All)
            {
                StringAssert.Contains($"**{info.Name}**", md);
                StringAssert.Contains($"<{info.NoticeKey}>", md);
                foreach (var link in info.Links)
                {
                    StringAssert.Contains($"[{link.Label}]({link.Url})", md);
                }
            }
            StringAssert.Contains("<APP_VERSION>: 1.2.3", md);
        }

        [Test]
        public void AboutMarkdown_WithoutVersion_OmitsVersionLine()
        {
            string md = DataSources.AboutMarkdown(FakeLocalize, null);

            StringAssert.DoesNotContain("<APP_VERSION>", md);
        }

        [Test]
        public void All_CoversEverySourceOnceWithHttpsLinks()
        {
            var sources = DataSources.All.Select(i => i.Source).ToList();
            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(DataSource)), sources);
            foreach (var info in DataSources.All)
            {
                Assert.IsNotEmpty(info.Links, info.Name);
                foreach (var link in info.Links)
                {
                    StringAssert.StartsWith("https://", link.Url, info.Name);
                }
            }
        }

        [Test]
        public void AllKeys_ExistInUiCsv()
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

            var expected = DataSources.All.Select(i => i.NoticeKey).ToList();
            expected.Add(DataSources.AboutTitleKey);
            expected.Add(DataSources.DataSourcesHeadingKey);
            expected.Add(DataSources.AppVersionKey);
            expected.Add(DataSources.ExpiryEstimatedKey);
            foreach (string key in expected)
            {
                Assert.IsTrue(keys.Contains(key), $"Missing UI key {key}");
            }
        }
    }
}
