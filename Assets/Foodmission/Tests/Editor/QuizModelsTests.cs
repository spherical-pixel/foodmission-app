using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class QuizModelsTests
    {
        [Test]
        public void QuizLevel_Constants_ShouldMatchExpectedValues()
        {
            Assert.AreEqual("BEGINNER", QuizLevel.Beginner);
            Assert.AreEqual("INTERMEDIATE", QuizLevel.Intermediate);
            Assert.AreEqual("ADVANCED", QuizLevel.Advanced);
            Assert.AreEqual(3, QuizLevel.All.Length);
        }

        [Test]
        public void QuizOptionLabel_Constants_ShouldMatchExpectedValues()
        {
            Assert.AreEqual("A", QuizOptionLabel.A);
            Assert.AreEqual("B", QuizOptionLabel.B);
            Assert.AreEqual("C", QuizOptionLabel.C);
            Assert.AreEqual("D", QuizOptionLabel.D);
            Assert.AreEqual(4, QuizOptionLabel.All.Length);
        }

        [Test]
        public void Quiz_Deserialization_ShouldPopulateAllFields()
        {
            string json = @"{
                ""id"": ""quiz-uuid-123"",
                ""code"": ""Q1.1.1"",
                ""topicId"": ""topic-uuid-456"",
                ""question"": ""What is the main driver of food-related emissions?"",
                ""explanation"": ""Livestock production generates significant GHG emissions."",
                ""source"": ""EU Food Waste Report 2024"",
                ""level"": ""BEGINNER"",
                ""health"": true,
                ""foodChoice"": true,
                ""foodWaste"": false,
                ""available"": true,
                ""options"": [
                    { ""id"": ""opt-1"", ""label"": ""A"", ""text"": ""Option A text"", ""sortOrder"": 0 },
                    { ""id"": ""opt-2"", ""label"": ""B"", ""text"": ""Option B text"", ""sortOrder"": 1 }
                ]
            }";

            var quiz = JsonConvert.DeserializeObject<Quiz>(json);

            Assert.IsNotNull(quiz);
            Assert.AreEqual("quiz-uuid-123", quiz.id);
            Assert.AreEqual("Q1.1.1", quiz.code);
            Assert.AreEqual("topic-uuid-456", quiz.topicId);
            Assert.AreEqual("What is the main driver of food-related emissions?", quiz.question);
            Assert.AreEqual("Livestock production generates significant GHG emissions.", quiz.explanation);
            Assert.AreEqual("EU Food Waste Report 2024", quiz.source);
            Assert.AreEqual("BEGINNER", quiz.level);
            Assert.IsTrue(quiz.health);
            Assert.IsTrue(quiz.foodChoice);
            Assert.IsFalse(quiz.foodWaste);
            Assert.IsTrue(quiz.available);
            Assert.IsNotNull(quiz.options);
            Assert.AreEqual(2, quiz.options.Length);
            Assert.AreEqual("opt-1", quiz.options[0].id);
            Assert.AreEqual("A", quiz.options[0].label);
            Assert.AreEqual("Option A text", quiz.options[0].text);
            Assert.AreEqual(0, quiz.options[0].sortOrder);
        }

        [Test]
        public void QuizProgress_Deserialization_ShouldHandleNullableAndCompleted()
        {
            string jsonWithNullCorrect = @"{
                ""id"": ""prog-1"",
                ""userId"": ""user-1"",
                ""quizId"": ""quiz-1"",
                ""quizCode"": ""Q1.1.1"",
                ""question"": ""Sample Question?"",
                ""selectedOptionId"": null,
                ""isCorrect"": null,
                ""completed"": false,
                ""answeredAt"": null
            }";

            var prog1 = JsonConvert.DeserializeObject<QuizProgress>(jsonWithNullCorrect);
            Assert.IsNotNull(prog1);
            Assert.AreEqual("prog-1", prog1.id);
            Assert.AreEqual("user-1", prog1.userId);
            Assert.AreEqual("quiz-1", prog1.quizId);
            Assert.AreEqual("Q1.1.1", prog1.quizCode);
            Assert.IsNull(prog1.selectedOptionId);
            Assert.IsNull(prog1.isCorrect);
            Assert.IsFalse(prog1.completed);
            Assert.IsNull(prog1.answeredAt);

            string jsonCompleted = @"{
                ""id"": ""prog-2"",
                ""userId"": ""user-1"",
                ""quizId"": ""quiz-1"",
                ""quizCode"": ""Q1.1.1"",
                ""selectedOptionId"": ""opt-1"",
                ""isCorrect"": true,
                ""completed"": true,
                ""answeredAt"": ""2026-08-17T12:00:00.000Z""
            }";

            var prog2 = JsonConvert.DeserializeObject<QuizProgress>(jsonCompleted);
            Assert.IsNotNull(prog2);
            Assert.AreEqual("prog-2", prog2.id);
            Assert.AreEqual("opt-1", prog2.selectedOptionId);
            Assert.IsTrue(prog2.isCorrect);
            Assert.IsTrue(prog2.completed);
            Assert.AreEqual("2026-08-17T12:00:00.000Z", prog2.answeredAt);
        }

        [Test]
        public void PaginatedQuizResponse_Deserialization_ShouldParseDataAndMeta()
        {
            string json = @"{
                ""data"": [
                    { ""id"": ""q1"", ""code"": ""Q1.1.1"", ""question"": ""Q1?"", ""options"": [] },
                    { ""id"": ""q2"", ""code"": ""Q1.1.2"", ""question"": ""Q2?"", ""options"": [] }
                ],
                ""meta"": {
                    ""page"": 1,
                    ""limit"": 10,
                    ""total"": 25,
                    ""totalPages"": 3,
                    ""hasNext"": true,
                    ""hasPrevious"": false
                }
            }";

            var response = JsonConvert.DeserializeObject<PaginatedQuizResponse>(json);
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.data);
            Assert.AreEqual(2, response.data.Length);
            Assert.AreEqual("q1", response.data[0].id);
            Assert.AreEqual("q2", response.data[1].id);

            Assert.IsNotNull(response.meta);
            Assert.AreEqual(1, response.meta.page);
            Assert.AreEqual(10, response.meta.limit);
            Assert.AreEqual(25, response.meta.total);
            Assert.AreEqual(3, response.meta.totalPages);
            Assert.IsTrue(response.meta.hasNext);
            Assert.IsFalse(response.meta.hasPrevious);
        }

        [Test]
        public void UpdateQuizProgressRequest_ToJsonBody_ShouldProduceValidJson()
        {
            var req = new UpdateQuizProgressRequest
            {
                selectedLabel = "B"
            };

            byte[] bytes = req.ToJsonBody();
            string json = Encoding.UTF8.GetString(bytes);

            Assert.IsTrue(json.Contains("\"selectedLabel\":\"B\""));
        }
    }
}
