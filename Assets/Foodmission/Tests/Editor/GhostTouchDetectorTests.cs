using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// Sequences taken from iOS device logs of issue #27: after the soft keyboard shows or hides,
    /// iOS replays the last touch as a new PointerDown/MouseDown at the exact same position.
    /// </summary>
    [TestFixture]
    public class GhostTouchDetectorTests
    {
        private static readonly Vector2 FieldTap = new Vector2(852f, 1388f);

        private GhostTouchDetector _detector;

        [SetUp]
        public void SetUp()
        {
            _detector = new GhostTouchDetector();
        }

        [Test]
        public void PointerDown_FirstTap_IsNotGhost()
        {
            Assert.IsFalse(_detector.ShouldIgnorePointerDown(FieldTap, 10f, 100));
        }

        [Test]
        public void PointerDown_ReplayedAtSamePositionAfterKeyboardShown_IsGhost()
        {
            _detector.ShouldIgnorePointerDown(FieldTap, 10f, 100);
            _detector.RecordPointerUp(FieldTap);
            _detector.NotifyKeyboardTransition(10.1f);

            Assert.IsTrue(_detector.ShouldIgnorePointerDown(FieldTap, 10.5f, 120));
        }

        [Test]
        public void PointerDown_ReplayedAtSamePositionAfterKeyboardHidden_IsGhost()
        {
            _detector.ShouldIgnorePointerDown(FieldTap, 10f, 100);
            _detector.NotifyKeyboardTransition(11f);

            Assert.IsTrue(_detector.ShouldIgnorePointerDown(FieldTap, 11.02f, 140));
        }

        [Test]
        public void PointerDown_SamePositionWithoutKeyboardTransition_IsNotGhost()
        {
            _detector.ShouldIgnorePointerDown(FieldTap, 10f, 100);
            _detector.RecordPointerUp(FieldTap);

            Assert.IsFalse(_detector.ShouldIgnorePointerDown(FieldTap, 10.5f, 120));
        }

        [Test]
        public void PointerDown_SamePositionLongAfterKeyboardTransition_IsNotGhost()
        {
            _detector.ShouldIgnorePointerDown(FieldTap, 10f, 100);
            _detector.NotifyKeyboardTransition(10.1f);

            Assert.IsFalse(_detector.ShouldIgnorePointerDown(FieldTap, 12f, 200));
        }

        [Test]
        public void PointerDown_DifferentPositionAfterKeyboardTransition_IsNotGhost()
        {
            _detector.ShouldIgnorePointerDown(FieldTap, 10f, 100);
            _detector.NotifyKeyboardTransition(10.1f);

            Assert.IsFalse(_detector.ShouldIgnorePointerDown(new Vector2(955f, 1104f), 10.5f, 120));
        }

        [Test]
        public void PointerDown_LastPositionComesFromPointerUp()
        {
            _detector.ShouldIgnorePointerDown(FieldTap, 10f, 100);
            var dragEnd = new Vector2(462f, 1946f);
            _detector.RecordPointerUp(dragEnd);
            _detector.NotifyKeyboardTransition(10.5f);

            Assert.IsTrue(_detector.ShouldIgnorePointerDown(dragEnd, 10.6f, 130));
        }

        [Test]
        public void PointerDown_RepeatedGhosts_AreAllIgnored()
        {
            _detector.ShouldIgnorePointerDown(FieldTap, 10f, 100);
            _detector.NotifyKeyboardTransition(10.1f);

            Assert.IsTrue(_detector.ShouldIgnorePointerDown(FieldTap, 10.5f, 120));
            Assert.IsTrue(_detector.ShouldIgnorePointerDown(FieldTap, 10.6f, 125));
        }

        [Test]
        public void MouseDown_CompatibilityEventOfRealPointerDown_IsNotGhost()
        {
            _detector.NotifyKeyboardTransition(9.9f);
            _detector.ShouldIgnorePointerDown(FieldTap, 10f, 100);

            Assert.IsFalse(_detector.ShouldIgnoreMouseDown(FieldTap, 10f, 100));
        }

        [Test]
        public void MouseDown_CompatibilityEventOfGhostPointerDown_IsGhost()
        {
            _detector.ShouldIgnorePointerDown(FieldTap, 10f, 100);
            _detector.NotifyKeyboardTransition(10.1f);
            _detector.ShouldIgnorePointerDown(FieldTap, 10.5f, 120);

            Assert.IsTrue(_detector.ShouldIgnoreMouseDown(FieldTap, 10.5f, 120));
        }

        [Test]
        public void MouseDown_StandaloneReplayAfterKeyboardTransition_IsGhost()
        {
            _detector.ShouldIgnorePointerDown(FieldTap, 10f, 100);
            _detector.NotifyKeyboardTransition(10.1f);

            Assert.IsTrue(_detector.ShouldIgnoreMouseDown(FieldTap, 10.15f, 102));
        }

        [Test]
        public void MouseDown_StandaloneWithoutKeyboardTransition_IsNotGhost()
        {
            _detector.ShouldIgnorePointerDown(FieldTap, 10f, 100);

            Assert.IsFalse(_detector.ShouldIgnoreMouseDown(FieldTap, 10.15f, 102));
        }
    }
}
