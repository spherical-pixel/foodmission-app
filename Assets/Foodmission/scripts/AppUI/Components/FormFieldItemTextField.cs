
using Unity.AppUI.UI;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using Unity.Properties;


namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FormFieldItemTextField : FormFieldItemBase
    {

        /* ========= UXML ATTRIBUTES ========= */

        

        [UxmlAttribute("textField-placeholder")][CreateProperty]
        public string TextFieldPlaceholder
        {
            get => _textField?.placeholder ?? "";
            set
            {
                if( _textField != null)
                {
                    _textField.placeholder = value;
                }
            }
        }

        [UxmlAttribute("textField-value")][CreateProperty]
        public string TextFieldValue
        {
            get => _textField?.value ?? "";
            set
            {
                if( _textField != null)
                {
                    _textField.value = value;
                }
            }
        }

        [UxmlAttribute("textField-is-password")][CreateProperty]
        public bool TextFieldIsPassword
        {
            get => _textField?.isPassword ?? false;
            set
            {
                if( _textField != null)
                {
                    _textField.isPassword = value;
                }
            }
        }

        

        /* ========= INTERNAL ELEMENTS ========= */
        protected Unity.AppUI.UI.TextField _textField;

        public FormFieldItemTextField()
        {
            _textField = new Unity.AppUI.UI.TextField();
            _textField.AddToClassList("item-field");
            _fieldContainer.Add(_textField);

            _textField.RegisterValueChangedCallback(OnTextFieldValueChanged);            
        }

        public override AccessibilityNode CreateAccessibilityNode(AccessibilityHierarchy hierarchy, string label)
        {
            base.CreateAccessibilityNode(hierarchy, label);
            if (_accessibilityNode == null) return null;

            _accessibilityNode.role = AccessibilityRole.TextField;
            _accessibilityNode.value = _textField?.value ?? "";
            _accessibilityNode.frameGetter = MakeFrameGetter(_textField);

            if (_textField != null && !_textField.enabledSelf)
            {
                _accessibilityNode.state = AccessibilityState.Disabled;
            }

            return _accessibilityNode;
        }

        public override void UpdateAccessibilityNode()
        {
            if (_accessibilityNode != null && _textField != null)
            {
                _accessibilityNode.value = _textField.value;
            }
        }

        private void OnTextFieldValueChanged(ChangeEvent<string> evt)
        {
            this.NotifyPropertyChanged(nameof(TextFieldValue));
            UpdateAccessibilityNode();
        }
        
    }
}