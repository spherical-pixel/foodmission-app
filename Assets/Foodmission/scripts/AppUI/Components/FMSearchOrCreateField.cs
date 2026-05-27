using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine;
using Unity.Properties;


namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMSearchOrCreateField : VisualElement
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
        

        /* ========= INTERNAL ELEMENTS ========= */
        protected Unity.AppUI.UI.TextField _textField;
        protected Unity.AppUI.UI.Button _actionButton;

        public Unity.AppUI.UI.TextField TextField => _textField;
        public Unity.AppUI.UI.Button ActionButton => _actionButton;

        public FMSearchOrCreateField()
        {
            
            _textField = new Unity.AppUI.UI.TextField();
            _textField.AddToClassList("item-field");
            this.Add(_textField);

            _actionButton = new Unity.AppUI.UI.Button();
            _actionButton.style.position = Position.Absolute;
            _actionButton.style.right = 0;
            _actionButton.quiet = true;
            

            _actionButton.leadingIcon = "fm-add-icon";
            this.Add(_actionButton);


            _textField.RegisterValueChangedCallback(OnTextFieldValueChanged);            
        }

        private void OnTextFieldValueChanged(ChangeEvent<string> evt)
        {
            this.NotifyPropertyChanged(nameof(TextFieldValue));
        }

        

        
        
    }
}
