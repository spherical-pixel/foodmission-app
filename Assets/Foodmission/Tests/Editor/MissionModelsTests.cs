using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionModelsTests
    {
        [Test]
        public void MissionLevel_Constants_ShouldMatchExpectedValues()
        {
            Assert.AreEqual("BEGINNER", MissionLevel.Beginner);
            Assert.AreEqual("INTERMEDIATE", MissionLevel.Intermediate);
            Assert.AreEqual("ADVANCED", MissionLevel.Advanced);
            Assert.AreEqual(3, MissionLevel.All.Length);
        }

        [Test]
        public void Mission_Deserialization_ShouldPopulateAllFields()
        {
            string json = @"{
                ""id"": ""m-uuid-123"",
                ""code"": ""M.A1.1"",
                ""dimensionId"": ""dim-uuid-456"",
                ""topicId"": ""topic-uuid-789"",
                ""level"": ""BEGINNER"",
                ""title"": ""Plastic-Free Week"",
                ""duration"": ""1 week"",
                ""goal"": ""Avoid single-use plastics for one week"",
                ""whyItMatters"": ""Reducing plastic waste protects oceans and wildlife"",
                ""health"": false,
                ""foodChoice"": true,
                ""foodWaste"": false,
                ""available"": true,
                ""progress"": 50.0
            }";

            var mission = JsonConvert.DeserializeObject<Mission>(json);

            Assert.IsNotNull(mission);
            Assert.AreEqual("m-uuid-123", mission.id);
            Assert.AreEqual("M.A1.1", mission.code);
            Assert.AreEqual("dim-uuid-456", mission.dimensionId);
            Assert.AreEqual("topic-uuid-789", mission.topicId);
            Assert.AreEqual("BEGINNER", mission.level);
            Assert.AreEqual("Plastic-Free Week", mission.title);
            Assert.AreEqual("1 week", mission.duration);
            Assert.AreEqual("Avoid single-use plastics for one week", mission.goal);
            Assert.AreEqual("Reducing plastic waste protects oceans and wildlife", mission.whyItMatters);
            Assert.IsFalse(mission.health);
            Assert.IsTrue(mission.foodChoice);
            Assert.IsFalse(mission.foodWaste);
            Assert.IsTrue(mission.available);
            Assert.AreEqual(50f, mission.progress);
        }

        [Test]
        public void MissionProgress_Deserialization_ShouldPopulateFields()
        {
            string json = @"{
                ""missionId"": ""m-uuid-123"",
                ""userId"": ""user-uuid-999"",
                ""progress"": 75.5,
                ""completed"": true,
                ""missionTitle"": ""Plastic-Free Week""
            }";

            var progress = JsonConvert.DeserializeObject<MissionProgress>(json);

            Assert.IsNotNull(progress);
            Assert.AreEqual("m-uuid-123", progress.missionId);
            Assert.AreEqual("user-uuid-999", progress.userId);
            Assert.AreEqual(75.5f, progress.progress);
            Assert.IsTrue(progress.completed);
            Assert.AreEqual("Plastic-Free Week", progress.missionTitle);
        }

        [Test]
        public void UpdateMissionProgressRequest_ToJsonBody_ShouldProduceValidJson()
        {
            var req = new UpdateMissionProgressRequest
            {
                progress = 100f,
                completed = true
            };

            byte[] body = req.ToJsonBody();
            string json = Encoding.UTF8.GetString(body);

            Assert.IsTrue(json.Contains("\"progress\":100.0"));
            Assert.IsTrue(json.Contains("\"completed\":true"));
        }

        [Test]
        public void UpdateMissionProgressRequest_ToJsonBody_ShouldIgnoreNullFields()
        {
            var req = new UpdateMissionProgressRequest
            {
                completed = true
            };

            byte[] body = req.ToJsonBody();
            string json = Encoding.UTF8.GetString(body);

            Assert.IsFalse(json.Contains("\"progress\""));
            Assert.IsTrue(json.Contains("\"completed\":true"));
        }
    }
}
