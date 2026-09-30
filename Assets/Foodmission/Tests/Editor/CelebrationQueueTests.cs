using NUnit.Framework;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CelebrationQueueTests
    {
        private CelebrationQueue<string> _queue;

        [SetUp]
        public void SetUp()
        {
            _queue = new CelebrationQueue<string>();
        }

        [Test]
        public void TryBegin_WhenIdle_ShowsImmediately()
        {
            Assert.IsTrue(_queue.TryBegin("fact"));
        }

        [Test]
        public void TryBegin_WhileShowing_QueuesInsteadOfReplacing()
        {
            _queue.TryBegin("fact");

            Assert.IsFalse(_queue.TryBegin("challenge"));
        }

        [Test]
        public void OnDismissed_ShowsQueuedCelebrationsInArrivalOrder()
        {
            _queue.TryBegin("fact");
            _queue.TryBegin("challenge");
            _queue.TryBegin("quest");

            Assert.IsTrue(_queue.OnDismissed(out string next));
            Assert.AreEqual("challenge", next);
            Assert.IsTrue(_queue.OnDismissed(out next));
            Assert.AreEqual("quest", next);
            Assert.IsFalse(_queue.OnDismissed(out next));
        }

        [Test]
        public void OnDismissed_WhenQueueEmpty_NextCelebrationShowsImmediately()
        {
            _queue.TryBegin("fact");
            _queue.OnDismissed(out _);

            Assert.IsTrue(_queue.TryBegin("challenge"));
        }
    }
}
