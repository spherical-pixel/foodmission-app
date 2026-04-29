using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ProfileUpdateModelsTests
    {
        [Test]
        public void ToJson_OmitsNullStringFields()
        {
            var request = new ProfileUpdateRequest { gender = "MALE" };

            string json = request.ToJson();

            StringAssert.Contains("\"gender\"", json);
            StringAssert.DoesNotContain("activityLevel", json);
            StringAssert.DoesNotContain("educationLevel", json);
            StringAssert.DoesNotContain("annualIncome", json);
            StringAssert.DoesNotContain("preferences", json);
            StringAssert.DoesNotContain("settings", json);
        }

        [Test]
        public void ToJson_IncludesSettingsWhenSet()
        {
            var request = new ProfileUpdateRequest
            {
                settings = new UserSettingsDto
                {
                    theme = "dark",
                    scale = "medium",
                    font = "roboto",
                    soundVolume = 80,
                    musicVolume = 60,
                    pushNotificationsEnabled = false,
                    backgroundPattern = true
                }
            };

            string json = request.ToJson();

            StringAssert.Contains("\"settings\"", json);
            StringAssert.Contains("\"theme\"", json);
            StringAssert.Contains("dark", json);
        }

        [Test]
        public void ToJson_IncludesPreferencesWhenSet()
        {
            var request = new ProfileUpdateRequest
            {
                preferences = new ProfileUpdatePreferences
                {
                    dietaryPreference = "VEGAN"
                }
            };

            string json = request.ToJson();

            StringAssert.Contains("\"preferences\"", json);
            StringAssert.Contains("VEGAN", json);
            StringAssert.DoesNotContain("shoppingResponsibility", json);
        }

        [Test]
        public void ToJson_OmitsNullPreferencesFields()
        {
            var request = new ProfileUpdateRequest
            {
                preferences = new ProfileUpdatePreferences
                {
                    shoppingResponsibility = "SHARED"
                    // dietaryPreference is null — should be omitted
                }
            };

            string json = request.ToJson();

            StringAssert.Contains("shoppingResponsibility", json);
            StringAssert.DoesNotContain("dietaryPreference", json);
        }

        [Test]
        public void ToJson_IncludesLanguageWhenSet()
        {
            var request = new ProfileUpdateRequest { language = "es" };

            string json = request.ToJson();

            StringAssert.Contains("\"language\"", json);
            StringAssert.Contains("es", json);
        }

        [Test]
        public void ToJson_OnlySettingsRequest_ContainsOnlySettingsKey()
        {
            var request = new ProfileUpdateRequest
            {
                settings = new UserSettingsDto { theme = "light" }
            };

            string json = request.ToJson();

            StringAssert.Contains("\"settings\"", json);
            StringAssert.DoesNotContain("gender", json);
            StringAssert.DoesNotContain("preferences", json);
            StringAssert.DoesNotContain("language", json);
        }
    }
}
