using NUnit.Framework;

using UnityEngine.Accessibility;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class IAccessibleComponentContractTests
    {
        [Test]
        public void FormFieldItemBase_CreateAccessibilityNode_ReturnsNodeWithLabel()
        {
            var component = new TestableFormFieldItem();

            AccessibilityHierarchy hierarchy = new AccessibilityHierarchy();
            AccessibilityNode node = component.CreateAccessibilityNode(hierarchy, "Test Label");

            Assert.IsNotNull(node);
            Assert.AreSame(node, component.AccessibilityNode);
        }

        [Test]
        public void FormFieldItemBase_CreateAccessibilityNode_UsesHeadingAsFallback()
        {
            var component = new TestableFormFieldItem();

            AccessibilityHierarchy hierarchy = new AccessibilityHierarchy();
            AccessibilityNode node = component.CreateAccessibilityNode(hierarchy, null);

            Assert.IsNotNull(node);
        }

        [Test]
        public void FormFieldItemBase_DestroyAccessibilityNode_ClearsNode()
        {
            var component = new TestableFormFieldItem();
            AccessibilityHierarchy hierarchy = new AccessibilityHierarchy();
            component.CreateAccessibilityNode(hierarchy, "Test");

            component.DestroyAccessibilityNode();

            Assert.IsNull(component.AccessibilityNode);
        }

        [Test]
        public void FormFieldItemBase_DoubleCreateAccessibilityNode_ReplacesOldNode()
        {
            var component = new TestableFormFieldItem();
            AccessibilityHierarchy hierarchy = new AccessibilityHierarchy();

            AccessibilityNode first = component.CreateAccessibilityNode(hierarchy, "First");
            AccessibilityNode second = component.CreateAccessibilityNode(hierarchy, "Second");

            Assert.AreSame(second, component.AccessibilityNode);
            Assert.AreNotSame(first, component.AccessibilityNode);
        }

        [Test]
        public void FormFieldItemBase_UpdateAccessibilityNode_DoesNotThrow()
        {
            var component = new TestableFormFieldItem();

            Assert.DoesNotThrow(() => component.UpdateAccessibilityNode());
        }
    }

    /// <summary>
    /// Concrete subclass of FormFieldItemBase for testing.
    /// FormFieldItemBase is abstract in the sense that it's meant to be
    /// subclassed, but is not actually declared abstract. We subclass it
    /// to access its public interface without instantiating FormFieldItemTextField etc.
    /// </summary>
    public class TestableFormFieldItem : FormFieldItemBase
    {
    }
}
