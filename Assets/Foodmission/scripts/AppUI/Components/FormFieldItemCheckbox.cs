
using UnityEngine.UIElements;
using Unity.Properties;
using Unity.AppUI.UI;
using System;


namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FormFieldItemCheckbox : FormFieldItemBase
    {

        /* ========= UXML ATTRIBUTES ========= */

        [UxmlAttribute("checkbox-value")][CreateProperty]
        public CheckboxState CheckboxValue
        {
            get => _checkBox?.value ?? CheckboxState.Unchecked;
            set
            {
                if( _checkBox != null)
                {
                    _checkBox.value = value;
                }
            }
        }

        [UxmlAttribute("text")][CreateProperty]
        public string Text
        {
            get => _button?.title ?? "";
            set
            {
                if( _button != null)
                {
                    _button.title = value;
                }
            }
        }


        /* ========= INTERNAL ELEMENTS ========= */
        protected Unity.AppUI.UI.Checkbox _checkBox;
        
        protected Unity.AppUI.UI.Button _button;

        public Unity.AppUI.UI.Button Button
        {
            get => _button;
            //set => _button = value;
        }

        public FormFieldItemCheckbox()
        {
            VisualElement ve = new VisualElement();
            ve.AddToClassList("item-field");
            ve.style.flexDirection = FlexDirection.Row;

            _fieldContainer.Add(ve);

            _checkBox = new Checkbox();
            ve.Add(_checkBox);

            Spacer space = new Spacer
            {
                spacing = Unity.AppUI.UI.SpacerSpacing.S
            };
            ve.Add(space);

            _button = new Unity.AppUI.UI.Button()
            {
                quiet = true
            };

            
            ve.Add(_button);

            _headingContainer.style.display = DisplayStyle.None;

            _checkBox.RegisterValueChangedCallback(OnCheckBoxValueChanged);
        }

        private void OnCheckBoxValueChanged(ChangeEvent<CheckboxState> evt)
        {
            this.NotifyPropertyChanged(nameof(CheckboxValue));
        }
        
    }
}