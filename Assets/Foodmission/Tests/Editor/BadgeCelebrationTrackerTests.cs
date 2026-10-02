using System;

using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class BadgeCelebrationTrackerTests
    {
        private const string User = "tracker-user";
        private const string OtherUser = "tracker-user-2";
        private static readonly DateTime Now = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);

        private BadgeCelebrationTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _tracker = new BadgeCelebrationTracker();
            PlayerPrefs.DeleteKey(BadgeCelebrationTracker.StorageKey(User));
            PlayerPrefs.DeleteKey(BadgeCelebrationTracker.StorageKey(OtherUser));
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(BadgeCelebrationTracker.StorageKey(User));
            PlayerPrefs.DeleteKey(BadgeCelebrationTracker.StorageKey(OtherUser));
        }

        private static UserBadge Earned(string code, DateTime earnedAt)
        {
            return new UserBadge { code = code, earned = true, earnedAt = earnedAt, progress = 100 };
        }

        [Test]
        public void IsFirstRun_TrueUntilSomethingIsStored()
        {
            Assert.IsTrue(_tracker.IsFirstRun(User));
            _tracker.GetNewlyEarned(User, new string[0], new UserBadge[0], Now);
            Assert.IsFalse(_tracker.IsFirstRun(User));
        }

        [Test]
        public void FirstRun_SeedsOldBadges_ReturnsOnlyRecentOnes()
        {
            var details = new[]
            {
                Earned("FIRST_STEP", Now.AddMinutes(-2)),
                Earned("CHEF", Now.AddDays(-3)),
            };

            var result = _tracker.GetNewlyEarned(User, new[] { "CHEF", "FIRST_STEP" }, details, Now);

            CollectionAssert.AreEqual(new[] { "FIRST_STEP" }, result);
            // CHEF was seeded as already celebrated, so a later run does not return it.
            _tracker.MarkCelebrated(User, result);
            CollectionAssert.IsEmpty(_tracker.GetNewlyEarned(User, new[] { "CHEF", "FIRST_STEP" }, null, Now));
        }

        [Test]
        public void FirstRun_BadgeExactlyAtWindowEdge_IsRecent()
        {
            var details = new[] { Earned("FIRST_STEP", Now - BadgeCelebrationTracker.RecentWindow) };
            var result = _tracker.GetNewlyEarned(User, new[] { "FIRST_STEP" }, details, Now);
            CollectionAssert.AreEqual(new[] { "FIRST_STEP" }, result);
        }

        [Test]
        public void FirstRun_WithoutDetails_SeedsEverything()
        {
            var result = _tracker.GetNewlyEarned(User, new[] { "CHEF" }, null, Now);
            CollectionAssert.IsEmpty(result);
            Assert.IsFalse(_tracker.IsFirstRun(User));
        }

        [Test]
        public void LaterRun_ReturnsUncelebratedCodes_InProfileOrder()
        {
            _tracker.GetNewlyEarned(User, new[] { "FIRST_STEP" }, new[] { Earned("FIRST_STEP", Now.AddDays(-1)) }, Now);

            var result = _tracker.GetNewlyEarned(User, new[] { "FIRST_STEP", "STYLISH", "CHEF" }, null, Now);

            CollectionAssert.AreEqual(new[] { "STYLISH", "CHEF" }, result);
        }

        [Test]
        public void LaterRun_DoesNotMarkByItself_SoAFailedCelebrationIsRetried()
        {
            _tracker.GetNewlyEarned(User, new string[0], new UserBadge[0], Now);

            _tracker.GetNewlyEarned(User, new[] { "CHEF" }, null, Now);
            var again = _tracker.GetNewlyEarned(User, new[] { "CHEF" }, null, Now);

            CollectionAssert.AreEqual(new[] { "CHEF" }, again);
        }

        [Test]
        public void MarkCelebrated_PreventsDuplicates()
        {
            _tracker.GetNewlyEarned(User, new string[0], new UserBadge[0], Now);
            _tracker.MarkCelebrated(User, new[] { "CHEF" });

            CollectionAssert.IsEmpty(_tracker.GetNewlyEarned(User, new[] { "CHEF" }, null, Now));
        }

        [Test]
        public void State_IsIsolatedPerUser()
        {
            _tracker.GetNewlyEarned(User, new string[0], new UserBadge[0], Now);
            _tracker.MarkCelebrated(User, new[] { "CHEF" });

            Assert.IsTrue(_tracker.IsFirstRun(OtherUser));
        }

        [Test]
        public void NullOrDuplicateCodes_AreIgnored()
        {
            _tracker.GetNewlyEarned(User, new string[0], new UserBadge[0], Now);
            var result = _tracker.GetNewlyEarned(User, new[] { "CHEF", null, "", "CHEF" }, null, Now);
            CollectionAssert.AreEqual(new[] { "CHEF" }, result);
        }
    }
}
