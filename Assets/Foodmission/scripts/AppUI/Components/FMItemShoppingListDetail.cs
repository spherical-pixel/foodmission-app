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
    public partial class FMItemShoppingListDetail : VisualElement
    {


        /* ========= UXML ATTRIBUTES ========= */
        [UxmlAttribute("text")]
        [CreateProperty]
        public string Text
        {
            get => _text?.text ?? "";
            set
            {
                if (_text != null)
                {
                    _text.text = value;
                }
            }
        }


        /* ========= INTERNAL ELEMENTS ========= */
        protected Unity.AppUI.UI.Text _text;
        protected Unity.AppUI.UI.Button _openButton;
        protected VisualElement _buttonsContainer;
        protected Unity.AppUI.UI.Button _removeButton;
        protected Unity.AppUI.UI.Button _editButton;
        protected Unity.AppUI.UI.Checkbox _checkbox;

        public Unity.AppUI.UI.Button OpenButton => _openButton;
        public Unity.AppUI.UI.Button RemoveButton => _removeButton;
        public Unity.AppUI.UI.Button EditButton => _editButton;
        public Unity.AppUI.UI.Checkbox Checkbox => _checkbox;

        public FMItemShoppingListDetail()
        {
            this.AddToClassList("fm-shopping-list-detail-item");

            _checkbox = new Unity.AppUI.UI.Checkbox();
            _checkbox.style.marginRight = 10;
            _checkbox.style.marginLeft = 15;
            this.Add(_checkbox);

            _text = new Unity.AppUI.UI.Text();
            _text.primary = true;
            this.Add(_text);

            _openButton = new Unity.AppUI.UI.Button();
            _openButton.AddToClassList("fm-full-button");
            _openButton.quiet = true;

            _text.Add(_openButton);

            _buttonsContainer = new VisualElement();
            _buttonsContainer.style.position = Position.Absolute;
            _buttonsContainer.style.right = 0;
            _buttonsContainer.style.flexDirection = FlexDirection.Row;
            _buttonsContainer.style.height = Length.Percent(100);
            _buttonsContainer.style.paddingRight = 10;
            _buttonsContainer.style.justifyContent = Justify.FlexEnd;
            _buttonsContainer.style.alignContent = Align.Center;
            _buttonsContainer.style.alignItems = Align.Center;
            this.Add(_buttonsContainer);

            _editButton = new Unity.AppUI.UI.Button();
            // _editButton.style.position = Position.Absolute;
            // _editButton.style.right = 30;
            _editButton.quiet = true;
            _editButton.leadingIcon = "fm-edit";
            _editButton.size = Size.S;
            _editButton.AddToClassList("fm-icon-button-item-list");
            _buttonsContainer.Add(_editButton);

            _removeButton = new Unity.AppUI.UI.Button();
            // _removeButton.style.position = Position.Absolute;
            // _removeButton.style.right = 0;
            _removeButton.quiet = true;
            _removeButton.leadingIcon = "fm-trash";
            _removeButton.size = Size.S;
            _removeButton.AddToClassList("fm-icon-button-item-list");
            _buttonsContainer.Add(_removeButton);



        }


    }
}
