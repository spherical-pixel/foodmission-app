using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class DimensionLevelsProfileTests
    {
        [Test]
        public void ProfileResponse_ReadsDimensionLevelsMap()
        {
            const string json = "{\"id\":\"u1\",\"preferences\":{\"dimensionLevels\":{\"DIET_CHANGES\":\"ADVANCED\",\"FOOD_WASTE\":\"BEGINNER\"}}}";

            ProfileResponse profile = JsonConvert.DeserializeObject<ProfileResponse>(json);

            Assert.AreEqual("ADVANCED", profile.preferences.dimensionLevels["DIET_CHANGES"]);
            Assert.AreEqual(2, DimensionLevels.FromMap(profile.preferences.dimensionLevels).Length);
        }

        [TestCase("[\"ADVANCED\"]")]
        [TestCase("\"ADVANCED\"")]
        [TestCase("42")]
        [TestCase("null")]
        public void ProfileResponse_MalformedDimensionLevels_DeserializesWithoutThrowing(string value)
        {
            string json = "{\"id\":\"u1\",\"preferences\":{\"motivation\":\"HEALTH\",\"dimensionLevels\":" + value + "}}";

            ProfileResponse profile = JsonConvert.DeserializeObject<ProfileResponse>(json);

            Assert.AreEqual("HEALTH", profile.preferences.motivation);
            Assert.AreEqual(0, DimensionLevels.FromMap(profile.preferences.dimensionLevels).Length);
        }

        [Test]
        public void ProfileResponse_MalformedDimensionLevels_NonStringValuesAreSkipped()
        {
            const string json = "{\"preferences\":{\"dimensionLevels\":{\"DIET_CHANGES\":1,\"PACKAGING\":{\"x\":1},\"FOOD_WASTE\":\"ADVANCED\"}}}";

            ProfileResponse profile = JsonConvert.DeserializeObject<ProfileResponse>(json);

            DimensionLevelEntry[] entries = DimensionLevels.FromMap(profile.preferences.dimensionLevels);
            Assert.AreEqual(1, entries.Length);
            Assert.AreEqual(DimensionCode.FoodWaste, entries[0].dimensionCode);
        }

        [Test]
        public void ProfileUpdateRequest_SerializesDimensionLevelsOnlyWhenSet()
        {
            var withLevels = new ProfileUpdateRequest
            {
                preferences = new ProfileUpdatePreferences
                {
                    dimensionLevels = new Dictionary<string, string> { { "PACKAGING", "INTERMEDIATE" } }
                }
            };
            var without = new ProfileUpdateRequest { preferences = new ProfileUpdatePreferences { motivation = "HEALTH" } };

            StringAssert.Contains("\"dimensionLevels\":{\"PACKAGING\":\"INTERMEDIATE\"}", withLevels.ToJson());
            StringAssert.DoesNotContain("dimensionLevels", without.ToJson());
        }
    }
}
