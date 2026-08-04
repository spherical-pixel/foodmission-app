using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CatalogModelsTests
    {
        [Test]
        public void CatalogItem_Roundtrips_Via_JsonUtility()
        {
            var item = new CatalogItem { code = "M", label = "Male" };
            string json = JsonUtility.ToJson(item);
            var result = JsonUtility.FromJson<CatalogItem>(json);
            Assert.AreEqual("M", result.code);
            Assert.AreEqual("Male", result.label);
        }

        [Test]
        public void CatalogData_Roundtrips_Via_JsonUtility()
        {
            var data = new CatalogData
            {
                genders = new[] { new CatalogItem { code = "M", label = "Male" } },
                activityLevels = new[] { new CatalogItem { code = "SED", label = "Sedentary" } }
            };
            string json = JsonUtility.ToJson(data);
            var result = JsonUtility.FromJson<CatalogData>(json);
            Assert.IsNotNull(result.genders);
            Assert.AreEqual(1, result.genders.Length);
            Assert.AreEqual("Male", result.genders[0].label);
            Assert.IsNotNull(result.activityLevels);
            Assert.AreEqual("SED", result.activityLevels[0].code);
        }

        [Test]
        public void CatalogData_WithAllArrays_SerializesCorrectly()
        {
            var data = new CatalogData
            {
                genders = new[] { new CatalogItem { code = "F", label = "Female" } },
                dietaryPreferences = new[] { new CatalogItem { code = "VEG", label = "Vegetarian" } },
                educationLevels = new[] { new CatalogItem { code = "BACH", label = "Bachelor" } },
                annualIncomeLevels = new[] { new CatalogItem { code = "LOW", label = "Low" } },
                shoppingResponsibilities = new[] { new CatalogItem { code = "MAIN", label = "Main shopper" } }
            };
            string json = JsonUtility.ToJson(data);
            var result = JsonUtility.FromJson<CatalogData>(json);
            Assert.AreEqual(1, result.genders.Length);
            Assert.AreEqual(1, result.dietaryPreferences.Length);
            Assert.AreEqual(1, result.educationLevels.Length);
            Assert.AreEqual(1, result.annualIncomeLevels.Length);
            Assert.AreEqual(1, result.shoppingResponsibilities.Length);
        }

        [Test]
        public void StartupResponse_WithOnboardingAndIndicators_Roundtrips_Via_JsonUtility()
        {
            var resp = new StartupResponse
            {
                data = new CatalogData
                {
                    genders = new[] { new CatalogItem { code = "F", label = "Female" } },
                    onboarding = new CatalogOnboardingStartupData
                    {
                        weeklyMeatRanges = new[] { new CatalogItem { code = "1_2_TIMES", label = "1-2 times/week" } },
                        weeklyBeefFrequencies = new[] { new CatalogItem { code = "RARELY", label = "Rarely" } },
                        weeklyFoodWasteRanges = new[] { new CatalogItem { code = "NONE", label = "None" } },
                        weeklyUpfRanges = new[] { new CatalogItem { code = "LOW", label = "Low" } },
                        weeklyReusableRanges = new[] { new CatalogItem { code = "ALWAYS", label = "Always" } },
                        userSegments = new[] { new CatalogItem { code = "ECO_WARRIOR", label = "Eco Warrior" } },
                        motivations = new[] { new CatalogItem { code = "HEALTH", label = "Health" } }
                    },
                    progressIndicatorKinds = new[] { new CatalogItem { code = "STREAK", label = "Streak" } },
                    progressPrecisions = new[] { new CatalogItem { code = "INTEGER", label = "Integer" } },
                    walletCurrencies = new[] { new CatalogItem { code = "XP", label = "Experience Points" } }
                }
            };

            string json = JsonUtility.ToJson(resp);
            var result = JsonUtility.FromJson<StartupResponse>(json);

            Assert.IsNotNull(result.data);
            Assert.AreEqual("Female", result.data.genders[0].label);

            Assert.IsNotNull(result.data.onboarding);
            Assert.AreEqual(1, result.data.onboarding.weeklyMeatRanges.Length);
            Assert.AreEqual("1-2 times/week", result.data.onboarding.weeklyMeatRanges[0].label);
            Assert.AreEqual("Rarely", result.data.onboarding.weeklyBeefFrequencies[0].label);
            Assert.AreEqual("Eco Warrior", result.data.onboarding.userSegments[0].label);

            Assert.IsNotNull(result.data.progressIndicatorKinds);
            Assert.AreEqual("STREAK", result.data.progressIndicatorKinds[0].code);

            Assert.IsNotNull(result.data.progressPrecisions);
            Assert.AreEqual("Integer", result.data.progressPrecisions[0].label);

            Assert.IsNotNull(result.data.walletCurrencies);
            Assert.AreEqual("XP", result.data.walletCurrencies[0].code);
        }
    }
}
