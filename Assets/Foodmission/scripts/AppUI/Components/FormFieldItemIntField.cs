
using Unity.AppUI.UI;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using Unity.Properties;


namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FormFieldItemIntField : FormFieldItemBase
    {

        /* ========= UXML ATTRIBUTES ========= */
        [UxmlAttribute("intField-value")][CreateProperty]
        public int IntFieldValue
        {
            get => _intField?.value ?? 0;
            set
            {
                if( _intField != null)
                {
                    _intField.value = value;
                }
            }
        }

        /* ========= INTERNAL ELEMENTS ========= */
        protected Unity.AppUI.UI.IntField _intField;
        


        public FormFieldItemIntField()
        {
            _intField = new Unity.AppUI.UI.IntField();
            _intField.AddToClassList("item-field");
            _fieldContainer.Add(_intField);

            _intField.RegisterValueChangedCallback(OnIntFieldValueChanged);
            
        }

        public override AccessibilityNode CreateAccessibilityNode(AccessibilityHierarchy hierarchy, string label)
        {
            base.CreateAccessibilityNode(hierarchy, label);
            if (_accessibilityNode == null) return null;

            _accessibilityNode.role = AccessibilityRole.TextField;
            _accessibilityNode.value = _intField?.value.ToString() ?? "0";
            _accessibilityNode.hint = "numeric field";
            _accessibilityNode.frameGetter = MakeFrameGetter(_intField);

            if (_intField != null && !_intField.enabledSelf)
            {
                _accessibilityNode.state = AccessibilityState.Disabled;
            }

            return _accessibilityNode;
        }

        public override void UpdateAccessibilityNode()
        {
            if (_accessibilityNode != null && _intField != null)
            {
                _accessibilityNode.value = _intField.value.ToString();
            }
        }

        private void OnIntFieldValueChanged(ChangeEvent<int> evt)
        {
            this.NotifyPropertyChanged(nameof(IntFieldValue));
            UpdateAccessibilityNode();
        }
        
    }
}