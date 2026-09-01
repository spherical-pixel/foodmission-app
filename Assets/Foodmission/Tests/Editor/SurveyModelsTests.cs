using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class SurveyModelsTests
    {
        [Test]
        public void SurveyDto_Deserialization_ParsesAllFieldsCorrectly()
        {
            string json = @"{
                ""id"": ""8b6de54e-7036-4db5-8d05-bafdc54c11bc"",
                ""slug"": ""third-use"",
                ""title"": ""tercer uso"",
                ""description"": ""Encuesta: tercer uso"",
                ""createdAt"": ""2026-07-31T12:54:08.005Z"",
                ""updatedAt"": ""2026-08-27T13:00:01.687Z"",
                ""questions"": [
                    {
                        ""id"": ""101c8d3e-453c-4abb-ab1e-a7b7644ab6fc"",
                        ""key"": ""third use_0"",
                        ""text"": ""La aplicación FOODMISSION es precisa."",
                        ""type"": ""likert"",
                        ""order"": 0,
                        ""surveyId"": ""8b6de54e-7036-4db5-8d05-bafdc54c11bc"",
                        ""answers"": [
                            { ""value"": 1, ""label"": ""Totalmente en desacuerdo"" },
                            { ""value"": 2, ""label"": ""En desacuerdo"" },
                            { ""value"": 3, ""label"": ""Ni de acuerdo ni en desacuerdo"" },
                            { ""value"": 4, ""label"": ""De acuerdo"" },
                            { ""value"": 5, ""label"": ""Totalmente de acuerdo"" }
                        ]
                    }
                ]
            }";

            var survey = JsonConvert.DeserializeObject<SurveyDto>(json);

            Assert.IsNotNull(survey);
            Assert.AreEqual("8b6de54e-7036-4db5-8d05-bafdc54c11bc", survey.id);
            Assert.AreEqual("third-use", survey.slug);
            Assert.AreEqual("tercer uso", survey.title);
            Assert.AreEqual("Encuesta: tercer uso", survey.description);
            Assert.IsNotNull(survey.questions);
            Assert.AreEqual(1, survey.questions.Length);

            var q = survey.questions[0];
            Assert.AreEqual("101c8d3e-453c-4abb-ab1e-a7b7644ab6fc", q.id);
            Assert.AreEqual("third use_0", q.key);
            Assert.AreEqual("La aplicación FOODMISSION es precisa.", q.text);
            Assert.AreEqual("likert", q.type);
            Assert.AreEqual(0, q.order);
            Assert.IsNotNull(q.answers);
            Assert.AreEqual(5, q.answers.Length);
            Assert.AreEqual(1, q.answers[0].value);
            Assert.AreEqual("Totalmente en desacuerdo", q.answers[0].label);
            Assert.AreEqual(5, q.answers[4].value);
            Assert.AreEqual("Totalmente de acuerdo", q.answers[4].label);
        }

        [Test]
        public void SubmitSurveyResponseDto_Serialization_ProducesValidJson()
        {
            var dto = new SubmitSurveyResponseDto
            {
                responses = new[]
                {
                    new SubmitQuestionResponseDto { questionId = "q1", value = 4 },
                    new SubmitQuestionResponseDto { questionId = "q2", value = 5 }
                }
            };

            string json = dto.ToJson();

            Assert.IsTrue(json.Contains("\"questionId\":\"q1\""));
            Assert.IsTrue(json.Contains("\"value\":4"));
            Assert.IsTrue(json.Contains("\"questionId\":\"q2\""));
            Assert.IsTrue(json.Contains("\"value\":5"));
        }

        [Test]
        public void SurveyResponseDto_Deserialization_ParsesAllFields()
        {
            string json = @"{
                ""id"": ""resp-123"",
                ""userId"": ""user-456"",
                ""surveyId"": ""survey-789"",
                ""attemptNumber"": 2,
                ""responses"": [
                    {
                        ""id"": ""qresp-1"",
                        ""questionId"": ""q1"",
                        ""value"": 4
                    }
                ],
                ""createdAt"": ""2026-09-01T10:00:00.000Z""
            }";

            var resp = JsonConvert.DeserializeObject<SurveyResponseDto>(json);

            Assert.IsNotNull(resp);
            Assert.AreEqual("resp-123", resp.id);
            Assert.AreEqual("user-456", resp.userId);
            Assert.AreEqual("survey-789", resp.surveyId);
            Assert.AreEqual(2, resp.attemptNumber);
            Assert.IsNotNull(resp.responses);
            Assert.AreEqual(1, resp.responses.Length);
            Assert.AreEqual("q1", resp.responses[0].questionId);
            Assert.AreEqual(4, resp.responses[0].value);
        }

        [Test]
        public void PilotSurveyCycleState_Copy_CreatesDeepIndependentCopy()
        {
            var state = new PilotSurveyCycleState
            {
                currentCycle = 2,
                cycleStartDate = "2026-09-01",
                activeDatesInCycle = new System.Collections.Generic.List<string> { "2026-09-01", "2026-09-02" },
                completedSlugsInCycle = new System.Collections.Generic.List<string> { "second-use" },
                skippedSlugsInCycle = new System.Collections.Generic.List<string> { "third-use" }
            };

            var copy = state.Copy();

            Assert.AreEqual(2, copy.currentCycle);
            Assert.AreEqual("2026-09-01", copy.cycleStartDate);
            Assert.AreEqual(2, copy.activeDatesInCycle.Count);
            Assert.AreEqual(1, copy.completedSlugsInCycle.Count);
            Assert.AreEqual(1, copy.skippedSlugsInCycle.Count);

            copy.activeDatesInCycle.Add("2026-09-03");
            Assert.AreEqual(3, copy.activeDatesInCycle.Count);
            Assert.AreEqual(2, state.activeDatesInCycle.Count); // original untouched
        }
    }
}
