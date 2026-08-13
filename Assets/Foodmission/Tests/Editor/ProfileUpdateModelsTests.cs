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
                    dietaryPreference = new[] { "VEGAN" }
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

        [Test]
        public void ToJson_IncludesOnboardingSurveyWithBackendKeys()
        {
            var request = new ProfileUpdateRequest
            {
                preferences = new ProfileUpdatePreferences
                {
                    onboardingSurvey = new OnboardingSurveyData
                    {
                        weeklyMeatConsumption = "ZERO_TO_FOUR",
                        weeklyBeefConsumption = "LESS_THAN_ONCE_PER_WEEK",
                        weeklyFoodWaste = "ZERO",
                        weeklyUpfConsumption = "TEN_TO_FOURTEEN",
                        weeklyReusableOrRefill = "TEN_PLUS"
                    }
                }
            };

            string json = request.ToJson();

            StringAssert.Contains("\"weeklyMeatConsumption\":\"ZERO_TO_FOUR\"", json);
            StringAssert.Contains("\"weeklyBeefConsumption\":\"LESS_THAN_ONCE_PER_WEEK\"", json);
            StringAssert.Contains("\"weeklyFoodWaste\":\"ZERO\"", json);
            StringAssert.Contains("\"weeklyUpfConsumption\":\"TEN_TO_FOURTEEN\"", json);
            StringAssert.Contains("\"weeklyReusableOrRefill\":\"TEN_PLUS\"", json);
            StringAssert.DoesNotContain("meatMeals", json);
            StringAssert.DoesNotContain("foodWasteFrequency", json);
        }

        [Test]
        public void OnboardingSurveyData_HasAnswers_ReturnsFalseWhenEmptyAndTrueWhenPopulated()
        {
            var emptySurvey = new OnboardingSurveyData();
            Assert.IsFalse(emptySurvey.HasAnswers());

            var populatedSurvey = new OnboardingSurveyData { weeklyMeatConsumption = "ZERO_TO_FOUR" };
            Assert.IsTrue(populatedSurvey.HasAnswers());
        }
    }
}
