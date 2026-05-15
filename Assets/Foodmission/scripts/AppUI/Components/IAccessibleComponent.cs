using UnityEngine.Accessibility;

namespace eu.foodmission.platform.Components
{
    public interface IAccessibleComponent
    {
        AccessibilityNode AccessibilityNode { get; }

        AccessibilityNode CreateAccessibilityNode(AccessibilityHierarchy hierarchy, string label);

        void UpdateAccessibilityNode();

        void DestroyAccessibilityNode();
    }
}
