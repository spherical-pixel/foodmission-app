using System;
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
            // Pin the flag so these tests don't depend on FM_UNLOCK_ALL_QUESTS being defined
            _service = new QuestProgressionService { UnlockAllQuests = false };
        }

        [Test]
        public void EvaluateProgression_WhenUnlockAllQuests_EveryQuestIsUnlocked()
        {
            var service = new QuestProgressionService { UnlockAllQuests = true };
            var quests = new List<Quest>
            {
                new Quest { id = "q1", code = "QUEST.DIET.BEGINNER.1", dimensionId = "dim1", level = "BEGINNER", title = "B1" },
                new Quest { id = "q2", code = "QUEST.DIET.BEGINNER.2", dimensionId = "dim1", level = "BEGINNER", title = "B2" },
                new Quest { id = "q3", code = "QUEST.DIET.INTERMEDIATE.1", dimensionId = "dim1", level = "INTERMEDIATE", title = "I1" }
            };

            var states = service.EvaluateProgression(quests, new List<QuestProgress>());

            Assert.AreEqual(3, states.Count);
            Assert.IsTrue(states.All(s => s.IsUnlocked), "All quests should be unlocked");
            Assert.IsTrue(states.All(s => !s.IsCompleted), "Unlocking must not mark quests as completed");
            Assert.AreEqual("q1", states[1].PreviousQuest?.id);
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

        private static Func<string, string> Levels(string level) => _ => level;

        [Test]
        public void EvaluateProgression_ChainRestartsPerDifficulty()
        {
            var quests = new List<Quest>
            {
                new Quest { id = "q1", code = "QUEST.DIET.BEGINNER.1", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "q2", code = "QUEST.DIET.BEGINNER.2", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "q3", code = "QUEST.DIET.INTERMEDIATE.1", dimensionId = "dim1", level = "INTERMEDIATE" },
                new Quest { id = "q4", code = "QUEST.DIET.INTERMEDIATE.2", dimensionId = "dim1", level = "INTERMEDIATE" }
            };

            var states = _service.EvaluateProgression(quests, new List<QuestProgress>(), Levels("ADVANCED"));

            Assert.IsTrue(states[0].IsUnlocked, "Beginner 1");
            Assert.AreEqual(ContentLockReason.Sequence, states[1].LockReason, "Beginner 2 waits for Beginner 1");
            Assert.IsTrue(states[2].IsUnlocked, "Intermediate 1 is the first of its difficulty");
            Assert.IsNull(states[2].PreviousQuest);
            Assert.AreEqual(ContentLockReason.Sequence, states[3].LockReason);
            Assert.AreEqual("q3", states[3].PreviousQuest?.id);
        }

        [Test]
        public void EvaluateProgression_DifficultyAboveUserLevel_IsLockedByLevel()
        {
            var quests = new List<Quest>
            {
                new Quest { id = "q1", code = "QUEST.DIET.BEGINNER.1", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "q3", code = "QUEST.DIET.INTERMEDIATE.1", dimensionId = "dim1", level = "INTERMEDIATE" },
                new Quest { id = "q4", code = "QUEST.DIET.INTERMEDIATE.2", dimensionId = "dim1", level = "INTERMEDIATE" }
            };

            var states = _service.EvaluateProgression(quests, new List<QuestProgress>(), Levels("BEGINNER"));

            Assert.AreEqual(ContentLockReason.None, states[0].LockReason);
            Assert.AreEqual(ContentLockReason.Level, states[1].LockReason);
            Assert.AreEqual(ContentLockReason.Level, states[2].LockReason);
        }

        [Test]
        public void EvaluateProgression_StartedQuestAboveLevel_StaysUnlocked()
        {
            var quests = new List<Quest>
            {
                new Quest { id = "q3", code = "QUEST.DIET.ADVANCED.1", dimensionId = "dim1", level = "ADVANCED" },
                new Quest { id = "q4", code = "QUEST.DIET.ADVANCED.2", dimensionId = "dim1", level = "ADVANCED" }
            };
            var progress = new List<QuestProgress> { new QuestProgress { questId = "q4", progress = 30f } };

            var states = _service.EvaluateProgression(quests, progress, Levels("BEGINNER"));

            Assert.AreEqual(ContentLockReason.Level, states[0].LockReason);
            Assert.AreEqual(ContentLockReason.None, states[1].LockReason, "Started quests are never locked");
        }

        [Test]
        public void GetNextQuest_LastOfDifficulty_ReturnsNull()
        {
            var quests = new List<Quest>
            {
                new Quest { id = "q1", code = "QUEST.DIET.BEGINNER.1", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "q2", code = "QUEST.DIET.BEGINNER.2", dimensionId = "dim1", level = "BEGINNER" },
                new Quest { id = "q3", code = "QUEST.DIET.INTERMEDIATE.1", dimensionId = "dim1", level = "INTERMEDIATE" }
            };

            Assert.AreEqual("q2", _service.GetNextQuest("q1", quests)?.id);
            Assert.IsNull(_service.GetNextQuest("q2", quests), "Completing the last beginner quest unlocks nothing new");
            Assert.IsNull(_service.GetPreviousQuest("q3", quests), "First intermediate quest has no predecessor");
        }

        [Test]
        public void EvaluateProgression_NoLevelResolver_OnlySequenceLocks()
        {
            var quests = new List<Quest>
            {
                new Quest { id = "q3", code = "QUEST.DIET.ADVANCED.1", dimensionId = "dim1", level = "ADVANCED" }
            };

            var states = _service.EvaluateProgression(quests, new List<QuestProgress>());

            Assert.IsTrue(states[0].IsUnlocked);
        }

        [Test]
        public void EvaluateProgression_UnlockAllQuests_IgnoresLevels()
        {
            var service = new QuestProgressionService { UnlockAllQuests = true };
            var quests = new List<Quest>
            {
                new Quest { id = "q3", code = "QUEST.DIET.ADVANCED.2", dimensionId = "dim1", level = "ADVANCED" }
            };

            var states = service.EvaluateProgression(quests, new List<QuestProgress>(), Levels("BEGINNER"));

            Assert.AreEqual(ContentLockReason.None, states[0].LockReason);
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
            Assert.IsNull(nextAfterB2, "The chain is per difficulty: the last beginner quest has no next quest");

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
            Assert.IsNull(prevInt1, "The first intermediate quest starts its own chain");
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
