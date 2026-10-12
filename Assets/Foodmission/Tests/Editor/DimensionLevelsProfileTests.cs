using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class DimensionLevelsProfileTests
    {
        [Test]
        public void ProfileResponse_LegacyPreferencesDimensionLevels_AreIgnored()
        {
            const string json = "{\"id\":\"u1\",\"preferences\":{\"motivation\":\"HEALTH\",\"dimensionLevels\":{\"DIET_CHANGES\":\"ADVANCED\"}}}";

            ProfileResponse profile = JsonConvert.DeserializeObject<ProfileResponse>(json);

            Assert.AreEqual("HEALTH", profile.preferences.motivation);
        }

        [Test]
        public void ProfileUpdateRequest_SerializesDimensionLevelsAtRootOnlyWhenSet()
        {
            var withLevels = new ProfileUpdateRequest
            {
                dimensionLevels = new[] { new DimensionLevelEntry(DimensionCode.Packaging, "INTERMEDIATE") }
            };
            var without = new ProfileUpdateRequest { preferences = new ProfileUpdatePreferences { motivation = "HEALTH" } };

            StringAssert.Contains("\"dimensionLevels\":[{\"dimensionCode\":\"PACKAGING\",\"level\":\"INTERMEDIATE\"}]", withLevels.ToJson());
            StringAssert.DoesNotContain("preferences", withLevels.ToJson());
            StringAssert.DoesNotContain("dimensionLevels", without.ToJson());
        }
    }
}
