using System;
using System.Linq;

using Newtonsoft.Json;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CurrentQuestViewTests
    {
        private static CurrentQuestItem Item(string type, string code, string status, int sortOrder, float progress = 0f, string title = null) =>
            new CurrentQuestItem { contentType = type, contentCode = code, status = status, sortOrder = sortOrder, progress = progress, title = title ?? code };

        [Test]
        public void TimelineStates_FollowSortOrderAndStatus()
        {
            var quest = new CurrentQuest
            {
                items = new[]
                {
                    Item(QuestContentType.Mission, "M1", CurrentQuestItemStatus.InProgress, 3, 40f),
                    Item(QuestContentType.Quiz, "Q1", CurrentQuestItemStatus.Completed, 1),
                    Item(QuestContentType.FoodFact, "F1", CurrentQuestItemStatus.NotStarted, 2),
                },
            };

            CollectionAssert.AreEqual(new[] { true, false, false }, CurrentQuestView.TimelineStates(quest));
        }

        [Test]
        public void TimelineStates_CompletedQuest_MarksEverything()
        {
            var quest = new CurrentQuest
            {
                completed = true,
                items = new[] { Item(QuestContentType.Quiz, "Q1", CurrentQuestItemStatus.Failed, 1) },
            };

            CollectionAssert.AreEqual(new[] { true }, CurrentQuestView.TimelineStates(quest));
        }

        [Test]
        public void TimelineStates_NoItems_IsEmpty()
        {
            Assert.AreEqual(0, CurrentQuestView.TimelineStates(new CurrentQuest()).Length);
            Assert.AreEqual(0, CurrentQuestView.TimelineStates(null).Length);
        }

        [Test]
        public void ToQuest_MapsFieldsAndItemTitles()
        {
            var current = new CurrentQuest
            {
                questId = "q-1",
                questCode = "QUEST.DIET_CHANGES.BEGINNER.2",
                name = "Meat-free days",
                description = "Try it",
                dimensionCode = "DIET_CHANGES",
                level = "BEGINNER",
                items = new[] { Item(QuestContentType.Quiz, "Q1", CurrentQuestItemStatus.Completed, 1, title: "Sugar quiz") },
            };

            Quest quest = CurrentQuestView.ToQuest(current);

            Assert.AreEqual("q-1", quest.id);
            Assert.AreEqual("QUEST.DIET_CHANGES.BEGINNER.2", quest.code);
            Assert.AreEqual("Meat-free days", quest.GetDisplayName());
            Assert.AreEqual("Try it", quest.description);
            Assert.AreEqual("DIET_CHANGES", quest.dimensionId);
            Assert.AreEqual("BEGINNER", quest.level);
            Assert.AreEqual("Sugar quiz", quest.items.Single().label);
            Assert.AreEqual("Q1", quest.items.Single().contentCode);
        }

        [Test]
        public void StatusOf_MapsCompletedFailedAndMissionPercent()
        {
            var current = new CurrentQuest
            {
                items = new[]
                {
                    Item(QuestContentType.Mission, "M1", CurrentQuestItemStatus.Failed, 1, 30f),
                    Item(QuestContentType.Mission, "M2", CurrentQuestItemStatus.InProgress, 2, 40f),
                    Item(QuestContentType.Quiz, "Q1", CurrentQuestItemStatus.Failed, 3),
                    Item(QuestContentType.FoodFact, "F1", CurrentQuestItemStatus.Completed, 4, 100f),
                },
            };
            var statusOf = CurrentQuestView.StatusLookup(current);

            QuestItemStatus m1 = statusOf(new QuestItem { contentType = QuestContentType.Mission, contentCode = "M1" });
            QuestItemStatus m2 = statusOf(new QuestItem { contentType = QuestContentType.Mission, contentCode = "M2" });
            QuestItemStatus q1 = statusOf(new QuestItem { contentType = QuestContentType.Quiz, contentCode = "Q1" });
            QuestItemStatus f1 = statusOf(new QuestItem { contentType = QuestContentType.FoodFact, contentCode = "F1" });
            QuestItemStatus unknown = statusOf(new QuestItem { contentType = QuestContentType.Quiz, contentCode = "X" });

            Assert.IsTrue(m1.Failed);
            Assert.IsFalse(m1.Completed);
            Assert.AreEqual(40f, m2.MissionPercent);
            Assert.IsFalse(m2.Failed);
            // A wrong quiz answer is just "not done", as before; only missions show a failed state
            Assert.IsFalse(q1.Failed);
            Assert.IsFalse(q1.Completed);
            Assert.IsTrue(f1.Completed);
            Assert.IsFalse(unknown.Completed);
        }

        [Test]
        public void CurrentQuestItem_StartedAtReported_OnlyWhenTheFieldIsSent()
        {
            CurrentQuestItem legacy = JsonConvert.DeserializeObject<CurrentQuestItem>("{\"contentType\":\"MISSION\",\"contentCode\":\"M1\",\"status\":\"NOT_STARTED\"}");
            CurrentQuestItem notStarted = JsonConvert.DeserializeObject<CurrentQuestItem>("{\"contentType\":\"MISSION\",\"contentCode\":\"M1\",\"status\":\"NOT_STARTED\",\"startedAt\":null}");
            CurrentQuestItem started = JsonConvert.DeserializeObject<CurrentQuestItem>("{\"contentType\":\"MISSION\",\"contentCode\":\"M1\",\"status\":\"IN_PROGRESS\",\"startedAt\":\"2026-10-15T08:00:00Z\"}");

            Assert.IsFalse(legacy.StartedAtReported);
            Assert.IsTrue(notStarted.StartedAtReported);
            Assert.IsNull(notStarted.startedAt);
            Assert.IsTrue(started.StartedAtReported);
            Assert.IsNotNull(started.startedAt);
        }

        [Test]
        public void StatusLookup_MissionWithoutStartTime_IsNotStarted()
        {
            var current = new CurrentQuest
            {
                items = new[]
                {
                    new CurrentQuestItem { contentType = QuestContentType.Mission, contentCode = "M1", status = CurrentQuestItemStatus.NotStarted, startedAt = null },
                    new CurrentQuestItem { contentType = QuestContentType.Mission, contentCode = "M2", status = CurrentQuestItemStatus.InProgress, startedAt = DateTime.UtcNow },
                    new CurrentQuestItem { contentType = QuestContentType.Mission, contentCode = "M3", status = CurrentQuestItemStatus.NotStarted },
                    new CurrentQuestItem { contentType = QuestContentType.Quiz, contentCode = "Q1", status = CurrentQuestItemStatus.NotStarted, startedAt = null },
                }
            };
            Func<QuestItem, QuestItemStatus> statusOf = CurrentQuestView.StatusLookup(current);

            Assert.IsTrue(statusOf(new QuestItem { contentType = QuestContentType.Mission, contentCode = "M1" }).NotStarted);
            Assert.IsFalse(statusOf(new QuestItem { contentType = QuestContentType.Mission, contentCode = "M2" }).NotStarted);
            Assert.IsFalse(statusOf(new QuestItem { contentType = QuestContentType.Mission, contentCode = "M3" }).NotStarted, "backend without #424");
            Assert.IsFalse(statusOf(new QuestItem { contentType = QuestContentType.Quiz, contentCode = "Q1" }).NotStarted, "only missions");
        }

        private static CurrentQuestItem Mission(string code, string status, DateTime? startedAt, float progress = 0f) =>
            new CurrentQuestItem { contentType = QuestContentType.Mission, contentCode = code, status = status, startedAt = startedAt, progress = progress };

        [Test]
        public void MissionsWaitingToStart_NoneRunning_CountsTheNotStartedAndFailedOnes()
        {
            var quest = new CurrentQuest
            {
                items = new[]
                {
                    Mission("M1", CurrentQuestItemStatus.NotStarted, null),
                    Mission("M2", CurrentQuestItemStatus.NotStarted, null),
                    Mission("M3", CurrentQuestItemStatus.Completed, DateTime.UtcNow, 100f),
                    Mission("M4", CurrentQuestItemStatus.Failed, DateTime.UtcNow, 20f),
                    Item(QuestContentType.Quiz, "Q1", CurrentQuestItemStatus.NotStarted, 5),
                },
            };

            Assert.AreEqual(3, CurrentQuestView.MissionsWaitingToStart(quest));
        }

        [Test]
        public void MissionsWaitingToStart_OnlyAFailedMissionLeft_CountsIt()
        {
            var quest = new CurrentQuest
            {
                items = new[]
                {
                    Mission("M1", CurrentQuestItemStatus.Completed, DateTime.UtcNow, 100f),
                    Mission("M2", CurrentQuestItemStatus.Failed, DateTime.UtcNow, 30f),
                },
            };

            Assert.AreEqual(1, CurrentQuestView.MissionsWaitingToStart(quest));
        }

        [Test]
        public void MissionsWaitingToStart_OneRunning_IsZero()
        {
            var quest = new CurrentQuest
            {
                items = new[]
                {
                    Mission("M1", CurrentQuestItemStatus.NotStarted, null),
                    Mission("M2", CurrentQuestItemStatus.InProgress, DateTime.UtcNow),
                },
            };

            Assert.AreEqual(0, CurrentQuestView.MissionsWaitingToStart(quest));
        }

        [Test]
        public void MissionsWaitingToStart_NothingToRemind_IsZero()
        {
            var allDone = new CurrentQuest { items = new[] { Mission("M1", CurrentQuestItemStatus.Completed, DateTime.UtcNow, 100f) } };
            var completed = new CurrentQuest { completed = true, items = new[] { Mission("M1", CurrentQuestItemStatus.NotStarted, null) } };
            var legacy = new CurrentQuest { items = new[] { new CurrentQuestItem { contentType = QuestContentType.Mission, contentCode = "M1", status = CurrentQuestItemStatus.NotStarted } } };

            Assert.AreEqual(0, CurrentQuestView.MissionsWaitingToStart(allDone));
            Assert.AreEqual(0, CurrentQuestView.MissionsWaitingToStart(completed));
            Assert.AreEqual(0, CurrentQuestView.MissionsWaitingToStart(legacy), "backend without #424");
            Assert.AreEqual(0, CurrentQuestView.MissionsWaitingToStart(null));
        }
    }
}
