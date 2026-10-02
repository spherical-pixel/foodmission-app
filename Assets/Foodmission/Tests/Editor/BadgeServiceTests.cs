using System;

using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class BadgeServiceTests
    {
        private const string SampleJson = @"{
            ""badges"": [
                { ""code"": ""FIRST_STEP"", ""name"": ""Primer paso"", ""description"": ""Completa tu registro."",
                  ""imageUrl"": null, ""sortOrder"": 1, ""ruleCode"": ""FIRST_STEP"",
                  ""earned"": true, ""earnedAt"": ""2026-10-01T09:30:00.000Z"", ""progress"": 100,
                  ""status"": ""COMPLETED"", ""counters"": { ""registrations"": 1 } },
                { ""code"": ""CHEF"", ""name"": ""Chef"", ""description"": ""Mira 5 recetas distintas."",
                  ""imageUrl"": null, ""sortOrder"": 10, ""ruleCode"": ""CHEF"",
                  ""earned"": false, ""earnedAt"": null, ""progress"": 60,
                  ""status"": ""IN_PROGRESS"", ""counters"": { ""recipesViewed"": 3 } }
            ],
            ""earnedCount"": 1,
            ""totalCount"": 10
        }";

        [Test]
        public void UserBadgesResponse_Deserializes_AllFields()
        {
            var response = JsonConvert.DeserializeObject<UserBadgesResponse>(SampleJson);

            Assert.AreEqual(2, response.badges.Length);
            Assert.AreEqual(1, response.earnedCount);
            Assert.AreEqual(10, response.totalCount);

            UserBadge first = response.badges[0];
            Assert.AreEqual("FIRST_STEP", first.code);
            Assert.AreEqual("Primer paso", first.name);
            Assert.IsTrue(first.earned);
            Assert.IsTrue(first.earnedAt.HasValue);
            Assert.AreEqual(new DateTime(2026, 10, 1, 9, 30, 0, DateTimeKind.Utc), first.earnedAt.Value.ToUniversalTime());

            UserBadge chef = response.badges[1];
            Assert.IsFalse(chef.earned);
            Assert.IsNull(chef.earnedAt);
            Assert.AreEqual(60f, chef.progress);
            Assert.AreEqual(10, chef.sortOrder);
        }

        [Test]
        public void BuildMyBadgesUrl_IncludesEscapedLang()
        {
            string url = BadgeService.BuildMyBadgesUrl("https://api.test", "es");
            Assert.AreEqual("https://api.test/api/v1/badges/me?lang=es", url);
        }

        [Test]
        public void ContentReward_BadgeName_IsNotSerialized()
        {
            string json = JsonConvert.SerializeObject(new ContentReward { badgeId = "CHEF", badgeName = "Chef" });
            StringAssert.DoesNotContain("badgeName", json);
        }

        [Test]
        public void BadgeSprites_Address_EarnedAndDisabled()
        {
            Assert.AreEqual("badges/badge_CHEF", BadgeSprites.Address("CHEF", true));
            Assert.AreEqual("badges/badge_CHEF_disabled", BadgeSprites.Address("CHEF", false));
        }

        [TestCase("FIRST_STEP", true)]
        [TestCase("LEGENDARY_ADVENTURER", true)]
        [TestCase("3f2a9c1e-1b2c-4d5e-8f90-123456789abc", false)]
        [TestCase("Maestro Sostenible", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void BadgeSprites_LooksLikeCode(string code, bool expected)
        {
            Assert.AreEqual(expected, BadgeSprites.LooksLikeCode(code));
        }
    }
}
