using Unity.AppUI.UI;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using Unity.Properties;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FormFieldItemPassword : FormFieldItemTextField
    {

        public FormFieldItemPassword() : base()
        {
            TextFieldIsPassword = true;
            IconButtonVisible = true;
            IconButtonIcon = "eye";
            IconButtonQuiet = true;
            _iconButton.clicked += IconButtonClicked;
        }

        public override AccessibilityNode CreateAccessibilityNode(AccessibilityHierarchy hierarchy, string label)
        {
            base.CreateAccessibilityNode(hierarchy, label);
            if (_accessibilityNode != null)
            {
                _accessibilityNode.hint = "password field";
            }
            return _accessibilityNode;
        }

        private void IconButtonClicked()
        {
            TextFieldIsPassword = !TextFieldIsPassword;
            IconButtonIcon = TextFieldIsPassword ? "eye" : "eye-slash";
        }
    }
}