using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class NotificationModelTests
    {
        [Test]
        public void NotificationModel_Roundtrips_Via_JsonUtility()
        {
            var model = new NotificationModel
            {
                Id = "notif-1",
                Text = "Time to log lunch",
                Timestamp = "2026-05-29T12:00:00Z",
                Type = NotificationType.System,
                IsRead = false
            };
            string json = JsonUtility.ToJson(model);
            var result = JsonUtility.FromJson<NotificationModel>(json);
            Assert.AreEqual("notif-1", result.Id);
            Assert.AreEqual("Time to log lunch", result.Text);
            Assert.AreEqual("2026-05-29T12:00:00Z", result.Timestamp);
            Assert.AreEqual(NotificationType.System, result.Type);
            Assert.IsFalse(result.IsRead);
        }

        [Test]
        public void NotificationModel_WithReadTrue_SerializesCorrectly()
        {
            var model = new NotificationModel { Id = "n2", IsRead = true, Type = NotificationType.Badge };
            string json = JsonUtility.ToJson(model);
            var result = JsonUtility.FromJson<NotificationModel>(json);
            Assert.IsTrue(result.IsRead);
            Assert.AreEqual(NotificationType.Badge, result.Type);
        }

        [Test]
        public void NotificationType_DefaultValueIsSocial()
        {
            var model = new NotificationModel { Id = "n3" };
            Assert.AreEqual(NotificationType.Social, model.Type);
        }
    }
}
