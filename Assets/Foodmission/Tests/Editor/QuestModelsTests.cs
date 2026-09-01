using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class QuestModelsTests
    {
        [Test]
        public void QuestContentType_Constants_ShouldMatchExpected()
        {
            Assert.AreEqual("MISSION", QuestContentType.Mission);
            Assert.AreEqual("QUIZ", QuestContentType.Quiz);
            Assert.AreEqual("FOOD_FACT", QuestContentType.FoodFact);
            Assert.AreEqual("MICRO_LEARNING", QuestContentType.MicroLearning);
        }

        [Test]
        public void Quest_Deserialization_PopulatesAllFieldsAndItems()
        {
            string json = @"{
                ""id"": ""quest-uuid-1"",
                ""code"": ""QUEST.DIET_CHANGES.BEGINNER.1"",
                ""dimensionId"": ""dim-uuid-1"",
                ""level"": ""BEGINNER"",
                ""name"": ""Learn to Log Your Food"",
                ""title"": ""Diet changes — Beginner"",
                ""description"": ""Beginner quest for sustainable changes."",
                ""available"": true,
                ""items"": [
                    {
                        ""id"": ""item-1"",
                        ""contentType"": ""MISSION"",
                        ""contentCode"": ""M.A1.1"",
                        ""label"": ""Stay in Green Zone"",
                        ""sortOrder"": 0
                    },
                    {
                        ""id"": ""item-2"",
                        ""contentType"": ""QUIZ"",
                        ""contentCode"": ""Q1.1.1"",
                        ""label"": ""Food Emissions Quiz"",
                        ""sortOrder"": 1
                    }
                ]
            }";

            var quest = JsonConvert.DeserializeObject<Quest>(json);

            Assert.IsNotNull(quest);
            Assert.AreEqual("quest-uuid-1", quest.id);
            Assert.AreEqual("QUEST.DIET_CHANGES.BEGINNER.1", quest.code);
            Assert.AreEqual("dim-uuid-1", quest.dimensionId);
            Assert.AreEqual("BEGINNER", quest.level);
            Assert.AreEqual("Learn to Log Your Food", quest.name);
            Assert.AreEqual("Diet changes — Beginner", quest.title);
            Assert.IsTrue(quest.available);
            Assert.IsNotNull(quest.items);
            Assert.AreEqual(2, quest.items.Length);
            Assert.AreEqual("MISSION", quest.items[0].contentType);
            Assert.AreEqual("M.A1.1", quest.items[0].contentCode);
            Assert.AreEqual("Stay in Green Zone", quest.items[0].label);
            Assert.AreEqual(0, quest.items[0].sortOrder);
            Assert.AreEqual("QUIZ", quest.items[1].contentType);
            Assert.AreEqual("Q1.1.1", quest.items[1].contentCode);
        }

        [Test]
        public void QuestProgress_Deserialization_PopulatesFields()
        {
            string json = @"{
                ""id"": ""prog-uuid-1"",
                ""userId"": ""user-uuid-1"",
                ""questId"": ""quest-uuid-1"",
                ""questCode"": ""QUEST.DIET_CHANGES.BEGINNER.1"",
                ""completed"": true,
                ""completedAt"": ""2026-08-26T12:00:00.000Z"",
                ""progressPercent"": 100.0
            }";

            var progress = JsonConvert.DeserializeObject<QuestProgress>(json);

            Assert.IsNotNull(progress);
            Assert.AreEqual("prog-uuid-1", progress.id);
            Assert.AreEqual("user-uuid-1", progress.userId);
            Assert.AreEqual("quest-uuid-1", progress.questId);
            Assert.AreEqual("QUEST.DIET_CHANGES.BEGINNER.1", progress.questCode);
            Assert.IsTrue(progress.completed);
            Assert.AreEqual("2026-08-26T12:00:00.000Z", progress.completedAt);
            Assert.AreEqual(100.0f, progress.progressPercent);
        }

        [Test]
        public void UpdateQuestProgressRequest_ToJsonBody_ProducesValidJson()
        {
            var req = new UpdateQuestProgressRequest
            {
                completed = true,
                progressPercent = 50f
            };

            byte[] bytes = req.ToJsonBody();
            string json = Encoding.UTF8.GetString(bytes);

            Assert.IsTrue(json.Contains("\"completed\":true"));
            Assert.IsTrue(json.Contains("\"progressPercent\":50.0"));
        }
    }
}
