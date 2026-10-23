using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// UI Toolkit draws the caret of an empty field one unscaled line height below the top,
    /// ignoring the text alignment and the font scale. <see cref="EmptyFieldCaret"/> draws it
    /// where the first character will be laid out instead.
    /// </summary>
    [TestFixture]
    public class EmptyFieldCaretTests
    {
        // Content box of an App UI TextField input measured in the Editor: padding 2, 410x87, Roboto 42 → line 50
        private static readonly Rect Content = new Rect(2f, 0f, 410f, 87f);
        private const float LineHeight = 50f;
        private const float Width = 1f;

        [Test]
        public void ComputeCaretRect_MiddleLeft_CentersTheLineAtTheLeftEdge()
        {
            var rect = EmptyFieldCaret.ComputeCaretRect(Content, LineHeight, Width, TextAnchor.MiddleLeft);

            Assert.AreEqual(2f, rect.x, 0.001f);
            Assert.AreEqual(18.5f, rect.y, 0.001f);
            Assert.AreEqual(Width, rect.width, 0.001f);
            Assert.AreEqual(LineHeight, rect.height, 0.001f);
        }

        [Test]
        public void ComputeCaretRect_UpperLeft_StartsAtTheTop()
        {
            var rect = EmptyFieldCaret.ComputeCaretRect(Content, LineHeight, Width, TextAnchor.UpperLeft);

            Assert.AreEqual(2f, rect.x, 0.001f);
            Assert.AreEqual(0f, rect.y, 0.001f);
        }

        [Test]
        public void ComputeCaretRect_LowerRight_EndsAtTheBottomRightEdge()
        {
            var rect = EmptyFieldCaret.ComputeCaretRect(Content, LineHeight, Width, TextAnchor.LowerRight);

            Assert.AreEqual(411f, rect.x, 0.001f);
            Assert.AreEqual(37f, rect.y, 0.001f);
        }

        [Test]
        public void ComputeCaretRect_MiddleCenter_IsHorizontallyCentered()
        {
            var rect = EmptyFieldCaret.ComputeCaretRect(Content, LineHeight, Width, TextAnchor.MiddleCenter);

            Assert.AreEqual(206.5f, rect.x, 0.001f);
            Assert.AreEqual(18.5f, rect.y, 0.001f);
        }

        [Test]
        public void ShouldDraw_EmptyEditableField_IsTrue()
        {
            Assert.IsTrue(EmptyFieldCaret.ShouldDraw("", isReadOnly: false, showsPlaceholder: false));
        }

        [Test]
        public void ShouldDraw_NullText_IsTrue()
        {
            Assert.IsTrue(EmptyFieldCaret.ShouldDraw(null, isReadOnly: false, showsPlaceholder: false));
        }

        [Test]
        public void ShouldDraw_FieldWithText_IsFalse()
        {
            Assert.IsFalse(EmptyFieldCaret.ShouldDraw("a", isReadOnly: false, showsPlaceholder: false));
        }

        [Test]
        public void ShouldDraw_ReadOnlyField_IsFalse()
        {
            Assert.IsFalse(EmptyFieldCaret.ShouldDraw("", isReadOnly: true, showsPlaceholder: false));
        }

        [Test]
        public void ShouldDraw_NativePlaceholderShown_IsFalse()
        {
            // The native placeholder is real laid-out text, so the native caret already sits on its first character
            Assert.IsFalse(EmptyFieldCaret.ShouldDraw("", isReadOnly: false, showsPlaceholder: true));
        }
    }
}
