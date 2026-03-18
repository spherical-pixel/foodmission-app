
using Unity.AppUI.UI;
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

        private void OnIntFieldValueChanged(ChangeEvent<int> evt)
        {
            this.NotifyPropertyChanged(nameof(IntFieldValue));
        }
        
    }
}