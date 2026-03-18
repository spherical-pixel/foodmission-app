
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using Unity.Properties;
using System.Diagnostics.Contracts;


namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FormFieldItemDropDownField : FormFieldItemBase
    {

        /* ========= UXML ATTRIBUTES ========= */
        [UxmlAttribute("dropdown-defaultmessage")] [CreateProperty]
        public string DropdownDefaultMessage
        {
            get => _dropdown?.defaultMessage ?? "";
            set
            {
                if (_dropdown != null)
                {
                    _dropdown.defaultMessage = value;
                }
            }
        }
        
        public Dropdown Dropdown
        {
            get => _dropdown;
            set
            {
                _dropdown = value;
            }
        }

        /* ========= INTERNAL ELEMENTS ========= */
        protected Unity.AppUI.UI.Dropdown _dropdown;
        

        public FormFieldItemDropDownField()
        {
            _dropdown = new Unity.AppUI.UI.Dropdown();
            _dropdown.AddToClassList("item-field");
            _fieldContainer.Add(_dropdown);
        }
        
    }
}