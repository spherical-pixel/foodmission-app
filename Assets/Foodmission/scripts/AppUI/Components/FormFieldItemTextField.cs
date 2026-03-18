
using Unity.AppUI.UI;
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

        private void OnTextFieldValueChanged(ChangeEvent<string> evt)
        {
            this.NotifyPropertyChanged(nameof(TextFieldValue));
        }
        
    }
}