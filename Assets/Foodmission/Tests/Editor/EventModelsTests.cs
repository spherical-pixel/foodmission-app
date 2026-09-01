using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class EventModelsTests
    {
        [Test]
        public void ClientEventTypes_ContainsExpectedConstants()
        {
            Assert.AreEqual("APP_SESSION_OPENED", ClientEventTypes.AppSessionOpened);
            Assert.AreEqual("APP_SESSION_ENDED", ClientEventTypes.AppSessionEnded);
            Assert.AreEqual("MEAL_MEAT_FREE", ClientEventTypes.MealMeatFree);
            Assert.AreEqual("FOOD_WASTE_LOGGED", ClientEventTypes.FoodWasteLogged);
            Assert.AreEqual("SHOPPING_ORIGIN_CHECKED", ClientEventTypes.ShoppingOriginChecked);
            Assert.Contains(ClientEventTypes.AppSessionOpened, ClientEventTypes.All);
            Assert.Contains(ClientEventTypes.AppSessionEnded, ClientEventTypes.All);
            Assert.Contains(ClientEventTypes.MealMeatFree, ClientEventTypes.All);
            Assert.Contains(ClientEventTypes.FoodWasteLogged, ClientEventTypes.All);
        }

        [Test]
        public void ClientEventMetadata_Serializes_Via_JsonUtility()
        {
            var meta = new ClientEventMetadata
            {
                sessionId = "550e8400-e29b-41d4-a716-446655440000",
                platform = "ios",
                appVersion = "1.0.0",
                durationSeconds = 120
            };

            string json = JsonUtility.ToJson(meta);
            var deserialized = JsonUtility.FromJson<ClientEventMetadata>(json);

            Assert.AreEqual("550e8400-e29b-41d4-a716-446655440000", deserialized.sessionId);
            Assert.AreEqual("ios", deserialized.platform);
            Assert.AreEqual("1.0.0", deserialized.appVersion);
            Assert.AreEqual(120, deserialized.durationSeconds);
        }

        [Test]
        public void CreateClientEventRequest_ToJsonBody_ReturnsValidJsonBytes()
        {
            var req = new CreateClientEventRequest
            {
                eventType = ClientEventTypes.AppSessionOpened,
                metadata = new ClientEventMetadata
                {
                    sessionId = "test-session-id",
                    platform = "android",
                    appVersion = "2.0.0"
                }
            };

            byte[] body = req.ToJsonBody();
            Assert.IsNotNull(body);
            Assert.Greater(body.Length, 0);

            string json = System.Text.Encoding.UTF8.GetString(body);
            Assert.IsTrue(json.Contains("APP_SESSION_OPENED"));
            Assert.IsTrue(json.Contains("test-session-id"));
        }

        [Test]
        public void UserEvent_Deserializes_From_Json()
        {
            string json = "{\"id\":\"evt-123\",\"userId\":\"usr-456\",\"eventType\":\"APP_SESSION_OPENED\",\"source\":\"app\",\"timestamp\":\"2026-08-04T07:00:00Z\",\"groupId\":null}";
            var userEvent = JsonUtility.FromJson<UserEvent>(json);

            Assert.IsNotNull(userEvent);
            Assert.AreEqual("evt-123", userEvent.id);
            Assert.AreEqual("usr-456", userEvent.userId);
            Assert.AreEqual("APP_SESSION_OPENED", userEvent.eventType);
            Assert.AreEqual("app", userEvent.source);
            Assert.AreEqual("2026-08-04T07:00:00Z", userEvent.timestamp);
            Assert.IsTrue(string.IsNullOrEmpty(userEvent.groupId));
        }
    }
}
