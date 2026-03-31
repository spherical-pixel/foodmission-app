using NUnit.Framework;
using eu.foodmission.platform;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class NotificationCardTests
    {
        private VisualTreeAsset _template;

        [SetUp]
        public void SetUp()
        {
            _template = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Foodmission/AppUI/Template/NotificationCard.uxml");
        }

        [Test]
        public void Bind_SetsTextLabel()
        {
            var card = new NotificationCard(_template);
            var model = new NotificationModel
            {
                Id = "1", Text = "Hello world", Timestamp = "1 h",
                Type = NotificationType.Social, IsRead = false
            };

            card.Bind(model);

            var label = card.Q<Label>("text");
            Assert.AreEqual("Hello world", label.text);
        }

        [Test]
        public void Bind_SetsTimestampLabel()
        {
            var card = new NotificationCard(_template);
            var model = new NotificationModel
            {
                Id = "2", Text = "Test", Timestamp = "2 h",
                Type = NotificationType.Badge, IsRead = false
            };

            card.Bind(model);

            var label = card.Q<Label>("timestamp");
            Assert.AreEqual("2 h", label.text);
        }

        [Test]
        public void Bind_UnreadNotification_DoesNotHaveReadClass()
        {
            var card = new NotificationCard(_template);
            var model = new NotificationModel
            {
                Id = "3", Text = "Test", Timestamp = "1 h",
                Type = NotificationType.System, IsRead = false
            };

            card.Bind(model);

            Assert.IsFalse(card.ClassListContains("fm-notification-card--read"));
        }

        [Test]
        public void Bind_ReadNotification_HasReadClass()
        {
            var card = new NotificationCard(_template);
            var model = new NotificationModel
            {
                Id = "4", Text = "Test", Timestamp = "1 h",
                Type = NotificationType.System, IsRead = true
            };

            card.Bind(model);

            Assert.IsTrue(card.ClassListContains("fm-notification-card--read"));
        }

        [Test]
        public void Bind_CalledTwice_UpdatesState()
        {
            var card = new NotificationCard(_template);
            var model = new NotificationModel
            {
                Id = "5", Text = "First", Timestamp = "1 h",
                Type = NotificationType.Social, IsRead = false
            };
            card.Bind(model);

            model.Text = "Second";
            model.IsRead = true;
            card.Bind(model);

            var label = card.Q<Label>("text");
            Assert.AreEqual("Second", label.text);
            Assert.IsTrue(card.ClassListContains("fm-notification-card--read"));
        }

        [Test]
        public void OverflowBtn_WhenBound_IsPresent()
        {
            var card = new NotificationCard(_template);
            var model = new NotificationModel
            {
                Id = "99", Text = "Test", Timestamp = "1 h",
                Type = NotificationType.Social, IsRead = false
            };
            card.Bind(model);

            Assert.IsNotNull(card.Q<Unity.AppUI.UI.ActionButton>("overflow-btn"));
        }
    }
}
