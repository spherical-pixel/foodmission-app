using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ChallengeModelsTests
    {
        [Test]
        public void ChallengeLevel_Constants_ShouldMatchExpectedValues()
        {
            Assert.AreEqual("BEGINNER", ChallengeLevel.Beginner);
            Assert.AreEqual("INTERMEDIATE", ChallengeLevel.Intermediate);
            Assert.AreEqual("ADVANCED", ChallengeLevel.Advanced);
            Assert.AreEqual(3, ChallengeLevel.All.Length);
        }

        [Test]
        public void Challenge_Deserialization_ShouldPopulateAllFields()
        {
            string json = @"{
                ""id"": ""ch-uuid-123"",
                ""code"": ""CH.A1.1"",
                ""dimensionId"": ""dim-uuid-456"",
                ""topicId"": ""topic-uuid-789"",
                ""level"": ""BEGINNER"",
                ""title"": ""Bring Your Own Bag"",
                ""task"": ""Use a reusable shopping bag for your groceries today"",
                ""whyItMatters"": ""Reusable bags cut plastic waste from everyday shopping."",
                ""tags"": [""FOOD_CHOICE"", ""FOOD_AND_WASTE""],
                ""health"": false,
                ""foodChoice"": true,
                ""foodWaste"": false,
                ""available"": true,
                ""progress"": 50.0
            }";

            var challenge = JsonConvert.DeserializeObject<Challenge>(json);

            Assert.IsNotNull(challenge);
            Assert.AreEqual("ch-uuid-123", challenge.id);
            Assert.AreEqual("CH.A1.1", challenge.code);
            Assert.AreEqual("dim-uuid-456", challenge.dimensionId);
            Assert.AreEqual("topic-uuid-789", challenge.topicId);
            Assert.AreEqual("BEGINNER", challenge.level);
            Assert.AreEqual("Bring Your Own Bag", challenge.title);
            Assert.AreEqual("Use a reusable shopping bag for your groceries today", challenge.task);
            Assert.AreEqual("Reusable bags cut plastic waste from everyday shopping.", challenge.whyItMatters);
            Assert.IsNotNull(challenge.tags);
            Assert.AreEqual(2, challenge.tags.Length);
            Assert.AreEqual("FOOD_CHOICE", challenge.tags[0]);
            Assert.AreEqual("FOOD_AND_WASTE", challenge.tags[1]);
            Assert.IsFalse(challenge.health);
            Assert.IsTrue(challenge.foodChoice);
            Assert.IsFalse(challenge.foodWaste);
            Assert.IsTrue(challenge.available);
            Assert.AreEqual(50f, challenge.progress);
        }

        [Test]
        public void ChallengeProgress_Deserialization_ShouldPopulateAllFields()
        {
            string json = @"{
                ""challengeId"": ""ch-uuid-123"",
                ""userId"": ""user-uuid-456"",
                ""progress"": 100.0,
                ""completed"": true,
                ""challengeTitle"": ""Bring Your Own Bag""
            }";

            var progress = JsonConvert.DeserializeObject<ChallengeProgress>(json);

            Assert.IsNotNull(progress);
            Assert.AreEqual("ch-uuid-123", progress.challengeId);
            Assert.AreEqual("user-uuid-456", progress.userId);
            Assert.AreEqual(100f, progress.progress);
            Assert.IsTrue(progress.completed);
            Assert.AreEqual("Bring Your Own Bag", progress.challengeTitle);
        }

        [Test]
        public void UpdateChallengeProgressRequest_Serialization_ProducesValidJson()
        {
            var req = new UpdateChallengeProgressRequest
            {
                completed = true,
                progress = 100f
            };

            byte[] bytes = req.ToJsonBody();
            string json = Encoding.UTF8.GetString(bytes);

            Assert.IsTrue(json.Contains("\"completed\":true"));
            Assert.IsTrue(json.Contains("\"progress\":100"));
        }

        [Test]
        public void UpdateChallengeProgressRequest_NullValues_ShouldBeOmitted()
        {
            var req = new UpdateChallengeProgressRequest
            {
                completed = true,
                progress = null
            };

            byte[] bytes = req.ToJsonBody();
            string json = Encoding.UTF8.GetString(bytes);

            Assert.IsTrue(json.Contains("\"completed\":true"));
            Assert.IsFalse(json.Contains("\"progress\""));
        }
    }
}
