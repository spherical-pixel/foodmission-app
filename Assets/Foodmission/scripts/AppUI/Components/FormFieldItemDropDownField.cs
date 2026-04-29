
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

        [UxmlAttribute("dropdown-selectiontype")] [CreateProperty]
        public PickerSelectionType DropdownSelectionType
        {
            get => _dropdown?.selectionType ?? PickerSelectionType.Single;
            set
            {
                if (_dropdown != null)
                {
                    _dropdown.selectionType = value;
                }
            }
        }

        [UxmlAttribute("dropdown-closeonselect")] [CreateProperty]
        public bool DropdownCloseOnSelect
        {
            get => _dropdown?.closeOnSelection ?? false;
            set
            {
                if (_dropdown != null)
                {
                    _dropdown.closeOnSelection = value;
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