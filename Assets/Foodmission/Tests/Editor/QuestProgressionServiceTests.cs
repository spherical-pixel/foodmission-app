using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using eu.foodmission.platform;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class QuestProgressionServiceTests
    {
        private IQuestProgressionService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new QuestProgressionService();
        }

        [Test]
        public void EvaluateProgression_FirstBeginnerQuest_IsAlwaysUnlocked()
        {
            var quests = new List<Quest>
            {
                new Quest { id = "q1", code = "QUEST.DIET.BEGINNER.1", dimensionId = "dim1", level = "BEGINNER", title = "B1" },
                new Quest { id = "q2", code = "QUEST.DIET.BEGINNER.2", dimensionId = "dim1", level = "BEGINNER", title = "B2" }
            };

            var states = _service.EvaluateProgression(quests, new List<QuestProgress>());

            Assert.AreEqual(2, states.Count);
            Assert.IsTrue(states[0].IsUnlocked, "First beginner quest should be unlocked");
            Assert.IsFalse(states[0].IsLocked);
            Assert.IsFalse(states[1].IsUnlocked, "Second beginner quest should be locked when first is not completed");
            Assert.IsTrue(states[1].IsLocked);
            Assert.AreEqual("q1", states[1].PreviousQuest?.id);
        }

        [Test]
        public void EvaluateProgression_CompletingFirst_UnlocksSecond()
        {
            var quests = new List<Quest>
            {
                new Quest { id = "q1", code = "QUEST.DIET.BEGINNER.1", dimensionId = "dim1", level = "BEGINNER", title = "B1" },
                new Quest { id = "q2", code = "QUEST.DIET.BEGINNER.2", dimensionId = "dim1", level = "BEGINNER", title = "B2" }
            };

            var progress = new List<QuestProgress>
            {
                new QuestProgress { questId = "q1", completed = true, progress = 100f }
            };

            var states = _service.EvaluateProgression(quests, progress);

            Assert.IsTrue(states[0].IsCompleted);
            Assert.IsTrue(states[0].IsUnlocked);
            Assert.IsTrue(states[1].IsUnlocked, "Second quest should be unlocked when first is completed");
            Assert.IsFalse(states[1].IsLocked);
            Assert.IsFalse(states[1].IsCompleted);
        }

        [Test]
        public void EvaluateProgression_LevelTransition_CompletingLastBeginner_UnlocksIntermediate1()
        {
            var quests = new List<Quest>
            {
                new Quest { id = "q1", code = "QUEST.DIET.BEGINNER.1", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "q2", code = "QUEST.DIET.BEGINNER.2", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "q3", code = "QUEST.DIET.INTERMEDIATE.1", dimensionId = "dim1", level = "INTERMEDIATE" }
            };

            var progress = new List<QuestProgress>
            {
                new QuestProgress { questId = "q1", completed = true },
                new QuestProgress { questId = "q2", completed = true }
            };

            var states = _service.EvaluateProgression(quests, progress);

            Assert.IsTrue(states[2].IsUnlocked, "Intermediate 1 should unlock after all Beginner quests complete");
            Assert.AreEqual("q2", states[2].PreviousQuest?.id);
        }

        [Test]
        public void GetNextQuest_ReturnsSuccessorInSameDimension()
        {
            var quests = new List<Quest>
            {
                new Quest { id = "q1", code = "QUEST.DIET.BEGINNER.1", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "q2", code = "QUEST.DIET.BEGINNER.2", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "q3", code = "QUEST.DIET.INTERMEDIATE.1", dimensionId = "dim1", level = "INTERMEDIATE" }
            };

            var next = _service.GetNextQuest("QUEST.DIET.BEGINNER.1", quests);
            Assert.IsNotNull(next);
            Assert.AreEqual("q2", next.id);

            var nextAfterB2 = _service.GetNextQuest("QUEST.DIET.BEGINNER.2", quests);
            Assert.IsNotNull(nextAfterB2);
            Assert.AreEqual("q3", nextAfterB2.id);

            var nextAfterLast = _service.GetNextQuest("QUEST.DIET.INTERMEDIATE.1", quests);
            Assert.IsNull(nextAfterLast, "Last quest in dimension should return null as next quest");
        }

        [Test]
        public void GetPreviousQuest_ReturnsPredecessorInSameDimension()
        {
            var quests = new List<Quest>
            {
                new Quest { id = "q1", code = "QUEST.DIET.BEGINNER.1", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "q2", code = "QUEST.DIET.BEGINNER.2", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "q3", code = "QUEST.DIET.INTERMEDIATE.1", dimensionId = "dim1", level = "INTERMEDIATE" }
            };

            var prevB1 = _service.GetPreviousQuest("QUEST.DIET.BEGINNER.1", quests);
            Assert.IsNull(prevB1, "First quest in dimension has no previous quest");

            var prevB2 = _service.GetPreviousQuest("QUEST.DIET.BEGINNER.2", quests);
            Assert.IsNotNull(prevB2);
            Assert.AreEqual("q1", prevB2.id);

            var prevInt1 = _service.GetPreviousQuest("QUEST.DIET.INTERMEDIATE.1", quests);
            Assert.IsNotNull(prevInt1);
            Assert.AreEqual("q2", prevInt1.id);
        }

        [Test]
        public void EvaluateProgression_MultipleDimensions_OperateIndependently()
        {
            var quests = new List<Quest>
            {
                new Quest { id = "d1_b1", code = "QUEST.DIM1.BEGINNER.1", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "d1_b2", code = "QUEST.DIM1.BEGINNER.2", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "d2_b1", code = "QUEST.DIM2.BEGINNER.1", dimensionId = "dim2", level = "BEGINNER" },
                new Quest { id = "d2_b2", code = "QUEST.DIM2.BEGINNER.2", dimensionId = "dim2", level = "BEGINNER" },
            };

            // Complete d1_b1 only
            var progress = new List<QuestProgress>
            {
                new QuestProgress { questId = "d1_b1", completed = true }
            };

            var states = _service.EvaluateProgression(quests, progress);

            var d1_b1_state = states.FirstOrDefault(s => s.Quest.id == "d1_b1");
            var d1_b2_state = states.FirstOrDefault(s => s.Quest.id == "d1_b2");
            var d2_b1_state = states.FirstOrDefault(s => s.Quest.id == "d2_b1");
            var d2_b2_state = states.FirstOrDefault(s => s.Quest.id == "d2_b2");

            Assert.IsTrue(d1_b1_state.IsCompleted);
            Assert.IsTrue(d1_b2_state.IsUnlocked);
            Assert.IsTrue(d2_b1_state.IsUnlocked, "Dim2 first quest should be unlocked");
            Assert.IsFalse(d2_b1_state.IsCompleted);
            Assert.IsTrue(d2_b2_state.IsLocked, "Dim2 second quest should be locked");
        }
    }
}
